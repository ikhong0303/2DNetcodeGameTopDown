/// =============================================================================
/// EnemyAI.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 적 AI 이동 로직을 담당하는 컴포넌트입니다.
/// - NetworkEnemy에서 분리된 단일 책임 클래스입니다.
/// - 가장 가까운 플레이어를 추적하여 이동합니다.
/// - SRP (단일 책임 원칙) 준수
/// =============================================================================

using Unity.Netcode;
using UnityEngine;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 적 AI 컴포넌트
    /// 플레이어 추적 및 이동을 담당
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : NetworkBehaviour
    {
        // ===== 설정 =====
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;

        // ===== 컴포넌트 캐시 =====
        
        private Rigidbody2D body;

        // ===== 상태 =====
        
        private bool isActive = false;

        // ===== 프로퍼티 =====
        
        /// <summary>이동 속도 설정</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        // ===== 라이프사이클 =====

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        // ===== 공개 메서드 =====

        /// <summary>
        /// AI 활성화 (NetworkEnemy.OnEnemyReady에서 호출)
        /// </summary>
        public void Activate()
        {
            isActive = true;
        }

        /// <summary>
        /// AI 비활성화 (사망 시 호출)
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        // ===== 물리 업데이트 =====

        private void FixedUpdate()
        {
            // 비활성화 상태거나 서버가 아니면 무시
            if (!isActive || !IsServer) return;

            // 가장 가까운 플레이어 찾기
            Transform target = FindClosestPlayer();
            
            // 타겟이 없으면 정지
            if (target == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            // 타겟 방향으로 이동
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            body.linearVelocity = direction * moveSpeed;
        }

        // ===== 내부 메서드 =====

        /// <summary>
        /// 가장 가까운 살아있는 플레이어를 찾습니다.
        /// </summary>
        private Transform FindClosestPlayer()
        {
            float closestDistance = float.MaxValue;
            Transform closestTransform = null;

            // 모든 플레이어 순회
            var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                // null이거나 다운된 플레이어는 제외
                if (player == null) continue;
                
                if (player.TryGetComponent<NetworkHealth>(out var playerHealth) && playerHealth.IsDowned.Value)
                {
                    continue;
                }

                // 거리 계산
                float distance = Vector2.Distance(transform.position, player.transform.position);
                
                // 더 가까우면 갱신
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTransform = player.transform;
                }
            }

            return closestTransform;
        }
    }
}
