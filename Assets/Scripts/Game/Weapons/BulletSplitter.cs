using UnityEngine;

namespace SoulKnight3D
{
    public interface IBulletImpactBehavior
    {
        void OnBulletImpact(Bullet sourceBullet, Collision collision);
    }

    public enum BulletSplitPattern
    {
        SixLocalAxes,
        EvenSphere
    }

    [DisallowMultipleComponent]
    public sealed class BulletSplitter : MonoBehaviour, IBulletImpactBehavior
    {
        [SerializeField] private GameObject _splitBulletPrefab;
        [SerializeField] private BulletSplitPattern _pattern = BulletSplitPattern.SixLocalAxes;
        [SerializeField, Min(1)] private int _sphereBulletCount = 18;
        [SerializeField, Min(0)] private int _splitBulletDamage = 2;
        [SerializeField, Min(0.01f)] private float _splitBulletSpeed = 12f;
        [SerializeField, Min(0f)] private float _spawnOffset = 0.12f;
        [SerializeField, Min(0.01f)] private float _splitBulletSize = 1f;
        [SerializeField, Min(0f)] private float _sourceCollisionIgnoreTime = 0.08f;

        public GameObject SplitBulletPrefab => _splitBulletPrefab;
        public BulletSplitPattern Pattern => _pattern;
        public int SplitBulletCount => _pattern == BulletSplitPattern.SixLocalAxes ? 6 : _sphereBulletCount;
        public int SplitBulletDamage => _splitBulletDamage;

        public void Configure(GameObject splitBulletPrefab, BulletSplitPattern pattern,
            int sphereBulletCount, int splitBulletDamage, float splitBulletSpeed,
            float spawnOffset, float splitBulletSize = 1f)
        {
            _splitBulletPrefab = splitBulletPrefab;
            _pattern = pattern;
            _sphereBulletCount = Mathf.Max(1, sphereBulletCount);
            _splitBulletDamage = Mathf.Max(0, splitBulletDamage);
            _splitBulletSpeed = Mathf.Max(0.01f, splitBulletSpeed);
            _spawnOffset = Mathf.Max(0f, spawnOffset);
            _splitBulletSize = Mathf.Max(0.01f, splitBulletSize);
        }

        public void OnBulletImpact(Bullet sourceBullet, Collision collision)
        {
            if (_splitBulletPrefab == null || GameObjectsManager.Instance == null)
            {
                return;
            }

            int count = SplitBulletCount;
            Vector3 splitOrigin = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            Collider ignoredCollider = collision.collider.TryGetComponent(out TargetableObject _)
                ? collision.collider
                : null;
            for (int i = 0; i < count; i++)
            {
                SpawnSplitBullet(sourceBullet, ignoredCollider, splitOrigin, GetDirection(i, count));
            }
        }

        private void SpawnSplitBullet(Bullet sourceBullet, Collider ignoredCollider,
            Vector3 splitOrigin, Vector3 direction)
        {
            GameObject splitObject = GameObjectsManager.Instance.SpawnBullet(_splitBulletPrefab);
            if (splitObject == null || !splitObject.TryGetComponent(out Bullet splitBullet))
            {
                return;
            }

            Vector3 spawnPosition = splitOrigin + direction * _spawnOffset;
            splitObject.transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(direction));
            splitBullet.InitializeBullet(sourceBullet.WeaponTag, _splitBulletDamage, false,
                _splitBulletPrefab, _splitBulletSize);
            splitBullet.SelfRigidbody.velocity = direction * _splitBulletSpeed;
            splitBullet.ShowFromPool();
            splitBullet.IgnoreCollisionTemporarily(ignoredCollider, _sourceCollisionIgnoreTime);
        }

        private Vector3 GetDirection(int index, int count)
        {
            if (_pattern == BulletSplitPattern.SixLocalAxes)
            {
                switch (index)
                {
                    case 0: return transform.right;
                    case 1: return -transform.right;
                    case 2: return transform.up;
                    case 3: return -transform.up;
                    case 4: return transform.forward;
                    default: return -transform.forward;
                }
            }

            const float goldenAngle = Mathf.PI * (3f - 2.2360679775f);
            float y = 1f - 2f * (index + 0.5f) / count;
            float horizontalRadius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float angle = goldenAngle * index;
            Vector3 localDirection = new Vector3(
                Mathf.Cos(angle) * horizontalRadius,
                y,
                Mathf.Sin(angle) * horizontalRadius);
            return transform.TransformDirection(localDirection).normalized;
        }
    }
}
