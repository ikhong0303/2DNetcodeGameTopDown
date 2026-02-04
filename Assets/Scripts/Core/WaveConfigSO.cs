/// =============================================================================
/// WaveConfigSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 적 웨이브 시스템의 설정을 저장하는 ScriptableObject입니다.
/// - 웨이브 간 대기 시간과 각 웨이브별 적 수, 스폰 간격을 정의합니다.
/// - WaveDefinition 구조체로 개별 웨이브의 세부 설정을 관리합니다.
/// - NetworkGameManager에서 이 설정을 참조하여 웨이브를 진행합니다.
/// =============================================================================

using System;
using UnityEngine;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 웨이브 시스템 설정 ScriptableObject
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Config/Wave Config로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Config/Wave Config")]
    public class WaveConfigSO : ScriptableObject
    {
        // ===== 인스펙터에서 설정할 필드들 =====
        
        [SerializeField] private float timeBetweenWaves = 3f;  // 웨이브 간 대기 시간 (초)
        [SerializeField] private WaveDefinition[] waves;        // 웨이브 정의 배열

        // ===== 읽기 전용 프로퍼티 =====
        
        /// <summary>웨이브 간 대기 시간 반환</summary>
        public float TimeBetweenWaves => timeBetweenWaves;
        
        /// <summary>웨이브 정의 배열 반환</summary>
        public WaveDefinition[] Waves => waves;
    }

    /// <summary>
    /// 개별 웨이브의 설정을 정의하는 구조체
    /// [Serializable] 속성으로 인스펙터에서 편집 가능
    /// </summary>
    [Serializable]
    public struct WaveDefinition
    {
        /// <summary>이 웨이브에서 스폰할 적의 수 (최소 1)</summary>
        [Min(1)] public int enemyCount;
        
        /// <summary>적 스폰 간격 (초, 최소 0.1)</summary>
        [Min(0.1f)] public float spawnInterval;
    }
}
