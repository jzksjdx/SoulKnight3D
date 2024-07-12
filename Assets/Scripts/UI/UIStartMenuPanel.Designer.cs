using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:fa6d199a-ffdc-48f4-bc5a-18a03bdcd8f3
	public partial class UIStartMenuPanel
	{
		public const string Name = "UIStartMenuPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button StartButton;
		[SerializeField]
		public UnityEngine.UI.Button BtnInstruction;
		[SerializeField]
		public UnityEngine.UI.Button BtnQuit;
		[SerializeField]
		public UnityEngine.UI.Button CreditButton;
		[SerializeField]
		public UnityEngine.RectTransform HelpPanel;
		[SerializeField]
		public UnityEngine.UI.Button BtnCloseHelp;
		
		private UIStartMenuPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			StartButton = null;
			BtnInstruction = null;
			BtnQuit = null;
			CreditButton = null;
			HelpPanel = null;
			BtnCloseHelp = null;
			
			mData = null;
		}
		
		public UIStartMenuPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIStartMenuPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIStartMenuPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
