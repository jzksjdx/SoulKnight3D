using UnityEngine;

namespace SoulKnight3D
{
    public sealed class HelixGun : Gun
    {
        [Header("Helix")]
        [SerializeField, Min(0f)] private float _radius = 0.22f;
        [SerializeField] private float _rotationsPerSecond = 2.5f;
        [SerializeField, Range(0f, 360f)] private float _pairPhaseOffset = 180f;

        public override void Attack()
        {
            if (_cooldownTimeout > 0f) { return; }

            FirePair(DeviateBullet(shootPoint.up));
            OnWeaponFired.Trigger();
            ShootFeedback?.PlayFeedbacks();
            _cooldownTimeout = InGameData.Cooldown;
        }

        public override void ShootWithDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f) { return; }

            FirePair(direction.normalized);
            ShootFeedback?.PlayFeedbacks();
        }

        public void ConfigureHelix(float radius, float rotationsPerSecond,
            float pairPhaseOffset)
        {
            _radius = Mathf.Max(0f, radius);
            _rotationsPerSecond = rotationsPerSecond;
            _pairPhaseOffset = Mathf.Repeat(pairPhaseOffset, 360f);
        }

        private void FirePair(Vector3 direction)
        {
            for (int i = 0; i < 2; i++)
            {
                float phase = i * _pairPhaseOffset;
                Bullet bullet = SpawnBulletFromPool(shootPoint.position);
                if (!bullet.TryGetComponent(out HelixBullet helixBullet))
                {
                    bullet.SelfRigidbody.velocity = direction * BulletSpeed;
                    bullet.transform.rotation =
                        Quaternion.LookRotation(direction, Vector3.up);
                    continue;
                }

                helixBullet.Arm(shootPoint.position, direction, BulletSpeed,
                    _radius, _rotationsPerSecond, phase);
            }
        }
    }
}
