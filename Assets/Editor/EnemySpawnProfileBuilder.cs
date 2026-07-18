using System;
using System.Collections.Generic;
using System.Text;
using SoulKnight3D;
using UnityEditor;
using UnityEngine;

public static class EnemySpawnProfileBuilder
{
    private const string ProfilePath =
        "Assets/Art/ScriptableObject/EnemyWaves/Forest Spawn Profile.asset";
    private const string FloorPath =
        "Assets/Art/ScriptableObject/Game Floors/1- Forest.asset";
    private const string EnemyPrefabFolder = "Assets/Art/Prefab/Characters/Enemy/";

    [MenuItem("Tools/Soul Knight/Rebuild Forest Spawn Profile")]
    public static void BuildForestProfile()
    {
        EnemySpawnProfileSO profile = AssetDatabase.LoadAssetAtPath<EnemySpawnProfileSO>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<EnemySpawnProfileSO>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        profile.SeedSalt = 1103;
        profile.EliteChance = 0.08f;
        profile.MaxEnemiesPerWave = 14;
        profile.LevelSettings = new List<EnemySpawnLevelSettings>
        {
            LevelSettings(1, 12, 8),
            LevelSettings(2, 14, 11),
            LevelSettings(3, 16, 14)
        };
        profile.Enemies = new List<EnemySpawnEntry>
        {
            Entry("Goblin Guard Pisol.prefab", "GoblinPisolElite.prefab", 1, 2, 3, 1),
            Entry("Goblin Guard Spear.prefab", "GoblinSpearElite.prefab", 1, 2, 3, 1),
            Entry("Goblin Guard Bow.prefab", null, 1, 2, 3, 1),
            Entry("Boar.prefab", "BoarElite.prefab", 1, 1, 2, 1),
            Entry("Elite Goblin Guard Shotgun.prefab", null, 2, 2, 1, 2),
            Entry("Elite Goblin Guard Blowpipe.prefab", null, 2, 2, 1, 2),
            Entry("Elite Goblin Guard Sickle.prefab", null, 2, 2, 1, 2),
            Entry("DireBoar.prefab", null, 1, 2, 2, 2),
            // The project compresses the Forest into three levels, so the original stage-four
            // Large Goblin unlock is placed at level three with a one-per-wave cap.
            Entry("GoblinGiant.prefab", null, 4, 4, 1, 3, 1)
        };

        EditorUtility.SetDirty(profile);
        WireProfileToForestFloor(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateProfile(profile);
        Debug.Log($"Rebuilt and validated Forest enemy spawn profile at '{ProfilePath}'.");
    }

    [MenuItem("Tools/Soul Knight/Validate Forest Spawn Profile")]
    public static void ValidateForestProfile()
    {
        EnemySpawnProfileSO profile = AssetDatabase.LoadAssetAtPath<EnemySpawnProfileSO>(ProfilePath);
        if (profile == null)
        {
            throw new InvalidOperationException($"Spawn profile is missing at '{ProfilePath}'.");
        }

        ValidateProfile(profile);
        Debug.Log("Forest enemy spawn profile validation passed.");
    }

    private static EnemySpawnLevelSettings LevelSettings(int level, int points, int waveCapacity)
    {
        return new EnemySpawnLevelSettings
        {
            Level = level,
            MinTotalPoints = points,
            MaxTotalPoints = points,
            WaveCapacity = waveCapacity
        };
    }

    private static EnemySpawnEntry Entry(string prefabName, string elitePrefabName,
        int pointCost, int waveSize, int weight, int minLevel, int maxCountPerWave = 0)
    {
        return new EnemySpawnEntry
        {
            EnemyPrefab = LoadEnemyPrefab(prefabName),
            ElitePrefab = string.IsNullOrEmpty(elitePrefabName)
                ? null
                : LoadEnemyPrefab(elitePrefabName),
            PointCost = pointCost,
            WaveSize = waveSize,
            Weight = weight,
            MinLevel = minLevel,
            MaxLevel = 0,
            MaxCountPerWave = maxCountPerWave,
            EliteChanceMultiplier = 1f
        };
    }

    private static GameObject LoadEnemyPrefab(string prefabName)
    {
        string path = EnemyPrefabFolder + prefabName;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Enemy prefab is missing at '{path}'.");
        }

        return prefab;
    }

    private static void WireProfileToForestFloor(EnemySpawnProfileSO profile)
    {
        GameFloorSO floor = AssetDatabase.LoadAssetAtPath<GameFloorSO>(FloorPath);
        if (floor == null)
        {
            throw new InvalidOperationException($"Forest floor asset is missing at '{FloorPath}'.");
        }

        foreach (GameLevel level in floor.GameLevels)
        {
            level.EnemySpawnProfile = profile;
        }

        EditorUtility.SetDirty(floor);
    }

    private static void ValidateProfile(EnemySpawnProfileSO profile)
    {
        if (profile.LevelSettings.Count == 0 || profile.Enemies.Count == 0)
        {
            throw new InvalidOperationException("The Forest spawn profile has no settings or enemies.");
        }

        foreach (EnemySpawnEntry entry in profile.Enemies)
        {
            if (entry.EnemyPrefab == null || entry.EnemyPrefab.GetComponent<Enemy>() == null)
            {
                throw new InvalidOperationException("Every spawn entry must reference a prefab with Enemy.");
            }

            if (entry.ElitePrefab != null && entry.ElitePrefab.GetComponent<Enemy>() == null)
            {
                throw new InvalidOperationException(
                    $"Elite prefab '{entry.ElitePrefab.name}' has no Enemy component.");
            }
        }

        for (int level = 1; level <= 3; level++)
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                EnemyWavePlan first = EnemyWavePlanner.Generate(profile, level, seed);
                EnemyWavePlan second = EnemyWavePlanner.Generate(profile, level, seed);
                int maxPointCost = GetMaximumPointCost(profile, level);
                if (first.PlannedPointTotal < first.TotalPointBudget ||
                    first.PlannedPointTotal >= first.TotalPointBudget + maxPointCost)
                {
                    throw new InvalidOperationException(
                        $"Level {level}, seed {seed} planned {first.PlannedPointTotal}/" +
                        $"{first.TotalPointBudget} points.");
                }

                if (BuildSignature(first) != BuildSignature(second))
                {
                    throw new InvalidOperationException(
                        $"Enemy planning is not deterministic for level {level}, seed {seed}.");
                }

                ValidatePlanEntries(profile, level, first);
            }
        }
    }

    private static int GetMaximumPointCost(EnemySpawnProfileSO profile, int level)
    {
        int maximum = 1;
        foreach (EnemySpawnEntry entry in profile.Enemies)
        {
            if (entry != null && entry.IsAvailableAtLevel(level))
            {
                maximum = Mathf.Max(maximum, entry.PointCost);
            }
        }

        return maximum;
    }

    private static void ValidatePlanEntries(EnemySpawnProfileSO profile, int level,
        EnemyWavePlan plan)
    {
        EnemySpawnLevelSettings settings = profile.GetSettings(level);
        int maxWaveSize = GetMaximumWaveSize(profile, level);

        for (int groupIndex = 0; groupIndex < plan.WaveGroups.Count; groupIndex++)
        {
            EnemyWaveGroup group = plan.WaveGroups[groupIndex];
            int count = 0;
            int usedCapacity = 0;
            foreach (EnemyWave wave in group.Waves)
            {
                count += wave.Count;
                EnemySpawnEntry entry = FindEntry(profile, wave.EnemyPrefab);
                if (entry == null || !entry.IsAvailableAtLevel(level))
                {
                    throw new InvalidOperationException(
                        $"Prefab '{wave.EnemyPrefab.name}' is not valid at level {level}.");
                }

                usedCapacity += entry.WaveSize * wave.Count;
                if (entry.MaxCountPerWave > 0 && wave.Count > entry.MaxCountPerWave)
                {
                    throw new InvalidOperationException(
                        $"Prefab '{wave.EnemyPrefab.name}' exceeds its per-wave cap.");
                }
            }

            if (count > profile.MaxEnemiesPerWave)
            {
                throw new InvalidOperationException("A generated wave exceeds MaxEnemiesPerWave.");
            }

            bool isFinalWave = groupIndex == plan.WaveGroups.Count - 1;
            if (!isFinalWave && usedCapacity < settings.WaveCapacity)
            {
                throw new InvalidOperationException(
                    $"Level {level} generated a non-final wave using only " +
                    $"{usedCapacity}/{settings.WaveCapacity} capacity.");
            }

            if (usedCapacity >= settings.WaveCapacity + maxWaveSize)
            {
                throw new InvalidOperationException(
                    $"Level {level} wave capacity overshoot is too large: " +
                    $"{usedCapacity}/{settings.WaveCapacity}.");
            }
        }
    }

    private static int GetMaximumWaveSize(EnemySpawnProfileSO profile, int level)
    {
        int maximum = 1;
        foreach (EnemySpawnEntry entry in profile.Enemies)
        {
            if (entry != null && entry.IsAvailableAtLevel(level))
            {
                maximum = Mathf.Max(maximum, entry.WaveSize);
            }
        }

        return maximum;
    }

    private static EnemySpawnEntry FindEntry(EnemySpawnProfileSO profile, GameObject prefab)
    {
        foreach (EnemySpawnEntry entry in profile.Enemies)
        {
            if (entry.EnemyPrefab == prefab || entry.ElitePrefab == prefab)
            {
                return entry;
            }
        }

        return null;
    }

    private static string BuildSignature(EnemyWavePlan plan)
    {
        var signature = new StringBuilder();
        foreach (EnemyWaveGroup group in plan.WaveGroups)
        {
            signature.Append('[');
            foreach (EnemyWave wave in group.Waves)
            {
                signature.Append(wave.EnemyPrefab.name).Append(':').Append(wave.Count).Append(',');
            }
            signature.Append(']');
        }

        return signature.ToString();
    }
}
