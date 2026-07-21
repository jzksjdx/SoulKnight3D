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
        [Tooltip("-1 means this weapon is excluded from normal chest weapon pools.")]
        public int DropLevel = 1;
        [Tooltip("Extra weapon pool levels this weapon can appear in.")]
        public List<int> ExtraDropLevels = new List<int>();

        public GameObject WeaponPrefab;
        public GameObject PickUpPrefab;

        public bool CanDropAtLevel(int level)
        {
            if (DropLevel == level)
            {
                return true;
            }

            for (int i = 0; i < ExtraDropLevels.Count; i++)
            {
                if (ExtraDropLevels[i] == level)
                {
                    return true;
                }
            }

            return false;
        }

        public enum WeaponCategory
        {
            Pistol, Rifle, Shotgun, DoubleGun, Launcher, Lazer, Bow, Melee, Miscellaneous
        }

        public enum WeaponAnimation
        {
            Pistol, Rifle, DoubleGun, Melee, Bow, Launcher, MachineGun
        }

        public enum WeaponRarity
        {
            White, Green, Blue, Purple, Orange, Red, Magenta
        }
    }

}
