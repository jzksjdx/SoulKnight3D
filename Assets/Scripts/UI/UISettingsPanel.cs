using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public class UISettingsPanelData : UIPanelData
	{
	}
	public partial class UISettingsPanel : UIPanel, IController
    {
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UISettingsPanelData ?? new UISettingsPanelData();
			// please add init code here

			BtnClose.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
                CloseSelf();
			});

			AudioSystem audioSystem = this.GetSystem<AudioSystem>();
			LanguageSystem languageSystem = this.GetSystem<LanguageSystem>();
			ControlSystem controlSystem = this.GetSystem<ControlSystem>();

            SliderMusic.value = audioSystem.MusicVolume.Value;
			SliderSound.value = audioSystem.SoundVolume.Value;

            SliderMusic.onValueChanged.AddListener((value) =>
			{
                audioSystem.ChangeMusicVolume(value);
            });

            SliderSound.onValueChanged.AddListener((value) =>
            {
                audioSystem.ChangeSoundVolume(value);
            });

			SliderSensitivity.value = controlSystem.Sensitivity.Value;

            SliderSensitivity.onValueChanged.AddListener((value) =>
			{
				controlSystem.ChangeSensitivity(value);
            });

			BtnChinese.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
				languageSystem.SetLanguage(LanguageSystem.Languages.Chinese);
            });

			BtnEnglish.onClick.AddListener(() =>
            {
                AudioKit.PlaySound("fx_btn");
                languageSystem.SetLanguage(LanguageSystem.Languages.English);
            });
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
