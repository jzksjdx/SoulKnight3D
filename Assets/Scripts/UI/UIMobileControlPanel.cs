using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public class UIMobileControlPanelData : UIPanelData
	{
	}
	public partial class UIMobileControlPanel : UIPanel
	{
		private Animator _joystickAtkAnimator;
		private int _animIdInteract;
		private bool _canInteract = false;
		private bool _isJoystickRightPressed = false;
		private IUnRegister _specialAttackChargeRegistration;
		private IUnRegister _hoverButtonRegistration;
		private RectTransform _jumpButtonImageTransform;
		private Quaternion _defaultJumpButtonRotation;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIMobileControlPanelData ?? new UIMobileControlPanelData();
			// please add init code here
			UIPressStateRelay jumpPressRelay =
				JoystickJump.GetComponent<UIPressStateRelay>();
			if (jumpPressRelay == null)
			{
				jumpPressRelay =
					JoystickJump.gameObject.AddComponent<UIPressStateRelay>();
			}
			jumpPressRelay.OnPressedChanged.Register(isPressed =>
			{
				PlayerInputs.Instance.TriggerJumpInput(isPressed);
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			_jumpButtonImageTransform = JoystickJump.image != null
				? JoystickJump.image.rectTransform
				: JoystickJump.transform as RectTransform;
			if (_jumpButtonImageTransform != null)
			{
				_defaultJumpButtonRotation =
					_jumpButtonImageTransform.localRotation;
			}

			// joystick attack
			_joystickAtkAnimator = JoystickAttack.GetComponent<Animator>();
			_animIdInteract = Animator.StringToHash("Interact");

            JoystickRight.OnJoystickRightPressed.Register((isPressed) =>
            {
				if (isPressed == _isJoystickRightPressed) { return; }
				_isJoystickRightPressed = isPressed;
                if (Time.timeScale == 0) { return; }
				if (_canInteract)
				{
					if (isPressed)
					{
                        PlayerInputs.Instance.OnInteractPerformed.Trigger();
                    }
					return;
				}

                PlayerInputs.Instance.OnAttackPerformed.Trigger(isPressed);
            }).UnRegisterWhenGameObjectDestroyed(this);

			BtnSpecialAttack.gameObject.Hide();
			BtnSpecialAttack.onClick.AddListener(() =>
			{
				PlayerInputs.Instance.TriggerSpecialAttackAction();
			});

			MountRider mountRider = PlayerController.Instance.MountRider;
			mountRider.OnMountChanged.Register(HandleMountChanged)
				.UnRegisterWhenGameObjectDestroyed(gameObject);
			HandleMountChanged(mountRider.CurrentMount);
        }
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
            PlayerController.Instance.PlayerAttack.OnInteractiveItemChanged.Register((interactiveItem) =>
            {

                if (interactiveItem)
                {
                    _joystickAtkAnimator.SetBool(_animIdInteract, true);
                    _canInteract = true;
                    //BtnInteract.Show();
                }
                else
                {
                    _joystickAtkAnimator.SetBool(_animIdInteract, false);
                    _canInteract = false;
                    //BtnInteract.Hide();
                }
            }).UnRegisterWhenCurrentSceneUnloaded();
        }
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			_specialAttackChargeRegistration?.UnRegister();
			_specialAttackChargeRegistration = null;
			_hoverButtonRegistration?.UnRegister();
			_hoverButtonRegistration = null;
			SetLandingButtonState(false);
		}

		private void HandleMountChanged(MountBase mount)
		{
			BindSpecialAttack(mount);
			BindHoverAbility(mount);
		}

		private void BindSpecialAttack(MountBase mount)
		{
			_specialAttackChargeRegistration?.UnRegister();
			_specialAttackChargeRegistration = null;

			MountSpecialAttack specialAttack =
				mount != null ? mount.SpecialAttack : null;
			if (specialAttack == null)
			{
				BtnSpecialAttack.gameObject.Hide();
				return;
			}

			BtnSpecialAttack.gameObject.Show();
			UpdateSpecialAttackButton(specialAttack.ChargeNormalized);
			_specialAttackChargeRegistration =
				specialAttack.OnChargeChanged.Register(
					UpdateSpecialAttackButton);
		}

		private void UpdateSpecialAttackButton(float charge)
		{
			float normalizedCharge = Mathf.Clamp01(charge);
			if (BtnSpecialAttack.image != null)
			{
				BtnSpecialAttack.image.fillAmount = normalizedCharge;
			}
			BtnSpecialAttack.interactable = normalizedCharge >= 0.999f;
		}

		private void BindHoverAbility(MountBase mount)
		{
			_hoverButtonRegistration?.UnRegister();
			_hoverButtonRegistration = null;

			MountHoverAbility hoverAbility =
				mount != null ? mount.HoverAbility : null;
			if (hoverAbility == null)
			{
				SetLandingButtonState(false);
				return;
			}

			SetLandingButtonState(
				hoverAbility.IsLandingButtonActive);
			_hoverButtonRegistration =
				hoverAbility.OnLandingButtonStateChanged.Register(
					SetLandingButtonState);
		}

		private void SetLandingButtonState(bool isLanding)
		{
			if (_jumpButtonImageTransform == null) { return; }

			_jumpButtonImageTransform.localRotation = isLanding
				? _defaultJumpButtonRotation *
					Quaternion.Euler(0f, 0f, 180f)
				: _defaultJumpButtonRotation;
		}
	}
}
