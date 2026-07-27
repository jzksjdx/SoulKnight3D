using MoreMountains.Feedbacks;
using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class ClusterRocket : MonoBehaviour
    {
        [Header("Split")]
        [SerializeField] private GameObject _splitRocketPrefab;
        [SerializeField] private MMF_Player _splitFeedback;
        [SerializeField, Min(0.05f)] private float _splitDelay = 0.3f;
        [SerializeField, Min(2)] private int _splitCount = 4;
        [SerializeField, Min(0f)] private float _angleStep = 22f;
        [SerializeField, Min(0.01f)] private float _splitRocketSpeed = 16f;
        [SerializeField, Min(0)] private int _splitRocketDamage = 3;
        [SerializeField, Min(0f)] private float _spawnOffset = 0.12f;

        private Bullet _bullet;
        private float _elapsed;
        private bool _didSplit;

        private void Awake()
        {
            _bullet = GetComponent<Bullet>();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _didSplit = false;
        }

        private void Update()
        {
            if (_didSplit || _bullet == null || _bullet._didHit) { return; }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _splitDelay)
            {
                Split();
            }
        }

        public void Configure(GameObject splitRocketPrefab, MMF_Player splitFeedback,
            float splitDelay, int splitCount, float angleStep,
            float splitRocketSpeed, int splitRocketDamage, float spawnOffset)
        {
            _splitRocketPrefab = splitRocketPrefab;
            _splitFeedback = splitFeedback;
            _splitDelay = Mathf.Max(0.05f, splitDelay);
            _splitCount = Mathf.Max(2, splitCount);
            _angleStep = Mathf.Max(0f, angleStep);
            _splitRocketSpeed = Mathf.Max(0.01f, splitRocketSpeed);
            _splitRocketDamage = Mathf.Max(0, splitRocketDamage);
            _spawnOffset = Mathf.Max(0f, spawnOffset);
        }

        private void Split()
        {
            _didSplit = true;

            if (_splitRocketPrefab != null && GameObjectsManager.Instance != null)
            {
                Vector3 forward = _bullet.SelfRigidbody != null
                    ? _bullet.SelfRigidbody.velocity
                    : Vector3.zero;
                forward.y = 0f;
                if (forward.sqrMagnitude <= 0.0001f)
                {
                    forward = transform.forward;
                    forward.y = 0f;
                }
                forward.Normalize();

                float center = (_splitCount - 1) * 0.5f;
                for (int i = 0; i < _splitCount; i++)
                {
                    float angle = (i - center) * _angleStep;
                    Vector3 direction =
                        Quaternion.AngleAxis(angle, Vector3.up) * forward;
                    SpawnSplitRocket(direction);
                }
            }

            _splitFeedback?.PlayFeedbacks();
            _bullet.DestroyBullet();
        }

        private void SpawnSplitRocket(Vector3 direction)
        {
            GameObject rocketObject =
                GameObjectsManager.Instance.SpawnBullet(_splitRocketPrefab);
            if (rocketObject == null ||
                !rocketObject.TryGetComponent(out Bullet rocket))
            {
                return;
            }

            Vector3 position = transform.position + direction * _spawnOffset;
            rocketObject.transform.SetPositionAndRotation(
                position, Quaternion.LookRotation(direction, Vector3.up));
            rocket.InitializeBullet(_bullet.WeaponTag, _splitRocketDamage, false,
                _splitRocketPrefab);
            rocket.SelfRigidbody.velocity = direction * _splitRocketSpeed;
            rocket.ShowFromPool();
        }
    }
}
