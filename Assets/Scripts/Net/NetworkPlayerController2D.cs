using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkHealth))]
    public class NetworkPlayerController2D : NetworkBehaviour
    {
        [Header("Move")]
        [SerializeField] private float moveSpeed = 5.5f;

        [Header("Shoot")]
        [SerializeField] private NetworkObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float fireInterval = 0.18f;
        [SerializeField] private float projectileSpawnOffset = 0.35f;
        [SerializeField] private int projectileDamage = 1;

        private Rigidbody2D _rb;
        private Vector2 _lastSentMove;
        private float _nextFireTime;
        private Vector2 _serverMoveInput;
        private PlayerPowerups _powerups;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
        }

        private void Start()
        {
            _powerups = GetComponent<PlayerPowerups>();
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            Vector2 shootDir = ReadShootDirection();
            if (shootDir.sqrMagnitude > 0.01f && Time.time >= _nextFireTime)
            {
                if (projectilePrefab == null)
                {
                    Debug.LogWarning("Projectile prefab is not assigned!");
                    return;
                }

                float actualFireInterval = fireInterval;
                if (_powerups != null)
                {
                    actualFireInterval /= _powerups.FireRateMultiplier.Value;
                }

                _nextFireTime = Time.time + actualFireInterval;
                RequestShootServerRpc(shootDir.normalized);
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                Vector2 move = ReadMoveInput();

                if (move.sqrMagnitude > 1f)
                {
                    move.Normalize();
                }

                if ((move - _lastSentMove).sqrMagnitude > 0.0001f)
                {
                    _lastSentMove = move;
                    SetMoveInputServerRpc(move);
                }
            }

            if (IsServer)
            {
                float actualMoveSpeed = moveSpeed;
                if (_powerups != null)
                {
                    actualMoveSpeed *= _powerups.SpeedMultiplier.Value;
                }
                _rb.velocity = _serverMoveInput * actualMoveSpeed;
            }
        }

        private Vector2 ReadMoveInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A))
            {
                x -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                x += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                y -= 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                y += 1f;
            }

            return new Vector2(x, y);
        }

        [ServerRpc]
        private void SetMoveInputServerRpc(Vector2 move)
        {
            _serverMoveInput = move;
        }

        [ServerRpc]
        private void RequestShootServerRpc(Vector2 direction, ServerRpcParams rpcParams = default)
        {
            if (projectilePrefab == null)
            {
                return;
            }

            if (!IsServer)
            {
                return;
            }

            Vector2 spawnPos = (Vector2)transform.position + direction.normalized * projectileSpawnOffset;

            NetworkObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projObj.GetComponent<NetworkProjectile2D>();

            if (proj != null)
            {
                int actualDamage = projectileDamage;
                if (_powerups != null)
                {
                    actualDamage = Mathf.RoundToInt(projectileDamage * _powerups.DamageMultiplier.Value);
                }
                proj.SetData(direction.normalized, projectileSpeed, actualDamage, OwnerClientId);
            }

            projObj.Spawn(true);
        }

        private Vector2 ReadShootDirection()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                x -= 1f;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                x += 1f;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                y -= 1f;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                y += 1f;
            }

            Vector2 dir = new Vector2(x, y);
            if (dir.sqrMagnitude > 1f)
            {
                dir.Normalize();
            }

            return dir;
        }
    }
}
