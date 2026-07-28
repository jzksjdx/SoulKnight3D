using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
	public partial class Gun : Weapon
	{
		public float BulletSpeed = 5f;

		public Transform shootPoint;
        public GameObject bulletPrefab;
		public float BulletSize = 1f;

		protected override void Start()
		{
			base.Start();
		}

        protected override void Update()
        {
			base.Update();
        }

        public override void Attack()
		{
			if (_cooldownTimeout > 0f) { return; }
			Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
			Vector3 bulletDirection = DeviateBullet(shootPoint.up);
            newBullet.SelfRigidbody.velocity = bulletDirection * BulletSpeed;
			newBullet.transform.rotation = Quaternion.LookRotation(bulletDirection);

            OnWeaponFired.Trigger();

            //feedback
            ShootFeedback?.PlayFeedbacks();

            _cooldownTimeout = InGameData.Cooldown;
        }

        public bool AimAt(Vector3 targetPosition)
        {
            if (shootPoint == null) { return false; }

            Vector3 aimDirection = targetPosition - shootPoint.position;
            if (aimDirection.sqrMagnitude <= 0.0001f) { return false; }

            Quaternion correction = Quaternion.FromToRotation(shootPoint.up, aimDirection.normalized);
            transform.rotation = correction * transform.rotation;
            return true;
        }

		protected Vector3 DeviateBullet(Vector3 shootDirection)
		{
			//float deviateAmount = 0;
            float deviateAmount = (float)InGameData.Inaccuracy / 500;
			Vector3 deviatedDirection = new Vector3(
				shootDirection.x + Random.Range(-deviateAmount, deviateAmount),
				shootDirection.y + Random.Range(-deviateAmount, deviateAmount),
				shootDirection.z + Random.Range(-deviateAmount, deviateAmount)
				);
            return deviatedDirection.sqrMagnitude <= 0.0001f ? shootDirection.normalized : deviatedDirection.normalized;
		}

		public virtual void ShootWithDirection(Vector3 direction)
		{
			Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
            newBullet.SelfRigidbody.velocity = direction * BulletSpeed;
			newBullet.transform.rotation = Quaternion.LookRotation(direction);
            ShootFeedback?.PlayFeedbacks();
        }

        public virtual void ShootAtTarget(Transform target, Vector3 aimPosition)
        {
            if (target == null || shootPoint == null) { return; }

            Vector3 direction = aimPosition - shootPoint.position;
            if (direction.sqrMagnitude <= 0.0001f) { return; }
            ShootWithDirection(direction.normalized);
        }

		public virtual Bullet SpawnBulletFromPool(Vector3 position)
		{
            return SpawnBulletFromPool(
                position, bulletPrefab, InGameData.Damage, BulletSize);
		}

        protected Bullet SpawnBulletFromPool(
            Vector3 position, GameObject projectilePrefab, int damage,
            float bulletSize)
        {
            GameObject newBulletObj =
                GameObjectsManager.Instance.SpawnBullet(projectilePrefab)
                    .Position(position);
            Bullet newBullet = newBulletObj.GetComponent<Bullet>();
            newBullet.InitializeBullet(
                tag, damage, GetIsCritHit(), projectilePrefab, bulletSize);
            newBullet.ShowFromPool();
            return newBullet;
        }
    }
}
