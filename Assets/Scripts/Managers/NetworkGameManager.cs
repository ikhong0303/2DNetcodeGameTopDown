/// =============================================================================
/// NetworkGameManager.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 멀티플레이어 게임의 전체 흐름을 관리하는 핵심 매니저입니다.
/// - 싱글톤 패턴으로 구현되어 게임 전역에서 접근할 수 있습니다.
/// - 웨이브 시스템: 설정된 웨이브에 따라 적을 스폰하고 진행합니다.
/// - 게임 오버 체크: 모든 플레이어가 다운되면 게임 오버를 선언합니다.
/// - 자동 재시작: 게임 오버 후 일정 시간 뒤 자동으로 게임을 재시작합니다.
/// - 오브젝트 풀 초기화: 투사체와 이펙트 풀을 미리 생성합니다.
/// - 이벤트 방송: 웨이브 시작/완료, 게임 오버 이벤트를 서버와 클라이언트에 전달합니다.
/// =============================================================================

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;
using TopDownShooter.UI;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 게임 매니저 (싱글톤)
    /// 게임 전체 흐름을 관리하는 핵심 클래스
    /// </summary>
    public class NetworkGameManager : NetworkBehaviour
    {
        // ===== 싱글톤 인스턴스 =====
        
        /// <summary>전역 인스턴스 (싱글톤)</summary>
        public static NetworkGameManager Instance { get; private set; }

        // ===== 인스펙터에서 설정할 필드들 =====
        
        [Header("Config")]
        [SerializeField] private GameConfigSO gameConfig;          // 게임 전체 설정
        [SerializeField] private EnemySpawner enemySpawner;        // 적 스폰 관리자 (SRP 분리)

        [Header("Events")]
        [SerializeField] private IntEventChannelSO waveStartedEvent;     // 웨이브 시작 이벤트 (웨이브 번호 전달)
        [SerializeField] private GameEventChannelSO waveCompletedEvent;  // 웨이브 완료 이벤트
        [SerializeField] private GameEventChannelSO gameOverEvent;       // 게임 오버 이벤트 (패배)
        [SerializeField] private GameEventChannelSO gameWinEvent;        // 게임 승리 이벤트

        [Header("UI Events")]
        [SerializeField] private StringEventChannelSO statusMessageEvent;      // 상태 메시지 이벤트
        [SerializeField] private IntEventChannelSO enemyCountUpdatedEvent;     // 남은 적 수 업데이트 이벤트

        // ===== 상태 변수들 =====
        
        private int currentWaveIndex;                               // 현재 웨이브 인덱스
        private bool isGameOver;                                    // 게임 오버 상태
        private bool isGameStarted;                                 // 게임 시작 여부
        private int difficultyLevel = 1;                           // 난이도 레벨 (1부터 시작)
        private bool waitingForRestart = false;                    // 재시작 대기 중
        private Coroutine waveLoopCoroutine;                       // 웨이브 루프 코루틴 참조
        private bool wasVictory = false;                           // 승리 후 재시작인지 확인용
        
        /// <summary>현재 난이도 레벨 (외부 접근용)</summary>
        public int DifficultyLevel => difficultyLevel;
        
        // ===== UI 업데이트용 변수들 =====
        
        private int currentWaveTotalEnemies;                        // 현재 웨이브 총 적 수
        private int killedEnemiesInWave;                            // 이번 웨이브에서 처치한 적 수

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
        /// 네트워크 스폰 시 호출
        /// 서버에서 풀 초기화 및 게임 시작
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                PrewarmPools();                          // 오브젝트 풀 미리 생성
                StartCoroutine(StartGameRoutine());      // 게임 시작 루틴
            }
        }

        /// <summary>
        /// 게임 시작 루틴
        /// 2명의 플레이어가 접속할 때까지 대기 후 카운트다운
        /// </summary>
        private IEnumerator StartGameRoutine()
        {
            
            // 2명 접속할 때까지 대기
            while (NetworkManager.Singleton.ConnectedClients.Count < 2)
            {
                int currentCount = NetworkManager.Singleton.ConnectedClients.Count;
                
                // UI에 대기 상태 표시
                BroadcastStatusMessageClientRpc($"플레이어 대기 중... ({currentCount}/2)");
                
                yield return new WaitForSeconds(1f);
            }

            // 10초 카운트다운
            BroadcastStatusMessageClientRpc("2명 접속 완료!");
            yield return new WaitForSeconds(1f);
            
            for (int i = 10; i > 0; i--)
            {
                
                // UI에 카운트다운 표시
                BroadcastStatusMessageClientRpc($"게임 시작까지 {i}초");
                
                yield return new WaitForSeconds(1f);
            }

            // 게임 시작
            BroadcastStatusMessageClientRpc("게임 시작!");
            isGameStarted = true;
            waveLoopCoroutine = StartCoroutine(WaveLoop());
        }

        /// <summary>
        /// 오브젝트 풀 미리 생성 (Prewarm)
        /// 투사체와 이펙트를 미리 인스턴스화하여 게임 중 렉 방지
        /// </summary>
        private void PrewarmPools()
        {
            if (gameConfig == null)
            {
                return;
            }

            // 투사체 풀 등록
            var projectileConfig = gameConfig.ProjectileConfig;
            if (projectileConfig != null && projectileConfig.ProjectilePrefab != null)
            {
                // 주의: 프리팹에서는 .NetworkObject가 null이므로 GetComponent 사용
                NetworkObjectPool.Instance.RegisterPrefab(projectileConfig.ProjectilePrefab.GetComponent<NetworkObject>(), projectileConfig.PoolSize);
            }

            // 이펙트 풀 등록
            var effectConfig = gameConfig.HitEffectConfig;
            if (effectConfig != null && effectConfig.EffectPrefab != null)
            {
                NetworkObjectPool.Instance.RegisterPrefab(effectConfig.EffectPrefab.GetComponent<NetworkObject>(), effectConfig.PoolSize);
            }
        }

        /// <summary>
        /// 웨이브 루프 코루틴
        /// 설정된 모든 웨이브를 순차적으로 진행
        /// </summary>
        private IEnumerator WaveLoop()
        {
            // 설정 검증
            if (gameConfig == null || gameConfig.WaveConfig == null)
            {
                yield break;
            }

            var waves = gameConfig.WaveConfig.Waves;
            
            // 모든 웨이브 순회
            for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
            {
                // 게임 오버면 중단
                if (isGameOver)
                {
                    yield break;
                }

                int waveNumber = currentWaveIndex + 1;
                var currentWave = waves[currentWaveIndex];
                
                // 웨이브 UI 변수 초기화
                currentWaveTotalEnemies = currentWave.enemyCount;
                killedEnemiesInWave = 0;
                
                // 웨이브 시작 카운트다운 (5초)
                for (int countdown = 5; countdown > 0; countdown--)
                {
                    BroadcastStatusMessageClientRpc($"웨이브 {waveNumber} {countdown}초 후 시작!");
                    yield return new WaitForSeconds(1f);
                }
                
                // 웨이브 시작 메시지 및 이벤트 방송
                BroadcastStatusMessageClientRpc($"웨이브 {waveNumber} 시작!");
                waveStartedEvent?.Raise(waveNumber);
                RaiseWaveStartedClientRpc(waveNumber);
                
                // 초기 남은 적 수 표시 (=총 적 수)
                BroadcastEnemyCountClientRpc(currentWaveTotalEnemies);
                
                // 웨이브 스폰 (적 생성)
                yield return StartCoroutine(SpawnWave(currentWave));
                
                // 웨이브 클리어 대기 (모든 적 처치)
                yield return StartCoroutine(WaitForWaveClear());
                
                // 웨이브 클리어 시 다운된 플레이어 부활
                ReviveAllDownedPlayers();
                
                // 남은 적 수 0으로 업데이트
                BroadcastEnemyCountClientRpc(0);
                
                // 클리어 메시지 전송 (NetworkGameManager에서 직접 관리)
                BroadcastStatusMessageClientRpc($"웨이브 {waveNumber} 클리어!");
                
                // 웨이브 완료 이벤트 방송 (UI에서는 추가 처리 안 함)
                waveCompletedEvent?.Raise();
                RaiseWaveCompletedClientRpc();

                // 다음 웨이브 전 대기 시간
                yield return new WaitForSeconds(gameConfig.WaveConfig.TimeBetweenWaves);
            }
            
            // 모든 웨이브 클리어 = 승리!
            if (!isGameOver)
            {
                HandleVictory();
            }
        }

        /// <summary>
        /// 웨이브 스폰 코루틴
        /// 적이 준비 완료되면 다음 적을 스폰하는 이벤트 기반 방식
        /// </summary>
        /// <param name="wave">웨이브 정의</param>
        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            // EnemySpawner 검증
            if (enemySpawner == null)
            {
                yield break;
            }

            // 설정된 적 수만큼 스폰
            for (int i = 0; i < wave.enemyCount; i++)
            {
                // 게임 오버면 중단
                if (isGameOver)
                {
                    yield break;
                }
                
                // EnemySpawner를 통해 적 스폰
                enemySpawner.SpawnEnemy();
                
                // 적이 준비 완료될 때까지 대기 (이벤트 기반)
                while (enemySpawner.IsWaitingForEnemyReady && !isGameOver)
                {
                    yield return null;
                }
                
                // 스폰 간격만큼 추가 대기 (선택적)
                if (wave.spawnInterval > 0)
                {
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }
        }

        /// <summary>
        /// 적이 준비 완료되었을 때 호출됨 (NetworkEnemy에서 호출)
        /// EnemySpawner로 위임
        /// </summary>
        public void OnEnemyReady()
        {
            enemySpawner?.OnEnemyReady();
        }

        /// <summary>
        /// 웨이브 클리어 대기 코루틴
        /// 모든 적이 죽을 때까지 대기
        /// </summary>
        private IEnumerator WaitForWaveClear()
        {
            // 모든 적이 처치될 때까지 대기 (처치 수 기반)
            while (killedEnemiesInWave < currentWaveTotalEnemies)
            {
                // null이거나 디스폰된 적 정리 (EnemySpawner에서 처리)
                enemySpawner?.CleanupDeadEnemies();
                yield return null;
            }
        }

        /// <summary>
        /// 모든 다운된 플레이어를 부활시킵니다.
        /// 웨이브 클리어 시 살아있는 플레이어가 1명이라도 있으면 호출됩니다.
        /// </summary>
        private void ReviveAllDownedPlayers()
        {
            if (!IsServer)
            {
                return;
            }

            // 모든 플레이어 찾기
            var players = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            
            // 살아있는 플레이어가 있는지 확인
            bool hasAlivePlayer = false;
            foreach (var player in players)
            {
                if (player.TryGetComponent<NetworkHealth>(out var health) && !health.IsDowned.Value)
                {
                    hasAlivePlayer = true;
                    break;
                }
            }

            // 살아있는 플레이어가 없으면 부활 안 함 (게임 오버 상태)
            if (!hasAlivePlayer)
            {
                return;
            }

            // 다운된 플레이어 모두 부활
            foreach (var player in players)
            {
                if (player.TryGetComponent<NetworkHealth>(out var health) && health.IsDowned.Value)
                {
                    health.Revive();
                }
            }
        }

        /// <summary>
        /// 적 사망 등록
        /// 적이 죽을 때 호출하여 목록에서 제거
        /// </summary>
        /// <param name="enemy">사망한 적</param>
        public void RegisterEnemyDeath(NetworkEnemy enemy)
        {
            // EnemySpawner에서 적 목록 관리
            enemySpawner?.RegisterEnemyDeath(enemy);
            
            // 처치 수 증가
            killedEnemiesInWave++;
            
            // 남은 적 수 = 총 적 수 - 처치한 적 수
            int remainingEnemies = currentWaveTotalEnemies - killedEnemiesInWave;
            BroadcastEnemyCountClientRpc(remainingEnemies);
        }

        /// <summary>
        /// 게임 오버 체크
        /// 모든 플레이어가 다운되었는지 확인
        /// </summary>
        public void CheckGameOver()
        {
            // 서버가 아니거나, 이미 게임 오버거나, 아직 시작 안 했으면 무시
            if (!IsServer || isGameOver || !isGameStarted)
            {
                return;
            }

            // 살아있는 플레이어 찾기
            bool anyAlive = false;
            foreach (var player in FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None))
            {
                if (player != null && player.TryGetComponent<NetworkHealth>(out var playerHealth) && !playerHealth.IsDowned.Value)
                {
                    anyAlive = true;
                    break;
                }
            }

            // 모든 플레이어가 다운되었으면 게임 오버
            if (!anyAlive)
            {
                isGameOver = true;
                waitingForRestart = true;
                
                // 패배 메시지 방송 (웨이브 1부터 재시작 안내)
                BroadcastStatusMessageClientRpc($"패배! 웨이브 1부터 다시 시작합니다.\n[재시작] 버튼을 눌러주세요.");
                
                gameOverEvent?.Raise();
                RaiseGameOverClientRpc();
                
                // 재시작 버튼 팝업 표시 요청
                ShowRestartPopupClientRpc(false);  // false = 패배
            }
        }
        // ===== 승리 처리 =====

        /// <summary>
        /// 모든 웨이브를 클리어했을 때 호출 (승리)
        /// </summary>
        private void HandleVictory()
        {
            if (!IsServer) return;
            
            waitingForRestart = true;
            wasVictory = true;  // 승리 플래그 설정
            
            // 다음 난이도 계산 (다음 게임에 적용)
            int nextDifficulty = difficultyLevel + 1;
            
            // 승리 메시지 방송 (난이도 증가 안내)
            BroadcastStatusMessageClientRpc($"승리! 모든 웨이브 클리어!\n난이도 {nextDifficulty}로 증가합니다.\n[재시작] 버튼을 눌러주세요.");
            
            // 승리 이벤트 방송
            gameWinEvent?.Raise();
            RaiseGameWinClientRpc();
            
            // 재시작 버튼 팝업 표시 요청
            ShowRestartPopupClientRpc(true);  // true = 승리
        }

        // ===== 재시작 =====

        /// <summary>
        /// 게임을 재시작합니다. (외부에서 버튼으로 호출)
        /// </summary>
        public void RestartGame()
        {
            if (!IsServer || !waitingForRestart) return;
            
            waitingForRestart = false;
            StartCoroutine(RestartGameRoutine());
        }

        /// <summary>
        /// 서버RPC: 클라이언트에서 재시작 요청
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestRestartServerRpc()
        {
            RestartGame();
        }

        /// <summary>
        /// 게임 재시작 루틴
        /// 모든 상태 초기화하고 다시 시작
        /// </summary>
        private IEnumerator RestartGameRoutine()
        {
            // 기존 웨이브 루프 코루틴 중지 (중복 실행 방지)
            if (waveLoopCoroutine != null)
            {
                StopCoroutine(waveLoopCoroutine);
                waveLoopCoroutine = null;
            }
            
            // 재시작 안내
            BroadcastStatusMessageClientRpc("게임 재시작 중...");
            
            // 팝업 숨기기
            HideRestartPopupClientRpc();
            
            yield return new WaitForSeconds(1f);

            // 1단계: 모든 적 디스폰 (EnemySpawner에서 처리)
            enemySpawner?.DespawnAllEnemies();

            // 2단계: 모든 플레이어 리셋
            foreach (var player in FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None))
            {
                if (player != null)
                {
                    // 체력 리셋
                    if (player.TryGetComponent<NetworkHealth>(out var health))
                    {
                        health.ResetState();
                    }
                    // 위치 리셋
                    player.ResetPosition();
                }
            }

            // 3단계: 게임 상태 완전 리셋
            currentWaveIndex = 0;
            currentWaveTotalEnemies = 0;
            killedEnemiesInWave = 0;
            
            // 난이도 처리: 승리 후 재시작이면 난이도 증가, 패배면 1로 리셋
            if (wasVictory)
            {
                difficultyLevel++;    // 승리 시 난이도 증가
            }
            else
            {
                difficultyLevel = 1;  // 패배 시 난이도 1로 리셋
            }
            
            isGameOver = false;
            wasVictory = false;  // 플래그 리셋
            
            // 4단계: 웨이브 루프 재시작
            BroadcastStatusMessageClientRpc($"난이도 {difficultyLevel} - 게임 시작!");
            yield return new WaitForSeconds(2f);
            
            waveLoopCoroutine = StartCoroutine(WaveLoop());
        }

        /// <summary>히트 이펙트 설정 반환 (외부 접근용)</summary>
        public EffectConfigSO HitEffectConfig => gameConfig != null ? gameConfig.HitEffectConfig : null;

        // ===== ClientRpc 메서드들 =====
        // 서버에서 호출하면 모든 클라이언트에서 실행됨

        /// <summary>
        /// 웨이브 시작 이벤트를 클라이언트들에게 전파
        /// </summary>
        [ClientRpc]
        private void RaiseWaveStartedClientRpc(int waveNumber)
        {
            // 서버는 이미 이벤트를 발생시켰으므로 무시
            if (IsServer)
            {
                return;
            }

            waveStartedEvent?.Raise(waveNumber);
        }

        /// <summary>
        /// 웨이브 완료 이벤트를 클라이언트들에게 전파
        /// </summary>
        [ClientRpc]
        private void RaiseWaveCompletedClientRpc()
        {
            if (IsServer)
            {
                return;
            }

            waveCompletedEvent?.Raise();
        }

        /// <summary>
        /// 게임 오버 이벤트를 클라이언트들에게 전파
        /// </summary>
        [ClientRpc]
        private void RaiseGameOverClientRpc()
        {
            if (IsServer)
            {
                return;
            }

            gameOverEvent?.Raise();
        }

        /// <summary>
        /// 상태 메시지를 모든 클라이언트에게 방송
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        [ClientRpc]
        private void BroadcastStatusMessageClientRpc(string message)
        {
            // 이벤트를 통해 UI 업데이트 (이중 호출 방지: 이벤트만 사용)
            statusMessageEvent?.Raise(message);
        }

        /// <summary>
        /// 남은 적 수를 모든 클라이언트에게 방송
        /// </summary>
        /// <param name="count">남은 적 수</param>
        [ClientRpc]
        private void BroadcastEnemyCountClientRpc(int count)
        {
            // 이벤트를 통해 UI 업데이트
            enemyCountUpdatedEvent?.Raise(count);
        }

        /// <summary>
        /// 게임 승리 이벤트를 클라이언트들에게 전파
        /// </summary>
        [ClientRpc]
        private void RaiseGameWinClientRpc()
        {
            if (IsServer)
            {
                return;
            }

            gameWinEvent?.Raise();
        }

        /// <summary>
        /// 재시작 팝업 표시를 클라이언트들에게 요청
        /// </summary>
        /// <param name="isVictory">승리인 경우 true, 패배인 경우 false</param>
        [ClientRpc]
        private void ShowRestartPopupClientRpc(bool isVictory)
        {
            // NetworkUIManager에 팝업 표시 요청
            NetworkUIManager.Instance?.ShowRestartPopup(isVictory, difficultyLevel);
        }

        /// <summary>
        /// 재시작 팝업 숨기기를 클라이언트들에게 요청
        /// </summary>
        [ClientRpc]
        private void HideRestartPopupClientRpc()
        {
            NetworkUIManager.Instance?.HideRestartPopup();
        }
    }
}
