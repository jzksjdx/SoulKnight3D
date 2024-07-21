using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D {
    public class PlayerAttack : MonoBehaviour
    {
        public List<GameObject> Weapons;
        public Transform WeaponPoint;
        public Transform target;
        private PlayerStats _playerStats;
        public PlayerAnimation PlayerAnimation;
        public LayerMask AimLayer;

        public int CurrentWeaponIndex = 0;
        private Gun _currentWeapon;

        private bool _isAttacking = false;
        private bool _isUsingInventory = false;

        public EasyEvent<int> OnWeaponSwitched = new EasyEvent<int>();

        void Start()
        {
            _playerStats = GetComponent<PlayerStats>();
            PlayerAnimation = GetComponent<PlayerAnimation>();

            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                _isAttacking = isAttacking;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerInputs.Instance.OnSwitchPerformed.Register(() =>
            {

                if (CurrentWeaponIndex + 1 == Weapons.Count)
                {
                    CurrentWeaponIndex = 0;
                }
                else
                {
                    CurrentWeaponIndex++;
                }
                SwitchWeapon(CurrentWeaponIndex);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerInputs.Instance.OnNumberKeyPerformed.Register((number) =>
            {
                if (number - 1 == CurrentWeaponIndex) { return; }
                SwitchWeapon(number - 1);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            UIInventoryPanel.OnToggleInventory.Register((isUsingInventory) =>
            {
                _isUsingInventory = isUsingInventory;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (_playerStats.IsDead) { return; }

            if (_isAttacking)
            {
                Attack();
            }

            Vector2 screenCenterPoint = new Vector2(Screen.width / 2, Screen.height / 2);
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
            // raycast for aiming
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, AimLayer))
            {
                target.position = raycastHit.point;
            }
        }

        private void Attack()
        {
            if (_currentWeapon == null) { return; }
            if (_isUsingInventory) { return; }
            if (_currentWeapon.Data.EnergyCost > _playerStats.Energy.Value) { return; }
            _currentWeapon.Attack();
        }

        public void InitPlayerAttackWithUnequipedPlant()
        {
            ItemPlant plant = WeaponPoint.GetComponentInChildren<ItemPlant>();
            plant.AddToInventory();
        }

        public void SwitchWeapon(int weaponIndex, bool playSound = false)
        {
            if (Weapons[CurrentWeaponIndex])
            {
                if (Weapons[CurrentWeaponIndex] == _currentWeapon)
                {
                    return;
                }
            }
            Debug.Log("SwitchWeapon: " + weaponIndex);
            if (playSound)
            {
                AudioKit.PlaySound("seedlift");
            }
            if (Weapons[CurrentWeaponIndex])
            {
                Weapons[CurrentWeaponIndex].Hide();
            }
            CurrentWeaponIndex = weaponIndex;
            if (Weapons[CurrentWeaponIndex])
            {
                Weapons[CurrentWeaponIndex].Show();
            }

            if (Weapons[CurrentWeaponIndex])
            {
                Weapon mWeapon = Weapons[CurrentWeaponIndex].GetComponent<Weapon>();
                if (mWeapon)
                {
                    mWeapon.OnWeaponFired.Register(() =>
                    {
                        _playerStats.Energy.Value -= mWeapon.Data.EnergyCost;
                    }).UnRegisterWhenDisabled(mWeapon);

                    _currentWeapon = Weapons[CurrentWeaponIndex].GetComponent<Gun>();

                    PlayerAnimation.SwitchWeaponAnimation(_currentWeapon.Data.Category);
                }
            }else
            {
                _currentWeapon = null;
            }
            OnWeaponSwitched.Trigger(CurrentWeaponIndex);
        }

        public Weapon GetCurrentWeapon()
        {
            return _currentWeapon;
        }
    }
}

