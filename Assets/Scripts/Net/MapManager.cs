using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace IsaacLike.Net
{
    [System.Serializable]
    public class MapData
    {
        public string mapName;
        public GameObject mapPrefab;
        public Vector2 mapSize = new Vector2(20, 20);
    }

    public class MapManager : NetworkBehaviour
    {
        public static MapManager Instance { get; private set; }

        [Header("Map Prefabs")]
        [SerializeField] private MapData[] availableMaps;

        [Header("Procedural Generation")]
        [SerializeField] private bool useProceduralGeneration = false;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private Vector2 arenaSize = new Vector2(20, 20);
        [SerializeField] private int obstacleCount = 10;

        private GameObject _currentMap;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            if (useProceduralGeneration)
            {
                GenerateProceduralMap();
            }
            else if (availableMaps != null && availableMaps.Length > 0)
            {
                LoadRandomMap();
            }
        }

        private void LoadRandomMap()
        {
            if (availableMaps == null || availableMaps.Length == 0) return;

            int index = Random.Range(0, availableMaps.Length);
            LoadMap(index);
        }

        public void LoadMap(int index)
        {
            if (!IsServer) return;

            if (index < 0 || index >= availableMaps.Length)
            {
                Debug.LogWarning($"Map index {index} out of range!");
                return;
            }

            if (_currentMap != null)
            {
                Destroy(_currentMap);
            }

            MapData mapData = availableMaps[index];
            if (mapData.mapPrefab != null)
            {
                _currentMap = Instantiate(mapData.mapPrefab, Vector3.zero, Quaternion.identity);
                Debug.Log($"Loaded map: {mapData.mapName}");
            }
        }

        private void GenerateProceduralMap()
        {
            if (_currentMap != null)
            {
                Destroy(_currentMap);
            }

            _currentMap = new GameObject("ProceduralMap");

            CreateBoundaryWalls();
            CreateRandomObstacles();
        }

        private void CreateBoundaryWalls()
        {
            if (wallPrefab == null) return;

            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(_currentMap.transform);

            float halfWidth = arenaSize.x / 2f;
            float halfHeight = arenaSize.y / 2f;

            CreateWallLine(new Vector3(-halfWidth, 0, 0), Vector3.up, arenaSize.y, wallsParent.transform);
            CreateWallLine(new Vector3(halfWidth, 0, 0), Vector3.up, arenaSize.y, wallsParent.transform);
            CreateWallLine(new Vector3(0, -halfHeight, 0), Vector3.right, arenaSize.x, wallsParent.transform);
            CreateWallLine(new Vector3(0, halfHeight, 0), Vector3.right, arenaSize.x, wallsParent.transform);
        }

        private void CreateWallLine(Vector3 start, Vector3 direction, float length, Transform parent)
        {
            int segments = Mathf.CeilToInt(length);

            for (int i = 0; i < segments; i++)
            {
                Vector3 position = start + direction * i;
                GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);
            }
        }

        private void CreateRandomObstacles()
        {
            if (obstaclePrefab == null) return;

            GameObject obstaclesParent = new GameObject("Obstacles");
            obstaclesParent.transform.SetParent(_currentMap.transform);

            float halfWidth = arenaSize.x / 2f - 2f;
            float halfHeight = arenaSize.y / 2f - 2f;

            for (int i = 0; i < obstacleCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-halfWidth, halfWidth),
                    Random.Range(-halfHeight, halfHeight),
                    0
                );

                if (Vector3.Distance(position, Vector3.zero) > 3f)
                {
                    Instantiate(obstaclePrefab, position, Quaternion.identity, obstaclesParent.transform);
                }
            }
        }

        public Vector3 GetRandomSpawnPosition()
        {
            float halfWidth = arenaSize.x / 2f - 2f;
            float halfHeight = arenaSize.y / 2f - 2f;

            return new Vector3(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(-halfHeight, halfHeight),
                0
            );
        }

        public bool IsPositionValid(Vector3 position, float radius = 0.5f)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);
            return colliders.Length == 0;
        }
    }
}
