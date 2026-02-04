/// =============================================================================
/// NetworkUIManager.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 게임 상태를 화면에 표시하는 UI 매니저입니다.
/// - 게임 시작 카운트다운, 웨이브 정보, 남은 적 수, 게임 오버 등을 표시합니다.
/// - StringEventChannelSO를 통해 상태 메시지를 수신합니다.
/// - IntEventChannelSO를 통해 웨이브 시작/남은 적 수를 수신합니다.
/// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TopDownShooter.Core;

namespace TopDownShooter.UI
{
    /// <summary>
    /// 네트워크 UI 매니저
    /// 게임 상태 정보를 화면에 표시하는 UI를 관리합니다.
    /// </summary>
    public class NetworkUIManager : MonoBehaviour
    {
        // ===== 싱글톤 인스턴스 =====
        
        /// <summary>전역 인스턴스 (싱글톤)</summary>
        public static NetworkUIManager Instance { get; private set; }

        // ===== 인스펙터에서 설정할 UI 요소들 =====
        
        [Header("UI References")]
        [Tooltip("상태 메시지를 표시할 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Tooltip("웨이브 정보를 표시할 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI waveInfoText;
        
        [Tooltip("남은 적 수를 표시할 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI enemyCountText;

        [Header("재시작 팝업 UI")]
        [Tooltip("재시작 팝업 패널 (Canvas 그룹)")]
        [SerializeField] private GameObject restartPopup;
        
        [Tooltip("팝업 제목 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI popupTitleText;
        
        [Tooltip("팝업 메시지 텍스트 (TMP)")]
        [SerializeField] private TextMeshProUGUI popupMessageText;
        
        [Tooltip("재시작 버튼")]
        [SerializeField] private UnityEngine.UI.Button restartButton;

        [Header("Event Channels")]
        [Tooltip("상태 메시지 이벤트 채널")]
        [SerializeField] private StringEventChannelSO statusMessageEvent;
        
        [Tooltip("웨이브 시작 이벤트 채널")]
        [SerializeField] private IntEventChannelSO waveStartedEvent;
        
        [Tooltip("웨이브 완료 이벤트 채널")]
        [SerializeField] private GameEventChannelSO waveCompletedEvent;
        
        [Tooltip("게임 오버 이벤트 채널")]
        [SerializeField] private GameEventChannelSO gameOverEvent;
        
        [Tooltip("남은 적 수 업데이트 이벤트 채널")]
        [SerializeField] private IntEventChannelSO enemyCountUpdatedEvent;

        [Header("Settings")]
        [Tooltip("상태 메시지 자동 숨김 시간 (0이면 자동 숨김 안 함)")]
        [SerializeField] private float statusMessageDuration = 3f;

        // ===== 내부 변수들 =====
        
        private int currentWave = 0;           // 현재 웨이브 번호
        private int currentEnemyCount = 0;     // 현재 남은 적 수
        private Coroutine hideStatusCoroutine; // 상태 메시지 숨김 코루틴

        /// <summary>
        /// Awake: 싱글톤 설정
        /// </summary>
        private void Awake()
        {
            // 이미 인스턴스가 있고 이 객체가 아니면 파괴
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// OnEnable: 이벤트 구독
        /// </summary>
        private void OnEnable()
        {
            // 상태 메시지 이벤트 구독
            if (statusMessageEvent != null)
            {
                statusMessageEvent.EventRaised += OnStatusMessageReceived;
            }

            // 웨이브 시작 이벤트 구독
            if (waveStartedEvent != null)
            {
                waveStartedEvent.EventRaised += OnWaveStarted;
            }

            // 웨이브 완료 이벤트 구독
            if (waveCompletedEvent != null)
            {
                waveCompletedEvent.EventRaised += OnWaveCompleted;
            }

            // 게임 오버 이벤트 구독
            if (gameOverEvent != null)
            {
                gameOverEvent.EventRaised += OnGameOver;
            }

            // 남은 적 수 업데이트 이벤트 구독
            if (enemyCountUpdatedEvent != null)
            {
                enemyCountUpdatedEvent.EventRaised += OnEnemyCountUpdated;
            }
        }

        /// <summary>
        /// OnDisable: 이벤트 구독 해제
        /// </summary>
        private void OnDisable()
        {
            // 상태 메시지 이벤트 구독 해제
            if (statusMessageEvent != null)
            {
                statusMessageEvent.EventRaised -= OnStatusMessageReceived;
            }

            // 웨이브 시작 이벤트 구독 해제
            if (waveStartedEvent != null)
            {
                waveStartedEvent.EventRaised -= OnWaveStarted;
            }

            // 웨이브 완료 이벤트 구독 해제
            if (waveCompletedEvent != null)
            {
                waveCompletedEvent.EventRaised -= OnWaveCompleted;
            }

            // 게임 오버 이벤트 구독 해제
            if (gameOverEvent != null)
            {
                gameOverEvent.EventRaised -= OnGameOver;
            }

            // 남은 적 수 업데이트 이벤트 구독 해제
            if (enemyCountUpdatedEvent != null)
            {
                enemyCountUpdatedEvent.EventRaised -= OnEnemyCountUpdated;
            }
        }

        /// <summary>
        /// Start: 초기 UI 상태 설정
        /// </summary>
        private void Start()
        {
            // 초기 UI 숨기기
            HideAllUI();
            
            // 대기 상태 메시지 표시
            ShowStatusMessage("플레이어 대기 중...", false);
        }

        // ===== 이벤트 핸들러 =====

        /// <summary>
        /// 상태 메시지 수신 시 호출
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        private void OnStatusMessageReceived(string message)
        {
            ShowStatusMessage(message, true);
        }

        /// <summary>
        /// 웨이브 시작 시 호출
        /// </summary>
        /// <param name="waveNumber">시작된 웨이브 번호</param>
        private void OnWaveStarted(int waveNumber)
        {
            currentWave = waveNumber;
            UpdateWaveInfoUI();
            
            // 주의: 상태 메시지는 NetworkGameManager에서 직접 관리
            // ShowStatusMessage($"웨이브 {waveNumber} 시작!", true); ← 제거됨
        }

        /// <summary>
        /// 웨이브 완료 시 호출
        /// </summary>
        private void OnWaveCompleted()
        {
            // 주의: 모든 UI 업데이트는 NetworkGameManager에서 BroadcastStatusMessageClientRpc와 
            // BroadcastEnemyCountClientRpc를 통해 직접 관리함
            // 여기서 추가 처리하면 중복 발생
        }

        /// <summary>
        /// 게임 오버 시 호출
        /// </summary>
        private void OnGameOver()
        {
            ShowStatusMessage("게임 오버!", false);
        }

        /// <summary>
        /// 남은 적 수 업데이트 시 호출
        /// </summary>
        /// <param name="count">남은 적 수</param>
        private void OnEnemyCountUpdated(int count)
        {
            currentEnemyCount = count;
            UpdateEnemyCountUI();
        }

        // ===== UI 업데이트 메서드 =====

        /// <summary>
        /// 상태 메시지를 표시합니다.
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <param name="autoHide">자동 숨김 여부</param>
        public void ShowStatusMessage(string message, bool autoHide = true)
        {
            if (statusText == null)
            {
                return;
            }

            // 기존 숨김 코루틴 취소
            if (hideStatusCoroutine != null)
            {
                StopCoroutine(hideStatusCoroutine);
                hideStatusCoroutine = null;
            }

            // 메시지 표시
            statusText.text = message;
            statusText.gameObject.SetActive(true);

            // 자동 숨김
            if (autoHide && statusMessageDuration > 0)
            {
                hideStatusCoroutine = StartCoroutine(HideStatusAfterDelay());
            }
        }

        /// <summary>
        /// 일정 시간 후 상태 메시지를 숨깁니다.
        /// </summary>
        private System.Collections.IEnumerator HideStatusAfterDelay()
        {
            yield return new WaitForSeconds(statusMessageDuration);
            
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
            
            hideStatusCoroutine = null;
        }

        /// <summary>
        /// 웨이브 정보 UI를 업데이트합니다.
        /// </summary>
        private void UpdateWaveInfoUI()
        {
            if (waveInfoText == null)
            {
                return;
            }

            waveInfoText.gameObject.SetActive(true);
            waveInfoText.text = $"웨이브 {currentWave}";
        }

        /// <summary>
        /// 남은 적 수 UI를 업데이트합니다.
        /// </summary>
        private void UpdateEnemyCountUI()
        {
            if (enemyCountText == null)
            {
                return;
            }

            enemyCountText.gameObject.SetActive(true);
            enemyCountText.text = $"남은 적: {currentEnemyCount}";
        }

        /// <summary>
        /// 모든 UI를 숨깁니다.
        /// </summary>
        private void HideAllUI()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            if (waveInfoText != null)
            {
                waveInfoText.gameObject.SetActive(false);
            }

            if (enemyCountText != null)
            {
                enemyCountText.gameObject.SetActive(false);
            }
        }

        // ===== 외부 접근용 메서드 =====

        /// <summary>
        /// 카운트다운 메시지를 표시합니다.
        /// NetworkGameManager에서 직접 호출할 수 있습니다.
        /// </summary>
        /// <param name="seconds">남은 초</param>
        public void ShowCountdown(int seconds)
        {
            ShowStatusMessage($"게임 시작까지 {seconds}초", false);
        }

        /// <summary>
        /// 재시작 카운트다운 메시지를 표시합니다.
        /// </summary>
        /// <param name="seconds">남은 초</param>
        public void ShowRestartCountdown(int seconds)
        {
            ShowStatusMessage($"재시작까지 {seconds}초", false);
        }

        /// <summary>
        /// 플레이어 대기 상태를 표시합니다.
        /// </summary>
        /// <param name="currentPlayers">현재 접속한 플레이어 수</param>
        /// <param name="requiredPlayers">필요한 플레이어 수</param>
        public void ShowWaitingForPlayers(int currentPlayers, int requiredPlayers)
        {
            ShowStatusMessage($"플레이어 대기 중... ({currentPlayers}/{requiredPlayers})", false);
        }

        // ===== 재시작 팝업 =====

        /// <summary>
        /// 재시작 팝업을 표시합니다.
        /// </summary>
        /// <param name="isVictory">승리인 경우 true</param>
        /// <param name="difficultyLevel">현재 난이도 레벨</param>
        public void ShowRestartPopup(bool isVictory, int difficultyLevel)
        {
            if (restartPopup == null) return;
            
            restartPopup.SetActive(true);
            
            if (isVictory)
            {
                // 승리 메시지
                if (popupTitleText != null)
                    popupTitleText.text = "승리!";
                    
                if (popupMessageText != null)
                    popupMessageText.text = $"모든 웨이브 클리어!\n\n난이도 {difficultyLevel + 1}로 증가합니다.";
            }
            else
            {
                // 패배 메시지
                if (popupTitleText != null)
                    popupTitleText.text = "패배";
                    
                if (popupMessageText != null)
                    popupMessageText.text = "웨이브 1부터 다시 시작합니다.";
            }
            
            // 재시작 버튼 클릭 이벤트 설정
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }
        }

        /// <summary>
        /// 재시작 팝업을 숨깁니다.
        /// </summary>
        public void HideRestartPopup()
        {
            if (restartPopup != null)
            {
                restartPopup.SetActive(false);
            }
        }

        /// <summary>
        /// 재시작 버튼 클릭 핸들러
        /// </summary>
        private void OnRestartButtonClicked()
        {
            // 서버에 재시작 요청
            TopDownShooter.Networking.NetworkGameManager.Instance?.RequestRestartServerRpc();
        }
    }
}
