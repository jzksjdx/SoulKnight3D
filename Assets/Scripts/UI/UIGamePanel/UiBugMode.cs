/****************************************************************************
 * 2025.7 Zach’s MacBook Pro
 ****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public partial class UiBugMode : UIElement
	{
		private string _titleText;
		private string _progressText = " (修复进度: 0/0)";
		private void Awake()
		{
		}

		public void ToggleTexts(bool shouldShow)
		{
			ChipTexts.gameObject.SetActive(shouldShow);
		}

		public void UpdateUi(Sprite icon, string title, string description)
		{
			ChipIcon.sprite = icon;
			_titleText = title;
            Title.text = _titleText + _progressText;
			Description.text = description;
        }

		public void UpdateProgress(int maxProgress, int currProgress)
		{
			_progressText = " (修复进度: " + currProgress + "/" + maxProgress + ")";
            Title.text = _titleText + _progressText;
        }

		protected override void OnBeforeDestroy()
		{
		}
	}
}