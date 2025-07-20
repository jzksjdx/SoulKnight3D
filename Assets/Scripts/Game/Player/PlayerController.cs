using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
	public partial class PlayerController : ViewController, IController
	{
        public static PlayerController Instance;

        public float JumpForce = 5f;
        public float LookRotationTorque = 1f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        public MinimapCam MinimapCam;
        private PlayerStats _playerStats;

        // cinemachine
        private float _lookSensitivity = 1f;
        private float _lookSensitivityFactor = 5f;
        private float _cinemachineTargetPitch;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _jumpTimeout = 0.3f;
        private float _fallTimeoutDelta;
        private float _fallTimeout = 0.2f;

        // system references
        ControlSystem _controlSystem;

        private void Awake()
        {
            Instance = this;
            _controlSystem = this.GetSystem<ControlSystem>();
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Instance = null;
            this.GetSystem<ControlSystem>().ToggleCursor(true);
        }

        private void Start()
        {
            _playerStats = GetComponent<PlayerStats>();
            // reset our timeouts on start
            _jumpTimeoutDelta = _jumpTimeout;
            _fallTimeoutDelta = _fallTimeout;

            PlayerInputs.Instance.OnJumpPerformed.Register(() =>
            {
                Jump();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            _controlSystem.Sensitivity.RegisterWithInitValue((value) =>
            {
                _lookSensitivity = 0.1f + value * 0.9f;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            AudioKit.PlaySound("fx_show_up");

            this.GetSystem<ControlSystem>().ToggleCursor(false);
        }

        private void Update()
        {
            //transform.Translate(new Vector3(0.005f, 0, 0f));
            GroundedCheck();
        }

        private void FixedUpdate()
        {
            if (_playerStats.IsDead) { return; }
            // move
            Vector2 movementVector = PlayerInputs.Instance.GetMovementVectorNormalized();
            Vector2 horizontalVelocity = new Vector2(SelfRigidbody.velocity.x, SelfRigidbody.velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            if (horizontalSpeed <= PlayerStats.Speed)
            {
                Quaternion rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                Vector3 rotatedMovementVector = rotation * new Vector3(movementVector.x, 0, movementVector.y);
                SelfRigidbody.velocity += rotatedMovementVector;
            }

            Vector2 rotatedVelocity = RotateVector2(horizontalVelocity, transform.eulerAngles.y);

            PlayerAnimation.SetAnimationSpeed(horizontalSpeed / PlayerStats.Speed, rotatedVelocity.normalized.x, rotatedVelocity.normalized.y);
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void Jump()
        {
            if (_playerStats.IsDead) { return; }
            if (_jumpTimeoutDelta > 0 || !Grounded) { return; }

            SelfRigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            PlayerAnimation.SetAnimatorJump();
            _jumpTimeoutDelta = _jumpTimeout;
            Grounded = false;
            PlayerAnimation.SetAnimatorGrounded(Grounded);
        }

        private void CameraRotation()
        {
            Vector2 lookVector = PlayerInputs.Instance.GetLookVector();
            _cinemachineTargetPitch += lookVector.y * _lookSensitivityFactor * _lookSensitivity;
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, CameraTarget.transform.rotation.eulerAngles.y, 0f);
            //CameraTarget.transform.Rotate(new Vector3(0f, lookVector.x * lookSensitivity, 0f));
            //transform.Rotate(new Vector3(0f, lookVector.x * lookSensitivity, 0f));
            SelfRigidbody.AddTorque(new Vector3(0f, lookVector.x * _lookSensitivityFactor * _lookSensitivity * LookRotationTorque, 0f));

            //SelfRigidbody.AddTorque(new Vector3(0f, lookVector.x * lookSensitivity, 0f));
            //transform.Rotate(new Vector3(0f, 20 * Time.deltaTime, 0f));
            //transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + lookVector.x * lookSensitivity, transform.rotation.eulerAngles.z));
        }

        private void GroundedCheck()
        {
            if (_jumpTimeoutDelta >= 0)
            {
                // player just jumped, no ground check
                _jumpTimeoutDelta -= Time.deltaTime;
                return;
            }
            if (Mathf.Abs(SelfRigidbody.velocity.y) > 0.1f )
            {
                return;
            }

            // set sphere position, with offset
            Vector3 checkPosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Ray groundedCheckRay = new Ray(checkPosition, Vector3.down);
            Grounded = Physics.Raycast(groundedCheckRay, 0.2f, GroundLayers);
            PlayerAnimation.SetAnimatorGrounded(Grounded);

            if (Grounded)
            {
                //PlayerAnimation.SetAnimatorJump(false);
                PlayerAnimation.SetAnimatorFreeFall(false);
            } else
            {
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    PlayerAnimation.SetAnimatorFreeFall(true);
                }

            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 checkPosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Gizmos.DrawLine(checkPosition, new Vector3(checkPosition.x, checkPosition.y - 0.4f, checkPosition.z));
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        Vector2 RotateVector2(Vector2 vector, float angleDegrees)
        {
            float angleRadians = angleDegrees * Mathf.Deg2Rad; // Convert degrees to radians
            float cosTheta = Mathf.Cos(angleRadians);
            float sinTheta = Mathf.Sin(angleRadians);

            return new Vector2(
                vector.x * cosTheta - vector.y * sinTheta,
                vector.x * sinTheta + vector.y * cosTheta
            );
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}