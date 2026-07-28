using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SoulKnight3D
{
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _roomItemPresets;
        [SerializeField] private LayerMask _itemLayerMask;
        private List<RoomGate> _gates;
        private IReadOnlyList<EnemyWaveGroup> _enemyWaveGroups = Array.Empty<EnemyWaveGroup>();
        private BossEncounterDataSO _bossEncounter;
        private GameObject _generatedPortal;

        [Header("Minimap")]
        [SerializeField] private SpriteRenderer _roomIcon;
        [SerializeField] private Sprite IconHome, IconChest, IconSpecial, IconBoss, IconProtal;
        [HideInInspector] public SpriteRenderer MinimapTile;
        [HideInInspector] public List<SpriteRenderer> HallwayMinimapTiles = new List<SpriteRenderer>();
        [HideInInspector] public List<RoomManager> ConnectedRooms = new List<RoomManager>();
        private Color _unexploredColor = Color.clear; // room not connected not entered
        private Color _detectedColor = new Color(0.3f, 0.3f, 0.3f, 0.78f); // room connected but not entered
        private Color _exploredColor = new Color(1f, 1f, 1f, 0.78f); // room entereds
        public Transform IconTransform;
        private PlayerController _player;

        [Header("Room Parameters")]
        // room index from map generator
        private int _key;

        // distance from room center to gate
        private float _radius;

        public RoomType Type = RoomType.Battle;
        public RoomStatus Status = RoomStatus.Unexplored;

        [Header("Chest Prefabs")]
        public GameObject WhiteChest;
        public GameObject DungeonChest;

        [Header("Merchant Room")]
        [SerializeField] private GameObject _merchantRoomPrefab;
        [SerializeField] private WeaponDropPoolSO _merchantWeaponPool;
        [SerializeField] private List<GameObject> _merchantPotionPrefabs = new List<GameObject>();
        [SerializeField] private GameObject _merchantPriceLabelPrefab;
        [SerializeField, Min(0f)] private float _merchantPotionStockYOffset = 0.2f;
        [SerializeField, Min(0f)] private float _merchantPriceIncreasePerLevel = 0.15f;

        [Header("Special Room")]
        [SerializeField] private List<SpecialRoomContentGroup> _specialRoomContentGroups =
            new List<SpecialRoomContentGroup>();
        [SerializeField] private float _specialRoomYOffset = 0.02f;

        private static readonly Dictionary<SpecialRoomContentType, int>
            LastSpawnedRunByContentType =
                new Dictionary<SpecialRoomContentType, int>();

        // room objects references
        private List<GameObject> _enemies = new List<GameObject>();
        private SpikeTilesController _spikeTilesController;
        private static readonly WaitForSeconds EnemyWavePollDelay = new WaitForSeconds(0.8f);
        private const int SpawnPositionMaxAttempts = 32;

        public enum RoomType
        {
            Home, Battle, Reward, Portal, Boss, Merchant, Special
        }

        public enum RoomStatus
        {
            Unexplored, InBattle, Explored
        }

        private enum SpecialRoomContentType
        {
            Mine,
            Mount
        }

        [Serializable]
        private sealed class SpecialRoomContentGroup
        {
            [SerializeField] private SpecialRoomContentType _contentType;
            [SerializeField, Min(0f)] private float _weight = 1f;
            [SerializeField] private bool _oncePerRun;
            [SerializeField] private List<GameObject> _prefabs =
                new List<GameObject>();

            public SpecialRoomContentType ContentType => _contentType;
            public float Weight => _weight;
            public bool OncePerRun => _oncePerRun;
            public List<GameObject> Prefabs => _prefabs;
        }

        private void Awake()
        {
            IconTransform = _roomIcon.transform;
            _player = PlayerController.Instance;
        }

        private void Start()
        {
            if (_player == null)
            {
                _player = PlayerController.Instance;
            }
        }

        private void Update()
        {
            if (_roomIcon.sprite == null || IconTransform == null || !IconTransform.gameObject.activeInHierarchy) { return; }
            if (_player == null)
            {
                _player = PlayerController.Instance;
                if (_player == null) { return; }
            }
            IconTransform.rotation = Quaternion.Euler(90f, _player.transform.eulerAngles.y, 0f);
        }

        public RoomManager SetDimension(Vector3 position, float radius)
        {
            transform.position = position;
            _radius = radius;
            return this;
        }

        public RoomManager SetGates(List<RoomGate> gates)
        {
            _gates = gates;
            return this;
        }

        public RoomManager SetEnemyWaves(EnemyWaveSO waves)
        {
            _enemyWaveGroups = waves != null
                ? waves.EnemyWaveGroups
                : Array.Empty<EnemyWaveGroup>();
            return this;
        }

        public RoomManager SetEnemyWavePlan(EnemyWavePlan plan)
        {
            _enemyWaveGroups = plan?.WaveGroups ?? Array.Empty<EnemyWaveGroup>();
            return this;
        }

        public RoomManager SetBossEncounter(BossEncounterDataSO bossEncounter)
        {
            _bossEncounter = bossEncounter;
            return this;
        }

        public RoomManager SetPortal(GameObject generatedPortal)
        {
            _generatedPortal = generatedPortal;
            return this;
        }

        public RoomManager SetKey(int key)
        {
            _key = key;
            return this;
        }

        public RoomManager SetRoomType(RoomType type)
        {
            Type = type;
            if (type == RoomType.Reward)
            {
                GameObject newReward = Instantiate(DungeonChest, transform.position, Quaternion.identity);
                newReward.transform.SetParent(transform);
                newReward.transform.Translate(new Vector3(0, 0.043f, 0));
                _roomIcon.sprite = IconChest;
            }
            else if (type == RoomType.Merchant)
            {
                SpawnMerchantRoom();
                _roomIcon.sprite = IconChest;
            }
            else if (type == RoomType.Special)
            {
                SpawnSpecialRoom();
                _roomIcon.sprite = IconSpecial;
            }
            else if (type == RoomType.Boss)
            {
                _roomIcon.sprite = IconBoss;
            }
            else if (type == RoomType.Home)
            {
                _roomIcon.sprite = IconHome;
            }
            else if (type == RoomType.Portal)
            {
                _roomIcon.sprite = IconProtal;
            }
            return this;
        }

        private void SpawnMerchantRoom()
        {
            if (_merchantRoomPrefab == null)
            {
                Debug.LogWarning("Merchant room prefab is not configured. Falling back to a reward chest.");
                GameObject fallbackReward = Instantiate(DungeonChest, transform.position,
                    Quaternion.identity, transform);
                fallbackReward.transform.Translate(new Vector3(0f, 0.043f, 0f));
                return;
            }

            GameObject merchantObject = Instantiate(_merchantRoomPrefab, transform.position,
                GetEntranceFacingRotation(), transform);
            MerchantRoom merchantRoom = merchantObject.GetComponent<MerchantRoom>();
            if (merchantRoom == null)
            {
                merchantRoom = merchantObject.AddComponent<MerchantRoom>();
            }

            int level = GameController.Instance != null ? GameController.Instance.Level : 1;
            merchantRoom.Configure(_merchantWeaponPool, _merchantPotionPrefabs, level,
                _merchantPriceIncreasePerLevel, _merchantPriceLabelPrefab,
                _merchantPotionStockYOffset);
        }

        private void SpawnSpecialRoom()
        {
            int runId = GameController.Instance != null
                ? GameController.Instance.RunsStarted
                : 0;
            List<SpecialRoomContentGroup> eligibleGroups =
                new List<SpecialRoomContentGroup>();
            float totalWeight = 0f;

            for (int i = 0; i < _specialRoomContentGroups.Count; i++)
            {
                SpecialRoomContentGroup group = _specialRoomContentGroups[i];
                if (group == null) { continue; }

                group.Prefabs.RemoveAll(prefab => prefab == null);
                bool alreadySpawnedThisRun =
                    LastSpawnedRunByContentType.TryGetValue(
                        group.ContentType, out int lastRunId) &&
                    lastRunId == runId;
                if (group.Prefabs.Count == 0 || group.Weight <= 0f ||
                    (group.OncePerRun && alreadySpawnedThisRun))
                {
                    continue;
                }

                eligibleGroups.Add(group);
                totalWeight += group.Weight;
            }

            if (eligibleGroups.Count == 0 || totalWeight <= 0f)
            {
                Debug.LogWarning($"Special room '{name}' has no content prefabs configured.");
                return;
            }

            float roll = Random.value * totalWeight;
            SpecialRoomContentGroup selectedGroup =
                eligibleGroups[eligibleGroups.Count - 1];
            for (int i = 0; i < eligibleGroups.Count; i++)
            {
                roll -= eligibleGroups[i].Weight;
                if (roll <= 0f)
                {
                    selectedGroup = eligibleGroups[i];
                    break;
                }
            }

            GameObject contentPrefab = selectedGroup.Prefabs[
                Random.Range(0, selectedGroup.Prefabs.Count)];
            Vector3 spawnPosition = transform.position + Vector3.up * _specialRoomYOffset;
            Instantiate(contentPrefab, spawnPosition, GetEntranceFacingRotation(), transform);

            if (selectedGroup.OncePerRun)
            {
                LastSpawnedRunByContentType[selectedGroup.ContentType] = runId;
            }
        }

        private Quaternion GetEntranceFacingRotation()
        {
            if (_gates == null)
            {
                return Quaternion.identity;
            }

            for (int i = 0; i < _gates.Count; i++)
            {
                if (_gates[i] == null) { continue; }

                Vector3 direction = _gates[i].transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            return Quaternion.identity;
        }

        public RoomManager SetRoomStatus(RoomStatus status)
        {
            Status = status;
            return this;
        }

        // set minimap tiles
        public void AddHallwayMinimapTile(SpriteRenderer tile)
        {
            HallwayMinimapTiles.Add(tile);
        }

        public void AddConnectedRoom(RoomManager room)
        {
            ConnectedRooms.Add(room);
        }

        public SpriteRenderer GetRoomTile()
        {
            return MinimapTile;
        }

        public void InitializeForMinimap()
        {
            if (Type == RoomType.Home) { return; }
            MinimapTile.color = _unexploredColor;
            foreach(SpriteRenderer hallwayTile in HallwayMinimapTiles)
            {
                hallwayTile.color = _unexploredColor;
            }
            IconTransform.Hide();
        }

        public void CompleteSetup()
        {
            if (Status != RoomStatus.Unexplored) { return; }
            
            if (Type == RoomType.Battle)
            {
                // setup map items
                GameObject roomItems = Instantiate(_roomItemPresets[Random.Range(0, _roomItemPresets.Count)], transform)
                .Position(transform.position);
                if (roomItems.GetComponentInChildren<SpikeTilesController>())
                {
                    _spikeTilesController = roomItems.GetComponentInChildren<SpikeTilesController>();
                }
            }
            else if (Type == RoomType.Boss)
            {
                _generatedPortal.Hide();
            }
            

            // setup gates
            foreach (RoomGate gate in _gates)
            {
                gate.OnPlayerEnter.Register(() =>
                {
                    // determine if player in room
                    if (_player == null)
                    {
                        _player = PlayerController.Instance;
                    }
                    if (_player == null) { return; }

                    if ((_player.transform.position - transform.position).sqrMagnitude > _radius * _radius) {
                        PlayerExitsRoom();
                        return;
                    }
                    PlayerEntersRoom();

                    if (Type != RoomType.Battle && Type != RoomType.Boss) { return; }
                    if (Status != RoomStatus.Unexplored) { return; }
                    foreach (RoomGate mGate in _gates)
                    {
                        mGate.ToggleGate();
                    }
                    Status = RoomStatus.InBattle;
                    GameController.Instance?.SetRoomBattleState(true);
                    AudioKit.PlaySound("fx_door");
                    _roomIcon.Hide();
                    //Debug.Log("Closing Door");

                    if (Type == RoomType.Boss)
                    {
                        StartCoroutine(BossFightWorkFlow());
                    }
                    else if (Type == RoomType.Battle)
                    {
                        StartCoroutine(WaveWorkFlow());
                    }
                    
                }).UnRegisterWhenGameObjectDestroyed(gameObject);
            }
        }

        private IEnumerator BossFightWorkFlow()
        {
            if (_bossEncounter == null || !_bossEncounter.IsValid)
            {
                Debug.LogError($"Boss room {_key} has no valid boss encounter.");
                CompleteBossRoomWithoutBoss();
                yield break;
            }

            bool introFinished = false;
            UIBossFightData introData = new UIBossFightData(
                _bossEncounter,
                () => introFinished = true);
            yield return UIKit.OpenPanelAsync<UIBossFight>(UILevel.PopUI, introData);
            AudioKit.PlayMusic("bgm_boss");

            while (!introFinished)
            {
                yield return null;
            }

            GameObject generatedBoss = Instantiate(
                _bossEncounter.BossPrefab,
                transform.position + Vector3.up * 0.05f,
                Quaternion.identity,
                transform);
            BossEnemy boss = generatedBoss.GetComponent<BossEnemy>();
            if (boss == null)
            {
                Debug.LogError(
                    $"Boss prefab '{_bossEncounter.BossPrefab.name}' has no BossEnemy component.");
                Destroy(generatedBoss);
                CompleteBossRoomWithoutBoss();
                yield break;
            }

            AudioKit.PlaySound("fx_show_up");

            UIGamePanel gamePanel = UIKit.GetPanel<UIGamePanel>();
            if (gamePanel != null)
            {
                gamePanel.BossHealthBar.fillAmount = 1f;
                gamePanel.BossHealthRect.Show();
            }

            boss.OnDeath.Register(() =>
            {
                if (_generatedPortal != null) { _generatedPortal.Show(); }
                Status = RoomStatus.Explored;
                GameController.Instance?.SetRoomBattleState(false);
                GameController.Instance?.OnRoomClear.Trigger();
                AudioKit.StopMusic();
                _roomIcon.Show();
                foreach (RoomGate gate in _gates)
                {
                    gate.ToggleGate();
                }
            }).UnRegisterWhenGameObjectDestroyed(generatedBoss);
        }

        private void CompleteBossRoomWithoutBoss()
        {
            Status = RoomStatus.Explored;
            GameController.Instance?.SetRoomBattleState(false);
            if (_generatedPortal != null) { _generatedPortal.Show(); }
            _roomIcon.Show();

            foreach (RoomGate gate in _gates)
            {
                gate.ToggleGate();
            }
        }

        private IEnumerator WaveWorkFlow()
        {
            float reducedRadius = _radius * 0.9f;
            if (_enemyWaveGroups.Count == 0)
            {
                Debug.LogError($"Battle room {_key} has no enemy waves. Clearing it to avoid trapping the player.");
            }

            foreach (EnemyWaveGroup waveGroup in _enemyWaveGroups)
            {
                if (waveGroup == null)
                {
                    continue;
                }

                foreach(EnemyWave enemyWave in waveGroup.Waves)
                {
                    if (enemyWave == null || enemyWave.EnemyPrefab == null || enemyWave.Count <= 0)
                    {
                        continue;
                    }

                    for(int i = 1; i <= enemyWave.Count; i ++)
                    {
                        // ensure no room items around
                        Vector3 spawnPosition = GetOpenPosition(reducedRadius, 0.05f, 0.5f);

                        // generate new enemy
                        GameObject newEnemy = Instantiate(enemyWave.EnemyPrefab, spawnPosition, Quaternion.identity);
                        newEnemy.transform.SetParent(transform);
                        Enemy enemy = newEnemy.GetComponent<Enemy>();
                        if (enemy == null)
                        {
                            Debug.LogError($"Spawned prefab '{enemyWave.EnemyPrefab.name}' has no Enemy component.");
                            Destroy(newEnemy);
                            continue;
                        }

                        _enemies.Add(newEnemy);
                        enemy.OnDeath.Register(() =>
                        {
                            _enemies.Remove(newEnemy);
                        }).UnRegisterWhenGameObjectDestroyed(newEnemy);
                    }
                }
                AudioKit.PlaySound("fx_show_up");

                // wait for current wave
                while (_enemies.Count > 0)
                {
                    _enemies.RemoveAll(enemy => enemy == null);
                    yield return EnemyWavePollDelay;
                }
            }

            // handle minimap cam
            if (Type == RoomType.Battle || Type == RoomType.Boss)
            {
                _player.MinimapCam.TogglePosition(false);
            }
            // set connected room icon visible
            foreach(RoomManager room in ConnectedRooms)
            {
                room.IconTransform.Show();
            }
            // room clear
            GameController.Instance?.SetRoomBattleState(false);
            GameController.Instance.OnRoomClear.Trigger();

            // spawn white chest
            Vector3 spawnChestPosition = GetOpenPosition(_radius, 0.043f, 0.5f);
            AudioKit.PlaySound("fx_show_up");
            GameObject newWhiteChest = Instantiate(WhiteChest, spawnChestPosition, Quaternion.identity);
            newWhiteChest.transform.SetParent(transform);

            // Open gates
            foreach (RoomGate mGate in _gates)
            {
                mGate.ToggleGate();
            }
            AudioKit.PlaySound("fx_door");
            Status = RoomStatus.Explored;

            yield return null;
        }

        public void PlayerEntersRoom()
        {
            if (Type == RoomType.Portal || Type == RoomType.Reward ||
                Type == RoomType.Merchant || Type == RoomType.Special)
            {
                Status = RoomStatus.Explored;
            }
            if (MinimapTile != null)
            {
                MinimapTile.color = _exploredColor;
            }
            foreach (SpriteRenderer hallwayTile in HallwayMinimapTiles)
            {
                hallwayTile.color = _exploredColor;
            }
            foreach(RoomManager room in ConnectedRooms)
            {
                if (room.Status == RoomStatus.Unexplored)
                {
                    if (room.MinimapTile != null)
                    {
                        room.MinimapTile.color = _detectedColor;
                    }
                }
            }

            // handle minimap cam
            if (Status == RoomStatus.Unexplored)
            {
                if (Type == RoomType.Battle || Type == RoomType.Boss)
                {
                    if (_player == null)
                    {
                        _player = PlayerController.Instance;
                    }
                    if (_player == null) { return; }
                    _player.MinimapCam.TogglePosition(true);
                }
            }

            // handle spikes if any
            if (_spikeTilesController)
            {
                _spikeTilesController.ToggleSpikeTiles(true);
            }
        }

        private void PlayerExitsRoom()
        {
            if (_spikeTilesController)
            {
                _spikeTilesController.ToggleSpikeTiles(false);
            }
        }

        private Vector3 GetOpenPosition(float radius, float yOffset, float checkRadius)
        {
            for (int attempt = 0; attempt < SpawnPositionMaxAttempts; attempt++)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-radius, radius), yOffset, Random.Range(-radius, radius));
                Vector3 candidate = transform.position + randomOffset;
                if (!Physics.CheckSphere(candidate, checkRadius, _itemLayerMask))
                {
                    return candidate;
                }
            }

            return transform.position + Vector3.up * yOffset;
        }
    }
}
