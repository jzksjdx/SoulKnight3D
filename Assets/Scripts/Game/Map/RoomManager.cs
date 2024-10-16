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
        [SerializeField] private List<RoomGate> _gates;
        private EnemyWaveSO _enemyWaves;

        // room index from map generator
        [SerializeField] private int _key;

        // distance from room center to gate
        private float _radius;

        [SerializeField] private RoomType _roomType = RoomType.Battle;
        [SerializeField] private RoomStatus _status = RoomStatus.Unexplored;

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

        public RoomManager SetKey(int key)
        {
            _key = key;
            return this;
        }

        public RoomManager SetRoomType(RoomType type)
        {
            _roomType = type;
            if (type == RoomType.Reward)
            {
                GameObject newReward = Instantiate(DungeonChest, transform.position, Quaternion.identity);
                newReward.transform.Translate(new Vector3(0, 0.043f, 0));
            }
            return this;
        }

        public RoomManager SetRoomStatus(RoomStatus status)
        {
            _status = status;
            return this;
        }

        public void CompleteSetup()
        {
            if (_status != RoomStatus.Unexplored || _roomType != RoomType.Battle) { return; }
            // setup mapitems
            GameObject roomItems = Instantiate(_roomItemPresets[Random.Range(0, _roomItemPresets.Count)], transform)
                .Position(transform.position);
            if (roomItems.GetComponentInChildren<SpikeTilesController>())
            {
                _spikeTilesController = roomItems.GetComponentInChildren<SpikeTilesController>();
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

                    if (_status != RoomStatus.Unexplored) { return; }
                    foreach (RoomGate mGate in _gates)
                    {
                        mGate.ToggleGate();
                    }
                    _status = RoomStatus.InBattle;
                    AudioKit.PlaySound("fx_door");
                    //Debug.Log("Closing Door");
                    StartCoroutine(WaveWorkFlow());
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
            _status = RoomStatus.Explored;

            yield return null;
        }

        private void PlayerEntersRoom()
        {
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
