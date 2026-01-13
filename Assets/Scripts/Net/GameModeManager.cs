using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public enum GameMode
    {
        Normal,
        Survival,
        TimeAttack,
        BossRush
    }

    public class GameModeManager : NetworkBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [Header("Game Mode")]
        [SerializeField] private GameMode currentGameMode = GameMode.Normal;

        [Header("Time Attack Settings")]
        [SerializeField] private float timeAttackDuration = 180f;

        [Header("Survival Settings")]
        [SerializeField] private float survivalSpawnInterval = 5f;
        [SerializeField] private float survivalDifficultyIncrease = 1.1f;

        [Header("Boss Rush Settings")]
        [SerializeField] private NetworkObject[] bossPreabs;
        [SerializeField] private float bossRushInterval = 10f;

        public NetworkVariable<GameMode> CurrentMode { get; private set; }
        public NetworkVariable<float> TimeRemaining { get; private set; }
        public NetworkVariable<int> SurvivalWave { get; private set; }
        public NetworkVariable<int> BossesDefeated { get; private set; }

        private float _nextSpawnTime;
        private int _currentBossIndex;
        private float _currentDifficulty = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            CurrentMode = new NetworkVariable<GameMode>(
                currentGameMode,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            TimeRemaining = new NetworkVariable<float>(
                timeAttackDuration,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            SurvivalWave = new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            BossesDefeated = new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            StartGameMode();
        }

        private void StartGameMode()
        {
            switch (CurrentMode.Value)
            {
                case GameMode.Normal:
                    StartNormalMode();
                    break;

                case GameMode.Survival:
                    StartSurvivalMode();
                    break;

                case GameMode.TimeAttack:
                    StartTimeAttackMode();
                    break;

                case GameMode.BossRush:
                    StartBossRushMode();
                    break;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            switch (CurrentMode.Value)
            {
                case GameMode.Survival:
                    UpdateSurvivalMode();
                    break;

                case GameMode.TimeAttack:
                    UpdateTimeAttackMode();
                    break;

                case GameMode.BossRush:
                    UpdateBossRushMode();
                    break;
            }
        }

        private void StartNormalMode()
        {
            Debug.Log("Normal mode started");
        }

        private void StartSurvivalMode()
        {
            Debug.Log("Survival mode started");
            SurvivalWave.Value = 1;
            _nextSpawnTime = Time.time + survivalSpawnInterval;
        }

        private void UpdateSurvivalMode()
        {
            if (Time.time >= _nextSpawnTime)
            {
                SpawnSurvivalWave();
                _nextSpawnTime = Time.time + survivalSpawnInterval;
            }
        }

        private void SpawnSurvivalWave()
        {
            SurvivalWave.Value++;
            _currentDifficulty *= survivalDifficultyIncrease;

            int enemyCount = Mathf.RoundToInt(3 * _currentDifficulty);

            if (ServerSpawnManager.Instance != null)
            {
                Debug.Log($"Spawning survival wave {SurvivalWave.Value} with {enemyCount} enemies");
            }
        }

        private void StartTimeAttackMode()
        {
            Debug.Log("Time Attack mode started");
            TimeRemaining.Value = timeAttackDuration;
        }

        private void UpdateTimeAttackMode()
        {
            TimeRemaining.Value -= Time.deltaTime;

            if (TimeRemaining.Value <= 0)
            {
                TimeRemaining.Value = 0;
                OnTimeAttackEnd();
            }
        }

        private void OnTimeAttackEnd()
        {
            Debug.Log("Time Attack ended!");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.GameOverServerRpc();
            }
        }

        private void StartBossRushMode()
        {
            Debug.Log("Boss Rush mode started");
            BossesDefeated.Value = 0;
            _currentBossIndex = 0;
            SpawnNextBoss();
        }

        private void UpdateBossRushMode()
        {
            int aliveBosses = FindObjectsOfType<NetworkBossEnemy>().Length;

            if (aliveBosses == 0 && _currentBossIndex < bossPreabs.Length)
            {
                Invoke(nameof(SpawnNextBoss), bossRushInterval);
            }
        }

        private void SpawnNextBoss()
        {
            if (_currentBossIndex >= bossPreabs.Length)
            {
                OnBossRushComplete();
                return;
            }

            NetworkObject bossPrefab = bossPreabs[_currentBossIndex];
            if (bossPrefab != null)
            {
                Vector3 spawnPos = Vector3.zero;
                if (SpawnPointManager.Instance != null)
                {
                    spawnPos = SpawnPointManager.Instance.GetEnemySpawnPosition();
                }

                NetworkObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
                boss.Spawn(true);

                Debug.Log($"Spawned boss {_currentBossIndex + 1}");
            }

            _currentBossIndex++;
            BossesDefeated.Value = _currentBossIndex;
        }

        private void OnBossRushComplete()
        {
            Debug.Log("Boss Rush completed!");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.GameOverServerRpc();
            }
        }

        public void SetGameMode(GameMode mode)
        {
            if (!IsServer) return;

            CurrentMode.Value = mode;
            StartGameMode();
        }
    }
}
