using System;
using MoreMountains.Feedbacks;
using SoulKnight3D;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NewWeaponLogicBuilder
{
    private const string WeaponFolder =
        "Assets/Art/Prefab/WeaponPrefabs/";
    private const string BulletFolder =
        "Assets/Art/Prefab/Bullets/";
    private const string DataFolder =
        "Assets/Art/ScriptableObject/Weapons/";

    private const string RocketGunRocketPath =
        BulletFolder + "Rocket Gun Rocket.prefab";
    private const string ClusterRocketPath =
        BulletFolder + "Cluster Rocket.prefab";
    private const string HomingSmallRocketPath =
        BulletFolder + "Homing Small Rocket.prefab";
    private const string HelixBulletPath =
        BulletFolder + "Helix Bullet.prefab";
    private const string LargeExplosionPath =
        "Assets/Art/Prefab/Particle/Bullet Impact/" +
        "Rocket Gun FX_Explosion.prefab";

    static NewWeaponLogicBuilder()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    [MenuItem("SoulKnight3D/Weapons/Build New Weapon Logic")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before building weapon prefabs.");
            return;
        }

        BuildProjectileVariants();
        BuildWeaponVariants();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Validate();
        Debug.Log("Built weapon logic for Crossbow, Chu Ko Nu, Rocket Gun, " +
                  "Cluster Missile, and SMG Helix.");
    }

    private static void AutoBuild()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            !NeedsBuild())
        {
            return;
        }

        Build();
    }

    private static bool NeedsBuild()
    {
        GameObject crossbow =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                WeaponFolder + "Crossbow.prefab");
        GameObject rocketGun =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                WeaponFolder + "Rocket Gun.prefab");
        GameObject cluster =
            AssetDatabase.LoadAssetAtPath<GameObject>(ClusterRocketPath);
        GameObject helix =
            AssetDatabase.LoadAssetAtPath<GameObject>(HelixBulletPath);

        return crossbow == null || crossbow.GetComponent<Gun>() == null ||
               rocketGun == null ||
               rocketGun.GetComponent<Gun>() == null ||
               rocketGun.GetComponent<Gun>().bulletPrefab == null ||
               cluster == null || cluster.GetComponent<ClusterRocket>() == null ||
               helix == null || helix.GetComponent<HelixBullet>() == null;
    }

    private static void BuildProjectileVariants()
    {
        EnsureVariant(
            "Assets/Art/Prefab/Particle/Bullet Impact/FX_Explosion.prefab",
            LargeExplosionPath);
        EditPrefab(LargeExplosionPath, root =>
        {
            root.transform.localScale = Vector3.one * 2f;
        });

        EnsureVariant(BulletFolder + "Rocket.prefab", RocketGunRocketPath);
        EditPrefab(RocketGunRocketPath, root =>
        {
            root.transform.localScale = Vector3.one * 2f;
            Rocket rocket = RequireComponent<Rocket>(root);
            SetSerializedFloat(rocket, "ExplosionRadius", 2f);

            GameObject explosionObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(LargeExplosionPath);
            ParticleSystem explosion = explosionObject != null
                ? explosionObject.GetComponentInChildren<ParticleSystem>(true)
                : null;
            MMF_ParticlesInstantiation particles =
                rocket.ImpactFeedback != null
                    ? rocket.ImpactFeedback
                        .GetFeedbackOfType<MMF_ParticlesInstantiation>()
                    : null;
            if (particles != null)
            {
                particles.ParticlesPrefab = explosion;
                EditorUtility.SetDirty(rocket.ImpactFeedback);
            }
        });

        EnsureVariant(BulletFolder + "Small Rocket.prefab",
            HomingSmallRocketPath);
        EditPrefab(HomingSmallRocketPath, root =>
        {
            HomingRocket homing = RequireComponent<HomingRocket>(root);
            homing.Configure(16f, 300f, 1.2f, 0.1f, 20f, 0.5f);
        });

        EnsureVariant(BulletFolder + "Rocket.prefab", ClusterRocketPath);
        EditPrefab(ClusterRocketPath, root =>
        {
            Transform feedbackTransform =
                FindDeepChild(root.transform, "SplitFeedback");
            if (feedbackTransform == null)
            {
                GameObject feedbackObject = new GameObject("SplitFeedback");
                feedbackTransform = feedbackObject.transform;
                feedbackTransform.SetParent(root.transform, false);
            }

            MMF_Player feedback =
                RequireComponent<MMF_Player>(feedbackTransform.gameObject);
            ClusterRocket cluster = RequireComponent<ClusterRocket>(root);
            GameObject splitRocket =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    HomingSmallRocketPath);
            cluster.Configure(splitRocket, feedback, 0.3f, 22f,
                16f, 3, 0.12f);
        });

        EnsureVariant(BulletFolder + "Splitted Bullet.prefab",
            HelixBulletPath);
        EditPrefab(HelixBulletPath, root =>
        {
            RequireComponent<HelixBullet>(root);
        });
    }

    private static void BuildWeaponVariants()
    {
        ConfigureWeapon<Gun>("Crossbow", BulletFolder + "Arrow.prefab",
            15f, gun => { });
        ConfigureWeapon<ConsecutiveGun>("Chu Ko Nu",
            BulletFolder + "Arrow.prefab", 15f,
            gun => gun.ConfigureBurst(2, 0.1f));
        ConfigureWeapon<Gun>("Rocket Gun", RocketGunRocketPath,
            10f, gun => { });
        ConfigureWeapon<Gun>("Cluster Missile", ClusterRocketPath,
            10f, gun => { });
        ConfigureWeapon<HelixGun>("SMG Helix", HelixBulletPath,
            16f, gun => gun.ConfigureHelix(0.22f, 2.5f, 180f));
    }

    private static void ConfigureWeapon<T>(string weaponName,
        string bulletPath, float bulletSpeed, Action<T> configure)
        where T : Gun
    {
        string prefabPath = WeaponFolder + weaponName + ".prefab";
        EditPrefab(prefabPath, root =>
        {
            T gun = root.GetComponent<T>();
            Weapon[] weapons = root.GetComponents<Weapon>();
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != gun)
                {
                    UnityEngine.Object.DestroyImmediate(weapons[i], true);
                }
            }

            if (gun == null)
            {
                gun = root.AddComponent<T>();
            }

            WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(
                DataFolder + weaponName + ".asset");
            GameObject bullet =
                AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
            Transform shootPoint = FindDeepChild(root.transform, "ShootPoint");
            Transform feedbackTransform =
                FindDeepChild(root.transform, "ShootFeedback");

            SerializedObject serializedGun = new SerializedObject(gun);
            SerializedProperty dataProperty =
                serializedGun.FindProperty("Data");
            if (dataProperty == null)
            {
                throw new InvalidOperationException(
                    $"Could not find Weapon.Data on {weaponName}.");
            }
            dataProperty.objectReferenceValue = data;
            serializedGun.ApplyModifiedPropertiesWithoutUndo();

            gun.BulletSpeed = bulletSpeed;
            gun.BulletSize = 1f;
            gun.bulletPrefab = bullet;
            gun.shootPoint = shootPoint;
            gun.ShootFeedback = feedbackTransform != null
                ? feedbackTransform.GetComponent<MMF_Player>()
                : null;
            configure(gun);
            EditorUtility.SetDirty(gun);
        });
    }

    private static void EnsureVariant(string sourcePath, string targetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null)
        {
            return;
        }

        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException(
                $"Missing source prefab: {sourcePath}");
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(source) as GameObject;
        try
        {
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate prefab: {sourcePath}");
            }
            PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
        }
        finally
        {
            if (instance != null)
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static T RequireComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name) { return root; }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), name);
            if (result != null) { return result; }
        }
        return null;
    }

    private static void SetSerializedFloat(UnityEngine.Object target,
        string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                $"Could not find {propertyName} on {target.name}.");
        }
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Validate()
    {
        ValidateWeapon<Gun>("Crossbow", "Arrow");
        ValidateWeapon<ConsecutiveGun>("Chu Ko Nu", "Arrow");
        ValidateWeapon<Gun>("Rocket Gun", "Rocket Gun Rocket");
        ValidateWeapon<Gun>("Cluster Missile", "Cluster Rocket");
        ValidateWeapon<HelixGun>("SMG Helix", "Helix Bullet");

        ValidateComponent<ClusterRocket>(ClusterRocketPath);
        ValidateComponent<HomingRocket>(HomingSmallRocketPath);
        ValidateComponent<HelixBullet>(HelixBulletPath);
    }

    private static void ValidateWeapon<T>(string weaponName,
        string expectedBulletName) where T : Gun
    {
        string path = WeaponFolder + weaponName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        T gun = prefab != null ? prefab.GetComponent<T>() : null;
        if (gun == null || gun.shootPoint == null ||
            gun.ShootFeedback == null || gun.bulletPrefab == null ||
            gun.bulletPrefab.name != expectedBulletName)
        {
            throw new InvalidOperationException(
                $"Weapon validation failed: {path}");
        }
    }

    private static void ValidateComponent<T>(string path) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null || prefab.GetComponent<T>() == null)
        {
            throw new InvalidOperationException(
                $"Projectile validation failed: {path}");
        }
    }
}
