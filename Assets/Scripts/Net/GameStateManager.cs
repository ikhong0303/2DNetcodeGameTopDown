using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IsaacLike.Net
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    public class GameStateManager : NetworkBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float gameOverDelay = 2f;

        public NetworkVariable<GameState> CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentState = new NetworkVariable<GameState>(
                GameState.Menu,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            CurrentState.OnValueChanged += OnStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            CurrentState.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState previousState, GameState newState)
        {
            Debug.Log($"Game state changed: {previousState} -> {newState}");

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    Time.timeScale = 0.5f;
                    break;

                case GameState.Menu:
                    Time.timeScale = 1f;
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartGameServerRpc()
        {
            if (!IsServer) return;
            CurrentState.Value = GameState.Playing;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PauseGameServerRpc()
        {
            if (!IsServer) return;
            if (CurrentState.Value == GameState.Playing)
            {
                CurrentState.Value = GameState.Paused;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResumeGameServerRpc()
        {
            if (!IsServer) return;
            if (CurrentState.Value == GameState.Paused)
            {
                CurrentState.Value = GameState.Playing;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void GameOverServerRpc()
        {
            if (!IsServer) return;
            CurrentState.Value = GameState.GameOver;
        }

        public void TogglePause()
        {
            if (CurrentState.Value == GameState.Playing)
            {
                PauseGameServerRpc();
            }
            else if (CurrentState.Value == GameState.Paused)
            {
                ResumeGameServerRpc();
            }
        }

        public void CheckGameOver()
        {
            if (!IsServer) return;
            if (CurrentState.Value != GameState.Playing) return;

            int alivePlayers = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client?.PlayerObject != null)
                {
                    var health = client.PlayerObject.GetComponent<NetworkHealth>();
                    if (health != null && health.CurrentHp.Value > 0)
                    {
                        alivePlayers++;
                    }
                }
            }

            if (alivePlayers == 0)
            {
                StartCoroutine(TriggerGameOverAfterDelay());
            }
        }

        private System.Collections.IEnumerator TriggerGameOverAfterDelay()
        {
            yield return new WaitForSeconds(gameOverDelay);
            GameOverServerRpc();
        }

        public void RestartGame()
        {
            if (!IsServer) return;

            Time.timeScale = 1f;
            CurrentState.Value = GameState.Playing;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client?.PlayerObject != null)
                {
                    var health = client.PlayerObject.GetComponent<NetworkHealth>();
                    if (health != null)
                    {
                        health.CurrentHp.Value = health.GetMaxHp();
                        client.PlayerObject.transform.position = Vector3.zero;
                    }
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
    }
}
