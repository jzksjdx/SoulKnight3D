using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SoulKnight3D
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ArmorMountAimRig : MonoBehaviour
    {
        [Header("Animation Rigging")]
        [SerializeField] private Rig _aimRig;
        [SerializeField] private MultiAimConstraint _spine2Aim;
        [SerializeField] private MultiAimConstraint _rightHandAim;
        [SerializeField] private Transform _aimTarget;

        [Header("Aim Tuning")]
        [SerializeField] private Vector3 _spine2AimOffset =
            new Vector3(12.6f, 14.51f, -2.12f);
        [SerializeField, Min(0.01f)] private float _minimumAimDistance = 0.5f;

        private bool _isAiming;
        private Vector3 _appliedSpineOffset;

        public bool IsAiming => _isAiming;
        public Rig AimRig => _aimRig;
        public MultiAimConstraint Spine2Aim => _spine2Aim;
        public MultiAimConstraint RightHandAim => _rightHandAim;
        public Transform AimTarget => _aimTarget;

        private void Awake()
        {
            ApplySpineOffset();
            SetRigWeight(false);
        }

        private void OnEnable()
        {
            SetRigWeight(_isAiming);
        }

        private void OnDisable()
        {
            SetRigWeight(false);
        }

        private void OnValidate()
        {
            _minimumAimDistance = Mathf.Max(0.01f, _minimumAimDistance);
            ApplySpineOffset();
        }

        private void Update()
        {
            if (!_isAiming) { return; }

            if (_appliedSpineOffset != _spine2AimOffset)
            {
                ApplySpineOffset();
            }

            RefreshAimTarget();
        }

        public void SetAimingEnabled(bool isAiming)
        {
            _isAiming = isAiming;
            if (_isAiming)
            {
                RefreshAimTarget();
            }
            SetRigWeight(_isAiming);
        }

        public bool RefreshAimTarget()
        {
            PlayerController player = PlayerController.Instance;
            Transform playerAimTarget =
                player != null && player.PlayerAttack != null
                    ? player.PlayerAttack.target
                    : null;
            if (_aimTarget == null || playerAimTarget == null)
            {
                return false;
            }

            Vector3 aimPosition = playerAimTarget.position;
            Vector3 origin = GetAimOrigin();
            Vector3 aimDirection = aimPosition - origin;
            float minimumDistance =
                Mathf.Max(0.01f, _minimumAimDistance);
            if (aimDirection.sqrMagnitude <
                minimumDistance * minimumDistance)
            {
                aimPosition =
                    origin + transform.forward * minimumDistance;
            }

            _aimTarget.position = aimPosition;
            return true;
        }

        private Vector3 GetAimOrigin()
        {
            if (_spine2Aim != null &&
                _spine2Aim.data.constrainedObject != null)
            {
                return _spine2Aim.data.constrainedObject.position;
            }

            return transform.position;
        }

        private void ApplySpineOffset()
        {
            if (_spine2Aim == null) { return; }

            MultiAimConstraintData data = _spine2Aim.data;
            data.offset = _spine2AimOffset;
            _spine2Aim.data = data;
            _appliedSpineOffset = _spine2AimOffset;
        }

        private void SetRigWeight(bool isEnabled)
        {
            if (_aimRig != null)
            {
                _aimRig.weight = isEnabled ? 1f : 0f;
            }
        }
    }
}
