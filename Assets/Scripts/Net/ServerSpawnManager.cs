using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public class ServerSpawnManager : NetworkBehaviour
    {
        [SerializeField] private NetworkObject enemyPrefab;
        [SerializeField] private Transform[] enemySpawnPoints;

        [Header("Wave System (Optional)")]
        [SerializeField] private bool useWaveSystem = false;
        [SerializeField] private int initialEnemyCount = 3;
        [SerializeField] private float waveDelay = 5f;

        private int _currentWave = 0;

        public static ServerSpawnManager Instance { get; private set; }

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
            if (!IsServer)
            {
                Debug.Log("[ServerSpawnManager] Not server, skipping enemy spawn");
                return;
            }

            Debug.Log($"[ServerSpawnManager] OnNetworkSpawn - IsServer: {IsServer}, useWaveSystem: {useWaveSystem}, initialEnemyCount: {initialEnemyCount}");

            if (useWaveSystem)
            {
                StartWave();
            }
            else
            {
                SpawnEnemies(initialEnemyCount);
            }
        }

        public void SpawnEnemies(int count)
        {
            Debug.Log($"[ServerSpawnManager] SpawnEnemies called with count: {count}");

            if (enemyPrefab == null)
            {
                Debug.LogWarning("[ServerSpawnManager] Enemy prefab is not assigned!");
                return;
            }

            Transform[] spawnPoints = enemySpawnPoints;

            if (SpawnPointManager.Instance != null)
            {
                Debug.Log("[ServerSpawnManager] Using SpawnPointManager for spawn points");
                spawnPoints = SpawnPointManager.Instance.GetEnemySpawnPoints();
            }
            else
            {
                Debug.Log("[ServerSpawnManager] Using local enemySpawnPoints array");
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[ServerSpawnManager] No enemy spawn points available!");
                return;
            }

            Debug.Log($"[ServerSpawnManager] Found {spawnPoints.Length} spawn points");

            for (int i = 0; i < count; i++)
            {
                Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (sp == null)
                {
                    Debug.LogWarning($"[ServerSpawnManager] Spawn point {i} is null");
                    continue;
                }

                Debug.Log($"[ServerSpawnManager] Spawning enemy {i + 1}/{count} at position {sp.position}");
                NetworkObject obj = Instantiate(enemyPrefab, sp.position, Quaternion.identity);
                obj.Spawn(true);
            }

            Debug.Log($"[ServerSpawnManager] Successfully spawned {count} enemies");
        }

        private void StartWave()
        {
            if (_currentWave > 0 && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddWaveComplete(_currentWave);
            }

            _currentWave++;
            int enemyCount = initialEnemyCount + (_currentWave - 1);
            Debug.Log($"Starting wave {_currentWave} with {enemyCount} enemies");
            SpawnEnemies(enemyCount);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.CurrentWave.Value = _currentWave;
            }
        }

        private void Update()
        {
            if (!IsServer || !useWaveSystem)
            {
                return;
            }

            int aliveEnemies = FindObjectsOfType<NetworkEnemyChaser>().Length;
            if (aliveEnemies == 0)
            {
                Invoke(nameof(StartWave), waveDelay);
            }
        }
    }
}
