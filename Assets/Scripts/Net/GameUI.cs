using Unity.Netcode;
using UnityEngine;
using TMPro;

namespace IsaacLike.Net
{
    public class GameUI : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text gameStateText;

        [Header("Player Info")]
        [SerializeField] private TMP_Text player1HpText;
        [SerializeField] private TMP_Text player2HpText;

        [Header("Powerup Display")]
        [SerializeField] private TMP_Text powerupsText;

        private ScoreManager _scoreManager;
        private GameStateManager _gameStateManager;
        private NetworkPlayerController2D _localPlayer;

        private void Start()
        {
            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_scoreManager == null)
            {
                _scoreManager = ScoreManager.Instance;
            }

            if (_gameStateManager == null)
            {
                _gameStateManager = GameStateManager.Instance;
            }

            if (_localPlayer == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (playerObj != null)
                {
                    _localPlayer = playerObj.GetComponent<NetworkPlayerController2D>();
                }
            }

            UpdateScoreUI();
            UpdateGameStateUI();
            UpdatePlayerHealthUI();
            UpdatePowerupsUI();
        }

        private void UpdateScoreUI()
        {
            if (_scoreManager != null)
            {
                if (scoreText != null)
                {
                    scoreText.text = $"Score: {_scoreManager.TotalScore.Value}";
                }

                if (killsText != null)
                {
                    killsText.text = $"Kills: {_scoreManager.TotalKills.Value}";
                }

                if (waveText != null)
                {
                    waveText.text = $"Wave: {_scoreManager.CurrentWave.Value}";
                }
            }
        }

        private void UpdateGameStateUI()
        {
            if (_gameStateManager != null && gameStateText != null)
            {
                string stateStr = _gameStateManager.CurrentState.Value switch
                {
                    GameState.Menu => "Menu",
                    GameState.Playing => "Playing",
                    GameState.Paused => "PAUSED (ESC to resume)",
                    GameState.GameOver => "GAME OVER",
                    _ => "Unknown"
                };

                gameStateText.text = stateStr;
                gameStateText.gameObject.SetActive(_gameStateManager.CurrentState.Value == GameState.Paused ||
                                                    _gameStateManager.CurrentState.Value == GameState.GameOver);
            }
        }

        private void UpdatePlayerHealthUI()
        {
            if (NetworkManager.Singleton == null) return;

            int playerIndex = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client?.PlayerObject == null) continue;

                var health = client.PlayerObject.GetComponent<NetworkHealth>();
                if (health == null) continue;

                string hpText = $"P{playerIndex + 1}: {health.CurrentHp.Value}/{health.GetMaxHp()}";

                if (playerIndex == 0 && player1HpText != null)
                {
                    player1HpText.text = hpText;
                }
                else if (playerIndex == 1 && player2HpText != null)
                {
                    player2HpText.text = hpText;
                }

                playerIndex++;
            }

            if (playerIndex < 1 && player1HpText != null)
            {
                player1HpText.text = "P1: --";
            }

            if (playerIndex < 2 && player2HpText != null)
            {
                player2HpText.text = "P2: --";
            }
        }

        private void UpdatePowerupsUI()
        {
            if (_localPlayer == null || powerupsText == null) return;

            var powerups = _localPlayer.GetComponent<PlayerPowerups>();
            if (powerups == null)
            {
                powerupsText.text = "";
                return;
            }

            string text = "";

            if (powerups.SpeedMultiplier.Value > 1f)
            {
                text += $"Speed: x{powerups.SpeedMultiplier.Value:F1}\n";
            }

            if (powerups.DamageMultiplier.Value > 1f)
            {
                text += $"Damage: x{powerups.DamageMultiplier.Value:F1}\n";
            }

            if (powerups.FireRateMultiplier.Value > 1f)
            {
                text += $"Fire Rate: x{powerups.FireRateMultiplier.Value:F1}\n";
            }

            powerupsText.text = text;
        }
    }
}
