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
        private bool _isBugMode = false;
        [SerializeField] private Sprite _dismountSprite;
        private Image _skillButtonImage;
        private GameObject _skillButtonBackground;
        private Sprite _defaultSkillButtonSprite;
        private Color _defaultSkillButtonColor;
        private IUnRegister _mountHealthRegistration;

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

            if (CoinText == null)
            {
                Debug.LogError("UIGamePanel CoinText is not assigned. Rebuild the UI AssetBundle after updating the panel prefab.", this);
            }
            else
            {
                PlayerController.Instance.PlayerStats.Coins.RegisterWithInitValue((coins) =>
                {
                    CoinText.text = coins.ToString();
                }).UnRegisterWhenGameObjectDestroyed(gameObject);
            }

            // Skill button
            _skillButtonImage = SkillButton.image;
            Transform skillBackground =
                SkillButton.transform.Find("Background");
            _skillButtonBackground = skillBackground != null
                ? skillBackground.gameObject
                : null;
            if (_skillButtonImage != null)
            {
                _defaultSkillButtonSprite = _skillButtonImage.sprite;
                _defaultSkillButtonColor = _skillButtonImage.color;
            }
            PlayerController.Instance.PlayerAttack.Skill.SkillCdNormalized.RegisterWithInitValue((amount) =>
			{
                if (PlayerController.Instance.MountRider.IsMounted) { return; }
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
                PlayerInputs.Instance.TriggerSkillAction();
            });

            ArmorMountHealthBar.gameObject.Hide();
            MountRider mountRider = PlayerController.Instance.MountRider;
            mountRider.OnMountChanged.Register(HandleMountChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            HandleMountChanged(mountRider.CurrentMount);

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
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

            // Button Weapon
            BtnWeapon.onClick.AddListener(() =>
            {
                if (Time.timeScale == 0) { return; }
                PlayerInputs.Instance.OnSwitchPerformed.Trigger();
            });

            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponData, _) =>
            {
                if (PlayerController.Instance.MountRider.CurrentMount is ArmorMount)
                {
                    return;
                }

                SetWeaponDisplay(weaponData);
            }).UnRegisterWhenGameObjectDestroyed(this);
            
        }

		private void ShowPausePanel()
		{
            AudioKit.PlaySound("fx_btn");
            GameController.Instance.ToggleGameFreeze(true);
            PausePanel.Show();

            if (_isBugMode)
            {
                UiBugMode.ToggleTexts(true);
            }
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

            GameController.Instance.ToggleGameFreeze(false);
            PausePanel.Hide();

            if (_isBugMode)
            {
                UiBugMode.ToggleTexts(false);
            }
        }

        public void UpdateUiLevelTexts(int level)
        {
            LevelFlagText.text = "1-" + level.ToString();
            MinimapLevelText.text = "1-" + level.ToString();
        }

        public void ToggleBugMode()
        {
            _isBugMode = true;
            UiBugMode.Show();
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
            _mountHealthRegistration?.UnRegister();
            _mountHealthRegistration = null;
		}

        private void HandleMountChanged(MountBase mount)
        {
            _mountHealthRegistration?.UnRegister();
            _mountHealthRegistration = null;

            bool hasMount = mount != null;
            if (hasMount)
            {
                SkillImage.gameObject.Hide();
                _skillButtonBackground?.Hide();
                if (_skillButtonImage != null && _dismountSprite != null)
                {
                    _skillButtonImage.sprite = _dismountSprite;
                    _skillButtonImage.color = Color.white;
                    _skillButtonImage.preserveAspect = true;
                }
            }
            else
            {
                if (_skillButtonImage != null)
                {
                    _skillButtonImage.sprite = _defaultSkillButtonSprite;
                    _skillButtonImage.color = _defaultSkillButtonColor;
                }
                SkillImage.gameObject.Show();
                _skillButtonBackground?.Show();

                float skillAmount =
                    PlayerController.Instance.PlayerAttack.Skill.SkillCdNormalized.Value;
                SkillImage.fillAmount = skillAmount;
                SkillImage.color = skillAmount >= 0.999f
                    ? new Color(74f / 255f, 218f / 255f, 1f)
                    : Color.white;
            }

            ArmorMount armorMount = mount as ArmorMount;
            if (armorMount == null)
            {
                ArmorMountHealthBar.gameObject.Hide();
                Weapon currentWeapon =
                    PlayerController.Instance.PlayerAttack.GetCurrentWeapon();
                SetWeaponDisplay(
                    currentWeapon != null ? currentWeapon.InGameData : null);
                return;
            }

            SetWeaponDisplay(armorMount.BuiltInWeaponData);
            ArmorMountHealthBar.gameObject.Show();
            _mountHealthRegistration = armorMount.Health.RegisterWithInitValue(
                health =>
                {
                    if (PlayerController.Instance.MountRider.CurrentMount != armorMount)
                    {
                        return;
                    }

                    ArmorMountHealthBarFill.fillAmount =
                        armorMount.MaxHealth > 0
                            ? (float)health / armorMount.MaxHealth
                            : 0f;
                });
        }

        private void SetWeaponDisplay(WeaponData weaponData)
        {
            if (weaponData == null) { return; }

            WeaponSprite.sprite = weaponData.Sprite;
            EnergyCostText.text = weaponData.EnergyCost.ToString();
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}
