#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MoreMountains.Feedbacks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SoulKnight3D.Editor
{
    public static class GoblinPriestAssetBuilder
    {
        private const string BossPrefabPath =
            "Assets/Art/Prefab/Characters/Boss/Goblin Priest.prefab";
        private const string ControllerPath =
            "Assets/Art/Animation/Goblin Priest/Goblin Priest.controller";
        private const string AnimationFolder = "Assets/Art/Animation/Goblin Priest";
        private const string ProjectileFolder =
            "Assets/Art/Prefab/Bullets/Goblin Priest";
        private const string MaterialFolder =
            "Assets/Art/Materials/Enemy/Goblin Priest Projectiles";
        private const string ForestFloorPath =
            "Assets/Art/ScriptableObject/Game Floors/1- Forest.asset";
        private const string BossEncounterPath =
            "Assets/Art/ScriptableObject/Bosses/Goblin Priest.asset";
        private const string LegacyLinePrefabPath =
            ProjectileFolder + "/Priest Line Emitter.prefab";
        private const string LineBulletPrefabPath =
            ProjectileFolder + "/Priest Line Bullet.prefab";
        private const string SplitterClonePrefabPath =
            ProjectileFolder + "/Priest Splitter Swirl Clone.prefab";
        private const string SplitterParentPrefabPath =
            ProjectileFolder + "/Priest Splitter Parent Swirl Bullet.prefab";
        private const string AudioFolder = "Assets/Art/Audio";
        private const string DissolveShaderTemplatePath =
            "Assets/Plugins/ShaderGraph_Dissolve/URP/ShaderGraph/Dissolve_Metallic.shadergraph";
        private const string DissolveShaderPath =
            "Assets/Art/Shaders/Goblin Priest Dissolve.shadergraph";
        private const string PbrSubGraphPath =
            "Assets/Plugins/ShaderGraph_Dissolve/Utility/SubGraph/PBR_Metallic Sub Graph.shadersubgraph";
        private const string PriestMaterialPath =
            "Assets/Art/Materials/Enemy/Goblin Priest.mat";

        [MenuItem("SoulKnight3D/Build Goblin Priest Boss")]
        public static void Build()
        {
            EnsureFolder(ProjectileFolder);
            EnsureFolder(MaterialFolder);

            Material smallMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Small Bullet.mat",
                new Color(1f, 0.36f, 0.08f), true);
            Material swirlMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Swirl Bullet.mat",
                new Color(0.72f, 0.08f, 1f), true);
            MigrateAsset(
                $"{MaterialFolder}/Priest Line Emitter.mat",
                $"{MaterialFolder}/Priest Line Bullet.mat");
            Material lineBulletMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Line Bullet.mat",
                new Color(0.1f, 0.85f, 1f), true);
            Material orbMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Protective Orb.mat",
                new Color(1f, 0.8f, 0.12f), true);
            Material meteorMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Meteor.mat",
                new Color(0.95f, 0.12f, 0.04f), true);
            Material warningMaterial = GetOrCreateWarningMaterial(
                $"{MaterialFolder}/Priest Meteor Warning.mat");
            Material minimapMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Minimap.mat", Color.red, true);

            GameObject smallBullet = CreateBulletPrefab(
                "Priest Small Bullet", PrimitiveType.Sphere,
                new Vector3(0.13f, 0.13f, 0.13f), smallMaterial);
            GameObject swirlBullet = CreateBulletPrefab(
                "Priest Swirl Bullet", PrimitiveType.Cylinder,
                new Vector3(0.12f, 0.18f, 0.12f), swirlMaterial,
                root =>
                {
                    PriestSwirlBullet behavior = root.AddComponent<PriestSwirlBullet>();
                    behavior.Configure(smallBullet, 0.75f, 3, 28f, 7f, 2, 95f);
                });
            MigrateAsset(LegacyLinePrefabPath, LineBulletPrefabPath);
            GameObject lineBullet = ConfigureLineBulletPrefab(lineBulletMaterial);
            GameObject splitterClone =
                CreateSplitterClonePrefab(swirlMaterial, lineBullet);
            GameObject splitterParent = CreateBulletPrefab(
                "Priest Splitter Parent Swirl Bullet", PrimitiveType.Cylinder,
                new Vector3(0.12f, 0.18f, 0.12f), swirlMaterial,
                root =>
                {
                    PriestSplitterParentBullet behavior =
                        root.AddComponent<PriestSplitterParentBullet>();
                    behavior.Configure(splitterClone, 0.5f, 5f);
                });
            GameObject protectiveOrb = CreateProtectiveOrbPrefab(orbMaterial);
            GameObject warning = CreateWarningPrefab(warningMaterial);
            GameObject meteor = CreateMeteorPrefab(meteorMaterial);

            ConfigureController();
            ConfigureAnimationEvents();
            EnsureDissolveShaderEmission();
            ConfigurePriestDissolveMaterial();
            ConfigureBossPrefab(swirlBullet, splitterParent, protectiveOrb, meteor,
                warning, minimapMaterial);
            ConfigureSoundFeedback();
            AddBossToForestPool();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("Goblin Priest boss, attacks, projectiles, Animator, and Forest boss pool configured.");
        }

        [MenuItem("SoulKnight3D/Configure Goblin Priest Feedback And Dissolve")]
        public static void ConfigureFeedbackAndDissolve()
        {
            EnsureDissolveShaderEmission();
            ConfigurePriestDissolveMaterial();
            ConfigureSoundFeedback();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Goblin Priest feedback and dissolve configured.");
        }

        [MenuItem("SoulKnight3D/Validate Goblin Priest Boss")]
        public static void Validate()
        {
            GameObject boss = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (boss == null) { throw new InvalidOperationException("Goblin Priest prefab is missing."); }

            GoblinPriest priest = boss.GetComponent<GoblinPriest>();
            Rigidbody body = boss.GetComponent<Rigidbody>();
            CapsuleCollider capsule = boss.GetComponent<CapsuleCollider>();
            Animator animator = boss.GetComponentInChildren<Animator>(true);
            if (priest == null || body == null || capsule == null || animator == null)
            {
                throw new InvalidOperationException(
                    "Goblin Priest is missing its gameplay, Rigidbody, collider, or Animator component.");
            }
            if (animator.gameObject != priest.gameObject)
            {
                throw new InvalidOperationException(
                    "Goblin Priest animation events cannot reach the gameplay component.");
            }

            SerializedObject serializedPriest = new SerializedObject(priest);
            string[] referenceProperties =
            {
                "_rigidbody",
                "_collider",
                "_animator",
                "_attackOrigin",
                "_minimapIcon",
                "_swirlBulletPrefab",
                "_splitterParentBulletPrefab",
                "_protectiveOrbPrefab",
                "_meteorPrefab",
                "_meteorWarningPrefab",
                "_soundFeedback",
                "_splitterAttackSound",
                "_lavaAttackSound",
                "_starFallAndProtectiveOrbSound",
                "_enragedSound",
                "_deathSound"
            };
            for (int i = 0; i < referenceProperties.Length; i++)
            {
                SerializedProperty property =
                    serializedPriest.FindProperty(referenceProperties[i]);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        $"Goblin Priest reference '{referenceProperties[i]}' is not assigned.");
                }
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            string[] triggers =
            {
                "LavaBulletAttack",
                "SplitBulletAttack",
                "ProtectiveOrbAttack",
                "StarFallAttack",
                "Die"
            };
            if (controller == null || triggers.Any(trigger =>
                !controller.parameters.Any(parameter =>
                    parameter.name == trigger &&
                    parameter.type == AnimatorControllerParameterType.Trigger)))
            {
                throw new InvalidOperationException(
                    "Goblin Priest Animator trigger setup is incomplete.");
            }

            ValidateAnimationEvent("LavaBulletAttack", "AnimationLavaBulletAttack");
            ValidateAnimationEvent("SplitBulletAttack", "AnimationSplitBulletAttack");
            ValidateAnimationEvent("ProtectiveOrbAttack", "AnimationProtectiveOrbAttack");
            ValidateAnimationEvent("StarFallAttack", "AnimationStarFallAttack");

            GameFloorSO forest = AssetDatabase.LoadAssetAtPath<GameFloorSO>(ForestFloorPath);
            BossEncounterDataSO encounter =
                AssetDatabase.LoadAssetAtPath<BossEncounterDataSO>(BossEncounterPath);
            if (forest == null || encounter == null ||
                !forest.BossPool.Any(entry => entry != null && entry.Boss == encounter))
            {
                throw new InvalidOperationException(
                    "Goblin Priest is not registered in the Forest boss pool.");
            }

            Debug.Log("Goblin Priest validation passed.");
        }

        private static void ConfigureBossPrefab(GameObject swirlBullet,
            GameObject splitterParent,
            GameObject protectiveOrb, GameObject meteor, GameObject warning,
            Material minimapMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                root.layer = LayerMask.NameToLayer("Enemy");
                root.tag = "Enemy";

                Animator animator = root.GetComponentInChildren<Animator>(true);
                Rigidbody body = GetOrAdd<Rigidbody>(root);
                body.mass = 5f;
                body.useGravity = true;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(root);
                capsule.isTrigger = false;
                capsule.center = new Vector3(0f, 0.72f, 0f);
                capsule.height = 1.45f;
                capsule.radius = 0.38f;

                Transform attackOrigin = FindOrCreateChild(root.transform, "AttackOrigin");
                attackOrigin.localPosition = new Vector3(0f, 0.9f, 0.4f);
                attackOrigin.localRotation = Quaternion.identity;

                Transform minimapIcon = root.transform.Find("MinimapIcon");
                if (minimapIcon == null)
                {
                    GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    icon.name = "MinimapIcon";
                    Object.DestroyImmediate(icon.GetComponent<Collider>());
                    icon.transform.SetParent(root.transform, false);
                    minimapIcon = icon.transform;
                }
                minimapIcon.gameObject.layer = LayerMask.NameToLayer("Minimap");
                minimapIcon.localPosition = new Vector3(0f, 2.2f, 0f);
                minimapIcon.localRotation = Quaternion.identity;
                minimapIcon.localScale = new Vector3(0.22f, 0.01f, 0.22f);
                Renderer iconRenderer = minimapIcon.GetComponent<Renderer>();
                if (iconRenderer != null) { iconRenderer.sharedMaterial = minimapMaterial; }

                GoblinPriest priest = GetOrAdd<GoblinPriest>(root);
                priest.MaxHealth = 450;
                priest.Speed = 1.2f;
                priest.ConfigureReferences(body, capsule, animator, attackOrigin, minimapIcon,
                    swirlBullet, splitterParent, protectiveOrb, meteor, warning);

                EditorUtility.SetDirty(priest);
                PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureSoundFeedback()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                GoblinPriest priest = root.GetComponent<GoblinPriest>();
                Transform templateTransform = root.transform.Find("SoundFeedbacks");
                MMF_Player template = templateTransform != null
                    ? templateTransform.GetComponent<MMF_Player>()
                    : null;
                if (priest == null || template == null)
                {
                    throw new InvalidOperationException(
                        "Goblin Priest requires its gameplay component and the SoundFeedbacks template.");
                }

                string[] obsoleteFeedbackObjects =
                {
                    "LavaSoundFeedbacks",
                    "StarFallSoundFeedbacks",
                    "EnragedSoundFeedbacks",
                    "DeathSoundFeedbacks"
                };
                for (int i = 0; i < obsoleteFeedbackObjects.Length; i++)
                {
                    Transform obsolete = root.transform.Find(obsoleteFeedbackObjects[i]);
                    if (obsolete != null) { Object.DestroyImmediate(obsolete.gameObject); }
                }

                AudioClip splitter = LoadAudioClip("fx_boss8_atk1");
                AudioClip lava = LoadAudioClip("fx_boss8_atk2");
                AudioClip starFall = LoadAudioClip("fx_boss8_atk3");
                AudioClip enraged = LoadAudioClip("fx_boss8_angry");
                AudioClip death = LoadAudioClip("fx_boss8_dead");
                MMF_MMSoundManagerSound sound = template.FeedbacksList?
                    .OfType<MMF_MMSoundManagerSound>().FirstOrDefault();
                if (sound == null)
                {
                    throw new InvalidOperationException(
                        "SoundFeedbacks requires an MMSoundManager sound feedback.");
                }
                sound.Owner = template;
                sound.Sfx = splitter;

                priest.ConfigureSoundFeedback(template, splitter, lava, starFall,
                    enraged, death);
                SerializedObject serializedPriest = new SerializedObject(priest);
                serializedPriest.FindProperty("_enrageEnergyOrbCount").intValue = 10;
                serializedPriest.FindProperty("_dissolveDelay").floatValue = 3f;
                serializedPriest.FindProperty("_dissolveDuration").floatValue = 3f;
                SerializedProperty minimapIconProperty =
                    serializedPriest.FindProperty("_minimapIcon");
                if (minimapIconProperty.objectReferenceValue == null)
                {
                    Transform minimapIcon = root.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(candidate =>
                            candidate.name == "EnemyMinimapIcon" ||
                            candidate.name == "MinimapIcon");
                    minimapIconProperty.objectReferenceValue = minimapIcon;
                }
                serializedPriest.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(priest);
                PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AudioClip LoadAudioClip(string clipName)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"{AudioFolder}/{clipName}.wav");
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Could not load Goblin Priest audio clip '{clipName}'.");
            }
            return clip;
        }

        private static void EnsureDissolveShaderEmission()
        {
            EnsureFolder("Assets/Art/Shaders");
            if (!File.Exists(Path.GetFullPath(DissolveShaderPath)) &&
                !AssetDatabase.CopyAsset(DissolveShaderTemplatePath, DissolveShaderPath))
            {
                throw new InvalidOperationException(
                    "Could not create the Goblin Priest dissolve shader variant.");
            }

            string fullPath = Path.GetFullPath(DissolveShaderPath);
            JObject graph = JObject.Parse(File.ReadAllText(fullPath));
            JArray properties = (JArray)graph["m_SerializedProperties"];
            if (properties.Any(entry =>
                ParseJsonData(entry)["m_OverrideReferenceName"]?.Value<string>() ==
                "_EmissionMap"))
            {
                return;
            }

            const string emissionMapPropertyGuid =
                "dbd7886a-c003-4747-a69d-fcdb36a7e64f";
            const string emissionColorPropertyGuid =
                "ef8d7caf-1140-4e96-8f51-96db7a595932";
            const string emissionMapNodeGuid =
                "a05510d6-8b30-45dc-a1c0-195f1304d6f3";
            const string emissionColorNodeGuid =
                "ad46648c-a0a9-4ea9-bd4b-cf927d14a035";
            const string sampleNodeGuid =
                "84043c2b-374e-4db6-aa48-b92712f22c20";
            const string multiplyNodeGuid =
                "81950d84-b956-4340-82dd-c6a98443e822";
            const string addNodeGuid =
                "8eab5ff7-3b9c-484e-ab31-d89375ac06f8";
            const string masterNodeGuid =
                "0a6384c8-e81f-40ce-926b-1c013e55e9f1";
            const string dissolveNodeGuid =
                "c99e03f6-7e17-4faf-ada1-f2e6f24302f7";

            properties.Add(CreateTextureProperty(emissionMapPropertyGuid,
                "EmissionMap", "_EmissionMap"));
            properties.Add(CreateColorProperty(emissionColorPropertyGuid,
                "EmissionColor", "_EmissionColor"));

            JArray nodes = (JArray)graph["m_SerializableNodes"];
            JObject baseMapPropertyNode = FindNode(nodes,
                "a24f45e1-72c8-4172-9c12-d6c6f6e72a8d");
            JObject edgeColorPropertyNode = FindNode(nodes,
                "4f6ea8f4-8277-4d00-b5f8-04dfcf9d5540");
            nodes.Add(ClonePropertyNode(baseMapPropertyNode, emissionMapNodeGuid,
                emissionMapPropertyGuid, "EmissionMap", -400f, 780f));
            nodes.Add(ClonePropertyNode(edgeColorPropertyNode, emissionColorNodeGuid,
                emissionColorPropertyGuid, "EmissionColor", -400f, 900f));

            JObject pbrSubGraph = JObject.Parse(
                File.ReadAllText(Path.GetFullPath(PbrSubGraphPath)));
            JArray templateNodes = (JArray)pbrSubGraph["m_SerializableNodes"];
            JObject sampleTemplate = templateNodes.Children<JObject>().First(entry =>
                entry["typeInfo"]?["fullName"]?.Value<string>() ==
                "UnityEditor.ShaderGraph.SampleTexture2DNode" &&
                ParseJsonData(entry)["m_TextureType"]?.Value<int>() == 0);
            JObject multiplyTemplate = templateNodes.Children<JObject>().First(entry =>
                entry["typeInfo"]?["fullName"]?.Value<string>() ==
                "UnityEditor.ShaderGraph.MultiplyNode");
            JObject addTemplate = templateNodes.Children<JObject>().First(entry =>
                entry["typeInfo"]?["fullName"]?.Value<string>() ==
                "UnityEditor.ShaderGraph.AddNode");
            nodes.Add(CloneNode(sampleTemplate, sampleNodeGuid, -180f, 720f));
            nodes.Add(CloneNode(multiplyTemplate, multiplyNodeGuid, 40f, 760f));
            nodes.Add(CloneNode(addTemplate, addNodeGuid, 260f, 400f));

            JArray edges = (JArray)graph["m_SerializableEdges"];
            JObject directEmissionEdge = edges.Children<JObject>().First(entry =>
            {
                JObject data = ParseJsonData(entry);
                return NodeGuid(data["m_OutputSlot"]) == dissolveNodeGuid &&
                    data["m_OutputSlot"]?["m_SlotId"]?.Value<int>() == 1 &&
                    NodeGuid(data["m_InputSlot"]) == masterNodeGuid &&
                    data["m_InputSlot"]?["m_SlotId"]?.Value<int>() == 4;
            });
            edges.Remove(directEmissionEdge);
            JObject edgeTemplate = (JObject)edges[0];
            edges.Add(CreateEdge(edgeTemplate, emissionMapNodeGuid, 0,
                sampleNodeGuid, 1));
            edges.Add(CreateEdge(edgeTemplate, sampleNodeGuid, 0,
                multiplyNodeGuid, 0));
            edges.Add(CreateEdge(edgeTemplate, emissionColorNodeGuid, 0,
                multiplyNodeGuid, 1));
            edges.Add(CreateEdge(edgeTemplate, dissolveNodeGuid, 1,
                addNodeGuid, 0));
            edges.Add(CreateEdge(edgeTemplate, multiplyNodeGuid, 2,
                addNodeGuid, 1));
            edges.Add(CreateEdge(edgeTemplate, addNodeGuid, 2,
                masterNodeGuid, 4));

            File.WriteAllText(fullPath, graph.ToString(Formatting.Indented));
            AssetDatabase.ImportAsset(DissolveShaderPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigurePriestDissolveMaterial()
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(PriestMaterialPath);
            Shader dissolveShader =
                AssetDatabase.LoadAssetAtPath<Shader>(DissolveShaderPath);
            if (material == null || dissolveShader == null)
            {
                throw new InvalidOperationException(
                    "Goblin Priest material or dissolve shader is missing.");
            }

            Texture baseMap = material.GetTexture("_BaseMap");
            Vector2 baseScale = material.GetTextureScale("_BaseMap");
            Vector2 baseOffset = material.GetTextureOffset("_BaseMap");
            Texture emissionMap = material.GetTexture("_EmissionMap");
            Vector2 emissionScale = material.GetTextureScale("_EmissionMap");
            Vector2 emissionOffset = material.GetTextureOffset("_EmissionMap");
            Color baseColor = material.GetColor("_BaseColor");
            Color emissionColor = material.GetColor("_EmissionColor");
            float metallic = material.GetFloat("_Metallic");
            float smoothness = material.GetFloat("_Smoothness");

            material.shader = dissolveShader;
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", baseScale);
            material.SetTextureOffset("_BaseMap", baseOffset);
            material.SetTexture("_EmissionMap", emissionMap);
            material.SetTextureScale("_EmissionMap", emissionScale);
            material.SetTextureOffset("_EmissionMap", emissionOffset);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emissionColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Dissolve", 0f);
            material.SetFloat("_NoiseScale", 50f);
            material.SetFloat("_EdgeWidth", 0.05f);
            material.SetColor("_EdgeColor", new Color(4f, 0.25f, 0f, 1f));
            material.SetFloat("_EdgeColorIntensity", 1f);
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
        }

        private static JObject CreateTextureProperty(string guid, string name,
            string referenceName)
        {
            JObject data = new JObject
            {
                ["m_Guid"] = new JObject { ["m_GuidSerialized"] = guid },
                ["m_Name"] = name,
                ["m_DefaultReferenceName"] = $"Texture2D_{guid.Substring(0, 8)}",
                ["m_OverrideReferenceName"] = referenceName,
                ["m_GeneratePropertyBlock"] = true,
                ["m_Precision"] = 0,
                ["m_GPUInstanced"] = false,
                ["m_Hidden"] = false,
                ["m_Value"] = new JObject
                {
                    ["m_SerializedTexture"] = "{\"texture\":{\"instanceID\":0}}",
                    ["m_Guid"] = string.Empty
                },
                ["m_Modifiable"] = true,
                ["m_DefaultType"] = 1
            };
            return WrapJsonData(
                "UnityEditor.ShaderGraph.Internal.Texture2DShaderProperty", data);
        }

        private static JObject CreateColorProperty(string guid, string name,
            string referenceName)
        {
            JObject data = new JObject
            {
                ["m_Guid"] = new JObject { ["m_GuidSerialized"] = guid },
                ["m_Name"] = name,
                ["m_DefaultReferenceName"] = $"Color_{guid.Substring(0, 8)}",
                ["m_OverrideReferenceName"] = referenceName,
                ["m_GeneratePropertyBlock"] = true,
                ["m_Precision"] = 0,
                ["m_GPUInstanced"] = false,
                ["m_Hidden"] = false,
                ["m_Value"] = new JObject
                {
                    ["r"] = 0f,
                    ["g"] = 0f,
                    ["b"] = 0f,
                    ["a"] = 1f
                },
                ["m_ColorMode"] = 1
            };
            return WrapJsonData(
                "UnityEditor.ShaderGraph.Internal.ColorShaderProperty", data);
        }

        private static JObject ClonePropertyNode(JObject template, string nodeGuid,
            string propertyGuid, string displayName, float x, float y)
        {
            JObject clone = CloneNode(template, nodeGuid, x, y);
            JObject data = ParseJsonData(clone);
            data["m_PropertyGuidSerialized"] = propertyGuid;
            JObject slot = ParseJsonData(data["m_SerializableSlots"][0]);
            slot["m_DisplayName"] = displayName;
            data["m_SerializableSlots"][0]["JSONnodeData"] =
                slot.ToString(Formatting.Indented);
            clone["JSONnodeData"] = data.ToString(Formatting.Indented);
            return clone;
        }

        private static JObject CloneNode(JObject template, string nodeGuid,
            float x, float y)
        {
            JObject clone = (JObject)template.DeepClone();
            JObject data = ParseJsonData(clone);
            data["m_GuidSerialized"] = nodeGuid;
            data["m_DrawState"]["m_Position"]["x"] = x;
            data["m_DrawState"]["m_Position"]["y"] = y;
            clone["JSONnodeData"] = data.ToString(Formatting.Indented);
            return clone;
        }

        private static JObject CreateEdge(JObject template, string outputNode,
            int outputSlot, string inputNode, int inputSlot)
        {
            JObject edge = (JObject)template.DeepClone();
            JObject data = ParseJsonData(edge);
            data["m_OutputSlot"]["m_NodeGUIDSerialized"] = outputNode;
            data["m_OutputSlot"]["m_SlotId"] = outputSlot;
            data["m_InputSlot"]["m_NodeGUIDSerialized"] = inputNode;
            data["m_InputSlot"]["m_SlotId"] = inputSlot;
            edge["JSONnodeData"] = data.ToString(Formatting.Indented);
            return edge;
        }

        private static JObject FindNode(JArray nodes, string nodeGuid)
        {
            return nodes.Children<JObject>().First(entry =>
                ParseJsonData(entry)["m_GuidSerialized"]?.Value<string>() == nodeGuid);
        }

        private static string NodeGuid(JToken slot)
        {
            return slot?["m_NodeGUIDSerialized"]?.Value<string>();
        }

        private static JObject ParseJsonData(JToken wrapper)
        {
            return JObject.Parse(wrapper["JSONnodeData"].Value<string>());
        }

        private static JObject WrapJsonData(string typeName, JObject data)
        {
            return new JObject
            {
                ["typeInfo"] = new JObject { ["fullName"] = typeName },
                ["JSONnodeData"] = data.ToString(Formatting.Indented)
            };
        }

        private static void ConfigureController()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Missing AnimatorController: {ControllerPath}");
            }

            AddTrigger(controller, "LavaBulletAttack");
            AddTrigger(controller, "SplitBulletAttack");
            AddTrigger(controller, "ProtectiveOrbAttack");
            AddTrigger(controller, "StarFallAttack");
            AddTrigger(controller, "Die");

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState move = FindState(stateMachine, "Move");
            AnimatorState death = FindState(stateMachine, "DeathForward");
            string[] attacks =
            {
                "LavaBulletAttack",
                "SplitBulletAttack",
                "ProtectiveOrbAttack",
                "StarFallAttack"
            };

            for (int i = 0; i < attacks.Length; i++)
            {
                AnimatorState attack = FindState(stateMachine, attacks[i]);
                AddTriggerTransition(move, attack, attacks[i], 0.1f);
                AddExitTransition(attack, move, 0.95f, 0.12f);
            }
            AddAnyStateTriggerTransition(stateMachine, death, "Die", 0.08f);

            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureAnimationEvents()
        {
            SetAnimationEvent("LavaBulletAttack", "AnimationLavaBulletAttack");
            SetAnimationEvent("SplitBulletAttack", "AnimationSplitBulletAttack");
            SetAnimationEvent("ProtectiveOrbAttack", "AnimationProtectiveOrbAttack");
            SetAnimationEvent("StarFallAttack", "AnimationStarFallAttack");
        }

        private static void SetAnimationEvent(string clipName, string functionName)
        {
            string path = $"{AnimationFolder}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                throw new InvalidOperationException($"Missing animation clip: {path}");
            }

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Length == 0)
            {
                events = new[]
                {
                    new AnimationEvent
                    {
                        time = clip.length * 0.5f,
                        functionName = functionName
                    }
                };
            }
            else
            {
                events[0].functionName = functionName;
            }
            AnimationUtility.SetAnimationEvents(clip, events);
            EditorUtility.SetDirty(clip);
        }

        private static void ValidateAnimationEvent(string clipName, string functionName)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{AnimationFolder}/{clipName}.anim");
            if (clip == null || !AnimationUtility.GetAnimationEvents(clip)
                .Any(animationEvent => animationEvent.functionName == functionName))
            {
                throw new InvalidOperationException(
                    $"Animation '{clipName}' does not invoke '{functionName}'.");
            }
        }

        private static void AddBossToForestPool()
        {
            GameFloorSO forest = AssetDatabase.LoadAssetAtPath<GameFloorSO>(ForestFloorPath);
            BossEncounterDataSO encounter =
                AssetDatabase.LoadAssetAtPath<BossEncounterDataSO>(BossEncounterPath);
            if (forest == null || encounter == null) { return; }

            if (!forest.BossPool.Any(entry => entry != null && entry.Boss == encounter))
            {
                forest.BossPool.Add(new WeightedBossEncounter
                {
                    Boss = encounter,
                    Weight = 1f
                });
                EditorUtility.SetDirty(forest);
            }
        }

        private static GameObject CreateBulletPrefab(string name, PrimitiveType primitive,
            Vector3 scale, Material material, Action<GameObject> configure = null)
        {
            string path = $"{ProjectileFolder}/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { return existing; }

            GameObject root = GameObject.CreatePrimitive(primitive);
            root.name = name;
            root.layer = LayerMask.NameToLayer("EnemyBullet");
            root.tag = "Enemy";
            root.transform.localScale = scale;
            Object.DestroyImmediate(root.GetComponent<Collider>());

            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer != null) { renderer.sharedMaterial = material; }

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.radius = 0.5f;
            capsule.height = 1f;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 0.1f;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Bullet bullet = root.AddComponent<Bullet>();
            bullet.SelfRigidbody = body;
            bullet.SelfCapsuleCollider = capsule;
            configure?.Invoke(root);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject ConfigureLineBulletPrefab(Material material)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LineBulletPrefabPath);
            if (prefab == null)
            {
                return CreateBulletPrefab("Priest Line Bullet", PrimitiveType.Cube,
                    new Vector3(0.18f, 0.18f, 0.18f), material);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(LineBulletPrefabPath);
            try
            {
                root.name = "Priest Line Bullet";
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

                Bullet bullet = root.GetComponent<Bullet>();
                if (bullet != null)
                {
                    SerializedObject serializedBullet = new SerializedObject(bullet);
                    SerializedProperty scriptName =
                        serializedBullet.FindProperty("ScriptName");
                    if (scriptName != null)
                    {
                        scriptName.stringValue = "Priest Line Bullet";
                        serializedBullet.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                Renderer renderer = root.GetComponentInChildren<Renderer>(true);
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }
                PrefabUtility.SaveAsPrefabAsset(root, LineBulletPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(LineBulletPrefabPath);
        }

        private static GameObject CreateSplitterClonePrefab(Material material,
            GameObject lineBulletPrefab)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(SplitterClonePrefabPath);
            if (existing != null) { return existing; }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Priest Splitter Swirl Clone";
            root.layer = LayerMask.NameToLayer("EnemyBullet");
            root.tag = "Enemy";
            root.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);

            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer != null) { renderer.sharedMaterial = material; }
            Collider collider = root.GetComponent<Collider>();
            collider.isTrigger = false;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;

            root.AddComponent<PooledGameObject>();
            PriestSplitterClone clone = root.AddComponent<PriestSplitterClone>();
            clone.Configure(lineBulletPrefab, 0.3f, 7f, 2, 5f);

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(root, SplitterClonePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateProtectiveOrbPrefab(Material material)
        {
            string path = $"{ProjectileFolder}/Priest Protective Orb.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { return existing; }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Priest Protective Orb";
            root.layer = LayerMask.NameToLayer("EnemyBullet");
            root.tag = "Enemy";
            root.transform.localScale = Vector3.one * 0.28f;
            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer != null) { renderer.sharedMaterial = material; }
            SphereCollider collider = root.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            root.AddComponent<PriestOrbitalProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateMeteorPrefab(Material material)
        {
            string path = $"{ProjectileFolder}/Priest Meteor.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { return existing; }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Priest Meteor";
            root.layer = LayerMask.NameToLayer("EnemyBullet");
            root.tag = "Enemy";
            root.transform.localScale = new Vector3(0.28f, 0.6f, 0.28f);
            Object.DestroyImmediate(root.GetComponent<Collider>());
            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer != null) { renderer.sharedMaterial = material; }
            root.AddComponent<PriestMeteorProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateWarningPrefab(Material material)
        {
            string path = $"{ProjectileFolder}/Priest Meteor Warning.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { return existing; }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Priest Meteor Warning";
            root.layer = 0;
            root.transform.localScale = new Vector3(0.1f, 0.015f, 0.1f);
            Object.DestroyImmediate(root.GetComponent<Collider>());
            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material GetOrCreateMaterial(string path, Color color, bool emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = FindLitShader();
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateWarningMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = FindLitShader();
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = new Color(1f, 0f, 0f, 0.48f);
            material.renderQueue = 3000;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", material.color);
            }
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindLitShader()
        {
            Material projectMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Art/Materials/Enemy/Goblin Priest.mat");
            Shader shader = projectMaterial != null ? projectMaterial.shader : null;
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                throw new InvalidOperationException("No supported lit shader is available.");
            }
            return shader;
        }

        private static void AddTrigger(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(parameter => parameter.name == name)) { return; }
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            AnimatorState state = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(candidate => candidate.name == name);
            if (state == null)
            {
                throw new InvalidOperationException($"Animator state '{name}' was not found.");
            }
            return state;
        }

        private static void AddTriggerTransition(AnimatorState source, AnimatorState destination,
            string parameter, float duration)
        {
            if (source.transitions.Any(transition =>
                transition.destinationState == destination &&
                transition.conditions.Any(condition => condition.parameter == parameter)))
            {
                return;
            }

            AnimatorStateTransition result = source.AddTransition(destination);
            result.hasExitTime = false;
            result.duration = duration;
            result.hasFixedDuration = true;
            result.canTransitionToSelf = false;
            result.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitTransition(AnimatorState source, AnimatorState destination,
            float exitTime, float duration)
        {
            if (source.transitions.Any(transition =>
                transition.destinationState == destination && transition.hasExitTime))
            {
                return;
            }

            AnimatorStateTransition result = source.AddTransition(destination);
            result.hasExitTime = true;
            result.exitTime = exitTime;
            result.duration = duration;
            result.hasFixedDuration = true;
            result.canTransitionToSelf = false;
        }

        private static void AddAnyStateTriggerTransition(AnimatorStateMachine stateMachine,
            AnimatorState destination, string parameter, float duration)
        {
            if (stateMachine.anyStateTransitions.Any(transition =>
                transition.destinationState == destination &&
                transition.conditions.Any(condition => condition.parameter == parameter)))
            {
                return;
            }

            AnimatorStateTransition result = stateMachine.AddAnyStateTransition(destination);
            result.hasExitTime = false;
            result.duration = duration;
            result.hasFixedDuration = true;
            result.canTransitionToSelf = false;
            result.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) { return child; }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void MigrateAsset(string oldPath, string newPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null ||
                AssetDatabase.LoadAssetAtPath<Object>(oldPath) == null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(
                    $"Could not rename '{oldPath}' to '{newPath}': {error}");
            }
        }
    }
}
#endif
