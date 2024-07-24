using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
	public class UITempStartPanelData : UIPanelData
	{
	}
	public partial class UITempStartPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UITempStartPanelData ?? new UITempStartPanelData();
			// please add init code here
			BtnStart.onClick.AddListener(() =>
			{
				SceneManager.LoadScene(1);
				CloseSelf();
			});

			BtnQuit.onClick.AddListener(() =>
			{
				Application.Quit();
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
	}
}
