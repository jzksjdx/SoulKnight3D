using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace SoulKnight3D
{
	public partial class GameController : ViewController, IController
	{
        private const string RunsStartedSaveKey = "RunsStarted";

        public static GameController Instance;
       
        [Header("Game Level")]
        [SerializeField] private List<GameObject> PlayerPrefabs;
        private Vector3 _playerSpawnPoint = new Vector3(45, 0.05f, 45);
        public GameFloorSO GameFloor;
        public MapGenerator MapGenerator;
        //public int Floor = 1;
        public int Level = 1;
        public bool IsFinalLevel = false;
        public int RunsStarted { get; private set; } = int.MaxValue;

        [Header("Level 1 Starter Chest")]
        [SerializeField] private GameObject _levelOneStarterChestPrefab;
        [SerializeField, Min(0.5f)] private float _levelOneStarterChestDistance = 2.25f;

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

            SaveSystem saveSystem = this.GetSystem<SaveSystem>();
            Level = Mathf.Clamp(saveSystem.LoadInt("Level", 1), 1, GameFloor.GameLevels.Count);
            RunsStarted = saveSystem.LoadInt(RunsStartedSaveKey);
            if (Level == 1)
            {
                RunsStarted++;
                saveSystem.SaveInt(RunsStartedSaveKey, RunsStarted);
            }
            IsFinalLevel = Level == 3;
            GameLevel currentLevel = GameFloor.GameLevels[Level - 1];
            MapGenerator.EnemySpawnProfile = currentLevel.EnemySpawnProfile;
            MapGenerator.EnemySpawnLevel = Level;
            MapGenerator.EnemySpawnSeed = Random.Range(1, int.MaxValue);
            MapGenerator.EnemyWaveSOs = currentLevel.LevelWaves;
            MapGenerator.BossEncounter = GameFloor.SelectBoss(Random.value);
            if (MapGenerator.BossEncounter == null)
            {
                Debug.LogError($"Game floor '{GameFloor.name}' has no valid weighted boss encounter.");
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private IEnumerator Start()
		{
            if (!ResMgr.ResMgrInited)
            {
                yield return ResKit.InitAsync();
            }

            yield return LocalizationSettings.InitializationOperation;

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
                yield return UIKit.OpenPanelAsync<UIMobileControlPanel>();
            }
            yield return UIKit.OpenPanelAsync<UIGamePanel>();
            UIKit.GetPanel<UIGamePanel>().UpdateUiLevelTexts(Level);

            PlayerAttack playerAttack = PlayerController.Instance.PlayerAttack;
            playerAttack.Skill?.CancelForLevelTransition();
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
            SpawnLevelOneStarterChest(PlayerController.Instance.transform);
            PlayerController.Instance.gameObject.Show();
            PlayerController.Instance.PlayerAttack.RestoreWeaponState();
            UIMinimapUpdater.Instance?.UpdateMap();
        }

        private void SpawnLevelOneStarterChest(Transform playerTransform)
        {
            if (Level != 1 || _levelOneStarterChestPrefab == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            Vector3 spawnPosition = playerTransform.position + forward * _levelOneStarterChestDistance;
            Physics.SyncTransforms();
            if (TryFindFloorHeight(spawnPosition, playerTransform.position.y + 0.1f,
                out float floorHeight))
            {
                spawnPosition.y = floorHeight;
            }
            else
            {
                spawnPosition.y = _playerSpawnPoint.y - 0.05f;
            }

            Vector3 facingDirection = -forward;
            float facingYaw = Mathf.Atan2(facingDirection.x, facingDirection.z) * Mathf.Rad2Deg;
            float snappedYaw = Mathf.Round(facingYaw / 90f) * 90f;
            Quaternion rotation = Quaternion.Euler(0f, snappedYaw, 0f);
            GameObject chest = Instantiate(_levelOneStarterChestPrefab, spawnPosition, rotation);
            SnapNonTriggerColliderToFloor(chest, spawnPosition.y);
        }

        private static bool TryFindFloorHeight(Vector3 position, float maximumHeight,
            out float floorHeight)
        {
            RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 5f,
                Vector3.down, 10f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            floorHeight = float.NegativeInfinity;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.normal.y > 0.5f && hit.point.y <= maximumHeight)
                {
                    floorHeight = Mathf.Max(floorHeight, hit.point.y);
                }
            }

            return !float.IsNegativeInfinity(floorHeight);
        }

        private static void SnapNonTriggerColliderToFloor(GameObject target, float floorHeight)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            float lowestPoint = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger)
                {
                    lowestPoint = Mathf.Min(lowestPoint, colliders[i].bounds.min.y);
                }
            }

            if (!float.IsPositiveInfinity(lowestPoint))
            {
                target.transform.position += Vector3.up * (floorHeight - lowestPoint);
            }
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

            _isSceneTransitioning = true;
            PreparePlayerForLevelTransition();
            SaveCurrentLevel(Level + 1);
            StartCoroutine(LoadCurrentSceneAsync());
        }

        private static void PreparePlayerForLevelTransition()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null)
            {
                return;
            }

            player.PlayerAttack.Skill?.CancelForLevelTransition();
            player.PlayerAttack.CancelCurrentWeaponCharge();
            player.SelfRigidbody.velocity = Vector3.zero;
            player.SelfRigidbody.angularVelocity = Vector3.zero;
        }

        private IEnumerator LoadCurrentSceneAsync()
        {
            Time.timeScale = 1;
            yield return UIKit.OpenPanelAsync<UILoadingPanel>(UILevel.PopUI);

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
