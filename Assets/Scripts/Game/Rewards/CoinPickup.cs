using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class CoinPickup : MonoBehaviour, IPoolable
    {
        public enum CoinType
        {
            Copper,
            Silver,
            Gold
        }

        [SerializeField] private CoinType _type = CoinType.Copper;
        [SerializeField, Min(1)] private int _value = 1;
        [SerializeField, Min(0.1f)] private float _pickUpDistance = 2f;
        [SerializeField, Min(0.1f)] private float _speed = 20f;
        [SerializeField, Min(0f)] private float _pickUpDelay = 0.45f;

        private PlayerController _player;
        private Rigidbody _rigidbody;
        private bool _isPickingUp;
        private float _pickUpDelayDelta;

        public CoinType Type => _type;
        public int Value => _value;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _pickUpDelayDelta = _pickUpDelay;
        }

        private void Update()
        {
            if (_pickUpDelayDelta > 0f)
            {
                _pickUpDelayDelta -= Time.deltaTime;
                return;
            }

            if (_player == null)
            {
                _player = PlayerController.Instance;
                if (_player == null) { return; }
            }

            Vector3 direction = _player.CameraTarget.transform.position - transform.position;
            if (!_isPickingUp)
            {
                if (direction.sqrMagnitude <= _pickUpDistance * _pickUpDistance)
                {
                    _isPickingUp = true;
                }
                return;
            }

            if (_rigidbody != null && direction.sqrMagnitude > 0.0001f)
            {
                _rigidbody.velocity = direction.normalized * _speed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider other)
        {
            if (_pickUpDelayDelta > 0f || !other.CompareTag("Player")) { return; }

            PlayerController player = PlayerController.Instance;
            if (player == null) { return; }

            player.PlayerStats.AddCoins(_value);
            AudioKit.PlaySound("fx_coin");
            GameObjectsManager.Instance?.DespawnCoin(this);
        }

        public void HoldPickupFor(float duration)
        {
            _pickUpDelayDelta = Mathf.Max(_pickUpDelayDelta, duration);
            _isPickingUp = false;
        }

        public void Reset()
        {
            _isPickingUp = false;
            _pickUpDelayDelta = _pickUpDelay;
            _player = null;
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            gameObject.Hide();
        }

#if UNITY_EDITOR
        public void Configure(CoinType type, int value)
        {
            _type = type;
            _value = Mathf.Max(1, value);
        }
#endif
    }
}
