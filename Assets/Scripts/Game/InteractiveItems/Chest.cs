using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Chest : InteractiveItem
    {
        private Animator _animator;

        private int _animIdOpen;

        protected virtual void Start()
        {
            _animator = GetComponent<Animator>();
            _animIdOpen = Animator.StringToHash("Open");

            Label.SetLabelText("宝箱", WeaponData.WeaponRarity.White);
        }

        public override void Interact()
        {
            InteractCollider.enabled = false;
            AudioKit.PlaySound("fx_chest_open");
            _animator.SetTrigger(_animIdOpen);
        }
    }
}
