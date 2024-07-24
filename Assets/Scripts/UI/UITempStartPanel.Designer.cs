using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:ad022810-6acc-4916-a8b0-7ab604448b16
	public partial class UITempStartPanel
	{
		public const string Name = "UITempStartPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BtnStart;
		[SerializeField]
		public UnityEngine.UI.Button BtnQuit;
		
		private UITempStartPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BtnStart = null;
			BtnQuit = null;
			
			mData = null;
		}
		
		public UITempStartPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UITempStartPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UITempStartPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
