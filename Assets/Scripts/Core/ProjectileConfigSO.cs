/// =============================================================================
/// ProjectileConfigSO.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 투사체(총알, 미사일 등)의 설정을 저장하는 ScriptableObject입니다.
/// - 투사체 프리팹, 속도, 데미지, 지속 시간, 오브젝트 풀 크기를 설정합니다.
/// - 플레이어 컨트롤러에서 발사 시 이 설정 값들을 참조합니다.
/// =============================================================================

using UnityEngine;
using TopDownShooter.Networking;

namespace TopDownShooter.Core
{
    /// <summary>
    /// 투사체 설정 ScriptableObject
    /// Unity 에디터에서 Assets > Create > TopDownShooter/Config/Projectile Config로 생성
    /// </summary>
    [CreateAssetMenu(menuName = "TopDownShooter/Config/Projectile Config")]
    public class ProjectileConfigSO : ScriptableObject
    {
        // ===== 인스펙터에서 설정할 필드들 =====
        
        [SerializeField] private NetworkProjectile projectilePrefab;   // 투사체 프리팹
        [SerializeField] private float speed = 10f;                    // 투사체 속도 (초당 유닛)
        [SerializeField] private int damage = 1;                       // 투사체 데미지
        [SerializeField] private float lifetime = 2f;                  // 투사체 수명 (초), 이후 자동 삭제
        [SerializeField] private int poolSize = 32;                    // 오브젝트 풀 크기

        // ===== 읽기 전용 프로퍼티 =====
        
        /// <summary>투사체 프리팹 반환</summary>
        public NetworkProjectile ProjectilePrefab => projectilePrefab;
        
        /// <summary>투사체 속도 반환</summary>
        public float Speed => speed;
        
        /// <summary>투사체 데미지 반환</summary>
        public int Damage => damage;
        
        /// <summary>투사체 수명 반환 (초)</summary>
        public float Lifetime => lifetime;
        
        /// <summary>오브젝트 풀 크기 반환</summary>
        public int PoolSize => poolSize;
    }
}
