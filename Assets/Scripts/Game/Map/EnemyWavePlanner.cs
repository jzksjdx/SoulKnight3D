using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class EnemyWavePlan
    {
        public int Seed { get; }
        public int TotalPointBudget { get; }
        public int PlannedPointTotal { get; }
        public IReadOnlyList<EnemyWaveGroup> WaveGroups { get; }

        public EnemyWavePlan(int seed, int totalPointBudget, int plannedPointTotal,
            IReadOnlyList<EnemyWaveGroup> waveGroups)
        {
            Seed = seed;
            TotalPointBudget = totalPointBudget;
            PlannedPointTotal = plannedPointTotal;
            WaveGroups = waveGroups ?? Array.Empty<EnemyWaveGroup>();
        }
    }

    public static class EnemyWavePlanner
    {
        public static int CombineSeed(int runSeed, int roomKey, int profileSalt)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + runSeed;
                hash = hash * 31 + roomKey;
                hash = hash * 31 + profileSalt;
                return hash;
            }
        }

        public static EnemyWavePlan Generate(EnemySpawnProfileSO profile, int level, int seed)
        {
            if (profile == null)
            {
                return EmptyPlan(seed);
            }

            EnemySpawnLevelSettings settings = profile.GetSettings(level);
            if (settings == null)
            {
                Debug.LogError($"Spawn profile '{profile.name}' has no level settings.");
                return EmptyPlan(seed);
            }

            List<EnemySpawnEntry> eligibleEnemies = GetEligibleEnemies(profile, level);
            if (eligibleEnemies.Count == 0)
            {
                Debug.LogError($"Spawn profile '{profile.name}' has no valid enemies for level {level}.");
                return EmptyPlan(seed);
            }

            var random = new System.Random(seed);
            int minPoints = Mathf.Max(1, settings.MinTotalPoints);
            int maxPoints = Mathf.Max(minPoints, settings.MaxTotalPoints);
            int totalBudget = NextInclusive(random, minPoints, maxPoints);
            int remainingPoints = totalBudget;
            var groups = new List<EnemyWaveGroup>();
            int plannedPoints = 0;
            while (remainingPoints > 0)
            {
                EnemyWaveGroup group = GenerateWave(profile, eligibleEnemies,
                    settings.WaveCapacity, remainingPoints, random,
                    out int wavePoints);

                if (wavePoints <= 0)
                {
                    Debug.LogError($"Spawn profile '{profile.name}' could not spend its remaining " +
                                   $"{remainingPoints} points at level {level}.");
                    break;
                }

                groups.Add(group);
                plannedPoints += wavePoints;
                remainingPoints -= wavePoints;
            }

            return new EnemyWavePlan(seed, totalBudget, plannedPoints, groups);
        }

        private static EnemyWavePlan EmptyPlan(int seed)
        {
            return new EnemyWavePlan(seed, 0, 0, Array.Empty<EnemyWaveGroup>());
        }

        private static List<EnemySpawnEntry> GetEligibleEnemies(EnemySpawnProfileSO profile, int level)
        {
            var eligible = new List<EnemySpawnEntry>();
            foreach (EnemySpawnEntry enemy in profile.Enemies)
            {
                if (enemy != null && enemy.IsAvailableAtLevel(level))
                {
                    eligible.Add(enemy);
                }
            }

            return eligible;
        }

        private static EnemyWaveGroup GenerateWave(EnemySpawnProfileSO profile,
            List<EnemySpawnEntry> eligibleEnemies, int waveCapacity, int pointBudget,
            System.Random random,
            out int plannedPoints)
        {
            var group = new EnemyWaveGroup();
            var archetypeCounts = new Dictionary<EnemySpawnEntry, int>();
            int remainingCapacity = Mathf.Max(1, waveCapacity);
            int remainingPoints = Mathf.Max(1, pointBudget);
            int enemyCount = 0;
            plannedPoints = 0;

            while (remainingCapacity > 0 && remainingPoints > 0 &&
                   enemyCount < profile.MaxEnemiesPerWave)
            {
                List<EnemySpawnEntry> candidates = GetCandidates(eligibleEnemies, archetypeCounts);
                if (candidates.Count == 0)
                {
                    break;
                }

                EnemySpawnEntry selected = SelectWeighted(candidates, random);
                GameObject prefab = RollPrefab(profile, selected, random);
                AddOrIncrement(group, prefab);

                archetypeCounts.TryGetValue(selected, out int currentCount);
                archetypeCounts[selected] = currentCount + 1;
                remainingCapacity -= selected.WaveSize;
                remainingPoints -= selected.PointCost;
                plannedPoints += selected.PointCost;
                enemyCount++;
            }

            return group;
        }

        private static List<EnemySpawnEntry> GetCandidates(List<EnemySpawnEntry> eligibleEnemies,
            Dictionary<EnemySpawnEntry, int> counts)
        {
            var candidates = new List<EnemySpawnEntry>();
            foreach (EnemySpawnEntry enemy in eligibleEnemies)
            {
                counts.TryGetValue(enemy, out int currentCount);
                bool belowLimit = enemy.MaxCountPerWave == 0 || currentCount < enemy.MaxCountPerWave;
                if (belowLimit)
                {
                    candidates.Add(enemy);
                }
            }

            return candidates;
        }

        private static EnemySpawnEntry SelectWeighted(List<EnemySpawnEntry> candidates,
            System.Random random)
        {
            int totalWeight = 0;
            foreach (EnemySpawnEntry candidate in candidates)
            {
                totalWeight += candidate.Weight;
            }

            int roll = random.Next(totalWeight);
            foreach (EnemySpawnEntry candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll < 0)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static GameObject RollPrefab(EnemySpawnProfileSO profile, EnemySpawnEntry entry,
            System.Random random)
        {
            float eliteChance = Mathf.Clamp01(profile.EliteChance * entry.EliteChanceMultiplier);
            return entry.ElitePrefab != null && random.NextDouble() < eliteChance
                ? entry.ElitePrefab
                : entry.EnemyPrefab;
        }

        private static void AddOrIncrement(EnemyWaveGroup group, GameObject prefab)
        {
            foreach (EnemyWave wave in group.Waves)
            {
                if (wave.EnemyPrefab == prefab)
                {
                    wave.Count++;
                    return;
                }
            }

            group.Waves.Add(new EnemyWave
            {
                EnemyPrefab = prefab,
                Count = 1
            });
        }

        private static int NextInclusive(System.Random random, int minimum, int maximum)
        {
            return minimum == maximum ? minimum : random.Next(minimum, maximum + 1);
        }
    }
}
