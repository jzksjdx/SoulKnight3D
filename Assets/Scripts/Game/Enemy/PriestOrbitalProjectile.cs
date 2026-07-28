using UnityEngine;

namespace SoulKnight3D
{
    public sealed class PriestOrbitalProjectile : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 85f;

        private BossEnemy _owner;
        private PooledGameObject _pooledObject;
        private float _angle;
        private float _radius;
        private float _remainingLifetime;
        private int _damage;
        private bool _consumed;
        private bool _initialized;

        public bool IsActive =>
            _initialized && !_consumed && gameObject.activeInHierarchy;

        public void Initialize(BossEnemy owner, float startingAngle, float radius,
            float rotationSpeed, float lifetime, int damage)
        {
            if (_pooledObject == null)
            {
                _pooledObject = GetComponent<PooledGameObject>();
            }
            _owner = owner;
            _angle = startingAngle;
            _radius = Mathf.Max(0f, radius);
            _rotationSpeed = rotationSpeed;
            _remainingLifetime = Mathf.Max(0f, lifetime);
            _damage = Mathf.Max(0, damage);
            _consumed = false;
            _initialized = true;
            UpdatePosition();
        }

        private void Update()
        {
            if (!_initialized) { return; }

            if (_owner == null || _owner.IsDead)
            {
                Release();
                return;
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Release();
                return;
            }

            _angle += _rotationSpeed * Time.deltaTime;
            UpdatePosition();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed) { return; }

            MountBase mount = other.GetComponentInParent<MountBase>();
            if (mount != null && mount.IsMounted)
            {
                _consumed = true;
                mount.ApplyDamage(_damage);
                Release();
                return;
            }

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) { return; }

            _consumed = true;
            player.PlayerStats.ApplyDamage(_damage);
            Release();
        }

        private void UpdatePosition()
        {
            float radians = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(radians), 0.65f, Mathf.Cos(radians)) * _radius;
            offset.y = 0.65f;
            transform.position = _owner.transform.position + offset;
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnDisable()
        {
            _initialized = false;
            _owner = null;
        }

        private void Release()
        {
            _initialized = false;
            if (_pooledObject == null)
            {
                _pooledObject = GetComponent<PooledGameObject>();
            }

            if (_pooledObject != null)
            {
                _pooledObject.ReleaseToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
