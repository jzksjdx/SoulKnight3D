using UnityEngine;
using QFramework;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using RoomGen;

namespace SoulKnight3D
{
	public partial class MapGenerator : ViewController
	{
        public List<EnemyWaveSO> EnemyWaveSOs = new List<EnemyWaveSO>();
        public GameObject BossPrefab;

        [SerializeField] private RoomManager RoomManagerPrefab;
        [SerializeField] private bool _shouldGenerateMap = true;

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

        private void Start()
        {
            map = new int[gridHeight, gridWidth];
            if (_shouldGenerateMap)
            {
                InitializeMap();
                AddRoom();
                //PrintMap();
            }
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

        private void AddRoom()
        {
            int nextRoomNumber = AddRange(12);
            int nextRoomNumber1 = AddRange(nextRoomNumber, 2);
            int nextRoomNumber2 = AddRange(nextRoomNumber1, 2, isReward: true);
            int nextRoomNumber3 = AddRange(nextRoomNumber2, 1, isFinal: true);
            //int nextRoomNumber3 = AddRange(nextRoomNumber, 1, isFinal: true);

            _generatedRooms[12].PlayerEntersRoom();
        }

        public int AddRange(int oldRoomKey, int newRoomCount = 1, bool isReward = false , bool isFinal = false)
        {
            int x = oldRoomKey % gridWidth;
            int y = oldRoomKey / gridWidth;
            range.Clear();

            // Check all four possible directions
            if (y > 0) range.Add(oldRoomKey - gridWidth); // North
            if (y < gridHeight - 1) range.Add(oldRoomKey + gridWidth); // South
            if (x > 0) range.Add(oldRoomKey - 1); // West
            if (x < gridWidth - 1) range.Add(oldRoomKey + 1); // East

            // check if room is already occupied
            for (int i = range.Count - 1; i >= 0; i--)
            {
                int pos = range[i];
                if (map[pos / gridWidth, pos % gridWidth] != 0) // Already occupied
                {
                    range.RemoveAt(i);
                }
            }

            // remove unwanted rooms
            var roomLeftToRemove = range.Count - newRoomCount;
            for (int j = 0; j < roomLeftToRemove; j++)
            {
                int randomIndex = Random.Range(0, range.Count);
                range.RemoveAt(randomIndex);
            }

            // generate rooms
            int nextRoomKey = range[Random.Range(0, range.Count)];
            int deadEndKey = -99;
            for (int k = 0; k < range.Count; k++)
            {
                int newRoomKey = range[k];
                
                if (newRoomKey != nextRoomKey)
                {
                    deadEndKey = newRoomKey;
                }

                map[newRoomKey / gridWidth, newRoomKey % gridWidth] = 1;

                var newRoomWorldPosition = new Vector3(newRoomKey / gridWidth * mapScale, 0, newRoomKey % gridWidth * mapScale);
                // generate room
                //Debug.Log("Generated room at: (" + pos / gridWidth + ", " + pos % gridWidth + ")");
                var hallWayPosition = new Vector3((float)(newRoomKey / gridWidth + oldRoomKey / gridWidth) / 2 * mapScale, 0, (float)(newRoomKey % gridWidth + oldRoomKey % gridWidth) / 2 * mapScale);
                // generate gate
                Vector3 oldRoomGatePos;
                Vector3 newRoomGatePos;
                Quaternion rotation1 = Quaternion.identity;
                Quaternion rotation2 = Quaternion.identity;

                //Debug.Log("Old room: " + oldRoomKey + ", new room: " + newRoomKey);
                if (newRoomKey / gridWidth == oldRoomKey / gridWidth) // same row
                {
                    if (newRoomKey > oldRoomKey) // new room is on the right
                    {
                        newRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z + mapScale / 4);
                        oldRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z - mapScale / 4);
                    } else
                    {
                        newRoomGatePos = new Vector3(hallWayPosition.x, 0, hallWayPosition.z - mapScale / 4);
                        oldRoomGatePos= new Vector3(hallWayPosition.x, 0, hallWayPosition.z + mapScale / 4);
                    }
                    
                } else
                {
                    if (newRoomKey > oldRoomKey) // new room is on lower column
                    {
                        oldRoomGatePos = new Vector3(hallWayPosition.x - mapScale / 4, 0, hallWayPosition.z);
                        newRoomGatePos = new Vector3(hallWayPosition.x + mapScale / 4, 0, hallWayPosition.z);
                    } else
                    {
                        oldRoomGatePos = new Vector3(hallWayPosition.x + mapScale / 4, 0, hallWayPosition.z);
                        newRoomGatePos = new Vector3(hallWayPosition.x - mapScale / 4, 0, hallWayPosition.z);
                    }
                    
                    rotation1 = Quaternion.Euler(0, 90, 0);
                    rotation2 = Quaternion.Euler(0, 90, 0);
                }
                GameObject oldRoomGate = Instantiate(roomGatePrefab, oldRoomGatePos, rotation1);
                GameObject newRoomGate = Instantiate(roomGatePrefab, newRoomGatePos, rotation2);

                // asign generated rooms
                _roomDataDict[oldRoomKey].gates.Add(oldRoomGate.GetComponent<RoomGate>());
                _roomDataDict.Add(newRoomKey, new RoomData(newRoomWorldPosition, new List<RoomGate>()));
                _roomDataDict[newRoomKey].gates.Add(newRoomGate.GetComponent<RoomGate>());

                // generate portal or boss
                if (isFinal)
                {
                    var finalRoom = _roomDataDict[newRoomKey];
                    //finalRoom.status = RoomManager.RoomStatus.Explored;
                    if (GameController.Instance.IsFinalLevel)
                    {
                        finalRoom.type = RoomManager.RoomType.Boss;
                    } else
                    {
                        finalRoom.type = RoomManager.RoomType.Portal;
                    }
                    _roomDataDict[newRoomKey] = finalRoom;
                    _generatedPortal = Instantiate(PortalPrefab, newRoomWorldPosition, Quaternion.identity);
                }
                // reward
                if (isReward && deadEndKey != -99)
                {
                    var rewardRoom = _roomDataDict[deadEndKey];
                    //rewardRoom.status = RoomManager.RoomStatus.Explored;
                    rewardRoom.type = RoomManager.RoomType.Reward;
                    _roomDataDict[deadEndKey] = rewardRoom;
                }

                // setup room manager
                SetupRoomManager(newRoomKey, _generatedPortal);
                GameObject newRoom = GenerateRoom(newRoomKey, newRoomWorldPosition);

                // generate hallway
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

                // generate minimap for hallway
                GameObject minimapTile = Instantiate(MinimapTile, hallwayGenObj.transform);
                minimapTile.transform.localScale = isHorizontal ? new Vector3(3, 11, 0) : new Vector3(11, 3, 0);
                _generatedRooms[newRoomKey].AddHallwayMinimapTile(minimapTile.GetComponent<SpriteRenderer>());
                _generatedRooms[oldRoomKey].AddHallwayMinimapTile(minimapTile.GetComponent<SpriteRenderer>());
                _generatedRooms[newRoomKey].InitializeForMinimap();
                // add connected rooms
                _generatedRooms[newRoomKey].AddConnectedRoom(_generatedRooms[oldRoomKey]);
                _generatedRooms[oldRoomKey].AddConnectedRoom(_generatedRooms[newRoomKey]);
                // set parent for room gates
                newRoom.transform.SetParent(_generatedRooms[newRoomKey].transform);
                oldRoomGate.transform.SetParent(_generatedRooms[oldRoomKey].transform);
                newRoomGate.transform.SetParent(_generatedRooms[newRoomKey].transform);

                
            }

            return nextRoomKey;
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
                .SetEnemyWaves(EnemyWaveSOs[Random.Range(0, EnemyWaveSOs.Count)])
                .SetBossPrefab(BossPrefab)
                .SetKey(roomKey)
                .SetRoomType(_roomDataDict[roomKey].type)
                .SetRoomStatus(_roomDataDict[roomKey].status)
                .CompleteSetup();
            _generatedRooms.Add(roomKey, newRoom);
            
        }

        private void PrintMap()
        {
            string mapString = "";
            for (int i = 0; i < gridHeight; i++)
            {
                for (int j = 0; j < gridWidth; j++)
                {
                    mapString += map[i, j] + " ";
                }
                mapString += "\n"; // New line for each row
            }
            Debug.Log(mapString);
        }
    }
}
