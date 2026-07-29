using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MountBase))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MountHoverAbility : MonoBehaviour
    {
        [Header("Hover")]
        [SerializeField] private MountBase _mount;
        [SerializeField] private Rigidbody _body;
        [SerializeField, Min(0.1f)] private float _hoverDuration = 3f;
        [SerializeField, Min(0f)] private float _apexVelocityThreshold = 0.05f;

        [Header("Presentation")]
        [SerializeField] private List<GameObject> _thrustEffects =
            new List<GameObject>();
        [SerializeField] private string _hoverLoopSound = "fx_laser";

        private AudioPlayer _hoverAudio;
        private float _hoverRemaining;
        private bool _hoverArmed;
        private bool _hasObservedAscent;
        private bool _isHovering;
        private bool _jumpHeld;
        private bool _holdToHover;
        private bool _landingButtonActive;
        private RigidbodyConstraints _constraintsBeforeHover;

        public readonly EasyEvent<bool> OnLandingButtonStateChanged =
            new EasyEvent<bool>();

        public bool IsHovering => _isHovering;
        public bool IsLandingButtonActive => _landingButtonActive;
        public float HoverRemaining => _hoverRemaining;

        private void Awake()
        {
            if (_mount == null) { _mount = GetComponent<MountBase>(); }
            if (_body == null) { _body = GetComponent<Rigidbody>(); }
            SetThrustEffects(false);
        }

        private void Start()
        {
            if (PlayerInputs.Instance == null) { return; }

            _jumpHeld = PlayerInputs.Instance.IsJumpPressed;
            PlayerInputs.Instance.OnJumpStateChanged
                .Register(HandleJumpStateChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void FixedUpdate()
        {
            if (_mount == null || !_mount.IsMounted || _body == null)
            {
                return;
            }

            if (_isHovering)
            {
                _hoverRemaining -= Time.fixedDeltaTime;
                if (_hoverRemaining <= 0f)
                {
                    EndHover();
                    return;
                }

                _body.velocity = new Vector3(
                    _body.velocity.x, 0f, _body.velocity.z);
                _mount.KeepJumpMidAirAnimation();
                return;
            }

            if (!_hoverArmed || _mount.IsGrounded)
            {
                return;
            }

            if (_body.velocity.y > _apexVelocityThreshold)
            {
                _hasObservedAscent = true;
                return;
            }

            if (_hasObservedAscent)
            {
                BeginHover();
            }
        }

        private void OnDisable()
        {
            CancelHoverCycle();
        }

        public void ArmForJump()
        {
            if (_mount == null || !_mount.IsMounted) { return; }

            _hoverArmed = true;
            _hasObservedAscent = false;
            _jumpHeld = PlayerInputs.Instance != null &&
                PlayerInputs.Instance.IsJumpPressed;
        }

        public void HandleLanded()
        {
            EndHover();
            _hoverArmed = false;
            _hasObservedAscent = false;
            SetLandingButtonActive(false);
        }

        public void CancelHoverCycle()
        {
            EndHover();
            _hoverArmed = false;
            _hasObservedAscent = false;
            _jumpHeld = false;
            SetLandingButtonActive(false);
        }

        private void HandleJumpStateChanged(bool isPressed)
        {
            _jumpHeld = isPressed;
            if (_mount == null || !_mount.IsMounted || !_isHovering)
            {
                return;
            }

            if (isPressed || (_holdToHover && !isPressed))
            {
                EndHover();
            }
        }

        private void BeginHover()
        {
            _hoverArmed = false;
            _hasObservedAscent = false;
            _isHovering = true;
            _holdToHover = _jumpHeld;
            _hoverRemaining = Mathf.Max(0.1f, _hoverDuration);
            _constraintsBeforeHover = _body.constraints;
            _body.constraints =
                _constraintsBeforeHover |
                RigidbodyConstraints.FreezePositionY;
            _body.useGravity = false;
            _body.velocity = new Vector3(
                _body.velocity.x, 0f, _body.velocity.z);
            _mount.KeepJumpMidAirAnimation();
            SetThrustEffects(true);
            SetLandingButtonActive(true);

            if (!string.IsNullOrWhiteSpace(_hoverLoopSound))
            {
                _hoverAudio =
                    AudioKit.PlaySound(_hoverLoopSound, true);
            }
        }

        private void EndHover()
        {
            if (!_isHovering)
            {
                StopHoverPresentation();
                return;
            }

            _isHovering = false;
            _holdToHover = false;
            _hoverRemaining = 0f;
            if (_body != null)
            {
                _body.constraints = _constraintsBeforeHover;
                _body.useGravity = true;
            }
            StopHoverPresentation();
        }

        private void StopHoverPresentation()
        {
            SetThrustEffects(false);
            _hoverAudio?.Stop();
            _hoverAudio = null;
        }

        private void SetThrustEffects(bool isActive)
        {
            for (int i = 0; i < _thrustEffects.Count; i++)
            {
                GameObject effect = _thrustEffects[i];
                if (effect != null)
                {
                    effect.SetActive(isActive);
                }
            }
        }

        private void SetLandingButtonActive(bool isActive)
        {
            if (_landingButtonActive == isActive) { return; }

            _landingButtonActive = isActive;
            OnLandingButtonStateChanged.Trigger(isActive);
        }
    }
}
