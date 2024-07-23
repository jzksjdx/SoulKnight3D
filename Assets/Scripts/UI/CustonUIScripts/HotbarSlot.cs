using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using UnityEngine.EventSystems;

namespace SoulKnight3D
{
    public class HotbarSlot : UISlot
    {
        public int HotbarNumber;

        private void Start()
        {
            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponIndex) =>
            {
                if (weaponIndex + 1 == HotbarNumber)
                {
                    transform.localScale = Vector3.one * 1.2f;
                    AudioKit.PlaySound("seedlift");
                } else
                {
                    transform.localScale = Vector3.one;
                }
            }).UnRegisterWhenGameObjectDestroyed(this);
        }

        public override void UpdateView()
        {
            base.UpdateView();

            PlayerAttack playerAttack = PlayerController.Instance.PlayerAttack;
            if (Data.Item)
            {
                playerAttack.Weapons[HotbarNumber - 1] = Data.Item.Weapon;
                AudioKit.PlaySound("seedlift");
            } else
            {
                playerAttack.Weapons[HotbarNumber - 1] = null;
            }

            if (playerAttack.CurrentWeaponIndex == HotbarNumber - 1)
            {
                playerAttack.SwitchWeapon(HotbarNumber - 1);
            }
            

            // Handle Manual drag to hotbar
            if (Data.Item == null) { return; }

            if (Data.Item.State != ItemPlant.StorageState.Hotbar)
            {
                Data.Item.State = ItemPlant.StorageState.Hotbar;
                Data.Item.MoveToHotbar();
            }
        }
    }

}
