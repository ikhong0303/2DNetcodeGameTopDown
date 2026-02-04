/// =============================================================================
/// EnemySpawner.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 적 스폰 로직을 담당하는 컴포넌트입니다.
/// - NetworkGameManager에서 분리된 단일 책임 클래스입니다.
/// - 적 생성, 스폰 포인트 관리, 적 목록 추적을 담당합니다.
/// - SRP (단일 책임 원칙) 준수
/// =============================================================================

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 적 스폰 관리자
    /// 적 생성 및 목록 관리를 담당
    /// </summary>
    public class EnemySpawner : NetworkBehaviour
    {
        // ===== 싱글톤 =====
        
        public static EnemySpawner Instance { get; private set; }

        // ===== 인스펙터 설정 =====
        
        [Header("Config")]
        [SerializeField] private EnemyConfigSO enemyConfig;
        [SerializeField] private Transform[] spawnPoints;

        // ===== 상태 변수 =====
        
        private readonly List<NetworkEnemy> aliveEnemies = new();
        private int currentSpawnPointIndex = 0;
        private bool waitingForEnemyReady = false;

        // ===== 이벤트 =====
        
        /// <summary>적 사망 시 호출되는 델리게이트</summary>
        public System.Action<NetworkEnemy> OnEnemyDied;

        // ===== 프로퍼티 =====
        
        /// <summary>현재 살아있는 적 수</summary>
        public int AliveCount => aliveEnemies.Count;
        
        /// <summary>적 준비 대기 중 여부</summary>
        public bool IsWaitingForEnemyReady => waitingForEnemyReady;

        // ===== 라이프사이클 =====

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ===== 공개 메서드 =====

        /// <summary>
        /// 적 1마리 스폰
        /// </summary>
        /// <returns>스폰 성공 여부</returns>
        public bool SpawnEnemy()
        {
            if (!IsServer) return false;
            
            var enemyPrefab = enemyConfig?.EnemyPrefab;
            
            // 유효성 검사
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                waitingForEnemyReady = false;
                return false;
            }

            // 순환 방식으로 스폰 포인트 선택
            Transform spawnPoint = spawnPoints[currentSpawnPointIndex];
            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Length;
            
            // 적 인스턴스 생성
            var enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            
            // 네트워크에 스폰
            enemyInstance.NetworkObject.Spawn(true);
            
            // 생존 적 목록에 추가
            aliveEnemies.Add(enemyInstance);
            
            // 대기 플래그 설정
            waitingForEnemyReady = true;
            
            return true;
        }

        /// <summary>
        /// 적이 준비 완료되었을 때 호출 (NetworkEnemy에서 호출)
        /// </summary>
        public void OnEnemyReady()
        {
            waitingForEnemyReady = false;
        }

        /// <summary>
        /// 적 사망 등록
        /// </summary>
        /// <param name="enemy">사망한 적</param>
        public void RegisterEnemyDeath(NetworkEnemy enemy)
        {
            if (aliveEnemies.Remove(enemy))
            {
                OnEnemyDied?.Invoke(enemy);
            }
        }

        /// <summary>
        /// null이거나 디스폰된 적 정리
        /// </summary>
        public void CleanupDeadEnemies()
        {
            aliveEnemies.RemoveAll(enemy => enemy == null || !enemy.NetworkObject.IsSpawned);
        }

        /// <summary>
        /// 모든 적 디스폰 (게임 재시작 시)
        /// </summary>
        public void DespawnAllEnemies()
        {
            foreach (var enemy in aliveEnemies)
            {
                if (enemy != null && enemy.NetworkObject.IsSpawned)
                {
                    enemy.NetworkObject.Despawn();
                }
            }
            aliveEnemies.Clear();
        }

        /// <summary>
        /// 스폰 상태 리셋 (웨이브 시작 시)
        /// </summary>
        public void ResetSpawnState()
        {
            currentSpawnPointIndex = 0;
            waitingForEnemyReady = false;
        }
    }
}
