/// =============================================================================
/// SoundManager.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 게임의 사운드(효과음, 배경음악)를 중앙에서 관리하는 싱글톤 매니저입니다.
/// - DontDestroyOnLoad로 씬 전환 시에도 유지됩니다.
/// - PlaySfx(): 효과음을 재생합니다 (현재 플레이스홀더).
/// - PlayMusic(): 배경음악을 재생합니다.
/// - PlayClip(): 특정 AudioClip을 직접 재생합니다.
/// =============================================================================

using UnityEngine;
using TopDownShooter.Core;

namespace TopDownShooter.Managers
{
    /// <summary>
    /// 사운드 매니저 (싱글톤)
    /// 게임 전체의 음향 효과를 관리
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        // ===== 싱글톤 인스턴스 =====
        
        /// <summary>전역 인스턴스</summary>
        public static SoundManager Instance { get; private set; }

        /// <summary>
        /// Awake: 싱글톤 설정 및 씬 전환 시 유지
        /// </summary>
        private void Awake()
        {
            // 이미 인스턴스가 있고 이 객체가 아니면 파괴 (중복 방지)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            // 씬 전환 시에도 이 오브젝트 유지
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 효과음을 재생합니다.
        /// </summary>
        /// <param name="sfxName">재생할 효과음 이름</param>
        public void PlaySfx(string sfxName)
        {
            // TODO: 실제 오디오 재생 구현
            // 현재는 플레이스홀더 (성능을 위해 로그도 비활성화)
            // Debug.Log($"[SFX] Play Sound: {sfxName}");
        }

        /// <summary>
        /// 배경음악을 재생합니다.
        /// </summary>
        /// <param name="musicName">재생할 음악 이름</param>
        public void PlayMusic(string musicName)
        {
            // TODO: 실제 음악 재생 구현
        }
        
        /// <summary>
        /// 특정 AudioClip을 직접 재생합니다.
        /// </summary>
        /// <param name="clip">재생할 오디오 클립</param>
        public void PlayClip(AudioClip clip)
        {
            // null 체크
            if (clip != null)
            {
                // TODO: 실제 오디오 재생 구현
            }
        }
    }
}
