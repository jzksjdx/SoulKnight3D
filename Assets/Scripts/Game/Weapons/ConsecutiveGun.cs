using UnityEngine;

namespace SoulKnight3D
{
    public sealed class ConsecutiveGun : Gun
    {
        [Header("Consecutive Shots")]
        [SerializeField, Min(2)] private int _shotsPerAttack = 2;
        [SerializeField, Min(0.01f)] private float _shotInterval = 0.1f;

        [Header("Enhanced Bursts")]
        [SerializeField] private GameObject _enhancedBulletPrefab;
        [SerializeField, Min(1f)] private float _enhancedDamageMultiplier = 1.3f;
        [SerializeField] private string _regularShotSound;
        [SerializeField] private string _enhancedShotSound;

        private int _remainingShots;
        private float _nextShotTimer;
        private Vector3 _burstDirection;
        private int _enhancedBurstsRemaining;
        private int _enhancedShotsPerAttack = 8;
        private bool _currentBurstIsEnhanced;
        private bool _followShootPointDuringBurst;
        private Vector3 _shootPointLocalDirection = Vector3.up;

        public int EnhancedBurstsRemaining => _enhancedBurstsRemaining;

        protected override void Update()
        {
            if (_remainingShots <= 0)
            {
                base.Update();
                return;
            }

            _nextShotTimer -= Time.deltaTime;
            if (_nextShotTimer > 0f) { return; }

            FireShot();
            _remainingShots--;
            if (_remainingShots <= 0)
            {
                FinishBurst();
                return;
            }

            _nextShotTimer += _shotInterval;
        }

        private void OnDisable()
        {
            _remainingShots = 0;
            _nextShotTimer = 0f;
            _burstDirection = Vector3.zero;
            _currentBurstIsEnhanced = false;
            _followShootPointDuringBurst = false;
            _shootPointLocalDirection = Vector3.up;
        }

        public override void Attack()
        {
            AttackAlongShootPoint();
        }

        public bool AttackAlongShootPoint()
        {
            return AttackAlongShootPoint(Vector3.up);
        }

        public bool AttackAlongShootPoint(Vector3 localDirection)
        {
            if (shootPoint == null ||
                localDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            _shootPointLocalDirection = localDirection.normalized;
            return BeginBurst(
                shootPoint.TransformDirection(_shootPointLocalDirection),
                true);
        }

        public bool AttackTowards(Vector3 direction)
        {
            return BeginBurst(direction, false);
        }

        private bool BeginBurst(
            Vector3 direction, bool followShootPointDuringBurst)
        {
            if (shootPoint == null || direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            _burstDirection = direction.normalized;
            if (_cooldownTimeout > 0f || _remainingShots > 0)
            {
                return false;
            }

            _followShootPointDuringBurst = followShootPointDuringBurst;
            _currentBurstIsEnhanced =
                _enhancedBurstsRemaining > 0 &&
                _enhancedBulletPrefab != null;
            if (_currentBurstIsEnhanced)
            {
                _enhancedBurstsRemaining--;
            }

            FireShot();
            int shotCount = _currentBurstIsEnhanced
                ? Mathf.Max(2, _enhancedShotsPerAttack)
                : Mathf.Max(2, _shotsPerAttack);
            _remainingShots = shotCount - 1;
            _nextShotTimer = Mathf.Max(0.01f, _shotInterval);

            OnWeaponFired.Trigger();
            return true;
        }

        public void ConfigureBurst(int shotsPerAttack, float shotInterval)
        {
            _shotsPerAttack = Mathf.Max(2, shotsPerAttack);
            _shotInterval = Mathf.Max(0.01f, shotInterval);
        }

        public void ActivateEnhancedBursts(
            int burstCount, int shotsPerBurst, float damageMultiplier)
        {
            _enhancedBurstsRemaining = Mathf.Max(0, burstCount);
            _enhancedShotsPerAttack = Mathf.Max(2, shotsPerBurst);
            _enhancedDamageMultiplier = Mathf.Max(1f, damageMultiplier);
        }

        public void CancelEnhancedBursts()
        {
            _enhancedBurstsRemaining = 0;
            _currentBurstIsEnhanced = false;
        }

        public Bullet SpawnSpecialRocket(
            GameObject projectilePrefab, Vector3 position,
            Vector3 direction, float speed, float damageMultiplier)
        {
            if (projectilePrefab == null ||
                direction.sqrMagnitude <= 0.0001f)
            {
                return null;
            }

            int damage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    InGameData.Damage * Mathf.Max(1f, damageMultiplier)));
            Vector3 normalizedDirection = direction.normalized;
            Bullet bullet = SpawnBulletFromPool(
                position, projectilePrefab, damage, BulletSize);
            bullet.SelfRigidbody.velocity =
                normalizedDirection * Mathf.Max(0.01f, speed);
            bullet.transform.rotation =
                Quaternion.LookRotation(normalizedDirection, Vector3.up);
            return bullet;
        }

        private void FireShot()
        {
            Vector3 shootDirection =
                _followShootPointDuringBurst && shootPoint != null
                    ? shootPoint.TransformDirection(
                        _shootPointLocalDirection)
                    : _burstDirection;
            Vector3 direction = DeviateBullet(shootDirection);
            GameObject projectilePrefab = _currentBurstIsEnhanced
                ? _enhancedBulletPrefab
                : bulletPrefab;
            int damage = _currentBurstIsEnhanced
                ? Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        InGameData.Damage * _enhancedDamageMultiplier))
                : InGameData.Damage;
            Bullet bullet = SpawnBulletFromPool(
                shootPoint.position, projectilePrefab, damage, BulletSize);
            bullet.SelfRigidbody.velocity = direction * BulletSpeed;
            bullet.transform.rotation = Quaternion.LookRotation(direction);
            ShootFeedback?.PlayFeedbacks();

            string shotSound = _currentBurstIsEnhanced
                ? _enhancedShotSound
                : _regularShotSound;
            if (!string.IsNullOrWhiteSpace(shotSound))
            {
                QFramework.AudioKit.PlaySound(shotSound);
            }
        }

        private void FinishBurst()
        {
            _remainingShots = 0;
            _nextShotTimer = 0f;
            _burstDirection = Vector3.zero;
            _currentBurstIsEnhanced = false;
            _followShootPointDuringBurst = false;
            _cooldownTimeout = Mathf.Max(
                _cooldownTimeout,
                InGameData.Cooldown);
        }
    }
}
