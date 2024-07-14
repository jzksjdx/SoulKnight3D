using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	// Generate Id:05470252-3ea4-42e0-8680-22f1500319f7
	public partial class UIPvzGamePanel
	{
		public const string Name = "UIPvzGamePanel";
		
		[SerializeField]
		public UnityEngine.Animator TipStart;
		[SerializeField]
		public UnityEngine.Animator TipHugeWave;
		[SerializeField]
		public UnityEngine.Animator TipFinalWave;
		[SerializeField]
		public RectTransform ProgressBar;
		[SerializeField]
		public UnityEngine.UI.Image ProgressBarImage;
		[SerializeField]
		public RectTransform FlagRoot;
		[SerializeField]
		public UnityEngine.RectTransform FlagTemplate;
		[SerializeField]
		public UnityEngine.RectTransform Head;
		
		private UIPvzGamePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TipStart = null;
			TipHugeWave = null;
			TipFinalWave = null;
			ProgressBar = null;
			ProgressBarImage = null;
			FlagRoot = null;
			FlagTemplate = null;
			Head = null;
			
			mData = null;
		}
		
		public UIPvzGamePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIPvzGamePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIPvzGamePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
