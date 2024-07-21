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
                SceneManager.LoadScene(0);
                UIKit.ClosePanel<UIGamePanel>();
                UIKit.ClosePanel<UIMobileControlPanel>();
            });

            BtnResume.onClick.AddListener(() =>
            {
                HidePausePanel();
            });

            BtnRestart.onClick.AddListener(() =>
            {
                AudioKit.PlaySound("buttonclick");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });

            AudioSystem audioSystem = this.GetSystem<AudioSystem>();

            SliderMusic.value = audioSystem.MusicVolume.Value;
            SliderSound.value = audioSystem.SoundVolume.Value;

            SliderMusic.onValueChanged.AddListener((value) =>
            {
                audioSystem.ChangeMusicVolume(value);
            });

            SliderSound.onValueChanged.AddListener((value) =>
            {
                audioSystem.ChangeSoundVolume(value);
            });
        }

        private void HidePausePanel()
        {
            AudioKit.PlaySound("buttonclick");
            this.GetSystem<ControlSystem>().ToggleCursor(false);
            Time.timeScale = 1;
            Hide();
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