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
            SetAnimatorUpdateMode();
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
            SetAnimatorUpdateMode();
            BringToFront();
		}

        protected override void OnShow()
        {
            BringToFront();
        }

        private void SetAnimatorUpdateMode()
        {
            foreach (Animator animator in GetComponentsInChildren<Animator>(true))
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
        }
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
