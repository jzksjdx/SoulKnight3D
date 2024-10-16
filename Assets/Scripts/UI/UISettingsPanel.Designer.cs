using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:5cd0d597-a774-4d05-baef-77786a7da59f
	public partial class UISettingsPanel
	{
		public const string Name = "UISettingsPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BtnClose;
		[SerializeField]
		public UnityEngine.UI.Slider SliderMusic;
		[SerializeField]
		public UnityEngine.UI.Slider SliderSound;
		[SerializeField]
		public UnityEngine.UI.Slider SliderSensitivity;
		[SerializeField]
		public UnityEngine.UI.Slider Btns;
		[SerializeField]
		public UnityEngine.UI.Button BtnChinese;
		[SerializeField]
		public UnityEngine.UI.Button BtnEnglish;
		
		private UISettingsPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BtnClose = null;
			SliderMusic = null;
			SliderSound = null;
			SliderSensitivity = null;
			Btns = null;
			BtnChinese = null;
			BtnEnglish = null;
			
			mData = null;
		}
		
		public UISettingsPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UISettingsPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UISettingsPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
