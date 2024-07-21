using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class PlayerInputs : MonoBehaviour, IController
    {
        public static PlayerInputs Instance;
        private InputActions inputActions;

        public EasyEvent OnJumpPerformed = new EasyEvent();
        public EasyEvent<bool> OnAttackPerformed = new EasyEvent<bool>();
        public EasyEvent OnSkillPerformed = new EasyEvent();
        public EasyEvent OnSwitchPerformed = new EasyEvent();
        public EasyEvent OnInteractPerformed = new EasyEvent();
        public EasyEvent OnPausePerformed = new EasyEvent();
        public EasyEvent<int> OnNumberKeyPerformed = new EasyEvent<int>();

        private bool _isMobile = false;

        private void Awake()
        {
            inputActions = new InputActions();
            inputActions.Player.Enable();

            inputActions.Player.Jump.performed += Jump_performed;
            inputActions.Player.Attack.performed += Attack_performed;
            inputActions.Player.Attack.canceled += Attack_canceled;
            inputActions.Player.Skill.performed += Skill_performed;
            inputActions.Player.Switch.performed += Switch_performed;
            inputActions.Player.Interact.performed += Interact_performed;
            inputActions.Player.Pause.performed += Pause_performed;

            inputActions.Player.KeyOne.performed += KeyOne_performed;
            inputActions.Player.KeyTwo.performed += KeyTwo_performed;
            inputActions.Player.KeyThree.performed += KeyThree_performed;

            Instance = this;
        }

        private void Start()
        {
            _isMobile = this.GetSystem<ControlSystem>().IsMobile;
            if (_isMobile)
            {
                UIMobileControlPanel.OnJoystickAtkPerformed.Register((isAttacking) =>
                {
                    OnAttackPerformed.Trigger(isAttacking);
                }).UnRegisterWhenGameObjectDestroyed(this);
            }
        }

        private void OnDestroy()
        {
            Instance = null;

            inputActions.Player.Jump.performed -= Jump_performed;
            inputActions.Player.Attack.performed -= Attack_performed;
            inputActions.Player.Attack.canceled -= Attack_canceled;
            inputActions.Player.Skill.performed -= Skill_performed;
            inputActions.Player.Switch.performed -= Switch_performed;
            inputActions.Player.Interact.performed -= Interact_performed;
            inputActions.Player.Pause.performed -= Pause_performed;
        }

        public void DisableMoveAndAtk()
        {
            inputActions.Player.Jump.performed -= Jump_performed;
            inputActions.Player.Attack.performed -= Attack_performed;
            inputActions.Player.Attack.canceled -= Attack_canceled;
            inputActions.Player.Skill.performed -= Skill_performed;
            inputActions.Player.Switch.performed -= Switch_performed;
            inputActions.Player.Interact.performed -= Interact_performed;
        }

        private void Jump_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Time.timeScale == 0) { return; }
            OnJumpPerformed.Trigger();
        }

        private void Attack_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Time.timeScale == 0) { return; }
            OnAttackPerformed.Trigger(true);
        }

        private void Attack_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Time.timeScale == 0) { return; }
            OnAttackPerformed.Trigger(false);
        }

        private void Skill_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Time.timeScale == 0) { return; }
            OnSkillPerformed.Trigger();
        }

        private void Switch_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Time.timeScale == 0) { return; }
            OnSwitchPerformed.Trigger();
        }

        private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            OnPausePerformed.Trigger();
        }

        private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            OnInteractPerformed.Trigger();
        }

        private void KeyOne_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            OnNumberKeyPerformed.Trigger(1);
        }

        private void KeyTwo_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            OnNumberKeyPerformed.Trigger(2);
        }

        private void KeyThree_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            OnNumberKeyPerformed.Trigger(3);
        }

        public Vector2 GetMovementVectorNormalized()
        {
            if (Time.timeScale == 0) { return Vector2.zero; }
            if (_isMobile)
            {
                Vector2 JoystickVector = new Vector2(JoystickLeft.positionX, JoystickLeft.positionY);
                return JoystickVector;
            } else
            {
                Vector2 inputVector = inputActions.Player.Move.ReadValue<Vector2>();
                return inputVector.normalized;
            }
            
        }

        private Vector2 prevJoystickRot;

        public Vector2 GetLookVector()
        {
            if (Time.timeScale == 0) { return Vector2.zero; }
            if (_isMobile)
            {
                Vector2 currJoystickPos = new Vector2(JoystickRight.rotX, JoystickRight.rotY);
                Vector2 JoystickVector = (currJoystickPos - prevJoystickRot) * 0.5f;
                prevJoystickRot = currJoystickPos;
                return JoystickVector;
            }
            else
            {
                return inputActions.Player.Look.ReadValue<Vector2>();
            }
            
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}

