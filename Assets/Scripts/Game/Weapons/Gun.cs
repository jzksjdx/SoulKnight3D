using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
	public partial class Gun : Weapon
	{
		public float BulletSpeed = 5f;

		public Transform shootPoint;
        public GameObject bulletPrefab;

		void Start()
		{
			// Code Here
		}

        protected override void Update()
        {
			base.Update();
        }

        public virtual void Attack()
		{
			if (_cooldownTimeout > 0f) { return; }
			Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
			Vector3 bulletDirection = DeviateBullet(shootPoint.up);
            newBullet.SelfRigidbody.velocity = bulletDirection * BulletSpeed;
			newBullet.transform.rotation = Quaternion.LookRotation(bulletDirection);

			if (Data.EnergyCost > 0)
			{
                OnWeaponFired.Trigger();
            }

			//feedback
			ShootFeedback?.PlayFeedbacks();

            _cooldownTimeout = Data.Cooldown;
        }

		protected Vector3 DeviateBullet(Vector3 shootDirection)
		{
			//float deviateAmount = 0;
            float deviateAmount = (float)Data.Inaccuracy / 500;
			return new Vector3(
				shootDirection.x + Random.Range(-deviateAmount, deviateAmount),
				shootDirection.y + Random.Range(-deviateAmount, deviateAmount),
				shootDirection.z + Random.Range(-deviateAmount, deviateAmount)
				);
		}

		public void ShootWithDirection(Vector3 direction)
		{
			Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
            newBullet.SelfRigidbody.velocity = direction * BulletSpeed;

            ShootFeedback?.PlayFeedbacks();
        }

		public Bullet SpawnBulletFromPool(Vector3 position)
		{
			GameObject newBulletObj = GameObjectsManager.Instance.SpawnBullet(bulletPrefab)
				.Position(position);
			Bullet newBullet = newBulletObj.GetComponent<Bullet>();
            newBullet.InitializeBullet(tag, Data.Damage, GetIsCritHit(), bulletPrefab);
			newBulletObj.Show();
            return newBullet;
		}
    }
}
