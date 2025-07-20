using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
	public partial class RoomGate : ViewController
	{
		public EasyEvent OnPlayerEnter = new EasyEvent();

		private Vector3 _closedScale = new Vector3(1f, 1f, 1f);
        private Vector3 _openScale = new Vector3(1f, 0.04f, 1f);

        private bool _isClosed = false;
        private bool _isMoving = false;

		private float _toggleTimeoutDelta = 0f;
		private float _toggleTimeout = 0.8f;

        void Start()
		{
            _toggleTimeoutDelta = 0f;

            SelfBoxCollider.OnTriggerStayEvent((other) =>
            {
                if (other.gameObject.tag == "Wall")
                {
                    other.gameObject.Hide();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

			SelfBoxCollider.OnTriggerExitEvent((other) =>
			{
                if (other.gameObject.tag == "Player")
                {
					OnPlayerEnter.Trigger();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            SelfBoxCollider.OnTriggerEnterEvent((other) =>
            {
                if (other.TryGetComponent(out Bullet bullet))
                {
                    bullet.DestroyBullet();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_isMoving)
            {
                _toggleTimeoutDelta -= Time.deltaTime;
                GateModel.transform.localScale = Vector3.Lerp(
                    _isClosed ? _closedScale : _openScale,
                    _isClosed ? _openScale : _closedScale,
                    1 - _toggleTimeoutDelta / _toggleTimeout);
                if (_toggleTimeoutDelta <= 0)
                {
                    _isClosed = !_isClosed;
                    _isMoving = false;
                }
            }
        }

		public void ToggleGate()
		{
			_toggleTimeoutDelta = _toggleTimeout;
            _isMoving = true;
        }
	}
}
