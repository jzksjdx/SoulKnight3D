using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class VoidSummonerGripStatus : Status
    {
        [SerializeField] private Sprite _weaponIcon;
        [SerializeField] private string _energyCostText = "0";

        private VoidSummonerHand _hand;
        private PlayerAttack _playerAttack;
        private BareHand _bareHand;
        private IUnRegister _attackRegistration;
        private bool _throwRequested;

        protected override bool Expires => false;

        public void BindHand(VoidSummonerHand hand)
        {
            _hand = hand;
        }

        protected override void OnStatusApplied()
        {
            _throwRequested = false;
            _playerAttack = _target != null
                ? _target.GetComponent<PlayerAttack>()
                : null;
            _bareHand = _target != null
                ? _target.GetComponentInChildren<BareHand>(true)
                : null;
            _bareHand?.CancelBareHandAttack();
            if (_playerAttack != null)
            {
                _playerAttack.SetActionBlocker(this, true);
                _playerAttack.SetWeaponDisplayOverride(
                    this, _weaponIcon, _energyCostText);
            }

            if (PlayerInputs.Instance != null)
            {
                _attackRegistration = PlayerInputs.Instance.OnAttackPerformed
                    .Register(HandleAttackInput);
            }
        }

        protected override void OnStatusTick(float deltaTime)
        {
            if (_hand == null)
            {
                RemoveStatus();
            }
        }

        protected override void OnStatusRemoved()
        {
            _attackRegistration?.UnRegister();
            _attackRegistration = null;

            if (_playerAttack != null)
            {
                _playerAttack.SetActionBlocker(this, false);
                _playerAttack.ClearWeaponDisplayOverride(this);
            }

            _hand = null;
            _bareHand = null;
            _playerAttack = null;
            _throwRequested = false;
        }

        private void HandleAttackInput(bool isPressed)
        {
            if (!isPressed || _throwRequested || _hand == null) { return; }

            _throwRequested = true;
            _playerAttack?.SuppressAttackUntilReleased();
            _hand.ThrowBackAtSummoner();
            RemoveStatus();
        }
    }
}
