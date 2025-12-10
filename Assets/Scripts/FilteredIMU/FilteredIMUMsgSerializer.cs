using UnityEngine;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Sensor;
using UnitySensors.Sensor.IMU;
using UnitySensors.ROS.Serializer; // For HeaderSerializer

namespace UnitySensors.ROS.Serializer.IMU
{
    [System.Serializable]
    public class FilteredIMUMsgSerializer : RosMsgSerializer<FilteredIMUSensor, ImuMsg>
    {
        [SerializeField]
        private HeaderSerializer _header;

        public override void Init(FilteredIMUSensor sensor)
        {
            base.Init(sensor);
            _header.Init(sensor);
        }

        public override ImuMsg Serialize()
        {
            _msg.header = _header.Serialize();
            _msg.linear_acceleration = sensor.acceleration.To<FLU>();
            _msg.orientation = sensor.rotation.To<FLU>();
            _msg.angular_velocity = sensor.angularVelocity.To<FLU>();
            return _msg;
        }
    }
}
