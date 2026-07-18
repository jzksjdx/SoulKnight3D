using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "ChestRewardData", menuName = "ScriptableObject/ChestRewardData")]
    public class ChestRewardData : ScriptableObject
    {
        public List<RewardCategory> ChestRewards = new List<RewardCategory>();

        public enum ChestRewardType
        {
            EnergyAndCoin, Weapon, Potion
        }

        [Serializable]
        public class RewardCategory
        {
            public ChestRewardType Type;
            public float Rate;
            public List<RewardItem> Items = new List<RewardItem>();
            [Header("Weapon Pool")]
            public bool UseWeaponPool;
            public WeaponDropPoolSO WeaponPool;
            [Tooltip("-1 uses GameController.Level. Non-negative values use this fixed pool level.")]
            public int FixedWeaponPoolLevel = -1;
            public int WeaponPoolLevelOffset;
            public int MinWeaponPoolLevel = 0;
            public int MaxWeaponPoolLevel = 6;
        }

        [Serializable]
        public class RewardItem
        {
            public GameObject Item;
            public float Rate;
        }
    }
}

