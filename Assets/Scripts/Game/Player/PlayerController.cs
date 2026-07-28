using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
	public partial class PlayerController : ViewController, IController
	{
        public static PlayerController Instance;

        public float JumpForce = 5f;
        public float LookRotationTorque = 1f;

        [Header("Player Physics")]
        [Tooltip("Used while airborne so vertical surfaces cannot hold the player up through friction.")]
        [SerializeField] private PhysicMaterial _airbornePhysicsMaterial;
        [Tooltip("Contacts with an absolute vertical normal below this value are treated as frictionless sides.")]
        [SerializeField, Range(0f, 1f)] private float _sideContactMaxUpDot = 0.5f;

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
        private float _targetYaw;

        private CapsuleCollider _movementCollider;
        private PhysicMaterial _groundedPhysicsMaterial;
        private int _movementColliderInstanceId;

        public MountRider MountRider { get; private set; }
        public float FacingYaw => _targetYaw;

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
            _movementCollider = GetComponent<CapsuleCollider>();
            _groundedPhysicsMaterial = _movementCollider != null ? _movementCollider.sharedMaterial : null;
            if (_movementCollider != null)
            {
                _movementCollider.hasModifiableContacts = true;
                _movementColliderInstanceId = _movementCollider.GetInstanceID();
            }
            _targetYaw = transform.eulerAngles.y;
            SelfRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
            SelfRigidbody.angularVelocity = Vector3.zero;
            MountRider = GetComponent<MountRider>();
            if (MountRider == null)
            {
                MountRider = gameObject.AddComponent<MountRider>();
            }
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            Physics.ContactModifyEvent += HandleContactModification;
            Physics.ContactModifyEventCCD += HandleContactModification;
        }

        private void OnDisable()
        {
            Physics.ContactModifyEvent -= HandleContactModification;
            Physics.ContactModifyEventCCD -= HandleContactModification;
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
            SetGroundedState(true);

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
            if (MountRider != null && MountRider.IsMounted) { return; }
            GroundedCheck();
        }

        private void FixedUpdate()
        {
            if (_playerStats.IsDead) { return; }
            if (MountRider != null && MountRider.IsMounted) { return; }

            SelfRigidbody.MoveRotation(Quaternion.Euler(0f, _targetYaw, 0f));

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
            if (MountRider != null && MountRider.IsMounted) { return; }
            if (_jumpTimeoutDelta > 0 || !Grounded) { return; }

            SelfRigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            PlayerAnimation.SetAnimatorJump();
            _jumpTimeoutDelta = _jumpTimeout;
            SetGroundedState(false);
        }

        private void CameraRotation()
        {
            Vector2 lookVector = PlayerInputs.Instance.GetLookVector();
            _cinemachineTargetPitch += lookVector.y * _lookSensitivityFactor * _lookSensitivity;
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
            _targetYaw += lookVector.x * _lookSensitivityFactor * _lookSensitivity * LookRotationTorque;

            CameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);
        }

        private void GroundedCheck()
        {
            if (_jumpTimeoutDelta >= 0)
            {
                // player just jumped, no ground check
                _jumpTimeoutDelta -= Time.deltaTime;
                return;
            }
            // set sphere position, with offset
            Vector3 checkPosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Ray groundedCheckRay = new Ray(checkPosition, Vector3.down);
            bool isGrounded = SelfRigidbody.velocity.y <= 0.1f
                && Physics.Raycast(groundedCheckRay, 0.2f, GroundLayers, QueryTriggerInteraction.Ignore);
            SetGroundedState(isGrounded);

            if (Grounded)
            {
                _fallTimeoutDelta = _fallTimeout;
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

        private void SetGroundedState(bool isGrounded)
        {
            Grounded = isGrounded;
            PlayerAnimation.SetAnimatorGrounded(isGrounded);

            if (_movementCollider != null)
            {
                _movementCollider.sharedMaterial = isGrounded
                    ? _groundedPhysicsMaterial
                    : _airbornePhysicsMaterial;
            }
        }

        private void HandleContactModification(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        {
            for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
            {
                ModifiableContactPair pair = pairs[pairIndex];
                if (pair.colliderInstanceID != _movementColliderInstanceId
                    && pair.otherColliderInstanceID != _movementColliderInstanceId)
                {
                    continue;
                }

                for (int contactIndex = 0; contactIndex < pair.contactCount; contactIndex++)
                {
                    float normalY = pair.GetNormal(contactIndex).y;
                    if (normalY > -_sideContactMaxUpDot && normalY < _sideContactMaxUpDot)
                    {
                        pair.SetStaticFriction(contactIndex, 0f);
                        pair.SetDynamicFriction(contactIndex, 0f);
                    }
                }
            }
        }

        internal void EnterMountControl(bool hidePlayerModel)
        {
            PlayerAttack.Skill?.CancelForLevelTransition();
            PlayerAttack.CancelCurrentWeaponCharge();
            PlayerAttack.IsMountAttackSuppressed = hidePlayerModel;

            SelfRigidbody.velocity = Vector3.zero;
            SelfRigidbody.angularVelocity = Vector3.zero;
            SelfRigidbody.useGravity = false;
            SelfRigidbody.isKinematic = true;
            if (_movementCollider != null)
            {
                _movementCollider.enabled = false;
            }
            if (hidePlayerModel && ModelRoot != null)
            {
                ModelRoot.gameObject.SetActive(false);
            }
        }

        internal void ExitMountControl(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (ModelRoot != null)
            {
                ModelRoot.gameObject.SetActive(true);
            }
            PlayerAttack.IsMountAttackSuppressed = false;

            if (_movementCollider != null)
            {
                _movementCollider.enabled = true;
            }
            SelfRigidbody.isKinematic = false;
            SelfRigidbody.useGravity = true;
            SelfRigidbody.velocity = Vector3.zero;
            SelfRigidbody.angularVelocity = Vector3.zero;
            SetGroundedState(false);
        }

        internal void SyncToMount(Transform mountTransform)
        {
            if (mountTransform == null) { return; }
            transform.SetPositionAndRotation(
                mountTransform.position,
                mountTransform.rotation);
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
