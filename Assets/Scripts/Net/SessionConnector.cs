using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace IsaacLike.Net
{
    public class SessionConnector : MonoBehaviour
    {
        public static SessionConnector Instance { get; private set; }

        public bool IsBusy => _isBusy;

        private bool _isReady;
        private bool _isBusy;
        private ISession _activeSession;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");

            if (NetworkManager.Singleton.IsServer && GameStateManager.Instance != null)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count >= 1)
                {
                    GameStateManager.Instance.StartGameServerRpc();
                }
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");

            if (NetworkManager.Singleton.IsServer && GameStateManager.Instance != null)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count == 0)
                {
                    GameStateManager.Instance.GameOverServerRpc();
                }
            }
        }

        public async Task EnsureReadyAsync()
        {
            if (_isReady)
            {
                return;
            }

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _isReady = true;
        }

        public async Task StartHostAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("sessionId is empty.");
            }

            if (_isBusy)
            {
                return;
            }

            _isBusy = true;

            try
            {
                await EnsureReadyAsync();

                var options = new SessionOptions
                {
                    MaxPlayers = 2,
                    IsPrivate = false,
                    Name = $"Isaac2D_{sessionId}"
                };

                _activeSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);

                if (_activeSession.IsHost)
                {
                    NetworkManager.Singleton.StartHost();
                }
                else
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task JoinAsync(string sessionIdOrJoinCode)
        {
            if (string.IsNullOrWhiteSpace(sessionIdOrJoinCode))
            {
                throw new ArgumentException("sessionIdOrJoinCode is empty.");
            }

            if (_isBusy)
            {
                return;
            }

            _isBusy = true;

            try
            {
                await EnsureReadyAsync();
                _activeSession = await MultiplayerService.Instance.JoinSessionAsync(sessionIdOrJoinCode);
                NetworkManager.Singleton.StartClient();
            }
            finally
            {
                _isBusy = false;
            }
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            _activeSession = null;
        }
    }
}
