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

            // Skill button
            PlayerController.Instance.PlayerAttack.Skill.SkillCdNormalized.RegisterWithInitValue((amount) =>
			{
				SkillImage.fillAmount = amount;
				if (amount >= 0.999)
				{
					SkillImage.color = new Color(74f / 255f, 218f / 255f, 1);

                } else
				{
					SkillImage.color = Color.white;
                }
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

            SkillButton.onClick.AddListener(() =>
            {
                PlayerInputs.Instance.OnSkillPerformed.Trigger();
            });

            // Interact button
            BtnInteract.Hide();

            PlayerController.Instance.PlayerAttack.OnInteractiveItemChanged.Register((interactiveItem) =>
            {
                if(this.GetSystem<ControlSystem>().IsMobile) { return; }
                if (interactiveItem)
                {
                    BtnInteract.Show();
                } else
                {
                    BtnInteract.Hide();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            BtnInteract.onClick.AddListener(() =>
            {
                PlayerController.Instance.PlayerAttack.Interact();
            });

            // Pause Panel

            PausePanel.Hide();

			PauseButton.onClick.AddListener(() =>
			{
                ShowPausePanel();
            });

			PlayerInputs.Instance.OnPausePerformed.Register(() =>
			{
                if (gameObject.activeSelf == false) { return; }
				if (Time.timeScale == 1)
				{
                    ShowPausePanel();
                } else
				{
                    HidePausePanel();
                }
			});

            // Button Weapon
            BtnWeapon.onClick.AddListener(() =>
            {
                if (Time.timeScale == 0) { return; }
                PlayerInputs.Instance.OnSwitchPerformed.Trigger();
            });

            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponData) =>
            {
                Debug.Log("UIGamePanelData: " + weaponData.name);
                WeaponSprite.sprite = weaponData.Sprite;
                EnergyCostText.text = weaponData.EnergyCost.ToString();
            }).UnRegisterWhenGameObjectDestroyed(this);
        }

		private void ShowPausePanel()
		{
            AudioKit.PlaySound("fx_btn");
            this.GetSystem<ControlSystem>().ToggleCursor(true);
            Time.timeScale = 0;
            PausePanel.Show();
        }

        private void HidePausePanel()
        {
            AudioKit.PlaySound("fx_btn");

            // close settings panel if opened
            if (UIKit.GetPanel<UISettingsPanel>())
            {
                UIKit.ClosePanel<UISettingsPanel>();
                return;
            }

            this.GetSystem<ControlSystem>().ToggleCursor(false);
            Time.timeScale = 1;
            PausePanel.Hide();
        }

        protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
            if (PlayerController.Instance.PlayerAttack.Weapons.Count == 1)
            {
                PlayerController.Instance.PlayerAttack.SwitchWeapon();
            }
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
