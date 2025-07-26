using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _roomItemPresets;
        [SerializeField] private LayerMask _itemLayerMask;
        private List<RoomGate> _gates;
        private EnemyWaveSO _enemyWaves;
        private GameObject _bossPrefab;
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

        // room objects references
        private List<GameObject> _enemies = new List<GameObject>();
        private SpikeTilesController _spikeTilesController;

        public enum RoomType
        {
            Home, Battle, Reward, Portal, Boss
        }

        public enum RoomStatus
        {
            Unexplored, InBattle, Explored
        }

        private void Start()
        {
            IconTransform = _roomIcon.transform;
            _player = PlayerController.Instance;
        }

        private void Update()
        {
            if (_roomIcon.sprite == null) { return; }
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
            _enemyWaves = waves;
            return this;
        }

        public RoomManager SetBossPrefab(GameObject bossPrefab)
        {
            _bossPrefab = bossPrefab;
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
                newReward.transform.Translate(new Vector3(0, 0.043f, 0));
                _roomIcon.sprite = IconChest;
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
                    if ((PlayerController.Instance.transform.position - transform.position).magnitude > _radius) {
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
                    AudioKit.PlaySound("fx_door");
                    _roomIcon.Hide();
                    //Debug.Log("Closing Door");

                    if (Type == RoomType.Boss)
                    {
                        GameObject generatedBoss = Instantiate(_bossPrefab, transform.position + Vector3.up * 0.05f, Quaternion.identity);
                        generatedBoss.transform.SetParent(transform);
                        AudioKit.PlaySound("fx_show_up");
                        AudioKit.PlayMusic("bgm_boss");
                        UIKit.GetPanel<UIGamePanel>().BossHealthBar.fillAmount = 1;
                        UIKit.GetPanel<UIGamePanel>().BossHealthRect.Show();
                        UIKit.OpenPanel<UIBossFight>();
                        generatedBoss.GetComponent<Werewolf>().OnDeath.Register(() =>
                        {
                            _generatedPortal.Show();
                            AudioKit.StopMusic();
                            _roomIcon.Show();
                        }).UnRegisterWhenGameObjectDestroyed(generatedBoss);
                    }
                    else if (Type == RoomType.Battle)
                    {
                        StartCoroutine(WaveWorkFlow());
                    }
                    
                }).UnRegisterWhenGameObjectDestroyed(gameObject);
            }
        }

        private IEnumerator WaveWorkFlow()
        {
            float reducedRadius = _radius * 0.9f;
            foreach (EnemyWaveGroup waveGroup in _enemyWaves.EnemyWaveGroups)
            {
                foreach(EnemyWave enemyWave in waveGroup.Waves)
                {
                    for(int i = 1; i <= enemyWave.Count; i ++)
                    {
                        // ensure no room items around
                        Vector3 spawnPosition;
                        bool isValidPosition;
                        do
                        {
                            Vector3 randomOffset = new Vector3(Random.Range(-reducedRadius, reducedRadius), 0.05f, Random.Range(-reducedRadius, reducedRadius));
                            spawnPosition = transform.position + randomOffset;
                            isValidPosition = !Physics.CheckSphere(spawnPosition, 0.5f, _itemLayerMask);
                        }
                        while (!isValidPosition);

                        // generate new enemy
                        GameObject newEnemy = Instantiate(enemyWave.EnemyPrefab, spawnPosition, Quaternion.identity);
                        newEnemy.transform.SetParent(transform);
                        _enemies.Add(newEnemy);

                        newEnemy.GetComponent<Enemy>().OnDeath.Register(() =>
                        {
                            _enemies.Remove(newEnemy);
                        }).UnRegisterWhenGameObjectDestroyed(newEnemy);
                    }
                }
                AudioKit.PlaySound("fx_show_up");

                // wait for current wave
                while (_enemies.Count > 0)
                {
                    yield return new WaitForSeconds(1f);
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
            GameController.Instance.OnRoomClear.Trigger();

            // spawn white chest
            Vector3 spawnChestPosition;
            bool isChestPosValid;
            do
            {
                Vector3 randomOffset = new Vector3(Random.Range(-_radius, _radius), 0.043f, Random.Range(-_radius, _radius));
                spawnChestPosition = transform.position + randomOffset;
                isChestPosValid = !Physics.CheckSphere(spawnChestPosition, 0.5f, _itemLayerMask);
            }
            while (!isChestPosValid);
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
            if (Type == RoomType.Portal || Type == RoomType.Reward)
            {
                Status = RoomStatus.Explored;
            }
            MinimapTile.color = _exploredColor;
            foreach (SpriteRenderer hallwayTile in HallwayMinimapTiles)
            {
                hallwayTile.color = _exploredColor;
            }
            foreach(RoomManager room in ConnectedRooms)
            {
                if (room.Status == RoomStatus.Unexplored)
                {
                    room.MinimapTile.color = _detectedColor;
                }
            }

            // handle minimap cam
            if (Status == RoomStatus.Unexplored)
            {
                if (Type == RoomType.Battle || Type == RoomType.Boss)
                {
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
    }
}
