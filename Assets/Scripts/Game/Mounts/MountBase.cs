using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
    [DisallowMultipleComponent]
    public class MountBase : TargetableObject
    {
        [Header("Mount Physics")]
        [SerializeField] private Rigidbody _body;
        [SerializeField] private Collider _bodyCollider;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField, Min(0f)] private float _jumpForce = 5f;
        [SerializeField, Min(0f)] private float _groundCheckHeight = 0.2f;
        [SerializeField, Min(0.01f)] private float _groundCheckDistance = 0.35f;
        [SerializeField, Min(0f)] private float _jumpLockout = 0.3f;
        [SerializeField, Min(0.1f)] private float _dismountDistance = 0.9f;

        [Header("Mount Presentation")]
        [SerializeField] private Animator _animator;

        [Header("Mount Damage")]
        [SerializeField, Min(0f)] private float _damageInvulnerability = 0.4f;

        private static readonly int IdleTrigger = Animator.StringToHash("Idle");
        private static readonly int WalkForwardTrigger = Animator.StringToHash("WalkForward");
        private static readonly int WalkBackTrigger = Animator.StringToHash("WalkBack");
        private static readonly int WalkLeftTrigger = Animator.StringToHash("WalkLeft");
        private static readonly int WalkRightTrigger = Animator.StringToHash("WalkRight");
        private static readonly int JumpUpTrigger = Animator.StringToHash("JumpUp");
        private static readonly int JumpMidAirTrigger = Animator.StringToHash("JumpMidAir");
        private static readonly int JumpDownTrigger = Animator.StringToHash("JumpDown");

        private readonly HashSet<int> _availableTriggers = new HashSet<int>();
        private MountRider _rider;
        private MountInteraction _interaction;
        private MountAnimationState _animationState = MountAnimationState.None;
        private float _jumpLockoutRemaining;
        private float _damageInvulnerabilityRemaining;
        private bool _isGrounded;

        public bool IsMounted => _rider != null;
        public virtual bool ReplacesRider => false;

        private enum MountAnimationState
        {
            None,
            Idle,
            WalkForward,
            WalkBack,
            WalkLeft,
            WalkRight,
            JumpUp,
            JumpMidAir,
            JumpDown
        }

        protected virtual void Awake()
        {
            if (_body == null) { _body = GetComponent<Rigidbody>(); }
            if (_bodyCollider == null) { _bodyCollider = GetComponent<Collider>(); }
            if (_animator == null) { _animator = GetComponentInChildren<Animator>(true); }
            _interaction = GetComponent<MountInteraction>();
            CacheAnimatorParameters();
            SetOccupiedPhysics(false);
        }

        protected override void Start()
        {
            base.Start();
            SetAnimationState(MountAnimationState.Idle);

            if (PlayerInputs.Instance != null)
            {
                PlayerInputs.Instance.OnJumpPerformed.Register(TryJump)
                    .UnRegisterWhenGameObjectDestroyed(gameObject);
            }
        }

        private void Update()
        {
            if (_jumpLockoutRemaining > 0f)
            {
                _jumpLockoutRemaining -= Time.deltaTime;
            }

            if (_damageInvulnerabilityRemaining > 0f)
            {
                _damageInvulnerabilityRemaining -= Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            if (!IsMounted || _body == null || _rider == null)
            {
                return;
            }

            UpdateGroundedState();
            MoveMount();
            UpdateAnimation();
        }

        internal bool BeginRide(MountRider rider)
        {
            if (rider == null || IsMounted || IsDead)
            {
                return false;
            }

            _rider = rider;
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(
                rider.transform.position,
                Quaternion.Euler(0f, rider.Player.FacingYaw, 0f));
            DontDestroyOnLoad(gameObject);

            _jumpLockoutRemaining = 0f;
            _damageInvulnerabilityRemaining = 0f;
            SetOccupiedPhysics(true);
            _interaction?.RefreshAvailability();
            rider.EnterMountControl(this, ReplacesRider);
            SetAnimationState(MountAnimationState.Idle);
            return true;
        }

        internal Vector3 EndRide(MountRider rider, bool wasDestroyed)
        {
            if (_rider != rider)
            {
                return rider != null ? rider.transform.position : transform.position;
            }

            Vector3 dismountPosition =
                transform.position + transform.right * _dismountDistance;
            _rider = null;
            SetOccupiedPhysics(false);
            SetAnimationState(MountAnimationState.Idle);

            if (!wasDestroyed)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded)
                {
                    SceneManager.MoveGameObjectToScene(gameObject, activeScene);
                }
                _interaction?.RefreshAvailability();
            }

            return dismountPosition;
        }

        internal void PrepareForLevelTransition()
        {
            if (!IsMounted) { return; }

            SetOccupiedPhysics(false);
            gameObject.SetActive(false);
        }

        internal void RestoreAfterLevelTransition(Vector3 spawnPosition)
        {
            if (!IsMounted) { return; }

            gameObject.SetActive(true);
            transform.position = spawnPosition;
            Health.Value = Mathf.Min(
                MaxHealth,
                Health.Value + Mathf.FloorToInt(MaxHealth * 0.5f));
            SetOccupiedPhysics(true);
            SetAnimationState(MountAnimationState.Idle);
        }

        public override void ApplyDamage(int damage)
        {
            if (!IsMounted || IsDead || damage <= 0 ||
                _damageInvulnerabilityRemaining > 0f)
            {
                return;
            }

            _damageInvulnerabilityRemaining = _damageInvulnerability;
            Health.Value = Mathf.Max(0, Health.Value - damage);
            AudioKit.PlaySound("fx_hit_p1");

            if (IsDead)
            {
                MountRider rider = _rider;
                rider?.HandleMountDestroyed(this);
                Destroy(gameObject);
            }
        }

        private void MoveMount()
        {
            PlayerController player = _rider.Player;
            _body.MoveRotation(Quaternion.Euler(0f, player.FacingYaw, 0f));

            Vector2 movementInput = PlayerInputs.Instance != null
                ? PlayerInputs.Instance.GetMovementVectorNormalized()
                : Vector2.zero;
            Vector2 horizontalVelocity =
                new Vector2(_body.velocity.x, _body.velocity.z);
            if (horizontalVelocity.magnitude <= Speed)
            {
                Vector3 movement = transform.rotation *
                    new Vector3(movementInput.x, 0f, movementInput.y);
                _body.velocity += movement;
            }
        }

        private void TryJump()
        {
            if (!IsMounted || !_isGrounded || _jumpLockoutRemaining > 0f ||
                _body == null)
            {
                return;
            }

            _body.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _isGrounded = false;
            _jumpLockoutRemaining = _jumpLockout;
            SetAnimationState(MountAnimationState.JumpUp);
        }

        private void UpdateGroundedState()
        {
            if (_jumpLockoutRemaining > 0f)
            {
                _isGrounded = false;
                return;
            }

            Vector3 rayOrigin =
                transform.position + Vector3.up * _groundCheckHeight;
            _isGrounded = _body.velocity.y <= 0.1f &&
                Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    _groundCheckHeight + _groundCheckDistance,
                    _groundLayers,
                    QueryTriggerInteraction.Ignore);
        }

        private void UpdateAnimation()
        {
            if (!_isGrounded)
            {
                if (_body.velocity.y > 0.15f)
                {
                    SetAnimationState(MountAnimationState.JumpUp);
                }
                else if (_body.velocity.y < -0.15f)
                {
                    SetAnimationState(MountAnimationState.JumpDown);
                }
                else
                {
                    SetAnimationState(MountAnimationState.JumpMidAir);
                }
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(
                new Vector3(_body.velocity.x, 0f, _body.velocity.z));
            if (localVelocity.sqrMagnitude < 0.01f)
            {
                SetAnimationState(MountAnimationState.Idle);
                return;
            }

            if (Mathf.Abs(localVelocity.z) >= Mathf.Abs(localVelocity.x))
            {
                SetAnimationState(localVelocity.z >= 0f
                    ? MountAnimationState.WalkForward
                    : MountAnimationState.WalkBack);
            }
            else
            {
                SetAnimationState(localVelocity.x >= 0f
                    ? MountAnimationState.WalkRight
                    : MountAnimationState.WalkLeft);
            }
        }

        private void SetOccupiedPhysics(bool isOccupied)
        {
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = isOccupied;
            }

            if (_body == null) { return; }

            if (isOccupied)
            {
                _body.isKinematic = false;
                _body.useGravity = true;
                _body.velocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            else
            {
                if (!_body.isKinematic)
                {
                    _body.velocity = Vector3.zero;
                    _body.angularVelocity = Vector3.zero;
                }
                _body.useGravity = false;
                _body.isKinematic = true;
            }
        }

        private void CacheAnimatorParameters()
        {
            _availableTriggers.Clear();
            if (_animator == null) { return; }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Trigger)
                {
                    _availableTriggers.Add(parameters[i].nameHash);
                }
            }
        }

        private void SetAnimationState(MountAnimationState state)
        {
            if (_animationState == state || _animator == null)
            {
                return;
            }

            _animationState = state;
            int trigger = GetAnimationTrigger(state);
            if (!_availableTriggers.Contains(trigger))
            {
                return;
            }

            foreach (int availableTrigger in _availableTriggers)
            {
                _animator.ResetTrigger(availableTrigger);
            }
            _animator.SetTrigger(trigger);
        }

        private static int GetAnimationTrigger(MountAnimationState state)
        {
            switch (state)
            {
                case MountAnimationState.WalkForward: return WalkForwardTrigger;
                case MountAnimationState.WalkBack: return WalkBackTrigger;
                case MountAnimationState.WalkLeft: return WalkLeftTrigger;
                case MountAnimationState.WalkRight: return WalkRightTrigger;
                case MountAnimationState.JumpUp: return JumpUpTrigger;
                case MountAnimationState.JumpMidAir: return JumpMidAirTrigger;
                case MountAnimationState.JumpDown: return JumpDownTrigger;
                default: return IdleTrigger;
            }
        }
    }
}
