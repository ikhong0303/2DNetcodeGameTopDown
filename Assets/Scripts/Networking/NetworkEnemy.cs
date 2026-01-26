using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;

namespace TopDownShooter.Networking
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkEnemy : NetworkBehaviour
    {
        [SerializeField] private EnemyConfigSO config;

        private Rigidbody2D body;
        private NetworkHealth health;
        private float nextAttackTime;
        private ulong lastAttackerId;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<NetworkHealth>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && config != null && health != null)
            {
                health.CurrentHealth.Value = config.MaxHealth;
                health.IsDowned.Value = false;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer || config == null)
            {
                return;
            }

            var target = FindClosestPlayer();
            if (target == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = (target.position - transform.position).normalized;
            body.linearVelocity = direction * config.MoveSpeed;
        }

        private Transform FindClosestPlayer()
        {
            float closest = float.MaxValue;
            Transform closestTransform = null;

            foreach (var player in FindObjectsOfType<NetworkPlayerController>())
            {
                if (player == null || player.TryGetComponent<NetworkHealth>(out var playerHealth) && playerHealth.IsDowned.Value)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closest)
                {
                    closest = distance;
                    closestTransform = player.transform;
                }
            }

            return closestTransform;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!IsServer || config == null)
            {
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            if (collision.collider.TryGetComponent<NetworkHealth>(out var targetHealth))
            {
                targetHealth.ApplyDamage(config.ContactDamage);
                nextAttackTime = Time.time + config.AttackCooldown;
            }
        }

        public void ReceiveDamage(int amount, ulong attackerId)
        {
            if (!IsServer || health == null || health.IsDowned.Value)
            {
                return;
            }

            lastAttackerId = attackerId;
            health.ApplyDamage(amount);

            if (health.CurrentHealth.Value <= 0)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            if (IsServer)
            {
                if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(lastAttackerId, out var attackerObject) &&
                    attackerObject.TryGetComponent<NetworkPlayerController>(out var player))
                {
                    player.AddScore(config.ScoreValue);
                }

                NetworkGameManager.Instance?.RegisterEnemyDeath(this);
                NetworkObject.Despawn(true);
            }
        }
    }
}
