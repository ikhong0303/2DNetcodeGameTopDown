using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;

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
        private NetworkHealth reviveTarget;

        public NetworkVariable<int> Score => score;

        public override void OnNetworkSpawn()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<NetworkHealth>();

            if (IsOwner)
            {
                EnableInput(true);
                ConfigureCamera();
            }
            else
            {
                EnableInput(false);
            }
        }

        private void OnEnable()
        {
            if (IsOwner)
            {
                EnableInput(true);
            }
        }

        private void OnDisable()
        {
            EnableInput(false);
        }

        private void EnableInput(bool enable)
        {
            if (moveAction != null)
            {
                if (enable)
                {
                    moveAction.action.Enable();
                    moveAction.action.performed += OnMove;
                    moveAction.action.canceled += OnMove;
                }
                else
                {
                    moveAction.action.performed -= OnMove;
                    moveAction.action.canceled -= OnMove;
                    moveAction.action.Disable();
                }
            }

            if (lookAction != null)
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
                    lookAction.action.Disable();
                }
            }

            if (attackAction != null)
            {
                if (enable)
                {
                    attackAction.action.Enable();
                    attackAction.action.performed += OnAttack;
                }
                else
                {
                    attackAction.action.performed -= OnAttack;
                    attackAction.action.Disable();
                }
            }

            if (interactAction != null)
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
                    interactAction.action.Disable();
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
                playerCamera.Follow = transform;
                playerCamera.LookAt = transform;
                playerCamera.gameObject.SetActive(true);
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
            if (!IsOwner || !context.performed || health == null || health.IsDowned.Value)
            {
                return;
            }

            if (Time.time - lastFireTime < fireRate)
            {
                return;
            }

            Vector2 aimDirection = lookInput.sqrMagnitude > 0.1f ? lookInput.normalized : moveInput.normalized;
            if (aimDirection.sqrMagnitude < 0.1f)
            {
                aimDirection = Vector2.right;
            }

            lastFireTime = Time.time;
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
            if (!IsOwner || health == null || health.IsDowned.Value)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            body.linearVelocity = moveInput * moveSpeed;
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
                return;
            }

            var projectileObject = NetworkObjectPool.Instance.Spawn(projectilePrefab.NetworkObject, firePoint.position, Quaternion.identity);
            var projectile = projectileObject.GetComponent<NetworkProjectile>();
            projectile.Initialize(direction, projectileConfig.Speed, projectileConfig.Damage, projectileConfig.Lifetime, OwnerClientId);
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
    }
}
