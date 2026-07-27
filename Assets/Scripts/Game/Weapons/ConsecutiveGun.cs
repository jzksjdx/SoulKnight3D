using UnityEngine;

namespace SoulKnight3D
{
    public sealed class ConsecutiveGun : Gun
    {
        [Header("Consecutive Shots")]
        [SerializeField, Min(2)] private int _shotsPerAttack = 2;
        [SerializeField, Min(0.01f)] private float _shotInterval = 0.1f;

        private int _remainingShots;
        private float _nextShotTimer;

        protected override void Update()
        {
            base.Update();

            if (_remainingShots <= 0) { return; }

            _nextShotTimer -= Time.deltaTime;
            if (_nextShotTimer > 0f) { return; }

            FireShot();
            _remainingShots--;
            _nextShotTimer += _shotInterval;
        }

        private void OnDisable()
        {
            _remainingShots = 0;
            _nextShotTimer = 0f;
        }

        public override void Attack()
        {
            if (_cooldownTimeout > 0f || _remainingShots > 0) { return; }

            FireShot();
            _remainingShots = Mathf.Max(2, _shotsPerAttack) - 1;
            _nextShotTimer = Mathf.Max(0.01f, _shotInterval);

            OnWeaponFired.Trigger();
            _cooldownTimeout = InGameData.Cooldown;
        }

        public void ConfigureBurst(int shotsPerAttack, float shotInterval)
        {
            _shotsPerAttack = Mathf.Max(2, shotsPerAttack);
            _shotInterval = Mathf.Max(0.01f, shotInterval);
        }

        private void FireShot()
        {
            Vector3 direction = DeviateBullet(shootPoint.up);
            Bullet bullet = SpawnBulletFromPool(shootPoint.position);
            bullet.SelfRigidbody.velocity = direction * BulletSpeed;
            bullet.transform.rotation = Quaternion.LookRotation(direction);
            ShootFeedback?.PlayFeedbacks();
        }
    }
}
