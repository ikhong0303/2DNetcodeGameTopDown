/// =============================================================================
/// NetworkSessionLauncher.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크 세션(호스트, 클라이언트, 서버)을 시작하고 종료하는 유틸리티입니다.
/// - Unity Relay 서비스를 통한 온라인 멀티플레이를 지원합니다.
/// - StartHostWithRelay(): Relay를 통해 호스트로 게임을 시작하고 Join Code를 생성합니다.
/// - StartClientWithRelay(): Join Code를 입력하여 Relay 서버에 접속합니다.
/// - UI 버튼 등에 연결하여 네트워크 기능을 쉽게 사용할 수 있습니다.
/// =============================================================================

using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 세션 시작/종료 유틸리티
    /// Unity Relay 서비스를 통한 온라인 멀티플레이 지원
    /// </summary>
    public class NetworkSessionLauncher : MonoBehaviour
    {
        // ===== UI 참조 =====
        
        [Header("Relay UI")]
        [Tooltip("Join Code를 표시할 텍스트 (호스트용)")]
        [SerializeField] private TextMeshProUGUI joinCodeText;
        
        [Tooltip("Join Code를 입력할 필드 (클라이언트용)")]
        [SerializeField] private TMP_InputField joinCodeInput;

        // ===== 설정 =====
        
        [Header("Settings")]
        [Tooltip("최대 플레이어 수 (호스트 포함)")]
        [SerializeField, Min(2)] private int maxPlayers = 2;

        // ===== 상태 =====
        
        private bool servicesReady = false;
        
        /// <summary>현재 Join Code (호스트일 때만 유효)</summary>
        public string CurrentJoinCode { get; private set; }

        // ===== 상수 =====
        
        /// <summary>연결 타입 (Host와 Client가 동일해야 함)</summary>
        private const string ConnectionType = "dtls";

        /// <summary>호스트 제외 최대 연결 수</summary>
        private int MaxConnectionsExcludingHost => Mathf.Max(1, maxPlayers - 1);

        // ===== Unity Services 초기화 =====

        /// <summary>
        /// Unity Services 초기화 및 익명 로그인
        /// </summary>
        private async Task EnsureServicesAsync()
        {
            if (servicesReady) return;

            // Unity Services 초기화
            await UnityServices.InitializeAsync();

            // 익명 로그인 (로그인되지 않았을 때만)
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            servicesReady = true;
        }

        // ===== Relay 호스트 =====

        /// <summary>
        /// Unity Relay를 통해 호스트로 게임을 시작합니다.
        /// Join Code가 생성되어 다른 플레이어가 참가할 수 있습니다.
        /// </summary>
        public async void StartHostWithRelay()
        {
            try
            {
                await EnsureServicesAsync();

                var networkManager = NetworkManager.Singleton;
                if (networkManager == null)
                {
                    return;
                }

                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    return;
                }

                // 이미 실행 중이면 중지
                if (networkManager.IsListening)
                {
                    networkManager.Shutdown();
                    await Task.Delay(500);  // 잠시 대기
                }

                // 1) Relay 방 생성 (호스트 제외 인원 수)
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnectionsExcludingHost);

                // 2) Join Code 발급
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                CurrentJoinCode = joinCode;

                // 3) UnityTransport에 Relay 연결정보 설정
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));

                // 4) UI에 Join Code 표시
                if (joinCodeText != null)
                {
                    joinCodeText.text = $"Join Code: {joinCode}";
                }

                // 5) 호스트 시작
                networkManager.StartHost();
            }
            catch (Exception e)
            {
                if (joinCodeText != null)
                {
                    joinCodeText.text = $"Error: {e.Message}";
                }
            }
        }

        // ===== Relay 클라이언트 =====

        /// <summary>
        /// Unity Relay를 통해 클라이언트로 게임에 참가합니다.
        /// Join Code를 입력하여 호스트의 세션에 접속합니다.
        /// </summary>
        public async void StartClientWithRelay()
        {
            Debug.Log("[Relay] StartClientWithRelay 호출됨");
            
            try
            {
                // UI 피드백
                if (joinCodeText != null)
                {
                    joinCodeText.text = "서비스 초기화 중...";
                }
                
                await EnsureServicesAsync();
                Debug.Log("[Relay] 서비스 초기화 완료");

                var networkManager = NetworkManager.Singleton;
                if (networkManager == null)
                {
                    Debug.LogError("[Relay] NetworkManager.Singleton이 null입니다!");
                    return;
                }

                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[Relay] UnityTransport가 없습니다!");
                    return;
                }

                // Join Code 가져오기
                string joinCode = "";
                if (joinCodeInput != null)
                {
                    joinCode = (joinCodeInput.text ?? string.Empty).Trim().ToUpper();
                    Debug.Log($"[Relay] 입력된 Join Code: '{joinCode}'");
                }
                else
                {
                    Debug.LogError("[Relay] joinCodeInput이 할당되지 않았습니다!");
                    if (joinCodeText != null)
                    {
                        joinCodeText.text = "Error: Join Code 입력 필드가 없습니다";
                    }
                    return;
                }

                if (string.IsNullOrEmpty(joinCode))
                {
                    Debug.LogWarning("[Relay] Join Code가 비어있습니다!");
                    if (joinCodeText != null)
                    {
                        joinCodeText.text = "Join Code를 입력하세요";
                    }
                    return;
                }

                // 이미 실행 중이면 중지
                if (networkManager.IsListening)
                {
                    Debug.Log("[Relay] 기존 세션 종료 중...");
                    networkManager.Shutdown();
                    await Task.Delay(500);
                }

                // UI 피드백
                if (joinCodeText != null)
                {
                    joinCodeText.text = $"'{joinCode}' 세션에 참가 중...";
                }

                // 1) Join Code로 Relay 방 참가
                Debug.Log($"[Relay] Relay 서버에 참가 시도: {joinCode}");
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log("[Relay] Relay 참가 성공!");

                // 2) UnityTransport에 Relay 연결정보 설정
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, ConnectionType));
                Debug.Log("[Relay] Transport 설정 완료");

                // 3) 클라이언트 시작
                bool started = networkManager.StartClient();
                Debug.Log($"[Relay] StartClient 결과: {started}");
                
                if (joinCodeText != null)
                {
                    joinCodeText.text = started ? "접속 성공!" : "접속 실패";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Relay] Join Error: {e}");
                if (joinCodeText != null)
                {
                    joinCodeText.text = $"Join Error: {e.Message}";
                }
            }
        }

        // ===== 로컬 (기존 방식) =====

        /// <summary>
        /// 로컬 호스트로 게임을 시작합니다 (LAN 전용).
        /// </summary>
        public void StartHost()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }
        }

        /// <summary>
        /// 로컬 클라이언트로 접속합니다 (LAN 전용).
        /// </summary>
        public void StartClient()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        /// <summary>
        /// 전용 서버로 게임을 시작합니다.
        /// </summary>
        public void StartServer()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartServer();
            }
        }

        // ===== 세션 종료 =====

        /// <summary>
        /// 현재 네트워크 세션에서 나갑니다.
        /// </summary>
        public void LeaveSession()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            CurrentJoinCode = null;
            
            if (joinCodeText != null)
            {
                joinCodeText.text = "";
            }
        }
    }
}
