using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;

namespace SoulKnight3D
{
    public class ItemPlant : MonoBehaviour, IItem
    {
        public GameObject Weapon;
        public Transform WeaponModel;

        public float PickUpDistance = 2f;
        public float Speed = 1.5f;

        public enum StorageState
        {
            Dropped, Hotbar, Backpack, Crafting, Recipe
        }
        public StorageState State = StorageState.Dropped;

        // for crafting
        public List<IngredientRequirement> RequiredIngredients;

        private Collider _pickUpCollider;
        private Collider _modelCollider;
        private PlayerController _player;
        private bool _isPickingUp = false;
        private Rigidbody _rigidbody;
        private WeaponData _weaponData;

        private bool _hasStarted = false;

        // time out
        private float _pickUpDelayTimeout = 1f;
        private float _pickUpDelayTimeoutDelta;

        //public override void Interact()
        //{
        //    PlayerAttack playerAtk = PlayerController.Instance.PlayerAttack;
        //    if (!playerAtk) { return; }

        //    //GameObject newWeapon = Instantiate(Weapon, playerAtk.WeaponPoint.position, Quaternion.identity, playerAtk.WeaponPoint);
        //    //newWeapon.transform.rotation = new Quaternion(0, 0, 0, 0);
        //    //playerAtk.TakeNewWeapon(newWeapon);
        //    //Destroy(gameObject);
        //}

        private void Awake()
        {
            _weaponData = Weapon.GetComponent<Weapon>().Data;
        }

        private void Start()
        {
            StartItemPlant();
        }

        private void StartItemPlant()
        {
            if (_hasStarted) { return; }
            _hasStarted = true;
            _player = PlayerController.Instance;
            _pickUpCollider = GetComponent<SphereCollider>();
            _modelCollider = GetComponentInChildren<BoxCollider>();
            _rigidbody = GetComponent<Rigidbody>();
            _pickUpDelayTimeoutDelta = _pickUpDelayTimeout;


            _pickUpCollider.enabled = false;
            _pickUpCollider.OnTriggerEnterEvent((other) =>
            {
                if (other.gameObject.tag == "Player")
                {
                    AddToInventory();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (State != StorageState.Dropped) { return; }
            // model rotate
            WeaponModel.Rotate(Vector3.up * 30f * Time.deltaTime);
            // pick up
            if (_pickUpDelayTimeoutDelta >= 0f)
            {
                _pickUpDelayTimeoutDelta -= Time.deltaTime;
                if (_pickUpDelayTimeoutDelta <= 0f)
                {
                    _pickUpCollider.enabled = true;
                }
                return;
            }
            if (_player == null)
            {
                _player = PlayerController.Instance;
                return;
            }

            Vector3 direction = _player.CameraTarget.transform.position - transform.position;
            if (!_isPickingUp)
            {
                float distance = direction.magnitude;
                if (distance <= PickUpDistance)
                {
                    _isPickingUp = true;
                }
                return;
            }
            _rigidbody.velocity = direction.normalized * Speed;
        }

        public void AddToInventory()
        {
            _isPickingUp = false;
            PickUpItem();

            ItemKit.AddItemConfig(this);
            Slot slot = ItemKit.GetSlotGroupByKey("Hotbar").FindEmptySlot();
            if (slot != null)
            {
                // equip plant weapon to player attack
                State = StorageState.Hotbar;
                MoveToHotbar();
            }
            if (slot == null) // not enough slot in hotbar
            {
                State = StorageState.Backpack;
                MoveToItemManager();
                slot = ItemKit.GetSlotGroupByKey("Backpack").FindEmptySlot();
            }
            if (slot == null) // no space in hotbar or backpack
            {
                // TODO: throw item out
                State = StorageState.Dropped;
                return;
            }
            slot.Item = this;
            slot.Count = 1;
            slot.Changed.Trigger();
        }

        public void PickUpItem()
        {
            StartItemPlant();
            _modelCollider.gameObject.Hide();
            _pickUpCollider.enabled = false;
            _rigidbody.isKinematic = true;
            WeaponModel.gameObject.Hide();
        }

        public void ThrowFromInventory()
        {
            _pickUpCollider.enabled = false;
            _isPickingUp = false;
            _pickUpDelayTimeoutDelta = _pickUpDelayTimeout;
            State = StorageState.Dropped;
            _modelCollider.gameObject.Show();
            _rigidbody.isKinematic = false;

            transform.SetParent(null);
            transform.position = _player.PlayerAttack.WeaponPoint.position;
            transform.rotation = Quaternion.Euler(Vector3.zero); 
            WeaponModel.gameObject.Show();
            Weapon.gameObject.Hide();
            _rigidbody.AddForce(_player.PlayerAttack.WeaponPoint.up * 5, ForceMode.Impulse);
        }

        public void MoveToHotbar()
        {
            transform.SetParent(_player.PlayerAttack.WeaponPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        public void MoveToItemManager()
        {
            transform.SetParent(ItemManager.Instance.transform);
            transform.localPosition = Vector3.zero;
            if (_player.PlayerAttack.Weapons.Contains(Weapon))
            {
                int index = _player.PlayerAttack.Weapons.IndexOf(Weapon);
                _player.PlayerAttack.Weapons[index] = null;
            }
            // TODO: handle when current weapon is this
        }

        public Rigidbody GetRigidbody()
        {
            if (_rigidbody)
            {
                return _rigidbody;
            } else
            {
                _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
            }
        }

        public List<IngredientRequirement> GetIngredientRequirements()
        {
            if (RequiredIngredients.Count == 0)
            {
                return new List<IngredientRequirement> { new IngredientRequirement(Weapon.GetComponent<Weapon>().Data, 1) };
            } else
            {
                return RequiredIngredients;
            }
        }

        // IItem interface
        public string GetKey => _weaponData? _weaponData.Name : Weapon.GetComponent<Weapon>().Data.Name;
        public string GetName => _weaponData ? _weaponData.Name : Weapon.GetComponent<Weapon>().Data.Name;
        public Sprite GetIcon => _weaponData ? _weaponData.Sprite : Weapon.GetComponent<Weapon>().Data.Sprite;
    }

}