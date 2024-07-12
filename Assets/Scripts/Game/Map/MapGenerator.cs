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
        [SerializeField] private RoomManager RoomManagerPrefab;
        [SerializeField] private GameObject PortalPrefab;

        private int[,] map;
        private int gridWidth = 5;
        private int gridHeight = 5;
        private List<int> range = new List<int>();

        // room length * 2
        private float mapScale = 22;

        private Dictionary<int, RoomData> generatedRooms = new Dictionary<int, RoomData>();
        private List<GameObject> generatedHallways = new List<GameObject>();

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
            InitializeMap();
            AddRoom();
            //PrintMap();

            // TODO: Generate Room Script
            //Debug.Log(generatedRooms.Count);
            foreach(var room in generatedRooms)
            {
                //Debug.Log("Room #" + room.Key + " has " + room.Value.gates.Count + " gates");
                var newRoom = Instantiate(RoomManagerPrefab, transform);
                newRoom
                    .SetDimension(room.Value.position, mapScale / 4)
                    .SetGates(room.Value.gates)
                    .SetEnemyWaves(EnemyWaveSOs[Random.Range(0, EnemyWaveSOs.Count)])
                    .SetKey(room.Key)
                    .SetRoomType(room.Value.type)
                    .SetRoomStatus(room.Value.status)
                    .CompleteSetup();
            }
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
            //Instantiate(roomPrefab, new Vector3(2 * mapScale, 0, 2 * mapScale), roomPrefab.transform.rotation);
            Vector3 initialRoomPos = new Vector3(2 * mapScale, 0, 2 * mapScale);
            GenerateRoom(initialRoomPos);
            generatedRooms.Add(startRoom, new RoomData(initialRoomPos, new List<RoomGate>(), RoomManager.RoomType.Home, RoomManager.RoomStatus.Explored));
        }

        private void AddRoom()
        {
            int nextRoomNumber = AddRange(12);
            int nextRoomNumber1 = AddRange(nextRoomNumber, 2);
            int nextRoomNumber2 = AddRange(nextRoomNumber1, 2, isReward: true);
            int nextRoomNumber3 = AddRange(nextRoomNumber2, 1, isFinal: true);
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

                //Instantiate(roomPrefab, roomWorldPosition, roomPrefab.transform.rotation);
                //Debug.Log("Generated room at: (" + pos / gridWidth + ", " + pos % gridWidth + ")");
                GenerateRoom(newRoomWorldPosition);

                
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

                // generate hall way
                var hallwayGenObj = Instantiate(hallwayGenPrefab, hallWayPosition, Quaternion.identity);
                var hallwayGen = hallwayGenObj.GetComponent<RoomGenerator>();
                hallwayGen.id = 1000 + generatedHallways.Count;
                EventSystem.instance.SetGridSize(1000 + generatedHallways.Count,
                    newRoomKey / gridWidth == oldRoomKey / gridWidth ? 3 : 11,
                    newRoomKey / gridWidth == oldRoomKey / gridWidth ? 11 : 3);
                EventSystem.instance.SetRoomSeed(1000 + generatedHallways.Count, 0, Random.Range(0, 100000));
                hallwayGen.GenerateRoom(hallwayGen.id);
                generatedHallways.Add(hallwayGenObj);
                hallwayGen.roomCollider.transform.localScale = new Vector3(1, 0.1f, 1);
                hallwayGen.roomCollider.transform.Translate(Vector3.down * 0.28f);

                // asign generated rooms
                generatedRooms[oldRoomKey].gates.Add(oldRoomGate.GetComponent<RoomGate>());

                generatedRooms.Add(newRoomKey, new RoomData(newRoomWorldPosition, new List<RoomGate>()));
                generatedRooms[newRoomKey].gates.Add(newRoomGate.GetComponent<RoomGate>());

                if (isFinal)
                {
                    // generate portal or boss
                    var finalRoom = generatedRooms[newRoomKey];
                    finalRoom.status = RoomManager.RoomStatus.Explored;
                    finalRoom.type = RoomManager.RoomType.Portal;
                    generatedRooms[newRoomKey] = finalRoom;
                    Instantiate(PortalPrefab, newRoomWorldPosition, Quaternion.identity);
                }
            }

            if (isReward && deadEndKey != -99)
            {
                var rewardRoom = generatedRooms[deadEndKey];
                rewardRoom.status = RoomManager.RoomStatus.Explored;
                rewardRoom.type = RoomManager.RoomType.Reward;
                generatedRooms[deadEndKey] = rewardRoom;
            }

            return nextRoomKey;
        }

        private void GenerateRoom(Vector3 roomWorldPosition)
        {
            var roomGenObj = Instantiate(roomGenPrefab, roomWorldPosition, Quaternion.identity);
            var roomGen = roomGenObj.GetComponent<RoomGenerator>();
            roomGen.id = generatedRooms.Count;
            EventSystem.instance.SetRoomSeed(generatedRooms.Count, 0, Random.Range(0, 100000));
            roomGen.GenerateRoom(roomGen.id);
            // adjust room collider
            roomGen.roomCollider.transform.localScale = new Vector3(1, 0.1f, 1);
            roomGen.roomCollider.transform.Translate(Vector3.down * 0.28f);

            // generate light
            Vector3 lightPos = new Vector3(roomWorldPosition.x, roomWorldPosition.y + 2.2f, roomWorldPosition.z);
            Instantiate(roomLightPrefab, lightPos, Quaternion.identity);
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
