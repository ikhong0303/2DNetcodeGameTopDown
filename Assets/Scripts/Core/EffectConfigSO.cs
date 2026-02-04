/// =============================================================================
/// EffectConfigSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 시각 이펙트(파티클, 히트 이펙트 등)의 설정을 저장하는 ScriptableObject입니다.
/// - 이펙트 프리팹, 지속 시간(lifetime), 오브젝트 풀 크기를 설정할 수 있습니다.
/// - 게임 매니저에서 참조하여 히트 이펙트 등을 생성할 때 사용됩니다.
/// =============================================================================

using UnityEngine;
using TopDownShooter.Networking;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 이펙트 설정 ScriptableObject
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Config/Effect Config로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Config/Effect Config")]
    public class EffectConfigSO : ScriptableObject
    {
        // ===== 인스펙터에서 설정할 필드들 =====
        
        [SerializeField] private NetworkEffect effectPrefab;   // 이펙트 프리팹 (NetworkEffect 컴포넌트 필요)
        [SerializeField] private float lifetime = 1f;          // 이펙트 지속 시간 (초)
        [SerializeField] private int poolSize = 16;            // 오브젝트 풀 크기 (미리 생성할 개수)

        // ===== 읽기 전용 프로퍼티 (외부에서 값을 읽을 때 사용) =====
        
        /// <summary>이펙트 프리팹 반환</summary>
        public NetworkEffect EffectPrefab => effectPrefab;
        
        /// <summary>이펙트 지속 시간 반환 (초)</summary>
        public float Lifetime => lifetime;
        
        /// <summary>오브젝트 풀 크기 반환</summary>
        public int PoolSize => poolSize;
    }
}
