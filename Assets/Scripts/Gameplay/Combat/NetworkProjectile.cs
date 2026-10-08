using System;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkProjectile : NetworkBehaviour, IPooledObject
    {
        public struct FlightState : INetworkSerializable, IEquatable<FlightState>
        {
            public Vector3 Origin;
            public Vector2 Velocity;
            public double Started;
            public float Lifetime;
            public ulong Shooter;
            public uint Sequence;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Origin);
                serializer.SerializeValue(ref Velocity);
                serializer.SerializeValue(ref Started);
                serializer.SerializeValue(ref Lifetime);
                serializer.SerializeValue(ref Shooter);
                serializer.SerializeValue(ref Sequence);
            }
            public bool Equals(FlightState other) => Origin.Equals(other.Origin) && Velocity.Equals(other.Velocity)
                && Started.Equals(other.Started) && Lifetime.Equals(other.Lifetime)
                && Shooter == other.Shooter && Sequence == other.Sequence;
        }

        // One initial trajectory replaces a stream of NetworkTransform position updates.
        private readonly NetworkVariable<FlightState> flight = new();
        private FlightState pendingFlight;
        private Rigidbody2D body;
        private SpriteRenderer sprite;
        private int damage;
        private bool hit;
        private bool ownerVisual;
        private Vector3 ownerVisualPosition;
        private float ownerVisualAge;
        private float ownerVisualReceivedAt;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();
        }

        public void Initialize(Vector2 direction, float speed, int damageAmount, float lifetime,
            ulong shooter, uint sequence, double started)
        {
            damage = damageAmount;
            pendingFlight = new FlightState
            {
                Origin = transform.position, Velocity = direction.normalized * speed,
                Started = started, Lifetime = lifetime, Shooter = shooter, Sequence = sequence
            };
        }

        public override void OnNetworkSpawn()
        {
            hit = false;
            ownerVisual = false;
            body.simulated = IsServer;
            if (IsServer)
            {
                flight.Value = pendingFlight;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.linearVelocity = pendingFlight.Velocity;
            }
            else
            {
                var state = flight.Value;
                Vector3 position = PositionAt(state, NetworkManager.ServerTime.Time);
                if (state.Shooter == NetworkManager.LocalClientId &&
                    NetworkManager.LocalClient.PlayerObject != null &&
                    NetworkManager.LocalClient.PlayerObject.TryGetComponent<NetworkPlayerController>(out var player) &&
                    player.ConsumePredictedShot(state.Sequence, out var predictedPosition, out var predictedAge))
                {
                    // Keep the owner's visual timeline. Easing back to an older server position
                    // makes fast bullets visibly fly backwards at high RTT.
                    ownerVisual = true;
                    ownerVisualPosition = predictedPosition;
                    ownerVisualAge = predictedAge;
                    ownerVisualReceivedAt = Time.time;
                    position = predictedPosition;
                }
                transform.position = position;
            }
            if (sprite != null) sprite.enabled = true;
        }

        public static Vector3 PositionAt(FlightState state, double time)
            => state.Origin + (Vector3)state.Velocity * Mathf.Clamp((float)(time - state.Started), 0, state.Lifetime);

        private void Update()
        {
            if (!IsSpawned) return;
            var state = flight.Value;
            if (IsServer)
            {
                if (NetworkManager.ServerTime.Time - state.Started >= state.Lifetime)
                    NetworkObjectPool.Instance.Despawn(NetworkObject);
                return;
            }
            if (ownerVisual)
            {
                float elapsed = Time.time - ownerVisualReceivedAt;
                transform.position = ownerVisualPosition + (Vector3)state.Velocity *
                    Mathf.Min(elapsed, Mathf.Max(0, state.Lifetime - ownerVisualAge));
                if (sprite != null) sprite.enabled = ownerVisualAge + elapsed < state.Lifetime;
            }
            else
            {
                transform.position = PositionAt(state, NetworkManager.ServerTime.Time);
                if (sprite != null) sprite.enabled = NetworkManager.ServerTime.Time - state.Started < state.Lifetime;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned || hit) return;
            if (other.TryGetComponent<NetworkEnemy>(out var enemy))
            {
                hit = true;
                enemy.ReceiveDamage(damage, flight.Value.Shooter);
                // EnemyVisualFeedback already produces the impact effect; do not spawn it twice.
                NetworkObjectPool.Instance.Despawn(NetworkObject);
            }
            else if (other.TryGetComponent<NetworkHealth>(out var health) &&
                     !other.TryGetComponent<NetworkPlayerController>(out _))
            {
                hit = true;
                health.ApplyDamage(damage);
                NetworkObjectPool.Instance.Despawn(NetworkObject);
            }
        }

        public override void OnNetworkDespawn()
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        public void OnSpawned()
        {
            hit = false;
            ownerVisual = false;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        public void OnDespawned()
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            hit = true;
        }
    }
}
