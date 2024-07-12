using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Skill : MonoBehaviour
    {
        public BindableProperty<float> SkillCdNormalized = new BindableProperty<float>();
        public EasyEvent<WeaponData.WeaponCategory> OnKnightSkillActivated = new EasyEvent<WeaponData.WeaponCategory>();

        // double gun skill variables
        public Transform LeftHandWeaponPos;
        public ParticleSystem SkillEffect;

        private GameObject _rightHandWeapon;
        private GameObject _leftHandWeaponObj;
        private Gun _leftHandWeapon;
        private bool _isAttacking = false;

        // timeout deltatime
        private float _skillCooldownDelta;
        private float _skillCooldown = 10f;
        private float _skillDurationDelta;
        private float _skillDuration = 5f;

        public bool IsUsingSkill = false;

        void Start()
        {
            _skillCooldownDelta = _skillCooldown;

            PlayerInputs.Instance.OnSkillPerformed.Register(() =>
            {
                if (_skillDurationDelta > 0 || _skillCooldownDelta > 0) { return; } 
                UseSkill();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            // Left hand Attack
            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                _isAttacking = isAttacking;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsUsingSkill && _skillCooldownDelta >= 0)
            {
                _skillCooldownDelta -= Time.deltaTime;
                SkillCdNormalized.Value = 1 - _skillCooldownDelta / _skillCooldown;
            }
            else if (IsUsingSkill)
            {
                if (_skillDurationDelta >= 0)
                {
                    _skillDurationDelta -= Time.deltaTime;
                    SkillCdNormalized.Value = _skillDurationDelta / _skillDuration;
                }
                else
                {
                    // skill ends
                    IsUsingSkill = false;
                    _skillCooldownDelta = _skillCooldown;

                    _leftHandWeaponObj.Hide();
                    OnKnightSkillActivated.Trigger(_rightHandWeapon.GetComponent<Gun>().Data.Category);
                    SkillEffect.Stop();
                }

                // Left hand attack
                if (_isAttacking)
                {
                    LeftHandAttack();
                }
            }
        }

        public void UseSkill()
        {
            if (_skillCooldownDelta > 0f) { return; }
            _skillDurationDelta = _skillDuration;
            IsUsingSkill = true;
            // skill
            if (_leftHandWeaponObj)
            {
                if (_leftHandWeaponObj != _rightHandWeapon)
                {
                    Destroy(_leftHandWeaponObj);
                    AddNewWeaponToLeftHand();
                } else
                {
                    _leftHandWeaponObj.Show();
                }
            } else
            {
                AddNewWeaponToLeftHand();
            }
            
            _leftHandWeaponObj.transform.localPosition = Vector3.zero;
            _leftHandWeapon = _leftHandWeaponObj.GetComponent<Gun>();
            OnKnightSkillActivated.Trigger(WeaponData.WeaponCategory.DoubleGun);
            SkillEffect.Show();
            SkillEffect.Play();
            AudioKit.PlaySound("fx_skill_c1");
        }

        private void AddNewWeaponToLeftHand()
        {
            _leftHandWeaponObj = Instantiate(_rightHandWeapon, LeftHandWeaponPos);
            Weapon weapon = _leftHandWeaponObj.GetComponent<Weapon>();
            weapon.OnWeaponFired.Register(() =>
            {
                PlayerController.Instance.PlayerStats.Energy.Value -= weapon.Data.EnergyCost;
            }).UnRegisterWhenDisabled(_leftHandWeaponObj);
        }

        private void LeftHandAttack()
        {
            if (_leftHandWeapon.Data.EnergyCost >= PlayerController.Instance.PlayerStats.Energy.Value) { return; }
            if (_leftHandWeapon)
            {
                _leftHandWeapon.Attack();
            }
        }

        public void OnSwitchRightHandWeapon(GameObject newWeapon)
        {
            _rightHandWeapon = newWeapon;
            if (IsUsingSkill)
            {
                Destroy(_leftHandWeaponObj);
                _leftHandWeaponObj = Instantiate(_rightHandWeapon, LeftHandWeaponPos);
                _leftHandWeaponObj.transform.localPosition = Vector3.zero;
                _leftHandWeapon = _leftHandWeaponObj.GetComponent<Gun>();
                OnKnightSkillActivated.Trigger(WeaponData.WeaponCategory.DoubleGun);
            }
        }
    }

}