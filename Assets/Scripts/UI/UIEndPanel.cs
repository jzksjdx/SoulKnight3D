using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public class UIEndPanelData : UIPanelData
	{
	}
	public partial class UIEndPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIEndPanelData ?? new UIEndPanelData();
			// please add init code here
			BtnSubscribe.onClick.AddListener(() =>
			{
                AudioKit.PlaySound("fx_btn");
                Application.OpenURL("https://space.bilibili.com/131682633");
            });

			BtnContinue.onClick.AddListener(() =>
			{
                GameController.Instance.ToggleGameFreeze(false);
                AudioKit.PlaySound("fx_btn");
                GameController.Instance.QuitToMainScreen();
                CloseSelf();
            });

		}

		public void UpdateEndTitle(bool didWin)
		{
			EndTitle.text = didWin ? "体验通关" : "游戏结束";
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
