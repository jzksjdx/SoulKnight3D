using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class GunParallel : Gun
    {
        public List<Transform> ShootPoints = new List<Transform>();

        public override void Attack()
        {
            if (_cooldownTimeout > 0f) { return; }

            Vector3 bulletDirection = DeviateBullet(shootPoint.up);
            foreach (Transform _shootPoint in ShootPoints)
            {
                Bullet newBullet = SpawnBulletFromPool(_shootPoint.position);
                newBullet.SelfRigidbody.velocity = bulletDirection * BulletSpeed;
                newBullet.transform.rotation = Quaternion.LookRotation(bulletDirection);
            }

            if (Data.EnergyCost > 0)
            {
                OnWeaponFired.Trigger();
            }

            //feedback
            ShootFeedback?.PlayFeedbacks();

            _cooldownTimeout = Data.Cooldown;
        }
    }
}

