using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    public class NetworkGameManager : NetworkBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private GameConfigSO gameConfig;
        [SerializeField] private Transform[] enemySpawnPoints;

        [Header("Events")]
        [SerializeField] private IntEventChannelSO waveStartedEvent;
        [SerializeField] private GameEventChannelSO waveCompletedEvent;
        [SerializeField] private GameEventChannelSO gameOverEvent;

        private readonly List<NetworkEnemy> aliveEnemies = new();
        private int currentWaveIndex;
        private bool isGameOver;
        private bool isGameStarted;

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
            if (IsServer)
            {
                PrewarmPools();
                StartCoroutine(StartGameRoutine());
            }
        }
// ... (skipping lines)

        private IEnumerator StartGameRoutine()
        {
            Debug.Log("Waiting for 2 players...");
            while (NetworkManager.Singleton.ConnectedClients.Count < 2)
            {
                yield return new WaitForSeconds(1f);
                Debug.Log($"Waiting for players... ({NetworkManager.Singleton.ConnectedClients.Count}/2)");
            }

            Debug.Log("2 players connected! Game starting in 10 seconds...");
            for (int i = 10; i > 0; i--)
            {
                Debug.Log($"Game starting in {i}...");
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("Game Started!");
            isGameStarted = true;
            StartCoroutine(WaveLoop());
        }

        private void PrewarmPools()
        {
            if (gameConfig == null)
            {
                return;
            }

            var projectileConfig = gameConfig.ProjectileConfig;
            if (projectileConfig != null && projectileConfig.ProjectilePrefab != null)
            {
                // Use GetComponent because .NetworkObject property is null on prefabs
                NetworkObjectPool.Instance.RegisterPrefab(projectileConfig.ProjectilePrefab.GetComponent<NetworkObject>(), projectileConfig.PoolSize);
            }

            var effectConfig = gameConfig.HitEffectConfig;
            if (effectConfig != null && effectConfig.EffectPrefab != null)
            {
                // Use GetComponent because .NetworkObject property is null on prefabs
                NetworkObjectPool.Instance.RegisterPrefab(effectConfig.EffectPrefab.GetComponent<NetworkObject>(), effectConfig.PoolSize);
            }
        }

        private IEnumerator WaveLoop()
        {
            if (gameConfig == null || gameConfig.WaveConfig == null)
            {
                yield break;
            }

            var waves = gameConfig.WaveConfig.Waves;
            for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
            {
                if (isGameOver)
                {
                    yield break;
                }

                int waveNumber = currentWaveIndex + 1;
                waveStartedEvent?.Raise(waveNumber);
                RaiseWaveStartedClientRpc(waveNumber);
                yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));
                yield return StartCoroutine(WaitForWaveClear());
                waveCompletedEvent?.Raise();
                RaiseWaveCompletedClientRpc();

                yield return new WaitForSeconds(gameConfig.WaveConfig.TimeBetweenWaves);
            }
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            if (gameConfig.EnemyConfig == null || gameConfig.EnemyConfig.EnemyPrefab == null)
            {
                yield break;
            }

            for (int i = 0; i < wave.enemyCount; i++)
            {
                if (isGameOver)
                {
                    yield break;
                }

                SpawnEnemy();
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        private void SpawnEnemy()
        {
            var enemyPrefab = gameConfig.EnemyConfig.EnemyPrefab;
            if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                return;
            }

            Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
            var enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            enemyInstance.NetworkObject.Spawn(true);
            aliveEnemies.Add(enemyInstance);
        }

        private IEnumerator WaitForWaveClear()
        {
            while (aliveEnemies.Count > 0)
            {
                aliveEnemies.RemoveAll(enemy => enemy == null || !enemy.NetworkObject.IsSpawned);
                yield return null;
            }
        }

        public void RegisterEnemyDeath(NetworkEnemy enemy)
        {
            aliveEnemies.Remove(enemy);
        }

        public void CheckGameOver()
        {
            if (!IsServer || isGameOver || !isGameStarted)
            {
                return;
            }

            bool anyAlive = false;
            foreach (var player in FindObjectsOfType<NetworkPlayerController>())
            {
                if (player != null && player.TryGetComponent<NetworkHealth>(out var playerHealth) && !playerHealth.IsDowned.Value)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                isGameOver = true;
                gameOverEvent?.Raise();
                RaiseGameOverClientRpc();
                
                // Auto restart after 5 seconds
                StartCoroutine(RestartGameRoutine());
            }
        }

        private IEnumerator RestartGameRoutine()
        {
            Debug.Log("Game Over! Restarting in 10 seconds...");
            for (int i = 10; i > 0; i--)
            {
                Debug.Log($"Restarting in {i}...");
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("Restarting Game...");
            
            // 1. Despawn all enemies
            foreach (var enemy in aliveEnemies)
            {
                if (enemy != null && enemy.NetworkObject.IsSpawned)
                {
                    enemy.NetworkObject.Despawn();
                }
            }
            aliveEnemies.Clear();

            // 2. Reset all players
            foreach (var player in FindObjectsOfType<NetworkPlayerController>())
            {
                if (player != null)
                {
                    if (player.TryGetComponent<NetworkHealth>(out var health))
                    {
                        health.ResetState();
                    }
                    player.ResetPosition();
                }
            }

            // 3. Reset game state
            currentWaveIndex = 0;
            isGameOver = false;
            
            // 4. Restart Wave Loop
            StartCoroutine(WaveLoop());
        }

        public EffectConfigSO HitEffectConfig => gameConfig != null ? gameConfig.HitEffectConfig : null;

        [ClientRpc]
        private void RaiseWaveStartedClientRpc(int waveNumber)
        {
            if (IsServer)
            {
                return;
            }

            waveStartedEvent?.Raise(waveNumber);
        }

        [ClientRpc]
        private void RaiseWaveCompletedClientRpc()
        {
            if (IsServer)
            {
                return;
            }

            waveCompletedEvent?.Raise();
        }

        [ClientRpc]
        private void RaiseGameOverClientRpc()
        {
            if (IsServer)
            {
                return;
            }

            gameOverEvent?.Raise();
        }
    }
}
