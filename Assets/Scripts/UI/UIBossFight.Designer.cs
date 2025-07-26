using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:a516c386-c571-4316-b04f-44bb3c9a18d0
	public partial class UIBossFight
	{
		public const string Name = "UIBossFight";
		
		
		private UIBossFightData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public UIBossFightData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIBossFightData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIBossFightData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
