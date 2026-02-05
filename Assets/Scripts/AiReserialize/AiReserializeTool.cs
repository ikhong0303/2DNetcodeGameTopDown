/// =============================================================================
/// AiReserializeTool.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - Unity 에셋을 강제로 재직렬화하는 에디터 도구입니다.
/// - AI 도구나 버전 관리 시스템에서 YAML 형식의 에셋 파일을 깔끔하게 정리합니다.
/// - 에디터 로드 시 자동 실행 옵션을 제공합니다 (설정에서 조절 가능).
/// - Tools 메뉴와 Assets 컨텍스트 메뉴에서 수동으로 실행할 수 있습니다.
/// - 프리팹, 씬, ScriptableObject, 머티리얼 등 다양한 에셋 타입을 지원합니다.
/// - 배치 처리와 진행률 표시 기능을 제공하여 대량의 에셋을 효율적으로 처리합니다.
/// - Force Text Serialization 모드를 활성화하는 유틸리티 메서드도 포함합니다.
/// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AiReserialize;

/// <summary>
/// AI 재직렬화 에디터 도구
/// 정적 클래스로 메뉴에서 직접 호출 가능
/// </summary>
public static class AiReserializeTool
{
    // 세션당 한 번 실행 체크를 위한 키
    private const string SessionKeyDidRun = "AiReserializeTool_DidRunThisSession";

    /// <summary>
    /// 에디터 로드 시 자동 실행
    /// [InitializeOnLoadMethod]: 에디터가 로드될 때 자동으로 호출됨
    /// </summary>
    [InitializeOnLoadMethod]
    private static void RunOnEditorLoad()
    {
        // 에디터 초기화가 완료된 후 실행 (delayCall 사용)
        EditorApplication.delayCall += () =>
        {
            // 설정 에셋 로드
            AiReserializeSettings settings = LoadSettingsAsset();
            if (settings == null)
                return;  // 설정이 없으면 종료

            // 자동 실행 비활성화 상태면 종료
            if (!settings.runOnEditorLoad)
                return;

            // 세션당 한 번만 실행 옵션이 켜져 있고 이미 실행했으면 종료
            if (settings.runOncePerSession && SessionState.GetBool(SessionKeyDidRun, false))
                return;

            try
            {
                // 설정에 지정된 폴더들을 재직렬화
                ReserializeFoldersFromSettings(settings);
            }
            finally
            {
                // 실행 완료 표시 (세션당 한 번 실행용)
                if (settings.runOncePerSession)
                    SessionState.SetBool(SessionKeyDidRun, true);
            }
        };
    }

    // ===========================
    // Tools 메뉴 항목들
    // ===========================

    /// <summary>
    /// 설정된 폴더들을 즉시 재직렬화
    /// 메뉴: Tools > AI Reserialize > Run Now (Settings Folders)
    /// </summary>
    [MenuItem("Tools/AI Reserialize/Run Now (Settings Folders)")]
    private static void RunNowFromSettingsMenu()
    {
        AiReserializeSettings settings = LoadSettingsAsset();
        if (settings == null)
        {
            Debug.LogWarning("[AI Reserialize] Settings asset not found. Create one via 'Tools/AI Reserialize/Create Settings Asset'.");
            return;
        }

        ReserializeFoldersFromSettings(settings);
    }

    /// <summary>
    /// 설정 에셋을 프로젝트 창에서 선택
    /// 메뉴: Tools > AI Reserialize > Open Settings Asset
    /// </summary>
    [MenuItem("Tools/AI Reserialize/Open Settings Asset")]
    private static void OpenSettingsAssetMenu()
    {
        AiReserializeSettings settings = LoadSettingsAsset();
        if (settings == null)
        {
            Debug.LogWarning("[AI Reserialize] Settings asset not found. Create one via 'Tools/AI Reserialize/Create Settings Asset'.");
            return;
        }

        // Inspector에서 설정 에셋 선택 및 하이라이트
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    /// <summary>
    /// 새 설정 에셋 생성
    /// 메뉴: Tools > AI Reserialize > Create Settings Asset
    /// </summary>
    [MenuItem("Tools/AI Reserialize/Create Settings Asset")]
    private static void CreateSettingsAssetMenu()
    {
        const string defaultFolder = "Assets/Settings";
        EnsureFolderExists(defaultFolder);  // 폴더가 없으면 생성

        // 고유한 에셋 경로 생성
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{defaultFolder}/AiReserializeSettings.asset");
        var settings = ScriptableObject.CreateInstance<AiReserializeSettings>();

        // 에셋 저장
        AssetDatabase.CreateAsset(settings, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 생성된 에셋 선택
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);

        Debug.Log($"[AI Reserialize] Created settings asset: {assetPath}");
    }

    /// <summary>
    /// Force Text Serialization 모드 활성화
    /// Git 등 VCS에서 에셋 파일을 텍스트로 저장하도록 설정
    /// 메뉴: Tools > AI Reserialize > Enable Force Text Serialization
    /// </summary>
    [MenuItem("Tools/AI Reserialize/Enable Force Text Serialization")]
    private static void EnableForceTextSerializationMenu()
    {
        // 직렬화 모드를 텍스트로 강제
        EditorSettings.serializationMode = SerializationMode.ForceText;

        // VCS를 위해 메타 파일을 표시
        EditorSettings.externalVersionControl = "Visible Meta Files";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AI Reserialize] Enabled: Force Text Serialization + Visible Meta Files");
    }

    /// <summary>
    /// 선택된 에셋/폴더 재직렬화
    /// 메뉴: Tools > AI Reserialize > Reserialize Selected (Project Window)
    /// </summary>
    [MenuItem("Tools/AI Reserialize/Reserialize Selected (Project Window)")]
    private static void ReserializeSelectedMenu()
    {
        AiReserializeSettings settings = LoadSettingsAsset();
        ReserializeSelection(settings);
    }

    /// <summary>
    /// OS 폴더 선택 다이얼로그로 폴더 선택 후 재직렬화
    /// 메뉴: Tools > AI Reserialize > Reserialize Folder... (Pick in OS)
    /// </summary>
    [MenuItem("Tools/AI Reserialize/Reserialize Folder... (Pick in OS)")]
    private static void ReserializeFolderPickMenu()
    {
        // OS 폴더 선택 다이얼로그 표시
        string folderOsPath = EditorUtility.OpenFolderPanel("Pick a folder under this Unity project", Application.dataPath, "");
        if (string.IsNullOrEmpty(folderOsPath))
            return;

        // OS 경로를 Unity 에셋 경로로 변환
        string folderAssetPath = ConvertToAssetPath(folderOsPath);
        if (string.IsNullOrEmpty(folderAssetPath) || !AssetDatabase.IsValidFolder(folderAssetPath))
        {
            Debug.LogWarning("[AI Reserialize] Selected folder is not inside this project's Assets folder.");
            return;
        }

        AiReserializeSettings settings = LoadSettingsAsset();
        ReserializeFolders(new List<string> { folderAssetPath }, settings);
    }

    // ===========================
    // Assets 컨텍스트 메뉴 항목들
    // ===========================

    /// <summary>
    /// 선택된 에셋 재직렬화 (우클릭 메뉴)
    /// 메뉴: Assets > AI Reserialize > Reserialize Selected
    /// </summary>
    [MenuItem("Assets/AI Reserialize/Reserialize Selected", false, 2000)]
    private static void ReserializeSelectedContextMenu()
    {
        AiReserializeSettings settings = LoadSettingsAsset();
        ReserializeSelection(settings);
    }

    /// <summary>
    /// 컨텍스트 메뉴 활성화 조건: 에셋이 선택되어 있어야 함
    /// </summary>
    [MenuItem("Assets/AI Reserialize/Reserialize Selected", true)]
    private static bool ValidateReserializeSelectedContextMenu()
    {
        return Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;
    }

    // ===========================
    // 핵심 로직
    // ===========================

    /// <summary>
    /// 설정에 지정된 폴더들을 재직렬화
    /// </summary>
    private static void ReserializeFoldersFromSettings(AiReserializeSettings settings)
    {
        // 유효한 폴더 경로만 필터링
        List<string> folderPaths = settings != null
            ? settings.targetFolderPaths.Where(AssetDatabase.IsValidFolder).Distinct().ToList()
            : new List<string>();

        if (folderPaths.Count == 0)
        {
            Debug.LogWarning("[AI Reserialize] No valid folders to reserialize. Check settings.targetFolderPaths.");
            return;
        }

        ReserializeFolders(folderPaths, settings);
    }

    /// <summary>
    /// 현재 선택된 에셋/폴더를 재직렬화
    /// </summary>
    private static void ReserializeSelection(AiReserializeSettings settings)
    {
        string[] selectedGuids = Selection.assetGUIDs;
        if (selectedGuids == null || selectedGuids.Length == 0)
        {
            Debug.LogWarning("[AI Reserialize] Nothing selected.");
            return;
        }

        // GUID를 에셋 경로로 변환
        var selectedPaths = new List<string>(selectedGuids.Length);
        foreach (string guid in selectedGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                selectedPaths.Add(path);
        }

        // 폴더와 파일 분리
        List<string> folderPaths = selectedPaths.Where(AssetDatabase.IsValidFolder).Distinct().ToList();
        List<string> filePaths = selectedPaths.Where(p => !AssetDatabase.IsValidFolder(p)).Distinct().ToList();

        var collectedPaths = new List<string>();
        collectedPaths.AddRange(filePaths);

        // 폴더 내의 모든 에셋 수집
        if (folderPaths.Count > 0)
        {
            List<string> folderAssetPaths = CollectAssetPathsFromFolders(folderPaths);
            collectedPaths.AddRange(folderAssetPaths);
        }

        ReserializeAssets(collectedPaths, settings, "Selection");
    }

    /// <summary>
    /// 지정된 폴더들의 에셋을 재직렬화
    /// </summary>
    private static void ReserializeFolders(List<string> folderPaths, AiReserializeSettings settings)
    {
        List<string> assetPaths = CollectAssetPathsFromFolders(folderPaths);
        ReserializeAssets(assetPaths, settings, "Folders");
    }

    /// <summary>
    /// 폴더들에서 모든 에셋 경로 수집
    /// </summary>
    private static List<string> CollectAssetPathsFromFolders(List<string> folderPaths)
    {
        // 폴더 내 모든 에셋 GUID 검색
        string[] guids = AssetDatabase.FindAssets("", folderPaths.ToArray());

        var assetPaths = new List<string>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            // 폴더 자체는 제외
            if (AssetDatabase.IsValidFolder(path))
                continue;

            assetPaths.Add(path);
        }

        return assetPaths.Distinct().ToList();
    }

    /// <summary>
    /// 에셋 목록을 재직렬화 (핵심 실행 메서드)
    /// </summary>
    /// <param name="assetPaths">재직렬화할 에셋 경로 목록</param>
    /// <param name="settings">설정 (필터, 배치 크기 등)</param>
    /// <param name="scopeLabel">로그용 범위 레이블</param>
    private static void ReserializeAssets(List<string> assetPaths, AiReserializeSettings settings, string scopeLabel)
    {
        if (assetPaths == null || assetPaths.Count == 0)
        {
            Debug.LogWarning($"[AI Reserialize] No assets found to reserialize ({scopeLabel}).");
            return;
        }

        // null-safe 설정 (없으면 기본값 사용)
        AiReserializeSettings safeSettings = settings != null ? settings : CreateDefaultSettings();

        // 필터 적용
        List<string> filteredPaths = FilterAssetPaths(assetPaths, safeSettings);
        if (filteredPaths.Count == 0)
        {
            Debug.LogWarning($"[AI Reserialize] Assets found but none matched filters ({scopeLabel}).");
            return;
        }

        int batchSize = Mathf.Max(1, safeSettings.batchSize);
        int totalCount = filteredPaths.Count;
        int processedCount = 0;

        DateTime startTime = DateTime.Now;

        // 에셋 편집 시작 (성능 최적화)
        AssetDatabase.StartAssetEditing();
        try
        {
            // 배치 단위로 처리
            for (int i = 0; i < totalCount; i += batchSize)
            {
                int count = Mathf.Min(batchSize, totalCount - i);
                List<string> batchPaths = filteredPaths.GetRange(i, count);

                float progress = totalCount <= 0 ? 1f : (float)processedCount / totalCount;

                // 진행률 표시
                if (safeSettings.showProgressBar)
                {
                    bool canceled = EditorUtility.DisplayCancelableProgressBar(
                        "AI Reserialize",
                        $"Reserializing {scopeLabel}... ({processedCount}/{totalCount})",
                        progress);

                    // 사용자가 취소함
                    if (canceled)
                    {
                        Debug.LogWarning("[AI Reserialize] Canceled by user.");
                        break;
                    }
                }

                // 배치 재직렬화 실행
                ForceReserialize(batchPaths);
                processedCount += count;
            }
        }
        finally
        {
            // 에셋 편집 종료
            AssetDatabase.StopAssetEditing();
            if (safeSettings.showProgressBar)
                EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 결과 로그 출력
        if (safeSettings.logToConsole)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            Debug.Log($"[AI Reserialize] Done. Scope={scopeLabel}, Reserialized={processedCount}/{totalCount}, Time={elapsed.TotalSeconds:0.00}s");
        }
    }

    /// <summary>
    /// 에셋 강제 재직렬화 실행
    /// </summary>
    private static void ForceReserialize(List<string> assetPaths)
    {
        // Unity 2021.2 이상에서는 메타데이터도 함께 재직렬화
#if UNITY_2021_2_OR_NEWER
        AssetDatabase.ForceReserializeAssets(assetPaths, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
#else
        AssetDatabase.ForceReserializeAssets(assetPaths);
#endif
    }

    /// <summary>
    /// 설정에 따라 에셋 경로 필터링
    /// </summary>
    private static List<string> FilterAssetPaths(List<string> assetPaths, AiReserializeSettings settings)
    {
        var filtered = new List<string>(assetPaths.Count);

        foreach (string path in assetPaths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            // 스크립트 및 프로젝트 설정 파일 제외 (AI YAML 워크플로우에서 불필요)
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".cs" || ext == ".asmdef" || ext == ".dll" || ext == ".json" || ext == ".txt")
                continue;

            // 설정에 따른 포함 여부 체크
            if (!ShouldIncludePath(path, settings))
                continue;

            filtered.Add(path);
        }

        return filtered.Distinct().ToList();
    }

    /// <summary>
    /// 에셋 경로가 설정의 필터 조건을 만족하는지 확인
    /// </summary>
    private static bool ShouldIncludePath(string assetPath, AiReserializeSettings settings)
    {
        string ext = Path.GetExtension(assetPath).ToLowerInvariant();

        // 프리팹
        if (ext == ".prefab")
            return settings.includePrefabs;

        // 씬
        if (ext == ".unity")
            return settings.includeScenes;

        // 머티리얼
        if (ext == ".mat")
            return settings.includeMaterials;

        // .asset 파일 (ScriptableObject 또는 기타)
        if (ext == ".asset")
        {
            if (!settings.includeScriptableObjects && !settings.includeOtherAssets)
                return false;

            // 실제 타입 확인
            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            bool isScriptableObject = mainType != null && typeof(ScriptableObject).IsAssignableFrom(mainType);

            if (isScriptableObject)
                return settings.includeScriptableObjects;

            return settings.includeOtherAssets;
        }

        // 기타 에셋 타입
        if (!settings.includeOtherAssets)
            return false;

        // 일반적인 YAML 형식 Unity 에셋들
        return ext == ".anim"
            || ext == ".controller"
            || ext == ".overridecontroller"
            || ext == ".playable"
            || ext == ".mask"
            || ext == ".physicmaterial"
            || ext == ".guiskin";
    }

    /// <summary>
    /// 설정 에셋 로드
    /// </summary>
    private static AiReserializeSettings LoadSettingsAsset()
    {
        // 프로젝트에서 AiReserializeSettings 타입 에셋 검색
        string[] guids = AssetDatabase.FindAssets("t:AiReserializeSettings");
        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<AiReserializeSettings>(path);
    }

    /// <summary>
    /// 기본 설정 생성 (임시용)
    /// </summary>
    private static AiReserializeSettings CreateDefaultSettings()
    {
        var settings = ScriptableObject.CreateInstance<AiReserializeSettings>();
        settings.runOnEditorLoad = false;
        settings.runOncePerSession = false;
        return settings;
    }

    /// <summary>
    /// 폴더가 없으면 생성 (재귀적)
    /// </summary>
    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = "Assets";
        string[] parts = folderPath.Replace("\\", "/").Split('/');

        // 경로를 한 단계씩 생성
        for (int i = 1; i < parts.Length; i++)
        {
            string current = $"{parent}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(current))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = current;
        }
    }

    /// <summary>
    /// OS 경로를 Unity 에셋 경로로 변환
    /// </summary>
    private static string ConvertToAssetPath(string osPath)
    {
        if (string.IsNullOrEmpty(osPath))
            return null;

        // 경로 정규화
        string dataPath = Application.dataPath.Replace("\\", "/");
        string normalized = osPath.Replace("\\", "/");

        // Assets 폴더 하위인지 확인
        if (!normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            return null;

        // 상대 경로로 변환
        string relative = "Assets" + normalized.Substring(dataPath.Length);
        return relative;
    }
}
