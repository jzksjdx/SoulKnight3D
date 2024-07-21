using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:11200581-58d1-420c-82db-71ff9762a553
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
