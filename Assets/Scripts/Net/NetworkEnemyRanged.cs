using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkHealth))]
    public class NetworkEnemyRanged : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float keepDistance = 5f;
        [SerializeField] private float tooCloseDistance = 3f;

        [Header("Shooting")]
        [SerializeField] private NetworkObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private int projectileDamage = 1;
        [SerializeField] private float fireInterval = 2f;
        [SerializeField] private float shootRange = 10f;

        private Rigidbody2D _rb;
        private float _nextFireTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
        }

        private void FixedUpdate()
        {
            if (!IsServer)
            {
                return;
            }

            Transform target = FindNearestPlayer();
            if (target == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 dirToPlayer = (Vector2)target.position - (Vector2)transform.position;
            float distanceToPlayer = dirToPlayer.magnitude;

            if (distanceToPlayer < tooCloseDistance)
            {
                dirToPlayer = -dirToPlayer.normalized;
                _rb.linearVelocity = dirToPlayer * moveSpeed;
            }
            else if (distanceToPlayer > keepDistance)
            {
                dirToPlayer.Normalize();
                _rb.linearVelocity = dirToPlayer * moveSpeed;
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }

            if (distanceToPlayer <= shootRange && Time.time >= _nextFireTime)
            {
                ShootAtPlayer(target);
                _nextFireTime = Time.time + fireInterval;
            }
        }

        private Transform FindNearestPlayer()
        {
            float best = float.MaxValue;
            Transform bestTr = null;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client?.PlayerObject == null)
                {
                    continue;
                }

                float d = (client.PlayerObject.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestTr = client.PlayerObject.transform;
                }
            }

            return bestTr;
        }

        private void ShootAtPlayer(Transform target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("Projectile prefab is not assigned!");
                return;
            }

            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Vector2 spawnPos = (Vector2)transform.position + direction * 0.5f;

            NetworkObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projObj.GetComponent<NetworkProjectile2D>();

            if (proj != null)
            {
                proj.SetData(direction, projectileSpeed, projectileDamage, NetworkObject.OwnerClientId);
            }

            projObj.Spawn(true);
        }
    }
}
