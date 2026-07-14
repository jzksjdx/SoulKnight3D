using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SoulKnight3D
{
	public class UIStartMenuPanelData : UIPanelData
	{
	}
	public partial class UIStartMenuPanel : UIPanel, IController
	{
		private LanguageSystem _languageSystem;

		private int _selectedCharacterIndex = 0;
        private bool _isStartingGame;
		private List<string> _characterNames = new List<string>
        {
            "Knight", "Rouge"
		};
        [SerializeField] private LocalizeStringEvent _localizedStringEvent;
        private int _characterNameRequestVersion;

        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIStartMenuPanelData ?? new UIStartMenuPanelData();
			// please add init code here
			_languageSystem = this.GetSystem<LanguageSystem>();

            StartButton.onClick.AddListener(() =>
			{
                this.GetSystem<SaveSystem>().SaveBool("BugMode", false);
                AudioKit.PlaySound("fx_btn_start");
                StartCoroutine(DelayedStartGame());
			});

			BtnBugMode.onClick.AddListener(() =>
			{
                this.GetSystem<SaveSystem>().SaveBool("BugMode", true);
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
                StartCoroutine(UIKit.OpenPanelAsync<UISettingsPanel>());
            });

            UpdateMenuImage();
			_languageSystem.OnLanguageChanged.Register((currentLanguage) =>
			{
				//Debug.Log("Language changed");
                UpdateMenuImage();
            }).UnRegisterWhenGameObjectDestroyed(this);

			_selectedCharacterIndex = Mathf.Clamp(this.GetSystem<SaveSystem>().LoadInt("Character"), 0, _characterNames.Count - 1);
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
                MenuImage.rotation = Quaternion.Euler(0, 0, 0);
            } else
			{
                MenuImage.rotation = Quaternion.Euler(0, 0, 9);
            }
        }

        private IEnumerator DelayedStartGame()
		{
            if (_isStartingGame)
            {
                yield break;
            }

            _isStartingGame = true;
            yield return UIKit.OpenPanelAsync<UILoadingPanel>(UILevel.PopUI);
            yield return new WaitForSecondsRealtime(0.5f);
            CloseSelf();
			this.GetSystem<SaveSystem>().SaveInt("Level", 1);
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(1);
            while (loadOperation != null && !loadOperation.isDone)
            {
                yield return null;
            }
        }

        public void SetCharacterName(string characterKey)
        {
            StartCoroutine(SetCharacterNameAsync(characterKey, ++_characterNameRequestVersion));
        }

        private IEnumerator SetCharacterNameAsync(string characterKey, int requestVersion)
        {
            AsyncOperationHandle<string> operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                "MainTable",
                "CharacterName." + characterKey);

            yield return operation;

            // Ignore an older lookup if the player switched characters again while it was loading.
            if (requestVersion != _characterNameRequestVersion)
            {
                yield break;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to localize character name: {characterKey}");
                yield break;
            }

            _localizedStringEvent.StringReference.Arguments = new object[] { operation.Result };
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
			_characterNameRequestVersion++;
		}

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}
