using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:73f8be8c-f614-4085-8ba6-28faa4ad9787
	public partial class UIMobileControlPanel
	{
		public const string Name = "UIMobileControlPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button JoystickAttack;
		[SerializeField]
		public UnityEngine.UI.Button JoystickJump;
		[SerializeField]
		public UnityEngine.UI.Button BtnSpecialAttack;
		
		private UIMobileControlPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			JoystickAttack = null;
			JoystickJump = null;
			BtnSpecialAttack = null;
			
			mData = null;
		}
		
		public UIMobileControlPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIMobileControlPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIMobileControlPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
