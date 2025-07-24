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

		protected Vector3 DeviateBullet(Vector3 shootDirection)
		{
			//float deviateAmount = 0;
            float deviateAmount = (float)InGameData.Inaccuracy / 500;
			return new Vector3(
				shootDirection.x + Random.Range(-deviateAmount, deviateAmount),
				shootDirection.y + Random.Range(-deviateAmount, deviateAmount),
				shootDirection.z + Random.Range(-deviateAmount, deviateAmount)
				);
		}

		public virtual void ShootWithDirection(Vector3 direction)
		{
			Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
            newBullet.SelfRigidbody.velocity = direction * BulletSpeed;
			newBullet.transform.rotation = Quaternion.LookRotation(direction);
            ShootFeedback?.PlayFeedbacks();
        }

		public virtual Bullet SpawnBulletFromPool(Vector3 position)
		{
            GameObject newBulletObj = GameObjectsManager.Instance.SpawnBullet(bulletPrefab)
				.Position(position);
			Bullet newBullet = newBulletObj.GetComponent<Bullet>();
            newBullet.InitializeBullet(tag, InGameData.Damage, GetIsCritHit(), bulletPrefab, BulletSize);
            newBulletObj.Show();
            return newBullet;
		}
    }
}
