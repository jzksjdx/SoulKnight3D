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

        private float _disappearTimeout = 20f;
        private float _disappearTimeoutDelta = 20f;

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
                    var stats = PlayerController.Instance.PlayerStats;
                    if (stats.Energy.Value + Amount >= stats.MaxEnergy)
                    {
                        stats.Energy.Value = stats.MaxEnergy;
                    } else
                    {
                        stats.Energy.Value += Amount;
                    }
                    
                    AudioKit.PlaySound("points");
                    GameObjectsManager.Instance.DespawnSun(gameObject);
                    //Destroy(gameObject);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            HandleSelfDespawn();

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

        private void HandleSelfDespawn()
        {
            if (_disappearTimeoutDelta > 0f)
            {
                _disappearTimeoutDelta -= Time.deltaTime;
                if (_disappearTimeoutDelta <= 0f)
                {
                    GameObjectsManager.Instance.DespawnSun(gameObject);
                }
            }
           
        }

        public void Reset()
        {
            _isPickingUp = false;
            _pickUpDelayTimeoutDelta = _pickUpDelayTimeout;
            _pickUpCollider.enabled = false;
            _disappearTimeoutDelta = _disappearTimeout;
            this.Hide();
        }
    }

}
