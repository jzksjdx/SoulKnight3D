using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:131083b7-d7a3-4851-a4e2-f561cb8c6889
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
		[SerializeField]
		public UnityEngine.UI.RawImage MinimapRawImage;
		[SerializeField]
		public UnityEngine.UI.Text MinimapLevelText;
		[SerializeField]
		public UnityEngine.UI.Text LevelFlagText;
		
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
			MinimapRawImage = null;
			MinimapLevelText = null;
			LevelFlagText = null;
			
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
