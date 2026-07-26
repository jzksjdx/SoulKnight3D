using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "WeaponDropPoolSO", menuName = "ScriptableObject/Weapon Drop Pool")]
    public class WeaponDropPoolSO : ScriptableObject
    {
        public List<WeaponDropPoolLevel> Levels = new List<WeaponDropPoolLevel>();

        public GameObject GetRandomPickupPrefab(int level, System.Random random)
        {
            WeaponDropPoolLevel levelPool = FindLevelPool(level);
            if (levelPool == null)
            {
                return null;
            }

            return levelPool.GetRandomPickupPrefab(random);
        }

        public GameObject GetRandomPickupPrefabAtOrBelow(int level, System.Random random)
        {
            WeaponDropPoolLevel levelPool = FindLevelPool(level);
            if (levelPool == null)
            {
                levelPool = FindHighestLevelPoolAtOrBelow(level);
            }

            return levelPool != null ? levelPool.GetRandomPickupPrefab(random) : null;
        }

        public bool HasLevel(int level)
        {
            return FindLevelPool(level) != null;
        }

        private WeaponDropPoolLevel FindLevelPool(int level)
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i] != null && Levels[i].Level == level && Levels[i].HasValidEntries())
                {
                    return Levels[i];
                }
            }

            return null;
        }

        private WeaponDropPoolLevel FindHighestLevelPoolAtOrBelow(int level)
        {
            WeaponDropPoolLevel bestMatch = null;
            for (int i = 0; i < Levels.Count; i++)
            {
                WeaponDropPoolLevel candidate = Levels[i];
                if (candidate == null || candidate.Level > level || !candidate.HasValidEntries())
                {
                    continue;
                }

                if (bestMatch == null || candidate.Level > bestMatch.Level)
                {
                    bestMatch = candidate;
                }
            }

            return bestMatch;
        }
    }

    [Serializable]
    public class WeaponDropPoolLevel
    {
        public int Level;
        public List<WeaponDropPoolEntry> Entries = new List<WeaponDropPoolEntry>();

        public bool HasValidEntries()
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i] != null && Entries[i].IsAvailable)
                {
                    return true;
                }
            }

            return false;
        }

        public GameObject GetRandomPickupPrefab(System.Random random)
        {
            float totalWeight = 0f;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i] != null && Entries[i].IsAvailable)
                {
                    totalWeight += Mathf.Max(0f, Entries[i].Weight);
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = (float)(random.NextDouble() * totalWeight);
            float currentWeight = 0f;
            WeaponDropPoolEntry fallbackEntry = null;
            for (int i = 0; i < Entries.Count; i++)
            {
                WeaponDropPoolEntry entry = Entries[i];
                if (entry == null || !entry.IsAvailable)
                {
                    continue;
                }

                fallbackEntry = entry;
                currentWeight += Mathf.Max(0f, entry.Weight);
                if (roll < currentWeight)
                {
                    return entry.GetPickupPrefab();
                }
            }

            return fallbackEntry?.GetPickupPrefab();
        }
    }

    [Serializable]
    public class WeaponDropPoolEntry
    {
        public WeaponData Weapon;
        public GameObject PickupOverride;
        public float Weight = 1f;
        public bool Enabled = true;

        public bool IsAvailable => Enabled && GetPickupPrefab() != null;

        public GameObject GetPickupPrefab()
        {
            if (PickupOverride != null)
            {
                return PickupOverride;
            }

            return Weapon != null ? Weapon.PickUpPrefab : null;
        }
    }
}
