using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class Rocket : Bullet
    {
        [SerializeField] float ExplosionRadius = 1f;

        protected override void OnBulletCollision(Collision other)
        {
            Explode();
        }

        private void Explode()
        {
            Collider[] targets = Physics.OverlapSphere(transform.position, ExplosionRadius);
            foreach(Collider target in targets)
            {
                if (target.TryGetComponent(out TargetableObject targetableObject))
                {
                    if (targetableObject.IsDead) { continue; }
                    if (target.CompareTag(_weaponTag)) { continue; }
                    targetableObject.ApplyDamage(_damage);

                    if (_weaponTag == "Player" && target.CompareTag("Enemy"))
                    {
                        // player attaking other objects
                        if (_isCritHit)
                        {
                            GameController.Instance.SpawnCritText(_damage, transform.position);
                            AudioKit.PlaySound("fx_hit");
                        }
                        else
                        {
                            GameController.Instance.SpawnDamageText(_damage, transform.position);
                        }
                    }
                }
            }

            PlayImpactFeedback();
            DestroyBullet();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }

}
