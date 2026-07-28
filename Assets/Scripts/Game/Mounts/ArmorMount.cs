using System.Collections.Generic;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class ArmorMount : MountBase
    {
        [Header("Armor Mount Weapon")]
        [SerializeField] private ConsecutiveGun _builtInWeapon;
        [SerializeField] private ArmorMountAimRig _aimRig;

        private readonly List<BehaviourState> _weaponBehaviours =
            new List<BehaviourState>();

        public override bool ReplacesRider => true;
        public WeaponData BuiltInWeaponData =>
            _builtInWeapon != null
                ? _builtInWeapon.InGameData ?? _builtInWeapon.GetPrefabWeaponData()
                : null;

        protected override void Awake()
        {
            base.Awake();
            if (_builtInWeapon == null)
            {
                _builtInWeapon =
                    GetComponentInChildren<ConsecutiveGun>(true);
            }
            if (_aimRig == null)
            {
                _aimRig = GetComponent<ArmorMountAimRig>();
            }
            IsolateBuiltInWeaponShakeChannel();
            CacheBuiltInWeaponBehaviours();
            SetBuiltInWeaponEnabled(false);
            _aimRig?.SetAimingEnabled(false);
        }

        protected override void OnRideStarted()
        {
            SetBuiltInWeaponEnabled(true);
            _aimRig?.SetAimingEnabled(true);
            AudioKit.PlaySound("get_in_mecha");
        }

        protected override void OnRideEnded(bool wasDestroyed)
        {
            SetBuiltInWeaponEnabled(false);
            _aimRig?.SetAimingEnabled(false);
        }

        public override bool TryAttack(Vector3 _)
        {
            if (!IsMounted || _builtInWeapon == null ||
                _builtInWeapon.shootPoint == null)
            {
                return false;
            }

            return _builtInWeapon.AttackAlongShootPoint(Vector3.left);
        }

        private void SetBuiltInWeaponEnabled(bool isEnabled)
        {
            if (_builtInWeapon == null) { return; }

            if (!isEnabled)
            {
                MMF_Player[] feedbackPlayers =
                    _builtInWeapon.GetComponentsInChildren<MMF_Player>(true);
                for (int i = 0; i < feedbackPlayers.Length; i++)
                {
                    feedbackPlayers[i].StopFeedbacks();
                }
            }

            for (int i = 0; i < _weaponBehaviours.Count; i++)
            {
                BehaviourState state = _weaponBehaviours[i];
                if (state.Behaviour == null) { continue; }

                state.Behaviour.enabled =
                    isEnabled && state.WasEnabled;
            }

            if (isEnabled)
            {
                _builtInWeapon.enabled = true;
            }
        }

        private void IsolateBuiltInWeaponShakeChannel()
        {
            if (_builtInWeapon == null) { return; }

            int channel = Mathf.Abs(_builtInWeapon.GetInstanceID());
            MMRotationShaker[] rotationShakers =
                _builtInWeapon.GetComponentsInChildren<MMRotationShaker>(true);
            for (int i = 0; i < rotationShakers.Length; i++)
            {
                rotationShakers[i].Channel = channel;
            }

            MMF_RotationShake rotationFeedback =
                _builtInWeapon.ShootFeedback != null
                    ? _builtInWeapon.ShootFeedback
                        .GetFeedbackOfType<MMF_RotationShake>()
                    : null;
            if (rotationFeedback != null)
            {
                rotationFeedback.Channel = channel;
                rotationFeedback.TargetShaker =
                    rotationShakers.Length > 0
                        ? rotationShakers[0]
                        : null;
            }
        }

        private void CacheBuiltInWeaponBehaviours()
        {
            _weaponBehaviours.Clear();
            if (_builtInWeapon == null) { return; }

            Behaviour[] behaviours =
                _builtInWeapon.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                _weaponBehaviours.Add(new BehaviourState
                {
                    Behaviour = behaviours[i],
                    WasEnabled = behaviours[i].enabled
                });
            }
        }

        private struct BehaviourState
        {
            public Behaviour Behaviour;
            public bool WasEnabled;
        }
    }
}
