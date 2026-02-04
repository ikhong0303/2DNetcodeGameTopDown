/// =============================================================================
/// StringEventChannelSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 문자열(string) 값을 매개변수로 전달하는 이벤트 채널 ScriptableObject입니다.
/// - UI 상태 메시지, 알림 텍스트 등을 전달할 때 사용됩니다.
/// - Raise(string value) 메서드로 이벤트와 함께 문자열 값을 방송합니다.
/// =============================================================================

using System;
using UnityEngine;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 문자열 값을 전달하는 이벤트 채널 ScriptableObject
    /// Action<string> 델리게이트를 사용하여 문자열 매개변수 전달
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Events/String Event Channel로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Events/String Event Channel")]
    public class StringEventChannelSO : ScriptableObject
    {
        /// <summary>
        /// 문자열 매개변수를 받는 이벤트 델리게이트
        /// Action<string>: string 매개변수를 받고 반환값이 없는 델리게이트
        /// </summary>
        public event Action<string> EventRaised;

        /// <summary>
        /// 문자열 값과 함께 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="value">전달할 문자열 값 (예: 상태 메시지, 알림 등)</param>
        public void Raise(string value)
        {
            // 구독자가 있을 때만 이벤트 호출하고 value 전달
            EventRaised?.Invoke(value);
        }
    }
}
