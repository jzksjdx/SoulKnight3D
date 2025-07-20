using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:5a4db9b4-5d0d-4e57-8d47-5cf17659d0df
	public partial class UILoadingPanel
	{
		public const string Name = "UILoadingPanel";
		
		
		private UILoadingPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public UILoadingPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UILoadingPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UILoadingPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
