using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkProjectile : NetworkBehaviour, IPooledObject
    {
        private Rigidbody2D body;
        private int damage;
        private float lifetime;
        private Coroutine lifeRoutine;
        private ulong ownerId;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                body.simulated = true;
            }
        }

        public void Initialize(Vector2 direction, float speed, int damageAmount, float lifeTimeSeconds, ulong ownerClientId)
        {
            damage = damageAmount;
            lifetime = lifeTimeSeconds;
            ownerId = ownerClientId;
            body.linearVelocity = direction.normalized * speed;

            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
            }

            lifeRoutine = StartCoroutine(DespawnAfterLifetime());
        }

        private IEnumerator DespawnAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);
            NetworkObjectPool.Instance.Despawn(NetworkObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer)
            {
                return;
            }

            if (other.TryGetComponent<NetworkEnemy>(out var enemy))
            {
                enemy.ReceiveDamage(damage, ownerId);
                SpawnHitEffect(other.transform.position);
                NetworkObjectPool.Instance.Despawn(NetworkObject);
            }
            else if (other.TryGetComponent<NetworkHealth>(out var health) && !other.TryGetComponent<NetworkPlayerController>(out _))
            {
                health.ApplyDamage(damage);
                SpawnHitEffect(other.transform.position);
                NetworkObjectPool.Instance.Despawn(NetworkObject);
            }
        }

        private void SpawnHitEffect(Vector3 position)
        {
            var config = NetworkGameManager.Instance?.HitEffectConfig;
            if (config == null || config.EffectPrefab == null)
            {
                return;
            }

            var effectObject = NetworkObjectPool.Instance.Spawn(config.EffectPrefab.NetworkObject, position, Quaternion.identity);
            if (effectObject.TryGetComponent<NetworkEffect>(out var effect))
            {
                effect.Play(config.Lifetime);
            }
        }

        public void OnSpawned()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void OnDespawned()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
