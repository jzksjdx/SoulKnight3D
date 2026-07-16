using System;
using System.Linq;
using SoulKnight3D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class GoblinGiantBuilder
{
    private const string EnemyPrefabPath = "Assets/Art/Prefab/Characters/Enemy/GoblinGiant.prefab";
    private const string StaffDataPath = "Assets/Art/ScriptableObject/Weapons/Goblin Giant Staff.asset";
    private const string ControllerPath = "Assets/Art/Animation/GiantGoblin/GoblinGiant.controller";
    private const string AttackClipPath = "Assets/Art/Animation/GiantGoblin/GiantGoblinAttack.anim";
    private const string MoveClipPath = "Assets/Art/Animation/GiantGoblin/GiantGoblinMove.anim";
    private const string IdleClipPath = "Assets/Art/Animation/GiantGoblin/GiantGoblinIdle.anim";
    private const string DieClipPath = "Assets/Art/Animation/GiantGoblin/GiantGoblinDie.anim";
    private const string MinimapIconPath = "Assets/Art/Prefab/Characters/EnemyMinimapIcon.prefab";
    private const string EnemyBulletPath = "Assets/Art/Prefab/Bullets/Enemy Normal Bullet.prefab";

    [MenuItem("Tools/Soul Knight/Build Goblin Giant")]
    private static void BuildFromMenu()
    {
        BuildActiveTestSceneEnemy(true);
    }

    private static void BuildActiveTestSceneEnemy(bool showDialogs)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "TestScene")
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Goblin Giant", "Open TestScene before building the Goblin Giant.", "OK");
            }
            return;
        }

        GameObject giant = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "GoblinGiant");
        if (giant == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Goblin Giant", "No root object named GoblinGiant was found in TestScene.", "OK");
            }
            return;
        }

        ConfigureAnimator(giant);
        ConfigureEnemy(giant);
        ConfigureStaff(giant);
        ConfigureMinimapIcon(giant);

        Vector3 scenePosition = giant.transform.position;
        Quaternion sceneRotation = giant.transform.rotation;
        giant.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        PrefabUtility.SaveAsPrefabAssetAndConnect(giant, EnemyPrefabPath, InteractionMode.AutomatedAction);
        giant.transform.SetPositionAndRotation(scenePosition, sceneRotation);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Completed Goblin Giant enemy and saved prefab at {EnemyPrefabPath}.");
    }

    private static void ConfigureEnemy(GameObject giant)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        giant.layer = enemyLayer >= 0 ? enemyLayer : 8;
        giant.tag = "Enemy";

        Rigidbody body = giant.GetComponent<Rigidbody>() ?? giant.AddComponent<Rigidbody>();
        body.mass = 2.5f;
        body.drag = 1f;
        body.angularDrag = 0.05f;
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        CapsuleCollider capsule = giant.GetComponent<CapsuleCollider>() ?? giant.AddComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.direction = 1;
        capsule.radius = 0.26f;
        capsule.height = 0.95f;
        capsule.center = new Vector3(0f, 0.475f, 0f);

        PistolEnemy enemy = giant.GetComponent<PistolEnemy>() ?? giant.AddComponent<PistolEnemy>();
        enemy.MaxHealth = 36;
        enemy.Speed = 0.75f;
        enemy.Attack = 4;
        enemy.Range = 8f;
        enemy.State = Enemy.EnemyState.Chasing;
        enemy.SelfRigidbody = body;
        enemy.SelfCollider = capsule;
        enemy.SelfAnimator = giant.GetComponent<Animator>();

        SerializedObject serializedEnemy = new SerializedObject(enemy);
        SerializedProperty deathCleanupDelay = serializedEnemy.FindProperty("_deathCleanupDelay");
        if (deathCleanupDelay != null)
        {
            deathCleanupDelay.floatValue = 3.8f;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureAnimator(GameObject giant)
    {
        Animator animator = giant.GetComponent<Animator>();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (animator == null || controller == null)
        {
            throw new InvalidOperationException("Goblin Giant requires an Animator and its animator controller asset.");
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        EnsureTrigger(controller, "Move");
        EnsureTrigger(controller, "Attack");
        EnsureTrigger(controller, "Die");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = FindState(stateMachine, "GiantGoblinIdle");
        AnimatorState move = FindState(stateMachine, "GiantGoblinMove");
        AnimatorState attack = FindState(stateMachine, "GiantGoblinAttack");
        AnimatorState die = FindState(stateMachine, "GiantGoblinDie");

        if (idle == null || move == null || attack == null || die == null)
        {
            throw new InvalidOperationException("Goblin Giant controller is missing one or more expected states.");
        }

        stateMachine.defaultState = idle;
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }
        RemoveTransitions(idle);
        RemoveTransitions(move);
        RemoveTransitions(attack);

        AddAnyStateTriggeredTransition(stateMachine, die, "Die", 0.08f);
        AddTriggeredTransition(idle, move, "Move", 0.15f);
        AddTriggeredTransition(move, attack, "Attack", 0.1f);

        AnimatorStateTransition returnToIdle = attack.AddTransition(idle);
        returnToIdle.hasExitTime = true;
        returnToIdle.exitTime = 1f;
        returnToIdle.duration = 0.05f;
        returnToIdle.canTransitionToSelf = false;

        SetClipLooping(AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath), true);
        SetClipLooping(AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveClipPath), true);
        SetClipLooping(AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPath), false);
        SetClipLooping(AssetDatabase.LoadAssetAtPath<AnimationClip>(DieClipPath), false);
        AddStaffFireEvent(AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPath));

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(animator);
    }

    private static void ConfigureStaff(GameObject giant)
    {
        Transform staffTransform = giant.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "GoblinGiantStaff");
        if (staffTransform == null)
        {
            throw new InvalidOperationException("GoblinGiantStaff was not found below GoblinGiant.");
        }

        Animator animator = giant.GetComponent<Animator>();
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand)
            ?? giant.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "hand.R")
            ?? giant.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "RightHand");
        if (rightHand != null && staffTransform.parent != rightHand)
        {
            staffTransform.SetParent(rightHand, true);
        }

        GameObject staff = staffTransform.gameObject;
        staff.tag = "Enemy";
        staff.layer = giant.layer;

        GoblinGiantStaff gun = staff.GetComponent<GoblinGiantStaff>();
        if (gun == null)
        {
            foreach (Gun existingGun in staff.GetComponents<Gun>())
            {
                UnityEngine.Object.DestroyImmediate(existingGun);
            }
            gun = staff.AddComponent<GoblinGiantStaff>();
        }

        Transform shootPoint = staff.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "ShootPoint");
        if (shootPoint == null)
        {
            GameObject shootPointObject = new GameObject("ShootPoint");
            shootPoint = shootPointObject.transform;
            shootPoint.SetParent(staffTransform, false);
            shootPoint.localPosition = new Vector3(0f, -0.0918f, -0.335f);
        }

        WeaponData data = GetOrCreateStaffData();
        SerializedObject serializedGun = new SerializedObject(gun);
        SerializedProperty dataProperty = serializedGun.FindProperty("Data");
        if (dataProperty != null)
        {
            dataProperty.objectReferenceValue = data;
        }
        serializedGun.FindProperty("_rowCount").intValue = 5;
        serializedGun.FindProperty("_bulletsPerRow").intValue = 3;
        serializedGun.FindProperty("_horizontalArcAngle").floatValue = 45f;
        serializedGun.FindProperty("_rowGapAngle").floatValue = 3f;
        serializedGun.FindProperty("_oddRowAimHeight").floatValue = 0.65f;
        serializedGun.FindProperty("_evenRowAimHeight").floatValue = 0.2f;
        serializedGun.ApplyModifiedPropertiesWithoutUndo();

        gun.BulletSpeed = 8f;
        gun.BulletSize = 1.35f;
        gun.shootPoint = shootPoint;
        gun.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBulletPath);

        PistolEnemy enemy = giant.GetComponent<PistolEnemy>();
        enemy.Weapon = staff;
        EditorUtility.SetDirty(gun);
        EditorUtility.SetDirty(enemy);
    }

    private static void ConfigureMinimapIcon(GameObject giant)
    {
        PistolEnemy enemy = giant.GetComponent<PistolEnemy>();
        Transform icon = giant.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "EnemyMinimapIcon");

        if (icon == null)
        {
            GameObject iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinimapIconPath);
            GameObject iconObject = (GameObject)PrefabUtility.InstantiatePrefab(iconPrefab, giant.scene);
            icon = iconObject.transform;
            icon.SetParent(giant.transform, false);
            icon.localPosition = Vector3.zero;
            icon.localRotation = Quaternion.Euler(90f, 0f, 0f);
            icon.localScale = Vector3.one * 0.35f;
        }

        enemy.MinimapIcon = icon;
        EditorUtility.SetDirty(enemy);
    }

    private static WeaponData GetOrCreateStaffData()
    {
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(StaffDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(data, StaffDataPath);
        }

        data.name = "Goblin Giant Staff";
        data.Name = "Goblin Giant Staff";
        data.NameCN = "Goblin Giant Staff";
        data.Category = WeaponData.WeaponCategory.Miscellaneous;
        data.Rarity = WeaponData.WeaponRarity.Green;
        data.Animation = WeaponData.WeaponAnimation.Pistol;
        data.Damage = 4;
        data.EnergyCost = 0;
        data.CritChance = 0;
        data.Inaccuracy = 3;
        data.Price = 0;
        data.Cooldown = 0.5f;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void EnsureTrigger(AnimatorController controller, string parameterName)
    {
        if (controller.parameters.Any(parameter => parameter.name == parameterName))
        {
            return;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        return stateMachine.states.Select(child => child.state).FirstOrDefault(state => state.name == stateName);
    }

    private static void RemoveTransitions(AnimatorState state)
    {
        foreach (AnimatorStateTransition transition in state.transitions.ToArray())
        {
            state.RemoveTransition(transition);
        }
    }

    private static void AddAnyStateTriggeredTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger, float duration)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddTriggeredTransition(AnimatorState source, AnimatorState destination, string trigger, float duration)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void SetClipLooping(AnimationClip clip, bool shouldLoop)
    {
        if (clip == null)
        {
            return;
        }

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = shouldLoop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void AddStaffFireEvent(AnimationClip attackClip)
    {
        if (attackClip == null)
        {
            return;
        }

        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(attackClip);
        if (events.Any(animationEvent => animationEvent.functionName == "PistolAttackAnimationEffect"))
        {
            return;
        }

        AnimationEvent fireEvent = new AnimationEvent
        {
            functionName = "PistolAttackAnimationEffect",
            time = attackClip.length * 0.55f
        };
        AnimationUtility.SetAnimationEvents(attackClip, events.Append(fireEvent).ToArray());
        EditorUtility.SetDirty(attackClip);
    }
}
