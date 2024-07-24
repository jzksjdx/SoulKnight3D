using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:e573dac3-5a16-4592-a10c-db90001cfbff
	public partial class UIInventoryPanel
	{
		public const string Name = "UIInventoryPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform UIHotbarRoot;
		[SerializeField]
		public UnityEngine.UI.Button BtnOpenInventory;
		[SerializeField]
		public UnityEngine.UI.Button BtnAddItem1;
		[SerializeField]
		public UnityEngine.UI.Button BtnSubitem1;
		/// <summary>
		/// 

		/// </summary>
		[SerializeField]
		public UnityEngine.RectTransform StoragePanel;
		[SerializeField]
		public CraftingSystem CraftingSystem;
		
		private UIInventoryPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			UIHotbarRoot = null;
			BtnOpenInventory = null;
			BtnAddItem1 = null;
			BtnSubitem1 = null;
			StoragePanel = null;
			CraftingSystem = null;
			
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
