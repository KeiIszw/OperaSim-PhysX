using UnityEngine;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using UnitySensors.Interface.Sensor;

public class ConfigurableIMUPublisher : MonoBehaviour, IImuDataInterface
{
    public enum VectorFrame
    {
        ImuLocal,
        Parent,
        World
    }

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
    public double orientationCovariance = 0.0;
    public double angularVelocityCovariance = 0.0;
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
        if (mountedObject == null)
            mountedObject = transform;

        preprocessedTopicName = Utils.PreprocessNamespace(gameObject, topicName);
        preprocessedFrameID = Utils.PreprocessNamespace(gameObject, frameID);

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

        GetSensorPose(out lastPosition, out lastRotation);
        lastVelocity = Vector3.zero;
        lastSampleTime = Clock.time;
        hasLastSample = false;
    }

    private void FixedUpdate()
    {
        double now = Clock.time;
        double dt = now - lastSampleTime;
        if (dt < PublishInterval)
            return;

        GetSensorPose(out Vector3 position, out Quaternion rotation);

        Vector3 velocity = (position - lastPosition) / (float)dt;
        Vector3 worldAcceleration = hasLastSample
            ? (velocity - lastVelocity) / (float)dt
            : Vector3.zero;

        Vector3 accelerometerAcceleration = removeGravityFromAcceleration
            ? worldAcceleration
            : worldAcceleration - Physics.gravity;

        Vector3 worldAngularVelocity = hasLastSample
            ? CalculateWorldAngularVelocity(lastRotation, rotation, (float)dt)
            : Vector3.zero;

        Quaternion outputOrientation = GetOutputOrientation(rotation);
        Vector3 outputAcceleration = ExpressVector(accelerometerAcceleration, rotation);
        Vector3 outputAngularVelocity = ExpressVector(worldAngularVelocity, rotation);

        latestRotation = outputOrientation;
        latestAcceleration = outputAcceleration;
        latestAngularVelocity = outputAngularVelocity;

        message.header.frame_id = preprocessedFrameID;
        message.header.stamp = new TimeStamp(now);
        message.orientation = latestRotation.To<FLU>();
        message.linear_acceleration = latestAcceleration.To<FLU>();
        message.angular_velocity = latestAngularVelocity.To<FLU>();
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

    private void GetSensorPose(out Vector3 position, out Quaternion rotation)
    {
        Transform source = mountedObject != null ? mountedObject : transform;
        Quaternion mountRotation = Quaternion.Euler(mountRotationOffsetEuler);

        position = source.TransformPoint(mountPositionOffset);
        rotation = source.rotation * mountRotation;
        rotation = Normalize(rotation);
    }

    private Quaternion GetOutputOrientation(Quaternion sensorRotation)
    {
        if (orientationOutputFrame == OrientationFrame.Parent && referenceParent != null)
            return Normalize(Quaternion.Inverse(referenceParent.rotation) * sensorRotation);

        return sensorRotation;
    }

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

    private static Vector3 CalculateWorldAngularVelocity(Quaternion previous, Quaternion current, float dt)
    {
        Quaternion delta = current * Quaternion.Inverse(previous);
        delta = Normalize(delta);

        delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);
        if (angleDegrees > 180.0f)
            angleDegrees -= 360.0f;

        if (axis.sqrMagnitude < 1e-12f)
            return Vector3.zero;

        return axis.normalized * (angleDegrees * Mathf.Deg2Rad / dt);
    }

    private static Quaternion Normalize(Quaternion q)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (magnitude < 1e-12f)
            return Quaternion.identity;

        float inv = 1.0f / magnitude;
        return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
    }

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
