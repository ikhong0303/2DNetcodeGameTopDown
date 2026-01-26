using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkHealth))]
    public class NetworkBossEnemy : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float chargeSpeed = 8f;
        [SerializeField] private float chargeCooldown = 5f;

        [Header("Attack Patterns")]
        [SerializeField] private NetworkObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int projectileDamage = 2;
        [SerializeField] private float fireInterval = 1f;
        [SerializeField] private int burstCount = 5;
        [SerializeField] private float burstInterval = 0.2f;

        [Header("Contact Damage")]
        [SerializeField] private int contactDamage = 2;
        [SerializeField] private float contactDamageInterval = 0.5f;

        private Rigidbody2D _rb;
        private float _nextFireTime;
        private float _nextChargeTime;
        private float _nextContactDamageTime;
        private bool _isCharging;
        private Vector2 _chargeDirection;
        private float _chargeEndTime;

        private enum BossState
        {
            Idle,
            Chase,
            Charge,
            Shooting
        }

        private BossState _currentState = BossState.Chase;

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

            switch (_currentState)
            {
                case BossState.Chase:
                    ChasePlayer(target);
                    break;

                case BossState.Charge:
                    PerformCharge();
                    break;

                case BossState.Shooting:
                    ShootAtPlayer(target);
                    break;
            }

            UpdateState(target);
        }

        private void UpdateState(Transform target)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, target.position);

            if (_isCharging)
            {
                if (Time.time >= _chargeEndTime)
                {
                    _isCharging = false;
                    _currentState = BossState.Chase;
                }
                return;
            }

            if (Time.time >= _nextChargeTime && distanceToPlayer < 8f)
            {
                StartCharge(target);
            }
            else if (Time.time >= _nextFireTime && distanceToPlayer > 4f)
            {
                _currentState = BossState.Shooting;
            }
            else
            {
                _currentState = BossState.Chase;
            }
        }

        private void ChasePlayer(Transform target)
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
            }

            _rb.linearVelocity = dir * moveSpeed;
        }

        private void StartCharge(Transform target)
        {
            _chargeDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
            _isCharging = true;
            _chargeEndTime = Time.time + 1f;
            _nextChargeTime = Time.time + chargeCooldown;
            _currentState = BossState.Charge;
        }

        private void PerformCharge()
        {
            _rb.linearVelocity = _chargeDirection * chargeSpeed;
        }

        private void ShootAtPlayer(Transform target)
        {
            if (projectilePrefab == null)
            {
                _currentState = BossState.Chase;
                return;
            }

            _rb.linearVelocity = Vector2.zero;

            StartCoroutine(ShootBurstCoroutine(target));
            _nextFireTime = Time.time + fireInterval + (burstCount * burstInterval);
            _currentState = BossState.Chase;
        }

        private System.Collections.IEnumerator ShootBurstCoroutine(Transform target)
        {
            for (int i = 0; i < burstCount; i++)
            {
                if (target != null)
                {
                    Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
                    float angleOffset = (i - burstCount / 2) * 15f;
                    Vector2 rotatedDir = Quaternion.Euler(0, 0, angleOffset) * direction;

                    ShootProjectile(rotatedDir);
                }

                yield return new WaitForSeconds(burstInterval);
            }
        }

        private void ShootProjectile(Vector2 direction)
        {
            Vector2 spawnPos = (Vector2)transform.position + direction * 0.5f;

            NetworkObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projObj.GetComponent<NetworkProjectile2D>();

            if (proj != null)
            {
                proj.SetData(direction, projectileSpeed, projectileDamage, NetworkObject.OwnerClientId);
            }

            projObj.Spawn(true);
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
