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
	public partial class UIStartMenuPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIStartMenuPanelData ?? new UIStartMenuPanelData();
			// please add init code here
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
	}
}
