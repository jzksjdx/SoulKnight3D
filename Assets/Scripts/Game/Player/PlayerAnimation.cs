using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SoulKnight3D
{
    public class PlayerAnimation : MonoBehaviour
    {
        public Animator SelfAnimator;
        public UnityEngine.Animations.Rigging.MultiAimConstraint SpineAim;
        public UnityEngine.Animations.Rigging.MultiAimConstraint LeftHandAim;
        // animation IDs
        private int _animIDSpeed;
        private int _animIDMoveX;
        private int _animIDMoveY;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;

        private int _animIDDie;

        private int _animIdPistol;
        private int _animIdDoubleGun;
        private int _animIdRifle;

        private void Awake()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDMoveX = Animator.StringToHash("MoveX");
            _animIDMoveY = Animator.StringToHash("MoveY");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");

            _animIDDie = Animator.StringToHash("Die");

            _animIdPistol = Animator.StringToHash("Weapon_Pistol");
            _animIdRifle = Animator.StringToHash("Weapon_Rifle");
            _animIdDoubleGun = Animator.StringToHash("Weapon_DoubleGun");
        }
        void Start()
        {
           
        }

        public void SetAnimatorJump(bool isJumping)
        {
            SelfAnimator.SetBool(_animIDJump, isJumping);
        }

        public void SetAnimatorGrounded(bool isGrounded)
        {
            SelfAnimator.SetBool(_animIDGrounded, isGrounded);
        }

        public void SetAnimatorFreeFall(bool isFreeFall)
        {
            SelfAnimator.SetBool(_animIDFreeFall, isFreeFall);
        }

        public void SetAnimationSpeed(float speedNormalized, float speedX, float speedY)
        {
            SelfAnimator.SetFloat(_animIDSpeed, speedNormalized);
            SelfAnimator.SetFloat(_animIDMoveX, speedX);
            SelfAnimator.SetFloat(_animIDMoveY, speedY);
        }

        public void SetAnimationDie()
        {
            SelfAnimator.SetTrigger(_animIDDie);
            SelfAnimator.SetLayerWeight(1, 0);
            SpineAim.weight = 0;
        }

        public void SwitchWeaponAnimation(WeaponData.WeaponCategory weaponCategory)
        {
            LeftHandAim.weight = 0;
            switch (weaponCategory)
            {
                case WeaponData.WeaponCategory.Pistol:
                    SelfAnimator.SetTrigger(_animIdPistol);
                    SpineAim.data.offset = WeaponAnimationOffsets.Pistol;
                    break;
                case WeaponData.WeaponCategory.Rifle:
                    SelfAnimator.SetTrigger(_animIdRifle);
                    SpineAim.data.offset = WeaponAnimationOffsets.Rifle;
                    break;
                case WeaponData.WeaponCategory.DoubleGun:
                    SelfAnimator.SetTrigger(_animIdDoubleGun);
                    SpineAim.data.offset = WeaponAnimationOffsets.DoubleGun;
                    LeftHandAim.weight = 1;
                    break;
                default:
                    break;
            }
        }
    }
}
