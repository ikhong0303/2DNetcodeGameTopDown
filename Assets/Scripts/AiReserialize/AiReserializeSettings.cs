/// =============================================================================
/// AiReserializeSettings.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - AI 재직렬화 도구의 설정을 저장하는 ScriptableObject입니다.
/// - Unity 에디터 로드 시 자동 실행 여부, 대상 폴더 경로, 에셋 타입 필터 등을 설정합니다.
/// - 프리팹, 씬, ScriptableObject, 머티리얼 등 어떤 에셋을 재직렬화할지 선택할 수 있습니다.
/// - 배치 크기, 진행률 표시 여부, 콘솔 로그 출력 여부 등 실행 옵션을 제공합니다.
/// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace AiReserialize
{
    /// <summary>
    /// AI 재직렬화 도구 설정 ScriptableObject
    /// Unity 에디터에서 Assets > Create > Tools/AI Reserialize/Settings로 생성
    /// </summary>
    [CreateAssetMenu(
        fileName = "AiReserializeSettings",
        menuName = "Tools/AI Reserialize/Settings",
        order = 0)]
    public sealed class AiReserializeSettings : ScriptableObject
    {
        // ===== 자동 실행 설정 =====
        
        [Header("Auto Run")]
        /// <summary>에디터 로드 시 자동으로 재직렬화 실행 여부</summary>
        public bool runOnEditorLoad = true;

        /// <summary>세션당 한 번만 실행할지 여부</summary>
        [Tooltip("If true, auto-run occurs only once per editor session.")]
        public bool runOncePerSession = true;

        // ===== 대상 폴더 설정 =====
        
        [Header("Target Folders (Project Relative)")]
        /// <summary>
        /// 재직렬화할 대상 폴더 경로 목록
        /// 프로젝트 상대 경로로 지정 (예: Assets/Prefabs)
        /// </summary>
        [Tooltip("Example: Assets/Prefabs, Assets/Data")]
        public List<string> targetFolderPaths = new List<string>
        {
            "Assets/Prefabs",
            "Assets/Data",
            "Assets/Resources",
            "Assets/ETC"
        };

        // ===== 에셋 타입 필터 =====
        
        [Header("Asset Type Filters")]
        /// <summary>프리팹(.prefab) 포함 여부</summary>
        public bool includePrefabs = true;
        
        /// <summary>씬(.unity) 포함 여부</summary>
        public bool includeScenes = true;
        
        /// <summary>ScriptableObject(.asset) 포함 여부</summary>
        public bool includeScriptableObjects = true;
        
        /// <summary>머티리얼(.mat) 포함 여부</summary>
        public bool includeMaterials = false;

        /// <summary>
        /// 기타 에셋 포함 여부
        /// ScriptableObject가 아닌 .asset 파일과 .anim, .controller 등 YAML 형식 에셋
        /// </summary>
        [Tooltip("Include other .asset files that are NOT ScriptableObject, and other YAML-like assets (.anim, .controller, etc.).")]
        public bool includeOtherAssets = false;

        // ===== 실행 옵션 =====
        
        [Header("Execution")]
        /// <summary>한 번에 처리할 에셋 수 (배치 크기)</summary>
        [Min(1)]
        public int batchSize = 200;

        /// <summary>진행률 표시줄 표시 여부</summary>
        public bool showProgressBar = true;
        
        /// <summary>콘솔에 로그 출력 여부</summary>
        public bool logToConsole = true;
    }
}
