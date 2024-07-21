using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:8b6fc6cf-b6c5-4849-9793-c04eb05816ff
	public partial class UIInventoryPanel
	{
		public const string Name = "UIInventoryPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform UIHotbarRoot;
		[SerializeField]
		public UnityEngine.UI.Button BtnAddItem1;
		[SerializeField]
		public UnityEngine.UI.Button BtnSubitem1;
		/// <summary>
		/// 

		/// </summary>
		[SerializeField]
		public UnityEngine.RectTransform StoragePanel;
		
		private UIInventoryPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			UIHotbarRoot = null;
			BtnAddItem1 = null;
			BtnSubitem1 = null;
			StoragePanel = null;
			
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
