using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class PriestSplitterParentBullet : MonoBehaviour
    {
        [SerializeField] private GameObject _swirlClonePrefab;
        [SerializeField, Min(0.05f)] private float _cloneInterval = 0.5f;
        [SerializeField, Min(0.1f)] private float _lifetime = 5f;

        private Bullet _bullet;
        private Vector3 _pathDirection;
        private float _elapsed;
        private float _nextCloneTime;
        private bool _armed;

        private void Awake()
        {
            _bullet = GetComponent<Bullet>();
        }

        private void OnEnable()
        {
            if (!_armed) { return; }

            _elapsed = 0f;
            _nextCloneTime = _cloneInterval;
            _bullet?.SetRemainingLifetime(_lifetime);
        }

        private void OnDisable()
        {
            _armed = false;
        }

        private void Update()
        {
            if (!_armed || _bullet == null || _bullet._didHit) { return; }

            Vector3 velocity = _bullet.SelfRigidbody != null
                ? _bullet.SelfRigidbody.velocity
                : Vector3.zero;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                _pathDirection = velocity.normalized;
            }

            _elapsed += Time.deltaTime;
            while (_elapsed >= _nextCloneTime && _elapsed < _lifetime)
            {
                SpawnClone();
                _nextCloneTime += _cloneInterval;
            }

            if (_elapsed >= _lifetime)
            {
                _bullet.DestroyBullet();
            }
        }

        public void Configure(GameObject swirlClonePrefab, float cloneInterval, float lifetime)
        {
            _swirlClonePrefab = swirlClonePrefab;
            _cloneInterval = Mathf.Max(0.05f, cloneInterval);
            _lifetime = Mathf.Max(0.1f, lifetime);
        }

        public void Arm(Vector3 pathDirection)
        {
            pathDirection.y = 0f;
            _pathDirection = pathDirection.sqrMagnitude > 0.0001f
                ? pathDirection.normalized
                : transform.forward;
            _armed = true;
        }

        private void SpawnClone()
        {
            if (_swirlClonePrefab == null || GameObjectsManager.Instance == null) { return; }

            PooledGameObject pooledClone = GameObjectsManager.Instance.SpawnPooledObject(
                _swirlClonePrefab, transform.position,
                Quaternion.LookRotation(_pathDirection, Vector3.up));
            if (pooledClone == null ||
                !pooledClone.TryGetComponent(out PriestSplitterClone clone))
            {
                pooledClone?.ReleaseToPool();
                return;
            }

            clone.Initialize(_pathDirection);
            pooledClone.ShowFromPool();
        }
    }
}
