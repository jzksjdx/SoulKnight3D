using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Dodge : Skill
    {
        [SerializeField] float DodgeForce = 3f;

        private Weapon _weaponWithDodgeBonus;

        protected override void Start()
        {
            base.Start();

            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((_, __) =>
            {
                if (_weaponWithDodgeBonus != null)
                {
                    _weaponWithDodgeBonus.ClearGuaranteedCriticalHit();
                    _weaponWithDodgeBonus = null;
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public override bool UseSkill()
        {
            if (!base.UseSkill()) { return false; }
            PlayerController.Instance.PlayerAttack.CancelCurrentWeaponCharge();
            PlayerController.Instance.PlayerAnimation.ToggleDodge(true);
            PlayerController.Instance.PlayerAttack.DisableAttack = true;
            AudioKit.PlaySound("fx_skill_c2");
            // apply force
            Vector2 movementVector = PlayerInputs.Instance.GetMovementVectorNormalized();
            if (movementVector.magnitude == 0f)
            {
                movementVector = transform.up;
            }
            Quaternion rotation = Quaternion.Euler(0, PlayerController.Instance.transform.eulerAngles.y, 0);
            Vector3 rotatedMovementVector = rotation * new Vector3(movementVector.x, 0, movementVector.y);
            Vector3 dodgeDir = rotatedMovementVector.normalized + Vector3.up * 0.1f;
            PlayerController.Instance.SelfRigidbody.AddForce(dodgeDir * DodgeForce, ForceMode.Impulse);
            PlayerController.Instance.PlayerStats.IsInvincible = true;
            Physics.IgnoreLayerCollision(3, 10, true);
            return true;
        }


        protected override void HandleSkillEnd()
        {
            base.HandleSkillEnd();

            Weapon currentWeapon =
                PlayerController.Instance.PlayerAttack.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                currentWeapon.GrantGuaranteedCriticalHit();
                _weaponWithDodgeBonus = currentWeapon;
            }

            ResetDodgeState();
        }

        public override void CancelForLevelTransition()
        {
            bool wasUsingSkill = IsUsingSkill;
            IsUsingSkill = false;
            _skillDurationDelta = 0f;

            if (wasUsingSkill)
            {
                _skillCooldownDelta = _skillCooldown;
                SkillCdNormalized.Value = 0f;
                ResetDodgeState();
            }

            if (_weaponWithDodgeBonus != null)
            {
                _weaponWithDodgeBonus.ClearGuaranteedCriticalHit();
                _weaponWithDodgeBonus = null;
            }
        }

        private void ResetDodgeState()
        {
            PlayerController.Instance.PlayerAnimation.ToggleDodge(false);
            PlayerController.Instance.PlayerAttack.DisableAttack = false;
            PlayerController.Instance.PlayerStats.IsInvincible = false;
            Physics.IgnoreLayerCollision(3, 10, false);
        }
    }

}
