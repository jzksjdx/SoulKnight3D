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

        [Header("Reward Selection")]
        public RewardSelectionMode SelectionMode = RewardSelectionMode.Weighted;
        public RoomRewardSettings RoomReward = new RoomRewardSettings();

        public enum RewardSelectionMode
        {
            Weighted,
            OriginalRoomReward
        }

        public enum ChestRewardType
        {
            EnergyAndCoin, Weapon, Potion
        }

        [Serializable]
        public class RoomRewardSettings
        {
            [Tooltip("Rolls below this value out of 100 produce a potion.")]
            [Range(0, 100)] public int PotionUpperBound = 10;
            [Tooltip("Standard rolls below this value produce a weapon after the potion range.")]
            [Range(0, 100)] public int WeaponUpperBound = 16;
            [Tooltip("Weapon upper bound during the original game's new-player assistance period.")]
            [Range(0, 100)] public int BeginnerWeaponUpperBound = 30;
            [Tooltip("Number of newly started runs that receive the new-player weapon bonus.")]
            [Min(0)] public int BeginnerRunCount = 8;
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

