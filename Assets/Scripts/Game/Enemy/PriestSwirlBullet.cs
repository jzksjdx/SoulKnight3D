using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class PriestSwirlBullet : MonoBehaviour
    {
        [SerializeField] private GameObject _splitBulletPrefab;
        [SerializeField, Min(0.05f)] private float _splitDelay = 0.75f;
        [SerializeField, Min(1)] private int _splitCount = 3;
        [SerializeField, Range(0f, 180f)] private float _splitArc = 28f;
        [SerializeField, Min(0f)] private float _splitSpeed = 7f;
        [SerializeField, Min(0)] private int _splitDamage = 2;
        [SerializeField] private float _turnDegreesPerSecond = 95f;

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

        private void FixedUpdate()
        {
            if (_bullet == null || _bullet.SelfRigidbody == null || _bullet._didHit) { return; }

            Vector3 velocity = _bullet.SelfRigidbody.velocity;
            float speed = velocity.magnitude;
            if (speed <= 0.001f) { return; }

            Vector3 curvedDirection = Quaternion.Euler(
                0f, _turnDegreesPerSecond * Time.fixedDeltaTime, 0f) * velocity.normalized;
            _bullet.SelfRigidbody.velocity = curvedDirection * speed;
            transform.rotation = Quaternion.LookRotation(curvedDirection, Vector3.up);
        }

        public void Configure(GameObject splitBulletPrefab, float splitDelay, int splitCount,
            float splitArc, float splitSpeed, int splitDamage, float turnDegreesPerSecond)
        {
            _splitBulletPrefab = splitBulletPrefab;
            _splitDelay = Mathf.Max(0.05f, splitDelay);
            _splitCount = Mathf.Max(1, splitCount);
            _splitArc = Mathf.Clamp(splitArc, 0f, 180f);
            _splitSpeed = Mathf.Max(0f, splitSpeed);
            _splitDamage = Mathf.Max(0, splitDamage);
            _turnDegreesPerSecond = turnDegreesPerSecond;
        }

        private void Split()
        {
            _didSplit = true;
            if (_splitBulletPrefab != null && GameObjectsManager.Instance != null)
            {
                Vector3 baseDirection = _bullet.SelfRigidbody.velocity;
                baseDirection.y = 0f;
                if (baseDirection.sqrMagnitude <= 0.0001f)
                {
                    baseDirection = transform.forward;
                }
                baseDirection.Normalize();

                for (int i = 0; i < _splitCount; i++)
                {
                    float t = _splitCount == 1 ? 0.5f : i / (float)(_splitCount - 1);
                    float angle = Mathf.Lerp(-_splitArc * 0.5f, _splitArc * 0.5f, t);
                    SpawnFragment(Quaternion.Euler(0f, angle, 0f) * baseDirection);
                }
            }
            _bullet.DestroyBullet();
        }

        private void SpawnFragment(Vector3 direction)
        {
            GameObject fragmentObject = GameObjectsManager.Instance.SpawnBullet(_splitBulletPrefab);
            if (fragmentObject == null || !fragmentObject.TryGetComponent(out Bullet fragment))
            {
                return;
            }

            fragmentObject.transform.SetPositionAndRotation(
                transform.position + direction * 0.12f,
                Quaternion.LookRotation(direction, Vector3.up));
            fragment.InitializeBullet("Enemy", _splitDamage, false, _splitBulletPrefab);
            fragment.SelfRigidbody.velocity = direction * _splitSpeed;
            fragment.ShowFromPool();
        }
    }
}
