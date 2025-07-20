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
        }

        [Serializable]
        public class RewardItem
        {
            public GameObject Item;
            public float Rate;
        }
    }
}

