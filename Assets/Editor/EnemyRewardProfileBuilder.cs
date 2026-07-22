using System;
using System.Collections.Generic;
using SoulKnight3D;
using UnityEditor;
using UnityEngine;

public static class EnemyRewardProfileBuilder
{
    private const string EnemyFolder = "Assets/Art/Prefab/Characters/Enemy/";
    private const string BossFolder = "Assets/Art/Prefab/Characters/Boss/";

    private sealed class Profile
    {
        public readonly string Path;
        public readonly int Rate;
        public readonly int[] Values;

        public Profile(string path, int rate, params int[] values)
        {
            Path = path;
            Rate = rate;
            Values = values;
        }
    }

    private static readonly IReadOnlyList<Profile> Profiles = new[]
    {
        // Original forest IDs: e_boar01, e_boar02 and ex_boar02.
        Enemy("Boar.prefab", 15, 0, 0, 1, 1),
        Enemy("DireBoar.prefab", 15, 0, 0, 2, 1),
        Enemy("BoarElite.prefab", 100, 0, 0, 3, 3),

        // e_orc01-03: pistol, spear and bow Goblin Guards.
        Enemy("Goblin Guard Pisol.prefab", 20, 0, 0, 1, 1),
        Enemy("Goblin Guard Spear.prefab", 20, 0, 0, 1, 1),
        Enemy("Goblin Guard Bow.prefab", 20, 0, 0, 1, 1),

        // e_orc04-06: shotgun, blowpipe and sickle Elite Goblin Guards.
        Enemy("Elite Goblin Guard Shotgun.prefab", 25, 0, 0, 2, 1),
        Enemy("Elite Goblin Guard Blowpipe.prefab", 25, 0, 0, 2, 1),
        Enemy("Elite Goblin Guard Sickle.prefab", 25, 0, 0, 2, 1),

        // e_orc08 and the elite forms ex_orc01/ex_orc02.
        Enemy("GoblinGiant.prefab", 45, 0, 1, 0, 2),
        Enemy("GoblinPisolElite.prefab", 100, 0, 0, 3, 2),
        Enemy("GoblinSpearElite.prefab", 100, 0, 0, 3, 2),

        // All three original forest bosses use this same reward profile.
        Boss("Werewolf.prefab", 100, 3, 3, 0, 10)
    };

    [MenuItem("Tools/Soul Knight/Rebuild Enemy Reward Profiles")]
    public static void Rebuild()
    {
        foreach (Profile profile in Profiles)
        {
            Apply(profile);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Applied {Profiles.Count} extracted Soul Knight 1.8.4 reward profiles.");
    }

    [MenuItem("Tools/Soul Knight/Validate Enemy Reward Profiles")]
    public static void Validate()
    {
        foreach (Profile profile in Profiles)
        {
            Validate(profile);
        }

        Debug.Log($"Validated {Profiles.Count} extracted Soul Knight 1.8.4 reward profiles.");
    }

    private static Profile Enemy(string name, int rate, params int[] values)
    {
        return new Profile(EnemyFolder + name, rate, values);
    }

    private static Profile Boss(string name, int rate, params int[] values)
    {
        return new Profile(BossFolder + name, rate, values);
    }

    private static void Apply(Profile profile)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(profile.Path);
        try
        {
            MonoBehaviour enemy = GetRewardOwner(root, profile.Path);
            SerializedObject serializedEnemy = new SerializedObject(enemy);
            serializedEnemy.FindProperty("_rewardRate").intValue = profile.Rate;

            SerializedProperty values = serializedEnemy.FindProperty("_rewardValues");
            values.arraySize = EnemyRewardDropSystem.RewardValueCount;
            for (int i = 0; i < EnemyRewardDropSystem.RewardValueCount; i++)
            {
                values.GetArrayElementAtIndex(i).intValue = profile.Values[i];
            }

            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, profile.Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Validate(profile);
    }

    private static void Validate(Profile profile)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(profile.Path);
        MonoBehaviour enemy = GetRewardOwner(prefab, profile.Path);
        SerializedObject serializedEnemy = new SerializedObject(enemy);
        SerializedProperty rate = serializedEnemy.FindProperty("_rewardRate");
        SerializedProperty values = serializedEnemy.FindProperty("_rewardValues");

        if (rate == null || values == null || rate.intValue != profile.Rate ||
            values.arraySize != EnemyRewardDropSystem.RewardValueCount)
        {
            throw new InvalidOperationException($"Invalid reward profile on '{profile.Path}'.");
        }

        for (int i = 0; i < EnemyRewardDropSystem.RewardValueCount; i++)
        {
            if (values.GetArrayElementAtIndex(i).intValue != profile.Values[i])
            {
                throw new InvalidOperationException(
                    $"Reward value {i} is invalid on '{profile.Path}'.");
            }
        }
    }

    private static MonoBehaviour GetRewardOwner(GameObject root, string path)
    {
        Enemy enemy = root.GetComponent<Enemy>();
        if (enemy != null)
        {
            return enemy;
        }

        Werewolf boss = root.GetComponent<Werewolf>();
        if (boss != null)
        {
            return boss;
        }

        throw new MissingComponentException($"No supported enemy component found on '{path}'.");
    }
}
