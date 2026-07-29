using System.Collections.Generic;
using MoreMountains.Feedbacks;
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
        [SerializeField, Min(0f)] private float _movementAcceleration = 20f;
        [SerializeField, Min(0f)] private float _movementDeceleration = 40f;

        [Header("Mount Presentation")]
        [SerializeField] private Animator _animator;

        [Header("Mount Feedbacks")]
        [SerializeField] private MMF_Player _jumpFeedback;
        [SerializeField] private MMF_Player _landFeedback;
        [SerializeField] private MMF_Player _walkFeedback;
        [SerializeField, Min(0.05f)] private float _walkFeedbackInterval = 0.5f;
        [SerializeField, Min(0f)] private float _walkFeedbackMinSpeed = 0.1f;

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
        private MountSpecialAttack _specialAttack;
        private MountHoverAbility _hoverAbility;
        private MountAnimationState _animationState = MountAnimationState.None;
        private float _jumpLockoutRemaining;
        private float _damageInvulnerabilityRemaining;
        private float _walkFeedbackTimer;
        private bool _isGrounded;
        private bool _jumpWasStarted;
        private bool _wasWalking;
        private bool _isSettlingAfterDismount;
        private readonly List<TransformPose> _defaultPose =
            new List<TransformPose>();
        private readonly RaycastHit[] _settlingGroundHits =
            new RaycastHit[8];

        public bool IsMounted => _rider != null;
        public virtual bool ReplacesRider => false;
        public MountSpecialAttack SpecialAttack => _specialAttack;
        public bool HasSpecialAttack => _specialAttack != null;
        public MountHoverAbility HoverAbility => _hoverAbility;
        public Rigidbody Body => _body;
        public bool IsGrounded => _isGrounded;

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

        private struct TransformPose
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        protected virtual void Awake()
        {
            if (_body == null) { _body = GetComponent<Rigidbody>(); }
            if (_bodyCollider == null) { _bodyCollider = GetComponent<Collider>(); }
            if (_animator == null) { _animator = GetComponentInChildren<Animator>(true); }
            if (_jumpFeedback == null) { _jumpFeedback = FindFeedback("FeedbacksJump"); }
            if (_landFeedback == null) { _landFeedback = FindFeedback("FeedbacksLand"); }
            if (_walkFeedback == null) { _walkFeedback = FindFeedback("FeedbacksWalk"); }
            _interaction = GetComponent<MountInteraction>();
            _specialAttack = GetComponent<MountSpecialAttack>();
            _hoverAbility = GetComponent<MountHoverAbility>();
            CacheAnimatorParameters();
            CaptureDefaultPose();
            SetOccupiedPhysics(false);
            SetAnimatorActive(false);
        }

        protected override void Start()
        {
            base.Start();

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
                UpdateInactiveSettlement();
                return;
            }

            bool landedThisFrame = UpdateGroundedState();
            MoveMount();
            UpdateAnimation();
            UpdateWalkFeedback(landedThisFrame);
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
            _walkFeedbackTimer = 0f;
            _jumpWasStarted = false;
            _wasWalking = false;
            _isSettlingAfterDismount = false;
            SetOccupiedPhysics(true);
            SetAnimatorActive(true);
            _interaction?.RefreshAvailability();
            rider.EnterMountControl(this, ReplacesRider);
            SetAnimationState(MountAnimationState.Idle);
            OnRideStarted();
            return true;
        }

        protected virtual void OnRideStarted()
        {
        }

        public virtual bool TryAttack(Vector3 targetPosition)
        {
            return false;
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
            _specialAttack?.HandleRideEnded();
            _hoverAbility?.CancelHoverCycle();
            OnRideEnded(wasDestroyed);
            _jumpWasStarted = false;
            _wasWalking = false;
            _walkFeedbackTimer = 0f;
            BeginInactiveSettlement();
            SetAnimatorActive(false);

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

        protected virtual void OnRideEnded(bool wasDestroyed)
        {
        }

        internal void PrepareForLevelTransition()
        {
            if (!IsMounted) { return; }

            _isSettlingAfterDismount = false;
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
            SetAnimatorActive(true);
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
            Quaternion facingRotation =
                Quaternion.Euler(0f, player.FacingYaw, 0f);
            _body.MoveRotation(facingRotation);

            Vector2 movementInput = PlayerInputs.Instance != null
                ? PlayerInputs.Instance.GetMovementVectorNormalized()
                : Vector2.zero;
            Vector3 desiredVelocity = facingRotation *
                new Vector3(movementInput.x, 0f, movementInput.y) * Speed;
            Vector3 currentVelocity =
                new Vector3(_body.velocity.x, 0f, _body.velocity.z);
            float acceleration = movementInput.sqrMagnitude > 0.0001f
                ? _movementAcceleration
                : _movementDeceleration;
            Vector3 horizontalVelocity = Vector3.MoveTowards(
                currentVelocity,
                desiredVelocity,
                acceleration * Time.fixedDeltaTime);

            _body.velocity = new Vector3(
                horizontalVelocity.x,
                _body.velocity.y,
                horizontalVelocity.z);
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
            _jumpWasStarted = true;
            _jumpLockoutRemaining = _jumpLockout;
            _jumpFeedback?.PlayFeedbacks();
            SetAnimationState(MountAnimationState.JumpUp);
            _hoverAbility?.ArmForJump();
        }

        private bool UpdateGroundedState()
        {
            if (_jumpLockoutRemaining > 0f)
            {
                _isGrounded = false;
                return false;
            }

            bool wasGrounded = _isGrounded;
            Vector3 rayOrigin =
                transform.position + Vector3.up * _groundCheckHeight;
            _isGrounded = _body.velocity.y <= 0.1f &&
                Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    _groundCheckHeight + _groundCheckDistance,
                    _groundLayers,
                    QueryTriggerInteraction.Ignore);

            bool landedThisFrame =
                _jumpWasStarted && !wasGrounded && _isGrounded;
            if (landedThisFrame)
            {
                _jumpWasStarted = false;
                _hoverAbility?.HandleLanded();
                _landFeedback?.PlayFeedbacks();
            }

            return landedThisFrame;
        }

        private void UpdateWalkFeedback(bool landedThisFrame)
        {
            Vector2 horizontalVelocity =
                new Vector2(_body.velocity.x, _body.velocity.z);
            bool isWalking = _isGrounded &&
                horizontalVelocity.magnitude >= _walkFeedbackMinSpeed;

            if (!isWalking)
            {
                _wasWalking = false;
                _walkFeedbackTimer = 0f;
                return;
            }

            float interval = Mathf.Max(0.05f, _walkFeedbackInterval);
            if (landedThisFrame)
            {
                _wasWalking = true;
                _walkFeedbackTimer = interval * 0.5f;
                return;
            }

            if (!_wasWalking)
            {
                _wasWalking = true;
                _walkFeedbackTimer = interval;
                _walkFeedback?.PlayFeedbacks();
                return;
            }

            _walkFeedbackTimer -= Time.fixedDeltaTime;
            if (_walkFeedbackTimer > 0f) { return; }

            _walkFeedback?.PlayFeedbacks();
            _walkFeedbackTimer += interval;
        }

        private MMF_Player FindFeedback(string feedbackName)
        {
            Transform feedbackTransform = transform.Find(feedbackName);
            return feedbackTransform != null
                ? feedbackTransform.GetComponent<MMF_Player>()
                : null;
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
                _bodyCollider.enabled = true;
            }

            if (_body == null) { return; }

            _isSettlingAfterDismount = false;
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

        private void BeginInactiveSettlement()
        {
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
            }

            if (_body == null)
            {
                _isSettlingAfterDismount = false;
                return;
            }

            _isSettlingAfterDismount = true;
            _body.isKinematic = false;
            _body.useGravity = true;
            _body.velocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void UpdateInactiveSettlement()
        {
            if (!_isSettlingAfterDismount || _body == null)
            {
                return;
            }

            if (_body.velocity.y > 0.1f || !IsInactiveMountGrounded())
            {
                return;
            }

            _isSettlingAfterDismount = false;
            _isGrounded = true;
            _body.velocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.useGravity = false;
            _body.isKinematic = true;
        }

        private bool IsInactiveMountGrounded()
        {
            if (_bodyCollider == null)
            {
                return false;
            }

            Bounds bounds = _bodyCollider.bounds;
            float maxDistance = bounds.extents.y + 0.08f;
            int hitCount = Physics.RaycastNonAlloc(
                bounds.center,
                Vector3.down,
                _settlingGroundHits,
                maxDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _settlingGroundHits[i].collider;
                if (hitCollider != null &&
                    !hitCollider.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        private void CaptureDefaultPose()
        {
            _defaultPose.Clear();
            if (_animator == null) { return; }

            Transform[] transforms =
                _animator.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform poseTransform = transforms[i];
                if (poseTransform == transform) { continue; }

                _defaultPose.Add(new TransformPose
                {
                    Transform = poseTransform,
                    LocalPosition = poseTransform.localPosition,
                    LocalRotation = poseTransform.localRotation,
                    LocalScale = poseTransform.localScale
                });
            }
        }

        private void SetAnimatorActive(bool isActive)
        {
            if (_animator == null) { return; }

            if (isActive)
            {
                RestoreDefaultPose();
                _animator.enabled = true;
                _animator.Rebind();
                _animationState = MountAnimationState.None;
                return;
            }

            _animator.enabled = false;
            RestoreDefaultPose();
            _animationState = MountAnimationState.None;
        }

        private void RestoreDefaultPose()
        {
            for (int i = 0; i < _defaultPose.Count; i++)
            {
                TransformPose pose = _defaultPose[i];
                if (pose.Transform == null || pose.Transform == transform)
                {
                    continue;
                }

                pose.Transform.localPosition = pose.LocalPosition;
                pose.Transform.localRotation = pose.LocalRotation;
                pose.Transform.localScale = pose.LocalScale;
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
            if (_animationState == state || _animator == null ||
                !_animator.isActiveAndEnabled)
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

        public void KeepJumpMidAirAnimation()
        {
            SetAnimationState(MountAnimationState.JumpMidAir);
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
