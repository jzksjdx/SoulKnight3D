using System;
using System.Collections.Generic;
using SoulKnight3D;
using UnityEditor;
using UnityEngine;

public static class NewWeaponAssetsBuilder
{
    private const string WeaponDataFolder = "Assets/Art/ScriptableObject/Weapons";
    private const string WeaponPrefabFolder = "Assets/Art/Prefab/WeaponPrefabs";
    private const string PickupPrefabFolder = "Assets/Art/Prefab/PickUpWeapons";
    private const string PickupTemplatePath = PickupPrefabFolder + "/Pickup AK-47.prefab";
    private const string SplitterBulletPath = "Assets/Art/Prefab/Bullets/Splitter Bullet.prefab";
    private const string SplitterCannonBulletPath = "Assets/Art/Prefab/Bullets/Splitter Cannon Bullet.prefab";
    private const string SplitBulletPath = "Assets/Art/Prefab/Bullets/Splitted Bullet.prefab";

    private static readonly WeaponDefinition[] Definitions =
    {
        new WeaponDefinition(
            "Snow Fox L", "\u96ea\u72d0", "weapons_12.asset",
            WeaponData.WeaponCategory.Rifle, WeaponData.WeaponRarity.White,
            WeaponData.WeaponAnimation.Rifle, 2, 0, 5, 10, 15, 1f / 6f,
            new[] { 0, 1 }, "Snow Fox L.fbx", "Snow Fox L.mat",
            "Assets/Art/Prefab/Bullets/Cyan Bullet.prefab",
            WeaponPrefabFolder + "/Snow Fox XL.prefab"),
        new WeaponDefinition(
            "Pioneer", "\u5148\u9a71\u8005", "weapons_125.asset",
            WeaponData.WeaponCategory.Pistol, WeaponData.WeaponRarity.White,
            WeaponData.WeaponAnimation.Pistol, 4, 1, 15, 5, 12, 1f / 3f,
            new[] { 0, 2 }, "Pioneer.fbx", "Pioneer.mat",
            "Assets/Art/Prefab/Bullets/Pioneer Bullet.prefab",
            WeaponPrefabFolder + "/P250 Pistol.prefab"),
        new WeaponDefinition(
            "Splitter Gun", "\u5206\u88c2\u8005", "weapons_41.asset",
            WeaponData.WeaponCategory.Rifle, WeaponData.WeaponRarity.White,
            WeaponData.WeaponAnimation.Rifle, 5, 2, 5, 0, 20, 1f / 2.4f,
            new[] { 1 }, "Splitter Gun.fbx", "Splitter Gun.mat",
            SplitterBulletPath, WeaponPrefabFolder + "/AK-47.prefab"),
        new WeaponDefinition(
            "Splitter Cannon", "\u5f3a\u529b\u5206\u88c2\u8005", "weapons_123.asset",
            WeaponData.WeaponCategory.Rifle, WeaponData.WeaponRarity.Green,
            WeaponData.WeaponAnimation.Rifle, 6, 3, 10, 0, 22, 1f / 1.5f,
            new[] { 2 }, "Splitter Cannon.fbx", "Splitter Cannon.mat",
            SplitterCannonBulletPath, WeaponPrefabFolder + "/AK-47.prefab")
    };

    [MenuItem("Tools/Soul Knight/Build New Weapon Assets")]
    public static void BuildAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureSplitterBullets();

        foreach (WeaponDefinition definition in Definitions)
        {
            WeaponData data = GetOrCreateWeaponData(definition);
            GameObject weaponPrefab = BuildWeaponPrefab(definition, data);
            GameObject pickupPrefab = BuildPickupPrefab(definition, data);

            data.WeaponPrefab = weaponPrefab;
            data.PickUpPrefab = pickupPrefab;
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        WeaponRewardSystemBuilder.RebuildWeaponRewardSystem();
        ValidateAll();
        Debug.Log("Built Snow Fox L, Pioneer, Splitter Gun, and Splitter Cannon assets.");
    }

    [MenuItem("Tools/Soul Knight/Validate New Weapon Assets")]
    public static void ValidateAll()
    {
        foreach (WeaponDefinition definition in Definitions)
        {
            string dataPath = GetDataPath(definition.Name);
            WeaponData data = RequireAsset<WeaponData>(dataPath);
            GameObject weaponPrefab = RequireAsset<GameObject>(GetWeaponPrefabPath(definition.Name));
            GameObject pickupPrefab = RequireAsset<GameObject>(GetPickupPrefabPath(definition.Name));
            Gun gun = weaponPrefab.GetComponent<Gun>();
            PickupWeapon pickup = pickupPrefab.GetComponent<PickupWeapon>();

            if (gun == null || gun.GetPrefabWeaponData() != data || gun.bulletPrefab == null)
            {
                throw new InvalidOperationException(definition.Name + " weapon prefab is not fully wired.");
            }

            if (pickup == null || pickup.WeaponData != data || data.WeaponPrefab != weaponPrefab ||
                data.PickUpPrefab != pickupPrefab || data.Sprite == null)
            {
                throw new InvalidOperationException(definition.Name + " pickup or WeaponData is not fully wired.");
            }
        }

        ValidateSplitter(SplitterBulletPath, BulletSplitPattern.SixLocalAxes, 6, 2);
        ValidateSplitter(SplitterCannonBulletPath, BulletSplitPattern.EvenSphere, 18, 3);
        WeaponRewardSystemBuilder.ValidateWeaponRewardSystem();
        Debug.Log("New weapon asset validation passed.");
    }

    private static void ConfigureSplitterBullets()
    {
        GameObject splitBullet = RequireAsset<GameObject>(SplitBulletPath);
        ConfigureSplitterPrefab(SplitterBulletPath, "Splitter Bullet", splitBullet,
            BulletSplitPattern.SixLocalAxes, 6, 2, 12f);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(SplitterCannonBulletPath) == null &&
            !AssetDatabase.CopyAsset(SplitterBulletPath, SplitterCannonBulletPath))
        {
            throw new InvalidOperationException("Could not create the Splitter Cannon bullet prefab.");
        }

        ConfigureSplitterPrefab(SplitterCannonBulletPath, "Splitter Cannon Bullet", splitBullet,
            BulletSplitPattern.EvenSphere, 18, 3, 12f);
    }

    private static void ConfigureSplitterPrefab(string path, string prefabName, GameObject childPrefab,
        BulletSplitPattern pattern, int count, int childDamage, float childSpeed)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = prefabName;
            BulletSplitter splitter = root.GetComponent<BulletSplitter>();
            if (splitter == null)
            {
                splitter = root.AddComponent<BulletSplitter>();
            }

            splitter.Configure(childPrefab, pattern, count, childDamage, childSpeed, 0.12f);
            EditorUtility.SetDirty(splitter);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static WeaponData GetOrCreateWeaponData(WeaponDefinition definition)
    {
        string path = GetDataPath(definition.Name);
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.name = definition.Name;
        data.Name = definition.Name;
        data.NameCN = definition.ChineseName;
        data.Sprite = RequireAsset<Sprite>(
            "Assets/Art/SoulKnightOriginal/Weapon Sprite/" + definition.SpriteFile);
        data.Category = definition.Category;
        data.Rarity = definition.Rarity;
        data.Animation = definition.Animation;
        data.Damage = definition.Damage;
        data.EnergyCost = definition.EnergyCost;
        data.CritChance = definition.CritChance;
        data.Inaccuracy = definition.Inaccuracy;
        data.Price = definition.Price;
        data.Cooldown = definition.Cooldown;
        data.DropLevel = definition.DropLevels[0];
        data.ExtraDropLevels = new List<int>();
        for (int i = 1; i < definition.DropLevels.Length; i++)
        {
            data.ExtraDropLevels.Add(definition.DropLevels[i]);
        }

        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject BuildWeaponPrefab(WeaponDefinition definition, WeaponData data)
    {
        GameObject template = RequireAsset<GameObject>(definition.WeaponTemplatePath);
        GameObject modelAsset = RequireAsset<GameObject>(
            "Assets/Art/Models/Weapons/" + definition.ModelFile);
        Material material = RequireAsset<Material>(
            "Assets/Art/Materials/Weapons/" + definition.MaterialFile);
        GameObject bulletPrefab = RequireAsset<GameObject>(definition.BulletPrefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;

        if (instance == null)
        {
            throw new InvalidOperationException("Could not instantiate weapon template for " + definition.Name + ".");
        }

        try
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = definition.Name;
            RemoveHeldWeaponModel(instance.transform);
            AddModel(modelAsset, material, instance.transform, definition.Name + " Model", instance.layer);

            Gun gun = instance.GetComponent<Gun>();
            if (gun == null)
            {
                throw new InvalidOperationException("Weapon template has no Gun component.");
            }

            SerializedObject serializedGun = new SerializedObject(gun);
            serializedGun.FindProperty("Data").objectReferenceValue = data;
            serializedGun.ApplyModifiedPropertiesWithoutUndo();
            gun.bulletPrefab = bulletPrefab;
            gun.BulletSpeed = 20f;
            gun.BulletSize = 1f;

            return PrefabUtility.SaveAsPrefabAsset(instance, GetWeaponPrefabPath(definition.Name));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static GameObject BuildPickupPrefab(WeaponDefinition definition, WeaponData data)
    {
        GameObject template = RequireAsset<GameObject>(PickupTemplatePath);
        GameObject modelAsset = RequireAsset<GameObject>(
            "Assets/Art/Models/Weapons/" + definition.ModelFile);
        Material material = RequireAsset<Material>(
            "Assets/Art/Materials/Weapons/" + definition.MaterialFile);
        GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;

        if (instance == null)
        {
            throw new InvalidOperationException("Could not instantiate pickup template for " + definition.Name + ".");
        }

        try
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = "Pickup " + definition.Name;
            PickupWeapon pickup = instance.GetComponent<PickupWeapon>();
            if (pickup == null || pickup.WeaponModel == null)
            {
                throw new InvalidOperationException("Pickup template is missing its pickup setup.");
            }

            pickup.WeaponData = data;
            ClearChildren(pickup.WeaponModel);
            AddModel(modelAsset, material, pickup.WeaponModel,
                definition.Name + " Pickup Model", instance.layer);
            FitPickupCollider(instance, pickup.WeaponModel);

            return PrefabUtility.SaveAsPrefabAsset(instance, GetPickupPrefabPath(definition.Name));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void RemoveHeldWeaponModel(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child.name == "ShootPoint" || child.name == "ShootFeedback" || child.name == "Point Light")
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    private static GameObject AddModel(GameObject modelAsset, Material material, Transform parent,
        string modelName, int layer)
    {
        GameObject model = PrefabUtility.InstantiatePrefab(modelAsset, parent) as GameObject;
        if (model == null)
        {
            throw new InvalidOperationException("Could not instantiate model " + modelAsset.name + ".");
        }

        model.name = modelName;
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        SetLayerRecursively(model, layer);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }

        return model;
    }

    private static void FitPickupCollider(GameObject root, Transform modelRoot)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null)
        {
            return;
        }

        collider.center = root.transform.InverseTransformPoint(bounds.center);
        Vector3 size = bounds.size + Vector3.one * 0.04f;
        collider.size = new Vector3(
            Mathf.Max(0.08f, size.x),
            Mathf.Max(0.08f, size.y),
            Mathf.Max(0.08f, size.z));
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void ValidateSplitter(string path, BulletSplitPattern expectedPattern,
        int expectedCount, int expectedDamage)
    {
        GameObject prefab = RequireAsset<GameObject>(path);
        BulletSplitter splitter = prefab.GetComponent<BulletSplitter>();
        if (splitter == null || splitter.SplitBulletPrefab == null ||
            splitter.Pattern != expectedPattern || splitter.SplitBulletCount != expectedCount ||
            splitter.SplitBulletDamage != expectedDamage)
        {
            throw new InvalidOperationException(path + " has an invalid split configuration.");
        }
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidOperationException("Required asset is missing at '" + path + "'.");
        }
        return asset;
    }

    private static string GetDataPath(string name)
    {
        return WeaponDataFolder + "/" + name + ".asset";
    }

    private static string GetWeaponPrefabPath(string name)
    {
        return WeaponPrefabFolder + "/" + name + ".prefab";
    }

    private static string GetPickupPrefabPath(string name)
    {
        return PickupPrefabFolder + "/Pickup " + name + ".prefab";
    }

    private sealed class WeaponDefinition
    {
        public readonly string Name;
        public readonly string ChineseName;
        public readonly string SpriteFile;
        public readonly WeaponData.WeaponCategory Category;
        public readonly WeaponData.WeaponRarity Rarity;
        public readonly WeaponData.WeaponAnimation Animation;
        public readonly int Damage;
        public readonly int EnergyCost;
        public readonly int CritChance;
        public readonly int Inaccuracy;
        public readonly int Price;
        public readonly float Cooldown;
        public readonly int[] DropLevels;
        public readonly string ModelFile;
        public readonly string MaterialFile;
        public readonly string BulletPrefabPath;
        public readonly string WeaponTemplatePath;

        public WeaponDefinition(string name, string chineseName, string spriteFile,
            WeaponData.WeaponCategory category, WeaponData.WeaponRarity rarity,
            WeaponData.WeaponAnimation animation, int damage, int energyCost,
            int critChance, int inaccuracy, int price, float cooldown, int[] dropLevels,
            string modelFile, string materialFile, string bulletPrefabPath,
            string weaponTemplatePath)
        {
            Name = name;
            ChineseName = chineseName;
            SpriteFile = spriteFile;
            Category = category;
            Rarity = rarity;
            Animation = animation;
            Damage = damage;
            EnergyCost = energyCost;
            CritChance = critChance;
            Inaccuracy = inaccuracy;
            Price = price;
            Cooldown = cooldown;
            DropLevels = dropLevels;
            ModelFile = modelFile;
            MaterialFile = materialFile;
            BulletPrefabPath = bulletPrefabPath;
            WeaponTemplatePath = weaponTemplatePath;
        }
    }
}
