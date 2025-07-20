using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:ca6e3b2b-2ccd-4a66-91c2-a8704985870e
	public partial class UIEndPanel
	{
		public const string Name = "UIEndPanel";
		
		[SerializeField]
		public UnityEngine.UI.Text EndTitle;
		[SerializeField]
		public UnityEngine.UI.Button BtnContinue;
		[SerializeField]
		public UnityEngine.UI.Button BtnSubscribe;
		
		private UIEndPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			EndTitle = null;
			BtnContinue = null;
			BtnSubscribe = null;
			
			mData = null;
		}
		
		public UIEndPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIEndPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIEndPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
