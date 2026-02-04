/// =============================================================================
/// IntEventChannelSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 정수(int) 값을 매개변수로 전달하는 이벤트 채널 ScriptableObject입니다.
/// - GameEventChannelSO와 유사하지만, 이벤트 발생 시 정수 값을 함께 전달합니다.
/// - 웨이브 번호, 점수, 데미지 수치 등 숫자 정보를 전달할 때 사용됩니다.
/// - Raise(int value) 메서드로 이벤트와 함께 정수 값을 방송합니다.
/// =============================================================================

using System;
using UnityEngine;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 정수 값을 전달하는 이벤트 채널 ScriptableObject
    /// Action<int> 델리게이트를 사용하여 정수 매개변수 전달
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Events/Int Event Channel로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Events/Int Event Channel")]
    public class IntEventChannelSO : ScriptableObject
    {
        /// <summary>
        /// 정수 매개변수를 받는 이벤트 델리게이트
        /// Action<int>: int 매개변수를 받고 반환값이 없는 델리게이트
        /// </summary>
        public event Action<int> EventRaised;

        /// <summary>
        /// 정수 값과 함께 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="value">전달할 정수 값 (예: 웨이브 번호, 점수 등)</param>
        public void Raise(int value)
        {
            // 구독자가 있을 때만 이벤트 호출하고 value 전달
            EventRaised?.Invoke(value);
        }
    }
}
