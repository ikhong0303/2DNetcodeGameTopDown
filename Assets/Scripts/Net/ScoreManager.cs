using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public class ScoreManager : NetworkBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Settings")]
        [SerializeField] private int enemyKillScore = 10;
        [SerializeField] private int bossKillScore = 100;
        [SerializeField] private int waveCompleteBonus = 50;

        public NetworkVariable<int> TotalScore { get; private set; }
        public NetworkVariable<int> TotalKills { get; private set; }
        public NetworkVariable<int> CurrentWave { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            TotalScore = new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            TotalKills = new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            CurrentWave = new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            TotalScore.OnValueChanged += OnScoreChanged;
            TotalKills.OnValueChanged += OnKillsChanged;
            CurrentWave.OnValueChanged += OnWaveChanged;
        }

        public override void OnNetworkDespawn()
        {
            TotalScore.OnValueChanged -= OnScoreChanged;
            TotalKills.OnValueChanged -= OnKillsChanged;
            CurrentWave.OnValueChanged -= OnWaveChanged;
        }

        private void OnScoreChanged(int previousScore, int newScore)
        {
            Debug.Log($"Score: {previousScore} -> {newScore}");
        }

        private void OnKillsChanged(int previousKills, int newKills)
        {
            Debug.Log($"Kills: {previousKills} -> {newKills}");
        }

        private void OnWaveChanged(int previousWave, int newWave)
        {
            Debug.Log($"Wave: {previousWave} -> {newWave}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddEnemyKillServerRpc()
        {
            if (!IsServer) return;

            TotalKills.Value++;
            TotalScore.Value += enemyKillScore;
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddBossKillServerRpc()
        {
            if (!IsServer) return;

            TotalKills.Value++;
            TotalScore.Value += bossKillScore;
        }

        public void AddWaveComplete(int waveNumber)
        {
            if (!IsServer) return;

            CurrentWave.Value = waveNumber;
            int bonus = waveCompleteBonus * waveNumber;
            TotalScore.Value += bonus;

            Debug.Log($"Wave {waveNumber} complete! Bonus: {bonus}");
        }

        public void ResetScore()
        {
            if (!IsServer) return;

            TotalScore.Value = 0;
            TotalKills.Value = 0;
            CurrentWave.Value = 0;
        }

        public int GetEnemyKillScore()
        {
            return enemyKillScore;
        }

        public int GetBossKillScore()
        {
            return bossKillScore;
        }
    }
}
