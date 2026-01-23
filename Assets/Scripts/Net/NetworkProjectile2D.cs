using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkProjectile2D : NetworkBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private float lifeTime = 2.5f;

        private Rigidbody2D _rb;
        private float _dieAt;

        private NetworkVariable<Vector2> _dir = new NetworkVariable<Vector2>(
            Vector2.right,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> _speed = new NetworkVariable<float>(
            10f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _damage = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<ulong> _ownerId = new NetworkVariable<ulong>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            // Make projectile kinematic so it doesn't push other physics objects
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void SetData(Vector2 dir, float speed, int damage, ulong ownerClientId)
        {
            if (!IsServer)
            {
                return;
            }

            _dir.Value = dir;
            _speed.Value = speed;
            _damage.Value = damage;
            _ownerId.Value = ownerClientId;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _dieAt = Time.time + lifeTime;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer)
            {
                return;
            }

            _rb.linearVelocity = _dir.Value * _speed.Value;

            if (Time.time >= _dieAt)
            {
                NetworkObject.Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer)
            {
                return;
            }

            var targetNetObj = other.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.OwnerClientId == _ownerId.Value)
            {
                return;
            }

            var health = other.GetComponentInParent<NetworkHealth>();
            if (health != null)
            {
                health.ApplyDamage(_damage.Value);
                NetworkObject.Despawn();
            }
        }
    }
}
