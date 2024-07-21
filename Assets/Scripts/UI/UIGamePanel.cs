using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
	public class UIGamePanelData : UIPanelData
	{
	}
	public partial class UIGamePanel : UIPanel, IController
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIGamePanelData ?? new UIGamePanelData();
			// please add init code here

            // Player stats display panel
			PlayerController.Instance.PlayerStats.Health.RegisterWithInitValue((health) =>
			{
				HealthBar.fillAmount = (float)health / PlayerController.Instance.PlayerStats.MaxHealth;
				HealthText.text = health + "/" + PlayerController.Instance.PlayerStats.MaxHealth;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerController.Instance.PlayerStats.Armor.RegisterWithInitValue((armor) =>
            {
                ArmorBar.fillAmount = (float)armor / PlayerController.Instance.PlayerStats.MaxArmor;
                ArmorText.text = armor + "/" + PlayerController.Instance.PlayerStats.MaxArmor;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerController.Instance.PlayerStats.Energy.RegisterWithInitValue((energy) =>
            {
                EnergyBar.fillAmount = (float)energy / PlayerController.Instance.PlayerStats.MaxEnergy;
                EnergyText.text = energy + "/" + PlayerController.Instance.PlayerStats.MaxEnergy;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            // Pause Panel

            PausePanel.Hide();

			PauseButton.onClick.AddListener(() =>
			{
                ShowPausePanel();
            });

			PlayerInputs.Instance.OnPausePerformed.Register(() =>
			{
                if (gameObject.activeSelf == false) { return; }
                if (UIKit.GetPanel<UIInventoryPanel>().StoragePanel.gameObject.activeSelf) { return; }
				if (Time.timeScale == 1)
				{
                    ShowPausePanel();
                } else
				{
                    HidePausePanel();
                }
			});

        }

		private void ShowPausePanel()
		{
            AudioKit.PlaySound("pause");
            this.GetSystem<ControlSystem>().ToggleCursor(true);
            Time.timeScale = 0;
            PausePanel.Show();
        }

        private void HidePausePanel()
        {
            AudioKit.PlaySound("buttonclick");
            this.GetSystem<ControlSystem>().ToggleCursor(false);
            Time.timeScale = 1;
            PausePanel.Hide();
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

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}
