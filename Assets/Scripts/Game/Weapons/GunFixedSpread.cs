using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class GunFixedSpread : Gun
    {
        public int BulletCount;

        public override void Attack()
        {
            if (_cooldownTimeout > 0f) { return; }

            float deviateAmount = (float)InGameData.Inaccuracy / 6;
            List<Quaternion> angles = new List<Quaternion>
            {
                Quaternion.Euler(deviateAmount, 0, deviateAmount),
                Quaternion.Euler(-deviateAmount, 0, deviateAmount),
                Quaternion.Euler(-deviateAmount, 0, -deviateAmount),
                Quaternion.Euler(deviateAmount, 0, -deviateAmount)
            };
            foreach(Quaternion angle in angles)
            {
                Vector3 bulletDirection = angle * Vector3.up;
                
                Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
                Vector3 worldDirection = shootPoint.TransformDirection(bulletDirection);
                newBullet.SelfRigidbody.velocity = worldDirection * BulletSpeed;
                newBullet.transform.rotation = Quaternion.LookRotation(worldDirection);
            }
            

            if (InGameData.EnergyCost > 0)
            {
                OnWeaponFired.Trigger();
            }

            //feedback
            ShootFeedback?.PlayFeedbacks();

            _cooldownTimeout = InGameData.Cooldown;
        }
    }

}
