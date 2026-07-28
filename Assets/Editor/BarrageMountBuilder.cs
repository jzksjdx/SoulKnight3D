using System;
using System.Linq;
using MoreMountains.Feedbacks;
using QFramework;
using SoulKnight3D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

internal static class BarrageMountBuilder
{
    private const string ControllerPath =
        "Assets/Art/Animation/Mech/BlueMech.controller";
    private const string AnimationFolder =
        "Assets/Art/Animation/Mech/";
    private const string BarragePrefabPath =
        "Assets/Art/Prefab/Special/Mechs/Barrage.prefab";
    private static readonly Vector3 DefaultSpine2AimOffset =
        new Vector3(12.6f, 14.51f, -2.12f);

    private static readonly string[] StateNames =
    {
        "Idle",
        "WalkForward",
        "WalkBack",
        "WalkLeft",
        "WalkRight",
        "JumpUp",
        "JumpMidAir",
        "JumpDown"
    };

    [MenuItem("Tools/Soul Knight/Configure Barrage Mount Animator")]
    private static void ConfigureAnimator()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || controller.layers.Length == 0)
        {
            throw new InvalidOperationException(
                $"Barrage animator controller was not found at {ControllerPath}.");
        }

        for (int i = 0; i < StateNames.Length; i++)
        {
            EnsureTrigger(controller, StateNames[i]);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (AnimatorStateTransition transition in
                 stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }
        foreach (AnimatorStateTransition transition in
                 AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                     .OfType<AnimatorStateTransition>()
                     .ToArray())
        {
            UnityEngine.Object.DestroyImmediate(transition, true);
        }
        AssetDatabase.SaveAssets();

        for (int i = 0; i < StateNames.Length; i++)
        {
            AnimatorState state = FindState(stateMachine, StateNames[i]);
            if (state == null)
            {
                state = stateMachine.AddState(
                    StateNames[i], GetStatePosition(StateNames[i]));
                state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AnimationFolder + StateNames[i] + ".anim");
                if (state.motion == null)
                {
                    throw new InvalidOperationException(
                        $"Barrage animation '{StateNames[i]}.anim' was not found.");
                }
            }

            AnimatorStateTransition transition =
                stateMachine.AddAnyStateTransition(state);
            transition.AddCondition(
                AnimatorConditionMode.If, 0f, StateNames[i]);
            transition.duration = 0.08f;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
        }

        stateMachine.defaultState = FindState(stateMachine, "Idle");
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Configured Barrage mount animator parameters and transitions.");
    }

    [MenuItem("Tools/Soul Knight/Configure Barrage Aiming Rig")]
    private static void ConfigureAimingRig()
    {
        GameObject root =
            PrefabUtility.LoadPrefabContents(BarragePrefabPath);
        try
        {
            Animator animator = root.GetComponent<Animator>();
            ArmorMount mount = root.GetComponent<ArmorMount>();
            Require(animator != null,
                "Barrage's root Animator is missing.");
            Require(mount != null,
                "Barrage's ArmorMount component is missing.");

            Transform spine2 =
                FindUniqueDescendant(root.transform, "mixamorig:Spine2");
            Transform rightHand =
                FindUniqueDescendant(root.transform, "mixamorig:RightHand");
            Transform aimTarget =
                GetOrCreateChild(root.transform, "AimTarget");
            aimTarget.localPosition = new Vector3(0f, 1f, 10f);
            aimTarget.localRotation = Quaternion.identity;

            Transform rigTransform =
                GetOrCreateChild(root.transform, "Aim Rig");
            Rig rig = GetOrAddComponent<Rig>(rigTransform.gameObject);
            rig.weight = 0f;

            MultiAimConstraint spineAim = ConfigureAimConstraint(
                rigTransform, "Spine2Aim", spine2, aimTarget,
                MultiAimConstraintData.Axis.Z,
                MultiAimConstraintData.Axis.Y,
                DefaultSpine2AimOffset);
            MultiAimConstraint rightHandAim = ConfigureAimConstraint(
                rigTransform, "RightHandAim", rightHand, aimTarget,
                MultiAimConstraintData.Axis.Y,
                MultiAimConstraintData.Axis.X_NEG,
                Vector3.zero);

            RigBuilder rigBuilder = GetOrAddComponent<RigBuilder>(root);
            rigBuilder.layers.Clear();
            rigBuilder.layers.Add(new RigLayer(rig, true));

            ArmorMountAimRig aimRig =
                root.GetComponent<ArmorMountAimRig>();
            bool createdAimRig = aimRig == null;
            if (createdAimRig)
            {
                aimRig = root.AddComponent<ArmorMountAimRig>();
            }

            SerializedObject serializedAimRig =
                new SerializedObject(aimRig);
            serializedAimRig.FindProperty("_aimRig").objectReferenceValue =
                rig;
            serializedAimRig.FindProperty("_spine2Aim").objectReferenceValue =
                spineAim;
            serializedAimRig.FindProperty("_rightHandAim")
                .objectReferenceValue = rightHandAim;
            serializedAimRig.FindProperty("_aimTarget").objectReferenceValue =
                aimTarget;
            if (createdAimRig)
            {
                serializedAimRig.FindProperty("_spine2AimOffset")
                    .vector3Value = DefaultSpine2AimOffset;
            }
            serializedAimRig.ApplyModifiedPropertiesWithoutUndo();

            MultiAimConstraintData spineData = spineAim.data;
            spineData.offset = serializedAimRig
                .FindProperty("_spine2AimOffset").vector3Value;
            spineAim.data = spineData;

            SerializedObject serializedMount =
                new SerializedObject(mount);
            serializedMount.FindProperty("_aimRig").objectReferenceValue =
                aimRig;
            serializedMount.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BarragePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAimingRig();
        Debug.Log("Configured Barrage Spine 2 and right-hand aiming rig.");
    }

    [MenuItem("Tools/Soul Knight/Validate Barrage Aiming Rig")]
    private static void ValidateAimingRig()
    {
        GameObject barrage =
            AssetDatabase.LoadAssetAtPath<GameObject>(BarragePrefabPath);
        Require(barrage != null, "Barrage prefab is missing.");

        RigBuilder rigBuilder = barrage.GetComponent<RigBuilder>();
        ArmorMountAimRig aimRig =
            barrage.GetComponent<ArmorMountAimRig>();
        Require(rigBuilder != null && rigBuilder.layers.Count == 1,
            "Barrage RigBuilder is not configured.");
        Require(aimRig != null && aimRig.AimRig != null &&
                aimRig.AimTarget != null,
            "Barrage ArmorMountAimRig is not fully wired.");
        Require(rigBuilder.layers[0].rig == aimRig.AimRig &&
                rigBuilder.layers[0].active,
            "Barrage aim rig is not active in RigBuilder.");

        ValidateAimConstraint(
            aimRig.Spine2Aim, aimRig.AimTarget, "mixamorig:Spine2",
            MultiAimConstraintData.Axis.Z,
            MultiAimConstraintData.Axis.Y);
        ValidateAimConstraint(
            aimRig.RightHandAim, aimRig.AimTarget, "mixamorig:RightHand",
            MultiAimConstraintData.Axis.Y,
            MultiAimConstraintData.Axis.X_NEG);

        Debug.Log("Barrage aiming rig validation passed.");
    }

    [MenuItem("Tools/Soul Knight/Validate Barrage Mount in Play Mode")]
    private static void ValidateMountInPlayMode()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "Enter Play Mode before validating the Barrage mount.");
        }

        PlayerController player = PlayerController.Instance;
        ArmorMount mount = UnityEngine.Object.FindObjectsOfType<ArmorMount>()
            .FirstOrDefault();
        Require(player != null, "PlayerController is missing.");
        Require(mount != null, "No active Barrage ArmorMount was found.");

        MountInteraction interaction = mount.GetComponent<MountInteraction>();
        Collider bodyCollider = mount.GetComponents<Collider>()
            .FirstOrDefault(collider => !collider.isTrigger);
        Rigidbody body = mount.GetComponent<Rigidbody>();
        ConsecutiveGun builtInWeapon =
            mount.GetComponentInChildren<ConsecutiveGun>(true);
        ArmorMountAimRig aimRig =
            mount.GetComponent<ArmorMountAimRig>();
        Require(interaction != null, "MountInteraction is missing.");
        Require(interaction.Label != null, "Mount interaction label was not created.");
        Require(builtInWeapon != null,
            "Barrage's High-Energy SMG is missing.");
        Require(aimRig != null && aimRig.AimRig != null,
            "Barrage's aiming rig is missing.");
        Require(!aimRig.IsAiming &&
                Mathf.Approximately(aimRig.AimRig.weight, 0f),
            "Barrage's aiming rig should be disabled while parked.");
        Require(!builtInWeapon.enabled,
            "Barrage's weapon should be disabled while parked.");
        Require(mount.BuiltInWeaponData != null &&
                mount.BuiltInWeaponData.Name == "High-Energy SMG" &&
                mount.BuiltInWeaponData.NameCN == "高能冲锋枪",
            "Barrage's High-Energy SMG data is not wired.");

        SerializedObject serializedWeapon =
            new SerializedObject(builtInWeapon);
        Require(
            serializedWeapon.FindProperty("_shotsPerAttack").intValue == 4 &&
            Mathf.Approximately(
                serializedWeapon.FindProperty("_shotInterval").floatValue,
                0.1f),
            "Barrage's weapon is not configured for four consecutive shots.");
        ValidateFeedback(mount.transform, "FeedbacksJump");
        ValidateFeedback(mount.transform, "FeedbacksLand");
        ValidateFeedback(mount.transform, "FeedbacksWalk");
        Require(bodyCollider != null && !bodyCollider.enabled,
            "Parked mount body collider should be disabled.");
        Require(body != null && body.isKinematic,
            "Parked mount Rigidbody should be kinematic.");

        interaction.Interact();
        Require(player.MountRider.CurrentMount == mount,
            "Interacting did not mount Barrage.");
        Require(bodyCollider.enabled && !body.isKinematic,
            "Mounted Barrage did not enable occupied physics.");
        Require(!player.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.enabled),
            "Armor mount did not hide the complete player presentation.");
        Require(builtInWeapon.enabled,
            "Barrage's weapon was not enabled after mounting.");
        Require(aimRig.IsAiming &&
                Mathf.Approximately(aimRig.AimRig.weight, 1f),
            "Barrage's aiming rig was not enabled after mounting.");
        Require(aimRig.RefreshAimTarget() &&
                Vector3.Distance(
                    aimRig.AimTarget.position,
                    player.PlayerAttack.target.position) <= 0.001f,
            "Barrage's rig target did not follow the player aim target.");

        int activeBulletsBefore =
            CountActiveBullets(builtInWeapon.bulletPrefab);
        Require(mount.TryAttack(
                builtInWeapon.shootPoint.position + Vector3.up * 100f),
            "Barrage did not start its High-Energy SMG burst.");
        Require(builtInWeapon.GetRemainingCooldown() > 0f,
            "Barrage's weapon did not enter cooldown.");
        Require(CountActiveBullets(builtInWeapon.bulletPrefab) >
                activeBulletsBefore,
            "Barrage's first rocket was not spawned from the bullet pool.");

        PlayerAttack playerAttack = player.PlayerAttack;
        WeaponData pickupData = AssetDatabase.LoadAssetAtPath<WeaponData>(
            "Assets/Art/ScriptableObject/Weapons/AK-47.asset");
        Require(pickupData != null && pickupData.WeaponPrefab != null,
            "The pickup validation weapon is missing.");

        int weaponCountBeforePickup = playerAttack.Weapons.Count;
        GameObject pickedUpWeapon = UnityEngine.Object.Instantiate(
            pickupData.WeaponPrefab, playerAttack.WeaponPoint);
        pickedUpWeapon.transform.SetLocalPositionAndRotation(
            Vector3.zero, Quaternion.identity);
        playerAttack.TakeNewWeapon(pickedUpWeapon);
        Require(playerAttack.Weapons.Count ==
                Mathf.Min(2, weaponCountBeforePickup + 1),
            "Armor mount pickup did not fill or swap the expected slot.");
        Require(playerAttack.GetCurrentWeapon() != null &&
                playerAttack.GetCurrentWeapon().gameObject == pickedUpWeapon,
            "Armor mount pickup did not become the hidden current weapon.");
        Require(!pickedUpWeapon.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.enabled),
            "A weapon picked up on Barrage remained visible.");

        Weapon currentBeforeSwitch = playerAttack.GetCurrentWeapon();
        playerAttack.SwitchWeapon();
        Require(playerAttack.GetCurrentWeapon() == currentBeforeSwitch,
            "Weapon switching was not disabled on Barrage.");

        int healthBeforeDamage = mount.Health.Value;
        mount.ApplyDamage(1);
        Require(mount.Health.Value == healthBeforeDamage - 1,
            "Mounted Barrage did not receive damage.");

        UIGamePanel panel = UIKit.GetPanel<UIGamePanel>();
        Require(panel != null && panel.ArmorMountHealthBar.gameObject.activeSelf,
            "Armor mount health bar is not visible while mounted.");
        Require(!panel.SkillImage.gameObject.activeSelf,
            "Skill icon stayed visible under the dismount button.");
        Transform skillBackground =
            panel.SkillButton.transform.Find("Background");
        Require(skillBackground != null &&
                !skillBackground.gameObject.activeSelf,
            "Skill button background stayed visible under the dismount button.");
        Require(panel.SkillButton.image != null &&
                panel.SkillButton.image.color == Color.white,
            "Dismount button is not opaque white.");

        PlayerInputs.Instance.TriggerSkillAction();
        Require(!player.MountRider.IsMounted,
            "The shared skill action did not dismount.");
        Require(!builtInWeapon.enabled,
            "Barrage's weapon stayed enabled after dismount.");
        Require(!aimRig.IsAiming &&
                Mathf.Approximately(aimRig.AimRig.weight, 0f),
            "Barrage's aiming rig stayed enabled after dismount.");
        Require(player.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.enabled &&
                    renderer.gameObject.activeInHierarchy),
            "Dismount did not restore the player presentation.");
        Require(pickedUpWeapon.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.enabled &&
                    renderer.gameObject.activeInHierarchy),
            "Dismount did not restore the picked-up weapon presentation.");
        Require(!bodyCollider.enabled && body.isKinematic,
            "Dismount did not return Barrage to parked physics.");
        Require(!panel.ArmorMountHealthBar.gameObject.activeSelf,
            "Armor mount health bar stayed visible after dismount.");
        Require(panel.SkillImage.gameObject.activeSelf,
            "Skill icon did not return after dismount.");
        Require(skillBackground.gameObject.activeSelf,
            "Skill button background did not return after dismount.");

        GameController.Instance.SetRoomBattleState(true);
        interaction.RefreshAvailability();
        Require(!interaction.IsInteractable &&
                !player.MountRider.TryMount(mount),
            "Barrage can be remounted during battle.");

        GameController.Instance.SetRoomBattleState(false);
        interaction.RefreshAvailability();
        Require(interaction.IsInteractable,
            "Barrage did not become mountable after battle.");

        Debug.Log("Barrage mount Play Mode validation passed.");
    }

    private static int CountActiveBullets(GameObject bulletPrefab)
    {
        return UnityEngine.Object.FindObjectsOfType<Bullet>()
            .Count(bullet => bullet != null &&
                bullet.gameObject.activeInHierarchy &&
                bullet.PrefabRef == bulletPrefab);
    }

    private static MultiAimConstraint ConfigureAimConstraint(
        Transform rigTransform, string constraintName,
        Transform constrainedObject, Transform aimTarget,
        MultiAimConstraintData.Axis aimAxis,
        MultiAimConstraintData.Axis upAxis, Vector3 offset)
    {
        Transform constraintTransform =
            GetOrCreateChild(rigTransform, constraintName);
        MultiAimConstraint constraint =
            GetOrAddComponent<MultiAimConstraint>(
                constraintTransform.gameObject);

        WeightedTransformArray sources =
            new WeightedTransformArray(0);
        sources.Add(new WeightedTransform(aimTarget, 1f));

        MultiAimConstraintData data = constraint.data;
        data.constrainedObject = constrainedObject;
        data.sourceObjects = sources;
        data.offset = offset;
        data.limits = new Vector2(-180f, 180f);
        data.aimAxis = aimAxis;
        data.upAxis = upAxis;
        data.worldUpType = MultiAimConstraintData.WorldUpType.None;
        data.worldUpObject = null;
        data.worldUpAxis = MultiAimConstraintData.Axis.Y;
        data.maintainOffset = false;
        data.constrainedXAxis = true;
        data.constrainedYAxis = true;
        data.constrainedZAxis = true;
        constraint.data = data;
        constraint.weight = 1f;
        return constraint;
    }

    private static void ValidateAimConstraint(
        MultiAimConstraint constraint, Transform expectedTarget,
        string expectedBoneName,
        MultiAimConstraintData.Axis expectedAimAxis,
        MultiAimConstraintData.Axis expectedUpAxis)
    {
        Require(constraint != null,
            $"{expectedBoneName} aim constraint is missing.");

        MultiAimConstraintData data = constraint.data;
        Require(data.constrainedObject != null &&
                data.constrainedObject.name == expectedBoneName,
            $"{expectedBoneName} is not the constrained bone.");
        Require(data.sourceObjects.Count == 1 &&
                data.sourceObjects[0].transform == expectedTarget &&
                Mathf.Approximately(data.sourceObjects[0].weight, 1f),
            $"{expectedBoneName} does not use Barrage's aim target.");
        Require(data.aimAxis == expectedAimAxis &&
                data.upAxis == expectedUpAxis &&
                data.worldUpType ==
                    MultiAimConstraintData.WorldUpType.None,
            $"{expectedBoneName} aim axes do not match the player rig.");
        Require(data.constrainedXAxis && data.constrainedYAxis &&
                data.constrainedZAxis &&
                !data.maintainOffset,
            $"{expectedBoneName} constrained axes are incomplete.");
    }

    private static Transform FindUniqueDescendant(
        Transform root, string objectName)
    {
        Transform[] matches = root.GetComponentsInChildren<Transform>(true)
            .Where(candidate => candidate.name == objectName)
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected one '{objectName}' under Barrage, found " +
                matches.Length + ".");
        }
        return matches[0];
    }

    private static Transform GetOrCreateChild(
        Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null) { return child; }

        GameObject childObject = new GameObject(childName);
        childObject.layer = parent.gameObject.layer;
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject)
        where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null
            ? component
            : gameObject.AddComponent<T>();
    }

    private static void ValidateFeedback(
        Transform mountTransform, string feedbackName)
    {
        Transform feedbackTransform = mountTransform.Find(feedbackName);
        Require(feedbackTransform != null,
            $"{feedbackName} is missing from Barrage.");
        Require(feedbackTransform.localPosition.sqrMagnitude <= 0.0001f,
            $"{feedbackName} is not positioned at the mount origin.");

        MMF_Player feedback = feedbackTransform.GetComponent<MMF_Player>();
        Require(feedback != null,
            $"{feedbackName} is missing its MMF_Player component on Barrage.");
    }

    private static Vector3 GetStatePosition(string stateName)
    {
        switch (stateName)
        {
            case "Idle": return new Vector3(370f, 120f, 0f);
            case "WalkBack": return new Vector3(330f, 280f, 0f);
            case "WalkForward": return new Vector3(330f, 330f, 0f);
            case "WalkLeft": return new Vector3(330f, 380f, 0f);
            case "WalkRight": return new Vector3(330f, 430f, 0f);
            case "JumpUp": return new Vector3(690f, 300f, 0f);
            case "JumpMidAir": return new Vector3(690f, 360f, 0f);
            case "JumpDown": return new Vector3(690f, 420f, 0f);
            default: return Vector3.zero;
        }
    }

    private static AnimatorState FindState(
        AnimatorStateMachine stateMachine, string stateName)
    {
        ChildAnimatorState child = stateMachine.states.FirstOrDefault(
            item => item.state != null && item.state.name == stateName);
        return child.state;
    }

    private static void EnsureTrigger(
        AnimatorController controller, string parameterName)
    {
        AnimatorControllerParameter existing = controller.parameters
            .FirstOrDefault(parameter => parameter.name == parameterName);
        if (existing != null)
        {
            if (existing.type != AnimatorControllerParameterType.Trigger)
            {
                controller.RemoveParameter(existing);
                controller.AddParameter(
                    parameterName, AnimatorControllerParameterType.Trigger);
            }
            return;
        }

        controller.AddParameter(
            parameterName, AnimatorControllerParameterType.Trigger);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
