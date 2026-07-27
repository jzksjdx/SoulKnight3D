using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class HelixBullet : MonoBehaviour
    {
        private Bullet _bullet;
        private Vector3 _center;
        private Vector3 _forward;
        private Vector3 _right;
        private Vector3 _up;
        private float _speed;
        private float _radius;
        private float _radiansPerSecond;
        private float _phase;
        private bool _isArmed;

        private void Awake()
        {
            _bullet = GetComponent<Bullet>();
        }

        private void OnDisable()
        {
            _isArmed = false;
        }

        private void FixedUpdate()
        {
            if (!_isArmed || _bullet == null || _bullet._didHit ||
                _bullet.SelfRigidbody == null)
            {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            _center += _forward * (_speed * deltaTime);
            _phase += _radiansPerSecond * deltaTime;

            Vector3 offset =
                (_right * Mathf.Cos(_phase) + _up * Mathf.Sin(_phase)) *
                _radius;
            Vector3 nextPosition = _center + offset;
            Vector3 velocity =
                (nextPosition - _bullet.SelfRigidbody.position) / deltaTime;
            _bullet.SelfRigidbody.velocity = velocity;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }
        }

        public void Arm(Vector3 center, Vector3 forward, float speed,
            float radius, float rotationsPerSecond, float phaseDegrees)
        {
            _center = center;
            _forward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : transform.forward;
            _speed = Mathf.Max(0.01f, speed);
            _radius = Mathf.Max(0f, radius);
            _radiansPerSecond = rotationsPerSecond * Mathf.PI * 2f;
            _phase = phaseDegrees * Mathf.Deg2Rad;

            _right = Vector3.Cross(Vector3.up, _forward);
            if (_right.sqrMagnitude <= 0.0001f)
            {
                _right = Vector3.Cross(Vector3.forward, _forward);
            }
            _right.Normalize();
            _up = Vector3.Cross(_forward, _right).normalized;

            Vector3 offset =
                (_right * Mathf.Cos(_phase) + _up * Mathf.Sin(_phase)) *
                _radius;
            transform.position = _center + offset;

            Vector3 tangent =
                (-_right * Mathf.Sin(_phase) + _up * Mathf.Cos(_phase)) *
                (_radius * _radiansPerSecond);
            _bullet.SelfRigidbody.velocity = _forward * _speed + tangent;
            _isArmed = true;

            if (_bullet.TrailRenderer != null)
            {
                _bullet.TrailRenderer.Clear();
            }
        }
    }
}
