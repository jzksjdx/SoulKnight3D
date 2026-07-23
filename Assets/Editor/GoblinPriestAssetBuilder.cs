#if UNITY_EDITOR
using System;
using System.Linq;
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
            Material emitterMaterial = GetOrCreateMaterial(
                $"{MaterialFolder}/Priest Line Emitter.mat",
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
            GameObject lineEmitter = CreateBulletPrefab(
                "Priest Line Emitter", PrimitiveType.Cube,
                new Vector3(0.18f, 0.18f, 0.18f), emitterMaterial,
                root =>
                {
                    PriestLineEmitter behavior = root.AddComponent<PriestLineEmitter>();
                    behavior.Configure(smallBullet, 0.45f, 5, 0.18f, 7f, 2);
                });
            GameObject protectiveOrb = CreateProtectiveOrbPrefab(orbMaterial);
            GameObject warning = CreateWarningPrefab(warningMaterial);
            GameObject meteor = CreateMeteorPrefab(meteorMaterial);

            ConfigureController();
            ConfigureAnimationEvents();
            ConfigureBossPrefab(swirlBullet, lineEmitter, protectiveOrb, meteor,
                warning, minimapMaterial);
            AddBossToForestPool();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("Goblin Priest boss, attacks, projectiles, Animator, and Forest boss pool configured.");
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
                "_lineEmitterPrefab",
                "_protectiveOrbPrefab",
                "_meteorPrefab",
                "_meteorWarningPrefab"
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
            if (forest == null || !forest.BossPrefabs.Contains(boss))
            {
                throw new InvalidOperationException(
                    "Goblin Priest is not registered in the Forest boss pool.");
            }

            Debug.Log("Goblin Priest validation passed.");
        }

        private static void ConfigureBossPrefab(GameObject swirlBullet, GameObject lineEmitter,
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
                    swirlBullet, lineEmitter, protectiveOrb, meteor, warning);

                EditorUtility.SetDirty(priest);
                PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (forest == null || bossPrefab == null) { return; }

            if (!forest.BossPrefabs.Contains(bossPrefab))
            {
                forest.BossPrefabs.Add(bossPrefab);
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
    }
}
#endif
