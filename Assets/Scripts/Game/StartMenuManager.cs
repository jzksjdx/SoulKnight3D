using UnityEngine;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace SoulKnight3D
{
	public partial class StartMenuManager : ViewController, IController
    {
		public static StartMenuManager Instance;

		private int _selectedCharacterInt = 0;
		/// <summary>
		/// 0: Knight
		/// 1: Rouge
		/// </summary>
		[SerializeField] private List<GameObject> _characters = new List<GameObject>();

        private static bool s_WebAudioUnlocked;
        private bool _waitingForWebAudioUnlock;

        private void Awake()
        {
			Instance = this;
        }

        private void OnDestroy()
        {
			Instance = null;
        }

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_waitingForWebAudioUnlock &&
                (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0))
            {
                _waitingForWebAudioUnlock = false;
                s_WebAudioUnlocked = true;
                PlayMenuMusic();
            }
#endif
        }

		private IEnumerator Start()
		{
			if (!ResMgr.ResMgrInited)
			{
				yield return ResKit.InitAsync();
			}

			yield return LocalizationSettings.InitializationOperation;
            yield return UIKit.OpenPanelAsync<UIStartMenuPanel>();
			StartMenuMusic();

            _selectedCharacterInt = Mathf.Clamp(
                this.GetSystem<SaveSystem>().LoadInt("Character"),
                0,
                _characters.Count - 1);
            UpdateSelectedCharacter(_selectedCharacterInt);
        }

        private void StartMenuMusic()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!s_WebAudioUnlocked)
            {
                _waitingForWebAudioUnlock = true;
                return;
            }
#endif
            PlayMenuMusic();
        }

        private static void PlayMenuMusic()
        {
            AudioKit.PlayMusic("bgm_room");
        }

		public void UpdateSelectedCharacter(int index)
		{
			if (_characters.Count == 0)
			{
				return;
			}

			index = Mathf.Clamp(index, 0, _characters.Count - 1);
            foreach(GameObject character in _characters)
			{
				character.Hide();
			}
			_characters[index].Show();
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}
