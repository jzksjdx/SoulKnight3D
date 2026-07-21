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
        [SerializeField, Min(0f)] private float _surfaceOffset = 0.02f;
        [SerializeField, Min(0f)] private float _collisionSeparationPadding = 0.02f;

        public GameObject SplitBulletPrefab => _splitBulletPrefab;
        public BulletSplitPattern Pattern => _pattern;
        public int SplitBulletCount => _pattern == BulletSplitPattern.SixLocalAxes ? 6 : _sphereBulletCount;
        public int SplitBulletDamage => _splitBulletDamage;

        public void Configure(GameObject splitBulletPrefab, BulletSplitPattern pattern,
            int sphereBulletCount, int splitBulletDamage, float splitBulletSpeed,
            float spawnOffset, float splitBulletSize = 1f,
            float collisionSeparationPadding = 0.02f)
        {
            _splitBulletPrefab = splitBulletPrefab;
            _pattern = pattern;
            _sphereBulletCount = Mathf.Max(1, sphereBulletCount);
            _splitBulletDamage = Mathf.Max(0, splitBulletDamage);
            _splitBulletSpeed = Mathf.Max(0.01f, splitBulletSpeed);
            _spawnOffset = Mathf.Max(0f, spawnOffset);
            _splitBulletSize = Mathf.Max(0.01f, splitBulletSize);
            _collisionSeparationPadding = Mathf.Max(0f, collisionSeparationPadding);
        }

        public void OnBulletImpact(Bullet sourceBullet, Collision collision)
        {
            if (_splitBulletPrefab == null || GameObjectsManager.Instance == null)
            {
                return;
            }

            int count = SplitBulletCount;
            Vector3 incomingVelocity = sourceBullet.PreCollisionVelocity;
            Quaternion impactRotation = sourceBullet.PreCollisionRotation;
            bool hasContact = collision.contactCount > 0;
            Vector3 contactPoint = transform.position;
            Vector3 surfaceNormal = Vector3.zero;
            if (hasContact)
            {
                ContactPoint contact = collision.GetContact(0);
                contactPoint = contact.point;
                surfaceNormal = contact.normal.normalized;

                if (incomingVelocity.sqrMagnitude <= 0.0001f)
                {
                    incomingVelocity = impactRotation * Vector3.forward;
                }
                if (surfaceNormal.sqrMagnitude <= 0.0001f)
                {
                    surfaceNormal = -incomingVelocity.normalized;
                }

                // Contact normals can face either way, especially after CCD has
                // resolved a deep, head-on impact. The safe side is opposite the
                // incoming bullet velocity.
                if (Vector3.Dot(surfaceNormal, incomingVelocity) > 0f)
                {
                    surfaceNormal = -surfaceNormal;
                }
            }

            TargetableObject impactedTarget = collision.collider.GetComponentInParent<TargetableObject>();
            Collider[] ignoredColliders = impactedTarget != null
                ? impactedTarget.GetComponentsInChildren<Collider>(true)
                : null;
            for (int i = 0; i < count; i++)
            {
                SpawnSplitBullet(sourceBullet, collision.collider, ignoredColliders,
                    contactPoint, surfaceNormal, hasContact,
                    GetDirection(i, count, impactRotation));
            }
        }

        private void SpawnSplitBullet(Bullet sourceBullet, Collider impactCollider,
            Collider[] ignoredColliders, Vector3 contactPoint, Vector3 surfaceNormal,
            bool hasContact, Vector3 direction)
        {
            GameObject splitObject = GameObjectsManager.Instance.SpawnBullet(_splitBulletPrefab);
            if (splitObject == null || !splitObject.TryGetComponent(out Bullet splitBullet))
            {
                return;
            }

            direction.Normalize();
            splitObject.transform.rotation = Quaternion.LookRotation(direction);
            splitBullet.InitializeBullet(sourceBullet.WeaponTag, _splitBulletDamage, false,
                _splitBulletPrefab, _splitBulletSize);

            Vector3 spawnPosition;
            if (hasContact)
            {
                float colliderClearance = GetColliderClearance(splitBullet, surfaceNormal);
                Vector3 directionalOffset = direction * _spawnOffset;
                float inwardOffset = Vector3.Dot(directionalOffset, surfaceNormal);
                if (inwardOffset < 0f)
                {
                    directionalOffset -= surfaceNormal * inwardOffset;
                }

                spawnPosition = contactPoint +
                    surfaceNormal * (_surfaceOffset + colliderClearance) + directionalOffset;
            }
            else
            {
                spawnPosition = contactPoint + direction * _spawnOffset;
            }

            spawnPosition = ResolvePenetration(splitBullet.SelfCapsuleCollider,
                spawnPosition, splitObject.transform.rotation, impactCollider);
            splitObject.transform.position = spawnPosition;
            splitBullet.SelfRigidbody.velocity = direction * _splitBulletSpeed;
            splitBullet.ShowFromPool();

            // A collider activated from inside another collision callback can be
            // evaluated against that callback's stale contact manifold. Protect
            // only fragments that are leaving or skimming the impact surface.
            // Inward fragments still collide with ordinary walls as expected.
            if (ignoredColliders == null && hasContact && impactCollider != null &&
                Vector3.Dot(direction, surfaceNormal) >= -0.0001f)
            {
                splitBullet.IgnoreCollisionUntilSeparated(impactCollider, 0.002f);
            }

            if (ignoredColliders == null)
            {
                return;
            }

            for (int i = 0; i < ignoredColliders.Length; i++)
            {
                splitBullet.IgnoreCollisionUntilSeparated(ignoredColliders[i],
                    _collisionSeparationPadding);
            }
        }

        private static float GetColliderClearance(Bullet bullet, Vector3 surfaceNormal)
        {
            CapsuleCollider capsule = bullet.SelfCapsuleCollider;
            if (capsule == null)
            {
                return 0f;
            }

            Transform bulletTransform = capsule.transform;
            Vector3 scale = bulletTransform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x),
                Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            float localExtent = Mathf.Max(capsule.radius, capsule.height * 0.5f);
            Vector3 centerOffset = bulletTransform.TransformVector(capsule.center);
            return Mathf.Abs(Vector3.Dot(centerOffset, surfaceNormal)) +
                localExtent * maxScale;
        }

        private static Vector3 ResolvePenetration(CapsuleCollider bulletCollider,
            Vector3 position, Quaternion rotation, Collider impactCollider)
        {
            if (bulletCollider == null || impactCollider == null ||
                !impactCollider.enabled || !impactCollider.gameObject.activeInHierarchy)
            {
                return position;
            }

            const int maxIterations = 4;
            const float separationSkin = 0.002f;
            for (int i = 0; i < maxIterations; i++)
            {
                if (!Physics.ComputePenetration(
                    bulletCollider, position, rotation,
                    impactCollider, impactCollider.transform.position,
                    impactCollider.transform.rotation,
                    out Vector3 separationDirection, out float separationDistance))
                {
                    break;
                }

                position += separationDirection * (separationDistance + separationSkin);
            }

            return position;
        }

        private Vector3 GetDirection(int index, int count, Quaternion impactRotation)
        {
            if (_pattern == BulletSplitPattern.SixLocalAxes)
            {
                switch (index)
                {
                    case 0: return impactRotation * Vector3.right;
                    case 1: return impactRotation * Vector3.left;
                    case 2: return impactRotation * Vector3.up;
                    case 3: return impactRotation * Vector3.down;
                    case 4: return impactRotation * Vector3.forward;
                    default: return impactRotation * Vector3.back;
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
            return (impactRotation * localDirection).normalized;
        }
    }
}
