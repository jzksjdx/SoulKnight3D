using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;

namespace SoulKnight3D
{
    public class ItemPlant : InteractiveItem, IItem
    {
        public GameObject Weapon;
        public Transform WeaponModel;

        private WeaponData _weaponData;

        public override void Interact()
        {
            PlayerAttack playerAtk = PlayerController.Instance.PlayerAttack;
            if (!playerAtk) { return; }

            //GameObject newWeapon = Instantiate(Weapon, playerAtk.WeaponPoint.position, Quaternion.identity, playerAtk.WeaponPoint);
            //newWeapon.transform.rotation = new Quaternion(0, 0, 0, 0);
            //playerAtk.TakeNewWeapon(newWeapon);
            //Destroy(gameObject);
        }

        private void Awake()
        {
            _weaponData = Weapon.GetComponent<Weapon>().Data;
            Debug.Log(_weaponData.name);
            Label.SetLabelText(_weaponData.NameCN, _weaponData.Rarity);
        }

        private void Start()
        {
           
        }

        public string GetKey => _weaponData? _weaponData.Name : Weapon.GetComponent<Weapon>().Data.Name;

        public string GetName => _weaponData ? _weaponData.Name : Weapon.GetComponent<Weapon>().Data.Name;
        public Sprite GetIcon => _weaponData ? _weaponData.Sprite : Weapon.GetComponent<Weapon>().Data.Sprite;
    }

}