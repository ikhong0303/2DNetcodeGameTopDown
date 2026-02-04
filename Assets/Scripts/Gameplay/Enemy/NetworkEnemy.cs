/// =============================================================================
/// NetworkEnemy.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크에서 동기화되는 적(Enemy) 캐릭터의 AI와 상태를 관리합니다.
/// - 서버에서 가장 가까운 플레이어를 추적하여 이동합니다.
/// - 플레이어와 충돌 시 접촉 데미지를 주며, 공격 쿨다운이 적용됩니다.
/// - 데미지를 받으면 히트 이펙트와 빨간색 플래시 효과가 모든 클라이언트에 표시됩니다.
/// - 체력이 0이 되면 사망 처리되고, 처치한 플레이어에게 점수가 부여됩니다.
/// =============================================================================

using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 동기화 적 캐릭터 컴포넌트
    /// Rigidbody2D 필수 (물리 이동용)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkEnemy : NetworkBehaviour
    {
        // ===== 프리팩에서 직접 설정할 값들 =====
        
        [Header("Stats")]
        [SerializeField] private int maxHealth = 5;            // 최대 체력
        [SerializeField] private int contactDamage = 1;        // 접촉 데미지
        [SerializeField] private float attackCooldown = 1f;    // 공격 쿨다운
        [SerializeField] private int scoreValue = 10;          // 처치 점수

        // ===== 분리된 컴포넌트 (SRP) =====
        
        [Header("Components")]
        [SerializeField] private EnemyAI enemyAI;                        // AI 이동 컴포넌트
        [SerializeField] private EnemyVisualFeedback visualFeedback;     // 시각 효과 컴포넌트

        // ===== 컴포넌트 캐시 =====
        
        private NetworkHealth health;          // 체력 컴포넌트
        private Collider2D enemyCollider;      // 충돌 컴포넌트 (초기화 전 비활성화용)
        
        // ===== 상태 변수 =====
        
        private float nextAttackTime;          // 다음 공격 가능 시간
        private ulong lastAttackerId;          // 마지막으로 공격한 플레이어 ID (점수 부여용)
        private bool isReady = false;          // 초기화 완료 여부 (true일 때만 이동/충돌 가능)
        
        /// <summary>초기화 완료 여부 (외부에서 스폰 완료 확인용)</summary>
        public bool IsReady => isReady;
        
        /// <summary>최대 체력 (외부에서 HP바 계산용)</summary>
        public int MaxHealth => maxHealth;

        /// <summary>
        /// Awake: 컴포넌트 참조 캐시
        /// </summary>
        private void Awake()
        {
            health = GetComponent<NetworkHealth>();
            enemyCollider = GetComponent<Collider2D>();
            
            // 분리된 컴포넌트 참조
            if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
            if (visualFeedback == null) visualFeedback = GetComponent<EnemyVisualFeedback>();
            
            // 중요: 시작 시 콜라이더 비활성화 (초기화 완료 전 충돌 방지)
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }
        }

        /// <summary>
        /// 네트워크 스폰 시 호출
        /// 서버에서 체력 초기화, 그 후 콜라이더 활성화
        /// </summary>
        public override void OnNetworkSpawn()
        {
            // 컴포넌트 참조 확인 (Awake가 호출되지 않았을 수 있음)
            if (health == null)
            {
                health = GetComponent<NetworkHealth>();
            }
            if (enemyCollider == null)
            {
                enemyCollider = GetComponent<Collider2D>();
            }

            // 서버에서만 초기 체력 설정 (프리팹에 설정된 값 + 난이도 보너스)
            if (IsServer)
            {
                if (health != null)
                {
                    // 난이도에 따른 HP 보너스 (난이도 1 = 기본, 난이도 2 = +1, 난이도 3 = +2, ...)
                    int difficultyBonus = 0;
                    if (TopDownShooter.Networking.NetworkGameManager.Instance != null)
                    {
                        difficultyBonus = TopDownShooter.Networking.NetworkGameManager.Instance.DifficultyLevel - 1;
                    }
                    
                    int totalHealth = maxHealth + difficultyBonus;
                    health.CurrentHealth.Value = totalHealth;
                    health.IsDowned.Value = false;
                }
                else
                {
                }
            }
            
            // 중요: 초기화 완료 후 한 프레임 대기했다가 콜라이더 활성화
            StartCoroutine(EnableColliderNextFrame());
        }
        
        /// <summary>
        /// 한 프레임 대기 후 콜라이더 활성화
        /// 모든 초기화가 완료된 후에 충돌 허용
        /// </summary>
        private System.Collections.IEnumerator EnableColliderNextFrame()
        {
            // 한 프레임 대기 (모든 네트워크 동기화 완료 보장)
            yield return null;
            
            if (enemyCollider != null)
            {
                enemyCollider.enabled = true;
            }
            
            // 초기화 완료 - 이제 이동 및 충돌 가능
            isReady = true;
            
            // AI 활성화
            enemyAI?.Activate();
            
            // 스포너에게 "다음 적 소환해도 됨" 알림 (이벤트 기반)
            if (IsServer)
            {
                NetworkGameManager.Instance?.OnEnemyReady();
            }
        }


        /// <summary>
        /// 2D 충돌 유지 시 호출 (접촉 데미지)
        /// 서버에서만 처리
        /// </summary>
        /// <param name="collision">충돌 정보</param>
        private void OnCollisionStay2D(Collision2D collision)
        {
            // 서버가 아니면 무시
            if (!IsServer)
            {
                return;
            }

            // 쿨다운 체크: 아직 공격 가능 시간이 안 됐으면 무시
            if (Time.time < nextAttackTime)
            {
                return;
            }

            // 플레이어인지 확인 (적끼리 공격 방지)
            if (!collision.collider.TryGetComponent<NetworkPlayerController>(out _))
            {
                return;
            }

            // 충돌 대상이 NetworkHealth를 가지고 있으면 데미지
            if (collision.collider.TryGetComponent<NetworkHealth>(out var targetHealth))
            {
                targetHealth.ApplyDamage(contactDamage);
                nextAttackTime = Time.time + attackCooldown;  // 다음 공격 시간 설정
            }
        }

        /// <summary>
        /// 데미지를 받습니다.
        /// 투사체 등에서 호출됩니다. 서버에서만 실행.
        /// </summary>
        /// <param name="amount">데미지 양</param>
        /// <param name="attackerId">공격자의 클라이언트 ID</param>
        public void ReceiveDamage(int amount, ulong attackerId)
        {
            // 서버가 아니면 무시
            if (!IsServer)
            {
                return;
            }
            
            // 초기화가 안 됐거나 이미 죽는 중이면 무시
            if (!isReady)
            {
                return;
            }

            // 폴백: 체력 컴포넌트가 캐시되지 않았으면 다시 가져오기
            if (health == null)
            {
                health = GetComponent<NetworkHealth>();
                if (health == null)
                {
                    return;
                }
            }

            // 공격자 ID 저장 (사망 시 점수 부여용)
            lastAttackerId = attackerId;
            
            // 버그로 HP가 이미 0 이하인데 살아있는 경우 → 바로 죽음 처리
            if (health.CurrentHealth.Value <= 0)
            {
                HandleDeath();
                return;
            }
            
            int previousHealth = health.CurrentHealth.Value;
            
            // 데미지 적용 (0 이하로 내려가지 않도록)
            int newHealth = Mathf.Max(0, previousHealth - amount);
            health.CurrentHealth.Value = newHealth;

            // 시각 피드백 (히트 이펙트 + 플래시)
            visualFeedback?.TriggerHitFeedback();

            // 체력이 0 이하면 사망 처리
            if (newHealth <= 0)
            {
                HandleDeath();
            }
        }

        /// <summary>
        /// 사망 처리
        /// 점수 부여, 게임 매니저에 알림, 디스폰
        /// </summary>
        private void HandleDeath()
        {
            // 서버에서만 처리
            if (!IsServer)
            {
                return;
            }
            
            // 중복 호출 방지
            if (!isReady)
            {
                return;
            }
            
            // 더 이상 활동하지 않도록 플래그 설정
            isReady = false;
            
            // 콜라이더 즉시 비활성화 (추가 충돌 방지)
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }
            
            // AI 비활성화
            enemyAI?.Deactivate();
            
            // 공격자에게 점수 부여 (모든 플레이어에서 찾기)
            var players = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerClientId == lastAttackerId)
                {
                    player.AddScore(scoreValue);
                    break;
                }
            }

            // 게임 매니저에 사망 알림 (생존 적 목록에서 제거)
            NetworkGameManager.Instance?.RegisterEnemyDeath(this);
            
            // 네트워크에서 디스폰 (파괴)
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }


    }
}
