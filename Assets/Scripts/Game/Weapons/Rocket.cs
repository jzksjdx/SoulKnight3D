using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class Rocket : Bullet
    {
        [SerializeField] float ExplosionRadius = 1f;

        [Header("Explosion Force")]
        [SerializeField, Min(0f)] private float _horizontalImpulse = 5f;
        [SerializeField, Min(0f)] private float _upwardImpulse = 3f;

        private readonly HashSet<TargetableObject> _affectedTargets =
            new HashSet<TargetableObject>();

        protected override void OnBulletCollision(Collision other)
        {
            Explode();
        }

        private void Explode()
        {
            _affectedTargets.Clear();
            Collider[] targets = Physics.OverlapSphere(transform.position, ExplosionRadius);
            foreach (Collider target in targets)
            {
                TargetableObject targetableObject =
                    target.GetComponentInParent<TargetableObject>();
                if (targetableObject == null ||
                    !_affectedTargets.Add(targetableObject) ||
                    targetableObject.IsDead ||
                    targetableObject.CompareTag(_weaponTag))
                {
                    continue;
                }

                ApplyExplosionImpulse(target, targetableObject);
                targetableObject.ApplyDamage(_damage);

                if (_weaponTag == "Player" &&
                    targetableObject.CompareTag("Enemy"))
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
            _affectedTargets.Clear();

            PlayImpactFeedback();
            DestroyBullet();
        }

        private void ApplyExplosionImpulse(
            Collider targetCollider, TargetableObject target)
        {
            Rigidbody targetBody = targetCollider.attachedRigidbody;
            if (targetBody == null)
            {
                targetBody = target.GetComponent<Rigidbody>();
            }
            if (targetBody == null || targetBody.isKinematic) { return; }

            Vector3 horizontalDirection =
                targetBody.worldCenterOfMass - transform.position;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude <= 0.0001f)
            {
                horizontalDirection = PreCollisionVelocity;
                horizontalDirection.y = 0f;
            }

            Vector3 impulse = Vector3.up * _upwardImpulse;
            if (horizontalDirection.sqrMagnitude > 0.0001f)
            {
                impulse += horizontalDirection.normalized * _horizontalImpulse;
            }

            if (impulse.sqrMagnitude <= 0f) { return; }
            targetBody.AddForce(impulse, ForceMode.Impulse);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }

}
