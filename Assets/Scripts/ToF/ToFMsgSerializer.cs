using UnityEngine;
using UnitySensors.Sensor.ToF;
using UnitySensors.ROS.Serializer;
using RosMessageTypes.Sensor;

namespace UnitySensors.ROS.Serializer.ToF
{
    [System.Serializable]
    public class ToFMsgSerializer : RosMsgSerializer<ToFSensor, RangeMsg>
    {
        [SerializeField]
        private HeaderSerializer _header;

        // 0: ULTRASOUND, 1: INFRARED
        [SerializeField]
        private byte _radiationType = 1;

        public override void Init(ToFSensor sensor)
        {
            base.Init(sensor);
            _header.Init(sensor);
        }

        public override RangeMsg Serialize()
        {
            _msg.header = _header.Serialize();
            _msg.radiation_type = _radiationType;
            _msg.field_of_view = sensor.fieldOfView * Mathf.Deg2Rad;
            _msg.min_range = sensor.minRange;
            _msg.max_range = sensor.maxRange;
            _msg.range = sensor.range;
            
            return _msg;
        }
    }
}
