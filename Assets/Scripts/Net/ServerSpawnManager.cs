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

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            if (useWaveSystem)
            {
                StartWave();
            }
            else
            {
                SpawnEnemies(initialEnemyCount);
            }
        }

        private void SpawnEnemies(int count)
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("Enemy prefab is not assigned!");
                return;
            }

            Transform[] spawnPoints = enemySpawnPoints;

            if (SpawnPointManager.Instance != null)
            {
                spawnPoints = SpawnPointManager.Instance.GetEnemySpawnPoints();
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("No enemy spawn points available!");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (sp == null)
                {
                    continue;
                }

                NetworkObject obj = Instantiate(enemyPrefab, sp.position, Quaternion.identity);
                obj.Spawn(true);
            }
        }

        private void StartWave()
        {
            _currentWave++;
            int enemyCount = initialEnemyCount + (_currentWave - 1);
            Debug.Log($"Starting wave {_currentWave} with {enemyCount} enemies");
            SpawnEnemies(enemyCount);
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
