using System;
using MoreMountains.Feedbacks;
using SoulKnight3D;
using UnityEditor;
using UnityEngine;

internal static class BlindBoxBuilder
{
    private const string BlindBoxPrefabPath =
        "Assets/Art/Prefab/InteractiveItems/Chests/BlindBox.prefab";
    private const string InteractLabelPrefabPath =
        "Assets/Art/Prefab/InteractiveItems/InteractLabel.prefab";
    private const string WeaponPoolPath =
        "Assets/Art/ScriptableObject/ChestRewards/Dungeon Weapon Drop Pool.asset";
    private const string GameControllerPrefabPath =
        "Assets/Art/Prefab/GameController.prefab";

    [InitializeOnLoadMethod]
    private static void ConfigureNewAssetOnReload()
    {
        EditorApplication.delayCall += () =>
        {
            GameObject blindBoxPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(BlindBoxPrefabPath);
            if (blindBoxPrefab != null &&
                blindBoxPrefab.GetComponent<BlindBox>() == null)
            {
                ConfigureBlindBox();
            }
        };
    }

    [MenuItem("SoulKnight3D/Configure Blind Box")]
    private static void ConfigureBlindBox()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BlindBoxPrefabPath);
        try
        {
            BlindBox blindBox = GetOrAddComponent<BlindBox>(root);
            Collider interactCollider = ConfigureInteractionCollider(root);
            InteractLabel label = ConfigureInteractLabel(root);
            Transform rewardSpawnPoint = GetOrCreateChild(
                root.transform, "RewardSpawnPoint");
            rewardSpawnPoint.localPosition = new Vector3(0f, 0.8f, 0f);
            rewardSpawnPoint.localRotation = Quaternion.identity;

            WeaponDropPoolSO weaponPool =
                AssetDatabase.LoadAssetAtPath<WeaponDropPoolSO>(WeaponPoolPath);
            if (weaponPool == null)
            {
                throw new InvalidOperationException(
                    $"Weapon pool was not found at {WeaponPoolPath}.");
            }

            MMF_Player openFeedbacks =
                FindChildComponent<MMF_Player>(root.transform, "OpenFeedbacks");
            if (openFeedbacks == null)
            {
                throw new InvalidOperationException(
                    "BlindBox requires an OpenFeedbacks child with an MMF_Player.");
            }

            blindBox.InteractCollider = interactCollider;
            blindBox.Label = label;
            SerializedObject serializedBlindBox = new SerializedObject(blindBox);
            serializedBlindBox.FindProperty("_weaponPool").objectReferenceValue =
                weaponPool;
            serializedBlindBox.FindProperty("_weaponRewardChance").floatValue = 0.5f;
            serializedBlindBox.FindProperty("_minimumCoinValue").intValue = 5;
            serializedBlindBox.FindProperty("_maximumCoinValue").intValue = 25;
            serializedBlindBox.FindProperty("_rewardSpawnPoint").objectReferenceValue =
                rewardSpawnPoint;
            serializedBlindBox.FindProperty("_remainChance").floatValue = 0.5f;
            serializedBlindBox.FindProperty("_interactionCooldown").floatValue = 1f;
            serializedBlindBox.FindProperty("_openFeedbacks").objectReferenceValue =
                openFeedbacks;
            serializedBlindBox.FindProperty("_despawnDelay").floatValue = 0.5f;
            serializedBlindBox.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BlindBoxPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        WireStarterSpawn();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WeaponDropPoolSO configuredPool =
            AssetDatabase.LoadAssetAtPath<WeaponDropPoolSO>(WeaponPoolPath);
        int weaponCount = configuredPool.GetDistinctPickupPrefabs().Count;
        Debug.Log(
            $"Configured BlindBox with {weaponCount} distinct, equally likely weapons " +
            "and assigned it as the level 1 starter reward.");
    }

    private static Collider ConfigureInteractionCollider(GameObject root)
    {
        BoxCollider[] colliders = root.GetComponents<BoxCollider>();
        BoxCollider interactionCollider = null;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger)
            {
                interactionCollider = colliders[i];
                break;
            }
        }

        if (interactionCollider == null)
        {
            interactionCollider = root.AddComponent<BoxCollider>();
        }

        interactionCollider.isTrigger = true;
        interactionCollider.center = new Vector3(0f, 0.1f, 0f);
        interactionCollider.size = new Vector3(1f, 1f, 1f);
        return interactionCollider;
    }

    private static InteractLabel ConfigureInteractLabel(GameObject root)
    {
        InteractLabel existingLabel = root.GetComponentInChildren<InteractLabel>(true);
        if (existingLabel != null)
        {
            return existingLabel;
        }

        GameObject labelPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(InteractLabelPrefabPath);
        if (labelPrefab == null)
        {
            throw new InvalidOperationException(
                $"Interact label prefab was not found at {InteractLabelPrefabPath}.");
        }

        GameObject labelObject = (GameObject)PrefabUtility.InstantiatePrefab(
            labelPrefab, root.transform);
        labelObject.name = "InteractLabel";
        labelObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.SetActive(false);
        return labelObject.GetComponent<InteractLabel>();
    }

    private static void WireStarterSpawn()
    {
        GameObject blindBoxPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(BlindBoxPrefabPath);
        GameObject controllerRoot =
            PrefabUtility.LoadPrefabContents(GameControllerPrefabPath);
        try
        {
            GameController controller = controllerRoot.GetComponent<GameController>();
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "GameController prefab has no GameController component.");
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_levelOneStarterChestPrefab")
                .objectReferenceValue = blindBoxPrefab;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(
                controllerRoot, GameControllerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(controllerRoot);
        }
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static T FindChildComponent<T>(Transform parent, string childName)
        where T : Component
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
