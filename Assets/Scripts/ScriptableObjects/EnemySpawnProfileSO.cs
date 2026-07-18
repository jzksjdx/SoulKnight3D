using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "EnemySpawnProfileSO", menuName = "ScriptableObject/Enemy Spawn Profile")]
    public class EnemySpawnProfileSO : ScriptableObject
    {
        [Tooltip("Mixed into the run and room seeds so profiles produce independent results.")]
        public int SeedSalt = 1;

        [Range(0f, 1f)]
        public float EliteChance = 0.08f;

        [Min(1)]
        public int MaxEnemiesPerWave = 8;

        public List<EnemySpawnLevelSettings> LevelSettings = new List<EnemySpawnLevelSettings>();
        public List<EnemySpawnEntry> Enemies = new List<EnemySpawnEntry>();

        public EnemySpawnLevelSettings GetSettings(int level)
        {
            EnemySpawnLevelSettings closest = null;
            int closestDistance = int.MaxValue;

            foreach (EnemySpawnLevelSettings settings in LevelSettings)
            {
                if (settings == null)
                {
                    continue;
                }

                int distance = Mathf.Abs(settings.Level - level);
                if (distance < closestDistance)
                {
                    closest = settings;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private void OnValidate()
        {
            MaxEnemiesPerWave = Mathf.Max(1, MaxEnemiesPerWave);
            EliteChance = Mathf.Clamp01(EliteChance);

            foreach (EnemySpawnLevelSettings settings in LevelSettings)
            {
                settings?.Validate();
            }

            foreach (EnemySpawnEntry enemy in Enemies)
            {
                enemy?.Validate();
            }
        }
    }

    [Serializable]
    public class EnemySpawnLevelSettings
    {
        [Min(1)] public int Level = 1;
        [Min(1)] public int MinTotalPoints = 12;
        [Min(1)] public int MaxTotalPoints = 12;
        [Tooltip("Enemy wave sizes are spent against this capacity before another wave is created.")]
        [Min(1)] public int WaveCapacity = 8;

        public void Validate()
        {
            Level = Mathf.Max(1, Level);
            MinTotalPoints = Mathf.Max(1, MinTotalPoints);
            MaxTotalPoints = Mathf.Max(MinTotalPoints, MaxTotalPoints);
            WaveCapacity = Mathf.Max(1, WaveCapacity);
        }
    }

    [Serializable]
    public class EnemySpawnEntry
    {
        public GameObject EnemyPrefab;
        public GameObject ElitePrefab;

        [Min(1)] public int PointCost = 1;
        [Tooltip("How much room this enemy occupies in one wave.")]
        [Min(1)] public int WaveSize = 1;
        [Min(1)] public int Weight = 1;
        [Min(1)] public int MinLevel = 1;
        [Tooltip("Zero means that the enemy remains available for all later levels.")]
        [Min(0)] public int MaxLevel;
        [Tooltip("Zero means no archetype-specific limit.")]
        [Min(0)] public int MaxCountPerWave;
        [Min(0f)] public float EliteChanceMultiplier = 1f;

        public bool IsAvailableAtLevel(int level)
        {
            return EnemyPrefab != null && PointCost > 0 && Weight > 0 &&
                   level >= MinLevel && (MaxLevel == 0 || level <= MaxLevel);
        }

        public void Validate()
        {
            PointCost = Mathf.Max(1, PointCost);
            WaveSize = Mathf.Max(1, WaveSize);
            Weight = Mathf.Max(1, Weight);
            MinLevel = Mathf.Max(1, MinLevel);
            MaxLevel = Mathf.Max(0, MaxLevel);
            MaxCountPerWave = Mathf.Max(0, MaxCountPerWave);
            EliteChanceMultiplier = Mathf.Max(0f, EliteChanceMultiplier);
        }
    }
}
