using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:aac3e3e3-1a10-4a30-a223-55f70d8d678e
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
		public UnityEngine.UI.Button SkillButton;
		[SerializeField]
		public UnityEngine.UI.Image SkillImage;
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
			SkillButton = null;
			SkillImage = null;
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
