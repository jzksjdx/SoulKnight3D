using System;
using System.Collections.Generic;
using System.Linq;
using SoulKnight3D;
using UnityEditor;
using UnityEngine;

public static class WeaponRewardSystemBuilder
{
    private const string WeaponFolder = "Assets/Art/ScriptableObject/Weapons";
    private const string PickupWeaponFolder = "Assets/Art/Prefab/PickUpWeapons";
    private const string PoolPath = "Assets/Art/ScriptableObject/ChestRewards/Dungeon Weapon Drop Pool.asset";
    private const string BrownRewardPath = "Assets/Art/ScriptableObject/ChestRewards/BrownChestRewards.asset";
    private const string BlueRewardPath = "Assets/Art/ScriptableObject/ChestRewards/BlueChestRewards.asset";
    private const string WhiteRewardPath = "Assets/Art/ScriptableObject/ChestRewards/WhiteChestRewards.asset";
    private const string LevelZeroRewardPath = "Assets/Art/ScriptableObject/ChestRewards/Level0BrownChestRewards.asset";
    private const string BrownChestPrefabPath = "Assets/Art/Prefab/InteractiveItems/Chests/BrownChest.prefab";
    private const string BlueChestPrefabPath = "Assets/Art/Prefab/InteractiveItems/Chests/BlueChest.prefab";
    private const string LevelZeroChestPrefabPath = "Assets/Art/Prefab/InteractiveItems/Chests/Level0BrownChest.prefab";
    private const string GameControllerPrefabPath = "Assets/Art/Prefab/GameController.prefab";
    private const string RoomManagerPrefabPath = "Assets/Art/Prefab/MapPrefabs/RoomManagerPrefab.prefab";
    private const string MerchantRoomPrefabPath = "Assets/Art/Prefab/MapPrefabs/MerchantRoomLayout.prefab";
    private const string BrownSecondaryMaterialPath = "Assets/Art/Materials/Chest Materials/ChestBrown2.mat";
    private const string BlueSecondaryMaterialPath = "Assets/Art/Materials/Chest Materials/ChestBlue2.mat";

    // Current wiki levels take precedence; recovered 1.8.4 data fills remaining gaps.
    private static readonly Dictionary<string, int[]> AuthenticFiveLevelDropLevels =
        new Dictionary<string, int[]>
    {
        { "AK-47", new[] { 0, 1 } },
        { "Assault Rocket", new[] { 3 } },
        { "Bad Pistol", new[] { 3 } },
        { "Bazooka", new[] { 3 } },
        { "Blind Missile Battery", new[] { 3 } },
        { "Blowpipe", new[] { 2 } },
        { "Bow", new[] { 0, 1 } },
        { "Broadsword", new[] { 2 } },
        { "Chu Ko Nu", new[] { 2 } },
        { "Cluster Missile", new[] { 4 } },
        { "Crossbow", new[] { 2 } },
        { "Desert Eagle", new[] { 0, 1 } },
        { "Enemy Blowpipe", new[] { -1 } },
        { "Goblin Giant Staff", new[] { -1 } },
        { "High-Energy SMG", new[] { -1 } },
        { "M14", new[] { 1 } },
        { "M4", new[] { 3 } },
        { "Machete", new[] { 1 } },
        { "Meat", new[] { 4 } },
        { "Old Rocket Launcher", new[] { 1 } },
        { "Old Sniper Rifle", new[] { 1 } },
        { "P250 Pistol", new[] { 1 } },
        { "Pirate Saber", new[] { 2 } },
        { "Pioneer", new[] { 0, 2 } },
        { "Revolver", new[] { 0, 1 } },
        { "Rocket Gun", new[] { 3 } },
        { "Shotgun", new[] { 0, 1 } },
        { "SMG Helix", new[] { 2 } },
        { "Sniper Rifle", new[] { 3 } },
        { "Snow Fox L", new[] { 0, 1 } },
        { "Snow Fox XL", new[] { 2 } },
        { "Splitter Cannon", new[] { 2 } },
        { "Splitter Gun", new[] { 1 } },
        { "Strong Bow", new[] { 1 } },
        { "UZI", new[] { 2 } }
    };

    // Pool 0 remains exclusive to the level-one starter chest. Original tier 4 is
    // folded into game level 3 so the compact demo can award every weapon.
    private static readonly Dictionary<string, int[]> CompactThreeLevelDropLevels =
        AuthenticFiveLevelDropLevels.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .Select(level => level < 0 ? level : Mathf.Min(level, 3))
                .Distinct()
                .ToArray());

    [MenuItem("Tools/Soul Knight/Rebuild Weapon Reward System")]
    public static void RebuildWeaponRewardSystem()
    {
        RebuildWeaponRewardSystem(AuthenticFiveLevelDropLevels, 4);
        Debug.Log("Rebuilt authentic five-level weapon reward system.");
    }

    [MenuItem("Tools/Soul Knight/Build Compact 3-Level Weapon Reward System")]
    public static void BuildCompactThreeLevelWeaponRewardSystem()
    {
        RebuildWeaponRewardSystem(CompactThreeLevelDropLevels, 3);
        Debug.Log("Built compact three-level weapon reward system.");
    }

    private static void RebuildWeaponRewardSystem(
        IReadOnlyDictionary<string, int[]> dropLevels, int maxPoolLevel)
    {
        List<WeaponData> weapons = LoadWeapons();
        AssignDropLevels(weapons, dropLevels);

        WeaponDropPoolSO pool = BuildDropPool(weapons);
        ConfigureChestReward(BrownRewardPath, "BrownChestRewards", pool, 0, maxPoolLevel);
        ChestRewardData blueRewards = ConfigureChestReward(
            BlueRewardPath, "BlueChestRewards", pool, 1, maxPoolLevel);
        ChestRewardData levelZeroRewards = ConfigureChestReward(LevelZeroRewardPath,
            "Level0BrownChestRewards", pool, 0, maxPoolLevel, 0);
        ConfigureWhiteChestWeaponPool(pool, maxPoolLevel);
        BuildBlueChestPrefab(blueRewards);
        GameObject levelZeroChest = BuildLevelZeroChestPrefab(levelZeroRewards);
        WireLevelZeroChestToGameController(levelZeroChest);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateWeaponRewardSystem(dropLevels, maxPoolLevel);
    }

    [MenuItem("Tools/Soul Knight/Validate Weapon Reward System")]
    public static void ValidateWeaponRewardSystem()
    {
        ValidateWeaponRewardSystem(AuthenticFiveLevelDropLevels, 4);
        Debug.Log("Five-level weapon reward system validation passed.");
    }

    private static void ValidateWeaponRewardSystem(
        IReadOnlyDictionary<string, int[]> expectedDropLevels, int maxPoolLevel)
    {
        WeaponDropPoolSO pool = AssetDatabase.LoadAssetAtPath<WeaponDropPoolSO>(PoolPath);
        if (pool == null)
        {
            throw new InvalidOperationException($"Weapon drop pool is missing at '{PoolPath}'.");
        }

        List<WeaponData> weapons = LoadWeapons();
        ValidateWeaponDefinitions(weapons, expectedDropLevels);

        for (int level = 0; level <= maxPoolLevel; level++)
        {
            if (!pool.HasLevel(level))
            {
                throw new InvalidOperationException($"Weapon drop pool has no entries for level {level}.");
            }
        }

        foreach (WeaponDropPoolLevel levelPool in pool.Levels)
        {
            if (levelPool.Level < 0 || levelPool.Level > maxPoolLevel)
            {
                throw new InvalidOperationException(
                    $"Weapon drop pool contains unexpected level {levelPool.Level}.");
            }

            foreach (WeaponDropPoolEntry entry in levelPool.Entries)
            {
                if (entry == null || !entry.Enabled)
                {
                    continue;
                }

                if (entry.Weapon == null || entry.GetPickupPrefab() == null)
                {
                    throw new InvalidOperationException(
                        $"Level {levelPool.Level} contains a weapon without a pickup prefab.");
                }
            }
        }

        ValidatePoolCoverage(pool, weapons);
        ValidatePickupPrefabCoverage(weapons);

        ValidateChestReward(BrownRewardPath, 0, maxPoolLevel);
        ValidateChestReward(BlueRewardPath, 1, maxPoolLevel);
        ValidateChestReward(LevelZeroRewardPath, 0, maxPoolLevel, 0);
        ValidateWhiteChestReward(pool, maxPoolLevel);
        ValidateMerchantPool(pool);

        GameObject blueChest = AssetDatabase.LoadAssetAtPath<GameObject>(BlueChestPrefabPath);
        if (blueChest == null || blueChest.GetComponent<Chest>() == null)
        {
            throw new InvalidOperationException("Blue chest prefab is missing or has no Chest component.");
        }

        GameObject levelZeroChest = AssetDatabase.LoadAssetAtPath<GameObject>(LevelZeroChestPrefabPath);
        Chest levelZeroChestComponent = levelZeroChest != null ? levelZeroChest.GetComponent<Chest>() : null;
        if (levelZeroChestComponent == null ||
            levelZeroChestComponent.ChestReward !=
            AssetDatabase.LoadAssetAtPath<ChestRewardData>(LevelZeroRewardPath))
        {
            throw new InvalidOperationException("Level-0 brown chest prefab is missing or incorrectly wired.");
        }

        GameObject gameControllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameControllerPrefabPath);
        SerializedObject gameController = new SerializedObject(gameControllerPrefab.GetComponent<GameController>());
        if (gameController.FindProperty("_levelOneStarterChestPrefab").objectReferenceValue != levelZeroChest)
        {
            throw new InvalidOperationException("GameController is not wired to the level-0 brown chest.");
        }

    }

    private static List<WeaponData> LoadWeapons()
    {
        return AssetDatabase.FindAssets("t:WeaponData", new[] { WeaponFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<WeaponData>)
            .Where(weapon => weapon != null)
            .OrderBy(weapon => weapon.name)
            .ToList();
    }

    private static void AssignDropLevels(List<WeaponData> weapons,
        IReadOnlyDictionary<string, int[]> dropLevels)
    {
        foreach (WeaponData weapon in weapons)
        {
            if (!dropLevels.TryGetValue(weapon.name, out int[] levels))
            {
                throw new InvalidOperationException(
                    $"Weapon '{weapon.name}' has no explicit recovered drop-level definition.");
            }

            if (levels[0] >= 0 && weapon.PickUpPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Droppable weapon '{weapon.name}' has no pickup prefab.");
            }

            int[] currentLevels = new[] { weapon.DropLevel }
                .Concat(weapon.ExtraDropLevels)
                .ToArray();
            if (!currentLevels.SequenceEqual(levels))
            {
                weapon.DropLevel = levels[0];
                weapon.ExtraDropLevels = levels.Skip(1).ToList();
                EditorUtility.SetDirty(weapon);
            }
        }
    }

    private static WeaponDropPoolSO BuildDropPool(List<WeaponData> weapons)
    {
        WeaponDropPoolSO pool = AssetDatabase.LoadAssetAtPath<WeaponDropPoolSO>(PoolPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<WeaponDropPoolSO>();
            AssetDatabase.CreateAsset(pool, PoolPath);
        }

        Dictionary<int, List<WeaponDropPoolEntry>> levelEntries = new Dictionary<int, List<WeaponDropPoolEntry>>();
        foreach (WeaponData weapon in weapons)
        {
            AddWeaponToPool(levelEntries, weapon, weapon.DropLevel);
            for (int i = 0; i < weapon.ExtraDropLevels.Count; i++)
            {
                AddWeaponToPool(levelEntries, weapon, weapon.ExtraDropLevels[i]);
            }
        }

        pool.Levels = levelEntries
            .OrderBy(pair => pair.Key)
            .Select(pair => new WeaponDropPoolLevel
            {
                Level = pair.Key,
                Entries = pair.Value.OrderBy(entry => entry.Weapon.name).ToList()
            })
            .ToList();

        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static void AddWeaponToPool(Dictionary<int, List<WeaponDropPoolEntry>> levelEntries,
        WeaponData weapon, int level)
    {
        if (weapon == null || weapon.PickUpPrefab == null || level < 0)
        {
            return;
        }

        if (!levelEntries.TryGetValue(level, out List<WeaponDropPoolEntry> entries))
        {
            entries = new List<WeaponDropPoolEntry>();
            levelEntries[level] = entries;
        }

        entries.Add(new WeaponDropPoolEntry
        {
            Weapon = weapon,
            PickupOverride = null,
            Weight = 1f,
            Enabled = true
        });
    }

    private static ChestRewardData ConfigureChestReward(string path, string assetName,
        WeaponDropPoolSO pool, int levelOffset, int maxPoolLevel, int fixedLevel = -1)
    {
        ChestRewardData rewards = AssetDatabase.LoadAssetAtPath<ChestRewardData>(path);
        if (rewards == null)
        {
            rewards = ScriptableObject.CreateInstance<ChestRewardData>();
            AssetDatabase.CreateAsset(rewards, path);
        }

        rewards.ChestRewards = new List<ChestRewardData.RewardCategory>
        {
            new ChestRewardData.RewardCategory
            {
                Type = ChestRewardData.ChestRewardType.Weapon,
                Rate = 1f,
                Items = new List<ChestRewardData.RewardItem>(),
                UseWeaponPool = true,
                WeaponPool = pool,
                FixedWeaponPoolLevel = fixedLevel,
                WeaponPoolLevelOffset = levelOffset,
                MinWeaponPoolLevel = 0,
                MaxWeaponPoolLevel = maxPoolLevel
            }
        };

        rewards.name = assetName;
        EditorUtility.SetDirty(rewards);
        return rewards;
    }

    private static void ConfigureWhiteChestWeaponPool(WeaponDropPoolSO pool, int maxPoolLevel)
    {
        ChestRewardData rewards = AssetDatabase.LoadAssetAtPath<ChestRewardData>(WhiteRewardPath);
        ChestRewardData.RewardCategory weaponCategory = rewards?.ChestRewards.Find(
            category => category.Type == ChestRewardData.ChestRewardType.Weapon);
        if (weaponCategory == null)
        {
            throw new InvalidOperationException("White chest has no weapon reward category.");
        }

        weaponCategory.UseWeaponPool = true;
        weaponCategory.WeaponPool = pool;
        weaponCategory.FixedWeaponPoolLevel = 1;
        weaponCategory.WeaponPoolLevelOffset = 0;
        weaponCategory.MinWeaponPoolLevel = 0;
        weaponCategory.MaxWeaponPoolLevel = maxPoolLevel;
        EditorUtility.SetDirty(rewards);
    }

    private static GameObject BuildLevelZeroChestPrefab(ChestRewardData rewards)
    {
        GameObject brownChest = AssetDatabase.LoadAssetAtPath<GameObject>(BrownChestPrefabPath);
        if (brownChest == null)
        {
            throw new InvalidOperationException($"Brown chest prefab is missing at '{BrownChestPrefabPath}'.");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(brownChest) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException("Could not instantiate the level-0 brown chest.");
        }

        try
        {
            instance.name = "Level0BrownChest";
            Chest chest = instance.GetComponent<Chest>();
            if (chest == null)
            {
                throw new InvalidOperationException("Brown chest prefab has no Chest component.");
            }

            chest.ChestReward = rewards;
            return PrefabUtility.SaveAsPrefabAsset(instance, LevelZeroChestPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void WireLevelZeroChestToGameController(GameObject levelZeroChest)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameControllerPrefabPath);
        try
        {
            GameController gameController = root.GetComponent<GameController>();
            if (gameController == null)
            {
                throw new InvalidOperationException("GameController prefab has no GameController component.");
            }

            SerializedObject serializedController = new SerializedObject(gameController);
            SerializedProperty chestProperty =
                serializedController.FindProperty("_levelOneStarterChestPrefab");
            if (chestProperty == null)
            {
                throw new InvalidOperationException("Level-1 starter chest field was not found.");
            }

            chestProperty.objectReferenceValue = levelZeroChest;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, GameControllerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildBlueChestPrefab(ChestRewardData rewards)
    {
        GameObject brownChest = AssetDatabase.LoadAssetAtPath<GameObject>(BrownChestPrefabPath);
        if (brownChest == null)
        {
            throw new InvalidOperationException($"Brown chest prefab is missing at '{BrownChestPrefabPath}'.");
        }

        Material brownSecondary = AssetDatabase.LoadAssetAtPath<Material>(BrownSecondaryMaterialPath);
        if (brownSecondary == null)
        {
            throw new InvalidOperationException(
                $"Brown secondary material is missing at '{BrownSecondaryMaterialPath}'.");
        }

        Material blueSecondary = GetOrCreateBlueMaterial(brownSecondary);
        GameObject instance = PrefabUtility.InstantiatePrefab(brownChest) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException("Could not instantiate brown chest prefab.");
        }

        try
        {
            instance.name = "BlueChest";
            Chest chest = instance.GetComponent<Chest>();
            if (chest == null)
            {
                throw new InvalidOperationException("Brown chest prefab has no Chest component.");
            }

            chest.ChestReward = rewards;
            ReplaceMaterial(instance, brownSecondary, blueSecondary);
            PrefabUtility.SaveAsPrefabAsset(instance, BlueChestPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static Material GetOrCreateBlueMaterial(Material source)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(BlueSecondaryMaterialPath);
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, BlueSecondaryMaterialPath);
        }

        material.name = "ChestBlue2";
        Color blue = new Color(0.1f, 0.36f, 0.9f, 1f);
        material.color = blue;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", blue);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", blue);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ReplaceMaterial(GameObject root, Material oldMaterial, Material newMaterial)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == oldMaterial)
                {
                    materials[i] = newMaterial;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    private static void ValidateWeaponDefinitions(List<WeaponData> weapons,
        IReadOnlyDictionary<string, int[]> expectedDropLevels)
    {
        string[] missingAssets = expectedDropLevels.Keys
            .Except(weapons.Select(weapon => weapon.name))
            .OrderBy(name => name)
            .ToArray();
        if (missingAssets.Length > 0)
        {
            throw new InvalidOperationException(
                $"Drop-level definitions reference missing weapon assets: {string.Join(", ", missingAssets)}.");
        }

        foreach (WeaponData weapon in weapons)
        {
            if (!expectedDropLevels.TryGetValue(weapon.name, out int[] expectedLevels))
            {
                throw new InvalidOperationException(
                    $"Weapon '{weapon.name}' has no explicit drop-level definition.");
            }

            int[] actualLevels = new[] { weapon.DropLevel }
                .Concat(weapon.ExtraDropLevels)
                .ToArray();
            if (!actualLevels.SequenceEqual(expectedLevels))
            {
                throw new InvalidOperationException(
                    $"Weapon '{weapon.name}' has drop levels [{string.Join(", ", actualLevels)}], " +
                    $"expected [{string.Join(", ", expectedLevels)}].");
            }
        }
    }

    private static void ValidatePoolCoverage(WeaponDropPoolSO pool, List<WeaponData> weapons)
    {
        HashSet<WeaponData> pooledWeapons = new HashSet<WeaponData>(
            pool.Levels
                .Where(level => level != null)
                .SelectMany(level => level.Entries)
                .Where(entry => entry != null && entry.Enabled && entry.Weapon != null)
                .Select(entry => entry.Weapon));

        string[] missingWeapons = weapons
            .Where(weapon => weapon.DropLevel >= 0 && weapon.PickUpPrefab != null)
            .Where(weapon => !pooledWeapons.Contains(weapon))
            .Select(weapon => weapon.name)
            .OrderBy(name => name)
            .ToArray();
        if (missingWeapons.Length > 0)
        {
            throw new InvalidOperationException(
                $"Droppable weapons missing from the pool: {string.Join(", ", missingWeapons)}.");
        }
    }

    private static void ValidatePickupPrefabCoverage(List<WeaponData> weapons)
    {
        HashSet<GameObject> configuredPickups = new HashSet<GameObject>(
            weapons.Where(weapon => weapon.PickUpPrefab != null)
                .Select(weapon => weapon.PickUpPrefab));
        string[] unconfiguredPickups = AssetDatabase.FindAssets("t:Prefab", new[] { PickupWeaponFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(prefab => prefab != null && prefab.GetComponent<PickupWeapon>() != null)
            .Where(prefab => !configuredPickups.Contains(prefab))
            .Select(prefab => prefab.name)
            .OrderBy(name => name)
            .ToArray();
        if (unconfiguredPickups.Length > 0)
        {
            throw new InvalidOperationException(
                $"Pickup weapon prefabs without weapon data: {string.Join(", ", unconfiguredPickups)}.");
        }
    }

    private static void ValidateChestReward(string path, int expectedOffset,
        int expectedMaxPoolLevel, int expectedFixedLevel = -1)
    {
        ChestRewardData rewards = AssetDatabase.LoadAssetAtPath<ChestRewardData>(path);
        if (rewards == null || rewards.ChestRewards.Count != 1)
        {
            throw new InvalidOperationException($"Chest reward asset '{path}' is not configured.");
        }

        ChestRewardData.RewardCategory category = rewards.ChestRewards[0];
        if (category.Type != ChestRewardData.ChestRewardType.Weapon ||
            !category.UseWeaponPool ||
            category.WeaponPool == null ||
            category.WeaponPoolLevelOffset != expectedOffset ||
            category.MaxWeaponPoolLevel != expectedMaxPoolLevel ||
            category.FixedWeaponPoolLevel != expectedFixedLevel)
        {
            throw new InvalidOperationException($"Chest reward asset '{path}' has invalid weapon pool settings.");
        }
    }

    private static void ValidateWhiteChestReward(WeaponDropPoolSO expectedPool,
        int expectedMaxPoolLevel)
    {
        ChestRewardData rewards = AssetDatabase.LoadAssetAtPath<ChestRewardData>(WhiteRewardPath);
        ChestRewardData.RewardCategory category = rewards?.ChestRewards.Find(
            reward => reward.Type == ChestRewardData.ChestRewardType.Weapon);
        if (category == null || !category.UseWeaponPool || category.WeaponPool != expectedPool ||
            category.FixedWeaponPoolLevel != 1 || category.WeaponPoolLevelOffset != 0 ||
            category.MaxWeaponPoolLevel != expectedMaxPoolLevel)
        {
            throw new InvalidOperationException("White chest has invalid weapon pool settings.");
        }
    }

    private static void ValidateMerchantPool(WeaponDropPoolSO expectedPool)
    {
        GameObject roomManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomManagerPrefabPath);
        RoomManager roomManager = roomManagerPrefab != null
            ? roomManagerPrefab.GetComponent<RoomManager>()
            : null;
        SerializedProperty roomManagerPool = roomManager != null
            ? new SerializedObject(roomManager).FindProperty("_merchantWeaponPool")
            : null;
        if (roomManagerPool == null || roomManagerPool.objectReferenceValue != expectedPool)
        {
            throw new InvalidOperationException(
                "RoomManager prefab is not wired to the dungeon weapon drop pool.");
        }

        GameObject merchantRoomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MerchantRoomPrefabPath);
        MerchantRoom merchantRoom = merchantRoomPrefab != null
            ? merchantRoomPrefab.GetComponent<MerchantRoom>()
            : null;
        if (merchantRoom == null || merchantRoom.WeaponPool != expectedPool)
        {
            throw new InvalidOperationException(
                "Merchant room prefab is not wired to the dungeon weapon drop pool.");
        }
    }
}
