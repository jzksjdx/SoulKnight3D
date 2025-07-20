using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class Rocket : Bullet
    {
        [SerializeField] float ExplosionRadius = 1f;

        protected override void Awake()
        {
            SelfCapsuleCollider.OnCollisionEnterEvent((other) =>
            {
                if (_didHit) { return; }
                _didHit = true;
                Explode();

            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Explode()
        {
            Collider[] targets = Physics.OverlapSphere(transform.position, ExplosionRadius);
            foreach(Collider target in targets)
            {
                if (target.TryGetComponent(out TargetableObject targetableObject))
                {
                    if (targetableObject.IsDead) { continue; }
                    if (_weaponTag == "Player" && target.CompareTag("Player")) { continue; }
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

            if (ImpactFeedback)
            {
                ImpactFeedback.GetFeedbackOfType<MMF_ParticlesInstantiation>().TargetWorldPosition = transform.position;
                ImpactFeedback?.PlayFeedbacks();
            }

            DestroyBullet();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }

}
