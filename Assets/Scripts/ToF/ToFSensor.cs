using UnityEngine;
using UnitySensors.Attribute;
using UnitySensors.Sensor;

namespace UnitySensors.Sensor.ToF
{
    public class ToFSensor : UnitySensor
    {
        [SerializeField]
        private float _minRange = 0.02f;
        [SerializeField]
        private float _maxRange = 5.0f;
        [SerializeField]
        [Range(0, 360)]
        private float _fieldOfView = 10.0f; // Approximate FOV for metadata, actual sensor is a single ray here

        [SerializeField, ReadOnly]
        private float _range;

        private Transform _transform;
        
        public float minRange { get => _minRange; }
        public float maxRange { get => _maxRange; }
        public float fieldOfView { get => _fieldOfView; }
        public float range { get => _range; }

        protected override void Init()
        {
            _transform = this.transform;
            _range = _maxRange;
        }

        protected override void UpdateSensor()
        {
            RaycastHit hit;
            // Raycast forward from the sensor position
            if (Physics.Raycast(_transform.position, _transform.forward, out hit, _maxRange))
            {
                float dist = hit.distance;
                if (dist < _minRange)
                {
                     _range = float.NegativeInfinity; // Indicator of too close, or clamp to minRange
                }
                else
                {
                    _range = dist;
                }
            }
            else
            {
                _range = float.PositiveInfinity; // Indicator of out of range
            }

            if (onSensorUpdated != null)
                onSensorUpdated.Invoke();
        }

        protected override void OnSensorDestroy()
        {
        }
    }
}
