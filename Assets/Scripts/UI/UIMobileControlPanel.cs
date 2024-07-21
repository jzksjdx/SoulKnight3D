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
		public static EasyEvent<bool> OnJoystickAtkPerformed = new EasyEvent<bool>();

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
                if (Time.timeScale == 0) { return; }
				if (_canInteract)
				{
					if (isPressed)
					{
                        PlayerInputs.Instance.OnInteractPerformed.Trigger();
                    }
					return;
				}

				OnJoystickAtkPerformed.Trigger(isPressed);
            }).UnRegisterWhenGameObjectDestroyed(this);
        }
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
