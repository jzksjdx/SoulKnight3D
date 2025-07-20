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
        private Weapon _leftHandWeapon;
        private bool _isAttacking = false;


        protected override void Start()
        {
            base.Start();
            // Left hand Attack
            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                _isAttacking = isAttacking;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            
            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponData, weaponObject) =>
            {
                HandleRightHandWeaponChange(weaponData, weaponObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }


        protected override void UsingSkillOnUpdate()
        {
            base.UsingSkillOnUpdate();

            // Left hand attack
            if (_isAttacking)
            {
                LeftHandAttack();
            }
        }

        protected override void HandleSkillEnd()
        {
            base.HandleSkillEnd();
            SkillEffect.Stop();

            Weapon currentWeapon = PlayerController.Instance.PlayerAttack.GetCurrentWeapon();

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

                ChangePlayerAnimation(_rightHandWeaponObj.GetComponent<Gun>().InGameData.Animation);
            }
        }

        public override void UseSkill()
        {
            base.UseSkill();
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
                
                // Double Weapon
                if (_leftHandWeaponObj)
                {
                    if (_leftHandWeaponObj != _rightHandWeaponObj)
                    {
                        Destroy(_leftHandWeaponObj);
                        AddNewWeaponToLeftHand();
                    }
                    else
                    {
                        _leftHandWeaponObj.Show();
                    }
                }
                else
                {
                    AddNewWeaponToLeftHand();
                }
                _leftHandWeaponObj.transform.localPosition = Vector3.zero;
                _leftHandWeapon = _leftHandWeaponObj.GetComponent<Gun>();
                ChangePlayerAnimation(WeaponData.WeaponAnimation.DoubleGun);
            }


            SkillEffect.Show();
            SkillEffect.Play();
            AudioKit.PlaySound("fx_skill_c1");
        }

        private void AddNewWeaponToLeftHand()
        {
            _leftHandWeaponObj = Instantiate(_rightHandWeaponObj, LeftHandWeaponPos);
            Weapon weapon = _leftHandWeaponObj.GetComponent<Weapon>();
            weapon.OnWeaponFired.Register(() =>
            {
                PlayerController.Instance.PlayerStats.Energy.Value -= weapon.InGameData.EnergyCost;
            }).UnRegisterWhenDisabled(_leftHandWeaponObj);
        }

        private void LeftHandAttack()
        {
            if (!_leftHandWeapon) { return; }
            if (_leftHandWeapon.InGameData.EnergyCost >= PlayerController.Instance.PlayerStats.Energy.Value) { return; }
            if (_leftHandWeapon.InGameData)
            {
                _leftHandWeapon.Attack();
            }
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
                Destroy(_leftHandWeaponObj);
                _leftHandWeapon = null;
                ChangePlayerAnimation(WeaponData.WeaponAnimation.Bow);
                newWeapon.GetComponent<ChargeWeapon>().SetChargeSpeed(2);
                return;
            }
            else if (newWeaponData.Animation == WeaponData.WeaponAnimation.Melee)
            {
                Destroy(_leftHandWeaponObj);
                _leftHandWeapon = null;
                ChangePlayerAnimation(WeaponData.WeaponAnimation.Melee);
                newWeapon.GetComponent<Sword>().SetChargeSpeed(2);
                return;
            }

            // handle weapons other than bows or swords
            Destroy(_leftHandWeaponObj);
            _leftHandWeaponObj = Instantiate(_rightHandWeaponObj, LeftHandWeaponPos);
            _leftHandWeaponObj.transform.localPosition = Vector3.zero;
            _leftHandWeapon = _leftHandWeaponObj.GetComponent<Gun>();
            ChangePlayerAnimation(WeaponData.WeaponAnimation.DoubleGun);
        }

        private void ChangePlayerAnimation(WeaponData.WeaponAnimation animation)
        {
            PlayerController.Instance.PlayerAnimation.SwitchWeaponAnimation(animation);
        }
    }

}