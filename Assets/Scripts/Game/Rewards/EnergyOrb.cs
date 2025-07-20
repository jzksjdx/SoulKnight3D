using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class EnergyOrb : MonoBehaviour
    {
        public float PickUpDistance = 2f;
        public float Speed = 1.5f;
        public int Amount = 8;

        private PlayerController _player;
        private bool _isPickingUp = false;
        private SphereCollider _pickUpCollider;
        private Rigidbody _rigidbody;

        // time out
        private float _pickUpDelayTimeout = 1f;
        private float _pickUpDelayTimeoutDelta;

        void Start()
        {
            _player = PlayerController.Instance;
            _pickUpCollider = GetComponent<SphereCollider>();
            _rigidbody = GetComponent<Rigidbody>();

            _pickUpDelayTimeoutDelta = _pickUpDelayTimeout;

            _pickUpCollider.OnTriggerEnterEvent((other) =>
            {
                if (other.gameObject.tag == "Player")
                {
                    PlayerController.Instance.PlayerStats.RecoverEnergy(Amount);
                    AudioKit.PlaySound("fx_energy");
                    GameObjectsManager.Instance.DespawnEnergyOrb(gameObject);
                    //Destroy(gameObject);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            if (_pickUpDelayTimeoutDelta >= 0f)
            {
                _pickUpDelayTimeoutDelta -= Time.deltaTime;
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

        public void Reset()
        {
            _isPickingUp = false;
            _pickUpDelayTimeoutDelta = _pickUpDelayTimeout;
            this.Hide();
        }
    }

}
