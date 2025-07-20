using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using QFramework;
using UnityEngine.Animations.Rigging;

namespace SoulKnight3D
{
    public class PlayerAnimation : MonoBehaviour
    {
        public Animator SelfAnimator;
        public MultiAimConstraint SpineAim;
        public MultiAimConstraint RightHandAim;
        public Transform RightHand;
        public MultiAimConstraint LeftHandAim;
        public MultiRotationConstraint SpineRotation;

        public BareHand BareHand;
        
        // animation IDs
        private int _animIdSpeed;
        private int _animIdMoveX;
        private int _animIdMoveY;
        private int _animIdGrounded;
        private int _animIdJump;
        private int _animIdFreeFall;

        private int _animIdDie;
        private int _animIdDodge;

        private int _animIdPistol;
        private int _animIdDoubleGun;
        private int _animIdRifle;
        private int _animIdBow;

        private int _animIdBowDraw;
        private int _animIdBowShoot;
        private int _animIDChargeSpeed;
        private float _bowSpineAimYNormal = 40f;
        private float _bowSpineAimYDraw = 80f;

        private int _animIdSword;
        private int _animIdMeleeNextMove;

        private int _animIdLauncher;

        private int _animIdBareHand;

        // spine offset

        private Vector3 _spineAimPistol = new Vector3(21.8f, 18f, 0);
        private Vector3 _spineAimRifle = new Vector3(21.8f, 46.5f, 0);
        private Vector3 _spineAimDoubleGun = new Vector3(21.8f, 18f, 0);
        private Vector3 _spineAimBow = new Vector3(13f, 40f, 4f);
        private Vector3 _spineAimSword = new Vector3(13f, 46.7f, 5.56f);
        private Vector3 _spineAimLauncher = new Vector3(21.8f, 18f, -10f);

        private void Awake()
        {
            _spineAimBow = new Vector3(13f, 40f, 4f);

            _animIdSpeed = Animator.StringToHash("Speed");
            _animIdMoveX = Animator.StringToHash("MoveX");
            _animIdMoveY = Animator.StringToHash("MoveY");
            _animIdGrounded = Animator.StringToHash("Grounded");
            _animIdJump = Animator.StringToHash("Jump");
            _animIdFreeFall = Animator.StringToHash("FreeFall");

            _animIdDie = Animator.StringToHash("Die");
            _animIdDodge = Animator.StringToHash("Skill_Dodge");

            _animIdPistol = Animator.StringToHash("Weapon_Pistol");
            _animIdRifle = Animator.StringToHash("Weapon_Rifle");
            _animIdDoubleGun = Animator.StringToHash("Weapon_DoubleGun");
            _animIdBow = Animator.StringToHash("Weapon_Bow");
            _animIdBowDraw = Animator.StringToHash("Bow_Draw");
            _animIdBowShoot = Animator.StringToHash("Bow_Shoot");
            _animIDChargeSpeed = Animator.StringToHash("ChargeSpeed");
            _animIdSword = Animator.StringToHash("Weapon_Sword");
            _animIdMeleeNextMove = Animator.StringToHash("Melee_NextMove");
            _animIdLauncher = Animator.StringToHash("Weapon_Launcher");
            _animIdBareHand = Animator.StringToHash("Weapon_BareHand");
        }

        void Start()
        {
           
        }

        public void SetAnimatorJump()
        {
            SelfAnimator.SetTrigger(_animIdJump);
        }

        public void SetAnimatorGrounded(bool isGrounded)
        {
            SelfAnimator.SetBool(_animIdGrounded, isGrounded);
        }

        public void SetAnimatorFreeFall(bool isFreeFall)
        {
            SelfAnimator.SetBool(_animIdFreeFall, isFreeFall);
        }

        public void SetAnimationSpeed(float speedNormalized, float speedX, float speedY)
        {
            SelfAnimator.SetFloat(_animIdSpeed, speedNormalized);
            SelfAnimator.SetFloat(_animIdMoveX, speedX);
            SelfAnimator.SetFloat(_animIdMoveY, speedY);
        }

        public void SetAnimationDie()
        {
            SelfAnimator.SetTrigger(_animIdDie);
            SelfAnimator.SetLayerWeight(1, 0);
            SpineAim.weight = 0;
            RightHandAim.weight = 0f;
            SpineRotation.weight = 0f;
        }

        private bool _isDrawingArrow = false;

        public void SetChargeSpeed(float chargeSpeed = 1)
        {
            SelfAnimator.SetFloat(_animIDChargeSpeed, chargeSpeed);
        }

        public void SetAnimationBowDraw(float chargeTime = 1)
        {
            SelfAnimator.SetFloat(_animIDChargeSpeed, 1 / chargeTime);
            SelfAnimator.SetTrigger(_animIdBowDraw);
            SpineAim.data.offset = SpineAim.data.offset.Y(_bowSpineAimYDraw);
            SpineRotation.weight = 1f;
            RightHandAim.weight = 1f;
            _isDrawingArrow = true;
        }

        public void SetAnimationBowShoot()
        {
            SelfAnimator.SetTrigger(_animIdBowShoot);
            RightHandAim.weight = 0f;
            _isDrawingArrow = false;
            //SetBowAnimSpineOffset();
        }

        private void SetBowAnimSpineOffset()
        {
            ActionKit.Lerp(_bowSpineAimYDraw, _bowSpineAimYNormal, 0.25f, onLerp: (value) =>
            {
                if (_isDrawingArrow) { return; }
                SpineAim.data.offset = SpineAim.data.offset.Y(value);
                float percent = 1 -  (value - _bowSpineAimYDraw) / (_bowSpineAimYNormal - _bowSpineAimYDraw);
                SpineRotation.weight = percent;
            }).Start(this);
        }

        public void SetAnimationMeleeNextMove(bool shouldMove)
        {
            SelfAnimator.SetBool(_animIdMeleeNextMove, shouldMove);
        }

        public void SwitchWeaponAnimation(WeaponData.WeaponAnimation weaponAnimation)
        {
            LeftHandAim.weight = 0;
            SpineRotation.weight = 0f;
            RightHandAim.weight = 1f;
            SpineAim.weight = 1f;
            SelfAnimator.SetLayerWeight(2, 0);
            SelfAnimator.SetLayerWeight(1, 1);
            switch (weaponAnimation)
            {
                case WeaponData.WeaponAnimation.Pistol:
                    SelfAnimator.SetTrigger(_animIdPistol);
                    SpineAim.data.offset = _spineAimPistol;
                    break;
                case WeaponData.WeaponAnimation.Rifle:
                    SelfAnimator.SetTrigger(_animIdRifle);
                    SpineAim.data.offset = _spineAimRifle;
                    break;
                case WeaponData.WeaponAnimation.Bow:
                    SelfAnimator.SetTrigger(_animIdBow);
                    SpineAim.data.offset = _spineAimBow;
                    RightHandAim.weight = 0f;
                    break;
                case WeaponData.WeaponAnimation.DoubleGun:
                    SelfAnimator.SetTrigger(_animIdDoubleGun);
                    if (PlayerController.Instance.PlayerAttack.GetCurrentWeapon().InGameData.Animation == WeaponData.WeaponAnimation.Launcher)
                    {
                        SpineAim.data.offset = new Vector3(-5.3f, -4, 0);
                    } else
                    {
                        SpineAim.data.offset = _spineAimDoubleGun;
                    }
                    LeftHandAim.weight = 1;
                    break;
                case WeaponData.WeaponAnimation.Melee:
                    SelfAnimator.SetTrigger(_animIdSword);
                    SpineAim.data.offset = _spineAimSword;
                    RightHandAim.weight = 0f;
                    SpineAim.weight = 0f;
                    SelfAnimator.SetLayerWeight(2, 1);
                    break;
                case WeaponData.WeaponAnimation.Launcher:
                    SelfAnimator.SetTrigger(_animIdLauncher);
                    SpineAim.data.offset = _spineAimLauncher;
                    break;
                default:
                    SelfAnimator.SetTrigger(_animIdPistol);
                    SpineAim.data.offset = _spineAimPistol;
                    break;
            }
        }

        public void ToggleBareHandAnimation(bool isBareHand)
        {
            if (isBareHand)
            {
                SelfAnimator.SetTrigger(_animIdBareHand);
                SelfAnimator.SetLayerWeight(2, 1);
                SpineAim.data.offset = Vector3.zero;
                RightHandAim.weight = 0f;
                SpineAim.weight = 0f;
                SpineRotation.weight = 0f;
                SetAnimationMeleeNextMove(true);
               
            } else
            {
                SwitchWeaponAnimation(PlayerController.Instance.PlayerAttack.GetCurrentWeapon().InGameData.Animation);
            }
        }

        private void HandleBareHandAnimAtk(int isRight) // 0 = Right hand, 1 = left hand
        {
            BareHand.AttackFromAniamtion(isRight == 0 ? true : false);
        }

        public void ToggleDodge(bool isDodging)
        {
            if (isDodging)
            {
                SpineAim.data.offset = Vector3.zero;
                RightHandAim.weight = 0f;
                SpineAim.weight = 0f;
                SpineRotation.weight = 0f;
                SelfAnimator.SetLayerWeight(1, 0);
                SelfAnimator.SetLayerWeight(2, 0);
                SelfAnimator.SetTrigger(_animIdDodge);

            } else
            {
                SwitchWeaponAnimation(PlayerController.Instance.PlayerAttack.GetCurrentWeapon().InGameData.Animation);
            }
        }
    }
}