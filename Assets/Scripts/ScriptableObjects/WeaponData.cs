using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string Name;
        public string NameCN;
        public Sprite Sprite;
        public WeaponCategory Category;
        public WeaponRarity Rarity;
        public WeaponAnimation Animation;
        public int Damage;
        public int EnergyCost;
        public int CritChance;
        public int Inaccuracy;
        public int Price;
        public float Cooldown;

        public GameObject WeaponPrefab;
        public GameObject PickUpPrefab;

        public enum WeaponCategory
        {
            Pistol, Rifle, Shotgun, DoubleGun, Launcher, Lazer, Bow, Melee, Miscellaneous
        }

        public enum WeaponAnimation
        {
            Pistol, Rifle, DoubleGun, Melee, Bow, Launcher
        }

        public enum WeaponRarity
        {
            White, Green, Blue, Purple, Orange, Red, Magenta
        }
    }

}
