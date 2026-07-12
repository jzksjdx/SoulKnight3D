using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;
using UnityEngine.SceneManagement;
using System.Collections;
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
        public bool IsFinalLevel = false;

        [Header("MoreMountains")]
        public MoreMountains.Tools.MMSoundManager MMSoundManager;
        public MMF_Player DamageNumber;
        public Transform FloatingTextPos;
        public MMF_Player CritNumber;
        public MMF_Player CritText;

        [Header("BugMode")]
        [SerializeField] private List<GameObject> _bugModeChips;

        public EasyEvent OnRoomClear = new EasyEvent();
        public EasyEvent OnReturnMenu = new EasyEvent();
        private bool _isSceneTransitioning;

        private void Awake()
        {
            Instance = this;
            if (PlayerController.Instance == null)
            {
                int characterIndex = Mathf.Clamp(this.GetSystem<SaveSystem>().LoadInt("Character"), 0, PlayerPrefabs.Count - 1);
                Instantiate(PlayerPrefabs[characterIndex]);
            }

            Level = Mathf.Clamp(this.GetSystem<SaveSystem>().LoadInt("Level", 1), 1, GameFloor.GameLevels.Count);
            IsFinalLevel = Level == 3;
            MapGenerator.EnemyWaveSOs = GameFloor.GameLevels[Level - 1].LevelWaves;
            MapGenerator.BossPrefab = GameFloor.BossPrefabs[Random.Range(0, GameFloor.BossPrefabs.Count)];
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
            StartCoroutine(PreparePlayerAfterMapReady());
            
            AudioKit.PlayMusic("bgm_1Low");

            // bug mode
            if (this.GetSystem<SaveSystem>().LoadBool("BugMode"))
            {
                this.GetSystem<SaveSystem>().SaveBool("BugMode", false);
                Instantiate(_bugModeChips[Random.Range(0, _bugModeChips.Count)]);
            } else
            {
                UIKit.GetPanel<UIGamePanel>().UiBugMode.Hide();
            }
        }

        private IEnumerator PreparePlayerAfterMapReady()
        {
            while (MapGenerator != null && !MapGenerator.IsMapReady)
            {
                yield return null;
            }

            if (PlayerController.Instance == null)
            {
                yield break;
            }

            PlayerController.Instance.transform.position = _playerSpawnPoint;
            PlayerController.Instance.gameObject.Show();
            UIMinimapUpdater.Instance?.UpdateMap();
        }

        public void SpawnDamageText(int value, Vector3 position)
        {
            FloatingTextPos.position = position;
            MMF_FloatingText floatingText = DamageNumber?.GetFeedbackOfType<MMF_FloatingText>();
            if (floatingText == null) { return; }
            floatingText.Value = value.ToString();
            DamageNumber?.PlayFeedbacks();
        }

        public void SpawnCritText(int value, Vector3 position)
        {
            FloatingTextPos.position = position;
            MMF_FloatingText floatingText = CritNumber?.GetFeedbackOfType<MMF_FloatingText>();
            if (floatingText == null) { return; }
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
            if (_isSceneTransitioning)
            {
                return;
            }

            SaveCurrentLevel(Level + 1);
            StartCoroutine(LoadCurrentSceneAsync());
        }

        private IEnumerator LoadCurrentSceneAsync()
        {
            _isSceneTransitioning = true;
            Time.timeScale = 1;
            UIKit.OpenPanel<UILoadingPanel>(UILevel.PopUI);
            yield return null;

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            while (loadOperation != null && !loadOperation.isDone)
            {
                yield return null;
            }
        }

        public void QuitToMainScreen()
        {
            OnReturnMenu.Trigger();
            if (PlayerController.Instance != null)
            {
                Destroy(PlayerController.Instance.gameObject);
            }
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
