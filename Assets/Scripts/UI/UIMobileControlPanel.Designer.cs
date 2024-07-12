using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:843a7d87-0944-4f68-b9c0-1a83422890ef
	public partial class UIMobileControlPanel
	{
		public const string Name = "UIMobileControlPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button JoystickAttack;
		[SerializeField]
		public UnityEngine.UI.Button JoystickJump;
		
		private UIMobileControlPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			JoystickAttack = null;
			JoystickJump = null;
			
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
