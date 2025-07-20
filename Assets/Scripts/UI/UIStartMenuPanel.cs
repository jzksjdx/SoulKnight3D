using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;

namespace SoulKnight3D
{
	public class UIStartMenuPanelData : UIPanelData
	{
	}
	public partial class UIStartMenuPanel : UIPanel, IController
	{
		private LanguageSystem _languageSystem;

		private int _selectedCharacterIndex = 0;
		private List<string> _characterNames = new List<string>
        {
            "Knight", "Rouge"
		};
        [SerializeField] private LocalizeStringEvent _localizedStringEvent;

        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIStartMenuPanelData ?? new UIStartMenuPanelData();
			// please add init code here
			_languageSystem = this.GetSystem<LanguageSystem>();

            StartButton.onClick.AddListener(() =>
			{
				AudioKit.PlaySound("fx_btn_start");
                StartCoroutine(DelayedStartGame());
			});

			CreditButton.onClick.AddListener(() =>
			{
				AudioKit.PlaySound("fx_btn");
                ActionKit.Delay(0.5f, () =>
                {
                    Application.OpenURL("https://space.bilibili.com/131682633");
                }).Start(this);
            });

			HelpPanel.Hide();

			BtnInstruction.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
                HelpPanel.Show();
			});

			BtnCloseHelp.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
                HelpPanel.Hide();
			});

			BtnQuit.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
				Application.Quit();
            });

			BtnSettings.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
                UIKit.OpenPanel<UISettingsPanel>();
            });

            UpdateMenuImage();
			_languageSystem.OnLanguageChanged.Register((currentLanguage) =>
			{
				//Debug.Log("Language changed");
                UpdateMenuImage();
            }).UnRegisterWhenGameObjectDestroyed(this);

			_selectedCharacterIndex = this.GetSystem<SaveSystem>().LoadInt("Character");
            SetCharacterName(_characterNames[_selectedCharacterIndex]);
            BtnSelectCharacterRight.onClick.AddListener(() =>
			{
				_selectedCharacterIndex++;
				if (_selectedCharacterIndex >= _characterNames.Count)
				{
					_selectedCharacterIndex = 0;
                }
				SetCharacterName(_characterNames[_selectedCharacterIndex]);
				StartMenuManager.Instance.UpdateSelectedCharacter(_selectedCharacterIndex);
                this.GetSystem<SaveSystem>().SaveInt("Character", _selectedCharacterIndex);
                AudioKit.PlaySound("fx_btn");
            });

            BtnSelectCharacterLeft.onClick.AddListener(() =>
            {
                _selectedCharacterIndex--;
                if (_selectedCharacterIndex < 0)
                {
                    _selectedCharacterIndex = _characterNames.Count - 1;
                }
                SetCharacterName(_characterNames[_selectedCharacterIndex]);
                StartMenuManager.Instance.UpdateSelectedCharacter(_selectedCharacterIndex);
                this.GetSystem<SaveSystem>().SaveInt("Character", _selectedCharacterIndex);
                AudioKit.PlaySound("fx_btn");
            });
        }

		private void UpdateMenuImage()
		{
			if (MenuImage == null) { return; }
			if (_languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese)
			{
				Debug.Log("Changed to chinese");
                MenuImage.rotation = Quaternion.Euler(0, 0, 0);
            } else
			{
                MenuImage.rotation = Quaternion.Euler(0, 0, 9);
            }
        }

        private IEnumerator DelayedStartGame()
		{
			UIKit.OpenPanel<UILoadingPanel>();
            yield return new WaitForSeconds(0.5f);
            CloseSelf();
			this.GetSystem<SaveSystem>().SaveInt("Level", 1);
            SceneManager.LoadScene(1);
			yield return null;
        }

        public void SetCharacterName(string characterKey)
        {
            // Get the localized character name
            string localizedCharacterName = LocalizationSettings.StringDatabase.GetLocalizedString("MainTable", "CharacterName." + characterKey);

            // Set the argument for the {0} placeholder
            _localizedStringEvent.StringReference.Arguments = new object[] { localizedCharacterName };
            _localizedStringEvent.RefreshString();
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
