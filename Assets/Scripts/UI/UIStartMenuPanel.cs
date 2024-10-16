using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;
using System.Collections;

namespace SoulKnight3D
{
	public class UIStartMenuPanelData : UIPanelData
	{
	}
	public partial class UIStartMenuPanel : UIPanel, IController
	{
		private LanguageSystem _languageSystem;

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
			yield return new WaitForSeconds(0.5f);
            CloseSelf();
            SceneManager.LoadScene(1);
			yield return null;
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
