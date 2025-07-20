using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:d78bf1f7-c769-4bc5-8610-5caf10363103
	public partial class UIStartMenuPanel
	{
		public const string Name = "UIStartMenuPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform MenuImage;
		[SerializeField]
		public UnityEngine.UI.Button StartButton;
		[SerializeField]
		public UnityEngine.UI.Button BtnInstruction;
		[SerializeField]
		public UnityEngine.UI.Button BtnQuit;
		[SerializeField]
		public UnityEngine.UI.Button BtnSettings;
		[SerializeField]
		public UnityEngine.UI.Button CreditButton;
		[SerializeField]
		public UnityEngine.RectTransform HelpPanel;
		[SerializeField]
		public UnityEngine.UI.Button BtnCloseHelp;
		[SerializeField]
		public UnityEngine.UI.Button BtnSelectCharacterRight;
		[SerializeField]
		public UnityEngine.UI.Button BtnSelectCharacterLeft;
		
		private UIStartMenuPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			MenuImage = null;
			StartButton = null;
			BtnInstruction = null;
			BtnQuit = null;
			BtnSettings = null;
			CreditButton = null;
			HelpPanel = null;
			BtnCloseHelp = null;
			BtnSelectCharacterRight = null;
			BtnSelectCharacterLeft = null;
			
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
