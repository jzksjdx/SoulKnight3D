using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class Sword : Weapon
    {
        [SerializeField] private MMF_Player SwingFeedback;
        public float RangeMultiplier = 1f;
        [SerializeField] private Transform EffectPosition, SlashEffect;
        [SerializeField] private GameObject HitEffect;

        private bool _isAttacking = false;
        private bool _shouldSwingAgain = false;

        protected override void Start()
        {
            base.Start();

            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                if (gameObject.activeSelf == false) { return; }
                _isAttacking = isAttacking;
                if (isAttacking && !_shouldSwingAgain)
                {
                    ToggleNextSwing(true);
                }

            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            SetChargeSpeed();
        }

        protected override void Update()
        {
            if (_cooldownTimeout > 0f)
            {
                _cooldownTimeout -= Time.deltaTime;
            } else
            {
                if (_isAttacking)
                {
                    ToggleNextSwing(true);
                } else
                {
                    ToggleNextSwing(false);
                }
            }
        }

        public void SetChargeSpeed(float multiplier = 1)
        {
            PlayerController.Instance.PlayerAnimation.SetChargeSpeed(1 / InGameData.Cooldown * multiplier);
        }

        public void AttackFromAniamtion()
        {
            if (_cooldownTimeout > 0f) { return; }
            _cooldownTimeout = 0.3f * InGameData.Cooldown;
            float range = 0.3f * RangeMultiplier;
            Vector3 boxSize = new Vector3(range * 3f, 0.7f, range * 3);
            Vector3 attackCenter = PlayerController.Instance.transform.position + Vector3.up * 0.4f + PlayerController.Instance.transform.forward * range * 1.5f;

            Quaternion boxRotation = PlayerController.Instance.transform.rotation;

            bool didHit = false;
            Collider[] targets = Physics.OverlapBox(attackCenter, boxSize / 2, boxRotation);
            foreach (Collider target in targets)
            {
                if (target.CompareTag(tag)) { continue; }

                // handle targetable objects (enemies, room objects)
                if (target.TryGetComponent(out TargetableObject targetableObject))
                {
                    if (targetableObject.IsDead) { continue; }
                    didHit = true;
                    targetableObject.ApplyDamage(InGameData.Damage);

                    if (GetIsCritHit())
                    {
                        GameController.Instance.SpawnCritText(InGameData.Damage * 2, target.ClosestPoint(transform.position));
                        AudioKit.PlaySound("fx_hit");
                    }
                    else
                    {
                        GameController.Instance.SpawnDamageText(InGameData.Damage, target.ClosestPoint(transform.position));
                    }
                    continue;
                }

                // handle bullet hit
                if (target.TryGetComponent(out Bullet bullet))
                {
                    didHit = true;
                    bullet.DestroyBullet();
                }
            }
            
            SlashEffect.SetParent(null);
            SlashEffect.position = EffectPosition.position;
            SlashEffect.rotation = EffectPosition.rotation;
            HitEffect.Hide();
            if (didHit)
            {
                HitEffect.Show();
            }
            SwingFeedback?.PlayFeedbacks();

        }

        private void ToggleNextSwing(bool shouldSwing)
        {
            _shouldSwingAgain = shouldSwing;
            PlayerController.Instance.PlayerAnimation.SetAnimationMeleeNextMove(shouldSwing);
        }

        private void OnDestroy()
        {
            Destroy(SlashEffect);
        }

        private void OnEnable()
        {
            SetChargeSpeed();
        }

        //private void OnDrawGizmosSelected()
        //{
        //    if (PlayerController.Instance == null) { return; }
        //    float range = 0.3f * RangeMultiplier;
        //    Vector3 boxSize = new Vector3(range * 3f, 0.7f, range * 3);
        //    Vector3 attackCenter = PlayerController.Instance.transform.position + Vector3.up * 0.4f + PlayerController.Instance.transform.forward * range * 1.5f;
        //    Gizmos.DrawCube(attackCenter, boxSize);
        //}
    }

}
