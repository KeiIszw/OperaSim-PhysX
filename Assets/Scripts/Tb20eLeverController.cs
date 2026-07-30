using System;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>
/// TB20e のレバー操作量を受信し、各作業機の ArticulationDrive.target を連続的に更新する。
/// 既存の JointPosController と同じ関節を操作するため、レバー指令と角度指令は同時に送信しないこと。
/// </summary>
public class Tb20eLeverController : MonoBehaviour
{
    [Serializable]
    public sealed class LeverAxis
    {
        [Tooltip("購読する std_msgs/Float64 topic 名。[robot_name] は root GameObject 名に置換される。")]
        public string topicName;

        [Tooltip("レバー入力で xDrive.target を更新する ArticulationBody。")]
        public ArticulationBody targetArticulationBody;

        [Tooltip("正のレバー入力で target を増やす場合は 1、減らす場合は -1。実行時は符号だけを使用する。")]
        public float positiveDirectionSign = 1.0f;

        [Min(0.0f)]
        [Tooltip("レバー入力が ±100 のときの target 角速度 [deg/s]。")]
        public float fullLeverTargetSpeedDegPerSecond = 50.0f;

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
    [Tooltip("この秒数を超えて指令が届かなければ、その軸の target 更新を停止する。")]
    public float commandTimeoutSeconds = 0.2f;

    private ROSConnection ros;
    private EmergencyStop emergencyStop;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(gameObject);

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
        if (axis == null || axis.targetArticulationBody == null || !axis.hasReceivedMessage)
            return;

        if (float.IsNaN(commandTimeoutSeconds)
            || float.IsInfinity(commandTimeoutSeconds)
            || commandTimeoutSeconds < 0.0f
            || now - axis.lastMessageTime > commandTimeoutSeconds)
        {
            InvalidateCommand(axis);
            return;
        }

        if (Mathf.Approximately(axis.latestLeverInput, 0.0f))
            return;

        float speed = axis.fullLeverTargetSpeedDegPerSecond;
        if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0.0f)
            return;

        ArticulationBody target = axis.targetArticulationBody;
        ArticulationDrive drive = target.xDrive;
        float direction = axis.positiveDirectionSign < 0.0f ? -1.0f : 1.0f;
        float targetDelta =
            axis.latestLeverInput / 100.0f * direction * speed * Time.fixedDeltaTime;
        float nextTarget = drive.target + targetDelta;

        if (target.twistLock == ArticulationDofLock.LimitedMotion)
            nextTarget = Mathf.Clamp(nextTarget, drive.lowerLimit, drive.upperLimit);

        drive.target = nextTarget;
        target.xDrive = drive;
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

        axis.latestLeverInput = 0.0f;
        axis.hasReceivedMessage = false;
    }
}
