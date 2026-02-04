/// =============================================================================
/// GameEventListener.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - GameEventChannelSO의 이벤트를 수신하여 UnityEvent로 응답하는 컴포넌트입니다.
/// - 게임 오브젝트에 이 컴포넌트를 부착하고 채널을 연결하면 이벤트를 받을 수 있습니다.
/// - 인스펙터에서 response에 원하는 메서드를 연결하여 이벤트 발생 시 실행할 동작을 지정합니다.
/// - OnEnable/OnDisable에서 자동으로 이벤트 구독/해제를 처리합니다.
/// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 이벤트 채널의 이벤트를 수신하는 리스너 컴포넌트
    /// 게임 오브젝트에 부착하여 사용
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        // ===== 인스펙터에서 설정할 필드들 =====
        
        [SerializeField] private GameEventChannelSO channel;   // 구독할 이벤트 채널
        [SerializeField] private UnityEvent response;          // 이벤트 수신 시 실행할 UnityEvent

        /// <summary>
        /// 컴포넌트가 활성화될 때 호출
        /// 이벤트 채널에 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            // 채널이 설정되어 있을 때만 구독
            if (channel != null)
            {
                // += 연산자로 이벤트 구독 (델리게이트 추가)
                channel.EventRaised += OnEventRaised;
            }
        }

        /// <summary>
        /// 컴포넌트가 비활성화될 때 호출
        /// 이벤트 채널 구독을 해제합니다.
        /// 메모리 누수 방지를 위해 반드시 필요!
        /// </summary>
        private void OnDisable()
        {
            // 채널이 설정되어 있을 때만 구독 해제
            if (channel != null)
            {
                // -= 연산자로 이벤트 구독 해제 (델리게이트 제거)
                channel.EventRaised -= OnEventRaised;
            }
        }

        /// <summary>
        /// 이벤트가 발생했을 때 호출되는 콜백 메서드
        /// 인스펙터에서 설정한 response UnityEvent를 실행합니다.
        /// </summary>
        private void OnEventRaised()
        {
            // ?. 연산자: response가 null이 아닐 때만 Invoke 호출
            response?.Invoke();
        }
    }
}
