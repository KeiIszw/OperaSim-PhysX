using System;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>
/// TB20e のレバー操作量を受信し、レバー→速度のアクチュエータ近似で各作業機を動かす。
/// 位置サーボは使わず、有限の速度抵抗と力上限だけを ArticulationDrive に設定する。
/// </summary>
public class Tb20eLeverController : MonoBehaviour
{
    [Serializable]
    public sealed class LeverAxis
    {
        [Tooltip("購読する std_msgs/Float64 topic 名。[robot_name] は root GameObject 名に置換される。")]
        public string topicName;

        [Tooltip("速度指令を適用する ArticulationBody。")]
        public ArticulationBody targetArticulationBody;

        [Tooltip("正のレバー入力に対する関節の回転方向。実行時は符号だけを使用する。")]
        public float positiveDirectionSign = 1.0f;

        [Min(0.0f)]
        [Tooltip("正レバー100%での無負荷角速度 [deg/s]。既存sceneの値を引き継ぐ。")]
        public float fullLeverTargetSpeedDegPerSecond = 50.0f;

        [Min(0.0f)]
        [Tooltip("負レバー側の最高速度倍率。1なら正負対称。実測値に合わせる。")]
        public float negativeSpeedRatio = 1.0f;

        [Range(0.0f, 99.0f)]
        public float deadbandPercent = 0.0f;

        [Range(0.0f, 5.0f)]
        [Tooltip("入力のむだ時間 [s]。未校正の初期値は0。")]
        public float deadTimeSeconds = 0.0f;

        [Min(0.0f)]
        [Tooltip("流量応答の一次遅れ時定数 [s]。未校正の初期値は0。")]
        public float responseTimeSeconds = 0.0f;

        [Min(0.0f)]
        [Tooltip("速度差→駆動トルクの係数（xDrive.damping）。油圧抵抗の近似。0なら駆動力も出ない。未校正。")]
        public float velocityResistance = 10000.0f;

        [NonSerialized]
        internal readonly Tb20eActuatorResponse response = new Tb20eActuatorResponse();

        [NonSerialized]
        internal float latestLeverInput;

        [NonSerialized]
        internal double lastMessageTime;

        [NonSerialized]
        internal bool hasReceivedMessage;
    }

    [Header("Lever Axes")]
    public LeverAxis boom = new LeverAxis
    {
        topicName = "/manipulated_boom_lever",
        positiveDirectionSign = -1.0f
    };

    public LeverAxis arm = new LeverAxis
    {
        topicName = "/manipulated_arm_lever"
    };

    public LeverAxis bucket = new LeverAxis
    {
        topicName = "/manipulated_bucket_lever"
    };

    public LeverAxis swing = new LeverAxis
    {
        topicName = "/manipulated_swing_lever",
        positiveDirectionSign = -1.0f
    };

    [Header("Safety")]
    [Min(0.0f)]
    [Tooltip("この秒数を超えて指令が届かなければ、その軸の速度指令と遅延履歴をゼロにする。")]
    public float commandTimeoutSeconds = 0.2f;

    private ROSConnection ros;
    private EmergencyStop emergencyStop;

    // Other command paths must not restore a position servo on these axes.
    public static bool Owns(ArticulationBody body)
    {
        if (body == null) return false;
        foreach (var controller in body.GetComponentsInParent<Tb20eLeverController>())
        {
            if (controller.isActiveAndEnabled &&
                (controller.boom?.targetArticulationBody == body ||
                 controller.arm?.targetArticulationBody == body ||
                 controller.bucket?.targetArticulationBody == body ||
                 controller.swing?.targetArticulationBody == body)) return true;
        }
        return false;
    }

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(gameObject);

        InvalidateAllCommands();
        SubscribeAxis(boom, nameof(boom));
        SubscribeAxis(arm, nameof(arm));
        SubscribeAxis(bucket, nameof(bucket));
        SubscribeAxis(swing, nameof(swing));
    }

    private void FixedUpdate()
    {
        if (emergencyStop != null && emergencyStop.isEmergencyStop)
        {
            InvalidateAllCommands();
            return;
        }

        double now = Time.timeAsDouble;
        ApplyAxis(boom, now);
        ApplyAxis(arm, now);
        ApplyAxis(bucket, now);
        ApplyAxis(swing, now);
    }

    private void OnDisable()
    {
        InvalidateAllCommands();
    }

    private void SubscribeAxis(LeverAxis axis, string axisName)
    {
        if (axis == null)
        {
            Debug.LogWarning($"[{nameof(Tb20eLeverController)}] {axisName} settings are missing.", this);
            return;
        }

        if (axis.targetArticulationBody == null)
        {
            Debug.LogWarning(
                $"[{nameof(Tb20eLeverController)}] {axisName} ArticulationBody is not assigned.",
                this);
        }

        if (string.IsNullOrWhiteSpace(axis.topicName))
        {
            Debug.LogWarning($"[{nameof(Tb20eLeverController)}] {axisName} topic name is empty.", this);
            return;
        }

        string preprocessedTopicName = Utils.PreprocessNamespace(gameObject, axis.topicName);
        ros.Subscribe<Float64Msg>(preprocessedTopicName, message => OnLeverCommand(axis, message));
    }

    private void OnLeverCommand(LeverAxis axis, Float64Msg message)
    {
        if (!isActiveAndEnabled) return;

        if (message == null || double.IsNaN(message.data) || double.IsInfinity(message.data))
        {
            InvalidateCommand(axis);
            Debug.LogWarning($"[{nameof(Tb20eLeverController)}] Invalid lever command stopped the axis.", this);
            return;
        }

        if (emergencyStop != null && emergencyStop.isEmergencyStop)
        {
            InvalidateCommand(axis);
            return;
        }

        axis.latestLeverInput = Mathf.Clamp((float)message.data, -100.0f, 100.0f);
        axis.lastMessageTime = Time.timeAsDouble;
        axis.hasReceivedMessage = true;
    }

    private void ApplyAxis(LeverAxis axis, double now)
    {
        if (axis == null || axis.targetArticulationBody == null) return;

        if (!axis.hasReceivedMessage || !FiniteNonnegative(commandTimeoutSeconds)
            || now - axis.lastMessageTime > commandTimeoutSeconds
            || !FiniteNonnegative(axis.fullLeverTargetSpeedDegPerSecond)
            || !FiniteNonnegative(axis.negativeSpeedRatio)
            || !FiniteNonnegative(axis.velocityResistance)
            || !FiniteNonnegative(axis.deadbandPercent) || axis.deadbandPercent >= 100
            || !FiniteNonnegative(axis.deadTimeSeconds) || axis.deadTimeSeconds > 5
            || !FiniteNonnegative(axis.responseTimeSeconds))
        {
            InvalidateCommand(axis);
            return;
        }

        float velocity = (float)axis.response.Step(now, Time.fixedDeltaTime,
            axis.latestLeverInput, axis.deadbandPercent, axis.deadTimeSeconds,
            axis.responseTimeSeconds, axis.fullLeverTargetSpeedDegPerSecond,
            axis.negativeSpeedRatio);
        velocity *= axis.positiveDirectionSign < 0 ? -1 : 1;
        ArticulationBody body = axis.targetArticulationBody;
        ArticulationDrive drive = body.xDrive;
        // Limits act on the actual position; there is no accumulated target to unwind.
        if (body.twistLock == ArticulationDofLock.LimitedMotion && body.dofCount == 1)
        {
            float position = body.jointPosition[0] * Mathf.Rad2Deg;
            if ((position >= drive.upperLimit && velocity > 0)
                || (position <= drive.lowerLimit && velocity < 0)) velocity = 0;
        }
        SetVelocity(axis, velocity);
    }

    private static bool FiniteNonnegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
    }

    private static void SetVelocity(LeverAxis axis, float velocity)
    {
        if (axis.targetArticulationBody == null) return;
        var body = axis.targetArticulationBody;
        var drive = body.xDrive;
        drive.driveType = ArticulationDriveType.Force;
        drive.stiffness = 0;
        drive.damping = FiniteNonnegative(axis.velocityResistance) ? axis.velocityResistance : 0;
        drive.targetVelocity = float.IsNaN(velocity) || float.IsInfinity(velocity) ? 0 : velocity;
        // Keep the scene's finite force limit and joint limits. target is unused.
        body.xDrive = drive;
        body.linearDamping = 0;
        body.angularDamping = 0;
        body.jointFriction = 0;
    }

    private void InvalidateAllCommands()
    {
        InvalidateCommand(boom);
        InvalidateCommand(arm);
        InvalidateCommand(bucket);
        InvalidateCommand(swing);
    }

    private static void InvalidateCommand(LeverAxis axis)
    {
        if (axis == null)
            return;

        axis.response.Reset();
        SetVelocity(axis, 0);
        axis.latestLeverInput = 0.0f;
        axis.hasReceivedMessage = false;
    }
}
