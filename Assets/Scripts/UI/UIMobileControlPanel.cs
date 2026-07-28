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

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIMobileControlPanelData ?? new UIMobileControlPanelData();
			// please add init code here
			JoystickJump.onClick.AddListener(() =>
			{
				PlayerInputs.Instance.OnJumpPerformed.Trigger();
			});

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
			mountRider.OnMountChanged.Register(BindSpecialAttack)
				.UnRegisterWhenGameObjectDestroyed(gameObject);
			BindSpecialAttack(mountRider.CurrentMount);
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
	}
}
