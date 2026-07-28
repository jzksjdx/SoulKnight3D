using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class ArmorMount : MountBase
    {
        [Header("Armor Mount Weapon")]
        [SerializeField] private ConsecutiveGun _builtInWeapon;
        [SerializeField] private ArmorMountAimRig _aimRig;

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

        public override bool TryAttack(Vector3 targetPosition)
        {
            if (!IsMounted || _builtInWeapon == null ||
                _builtInWeapon.shootPoint == null)
            {
                return false;
            }

            Vector3 direction =
                targetPosition - _builtInWeapon.shootPoint.position;
            return _builtInWeapon.AttackTowards(direction);
        }

        private void SetBuiltInWeaponEnabled(bool isEnabled)
        {
            if (_builtInWeapon != null)
            {
                _builtInWeapon.enabled = isEnabled;
            }
        }
    }
}
