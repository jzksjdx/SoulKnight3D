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

        

        protected override void Start()
        {
            base.Start();
            _animator = GetComponent<Animator>();
            _animIdOpen = Animator.StringToHash("Open");

            string chestLabelText = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese ? "宝箱" : "Chest";
            Label.SetLabelText(chestLabelText, WeaponData.WeaponRarity.White);
        }

        public override void Interact()
        {
            InteractCollider.enabled = false;
            AudioKit.PlaySound("fx_chest_open");
            _animator.SetTrigger(_animIdOpen);
        }

       
    }
}
