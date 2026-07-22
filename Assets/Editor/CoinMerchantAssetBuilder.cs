using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnight3D.Editor
{
    public static class CoinMerchantAssetBuilder
    {
        private const string TestScenePath = "Assets/Scenes/TestScene.unity";
        private const string CoinSourceMaterialPath = "Assets/Art/Materials/Coin.mat";
        private const string CoinMaterialFolder = "Assets/Art/Materials/Coins";
        private const string CoinPrefabFolder = "Assets/Art/Prefab/InteractiveItems/Coins";
        private const string GameObjectsManagerPath = "Assets/Art/Prefab/GameObjectsManager.prefab";
        private const string RoomManagerPath = "Assets/Art/Prefab/MapPrefabs/RoomManagerPrefab.prefab";
        private const string MerchantLayoutPath = "Assets/Art/Prefab/MapPrefabs/MerchantRoomLayout.prefab";
        private const string PriceLabelPath = "Assets/Art/Prefab/InteractiveItems/PriceLabel.prefab";
        private const string WeaponPoolPath = "Assets/Art/ScriptableObject/ChestRewards/Dungeon Weapon Drop Pool.asset";
        private const string HealthPotionPath = "Assets/Art/Prefab/InteractiveItems/HealthPotion.prefab";
        private const string EnergyPotionPath = "Assets/Art/Prefab/InteractiveItems/EnergyPotion.prefab";
        private const string RestorationPotionPath = "Assets/Art/Prefab/InteractiveItems/RestorationPotion.prefab";

        [MenuItem("SoulKnight3D/Build Coin And Merchant Assets")]
        public static void BuildAll()
        {
            EnsureFolder(CoinMaterialFolder);
            EnsureFolder(CoinPrefabFolder);

            Scene temporaryScene = default;
            GameObject coinSource = FindSceneObject("Coin");
            if (coinSource == null)
            {
                temporaryScene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
                coinSource = FindSceneObject("Coin", temporaryScene);
            }

            if (coinSource == null)
            {
                throw new MissingReferenceException(
                    "The Coin prototype could not be found in TestScene.");
            }

            Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(CoinSourceMaterialPath);
            if (sourceMaterial == null)
            {
                throw new MissingReferenceException($"Missing source material at {CoinSourceMaterialPath}.");
            }

            Material copperMaterial = CreateOrUpdateMaterial("CoinCopper",
                new Color(0.72f, 0.31f, 0.09f), new Color(1.25f, 0.35f, 0.05f), sourceMaterial);
            Material silverMaterial = CreateOrUpdateMaterial("CoinSilver",
                new Color(0.72f, 0.8f, 0.9f), new Color(0.55f, 0.72f, 1f), sourceMaterial);
            Material goldMaterial = CreateOrUpdateMaterial("CoinGold",
                new Color(1f, 0.63f, 0.07f), new Color(1.7f, 0.62f, 0.04f), sourceMaterial);

            GameObject copperPrefab = BuildCoinPrefab(coinSource, "CoinCopper",
                CoinPickup.CoinType.Copper, 1, copperMaterial);
            GameObject silverPrefab = BuildCoinPrefab(coinSource, "CoinSilver",
                CoinPickup.CoinType.Silver, 3, silverMaterial);
            GameObject goldPrefab = BuildCoinPrefab(coinSource, "CoinGold",
                CoinPickup.CoinType.Gold, 5, goldMaterial);

            ConfigureGameObjectsManager(copperPrefab, silverPrefab, goldPrefab);
            ConfigurePotions();
            ConfigureMerchantLayout();
            ConfigureRoomManager();

            if (temporaryScene.IsValid())
            {
                EditorSceneManager.CloseScene(temporaryScene, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Coin and merchant assets built and wired successfully.");
        }

        private static Material CreateOrUpdateMaterial(string materialName, Color baseColor,
            Color emissionColor, Material sourceMaterial)
        {
            string path = $"{CoinMaterialFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(sourceMaterial) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildCoinPrefab(GameObject source, string prefabName,
            CoinPickup.CoinType type, int value, Material material)
        {
            GameObject root = new GameObject(prefabName);
            root.layer = source.layer;

            GameObject visual = Object.Instantiate(source, root.transform);
            visual.name = "Visual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, source.transform.localRotation);
            visual.transform.localScale = source.transform.localScale;

            Rigidbody[] inheritedRigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < inheritedRigidbodies.Length; i++)
            {
                Object.DestroyImmediate(inheritedRigidbodies[i]);
            }
            MonoBehaviour[] inheritedBehaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < inheritedBehaviours.Length; i++)
            {
                Object.DestroyImmediate(inheritedBehaviours[i]);
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }

            SphereCollider pickupTrigger = root.AddComponent<SphereCollider>();
            pickupTrigger.isTrigger = true;
            pickupTrigger.radius = 0.16f;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 0.05f;
            rigidbody.drag = 1f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CoinPickup coinPickup = root.AddComponent<CoinPickup>();
            coinPickup.Configure(type, value, visual.transform);

            string prefabPath = $"{CoinPrefabFolder}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureGameObjectsManager(params GameObject[] coinPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GameObjectsManagerPath);
            try
            {
                GameObjectsManager manager = root.GetComponent<GameObjectsManager>();
                manager.CoinPrefabs.Clear();
                manager.CoinPrefabs.AddRange(coinPrefabs);
                EditorUtility.SetDirty(manager);
                PrefabUtility.SaveAsPrefabAsset(root, GameObjectsManagerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurePotions()
        {
            ConfigurePotion(HealthPotionPath, "Health Potion", "生命药水", 25);
            ConfigurePotion(EnergyPotionPath, "Energy Potion", "能量药水", 20);
            ConfigurePotion(RestorationPotionPath, "Restoration Potion", "恢复药水", 30);
        }

        private static void ConfigurePotion(string path, string displayName, string displayNameCN,
            int basePrice)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Potion potion = root.GetComponent<Potion>();
                potion.ConfigureMerchantData(displayName, displayNameCN, basePrice);
                EditorUtility.SetDirty(potion);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureMerchantLayout()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MerchantLayoutPath);
            try
            {
                root.transform.localPosition = Vector3.zero;
                MerchantRoom merchantRoom = root.GetComponent<MerchantRoom>();
                if (merchantRoom == null)
                {
                    merchantRoom = root.AddComponent<MerchantRoom>();
                }

                merchantRoom.StockPoints.Clear();
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == "StockPoint")
                    {
                        merchantRoom.StockPoints.Add(transforms[i]);
                    }
                }

                merchantRoom.WeaponPool = AssetDatabase.LoadAssetAtPath<WeaponDropPoolSO>(WeaponPoolPath);
                merchantRoom.PotionPrefabs.Clear();
                merchantRoom.PotionPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(HealthPotionPath));
                merchantRoom.PotionPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(EnergyPotionPath));
                merchantRoom.PotionPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(RestorationPotionPath));
                merchantRoom.PriceLabelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PriceLabelPath);
                merchantRoom.PotionStockYOffset = 0.2f;
                merchantRoom.PriceIncreasePerLevel = 0.15f;
                EditorUtility.SetDirty(merchantRoom);
                PrefabUtility.SaveAsPrefabAsset(root, MerchantLayoutPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureRoomManager()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RoomManagerPath);
            try
            {
                RoomManager roomManager = root.GetComponent<RoomManager>();
                SerializedObject serializedRoom = new SerializedObject(roomManager);
                serializedRoom.FindProperty("_merchantRoomPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(MerchantLayoutPath);
                serializedRoom.FindProperty("_merchantWeaponPool").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<WeaponDropPoolSO>(WeaponPoolPath);
                serializedRoom.FindProperty("_merchantPriceLabelPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PriceLabelPath);
                serializedRoom.FindProperty("_merchantPotionStockYOffset").floatValue = 0.2f;

                SerializedProperty potions = serializedRoom.FindProperty("_merchantPotionPrefabs");
                potions.arraySize = 3;
                potions.GetArrayElementAtIndex(0).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(HealthPotionPath);
                potions.GetArrayElementAtIndex(1).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(EnergyPotionPath);
                potions.GetArrayElementAtIndex(2).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(RestorationPotionPath);
                serializedRoom.FindProperty("_merchantPriceIncreasePerLevel").floatValue = 0.15f;
                serializedRoom.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RoomManagerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject FindSceneObject(string objectName, Scene scene = default)
        {
            Scene[] scenes = scene.IsValid()
                ? new[] { scene }
                : GetLoadedScenes();
            for (int sceneIndex = 0; sceneIndex < scenes.Length; sceneIndex++)
            {
                GameObject[] roots = scenes[sceneIndex].GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                    for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                    {
                        if (transforms[transformIndex].name == objectName)
                        {
                            return transforms[transformIndex].gameObject;
                        }
                    }
                }
            }
            return null;
        }

        private static Scene[] GetLoadedScenes()
        {
            List<Scene> scenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                scenes.Add(SceneManager.GetSceneAt(i));
            }
            return scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
