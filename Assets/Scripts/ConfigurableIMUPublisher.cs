using UnityEngine;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

/// <summary>
/// Transform の位置・姿勢差分から仮想 IMU の sensor_msgs/Imu を生成して publish する。
/// IMU の取り付け対象、取り付けオフセット、出力座標系、重力補正を Inspector で切り替えられる。
/// </summary>
public class ConfigurableIMUPublisher : MonoBehaviour
{
    /// <summary>
    /// 加速度と角速度をどの座標系で出力するか。
    /// 実機 IMU と同じ扱いにしたい場合は ImuLocal を使う。
    /// </summary>
    public enum VectorFrame
    {
        ImuLocal,
        Parent,
        World
    }

    /// <summary>
    /// orientation を world 基準で出すか、指定した親フレーム基準で出すか。
    /// </summary>
    public enum OrientationFrame
    {
        World,
        Parent
    }

    [Header("ROS")]
    [Tooltip("ROS topic name. [robot_name] is replaced with the root GameObject name.")]
    public string topicName = "[robot_name]/imu";

    [Tooltip("Header frame_id to publish in the Imu message.")]
    public string frameID = "[robot_name]/imu_link";

    [Min(0.001f)]
    [Tooltip("Sensor publish frequency in Hz.")]
    public float sensorFrequency = 50.0f;

    [Header("Mount")]
    [Tooltip("Object that the IMU is mounted on. If empty, this GameObject transform is used.")]
    public Transform mountedObject;

    [Tooltip("Optional parent/reference frame. Used by Parent output modes.")]
    public Transform referenceParent;

    [Tooltip("IMU position offset in mountedObject local coordinates.")]
    public Vector3 mountPositionOffset;

    [Tooltip("IMU rotation offset in mountedObject local coordinates, in degrees.")]
    public Vector3 mountRotationOffsetEuler;

    [Header("Output Frame")]
    [Tooltip("Coordinate frame used for linear acceleration and angular velocity.")]
    public VectorFrame vectorOutputFrame = VectorFrame.ImuLocal;

    [Tooltip("Reference frame used for orientation.")]
    public OrientationFrame orientationOutputFrame = OrientationFrame.World;

    [Header("Acceleration")]
    [Tooltip("True: stationary IMU publishes 0 acceleration. False: stationary IMU publishes apparent gravity (-Physics.gravity).")]
    public bool removeGravityFromAcceleration = true;

    [Header("Covariance")]
    // sensor_msgs/Imu は orientation / angular_velocity / linear_acceleration ごとに
    // 3x3 の covariance 配列を持つ。ここでは Inspector で指定した 1 つの値を
    // x, y, z の対角成分へ同じ分散値として入れる。
    // 値を大きくすると、robot_localization などの下流フィルタはその計測を低信頼として扱う。
    // ROS の慣例では全要素 0 は「covariance 未知」と解釈されるため、実際に EKF で
    // 信頼度を調整したい場合は 0 ではなく用途に応じた非ゼロ値を設定する。
    [Tooltip("Variance used on the diagonal of orientation_covariance. Larger means lower trust in orientation.")]
    public double orientationCovariance = 0.0;
    [Tooltip("Variance used on the diagonal of angular_velocity_covariance. Larger means lower trust in gyro data.")]
    public double angularVelocityCovariance = 0.0;
    [Tooltip("Variance used on the diagonal of linear_acceleration_covariance. Larger means lower trust in acceleration data.")]
    public double linearAccelerationCovariance = 0.0;

    [Header("Latest Values")]
    [SerializeField]
    private Vector3 latestAcceleration;
    [SerializeField]
    private Quaternion latestRotation = Quaternion.identity;
    [SerializeField]
    private Vector3 latestAngularVelocity;

    public Vector3 acceleration => latestAcceleration;
    public Quaternion rotation => latestRotation;
    public Vector3 angularVelocity => latestAngularVelocity;

    private ROSConnection ros;
    private ImuMsg message;
    private string preprocessedTopicName;
    private string preprocessedFrameID;

    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private Quaternion lastRotation;
    private double lastSampleTime;
    private bool hasLastSample;

    private float PublishInterval => 1.0f / Mathf.Max(sensorFrequency, 0.001f);

    private void Start()
    {
        // mountedObject が未設定なら、このコンポーネントを付けた GameObject 自体を IMU として扱う。
        if (mountedObject == null)
            mountedObject = transform;

        // [robot_name] は既存スクリプトと同じく root GameObject 名へ置換する。
        preprocessedTopicName = Utils.PreprocessNamespace(gameObject, topicName);
        preprocessedFrameID = Utils.PreprocessNamespace(gameObject, frameID);

        // covariance は対角成分だけ Inspector から指定できるようにしている。
        // sensor_msgs/Imu の covariance 配列は row-major の 3x3 行列で、
        // [0], [4], [8] が x, y, z 軸の分散を表す。
        message = new ImuMsg
        {
            header = new HeaderMsg
            {
                stamp = new TimeMsg(),
                frame_id = preprocessedFrameID
            },
            orientation_covariance = CreateDiagonalCovariance(orientationCovariance),
            angular_velocity_covariance = CreateDiagonalCovariance(angularVelocityCovariance),
            linear_acceleration_covariance = CreateDiagonalCovariance(linearAccelerationCovariance)
        };

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImuMsg>(preprocessedTopicName);

        // 初回サンプルでは差分計算ができないため、現在姿勢を基準値として保存しておく。
        GetSensorPose(out lastPosition, out lastRotation);
        lastVelocity = Vector3.zero;
        lastSampleTime = Clock.time;
        hasLastSample = false;
    }

    private void FixedUpdate()
    {
        double now = Clock.time;
        double dt = now - lastSampleTime;

        // Unity の物理更新ごとではなく、Inspector の sensorFrequency に従って publish する。
        if (dt < PublishInterval)
            return;

        GetSensorPose(out Vector3 position, out Quaternion rotation);

        // 位置差分から速度、速度差分から world 座標系の並進加速度を推定する。
        Vector3 velocity = (position - lastPosition) / (float)dt;
        Vector3 worldAcceleration = hasLastSample
            ? (velocity - lastVelocity) / (float)dt
            : Vector3.zero;

        // removeGravityFromAcceleration=true では、静止時の加速度が 0 になる。
        // false では実機加速度計のように、静止時に見かけの重力 (-g) を含める。
        Vector3 accelerometerAcceleration = removeGravityFromAcceleration
            ? worldAcceleration
            : worldAcceleration - Physics.gravity;

        // 姿勢差分から world 座標系の角速度を推定する。初回は差分がないため 0 にする。
        Vector3 worldAngularVelocity = hasLastSample
            ? CalculateWorldAngularVelocity(lastRotation, rotation, (float)dt)
            : Vector3.zero;

        // orientation と vector 系データは別々に座標系を選べる。
        Quaternion outputOrientation = GetOutputOrientation(rotation);
        Vector3 outputAcceleration = ExpressVector(accelerometerAcceleration, rotation);
        Vector3 outputAngularVelocity = ExpressVector(worldAngularVelocity, rotation);

        // Inspector で実行中の値を確認できるように保持する。
        latestRotation = outputOrientation;
        latestAcceleration = outputAcceleration;
        latestAngularVelocity = outputAngularVelocity;

        // Unity 座標系から ROS の FLU 座標系へ変換して publish する。
        message.header.frame_id = preprocessedFrameID;
        message.header.stamp = new TimeStamp(now);
        message.orientation = latestRotation.To<FLU>();
        message.linear_acceleration = latestAcceleration.To<FLU>();
        message.angular_velocity = latestAngularVelocity.To<FLU>();

        // covariance は publish ごとに反映する。Play中に Inspector で値を変えると、
        // 次のメッセージから下流の EKF / sensor fusion 側の重み付けを変えられる。
        message.orientation_covariance = CreateDiagonalCovariance(orientationCovariance);
        message.angular_velocity_covariance = CreateDiagonalCovariance(angularVelocityCovariance);
        message.linear_acceleration_covariance = CreateDiagonalCovariance(linearAccelerationCovariance);

        ros.Publish(preprocessedTopicName, message);

        lastPosition = position;
        lastVelocity = velocity;
        lastRotation = rotation;
        lastSampleTime = now;
        hasLastSample = true;
    }

    /// <summary>
    /// mountedObject に対して指定した取り付けオフセットを適用し、IMU の world pose を求める。
    /// </summary>
    private void GetSensorPose(out Vector3 position, out Quaternion rotation)
    {
        Transform source = mountedObject != null ? mountedObject : transform;
        Quaternion mountRotation = Quaternion.Euler(mountRotationOffsetEuler);

        position = source.TransformPoint(mountPositionOffset);
        rotation = source.rotation * mountRotation;
        rotation = Normalize(rotation);
    }

    /// <summary>
    /// orientation を world 基準または referenceParent 基準へ変換する。
    /// </summary>
    private Quaternion GetOutputOrientation(Quaternion sensorRotation)
    {
        if (orientationOutputFrame == OrientationFrame.Parent && referenceParent != null)
            return Normalize(Quaternion.Inverse(referenceParent.rotation) * sensorRotation);

        return sensorRotation;
    }

    /// <summary>
    /// world 座標系の vector を、Inspector で選んだ出力座標系へ変換する。
    /// </summary>
    private Vector3 ExpressVector(Vector3 worldVector, Quaternion sensorRotation)
    {
        switch (vectorOutputFrame)
        {
            case VectorFrame.ImuLocal:
                return Quaternion.Inverse(sensorRotation) * worldVector;
            case VectorFrame.Parent:
                return referenceParent != null
                    ? referenceParent.InverseTransformDirection(worldVector)
                    : worldVector;
            default:
                return worldVector;
        }
    }

    /// <summary>
    /// 前回姿勢から今回姿勢への差分回転を使い、world 座標系の角速度 [rad/s] を計算する。
    /// </summary>
    private static Vector3 CalculateWorldAngularVelocity(Quaternion previous, Quaternion current, float dt)
    {
        Quaternion delta = current * Quaternion.Inverse(previous);
        delta = Normalize(delta);

        delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);

        // ToAngleAxis は 0..360 度で返すため、短い回転方向になるよう -180..180 度へ丸める。
        if (angleDegrees > 180.0f)
            angleDegrees -= 360.0f;

        if (axis.sqrMagnitude < 1e-12f)
            return Vector3.zero;

        return axis.normalized * (angleDegrees * Mathf.Deg2Rad / dt);
    }

    /// <summary>
    /// Quaternion の数値誤差を抑える。ゼロに近い場合は identity に戻す。
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
    /// sensor_msgs/Imu の 3x3 covariance 配列を対角値から作る。
    /// 配列は row-major で、以下の行列を意味する。
    /// [ value, 0,     0
    ///   0,     value, 0
    ///   0,     0,     value ]
    /// 現時点では軸ごとの個別分散や軸間相関は扱わず、全軸同じ独立ノイズとして扱う。
    /// </summary>
    private static double[] CreateDiagonalCovariance(double value)
    {
        return new[]
        {
            value, 0.0, 0.0,
            0.0, value, 0.0,
            0.0, 0.0, value
        };
    }
}
