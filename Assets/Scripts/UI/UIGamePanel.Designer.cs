using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:8734aff0-3051-418c-8282-2177c1e5de83
	public partial class UIGamePanel
	{
		public const string Name = "UIGamePanel";
		
		[SerializeField]
		public UnityEngine.UI.Image HealthBar;
		[SerializeField]
		public UnityEngine.UI.Text HealthText;
		[SerializeField]
		public UnityEngine.UI.Image ArmorBar;
		[SerializeField]
		public UnityEngine.UI.Text ArmorText;
		[SerializeField]
		public UnityEngine.UI.Image EnergyBar;
		[SerializeField]
		public UnityEngine.UI.Text EnergyText;
		[SerializeField]
		public UnityEngine.UI.Button BtnWeapon;
		[SerializeField]
		public UnityEngine.UI.Image WeaponSprite;
		[SerializeField]
		public UnityEngine.UI.Text EnergyCostText;
		[SerializeField]
		public UnityEngine.UI.Button BtnInteract;
		[SerializeField]
		public UnityEngine.UI.Button PauseButton;
		[SerializeField]
		public PausePanel PausePanel;
		
		private UIGamePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			HealthBar = null;
			HealthText = null;
			ArmorBar = null;
			ArmorText = null;
			EnergyBar = null;
			EnergyText = null;
			BtnWeapon = null;
			WeaponSprite = null;
			EnergyCostText = null;
			BtnInteract = null;
			PauseButton = null;
			PausePanel = null;
			
			mData = null;
		}
		
		public UIGamePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIGamePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIGamePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
