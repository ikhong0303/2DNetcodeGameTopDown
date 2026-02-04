/// =============================================================================
/// EnemyConfigSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 적(Enemy) 프리팹을 저장하는 ScriptableObject입니다.
/// - 각 적의 스탯(HP, 속도, 데미지 등)은 프리팹에 직접 설정됩니다.
/// - 게임 매니저에서 적을 스폰할 때 이 프리팹을 참조합니다.
/// =============================================================================

using UnityEngine;
using TopDownShooter.Networking;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 적 캐릭터 설정 ScriptableObject
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Config/Enemy Config로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Config/Enemy Config")]
    public class EnemyConfigSO : ScriptableObject
    {
        // ===== 인스펙터에서 설정할 필드들 =====
        
        [SerializeField] private NetworkEnemy enemyPrefab;     // 적 프리팹 (스탯은 프리팹에 직접 설정)

        // ===== 읽기 전용 프로퍼티 =====
        
        /// <summary>적 프리팹 반환</summary>
        public NetworkEnemy EnemyPrefab => enemyPrefab;
    }
}
