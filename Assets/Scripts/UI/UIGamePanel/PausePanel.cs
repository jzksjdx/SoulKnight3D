/****************************************************************************
 * 2024.6 Zach’s MacBook Pro
 ****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
	public partial class PausePanel : UIElement, IController
	{
		private void Awake()
		{
            BtnHome.onClick.AddListener(() =>
            {
                Time.timeScale = 1;
                GameController.Instance.QuitToMainScreen();
            });

            BtnResume.onClick.AddListener(() =>
            {
                HidePausePanel();
            });

            BtnSettings.onClick.AddListener(() =>
            {
                AudioKit.PlaySound("fx_btn");
                UIKit.OpenPanel<UISettingsPanel>();
            });
        }

        private void HidePausePanel()
        {
            AudioKit.PlaySound("fx_btn");
            this.GetSystem<ControlSystem>().ToggleCursor(false);
            Time.timeScale = 1;
            this.Hide();
        }

        protected override void OnBeforeDestroy()
		{
		}

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}