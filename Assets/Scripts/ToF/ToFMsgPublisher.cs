using UnityEngine;
using RosMessageTypes.Sensor;
using UnitySensors.Sensor.ToF;
using UnitySensors.ROS.Serializer.ToF;
using UnitySensors.ROS.Publisher; // Base class namespace

namespace UnitySensors.ROS.Publisher.ToF
{
    [RequireComponent(typeof(ToFSensor))]
    public class ToFMsgPublisher : RosMsgPublisher<ToFSensor, ToFMsgSerializer, RangeMsg>
    {
    }
}
