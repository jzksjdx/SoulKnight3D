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

        public bool DisableAttack = false;

        public EasyEvent<InteractiveItem> OnInteractiveItemChanged = new EasyEvent<InteractiveItem>();
        public EasyEvent<WeaponData, GameObject> OnWeaponSwitched = new EasyEvent<WeaponData, GameObject>();
        public EasyEvent OnPlayerAttaked = new EasyEvent();

        public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();

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

            if (_currentWeapon)
            {
                _currentWeapon.OnWeaponFired.Register(() =>
                {
                    OnPlayerAttaked.Trigger();
                }).AddToUnregisterList(this);
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

            Vector2 screenCenterPoint = new Vector2(Screen.width / 2, Screen.height / 2);
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
            // raycast for aiming
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, AimLayer))
            {
                target.position = raycastHit.point;
            }

            // raycast for interaction
            if (Physics.Raycast(ray, out RaycastHit interactableHit, _interactDistance, AimLayer))
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
            if (DisableAttack) { return; }
            if (_currentWeapon == null) { return; }
            if (_currentWeapon.InGameData.EnergyCost > _playerStats.Energy.Value) { return; }
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
            if (Weapons.Count >= 2)
            {
                DropCurrentWeapon();
            }
            Weapons.Add(newWeapon);
            SwitchWeapon();
        }

        public void DropCurrentWeapon()
        {
            GameObject oldWeapon = Weapons[_currentWeaponIndex];
            Weapons.Remove(Weapons[_currentWeaponIndex]);
            GameObject droppedWeapon = Instantiate(_currentWeapon.InGameData.PickUpPrefab, WeaponPoint.position, Quaternion.identity);
            droppedWeapon.GetComponent<PickupWeapon>().SelfRigidBody.AddForce(transform.forward * 3f, ForceMode.Impulse);
            _currentWeapon = null;
            Destroy(oldWeapon);
        }

        public void SwitchWeapon()
        {
            // handle one weapon
            if (Weapons.Count == 1)
            {
                // handle no weapon / game start
                if (_currentWeapon == null)
                {
                    _currentWeapon = Weapons[0].GetComponent<Weapon>();
                    PlayerAnimation.SwitchWeaponAnimation(_currentWeapon.InGameData.Animation);
                    _currentWeapon.OnWeaponFired.Register(() =>
                    {
                        _playerStats.Energy.Value -= _currentWeapon.InGameData.EnergyCost;
                    }).UnRegisterWhenDisabled(_currentWeapon);
                    OnWeaponSwitched.Trigger(_currentWeapon.InGameData, Weapons[_currentWeaponIndex]);

                    if (Skill.TryGetComponent(out DualWield skill))
                    {
                        skill.HandleRightHandWeaponChange(_currentWeapon.InGameData, Weapons[_currentWeaponIndex]);
                    }
                    
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
                _playerStats.Energy.Value -= mWeapon.InGameData.EnergyCost;
            }).UnRegisterWhenDisabled(mWeapon);

            _currentWeapon = Weapons[_currentWeaponIndex].GetComponent<Weapon>();

            if (Skill.IsUsingSkill == false)
            {
                PlayerAnimation.SwitchWeaponAnimation(_currentWeapon.InGameData.Animation);
            }

            AudioKit.PlaySound("fx_switch");

            OnWeaponSwitched.Trigger(_currentWeapon.InGameData, Weapons[_currentWeaponIndex]);

            this.UnRegisterAll();
            _currentWeapon.OnWeaponFired.Register(() =>
            {
                OnPlayerAttaked.Trigger();
            }).AddToUnregisterList(this);
        }

        public Weapon GetCurrentWeapon()
        {
            return _currentWeapon;
        }

        public void AllowChargeWeaponToShoot()
        {
            if (_currentWeapon.TryGetComponent(out ChargeWeapon chargeWeapon))
            {
                chargeWeapon.AllowShoot();
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
            if (_currentWeapon.TryGetComponent(out Sword sword))
            {
                sword.AttackFromAniamtion();
            }
        }

        public void ToggleBareHandAttack(bool isBareHand)
        {
            if (isBareHand)
            {
                Weapons[_currentWeaponIndex].Hide();
            } else
            {
                Weapons[_currentWeaponIndex].Show();
                _currentWeapon.OnWeaponFired.Register(() =>
                {
                    _playerStats.Energy.Value -= _currentWeapon.InGameData.EnergyCost;
                }).UnRegisterWhenDisabled(_currentWeapon);
            }
        }
    }
}

