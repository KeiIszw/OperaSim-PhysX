using UnityEngine;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

/// <summary>
/// bucket_imu をロッカープレート等に取り付けたまま、bucket_link の角度だけを publish する。
/// 実際のセンサ取り付け先ではなく、Inspector で指定した bucketLink Transform を角度計算の対象にする。
/// </summary>
public class BucketLinkAnglePublisher : MonoBehaviour
{
    /// <summary>
    /// ROS FLU 座標系に変換した後、どの軸回りの Euler 角を Float64 として出すか。
    /// </summary>
    public enum EulerAngleAxis
    {
        Roll,
        Pitch,
        Yaw
    }

    [Header("ROS")]
    [Tooltip("std_msgs/Float64 topic name. [robot_name] is replaced with the root GameObject name.")]
    public string topicName = "/current_bucket_angle";

    [Min(0.001f)]
    [Tooltip("Publish frequency in Hz.")]
    public float publishFrequency = 50.0f;

    [Tooltip("False: publish radians, which is common in ROS. True: publish degrees for debugging or existing controllers.")]
    public bool publishInDegrees = true;

    [Header("Bucket Link Angle")]
    [Tooltip("Transform of bucket_link. This is the link whose angle should be published.")]
    public Transform bucketLink;

    [Tooltip("Reference frame for the bucket angle, normally arm_link. If empty, bucketLink world orientation is used.")]
    public Transform referenceParent;

    [Tooltip("Euler axis to publish after Unity-to-ROS FLU conversion.")]
    public EulerAngleAxis angleAxis = EulerAngleAxis.Roll;

    [Tooltip("Invert the selected angle sign after axis selection.")]
    public bool invertAngleSign = false;

    [Tooltip("Constant offset added after sign inversion. Unit follows publishInDegrees.")]
    public double angleOffset = 0.0;

    [Header("Alignment Offset")]
    // bucket_link モデルのローカル軸と、制御側が期待するバケット角のゼロ姿勢がずれている場合に使う。
    // まずは両方 0 のまま動かし、静止姿勢で出る角度との差を angleOffset に入れる運用が簡単。
    // 軸そのものが違う場合だけ bucketLinkRotationOffsetEuler / referenceRotationOffsetEuler で姿勢補正する。
    [Tooltip("Rotation offset applied to bucketLink before calculating the relative angle, in degrees.")]
    public Vector3 bucketLinkRotationOffsetEuler;

    [Tooltip("Rotation offset applied to referenceParent before calculating the relative angle, in degrees.")]
    public Vector3 referenceRotationOffsetEuler;

    [Header("Latest Values")]
    [SerializeField]
    private Quaternion latestUnityRelativeRotation = Quaternion.identity;
    [SerializeField]
    private Vector3 latestRosRollPitchYaw;
    [SerializeField]
    private double latestPublishedAngle;

    public Quaternion unityRelativeRotation => latestUnityRelativeRotation;
    public Vector3 rosRollPitchYaw => latestRosRollPitchYaw;
    public double publishedAngle => latestPublishedAngle;

    private ROSConnection ros;
    private Float64Msg message;
    private string preprocessedTopicName;
    private double lastPublishTime;

    private float PublishInterval => 1.0f / Mathf.Max(publishFrequency, 0.001f);

    private void Start()
    {
        preprocessedTopicName = Utils.PreprocessNamespace(gameObject, topicName);

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<Float64Msg>(preprocessedTopicName);
        message = new Float64Msg();

        lastPublishTime = Clock.time;
    }

    private void FixedUpdate()
    {
        double now = Clock.time;
        if (now - lastPublishTime < PublishInterval)
            return;

        // bucketLink が未設定の場合は、このコンポーネントを付けた Transform を仮に使う。
        // bucket_imu に付けた場合はロッカープレート角になるため、通常は必ず bucket_link を設定する。
        Transform target = bucketLink != null ? bucketLink : transform;

        latestUnityRelativeRotation = GetRelativeRotation(target);

        // ConfigurableIMUPublisher と同じく、Unity の相対姿勢を ROS FLU に変換してから RPY を計算する。
        QuaternionMsg rosQuaternion = latestUnityRelativeRotation.To<FLU>();
        latestRosRollPitchYaw = QuaternionMsgToRollPitchYaw(rosQuaternion);

        double selectedAngle = GetSelectedEulerAngle(latestRosRollPitchYaw);
        if (invertAngleSign)
            selectedAngle = -selectedAngle;
        if (publishInDegrees)
            selectedAngle *= Mathf.Rad2Deg;

        latestPublishedAngle = selectedAngle + angleOffset;
        message.data = latestPublishedAngle;

        ros.Publish(preprocessedTopicName, message);
        lastPublishTime = now;
    }

    /// <summary>
    /// referenceParent から見た bucketLink の相対姿勢を求める。
    /// referenceParent に arm_link を入れると、上部旋回体やブーム・アームの姿勢ではなく
    /// arm_link に対する bucket_link の角度として扱える。
    /// </summary>
    private Quaternion GetRelativeRotation(Transform target)
    {
        Quaternion targetRotation = target.rotation * Quaternion.Euler(bucketLinkRotationOffsetEuler);

        if (referenceParent == null)
            return Normalize(targetRotation);

        Quaternion referenceRotation = referenceParent.rotation * Quaternion.Euler(referenceRotationOffsetEuler);
        return Normalize(Quaternion.Inverse(referenceRotation) * targetRotation);
    }

    /// <summary>
    /// ROS の QuaternionMsg [x, y, z, w] を roll-pitch-yaw [rad] に変換する。
    /// ここでの軸は Unity 座標系ではなく、To&lt;FLU&gt;() で変換した ROS 座標系に対応する。
    /// </summary>
    private static Vector3 QuaternionMsgToRollPitchYaw(QuaternionMsg q)
    {
        NormalizeQuaternionMsg(ref q);

        double sinrCosp = 2.0 * (q.w * q.x + q.y * q.z);
        double cosrCosp = 1.0 - 2.0 * (q.x * q.x + q.y * q.y);
        double roll = System.Math.Atan2(sinrCosp, cosrCosp);

        double sinp = 2.0 * (q.w * q.y - q.z * q.x);
        double pitch = System.Math.Abs(sinp) >= 1.0
            ? (sinp >= 0.0 ? System.Math.PI / 2.0 : -System.Math.PI / 2.0)
            : System.Math.Asin(sinp);

        double sinyCosp = 2.0 * (q.w * q.z + q.x * q.y);
        double cosyCosp = 1.0 - 2.0 * (q.y * q.y + q.z * q.z);
        double yaw = System.Math.Atan2(sinyCosp, cosyCosp);

        return new Vector3((float)roll, (float)pitch, (float)yaw);
    }

    /// <summary>
    /// Inspector で選んだ 1 軸だけを取り出す。
    /// </summary>
    private double GetSelectedEulerAngle(Vector3 rollPitchYaw)
    {
        switch (angleAxis)
        {
            case EulerAngleAxis.Roll:
                return rollPitchYaw.x;
            case EulerAngleAxis.Pitch:
                return rollPitchYaw.y;
            default:
                return rollPitchYaw.z;
        }
    }

    /// <summary>
    /// Unity Quaternion の数値誤差を抑える。ゼロに近い場合は identity に戻す。
    /// </summary>
    private static Quaternion Normalize(Quaternion q)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (magnitude < 1e-12f)
            return Quaternion.identity;

        float inv = 1.0f / magnitude;
        return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
    }

    /// <summary>
    /// ROS QuaternionMsg を単位クォータニオンへ正規化する。
    /// ゼロに近い場合は identity として扱い、Euler 角計算の NaN を防ぐ。
    /// </summary>
    private static void NormalizeQuaternionMsg(ref QuaternionMsg q)
    {
        double magnitude = System.Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (magnitude < 1e-12)
        {
            q.x = 0.0;
            q.y = 0.0;
            q.z = 0.0;
            q.w = 1.0;
            return;
        }

        double inv = 1.0 / magnitude;
        q.x *= inv;
        q.y *= inv;
        q.z *= inv;
        q.w *= inv;
    }
}
