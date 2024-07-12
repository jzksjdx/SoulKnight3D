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
        public int Damage;
        public int EnergyCost;
        public int CritChance;
        public int Inaccuracy;
        public int Price;
        public float Cooldown;

        public enum WeaponCategory
        {
            Pistol, Rifle, Shotgun, DoubleGun, Rocket, Lazer, Bow, Melee
        }

        public enum WeaponRarity
        {
            White, Green, Blue, Purple, Orange, Red, Magenta
        }
    }

}
