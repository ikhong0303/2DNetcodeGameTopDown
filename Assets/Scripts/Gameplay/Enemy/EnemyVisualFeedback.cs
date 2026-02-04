/// =============================================================================
/// EnemyVisualFeedback.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 적의 시각적 피드백(히트 이펙트, 플래시 효과)을 담당하는 컴포넌트입니다.
/// - NetworkEnemy에서 분리된 단일 책임 클래스입니다.
/// - 데미지 시 빨간색 플래시, 히트 파티클 효과 처리
/// - HP바는 NetworkHealth에서 통합 관리됨
/// - SRP (단일 책임 원칙) 준수
/// =============================================================================

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 적 시각 효과 컴포넌트
    /// 히트 피드백 및 이펙트 스폰을 담당 (HP바는 NetworkHealth에서 관리)
    /// </summary>
    public class EnemyVisualFeedback : NetworkBehaviour
    {
        // ===== 컴포넌트 캐시 =====
        
        private SpriteRenderer spriteRenderer;

        // ===== 설정 =====
        
        [Header("Flash Settings")]
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        // ===== 라이프사이클 =====

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void OnNetworkSpawn()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }



        // ===== 공개 메서드 =====

        /// <summary>
        /// 히트 피드백 트리거 (서버에서 호출)
        /// 모든 클라이언트에 플래시 효과 전송
        /// </summary>
        public void TriggerHitFeedback()
        {
            if (!IsServer) return;
            
            // 서버에서 히트 이펙트 스폰
            SpawnHitEffect(transform.position);
            
            // 모든 클라이언트에 플래시 효과 전송
            TriggerFlashClientRpc();
        }

        // ===== ClientRpc =====

        [ClientRpc]
        private void TriggerFlashClientRpc()
        {
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashCoroutine());
            }
        }

        // ===== 코루틴 =====

        private IEnumerator FlashCoroutine()
        {
            var originalColor = spriteRenderer.color;
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }

        // ===== 이펙트 스폰 =====

        private void SpawnHitEffect(Vector3 position)
        {
            if (!IsServer) return;

            // 게임 매니저에서 히트 이펙트 설정 가져오기
            var hitConfig = NetworkGameManager.Instance?.HitEffectConfig;
            if (hitConfig == null || hitConfig.EffectPrefab == null)
            {
                return;
            }

            // 프리팹에서 NetworkObject 컴포넌트 가져오기
            var prefabNetworkObject = hitConfig.EffectPrefab.GetComponent<NetworkObject>();
            if (prefabNetworkObject == null)
            {
                return;
            }

            // 오브젝트 풀에서 이펙트 스폰
            var effectObject = NetworkObjectPool.Instance.Spawn(prefabNetworkObject, position, Quaternion.identity);
            if (effectObject != null && effectObject.TryGetComponent<NetworkEffect>(out var effect))
            {
                effect.Play(hitConfig.Lifetime);
            }
        }
    }
}
