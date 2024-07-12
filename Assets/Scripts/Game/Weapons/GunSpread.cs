using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class GunSpread : Gun
    {
        public int BulletCount;

        public override void Attack()
        {
            if (_cooldownTimeout > 0f) { return; }


            for (int i = 0; i < BulletCount; i ++)
            {
                Vector3 bulletDirection = DeviateBullet(shootPoint.up);
                Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
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

