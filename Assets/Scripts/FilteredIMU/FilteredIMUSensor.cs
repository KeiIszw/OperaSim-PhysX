using UnityEngine;
using UnitySensors.Attribute;
using UnitySensors.Sensor;

namespace UnitySensors.Sensor.IMU
{
    public class FilteredIMUSensor : UnitySensor
    {
        [Header("Filter Settings")]
        [Range(0f, 1f)]
        public float filterFactor = 0.1f;

        private Transform _transform;

        [SerializeField, ReadOnly]
        private Vector3 _position;
        [SerializeField, ReadOnly]
        private Vector3 _velocity;
        [SerializeField, ReadOnly]
        private Vector3 _acceleration;
        [SerializeField, ReadOnly]
        private Quaternion _rotation;
        [SerializeField, ReadOnly]
        private Vector3 _angularVelocity;

        private Vector3 _position_tmp;
        private Vector3 _velocity_tmp;
        private Vector3 _acceleration_tmp;
        private Quaternion _rotation_tmp;
        private Vector3 _angularVelocity_tmp;

        // Filtered temporary values
        private Vector3 _acceleration_filtered_tmp;
        private Vector3 _angularVelocity_filtered_tmp;

        private Vector3 _position_last;
        private Vector3 _velocity_last;
        private Quaternion _rotation_last;

        public Vector3 position { get => _position; }
        public Vector3 velocity { get => _velocity; }
        public Vector3 acceleration { get => _acceleration; }
        public Quaternion rotation { get => _rotation; }
        public Vector3 angularVelocity { get => _angularVelocity; }

        public Vector3 localVelocity { get => _transform.InverseTransformDirection(_velocity); }
        public Vector3 localAcceleration { get => _transform.InverseTransformDirection(_acceleration.normalized) * _acceleration.magnitude; }

        private Vector3 _gravityDirection;
        private float _gravityMagnitude;

        protected override void Init()
        {
            _transform = this.transform;
            _gravityDirection = Physics.gravity.normalized;
            _gravityMagnitude = Physics.gravity.magnitude;

            // Initialize last values to avoid spikes on start
            _position_last = _transform.position;
            _velocity_last = Vector3.zero;
            _rotation_last = _transform.rotation;
        }

        protected override void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _position_tmp = _transform.position;
            _velocity_tmp = (_position_tmp - _position_last) / dt;
            _acceleration_tmp = (_velocity_tmp - _velocity_last) / dt;
            _acceleration_tmp -= _transform.InverseTransformDirection(_gravityDirection) * _gravityMagnitude;

            _rotation_tmp = _transform.rotation;
            Quaternion rotation_delta = Quaternion.Inverse(_rotation_last) * _rotation_tmp;
            rotation_delta.ToAngleAxis(out float angle, out Vector3 axis);
            // Ensure angle is correct range
            if (angle > 180f) angle -= 360f;
            
            float angularSpeed = (angle * Mathf.Deg2Rad) / dt;
            _angularVelocity_tmp = axis * angularSpeed;
            // Handle NaN/Inf case if axis is zero or dt is zero (though checked above)
            if (float.IsNaN(_angularVelocity_tmp.x)) _angularVelocity_tmp = Vector3.zero;

            // Apply Low Pass Filter
            // If filterFactor is 1, we use new value entirely (no filter). If 0, we never update (bad).
            // Usually factor is 'alpha' in: out = out * (1-alpha) + in * alpha
            // Here we use Lerp: Lerp(a, b, t) = a + (b-a)*t. So Lerp(current, new, factor).
            // If factor is small (0.1), we take 10% of new value, causing heavy smoothing.
            
            // Initialization check for first frame
            if (_acceleration_filtered_tmp == Vector3.zero && _acceleration_tmp != Vector3.zero)
                 _acceleration_filtered_tmp = _acceleration_tmp;
            
            _acceleration_filtered_tmp = Vector3.Lerp(_acceleration_filtered_tmp, _acceleration_tmp, filterFactor);
            _angularVelocity_filtered_tmp = Vector3.Lerp(_angularVelocity_filtered_tmp, _angularVelocity_tmp, filterFactor);

            _position_last = _position_tmp;
            _velocity_last = _velocity_tmp;
            _rotation_last = _rotation_tmp;

            base.Update();
        }

        protected override void UpdateSensor()
        {
            _position = _position_tmp;
            _velocity = _velocity_tmp;
            
            // Use filtered values
            _acceleration = _acceleration_filtered_tmp;
            
            _rotation = _rotation_tmp;
            _angularVelocity = _angularVelocity_filtered_tmp;

            if (onSensorUpdated != null)
                onSensorUpdated.Invoke();
        }

        protected override void OnSensorDestroy()
        {
        }
    }
}
