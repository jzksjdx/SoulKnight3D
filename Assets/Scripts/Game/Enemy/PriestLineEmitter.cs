using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class PriestLineEmitter : MonoBehaviour
    {
        [SerializeField] private GameObject _emittedBulletPrefab;
        [SerializeField, Min(0f)] private float _travelTime = 0.45f;
        [SerializeField, Min(1)] private int _burstCount = 5;
        [SerializeField, Min(0.02f)] private float _burstInterval = 0.18f;
        [SerializeField, Min(0f)] private float _emittedBulletSpeed = 7f;
        [SerializeField, Min(0)] private int _emittedBulletDamage = 2;

        private Bullet _bullet;
        private float _elapsed;
        private float _nextBurstTime;
        private int _burstsEmitted;
        private bool _isStationary;

        private void Awake()
        {
            _bullet = GetComponent<Bullet>();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _nextBurstTime = _travelTime;
            _burstsEmitted = 0;
            _isStationary = false;
        }

        private void Update()
        {
            if (_bullet == null || _bullet._didHit) { return; }

            _elapsed += Time.deltaTime;
            if (!_isStationary && _elapsed >= _travelTime)
            {
                _isStationary = true;
                _bullet.SelfRigidbody.velocity = Vector3.zero;
            }

            while (_isStationary && _burstsEmitted < _burstCount &&
                _elapsed >= _nextBurstTime)
            {
                EmitPair();
                _burstsEmitted++;
                _nextBurstTime += _burstInterval;
            }

            if (_burstsEmitted >= _burstCount)
            {
                _bullet.DestroyBullet();
            }
        }

        public void Configure(GameObject emittedBulletPrefab, float travelTime, int burstCount,
            float burstInterval, float emittedBulletSpeed, int emittedBulletDamage)
        {
            _emittedBulletPrefab = emittedBulletPrefab;
            _travelTime = Mathf.Max(0f, travelTime);
            _burstCount = Mathf.Max(1, burstCount);
            _burstInterval = Mathf.Max(0.02f, burstInterval);
            _emittedBulletSpeed = Mathf.Max(0f, emittedBulletSpeed);
            _emittedBulletDamage = Mathf.Max(0, emittedBulletDamage);
        }

        private void EmitPair()
        {
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();
            SpawnBullet(right);
            SpawnBullet(-right);
        }

        private void SpawnBullet(Vector3 direction)
        {
            if (_emittedBulletPrefab == null || GameObjectsManager.Instance == null) { return; }

            GameObject bulletObject = GameObjectsManager.Instance.SpawnBullet(_emittedBulletPrefab);
            if (bulletObject == null || !bulletObject.TryGetComponent(out Bullet bullet)) { return; }

            bulletObject.transform.SetPositionAndRotation(
                transform.position + direction * 0.12f,
                Quaternion.LookRotation(direction, Vector3.up));
            bullet.InitializeBullet("Enemy", _emittedBulletDamage, false, _emittedBulletPrefab);
            bullet.SelfRigidbody.velocity = direction * _emittedBulletSpeed;
            bullet.ShowFromPool();
        }
    }
}
