/// =============================================================================
/// GameEventChannelSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 매개변수 없는 게임 이벤트를 방송하는 ScriptableObject 기반 이벤트 채널입니다.
/// - 옵저버 패턴을 구현하여 게임 오브젝트 간 느슨한 결합(decoupling)을 지원합니다.
/// - 웨이브 완료, 게임 오버, 부활 완료 등의 이벤트를 전달할 때 사용됩니다.
/// - Raise() 메서드를 호출하면 구독 중인 모든 리스너에게 이벤트가 전달됩니다.
/// =============================================================================

using System;
using UnityEngine;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 매개변수 없는 이벤트 채널 ScriptableObject
    /// 옵저버 패턴 구현으로 발신자와 수신자 간의 직접 참조 없이 통신 가능
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Events/Game Event Channel로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Events/Game Event Channel")]
    public class GameEventChannelSO : ScriptableObject
    {
        /// <summary>
        /// 이벤트 델리게이트
        /// 외부에서 += 로 구독, -= 로 구독 해제
        /// </summary>
        public event Action EventRaised;

        /// <summary>
        /// 이벤트를 발생시킵니다.
        /// 이 메서드를 호출하면 구독 중인 모든 리스너의 콜백이 실행됩니다.
        /// </summary>
        public void Raise()
        {
            // null 조건부 연산자: 구독자가 있을 때만 이벤트 호출
            // ?. 연산자는 EventRaised가 null이면 아무것도 하지 않음
            EventRaised?.Invoke();
        }
    }
}
