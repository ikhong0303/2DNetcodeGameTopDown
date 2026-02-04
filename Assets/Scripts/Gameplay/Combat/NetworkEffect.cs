/// =============================================================================
/// NetworkEffect.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크에서 동기화되는 시각 이펙트(파티클 등)를 관리하는 컴포넌트입니다.
/// - IPooledObject 인터페이스를 구현하여 오브젝트 풀링을 지원합니다.
/// - Play() 메서드로 이펙트를 재생하고, 지정된 시간 후 자동으로 풀에 반환됩니다.
/// - 히트 이펙트, 폭발 이펙트 등 일시적인 시각 효과에 사용됩니다.
/// =============================================================================

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 동기화 이펙트 컴포넌트
    /// NetworkBehaviour를 상속하여 네트워크에서 동기화됨
    /// IPooledObject를 구현하여 오브젝트 풀링 지원
    /// </summary>
    public class NetworkEffect : NetworkBehaviour, IPooledObject
    {
        // 현재 실행 중인 수명 코루틴 참조
        private Coroutine lifeRoutine;

        /// <summary>
        /// 이펙트를 재생합니다.
        /// 지정된 시간 후 자동으로 풀에 반환됩니다.
        /// </summary>
        /// <param name="lifetime">이펙트 지속 시간 (초)</param>
        public void Play(float lifetime)
        {
            // 기존 코루틴이 있으면 중지 (중복 실행 방지)
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
            }

            // 지정된 시간 후 디스폰하는 코루틴 시작
            lifeRoutine = StartCoroutine(DespawnAfter(lifetime));
        }

        /// <summary>
        /// 지정된 시간 후 오브젝트를 풀에 반환하는 코루틴
        /// </summary>
        /// <param name="lifetime">대기 시간 (초)</param>
        private IEnumerator DespawnAfter(float lifetime)
        {
            // 지정된 시간만큼 대기
            yield return new WaitForSeconds(lifetime);
            
            // 오브젝트 풀을 통해 디스폰 (네트워크에서 제거 + 풀에 반환)
            NetworkObjectPool.Instance.Despawn(NetworkObject);
        }

        /// <summary>
        /// IPooledObject 인터페이스 구현
        /// 풀에서 스폰될 때 호출됩니다.
        /// 현재는 특별한 초기화 로직 없음
        /// </summary>
        public void OnSpawned()
        {
            // 스폰 시 필요한 초기화 로직을 여기에 추가
        }

        /// <summary>
        /// IPooledObject 인터페이스 구현
        /// 풀로 반환될 때 호출됩니다.
        /// 코루틴을 정리하여 메모리 누수 방지
        /// </summary>
        public void OnDespawned()
        {
            // 실행 중인 코루틴이 있으면 정리
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
                lifeRoutine = null;
            }
        }
    }
}
