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

        private int _currentWeaponIndex = 0;
        private Gun _currentWeapon;

        private bool _isAttacking = false;

        private float _interactDistance = 2f;
        private InteractiveItem _interactiveItem;


        public EasyEvent<InteractiveItem> OnInteractiveItemChanged = new EasyEvent<InteractiveItem>();
        public EasyEvent<WeaponData> OnWeaponSwitched = new EasyEvent<WeaponData>();

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
                SwitchWeapon();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerInputs.Instance.OnInteractPerformed.Register(() =>
            {
                Interact();
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

            // raycast for interaction
            if (Physics.Raycast(ray, out RaycastHit interactableHit, _interactDistance))
            {
                if (interactableHit.transform.TryGetComponent(out InteractiveItem interactiveItem))
                {
                    if (interactiveItem != _interactiveItem)
                    {
                        _interactiveItem = interactiveItem;
                        interactiveItem.Label.gameObject.Show();
                        OnInteractiveItemChanged.Trigger(interactiveItem);
                    }
                }
                else
                {
                    if (_interactiveItem != null)
                    {
                        _interactiveItem.Label.gameObject.Hide();
                        _interactiveItem = null;
                        OnInteractiveItemChanged.Trigger(null);
                    }
                }
            }
            else
            {
                if (_interactiveItem != null)
                {
                    _interactiveItem.Label.gameObject.Hide();
                    _interactiveItem = null;
                    OnInteractiveItemChanged.Trigger(null);
                }
            }
        }

        private void Attack()
        {
            if (_currentWeapon == null) { return; }
            if (_currentWeapon.Data.EnergyCost > _playerStats.Energy.Value) { return; }
            _currentWeapon.Attack();
        }

        public void Interact()
        {
            if (_interactiveItem == null) { return; }
            _interactiveItem.Interact();

            OnInteractiveItemChanged.Trigger(null);
        }

        public void TakeNewWeapon(GameObject newWeapon)
        {
            Weapons.Add(newWeapon);
            SwitchWeapon();
        }

        public void SwitchWeapon()
        {
            // handle one weapon
            if (Weapons.Count == 1)
            {
                // handle no weapon / game start
                if (_currentWeapon == null)
                {
                    _currentWeapon = Weapons[0].GetComponent<Gun>();
                    PlayerAnimation.SwitchWeaponAnimation(_currentWeapon.Data.Category);
                    _currentWeapon.OnWeaponFired.Register(() =>
                    {
                        _playerStats.Energy.Value -= _currentWeapon.Data.EnergyCost;
                    }).UnRegisterWhenDisabled(_currentWeapon);
                    OnWeaponSwitched.Trigger(_currentWeapon.Data);
                }
                return;
            }

            // handle more than one weapon
            Weapons[_currentWeaponIndex].Hide();

            if (_currentWeaponIndex + 1 == Weapons.Count)
            {
                _currentWeaponIndex = 0;
            }
            else
            {
                _currentWeaponIndex++;

            }
            Weapons[_currentWeaponIndex].Show();

            Weapon mWeapon = Weapons[_currentWeaponIndex].GetComponent<Weapon>();
            mWeapon.OnWeaponFired.Register(() =>
            {
                _playerStats.Energy.Value -= mWeapon.Data.EnergyCost;
            }).UnRegisterWhenDisabled(mWeapon);

            _currentWeapon = Weapons[_currentWeaponIndex].GetComponent<Gun>();


            AudioKit.PlaySound("fx_switch");

            OnWeaponSwitched.Trigger(_currentWeapon.Data);
        }

    }
}

