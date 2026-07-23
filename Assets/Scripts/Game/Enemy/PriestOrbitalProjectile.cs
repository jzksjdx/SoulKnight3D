using UnityEngine;

namespace SoulKnight3D
{
    public sealed class PriestOrbitalProjectile : MonoBehaviour
    {
        private BossEnemy _owner;
        private float _angle;
        private float _radius;
        private float _degreesPerSecond;
        private float _remainingLifetime;
        private int _damage;
        private bool _consumed;

        public void Initialize(BossEnemy owner, float startingAngle, float radius,
            float degreesPerSecond, float lifetime, int damage)
        {
            _owner = owner;
            _angle = startingAngle;
            _radius = Mathf.Max(0f, radius);
            _degreesPerSecond = degreesPerSecond;
            _remainingLifetime = Mathf.Max(0f, lifetime);
            _damage = Mathf.Max(0, damage);
            _consumed = false;
            UpdatePosition();
        }

        private void Update()
        {
            if (_owner == null || _owner.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _angle += _degreesPerSecond * Time.deltaTime;
            UpdatePosition();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed) { return; }

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) { return; }

            _consumed = true;
            player.PlayerStats.ApplyDamage(_damage);
            Destroy(gameObject);
        }

        private void UpdatePosition()
        {
            float radians = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(radians), 0.65f, Mathf.Cos(radians)) * _radius;
            offset.y = 0.65f;
            transform.position = _owner.transform.position + offset;
            transform.Rotate(Vector3.up, _degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
