using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SoulKnight3D
{
	public partial class GameController : ViewController, IController
	{
        public static GameController Instance;
       
        [Header("Game Level")]
        [SerializeField] private List<GameObject> PlayerPrefabs;
        private Vector3 _playerSpawnPoint = new Vector3(45, 0.05f, 45);
        public GameFloorSO GameFloor;
        public MapGenerator MapGenerator;
        //public int Floor = 1;
        public int Level = 1;

        [Header("MoreMountains")]
        public MoreMountains.Tools.MMSoundManager MMSoundManager;
        public MMF_Player DamageNumber;
        public Transform FloatingTextPos;
        public MMF_Player CritNumber;
        public MMF_Player CritText;

        public EasyEvent OnRoomClear = new EasyEvent();

        private void Awake()
        {
            Instance = this;
            if (PlayerController.Instance == null)
            {
                int characterIndex = this.GetSystem<SaveSystem>().LoadInt("Character");
                Instantiate(PlayerPrefabs[characterIndex]);
            }

            Level = this.GetSystem<SaveSystem>().LoadInt("Level", 1);
            MapGenerator.EnemyWaveSOs = GameFloor.GameLevels[Level - 1].LevelWaves;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        void Start()
		{
            this.GetSystem<AudioSystem>().MusicVolume.RegisterWithInitValue((value) =>
            {
				MMSoundManager.SetVolumeMusic(value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.GetSystem<AudioSystem>().SoundVolume.RegisterWithInitValue((value) =>
            {
                MMSoundManager.SetVolumeSfx(value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            if (this.GetSystem<ControlSystem>().IsMobile)
            {
                UIKit.OpenPanel<UIMobileControlPanel>();
            }
            UIKit.OpenPanel<UIGamePanel>();
            UIKit.GetPanel<UIGamePanel>().UpdateUiLevelTexts(Level);

            PlayerController.Instance.PlayerAttack.SwitchWeapon();
            PlayerController.Instance.gameObject.Hide();
            ActionKit.Delay(0.1f, () => 
            {
                PlayerController.Instance.transform.position = _playerSpawnPoint;
                PlayerController.Instance.gameObject.Show();
            }).Start(this);
            
            AudioKit.PlayMusic("bgm_1Low");

            UIMinimapUpdater.Instance.UpdateMap();
        }

        public void SpawnDamageText(int value, Vector3 position)
        {
            FloatingTextPos.position = position;
            MMF_FloatingText floatingText = DamageNumber?.GetFeedbackOfType<MMF_FloatingText>();
            floatingText.Value = value.ToString();
            DamageNumber?.PlayFeedbacks();
        }

        public void SpawnCritText(int value, Vector3 position)
        {
            FloatingTextPos.position = position;
            MMF_FloatingText floatingText = CritNumber?.GetFeedbackOfType<MMF_FloatingText>();
            floatingText.Value = value.ToString();
            CritNumber?.PlayFeedbacks();
            CritText?.PlayFeedbacks();
        }

        public void ToggleGameFreeze(bool isFrozen)
        {
            this.GetSystem<ControlSystem>().ToggleCursor(isFrozen);
            Time.timeScale = isFrozen ? 0 : 1;
        }

        public void SaveCurrentLevel(int level)
        {
            this.GetSystem<SaveSystem>().SaveInt("Level", level);
        }

        public void EnterNextLevel()
        {
            UIKit.OpenPanel<UILoadingPanel>();
            SaveCurrentLevel(Level + 1);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitToMainScreen()
        {
            Destroy(PlayerController.Instance.gameObject);
            SceneManager.LoadScene(0);
            UIKit.ClosePanel<UIGamePanel>();
            UIKit.HidePanel<UIMobileControlPanel>();
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}
