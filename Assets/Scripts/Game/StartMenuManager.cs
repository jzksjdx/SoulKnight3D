using UnityEngine;
using QFramework;
using System.Collections.Generic;

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

        private void Awake()
        {
			Instance = this;
        }

        private void OnDestroy()
        {
			Instance = null;
        }

        void Start()
		{
			ResKit.Init();
            UIKit.OpenPanel<UIStartMenuPanel>();
			AudioKit.PlayMusic("bgm_room");

            _selectedCharacterInt = this.GetSystem<SaveSystem>().LoadInt("Character");
            UpdateSelectedCharacter(_selectedCharacterInt);
        }

		public void UpdateSelectedCharacter(int index)
		{
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
