using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D {
    public class PlayerAttack : MonoBehaviour, IUnRegisterList
    {
        public List<GameObject> Weapons;
        public Transform WeaponPoint, LeftWeaponPoint;
        public Transform target;
        public Skill Skill;
        private PlayerStats _playerStats;
        public PlayerAnimation PlayerAnimation;
        public LayerMask AimLayer;
        public PlayerChargeBar ChargeBar;

        private int _currentWeaponIndex = 0;
        private Weapon _currentWeapon;

        private bool _isAttacking = false;

        private float _interactDistance = 2f;
        private InteractiveItem _interactiveItem;
        private Camera _mainCamera;

        public bool DisableAttack = false;
        public bool IsMountAttackSuppressed { get; set; }

        public EasyEvent<InteractiveItem> OnInteractiveItemChanged = new EasyEvent<InteractiveItem>();
        public EasyEvent<WeaponData, GameObject> OnWeaponSwitched = new EasyEvent<WeaponData, GameObject>();
        public EasyEvent OnPlayerAttaked = new EasyEvent();

        public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();

        void Start()
        {
            _playerStats = GetComponent<PlayerStats>();
            PlayerAnimation = GetComponent<PlayerAnimation>();
            _mainCamera = Camera.main;

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

            if (_currentWeapon)
            {
                RegisterCurrentWeaponFiredEvent();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_playerStats.IsDead) { return; }

            if (_isAttacking)
            {
                Attack();
            }

            UpdateAimAndInteraction();
        }

        private void Attack()
        {
            if (DisableAttack || IsMountAttackSuppressed) { return; }
            if (_currentWeapon == null) { return; }
            if (_currentWeapon.InGameData.EnergyCost > _playerStats.Energy.Value) { return; }
            _currentWeapon.Attack();
        }

        public void Interact()
        {
            if (_interactiveItem == null)
            {
                SetInteractiveItem(null);
                return;
            }
            if (!_interactiveItem.IsInteractable)
            {
                SetInteractiveItem(null);
                return;
            }

            InteractiveItem item = _interactiveItem;
            SetInteractiveItem(null);
            item.Interact();
        }

        public void TakeNewWeapon(GameObject newWeapon)
        {
            if (Weapons.Count >= 2)
            {
                DropCurrentWeapon();
            }
            Weapons.Add(newWeapon);
            EquipWeapon(Weapons.Count - 1, true);
        }

        public void DropCurrentWeapon()
        {
            if (_currentWeapon == null || Weapons.Count == 0) { return; }

            GameObject oldWeapon = Weapons[_currentWeaponIndex];
            Weapons.RemoveAt(_currentWeaponIndex);
            GameObject droppedWeapon = Instantiate(_currentWeapon.InGameData.PickUpPrefab, WeaponPoint.position, Quaternion.identity);
            droppedWeapon.GetComponent<PickupWeapon>().SelfRigidBody.AddForce(transform.forward * 3f, ForceMode.Impulse);
            _currentWeapon = null;
            _currentWeaponIndex = Mathf.Clamp(_currentWeaponIndex, 0, Mathf.Max(Weapons.Count - 1, 0));
            Destroy(oldWeapon);
        }

        public void SwitchWeapon()
        {
            if (IsMountAttackSuppressed) { return; }
            if (Weapons.Count == 0) { return; }

            if (Weapons.Count == 1)
            {
                if (_currentWeapon == null)
                {
                    EquipWeapon(0, false);
                }
                return;
            }

            int nextWeaponIndex = _currentWeapon == null ? _currentWeaponIndex : (_currentWeaponIndex + 1) % Weapons.Count;
            EquipWeapon(nextWeaponIndex, true);
        }

        public void RestoreWeaponState()
        {
            Weapons.RemoveAll(weapon => weapon == null);
            if (Weapons.Count == 0)
            {
                _currentWeapon = null;
                _currentWeaponIndex = 0;
                return;
            }

            int equippedIndex = _currentWeapon == null
                ? -1
                : Weapons.IndexOf(_currentWeapon.gameObject);

            if (equippedIndex < 0)
            {
                _currentWeapon = null;
                _currentWeaponIndex = Mathf.Clamp(_currentWeaponIndex, 0, Weapons.Count - 1);
                EquipWeapon(_currentWeaponIndex, false);
                return;
            }

            _currentWeaponIndex = equippedIndex;
            for (int i = 0; i < Weapons.Count; i++)
            {
                Weapons[i].SetActive(i == _currentWeaponIndex);
            }

            RegisterWeaponEnergySpend(_currentWeapon);
            RegisterCurrentWeaponFiredEvent();
            PlayerAnimation.SwitchWeaponAnimation(_currentWeapon.InGameData.Animation);
            OnWeaponSwitched.Trigger(_currentWeapon.InGameData, _currentWeapon.gameObject);
        }

        public Weapon GetCurrentWeapon()
        {
            return _currentWeapon;
        }

        public void AllowChargeWeaponToShoot()
        {
            if (_currentWeapon == null) { return; }
            if (_currentWeapon.TryGetComponent(out ChargeWeapon chargeWeapon))
            {
                chargeWeapon.AllowShoot();
            }
        }

        public void CancelCurrentWeaponCharge()
        {
            if (_currentWeapon == null) { return; }
            if (_currentWeapon.TryGetComponent(out ChargeWeapon chargeWeapon))
            {
                chargeWeapon.CancelCharge();
            }
        }

        public void SetChargeBarProgress(float progress)
        {
            if (progress == 0f)
            {
                ChargeBar.ResetChargeBar();
                return;
            }
            ChargeBar.UpdateChargeBar(progress);
        }

        public void ToggleChargeBar(bool isShown)
        {
            ChargeBar.gameObject.SetActive(isShown);
        }

        public void HandleMeleeWeaponAttack()
        {
            if (_currentWeapon == null) { return; }
            if (_currentWeapon.TryGetComponent(out Sword sword))
            {
                sword.AttackFromAniamtion();
            }
        }

        public void ToggleBareHandAttack(bool isBareHand)
        {
            if (_currentWeapon == null || Weapons.Count == 0) { return; }

            if (isBareHand)
            {
                Weapons[_currentWeaponIndex].Hide();
            } else
            {
                Weapons[_currentWeaponIndex].Show();
                RegisterWeaponEnergySpend(_currentWeapon);
            }
        }

        private void UpdateAimAndInteraction()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera == null)
            {
                SetInteractiveItem(null);
                return;
            }

            Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = _mainCamera.ScreenPointToRay(screenCenterPoint);

            if (!Physics.Raycast(ray, out RaycastHit raycastHit, 100f, AimLayer))
            {
                SetInteractiveItem(null);
                return;
            }

            target.position = raycastHit.point;

            if (raycastHit.distance <= _interactDistance
                && raycastHit.transform.TryGetComponent(out InteractiveItem interactiveItem)
                && interactiveItem.IsInteractable)
            {
                SetInteractiveItem(interactiveItem);
            }
            else
            {
                SetInteractiveItem(null);
            }
        }

        private void SetInteractiveItem(InteractiveItem interactiveItem)
        {
            if (ReferenceEquals(_interactiveItem, interactiveItem)) { return; }

            if (_interactiveItem != null)
            {
                _interactiveItem.Label.Hide();
            }

            _interactiveItem = interactiveItem;

            if (_interactiveItem != null)
            {
                _interactiveItem.Label.Show();
            }

            OnInteractiveItemChanged.Trigger(_interactiveItem);
        }

        private void EquipWeapon(int weaponIndex, bool playSound)
        {
            if (weaponIndex < 0 || weaponIndex >= Weapons.Count) { return; }

            bool isInitialEquip = _currentWeapon == null;
            if (_currentWeapon != null && _currentWeaponIndex >= 0 && _currentWeaponIndex < Weapons.Count)
            {
                Weapons[_currentWeaponIndex].Hide();
            }

            _currentWeaponIndex = weaponIndex;
            GameObject weaponObject = Weapons[_currentWeaponIndex];
            weaponObject.Show();
            _currentWeapon = weaponObject.GetComponent<Weapon>();

            RegisterWeaponEnergySpend(_currentWeapon);
            RegisterCurrentWeaponFiredEvent();

            if (Skill == null || Skill.IsUsingSkill == false)
            {
                PlayerAnimation.SwitchWeaponAnimation(_currentWeapon.InGameData.Animation);
            }

            if (playSound)
            {
                AudioKit.PlaySound("fx_switch");
            }

            OnWeaponSwitched.Trigger(_currentWeapon.InGameData, weaponObject);

            if (isInitialEquip && Skill != null && Skill.TryGetComponent(out DualWield skill))
            {
                skill.HandleRightHandWeaponChange(_currentWeapon.InGameData, weaponObject);
            }
        }

        private void RegisterWeaponEnergySpend(Weapon weapon)
        {
            weapon.OnWeaponFired.Register(() =>
            {
                _playerStats.Energy.Value -= weapon.InGameData.EnergyCost;
            }).UnRegisterWhenDisabled(weapon);
        }

        private void RegisterCurrentWeaponFiredEvent()
        {
            this.UnRegisterAll();
            if (_currentWeapon == null) { return; }

            _currentWeapon.OnWeaponFired.Register(() =>
            {
                OnPlayerAttaked.Trigger();
            }).AddToUnregisterList(this);
        }
    }
}

