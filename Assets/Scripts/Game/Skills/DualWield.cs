using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class DualWield : Skill
    {
        // double gun skill variables
        public Transform LeftHandWeaponPos;
        public ParticleSystem SkillEffect;

        private GameObject _rightHandWeaponObj;
        private Weapon _rightHandWeapon;
        private GameObject _leftHandWeaponObj;
        private GameObject _leftHandSourceWeaponObj;
        private Gun _leftHandWeapon;
        private bool _isAttacking = false;


        protected override void Start()
        {
            base.Start();
            StopSkillEffect();

            // Left hand Attack
            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                _isAttacking = isAttacking;
                if (isAttacking && IsUsingSkill)
                {
                    SynchronizeLeftHandAttackDelay();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            
            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponData, weaponObject) =>
            {
                HandleRightHandWeaponChange(weaponData, weaponObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }


        protected override void UsingSkillOnUpdate()
        {
            base.UsingSkillOnUpdate();
            if (!IsUsingSkill) { return; }

            // Left hand attack
            if (_isAttacking)
            {
                LeftHandAttack();
            }
        }

        protected override void HandleSkillEnd()
        {
            base.HandleSkillEnd();
            StopSkillEffect();

            Weapon currentWeapon = PlayerController.Instance.PlayerAttack.GetCurrentWeapon();
            if (currentWeapon == null)
            {
                return;
            }

            if (currentWeapon.InGameData.Animation == WeaponData.WeaponAnimation.Bow)
            {
                // for bow
                (currentWeapon as ChargeWeapon).SetChargeSpeed();
            }
            else if (currentWeapon.InGameData.Animation == WeaponData.WeaponAnimation.Melee)
            {
                (currentWeapon as Sword).SetChargeSpeed();
            }
            else
            {
                if (_leftHandWeaponObj)
                {
                    _leftHandWeaponObj.Hide();
                }

                if (_rightHandWeapon != null)
                {
                    ChangePlayerAnimation(_rightHandWeapon.InGameData.Animation);
                }
            }
        }

        public override void CancelForLevelTransition()
        {
            base.CancelForLevelTransition();
            _isAttacking = false;
            StopSkillEffect();
        }

        public override bool UseSkill()
        {
            if (_rightHandWeapon == null)
            {
                _rightHandWeapon = PlayerController.Instance.PlayerAttack.GetCurrentWeapon();
                _rightHandWeaponObj = _rightHandWeapon != null ? _rightHandWeapon.gameObject : null;
            }
            if (_rightHandWeapon == null || !base.UseSkill()) { return false; }

            // skill
            if (_rightHandWeapon.InGameData.Animation == WeaponData.WeaponAnimation.Bow)
            {
                // Double Attack Speed
                _rightHandWeapon.GetComponent<ChargeWeapon>().SetChargeSpeed(2);
            } else if (_rightHandWeapon.InGameData.Animation == WeaponData.WeaponAnimation.Melee)
            {
                _rightHandWeapon.GetComponent<Sword>().SetChargeSpeed(2);
            }
            else
            {
                EnsureLeftHandWeapon();
                SynchronizeLeftHandAttackDelay();
                ChangePlayerAnimation(WeaponData.WeaponAnimation.DoubleGun);
            }


            SkillEffect.Show();
            SkillEffect.Play();
            AudioKit.PlaySound("fx_skill_c1");
            return true;
        }

        private void LateUpdate()
        {
            if (IsUsingSkill)
            {
                AimLeftHandWeaponAtTarget();
            }
        }

        private void StopSkillEffect()
        {
            if (SkillEffect == null)
            {
                return;
            }

            SkillEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SkillEffect.gameObject.SetActive(false);
        }

        private void EnsureLeftHandWeapon()
        {
            if (_leftHandWeaponObj != null && _leftHandSourceWeaponObj == _rightHandWeaponObj)
            {
                _leftHandWeaponObj.transform.localPosition = Vector3.zero;
                _leftHandWeaponObj.transform.localRotation = Quaternion.identity;
                _leftHandWeaponObj.Show();
                return;
            }

            DestroyLeftHandWeapon();

            _leftHandWeaponObj = Instantiate(_rightHandWeaponObj, LeftHandWeaponPos, false);
            _leftHandWeaponObj.transform.localPosition = Vector3.zero;
            _leftHandWeaponObj.transform.localRotation = Quaternion.identity;
            _leftHandWeapon = _leftHandWeaponObj.GetComponent<Gun>();
            _leftHandSourceWeaponObj = _rightHandWeaponObj;

            if (_leftHandWeapon == null)
            {
                DestroyLeftHandWeapon();
                return;
            }

            Gun leftHandWeapon = _leftHandWeapon;
            leftHandWeapon.OnWeaponFired.Register(() =>
            {
                PlayerController.Instance.PlayerStats.Energy.Value -= leftHandWeapon.InGameData.EnergyCost;
            }).UnRegisterWhenDisabled(_leftHandWeaponObj);
        }

        private void LeftHandAttack()
        {
            if (!_leftHandWeapon) { return; }
            if (_leftHandWeapon.InGameData.EnergyCost > PlayerController.Instance.PlayerStats.Energy.Value) { return; }

            AimLeftHandWeaponAtTarget();
            _leftHandWeapon.Attack();
        }

        private void AimLeftHandWeaponAtTarget()
        {
            if (_leftHandWeapon == null || PlayerController.Instance == null) { return; }

            Transform aimTarget = PlayerController.Instance.PlayerAttack.target;
            if (aimTarget != null)
            {
                _leftHandWeapon.AimAt(aimTarget.position);
            }
        }

        private void SynchronizeLeftHandAttackDelay()
        {
            if (_leftHandWeapon == null || _rightHandWeapon == null) { return; }

            float halfCooldown = Mathf.Max(0f, _leftHandWeapon.InGameData.Cooldown * 0.5f);
            _leftHandWeapon.SetAttackDelay(_rightHandWeapon.GetRemainingCooldown() + halfCooldown);
        }

        private void DestroyLeftHandWeapon()
        {
            if (_leftHandWeaponObj != null)
            {
                Destroy(_leftHandWeaponObj);
            }

            _leftHandWeaponObj = null;
            _leftHandSourceWeaponObj = null;
            _leftHandWeapon = null;
        }

        public void HandleRightHandWeaponChange(WeaponData newWeaponData, GameObject newWeapon)
        {
            // handle previous weapon bow
            if (_rightHandWeapon && _rightHandWeapon.InGameData.Animation == WeaponData.WeaponAnimation.Bow)
            {
                _rightHandWeapon.GetComponent<ChargeWeapon>().SetChargeSpeed(1);
            }

            _rightHandWeaponObj = newWeapon;
            _rightHandWeapon = _rightHandWeaponObj.GetComponent<Weapon>();
            if (!IsUsingSkill) { return; }
            // handle bow
            if (newWeaponData.Animation == WeaponData.WeaponAnimation.Bow)
            {
                DestroyLeftHandWeapon();
                ChangePlayerAnimation(WeaponData.WeaponAnimation.Bow);
                newWeapon.GetComponent<ChargeWeapon>().SetChargeSpeed(2);
                return;
            }
            else if (newWeaponData.Animation == WeaponData.WeaponAnimation.Melee)
            {
                DestroyLeftHandWeapon();
                ChangePlayerAnimation(WeaponData.WeaponAnimation.Melee);
                newWeapon.GetComponent<Sword>().SetChargeSpeed(2);
                return;
            }

            // handle weapons other than bows or swords
            EnsureLeftHandWeapon();
            SynchronizeLeftHandAttackDelay();
            ChangePlayerAnimation(WeaponData.WeaponAnimation.DoubleGun);
        }

        private void ChangePlayerAnimation(WeaponData.WeaponAnimation animation)
        {
            PlayerController.Instance.PlayerAnimation.SwitchWeaponAnimation(animation);
        }
    }

}
