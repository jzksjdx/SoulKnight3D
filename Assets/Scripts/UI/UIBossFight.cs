using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public class UIBossFightData : UIPanelData
	{
	}
	public partial class UIBossFight : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIBossFightData ?? new UIBossFightData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{

			ActionKit.Delay(3f, () =>
			{
				CloseSelf();
			}).Start(this);
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
