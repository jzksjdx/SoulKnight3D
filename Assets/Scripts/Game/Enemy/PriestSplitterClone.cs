using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class PriestSplitterClone : MonoBehaviour
    {
        [SerializeField] private GameObject _lineBulletPrefab;
        [SerializeField, Min(0.05f)] private float _lineBulletInterval = 0.3f;
        [SerializeField, Min(0f)] private float _lineBulletSpeed = 7f;
        [SerializeField, Min(0)] private int _damage = 2;
        [SerializeField, Min(0.1f)] private float _lifetime = 5f;

        private PooledGameObject _pooledObject;
        private Vector3 _lineDirection;
        private float _elapsed;
        private float _nextShotTime;
        private bool _initialized;

        public void Configure(GameObject lineBulletPrefab, float lineBulletInterval,
            float lineBulletSpeed, int damage, float lifetime)
        {
            _lineBulletPrefab = lineBulletPrefab;
            _lineBulletInterval = Mathf.Max(0.05f, lineBulletInterval);
            _lineBulletSpeed = Mathf.Max(0f, lineBulletSpeed);
            _damage = Mathf.Max(0, damage);
            _lifetime = Mathf.Max(0.1f, lifetime);
        }

        public void Initialize(Vector3 parentPathDirection)
        {
            if (_pooledObject == null)
            {
                _pooledObject = GetComponent<PooledGameObject>();
            }

            parentPathDirection.y = 0f;
            parentPathDirection = parentPathDirection.sqrMagnitude > 0.0001f
                ? parentPathDirection.normalized
                : transform.forward;
            _lineDirection = Vector3.Cross(Vector3.up, parentPathDirection).normalized;
            _elapsed = 0f;
            _nextShotTime = 0f;
            _initialized = true;
        }

        private void OnDisable()
        {
            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized) { return; }

            _elapsed += Time.deltaTime;
            while (_elapsed >= _nextShotTime && _elapsed < _lifetime)
            {
                SpawnLineBullet(_lineDirection);
                SpawnLineBullet(-_lineDirection);
                _nextShotTime += _lineBulletInterval;
            }

            if (_elapsed >= _lifetime)
            {
                Release();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_initialized || collision.collider.CompareTag("Enemy")) { return; }

            MountBase mount =
                collision.collider.GetComponentInParent<MountBase>();
            if (mount != null && mount.IsMounted)
            {
                mount.ApplyDamage(_damage);
                Release();
                return;
            }

            PlayerController player =
                collision.collider.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.PlayerStats.ApplyDamage(_damage);
            }
            Release();
        }

        private void SpawnLineBullet(Vector3 direction)
        {
            if (_lineBulletPrefab == null || GameObjectsManager.Instance == null) { return; }

            GameObject bulletObject =
                GameObjectsManager.Instance.SpawnBullet(_lineBulletPrefab);
            if (bulletObject == null ||
                !bulletObject.TryGetComponent(out Bullet bullet))
            {
                return;
            }

            Vector3 spawnPosition = transform.position + direction * 0.12f;
            bulletObject.transform.SetPositionAndRotation(
                spawnPosition, Quaternion.LookRotation(direction, Vector3.up));
            bullet.InitializeBullet("Enemy", _damage, false, _lineBulletPrefab);
            bullet.SelfRigidbody.velocity = direction * _lineBulletSpeed;
            bullet.ShowFromPool();
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
