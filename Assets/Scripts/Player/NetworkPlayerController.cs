using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;
using TopDownShooter.Managers;

namespace TopDownShooter.Networking
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private InputActionReference interactAction;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float stepRate = 0.35f;

        [Header("Combat")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.2f;
        [SerializeField] private ProjectileConfigSO projectileConfig;

        [Header("Revive")]
        [SerializeField] private float reviveSearchRadius = 2.5f;

        [Header("Camera")]
        [SerializeField] private CinemachineCamera playerCamera;

        [Header("Events")]
        [SerializeField] private GameEventChannelSO playerDownedEvent;

        private readonly NetworkVariable<int> score = new(0);
        private Rigidbody2D body;
        private NetworkHealth health;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private float lastFireTime;
        private float lastStepTime;
        private NetworkHealth reviveTarget;
        private bool inputEnabled = false;

        public NetworkVariable<int> Score => score;

        public override void OnNetworkSpawn()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<NetworkHealth>();

            Debug.Log($"[OnNetworkSpawn] ClientId: {OwnerClientId}, IsOwner: {IsOwner}, IsLocalPlayer: {IsLocalPlayer}");

            ConfigureCamera();

            if (IsOwner)
            {
                EnableInput(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                EnableInput(false);
            }
        }

        private void OnEnable()
        {
            // Only enable input if we're already spawned and own this object
            if (IsSpawned && IsOwner && !inputEnabled)
            {
                EnableInput(true);
            }
        }

        private void OnDisable()
        {
            if (inputEnabled)
            {
                EnableInput(false);
            }
        }

        private void EnableInput(bool enable)
        {
            if (enable == inputEnabled) return; // Avoid duplicate subscription
            inputEnabled = enable;

            Debug.Log($"[EnableInput] ClientId: {OwnerClientId}, IsOwner: {IsOwner}, Enable: {enable}");

            if (moveAction != null && moveAction.action != null)
            {
                if (enable)
                {
                    moveAction.action.Enable();
                    moveAction.action.performed += OnMove;
                    moveAction.action.canceled += OnMove;
                    Debug.Log("[EnableInput] Subscribed to Move");
                }
                else
                {
                    moveAction.action.performed -= OnMove;
                    moveAction.action.canceled -= OnMove;
                    // Don't disable the action - other players might be using it
                }
            }
            else
            {
                Debug.LogError($"[EnableInput] MoveAction is null! ClientId: {OwnerClientId}");
            }

            if (lookAction != null && lookAction.action != null)
            {
                if (enable)
                {
                    lookAction.action.Enable();
                    lookAction.action.performed += OnLook;
                    lookAction.action.canceled += OnLook;
                }
                else
                {
                    lookAction.action.performed -= OnLook;
                    lookAction.action.canceled -= OnLook;
                }
            }

            if (attackAction != null && attackAction.action != null)
            {
                if (enable)
                {
                    attackAction.action.Enable();
                    attackAction.action.performed += OnAttack;
                    Debug.Log("[EnableInput] Subscribed to Attack");
                }
                else
                {
                    attackAction.action.performed -= OnAttack;
                }
            }
            else
            {
                Debug.LogError($"[EnableInput] AttackAction is null! ClientId: {OwnerClientId}");
            }

            if (interactAction != null && interactAction.action != null)
            {
                if (enable)
                {
                    interactAction.action.Enable();
                    interactAction.action.started += OnInteractStarted;
                    interactAction.action.canceled += OnInteractCanceled;
                }
                else
                {
                    interactAction.action.started -= OnInteractStarted;
                    interactAction.action.canceled -= OnInteractCanceled;
                }
            }
        }

        private void ConfigureCamera()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<CinemachineCamera>();
            }

            if (playerCamera != null)
            {
                if (IsOwner)
                {
                    playerCamera.Follow = transform;
                    playerCamera.LookAt = transform;
                    playerCamera.gameObject.SetActive(true);
                }
                else
                {
                    playerCamera.gameObject.SetActive(false);
                }
            }
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            if (!IsOwner || !context.performed)
            {
                return;
            }

            if (health != null && health.IsDowned.Value)
            {
                return;
            }

            if (Time.time - lastFireTime < fireRate)
            {
                return;
            }

            // Mouse Aiming Implementation with null check
            Vector2 aimDirection = Vector2.right;
            Camera mainCam = Camera.main;

            if (mainCam != null && Mouse.current != null)
            {
                Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
                mouseScreenPos.z = mainCam.nearClipPlane;
                Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
                Vector2 flatMousePos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
                Vector2 flatPlayerPos = new Vector2(transform.position.x, transform.position.y);
                aimDirection = (flatMousePos - flatPlayerPos).normalized;

                if (aimDirection.sqrMagnitude < 0.01f)
                {
                    aimDirection = Vector2.right;
                }
            }

            lastFireTime = Time.time;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx("Shoot");
            }

            FireServerRpc(aimDirection);
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (!IsOwner || health == null || health.IsDowned.Value)
            {
                return;
            }

            reviveTarget = FindClosestDownedPlayer();
            if (reviveTarget != null)
            {
                StartReviveServerRpc(reviveTarget.NetworkObjectId);
            }
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (!IsOwner)
            {
                return;
            }

            if (reviveTarget != null)
            {
                StopReviveServerRpc(reviveTarget.NetworkObjectId);
                reviveTarget = null;
            }
        }

        private NetworkHealth FindClosestDownedPlayer()
        {
            float closestDistance = reviveSearchRadius;
            NetworkHealth closest = null;

            var players = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);

            foreach (var player in players)
            {
                if (player == this)
                {
                    continue;
                }

                if (player.health == null || !player.health.IsDowned.Value)
                {
                    continue;
                }

                float distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closest = player.health;
                }
            }

            return closest;
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            if (health == null || health.IsDowned.Value)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            body.linearVelocity = moveInput * moveSpeed;

            // Play walk sound
            if (moveInput.sqrMagnitude > 0.01f && Time.time - lastStepTime > stepRate)
            {
                lastStepTime = Time.time;
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySfx("Walk");
                }
            }
        }

        [ServerRpc]
        private void FireServerRpc(Vector2 direction, ServerRpcParams rpcParams = default)
        {
            if (projectileConfig == null || firePoint == null)
            {
                return;
            }

            var projectilePrefab = projectileConfig.ProjectilePrefab;
            if (projectilePrefab == null)
            {
                Debug.LogError("[FireServerRpc] ProjectilePrefab is null in Config!");
                return;
            }

            // Use GetComponent because .NetworkObject property is null on prefabs
            var prefabNetworkObject = projectilePrefab.GetComponent<NetworkObject>();
            if (prefabNetworkObject == null)
            {
                Debug.LogError($"[FireServerRpc] ProjectilePrefab '{projectilePrefab.name}' is missing a NetworkObject component!");
                return;
            }

            var projectileObject = NetworkObjectPool.Instance.Spawn(prefabNetworkObject, firePoint.position, Quaternion.identity);
            
            if (projectileObject == null)
            {
                Debug.LogError("[FireServerRpc] Spawn returned null! Pool failed to spawn.");
                return;
            }

            var projectile = projectileObject.GetComponent<NetworkProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(direction, projectileConfig.Speed, projectileConfig.Damage, projectileConfig.Lifetime, OwnerClientId);
            }
            else
            {
                Debug.LogError("[FireServerRpc] Spawned object is missing NetworkProjectile component!");
            }
        }

        [ServerRpc]
        private void StartReviveServerRpc(ulong targetId)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObject))
            {
                return;
            }

            if (targetObject.TryGetComponent<NetworkHealth>(out var targetHealth))
            {
                targetHealth.BeginRevive(NetworkObject);
            }
        }

        [ServerRpc]
        private void StopReviveServerRpc(ulong targetId)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObject))
            {
                return;
            }

            if (targetObject.TryGetComponent<NetworkHealth>(out var targetHealth))
            {
                targetHealth.CancelRevive();
            }
        }

        public void AddScore(int amount)
        {
            if (!IsServer)
            {
                return;
            }

            score.Value += amount;
        }

        public void NotifyDowned()
        {
            playerDownedEvent?.Raise();
        }

        public void ResetPosition()
        {
            if (IsServer)
            {
                // Reset velocity
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }
                
                // Teleport to network spawn point or zero
                // Note: With ClientNetworkTransform (Owner authority), server might fight with client.
                // But for restart, forcing position from server usually works if client logic respects it or after a delay.
                // Ideally, we should use NetworkTransform.Teleport if available, but direct set works for basic sync.
                transform.position = Vector3.zero; 
                
                // If using ClientNetworkTransform, we might need a ClientRpc to force client to reset its position
                ResetPositionClientRpc(Vector3.zero);
            }
        }

        [ClientRpc]
        private void ResetPositionClientRpc(Vector3 position)
        {
            if (IsOwner)
            {
                transform.position = position;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }
            }
        }
    }
}
