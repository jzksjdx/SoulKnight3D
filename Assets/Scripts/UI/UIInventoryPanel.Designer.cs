using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:a23d8f8c-d94c-47a6-9a3c-8418f9f8d275
	public partial class UIInventoryPanel
	{
		public const string Name = "UIInventoryPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform UIHotbarRoot;
		[SerializeField]
		public UnityEngine.UI.Button BtnAddItem1;
		[SerializeField]
		public UnityEngine.UI.Button BtnSubitem1;
		
		private UIInventoryPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			UIHotbarRoot = null;
			BtnAddItem1 = null;
			BtnSubitem1 = null;
			
			mData = null;
		}
		
		public UIInventoryPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIInventoryPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIInventoryPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
