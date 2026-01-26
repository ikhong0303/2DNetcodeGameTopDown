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

                // Check if NetworkManager exists
                if (NetworkManager.Singleton == null)
                {
                    throw new InvalidOperationException(
                        "NetworkManager.Singleton is null. " +
                        "Make sure there is a NetworkManager in the scene and it is active.");
                }

                var options = new SessionOptions
                {
                    MaxPlayers = 2,
                    IsPrivate = false,
                    Name = $"Isaac2D_{sessionId}"
                }.WithRelayNetwork();

                _activeSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);

                // Note: WithRelayNetwork() automatically manages the Netcode connection.
                // The SDK will start Host or Client automatically based on the session role.
                Debug.Log($"Session created/joined. IsHost: {_activeSession.IsHost}, Code: {_activeSession.Code}");
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

                // Check if NetworkManager exists
                if (NetworkManager.Singleton == null)
                {
                    throw new InvalidOperationException(
                        "NetworkManager.Singleton is null. " +
                        "Make sure there is a NetworkManager in the scene and it is active.");
                }

                // Use CreateOrJoinSessionAsync with relay network options.
                // This will join the existing session if it exists with the same sessionId,
                // or create a new one if it doesn't exist.
                // JoinSessionByIdAsync doesn't work because the lobby ID is auto-generated,
                // not the sessionId we provide.
                var options = new SessionOptions
                {
                    MaxPlayers = 2,
                    IsPrivate = false,
                    Name = $"Isaac2D_{sessionIdOrJoinCode}"
                }.WithRelayNetwork();

                _activeSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionIdOrJoinCode, options);

                // Note: The SDK automatically starts the client connection when joining a relay session.
                Debug.Log($"Joined session. IsHost: {_activeSession.IsHost}, Id: {_activeSession.Id}, Code: {_activeSession.Code}");
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
