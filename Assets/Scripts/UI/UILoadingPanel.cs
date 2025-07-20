using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public class UILoadingPanelData : UIPanelData
	{
	}
	public partial class UILoadingPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UILoadingPanelData ?? new UILoadingPanelData();
			// please add init code here
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
