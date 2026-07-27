using UnityEngine;
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using RoomGen;

namespace SoulKnight3D
{
	public partial class MapGenerator : ViewController
	{
        public EnemySpawnProfileSO EnemySpawnProfile;
        public int EnemySpawnLevel = 1;
        public int EnemySpawnSeed;
        public List<EnemyWaveSO> EnemyWaveSOs = new List<EnemyWaveSO>();
        public BossEncounterDataSO BossEncounter;

        [SerializeField] private RoomManager RoomManagerPrefab;
        [SerializeField] private bool _shouldGenerateMap = true;

        [Header("Merchant Rooms")]
        [SerializeField, Range(0f, 1f)] private float _merchantRoomChance = 0.5f;
        [SerializeField, Min(1)] private int _merchantMinimumLevel = 1;

        [Header("Special Rooms")]
        [SerializeField, Range(0f, 1f)] private float _specialRoomChance = 1f;
        [SerializeField, Range(0f, 1f)] private float _bossExtraBattleRoomChance = 0.5f;

        [Header("Room Objects")]
        [SerializeField] private GameObject PortalPrefab;
        [SerializeField] private GameObject roomGenPrefab;
        [SerializeField] private GameObject roomGatePrefab;
        [SerializeField] private GameObject hallwayGenPrefab;
        [SerializeField] private GameObject roomLightPrefab;

        [Header("Minimap")]
        [SerializeField] private GameObject MinimapTile;

        private int[,] map;
        private int gridWidth = 5;
        private int gridHeight = 5;
        private List<int> range = new List<int>();

        // room length * 2
        private float mapScale = 22;

        private Dictionary<int, RoomData> _roomDataDict = new Dictionary<int, RoomData>();
        private Dictionary<int, RoomManager> _generatedRooms = new Dictionary<int, RoomManager>(); // saves room managers
        private List<GameObject> _generatedHallways = new List<GameObject>();
        private GameObject _generatedPortal;
        public bool IsMapReady { get; private set; }
        public EasyEvent OnMapReady = new EasyEvent();

        private struct RoomData
        {
            public Vector3 position;
            public List<RoomGate> gates;
            public RoomManager.RoomType type;
            public RoomManager.RoomStatus status;

            public RoomData(Vector3 _position, List<RoomGate> _gates,
                RoomManager.RoomType _type = RoomManager.RoomType.Battle, RoomManager.RoomStatus _status = RoomManager.RoomStatus.Unexplored)
            {
                position = _position;
                gates = _gates;
                type = _type;
                status = _status;
            }
        }

        private IEnumerator Start()
        {
            IsMapReady = false;
            map = new int[gridHeight, gridWidth];
            if (_shouldGenerateMap)
            {
                InitializeMap();
                yield return null;
                yield return AddRoomRoutine();
            }

            IsMapReady = true;
            OnMapReady.Trigger();
            UIKit.ClosePanel<UILoadingPanel>();
        }

        void InitializeMap()
        {
            for (int i = 0; i < gridHeight; i++)
            {
                for (int j = 0; j < gridWidth; j++)
                {
                    map[i, j] = 0; // 0 indicates unoccupied
                }
            }

            // Start from the middle of the grid
            int startRoom = gridWidth / 2 + gridHeight / 2 * gridWidth;
            map[startRoom / gridWidth, startRoom % gridWidth] = 1; // 1 indicates occupied

            // generate home room
            Vector3 initialRoomPos = new Vector3(2 * mapScale, 0, 2 * mapScale);
            _roomDataDict.Add(startRoom, new RoomData(initialRoomPos, new List<RoomGate>(), RoomManager.RoomType.Home, RoomManager.RoomStatus.Explored));
            SetupRoomManager(startRoom);
            GameObject homeRoom = GenerateRoom(startRoom, initialRoomPos);
            homeRoom.transform.SetParent(_generatedRooms[startRoom].transform);
            _generatedRooms[startRoom].InitializeForMinimap();
        }

        private IEnumerator AddRoomRoutine()
        {
            const int startRoomKey = 12;
            bool isBossFloor = GameController.Instance != null &&
                GameController.Instance.IsFinalLevel;
            int mandatoryBattleRoomCount = isBossFloor ? 3 : 2;
            List<RoomManager.RoomType?> branchRoomTypes =
                BuildBranchRoomTypes(mandatoryBattleRoomCount, isBossFloor);

            int currentRoomKey = startRoomKey;
            yield return AddRangeRoutine(
                currentRoomKey,
                value => currentRoomKey = value,
                1,
                RoomManager.RoomType.Battle,
                RoomManager.RoomType.Battle,
                1 + (branchRoomTypes[0].HasValue ? 1 : 0));
            if (currentRoomKey == startRoomKey)
            {
                Debug.LogError("Map generation could not place the first battle room.");
                yield break;
            }

            for (int i = 0; i < mandatoryBattleRoomCount; i++)
            {
                bool connectsToFinalRoom = i == mandatoryBattleRoomCount - 1;
                RoomManager.RoomType pathRoomType = connectsToFinalRoom
                    ? (isBossFloor
                        ? RoomManager.RoomType.Boss
                        : RoomManager.RoomType.Portal)
                    : RoomManager.RoomType.Battle;
                RoomManager.RoomType? branchRoomType = branchRoomTypes[i];

                int nextRoomKey = currentRoomKey;
                yield return AddRangeRoutine(
                    currentRoomKey,
                    value => nextRoomKey = value,
                    branchRoomType.HasValue ? 2 : 1,
                    pathRoomType,
                    branchRoomType ?? RoomManager.RoomType.Battle,
                    connectsToFinalRoom
                        ? 0
                        : 1 + (branchRoomTypes[i + 1].HasValue ? 1 : 0));
                if (nextRoomKey == currentRoomKey)
                {
                    Debug.LogError(
                        $"Map generation stopped at room {currentRoomKey}: " +
                        "the required path and branch rooms could not be placed.");
                    yield break;
                }
                currentRoomKey = nextRoomKey;
            }

            _generatedRooms[startRoomKey].PlayerEntersRoom();
        }

        private List<RoomManager.RoomType?> BuildBranchRoomTypes(
            int mandatoryBattleRoomCount, bool isBossFloor)
        {
            List<RoomManager.RoomType?> roomTypes =
                new List<RoomManager.RoomType?>
            {
                RoomManager.RoomType.Reward
            };
            if (Random.value < _specialRoomChance)
            {
                roomTypes.Add(RoomManager.RoomType.Special);
            }
            if (isBossFloor && Random.value < _bossExtraBattleRoomChance)
            {
                roomTypes.Add(RoomManager.RoomType.Battle);
            }

            while (roomTypes.Count < mandatoryBattleRoomCount)
            {
                roomTypes.Add(null);
            }

            for (int i = roomTypes.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (roomTypes[i], roomTypes[swapIndex]) =
                    (roomTypes[swapIndex], roomTypes[i]);
            }

            return roomTypes;
        }

        public int AddRange(int oldRoomKey, int newRoomCount = 1,
            RoomManager.RoomType pathRoomType = RoomManager.RoomType.Battle,
            RoomManager.RoomType branchRoomType = RoomManager.RoomType.Battle,
            int requiredPathRoomExits = 0)
        {
            if (!TryPrepareRoomRange(oldRoomKey, newRoomCount,
                    requiredPathRoomExits, out int nextRoomKey))
            {
                return oldRoomKey;
            }

            foreach (int newRoomKey in range)
            {
                GenerateRoomConnection(oldRoomKey, newRoomKey, nextRoomKey,
                    pathRoomType, branchRoomType);
            }

            return nextRoomKey;
        }

        private IEnumerator AddRangeRoutine(int oldRoomKey, Action<int> onComplete,
            int newRoomCount = 1,
            RoomManager.RoomType pathRoomType = RoomManager.RoomType.Battle,
            RoomManager.RoomType branchRoomType = RoomManager.RoomType.Battle,
            int requiredPathRoomExits = 0)
        {
            if (!TryPrepareRoomRange(oldRoomKey, newRoomCount,
                    requiredPathRoomExits, out int nextRoomKey))
            {
                onComplete?.Invoke(oldRoomKey);
                yield break;
            }

            foreach (int newRoomKey in range)
            {
                GenerateRoomConnection(oldRoomKey, newRoomKey, nextRoomKey,
                    pathRoomType, branchRoomType);
                yield return null;
            }

            onComplete?.Invoke(nextRoomKey);
        }

        private bool TryPrepareRoomRange(int oldRoomKey, int newRoomCount,
            int requiredPathRoomExits, out int nextRoomKey)
        {
            nextRoomKey = oldRoomKey;

            int x = oldRoomKey % gridWidth;
            int y = oldRoomKey / gridWidth;
            range.Clear();

            if (y > 0) range.Add(oldRoomKey - gridWidth);
            if (y < gridHeight - 1) range.Add(oldRoomKey + gridWidth);
            if (x > 0) range.Add(oldRoomKey - 1);
            if (x < gridWidth - 1) range.Add(oldRoomKey + 1);

            for (int i = range.Count - 1; i >= 0; i--)
            {
                int pos = range[i];
                if (map[pos / gridWidth, pos % gridWidth] != 0)
                {
                    range.RemoveAt(i);
                }
            }

            if (range.Count < newRoomCount)
            {
                return false;
            }

            List<int> pathCandidates = range.FindAll(
                roomKey => CountAvailableNeighbors(roomKey) >= requiredPathRoomExits);
            if (pathCandidates.Count == 0)
            {
                return false;
            }

            nextRoomKey = pathCandidates[Random.Range(0, pathCandidates.Count)];
            range.Remove(nextRoomKey);
            while (range.Count > newRoomCount - 1)
            {
                range.RemoveAt(Random.Range(0, range.Count));
            }
            range.Insert(0, nextRoomKey);
            return true;
        }

        private int CountAvailableNeighbors(int roomKey)
        {
            int x = roomKey % gridWidth;
            int y = roomKey / gridWidth;
            int availableCount = 0;

            if (y > 0 && map[y - 1, x] == 0) { availableCount++; }
            if (y < gridHeight - 1 && map[y + 1, x] == 0) { availableCount++; }
            if (x > 0 && map[y, x - 1] == 0) { availableCount++; }
            if (x < gridWidth - 1 && map[y, x + 1] == 0) { availableCount++; }

            return availableCount;
        }

        private void GenerateRoomConnection(int oldRoomKey, int newRoomKey,
            int pathRoomKey, RoomManager.RoomType pathRoomType,
            RoomManager.RoomType branchRoomType)
        {
            map[newRoomKey / gridWidth, newRoomKey % gridWidth] = 1;

            Vector3 newRoomWorldPosition = new Vector3(newRoomKey / gridWidth * mapScale, 0, newRoomKey % gridWidth * mapScale);
            Vector3 hallWayPosition = new Vector3((float)(newRoomKey / gridWidth + oldRoomKey / gridWidth) / 2 * mapScale, 0, (float)(newRoomKey % gridWidth + oldRoomKey % gridWidth) / 2 * mapScale);

            Vector3 oldRoomGatePos;
            Vector3 newRoomGatePos;
            Quaternion rotation1 = Quaternion.identity;
            Quaternion rotation2 = Quaternion.identity;

            if (newRoomKey / gridWidth == oldRoomKey / gridWidth)
            {
                if (newRoomKey > oldRoomKey)
                {
                    newRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z + mapScale / 4);
                    oldRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z - mapScale / 4);
                }
                else
                {
                    newRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z - mapScale / 4);
                    oldRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z + mapScale / 4);
                }
            }
            else
            {
                if (newRoomKey > oldRoomKey)
                {
                    oldRoomGatePos = new Vector3(hallWayPosition.x - mapScale / 4, 0, hallWayPosition.z);
                    newRoomGatePos = new Vector3(hallWayPosition.x + mapScale / 4, 0, hallWayPosition.z);
                }
                else
                {
                    oldRoomGatePos = new Vector3(hallWayPosition.x + mapScale / 4, 0, hallWayPosition.z);
                    newRoomGatePos = new Vector3(hallWayPosition.x - mapScale / 4, 0, hallWayPosition.z);
                }

                rotation1 = Quaternion.Euler(0, 90, 0);
                rotation2 = Quaternion.Euler(0, 90, 0);
            }

            GameObject oldRoomGate = Instantiate(roomGatePrefab, oldRoomGatePos, rotation1);
            GameObject newRoomGate = Instantiate(roomGatePrefab, newRoomGatePos, rotation2);
            RoomGate oldRoomGateComponent = oldRoomGate.GetComponent<RoomGate>();
            RoomGate newRoomGateComponent = newRoomGate.GetComponent<RoomGate>();
            _roomDataDict[oldRoomKey].gates.Add(oldRoomGateComponent);

            RoomManager.RoomType roomType = newRoomKey == pathRoomKey
                ? pathRoomType
                : branchRoomType;
            if (roomType == RoomManager.RoomType.Reward &&
                ShouldGenerateMerchantRoom())
            {
                roomType = RoomManager.RoomType.Merchant;
            }

            bool isFinalRoom = roomType == RoomManager.RoomType.Portal ||
                roomType == RoomManager.RoomType.Boss;
            if (isFinalRoom)
            {
                _generatedPortal = Instantiate(PortalPrefab, newRoomWorldPosition, Quaternion.identity);
            }

            _roomDataDict.Add(newRoomKey, new RoomData(newRoomWorldPosition, new List<RoomGate> { newRoomGateComponent }, roomType));

            SetupRoomManager(newRoomKey, isFinalRoom ? _generatedPortal : null);
            GameObject newRoom = GenerateRoom(newRoomKey, newRoomWorldPosition);

            GameObject hallwayGenObj = Instantiate(hallwayGenPrefab, hallWayPosition, Quaternion.identity);
            RoomGenerator hallwayGen = hallwayGenObj.GetComponent<RoomGenerator>();
            hallwayGen.id = 1000 + _generatedHallways.Count;
            bool isHorizontal = newRoomKey / gridWidth == oldRoomKey / gridWidth;
            EventSystem.instance.SetGridSize(1000 + _generatedHallways.Count,
                isHorizontal ? 3 : 11,
                isHorizontal ? 11 : 3);
            EventSystem.instance.SetRoomSeed(1000 + _generatedHallways.Count, 0, Random.Range(0, 100000));
            hallwayGen.GenerateRoom(hallwayGen.id);
            hallwayGen.parent.transform.SetParent(hallwayGenObj.transform);
            _generatedHallways.Add(hallwayGenObj);
            hallwayGen.roomCollider.transform.localScale = new Vector3(1, 0.1f, 1);
            hallwayGen.roomCollider.transform.Translate(Vector3.down * 0.28f);

            GameObject minimapTile = Instantiate(MinimapTile, hallwayGenObj.transform);
            minimapTile.transform.localScale = isHorizontal ? new Vector3(3, 11, 0) : new Vector3(11, 3, 0);
            _generatedRooms[newRoomKey].AddHallwayMinimapTile(minimapTile.GetComponent<SpriteRenderer>());
            _generatedRooms[oldRoomKey].AddHallwayMinimapTile(minimapTile.GetComponent<SpriteRenderer>());
            _generatedRooms[newRoomKey].InitializeForMinimap();
            _generatedRooms[newRoomKey].AddConnectedRoom(_generatedRooms[oldRoomKey]);
            _generatedRooms[oldRoomKey].AddConnectedRoom(_generatedRooms[newRoomKey]);

            newRoom.transform.SetParent(_generatedRooms[newRoomKey].transform);
            oldRoomGate.transform.SetParent(_generatedRooms[oldRoomKey].transform);
            newRoomGate.transform.SetParent(_generatedRooms[newRoomKey].transform);
        }

        private bool ShouldGenerateMerchantRoom()
        {
            int level = GameController.Instance != null
                ? GameController.Instance.Level
                : EnemySpawnLevel;
            return level >= _merchantMinimumLevel && Random.value < _merchantRoomChance;
        }

        private GameObject GenerateRoom(int roomKey, Vector3 roomWorldPosition)
        {
            GameObject roomGenObj = Instantiate(roomGenPrefab, roomWorldPosition, Quaternion.identity);
            RoomGenerator roomGen = roomGenObj.GetComponent<RoomGenerator>();
            roomGen.id = _roomDataDict.Count;
            EventSystem.instance.SetRoomSeed(_roomDataDict.Count, 0, Random.Range(0, 100000));
            roomGen.GenerateRoom(roomGen.id);
            roomGen.parent.transform.SetParent(roomGenObj.transform);
            // adjust room collider
            roomGen.roomCollider.transform.localScale = new Vector3(1, 0.1f, 1);
            roomGen.roomCollider.transform.Translate(Vector3.down * 0.28f);

            // generate light
            Vector3 lightPos = new Vector3(roomWorldPosition.x, roomWorldPosition.y + 2.2f, roomWorldPosition.z);
            GameObject light = Instantiate(roomLightPrefab, lightPos, Quaternion.identity);
            light.transform.SetParent(roomGenObj.transform);

            // generate minimap
            GameObject minimapTile = Instantiate(MinimapTile, roomGenObj.transform);
            minimapTile.transform.localScale *= 11;
            _generatedRooms[roomKey].MinimapTile = minimapTile.GetComponent<SpriteRenderer>();

            return roomGenObj;
        }

        private void SetupRoomManager(int roomKey, GameObject generatedPortal = null)
        {
            RoomManager newRoom = Instantiate(RoomManagerPrefab, transform);
            if (generatedPortal)
            {
                newRoom.SetPortal(generatedPortal);
            }
            newRoom
                .SetDimension(_roomDataDict[roomKey].position, mapScale / 4)
                .SetGates(_roomDataDict[roomKey].gates)
                .SetBossEncounter(BossEncounter)
                .SetKey(roomKey)
                .SetRoomType(_roomDataDict[roomKey].type)
                .SetRoomStatus(_roomDataDict[roomKey].status);

            if (_roomDataDict[roomKey].type == RoomManager.RoomType.Battle)
            {
                ConfigureBattleWaves(newRoom, roomKey);
            }

            newRoom.CompleteSetup();
            _generatedRooms.Add(roomKey, newRoom);
        }

        private void ConfigureBattleWaves(RoomManager room, int roomKey)
        {
            if (EnemySpawnProfile != null)
            {
                int seed = EnemyWavePlanner.CombineSeed(EnemySpawnSeed, roomKey,
                    EnemySpawnProfile.SeedSalt);
                room.SetEnemyWavePlan(EnemyWavePlanner.Generate(EnemySpawnProfile,
                    EnemySpawnLevel, seed));
                return;
            }

            if (EnemyWaveSOs.Count > 0)
            {
                room.SetEnemyWaves(EnemyWaveSOs[Random.Range(0, EnemyWaveSOs.Count)]);
                return;
            }

            Debug.LogError($"No enemy spawn profile or fixed waves are configured for room {roomKey}.");
        }

    }
}
