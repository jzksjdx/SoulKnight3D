#if UNITY_EDITOR
using System;
using System.Linq;
using MoreMountains.Feedbacks;
using SoulKnight3D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VoidSummonerAssetBuilder
{
    private const string SummonerPrefabPath =
        "Assets/Art/Prefab/Characters/Enemy/Void Summoner.prefab";
    private const string HandPrefabPath =
        "Assets/Art/Prefab/Characters/Enemy/Void Summoner Hand.prefab";
    private const string OrbPrefabPath =
        "Assets/Art/Prefab/Bullets/VoidSummonerOrb.prefab";
    private const string SlowCirclePrefabPath =
        "Assets/Art/Prefab/Particle/FX_VoidSummonerSlowCircle.prefab";
    private const string GripStatusPrefabPath =
        "Assets/Art/Prefab/StatusPrefabs/VoidSummonerGripStatus.prefab";
    private const string SlowStatusPrefabPath =
        "Assets/Art/Prefab/StatusPrefabs/VoidSummonerSlowStatus.prefab";
    private const string SummonerControllerPath =
        "Assets/Art/Animation/Void Summoner/Void Summoner.controller";
    private const string HandControllerPath =
        "Assets/Art/Animation/Void Summoner/Hand/Void Summoner Hand.controller";
    private const string SummonClipPath =
        "Assets/Art/Animation/Void Summoner/Summon.anim";
    private const string OrbClipPath =
        "Assets/Art/Animation/Goblin Priest/SplitBulletAttack.anim";
    private const string HandDieClipPath =
        "Assets/Art/Animation/Void Summoner/Hand/Die.anim";
    private const string SoundTemplatePrefabPath =
        "Assets/Art/Prefab/Characters/Boss/Goblin Priest.prefab";
    private const string SpawnProfilePath =
        "Assets/Art/ScriptableObject/EnemyWaves/Forest Spawn Profile.asset";
    private const string AudioFolder = "Assets/Art/Audio";
    private const string HandIconPath =
        "Assets/Art/Sprites/summoner_hand.png";

    [MenuItem("Tools/Soul Knight/Build Void Summoner")]
    public static void Build()
    {
        ValidateSourceAssets();

        GameObject gripStatus = ConfigureGripStatus();
        GameObject slowStatus = ConfigureSlowStatus();
        ConfigureSummonerController();
        ConfigureHandController();
        ConfigureSlowCircle(slowStatus);
        ConfigureOrb();
        ConfigureHand(gripStatus);
        ConfigureSummoner();
        AddToForestSpawnProfile();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log(
            "Void Summoner enemy, minions, attacks, statuses, audio, " +
            "Animators, pooling, and Forest 1-4/1-5 spawning configured.");
    }

    [MenuItem("Tools/Soul Knight/Validate Void Summoner")]
    public static void Validate()
    {
        GameObject summoner =
            AssetDatabase.LoadAssetAtPath<GameObject>(SummonerPrefabPath);
        GameObject hand =
            AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefabPath);
        GameObject orb =
            AssetDatabase.LoadAssetAtPath<GameObject>(OrbPrefabPath);
        GameObject circle =
            AssetDatabase.LoadAssetAtPath<GameObject>(SlowCirclePrefabPath);
        if (summoner == null || hand == null || orb == null || circle == null)
        {
            throw new InvalidOperationException(
                "One or more Void Summoner prefabs are missing.");
        }

        VoidSummoner behavior = summoner.GetComponent<VoidSummoner>();
        if (behavior == null || summoner.GetComponent<Rigidbody>() == null ||
            summoner.GetComponent<CapsuleCollider>() == null ||
            summoner.GetComponent<Animator>() == null)
        {
            throw new InvalidOperationException(
                "Void Summoner prefab gameplay or physics setup is incomplete.");
        }

        string[] summonerReferences =
        {
            "_attackOrigin",
            "_handPrefab",
            "_orbPrefab",
            "_shieldVisual",
            "_soundFeedback",
            "_summonSound",
            "_orbSound",
            "_deathSound"
        };
        ValidateReferences(behavior, summonerReferences);

        VoidSummonerHand handBehavior =
            hand.GetComponent<VoidSummonerHand>();
        if (handBehavior == null ||
            hand.GetComponent<PooledGameObject>() == null)
        {
            throw new InvalidOperationException(
                "Void Summoner Hand is not configured as a pooled minion.");
        }
        ValidateReferences(handBehavior, new[]
        {
            "_rigidbody",
            "_collider",
            "_animator",
            "_gripStatusPrefab",
            "_deadFeedback"
        });

        if (orb.GetComponent<VoidSummonerOrb>() == null ||
            orb.GetComponent<PooledGameObject>() == null ||
            circle.GetComponent<VoidSummonerSlowCircle>() == null)
        {
            throw new InvalidOperationException(
                "Void Summoner orb or slow circle pooling setup is incomplete.");
        }

        AnimatorController summonerController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                SummonerControllerPath);
        AnimatorController handController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                HandControllerPath);
        ValidateParameter(summonerController, "Summon",
            AnimatorControllerParameterType.Trigger);
        ValidateParameter(summonerController, "Orb",
            AnimatorControllerParameterType.Trigger);
        ValidateParameter(summonerController, "Die",
            AnimatorControllerParameterType.Trigger);
        ValidateParameter(handController, "Gripped",
            AnimatorControllerParameterType.Bool);
        ValidateParameter(handController, "Die",
            AnimatorControllerParameterType.Trigger);

        AnimationClip summonClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(SummonClipPath);
        if (!AnimationUtility.GetAnimationEvents(summonClip)
            .Any(animationEvent =>
                animationEvent.functionName == "AnimationSummonHands"))
        {
            throw new InvalidOperationException(
                "Void Summoner summon animation event is missing.");
        }

        EnemySpawnProfileSO profile =
            AssetDatabase.LoadAssetAtPath<EnemySpawnProfileSO>(
                SpawnProfilePath);
        EnemySpawnEntry spawnEntry = profile?.Enemies.FirstOrDefault(
            entry => entry != null && entry.EnemyPrefab == summoner);
        if (spawnEntry == null || spawnEntry.MinLevel != 4 ||
            spawnEntry.MaxLevel != 5 ||
            spawnEntry.MaxCountPerWave != 1)
        {
            throw new InvalidOperationException(
                "Void Summoner is not correctly limited to Forest 1-4/1-5.");
        }

        Debug.Log("Void Summoner validation passed.");
    }

    private static void ValidateSourceAssets()
    {
        string[] required =
        {
            SummonerPrefabPath,
            HandPrefabPath,
            OrbPrefabPath,
            SlowCirclePrefabPath,
            SummonerControllerPath,
            HandControllerPath,
            SummonClipPath,
            OrbClipPath,
            HandDieClipPath,
            SoundTemplatePrefabPath,
            SpawnProfilePath,
            HandIconPath,
            AudioFolder + "/summon_hand.ogg",
            AudioFolder + "/fx_fire.wav",
            AudioFolder + "/fx_fire_emit01.wav",
            AudioFolder + "/fx_envoy_fireball.wav",
            AudioFolder + "/fx_dead3.wav"
        };

        foreach (string path in required)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                throw new InvalidOperationException(
                    $"Required Void Summoner asset is missing: {path}");
            }
        }
    }

    private static GameObject ConfigureGripStatus()
    {
        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(GripStatusPrefabPath);
        GameObject root = existing != null
            ? PrefabUtility.LoadPrefabContents(GripStatusPrefabPath)
            : new GameObject("VoidSummonerGripStatus");
        try
        {
            VoidSummonerGripStatus status =
                GetOrAdd<VoidSummonerGripStatus>(root);
            SerializedObject serialized = new SerializedObject(status);
            serialized.FindProperty("Type").enumValueIndex =
                (int)Status.StatusType.Restrained;
            serialized.FindProperty("_duration").floatValue = 60f;
            serialized.FindProperty("_weaponIcon").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(HandIconPath);
            serialized.FindProperty("_energyCostText").stringValue = "0";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, GripStatusPrefabPath);
        }
        finally
        {
            DisposePrefabRoot(root, existing != null);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(
            GripStatusPrefabPath);
    }

    private static GameObject ConfigureSlowStatus()
    {
        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(SlowStatusPrefabPath);
        GameObject root = existing != null
            ? PrefabUtility.LoadPrefabContents(SlowStatusPrefabPath)
            : new GameObject("VoidSummonerSlowStatus");
        try
        {
            SpeedBuff status = GetOrAdd<SpeedBuff>(root);
            status.Type = Status.StatusType.SpeedDown;
            status.SpeedChange = -5f / 9f;
            SerializedObject serialized = new SerializedObject(status);
            serialized.FindProperty("_duration").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, SlowStatusPrefabPath);
        }
        finally
        {
            DisposePrefabRoot(root, existing != null);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(
            SlowStatusPrefabPath);
    }

    private static void ConfigureSummonerController()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                SummonerControllerPath);
        EnsureParameter(controller, "Move",
            AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Summon",
            AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Orb",
            AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die",
            AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState move = FindState(machine, "Move");
        AnimatorState summon = FindState(machine, "SummonAttack");
        AnimatorState orb = FindState(machine, "OrbAttack");
        AnimatorState death = FindState(machine, "DeathForward");
        if (move == null || summon == null || orb == null || death == null)
        {
            throw new InvalidOperationException(
                "Void Summoner controller is missing Move, SummonAttack, " +
                "OrbAttack, or DeathForward.");
        }

        machine.defaultState = move;
        foreach (AnimatorStateTransition transition in
            machine.anyStateTransitions.ToArray())
        {
            machine.RemoveAnyStateTransition(transition);
        }
        RemoveTransitions(summon);
        RemoveTransitions(orb);
        RemoveTransitions(death);
        AddAnyStateTrigger(machine, death, "Die");
        AddAnyStateTrigger(machine, summon, "Summon");
        AddAnyStateTrigger(machine, orb, "Orb");
        AddExitTransition(summon, move);
        AddExitTransition(orb, move);

        AnimationClip summonClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(SummonClipPath);
        SetLooping(summonClip, false);
        SetLooping(death.motion as AnimationClip, false);
        AnimationUtility.SetAnimationEvents(summonClip, new[]
        {
            new AnimationEvent
            {
                time = 1.1666666f,
                functionName = "AnimationSummonHands"
            }
        });

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(summonClip);
    }

    private static void ConfigureHandController()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                HandControllerPath);
        EnsureParameter(controller, "Gripped",
            AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Die",
            AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = FindState(machine, "IdleAndMove");
        AnimatorState grip = FindState(machine, "Grip");
        AnimatorState death = FindState(machine, "Die");
        if (idle == null || grip == null || death == null)
        {
            throw new InvalidOperationException(
                "Void Summoner Hand controller is missing IdleAndMove, " +
                "Grip, or Die.");
        }

        machine.defaultState = idle;
        foreach (AnimatorStateTransition transition in
            machine.anyStateTransitions.ToArray())
        {
            machine.RemoveAnyStateTransition(transition);
        }
        RemoveTransitions(grip);
        RemoveTransitions(death);
        AddAnyStateTrigger(machine, death, "Die");

        AnimatorStateTransition enterGrip =
            machine.AddAnyStateTransition(grip);
        enterGrip.hasExitTime = false;
        enterGrip.duration = 0.08f;
        enterGrip.canTransitionToSelf = false;
        enterGrip.AddCondition(
            AnimatorConditionMode.If, 0f, "Gripped");

        AnimatorStateTransition exitGrip = grip.AddTransition(idle);
        exitGrip.hasExitTime = false;
        exitGrip.duration = 0.08f;
        exitGrip.canTransitionToSelf = false;
        exitGrip.AddCondition(
            AnimatorConditionMode.IfNot, 0f, "Gripped");

        SetLooping(
            AssetDatabase.LoadAssetAtPath<AnimationClip>(HandDieClipPath),
            false);
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureSlowCircle(GameObject slowStatus)
    {
        GameObject root =
            PrefabUtility.LoadPrefabContents(SlowCirclePrefabPath);
        try
        {
            int statusZoneLayer = LayerMask.NameToLayer("StatusZone");
            root.layer = statusZoneLayer;

            Transform colliderRoot =
                FindOrCreateChild(root.transform, "StatusCollider");
            colliderRoot.gameObject.layer = statusZoneLayer;
            colliderRoot.localPosition =
                root.transform.InverseTransformVector(Vector3.up * 0.5f);
            colliderRoot.localRotation = Quaternion.identity;
            Vector3 rootScale = root.transform.localScale;
            colliderRoot.localScale = new Vector3(
                SafeReciprocal(rootScale.x),
                SafeReciprocal(rootScale.y),
                SafeReciprocal(rootScale.z));

            SphereCollider collider =
                GetOrAdd<SphereCollider>(colliderRoot.gameObject);
            collider.isTrigger = true;
            collider.radius = 3f;
            collider.center = Vector3.zero;

            MMF_Player feedback = ConfigureSoundFeedback(
                root.transform, "SpawnFeedbacks",
                LoadAudio("fx_envoy_fireball"));
            VoidSummonerSlowCircle circle =
                GetOrAdd<VoidSummonerSlowCircle>(root);
            circle.Configure(collider, slowStatus, 3f, feedback);

            EditorUtility.SetDirty(circle);
            PrefabUtility.SaveAsPrefabAsset(root, SlowCirclePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureOrb()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OrbPrefabPath);
        try
        {
            root.layer = LayerMask.NameToLayer("EnemyBullet");
            root.tag = "Bullet";

            SphereCollider collider = GetOrAdd<SphereCollider>(root);
            collider.isTrigger = true;
            collider.radius = 0.2f;

            Rigidbody body = GetOrAdd<Rigidbody>(root);
            body.useGravity = false;
            body.isKinematic = true;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;
            GetOrAdd<PooledGameObject>(root);

            MMF_Player feedback = ConfigureSoundFeedback(
                root.transform, "ImpactFeedbacks",
                LoadAudio("fx_fire_emit01"));
            VoidSummonerOrb orb = GetOrAdd<VoidSummonerOrb>(root);
            orb.Configure(
                10f,
                2.5f,
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    SlowCirclePrefabPath),
                2,
                1.2f,
                0.25f,
                feedback);

            EditorUtility.SetDirty(orb);
            PrefabUtility.SaveAsPrefabAsset(root, OrbPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureHand(GameObject gripStatus)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HandPrefabPath);
        try
        {
            root.layer = LayerMask.NameToLayer("Enemy");
            root.tag = "Enemy";

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    HandControllerPath);
            animator.applyRootMotion = false;

            Rigidbody body = GetOrAdd<Rigidbody>(root);
            body.mass = 1f;
            body.drag = 1f;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(root);
            capsule.isTrigger = false;
            capsule.direction = 1;
            capsule.center = new Vector3(0f, 0.32f, 0f);
            capsule.height = 0.65f;
            capsule.radius = 0.28f;

            GetOrAdd<PooledGameObject>(root);
            Transform minimap = FindTransform(
                root.transform, "EnemyMinimapIcon");
            MMF_Player deadFeedback = ConfigureSoundFeedback(
                root.transform, "DeadFeedbacks",
                LoadAudio("fx_dead3"));

            VoidSummonerHand hand =
                GetOrAdd<VoidSummonerHand>(root);
            hand.MaxHealth = 12;
            hand.Speed = 1.35f;
            hand.ConfigureReferences(
                body, capsule, animator, minimap, gripStatus, deadFeedback);
            SerializedObject serialized = new SerializedObject(hand);
            serialized.FindProperty("_throwHitLayers").intValue =
                1 << LayerMask.NameToLayer("Enemy");
            serialized.FindProperty("_dashTriggerDistance").floatValue = 5f;
            serialized.FindProperty("_dashSpeed").floatValue = 40f;
            serialized.FindProperty("_dashArrivalDistance").floatValue = 0.1f;
            serialized.FindProperty("_dashCooldown").floatValue = 4f;
            serialized.FindProperty("_patrolDurationRandomness").floatValue =
                0.6f;
            serialized.FindProperty("_initialPatrolChance").floatValue =
                0.65f;
            serialized.FindProperty("_gripOffset").vector3Value =
                Vector3.zero;
            serialized.FindProperty("_throwSpeed").floatValue = 20f;
            serialized.FindProperty("_throwDuration").floatValue = 0.42f;
            serialized.FindProperty("_throwDamage").intValue = 6;
            serialized.FindProperty("_occupiedGripDamage").intValue = 2;
            serialized.FindProperty("_dissolveDelay").floatValue = 3f;
            serialized.FindProperty("_dissolveDuration").floatValue = 3f;
            serialized.FindProperty("_deathCollisionLayers").intValue =
                (1 << LayerMask.NameToLayer("Default")) |
                (1 << LayerMask.NameToLayer("RoomProps"));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(hand);
            PrefabUtility.SaveAsPrefabAsset(root, HandPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSummoner()
    {
        GameObject root =
            PrefabUtility.LoadPrefabContents(SummonerPrefabPath);
        try
        {
            root.layer = LayerMask.NameToLayer("Enemy");
            root.tag = "Enemy";

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    SummonerControllerPath);
            animator.applyRootMotion = false;

            Rigidbody body = GetOrAdd<Rigidbody>(root);
            body.mass = 3f;
            body.drag = 1f;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(root);
            capsule.isTrigger = false;
            capsule.direction = 1;
            capsule.center = new Vector3(0f, 0.55f, 0f);
            capsule.height = 1.1f;
            capsule.radius = 0.33f;

            Transform attackOrigin =
                FindOrCreateChild(root.transform, "AttackOrigin");
            attackOrigin.localPosition = new Vector3(0f, 0.82f, 0.42f);
            attackOrigin.localRotation = Quaternion.identity;
            Transform minimap = FindTransform(
                root.transform, "EnemyMinimapIcon");
            GameObject shield = FindTransform(
                root.transform, "ShieldSoftPurple")?.gameObject;

            MMF_Player soundFeedback = ConfigureSoundFeedback(
                root.transform, "SoundFeedbacks",
                LoadAudio("summon_hand"));
            VoidSummoner summoner = GetOrAdd<VoidSummoner>(root);
            summoner.MaxHealth = 16;
            summoner.Speed = 1f;
            summoner.Attack = 2;
            summoner.Range = 8f;
            summoner.State = Enemy.EnemyState.Patroling;
            summoner.ConfigureReferences(
                body,
                capsule,
                animator,
                attackOrigin,
                minimap,
                AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefabPath),
                AssetDatabase.LoadAssetAtPath<GameObject>(OrbPrefabPath),
                shield);
            summoner.ConfigureSounds(
                soundFeedback,
                LoadAudio("summon_hand"),
                LoadAudio("fx_fire"),
                LoadAudio("fx_dead3"));

            SerializedObject serialized = new SerializedObject(summoner);
            int environmentMask =
                (1 << LayerMask.NameToLayer("Default")) |
                (1 << LayerMask.NameToLayer("RoomProps"));
            serialized.FindProperty("_obstacleLayers").intValue =
                environmentMask;
            serialized.FindProperty("_floorLayers").intValue =
                environmentMask;
            serialized.FindProperty("_deadFeedbacks").objectReferenceValue =
                soundFeedback;
            serialized.FindProperty("_shieldLayers").intValue = 3;
            serialized.FindProperty("_shieldDamageCap").intValue = 1;
            serialized.FindProperty("_shieldWeaknessDuration").floatValue =
                2f;
            serialized.FindProperty("_orbShotInterval").floatValue = 0.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(summoner);
            PrefabUtility.SaveAsPrefabAsset(root, SummonerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AddToForestSpawnProfile()
    {
        EnemySpawnProfileSO profile =
            AssetDatabase.LoadAssetAtPath<EnemySpawnProfileSO>(
                SpawnProfilePath);
        GameObject summoner =
            AssetDatabase.LoadAssetAtPath<GameObject>(SummonerPrefabPath);
        EnemySpawnEntry entry = profile.Enemies.FirstOrDefault(
            candidate => candidate != null &&
                         candidate.EnemyPrefab == summoner);
        if (entry == null)
        {
            entry = new EnemySpawnEntry();
            profile.Enemies.Add(entry);
        }

        entry.EnemyPrefab = summoner;
        entry.ElitePrefab = null;
        entry.PointCost = 5;
        entry.WaveSize = 5;
        entry.Weight = 1;
        entry.MinLevel = 4;
        entry.MaxLevel = 5;
        entry.MaxCountPerWave = 1;
        entry.EliteChanceMultiplier = 1f;
        EditorUtility.SetDirty(profile);
    }

    private static MMF_Player ConfigureSoundFeedback(
        Transform parent, string name, AudioClip clip)
    {
        Transform existing = parent.Find(name);
        GameObject feedbackObject =
            existing != null ? existing.gameObject : null;
        if (feedbackObject == null)
        {
            GameObject templateRoot =
                PrefabUtility.LoadPrefabContents(
                    SoundTemplatePrefabPath);
            try
            {
                Transform template =
                    templateRoot.transform.Find("SoundFeedbacks");
                if (template == null)
                {
                    throw new InvalidOperationException(
                        "Goblin Priest SoundFeedbacks template is missing.");
                }
                feedbackObject = Object.Instantiate(
                    template.gameObject, parent);
                feedbackObject.name = name;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(templateRoot);
            }
        }

        MMF_Player player = feedbackObject.GetComponent<MMF_Player>();
        MMF_MMSoundManagerSound sound =
            player?.GetFeedbackOfType<MMF_MMSoundManagerSound>();
        if (player == null || sound == null)
        {
            throw new InvalidOperationException(
                $"{name} requires an MMSoundManager sound feedback.");
        }

        sound.Owner = player;
        sound.Sfx = clip;
        sound.SpatialBlend = 0.7f;
        EditorUtility.SetDirty(player);
        return player;
    }

    private static AudioClip LoadAudio(string name)
    {
        string[] extensions = { ".wav", ".ogg", ".mp3" };
        foreach (string extension in extensions)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                AudioFolder + "/" + name + extension);
            if (clip != null) { return clip; }
        }

        throw new InvalidOperationException(
            $"Void Summoner audio clip '{name}' is missing.");
    }

    private static T GetOrAdd<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Transform FindOrCreateChild(
        Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) { return child; }

        GameObject childObject = new GameObject(name);
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private static float SafeReciprocal(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
    }

    private static Transform FindTransform(
        Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == name);
    }

    private static void EnsureParameter(
        AnimatorController controller, string name,
        AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter existing =
            controller.parameters.FirstOrDefault(
                parameter => parameter.name == name);
        if (existing != null)
        {
            if (existing.type != type)
            {
                controller.RemoveParameter(existing);
                controller.AddParameter(name, type);
            }
            return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorState FindState(
        AnimatorStateMachine machine, string name)
    {
        return machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state.name == name);
    }

    private static void RemoveTransitions(AnimatorState state)
    {
        foreach (AnimatorStateTransition transition in
            state.transitions.ToArray())
        {
            state.RemoveTransition(transition);
        }
    }

    private static void AddAnyStateTrigger(
        AnimatorStateMachine machine, AnimatorState destination,
        string trigger)
    {
        AnimatorStateTransition transition =
            machine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(
            AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddExitTransition(
        AnimatorState source, AnimatorState destination)
    {
        AnimatorStateTransition transition =
            source.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
    }

    private static void SetLooping(
        AnimationClip clip, bool isLooping)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings =
            serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loop = settings?.FindPropertyRelative(
            "m_LoopTime");
        if (loop != null)
        {
            loop.boolValue = isLooping;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ValidateReferences(
        Component component, string[] propertyNames)
    {
        SerializedObject serialized = new SerializedObject(component);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    $"{component.GetType().Name} reference " +
                    $"'{propertyName}' is not assigned.");
            }
        }
    }

    private static void ValidateParameter(
        AnimatorController controller, string name,
        AnimatorControllerParameterType type)
    {
        if (controller == null ||
            !controller.parameters.Any(parameter =>
                parameter.name == name && parameter.type == type))
        {
            throw new InvalidOperationException(
                $"Animator parameter '{name}' is missing or has the wrong type.");
        }
    }

    private static void DisposePrefabRoot(
        GameObject root, bool loadedPrefabContents)
    {
        if (loadedPrefabContents)
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        else
        {
            Object.DestroyImmediate(root);
        }
    }
}
#endif
