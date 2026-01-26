using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkHealth))]
    public class NetworkEnemyChaser : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 2.8f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactDamageInterval = 0.5f;

        private Rigidbody2D _rb;
        private float _nextContactDamageTime;

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

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
            }

            _rb.linearVelocity = dir * moveSpeed;
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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsServer)
            {
                return;
            }

            var health = collision.collider.GetComponentInParent<NetworkHealth>();
            if (health != null)
            {
                health.ApplyDamage(contactDamage);
                _nextContactDamageTime = Time.time + contactDamageInterval;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!IsServer)
            {
                return;
            }

            if (Time.time < _nextContactDamageTime)
            {
                return;
            }

            var health = collision.collider.GetComponentInParent<NetworkHealth>();
            if (health != null)
            {
                health.ApplyDamage(contactDamage);
                _nextContactDamageTime = Time.time + contactDamageInterval;
            }
        }
    }
}
