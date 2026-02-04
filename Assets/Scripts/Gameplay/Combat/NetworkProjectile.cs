/// =============================================================================
/// NetworkProjectile.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크에서 동기화되는 투사체(총알, 미사일 등)를 관리하는 컴포넌트입니다.
/// - 서버에서 물리 시뮬레이션을 처리하고 모든 클라이언트에 위치를 동기화합니다.
/// - IPooledObject 인터페이스를 구현하여 오브젝트 풀링을 지원합니다.
/// - 적이나 체력을 가진 오브젝트와 충돌 시 데미지를 주고 히트 이펙트를 생성합니다.
/// - 지정된 수명(lifetime) 후 자동으로 풀에 반환됩니다.
/// =============================================================================

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 동기화 투사체 컴포넌트
    /// Rigidbody2D 필수 (물리 이동용)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkProjectile : NetworkBehaviour, IPooledObject
    {
        // ===== 컴포넌트 캐시 =====
        private Rigidbody2D body;          // 물리 컴포넌트

        // ===== 투사체 속성 =====
        private int damage;                // 데미지
        private float lifetime;            // 수명 (초)
        private Coroutine lifeRoutine;     // 수명 코루틴 참조
        private ulong ownerId;             // 발사한 플레이어의 클라이언트 ID

        /// <summary>
        /// Awake: 컴포넌트 초기화
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// 네트워크 스폰 시 호출
        /// 서버에서만 물리 시뮬레이션 활성화
        /// </summary>
        public override void OnNetworkSpawn()
        {
            // 서버에서만 물리 시뮬레이션 실행
            if (IsServer)
            {
                body.simulated = true;
            }
        }

        /// <summary>
        /// 투사체를 초기화합니다.
        /// 스폰 후 호출하여 방향, 속도, 데미지 등을 설정합니다.
        /// </summary>
        /// <param name="direction">발사 방향</param>
        /// <param name="speed">이동 속도</param>
        /// <param name="damageAmount">데미지</param>
        /// <param name="lifeTimeSeconds">수명 (초)</param>
        /// <param name="ownerClientId">발사한 플레이어의 클라이언트 ID</param>
        public void Initialize(Vector2 direction, float speed, int damageAmount, float lifeTimeSeconds, ulong ownerClientId)
        {
            damage = damageAmount;
            lifetime = lifeTimeSeconds;
            ownerId = ownerClientId;
            
            // 정규화된 방향 × 속도로 속도 설정
            body.linearVelocity = direction.normalized * speed;

            // 기존 수명 코루틴이 있으면 중지
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
            }

            // 수명 후 자동 디스폰 코루틴 시작
            lifeRoutine = StartCoroutine(DespawnAfterLifetime());
        }

        /// <summary>
        /// 수명 후 자동 디스폰 코루틴
        /// </summary>
        private IEnumerator DespawnAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);
            
            // 스폰된 상태인지 확인 후 디스폰
            if (NetworkObject != null && NetworkObject.IsSpawned) 
            {
                NetworkObjectPool.Instance.Despawn(NetworkObject);
            }
        }

        /// <summary>
        /// 2D 트리거 충돌 시 호출
        /// 서버에서만 처리하여 데미지 계산
        /// </summary>
        /// <param name="other">충돌한 콜라이더</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 서버가 아니면 무시 (클라이언트에서 데미지 처리 X)
            if (!IsServer)
            {
                return;
            }

            // 적과 충돌한 경우
            if (other.TryGetComponent<NetworkEnemy>(out var enemy))
            {
                enemy.ReceiveDamage(damage, ownerId);   // 적에게 데미지
                SpawnHitEffect(other.transform.position); // 히트 이펙트 생성
                
                // 투사체 디스폰
                if (NetworkObject != null && NetworkObject.IsSpawned) 
                {
                    NetworkObjectPool.Instance.Despawn(NetworkObject);
                }
            }
            // NetworkHealth를 가진 오브젝트와 충돌 (단, 플레이어는 제외)
            else if (other.TryGetComponent<NetworkHealth>(out var health) && !other.TryGetComponent<NetworkPlayerController>(out _))
            {
                health.ApplyDamage(damage);               // 데미지 적용
                SpawnHitEffect(other.transform.position); // 히트 이펙트 생성
                
                // 투사체 디스폰
                if (NetworkObject != null && NetworkObject.IsSpawned) 
                {
                    NetworkObjectPool.Instance.Despawn(NetworkObject);
                }
            }
        }

        /// <summary>
        /// 히트 이펙트를 생성합니다.
        /// </summary>
        /// <param name="position">이펙트 생성 위치</param>
        private void SpawnHitEffect(Vector3 position)
        {
            // 게임 매니저에서 히트 이펙트 설정 가져오기
            var config = NetworkGameManager.Instance?.HitEffectConfig;
            
            // 설정이 없거나 프리팹이 없으면 리턴
            if (config == null || config.EffectPrefab == null)
            {
                return;
            }

            // 오브젝트 풀에서 이펙트 스폰
            var effectObject = NetworkObjectPool.Instance.Spawn(config.EffectPrefab.GetComponent<NetworkObject>(), position, Quaternion.identity);
            
            // 이펙트 재생
            if (effectObject != null && effectObject.TryGetComponent<NetworkEffect>(out var effect))
            {
                effect.Play(config.Lifetime);
            }
        }

        /// <summary>
        /// IPooledObject 인터페이스 구현
        /// 풀에서 스폰될 때 호출 - 속도 초기화
        /// </summary>
        public void OnSpawned()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// IPooledObject 인터페이스 구현
        /// 풀로 반환될 때 호출 - 속도 초기화
        /// </summary>
        public void OnDespawned()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
