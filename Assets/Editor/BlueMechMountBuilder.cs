using System;
using System.Linq;
using MoreMountains.Feedbacks;
using QFramework;
using SoulKnight3D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal static class BlueMechMountBuilder
{
    private const string ControllerPath =
        "Assets/Art/Animation/Mech/BlueMech.controller";
    private const string AnimationFolder =
        "Assets/Art/Animation/Mech/";

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

    [MenuItem("Tools/Soul Knight/Configure Blue Mech Mount Animator")]
    private static void ConfigureAnimator()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || controller.layers.Length == 0)
        {
            throw new InvalidOperationException(
                $"Blue Mech animator controller was not found at {ControllerPath}.");
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
                        $"Blue Mech animation '{StateNames[i]}.anim' was not found.");
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
        Debug.Log("Configured Blue Mech mount animator parameters and transitions.");
    }

    [MenuItem("Tools/Soul Knight/Validate Blue Mech Mount in Play Mode")]
    private static void ValidateMountInPlayMode()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "Enter Play Mode before validating the Blue Mech mount.");
        }

        PlayerController player = PlayerController.Instance;
        ArmorMount mount = UnityEngine.Object.FindObjectsOfType<ArmorMount>()
            .FirstOrDefault();
        Require(player != null, "PlayerController is missing.");
        Require(mount != null, "No active ArmorMount was found.");

        MountInteraction interaction = mount.GetComponent<MountInteraction>();
        Collider bodyCollider = mount.GetComponents<Collider>()
            .FirstOrDefault(collider => !collider.isTrigger);
        Rigidbody body = mount.GetComponent<Rigidbody>();
        Require(interaction != null, "MountInteraction is missing.");
        Require(interaction.Label != null, "Mount interaction label was not created.");
        ValidateFeedback(mount.transform, "FeedbacksJump");
        ValidateFeedback(mount.transform, "FeedbacksLand");
        ValidateFeedback(mount.transform, "FeedbacksWalk");
        Require(bodyCollider != null && !bodyCollider.enabled,
            "Parked mount body collider should be disabled.");
        Require(body != null && body.isKinematic,
            "Parked mount Rigidbody should be kinematic.");

        interaction.Interact();
        Require(player.MountRider.CurrentMount == mount,
            "Interacting did not mount Blue Mech.");
        Require(bodyCollider.enabled && !body.isKinematic,
            "Mounted Blue Mech did not enable occupied physics.");
        Require(!player.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.enabled),
            "Armor mount did not hide the complete player presentation.");

        int healthBeforeDamage = mount.Health.Value;
        mount.ApplyDamage(1);
        Require(mount.Health.Value == healthBeforeDamage - 1,
            "Mounted Blue Mech did not receive damage.");

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
        Require(player.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer != null && renderer.enabled &&
                    renderer.gameObject.activeInHierarchy),
            "Dismount did not restore the player presentation.");
        Require(!bodyCollider.enabled && body.isKinematic,
            "Dismount did not return Blue Mech to parked physics.");
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
            "Blue Mech can be remounted during battle.");

        GameController.Instance.SetRoomBattleState(false);
        interaction.RefreshAvailability();
        Require(interaction.IsInteractable,
            "Blue Mech did not become mountable after battle.");

        Debug.Log("Blue Mech mount Play Mode validation passed.");
    }

    private static void ValidateFeedback(
        Transform mountTransform, string feedbackName)
    {
        Transform feedbackTransform = mountTransform.Find(feedbackName);
        Require(feedbackTransform != null,
            $"{feedbackName} is missing from Blue Mech.");
        Require(feedbackTransform.localPosition.sqrMagnitude <= 0.0001f,
            $"{feedbackName} is not positioned at the mount origin.");

        MMF_Player feedback = feedbackTransform.GetComponent<MMF_Player>();
        Require(feedback != null,
            $"{feedbackName} is missing its MMF_Player component.");
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
