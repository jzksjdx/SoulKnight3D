using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "GameFloorSO", menuName = "ScriptableObject/GameFloorSO")]
    public class GameFloorSO : ScriptableObject
    {
        public List<GameLevel> GameLevels = new List<GameLevel>();
        public List<WeightedBossEncounter> BossPool = new List<WeightedBossEncounter>();

        public int GetWeaponPoolLevel(int gameLevel)
        {
            if (GameLevels.Count == 0)
            {
                return 1;
            }

            int index = Mathf.Clamp(gameLevel - 1, 0, GameLevels.Count - 1);
            return Mathf.Max(0, GameLevels[index].WeaponPoolLevel);
        }

        public BossEncounterDataSO SelectBoss(float normalizedRoll)
        {
            float totalWeight = 0f;
            for (int i = 0; i < BossPool.Count; i++)
            {
                if (BossPool[i] != null && BossPool[i].IsAvailable)
                {
                    totalWeight += BossPool[i].Weight;
                }
            }

            if (totalWeight <= 0f) { return null; }

            float selection = Mathf.Clamp01(normalizedRoll) * totalWeight;
            BossEncounterDataSO lastAvailableBoss = null;
            for (int i = 0; i < BossPool.Count; i++)
            {
                WeightedBossEncounter entry = BossPool[i];
                if (entry == null || !entry.IsAvailable) { continue; }

                lastAvailableBoss = entry.Boss;
                selection -= entry.Weight;
                if (selection <= 0f)
                {
                    return entry.Boss;
                }
            }

            return lastAvailableBoss;
        }

        private void OnValidate()
        {
            for (int i = 0; i < BossPool.Count; i++)
            {
                if (BossPool[i] != null)
                {
                    BossPool[i].Weight = Mathf.Max(0f, BossPool[i].Weight);
                }
            }
        }
    }

    [Serializable]
    public class GameLevel
    {
        [Min(0)] public int WeaponPoolLevel = 1;
        public EnemySpawnProfileSO EnemySpawnProfile;
        public List<EnemyWaveSO> LevelWaves = new List<EnemyWaveSO>();
    }
}
