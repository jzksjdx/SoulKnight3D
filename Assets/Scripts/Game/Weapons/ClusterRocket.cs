using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class ClusterRocket : MonoBehaviour
    {
        [Header("Split")]
        [SerializeField] private GameObject _splitRocketPrefab;
        [SerializeField] private MMF_Player _splitFeedback;
        [SerializeField, Min(0.05f)] private float _splitDelay = 0.3f;
        [FormerlySerializedAs("_angleStep")]
        [SerializeField, Min(0f)] private float _splitAngle = 22f;
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
            float splitDelay, float splitAngle,
            float splitRocketSpeed, int splitRocketDamage, float spawnOffset)
        {
            _splitRocketPrefab = splitRocketPrefab;
            _splitFeedback = splitFeedback;
            _splitDelay = Mathf.Max(0.05f, splitDelay);
            _splitAngle = Mathf.Max(0f, splitAngle);
            _splitRocketSpeed = Mathf.Max(0.01f, splitRocketSpeed);
            _splitRocketDamage = Mathf.Max(0, splitRocketDamage);
            _spawnOffset = Mathf.Max(0f, spawnOffset);
        }

        private void Split()
        {
            _didSplit = true;

            if (_splitRocketPrefab != null && GameObjectsManager.Instance != null)
            {
                Quaternion sourceRotation = transform.rotation;
                if (_bullet.SelfRigidbody != null &&
                    _bullet.SelfRigidbody.velocity.sqrMagnitude > 0.0001f)
                {
                    Vector3 velocityDirection =
                        _bullet.SelfRigidbody.velocity.normalized;
                    Quaternion velocityCorrection = Quaternion.FromToRotation(
                        sourceRotation * Vector3.forward, velocityDirection);
                    sourceRotation = velocityCorrection * sourceRotation;
                }

                float angle = _splitAngle;
                Quaternion[] localOffsets =
                {
                    Quaternion.Euler(angle, 0f, angle),
                    Quaternion.Euler(-angle, 0f, angle),
                    Quaternion.Euler(-angle, 0f, -angle),
                    Quaternion.Euler(angle, 0f, -angle)
                };

                Quaternion upToForward =
                    Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                Quaternion forwardToUp = Quaternion.Inverse(upToForward);

                for (int i = 0; i < localOffsets.Length; i++)
                {
                    Quaternion splitRotation =
                        sourceRotation * upToForward *
                        localOffsets[i] * forwardToUp;
                    Vector3 direction =
                        (splitRotation * Vector3.forward).normalized;
                    SpawnSplitRocket(direction, splitRotation);
                }
            }

            _splitFeedback?.PlayFeedbacks();
            _bullet.DestroyBullet();
        }

        private void SpawnSplitRocket(
            Vector3 direction, Quaternion splitRotation)
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
                position, splitRotation);
            rocket.InitializeBullet(_bullet.WeaponTag, _splitRocketDamage, false,
                _splitRocketPrefab);
            rocket.SelfRigidbody.velocity = direction * _splitRocketSpeed;
            rocket.ShowFromPool();
        }
    }
}
