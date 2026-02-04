/// =============================================================================
/// GameConfigSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 게임 전체의 설정을 통합 관리하는 ScriptableObject입니다.
/// - 웨이브 설정, 적 설정, 투사체 설정, 히트 이펙트 설정을 하나로 묶어 관리합니다.
/// - NetworkGameManager에서 이 설정을 참조하여 게임 로직을 실행합니다.
/// =============================================================================

using UnityEngine;
using TopDownShooter.Networking;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 게임 전체 설정을 통합 관리하는 ScriptableObject
    /// 모든 게임 설정을 하나의 에셋으로 관리하여 유지보수 용이
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Config/Game Config로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Config/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        // ===== 웨이브 관련 설정 =====
        [Header("Wave Settings")]
        [SerializeField] private WaveConfigSO waveConfig;          // 웨이브 시스템 설정

        // ===== 적 관련 설정 =====
        [Header("Enemy Settings")]
        [SerializeField] private EnemyConfigSO enemyConfig;        // 적 캐릭터 설정

        // ===== 투사체 관련 설정 =====
        [Header("Projectile Settings")]
        [SerializeField] private ProjectileConfigSO projectileConfig;  // 투사체(총알) 설정

        // ===== 이펙트 관련 설정 =====
        [Header("Effect Settings")]
        [SerializeField] private EffectConfigSO hitEffectConfig;   // 히트 이펙트 설정

        // ===== 읽기 전용 프로퍼티 =====
        
        /// <summary>웨이브 설정 반환</summary>
        public WaveConfigSO WaveConfig => waveConfig;
        
        /// <summary>적 설정 반환</summary>
        public EnemyConfigSO EnemyConfig => enemyConfig;
        
        /// <summary>투사체 설정 반환</summary>
        public ProjectileConfigSO ProjectileConfig => projectileConfig;
        
        /// <summary>히트 이펙트 설정 반환</summary>
        public EffectConfigSO HitEffectConfig => hitEffectConfig;
    }
}
