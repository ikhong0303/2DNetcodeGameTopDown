/// =============================================================================
/// NetworkHealth.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크에서 동기화되는 체력(HP) 시스템을 관리하는 컴포넌트입니다.
/// - 플레이어와 적 모두에게 사용되며, 데미지 처리와 사망/다운 상태를 관리합니다.
/// - NetworkVariable을 사용하여 모든 클라이언트에 체력 상태를 동기화합니다.
/// - HP바 슬라이더가 할당되어 있으면 자동으로 백분율 계산하여 업데이트합니다.
/// - 웨이브 클리어 시 살아있는 플레이어가 있으면 모든 다운된 플레이어가 부활합니다.
/// =============================================================================

using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TopDownShooter.Core;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 동기화 체력 컴포넌트
    /// 플레이어와 적 모두에게 사용 가능
    /// </summary>
    public class NetworkHealth : NetworkBehaviour
    {
        // ===== 인스펙터에서 설정할 필드들 =====
        
        [Header("Health")]
        [SerializeField] private int maxHealth = 5;            // 최대 체력
        
        [Header("Events")]
        [SerializeField] private GameEventChannelSO downedEvent;   // 다운 시 발생하는 이벤트
        [SerializeField] private GameEventChannelSO revivedEvent;  // 부활 시 발생하는 이벤트

        [Header("Health Bar (Optional)")]
        [SerializeField] private Slider healthSlider;          // HP 슬라이더 (할당 시 자동 업데이트)
        [SerializeField] private float sliderMaxValue = 100f;  // 슬라이더 최대값 (기본 100)

        // ===== 네트워크 동기화 변수들 =====
        // NetworkVariable: 서버에서 변경하면 모든 클라이언트에 자동 동기화됨
        
        /// <summary>현재 체력 (네트워크 동기화)</summary>
        public NetworkVariable<int> CurrentHealth { get; } = new();
        
        /// <summary>다운 상태 여부 (네트워크 동기화)</summary>
        public NetworkVariable<bool> IsDowned { get; } = new(false);
        
        /// <summary>최대 체력 (외부에서 접근용)</summary>
        public int MaxHealth => maxHealth;

        /// <summary>
        /// 네트워크 스폰 시 호출
        /// 서버에서 초기 상태 설정, 클라이언트에서 이벤트 구독
        /// </summary>
        public override void OnNetworkSpawn()
        {
            // 서버에서만 초기 체력 설정
            // 주의: 적(NetworkEnemy)은 자체적으로 HP를 설정하므로 여기서는 플레이어만 처리
            if (IsServer && GetComponent<NetworkEnemy>() == null)
            {
                CurrentHealth.Value = maxHealth;  // 최대 체력으로 초기화
                IsDowned.Value = false;           // 다운 상태 해제
            }

            // 클라이언트에서 다운 상태 변경 이벤트 구독
            if (IsClient)
            {
                IsDowned.OnValueChanged += OnDownedChanged;
            }
            
            // HP 변경 콜백 구독 (모든 클라이언트에서 HP바 업데이트용)
            CurrentHealth.OnValueChanged += OnHealthChanged;
            
            // 초기 HP바 설정
            UpdateHealthBar(CurrentHealth.Value);
        }

        /// <summary>
        /// 네트워크 디스폰 시 호출
        /// 이벤트 구독 해제로 메모리 누수 방지
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                IsDowned.OnValueChanged -= OnDownedChanged;
            }
            
            // HP 변경 콜백 구독 해제
            CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        /// <summary>
        /// 다운 상태가 변경될 때 호출되는 콜백
        /// 적절한 이벤트를 방송합니다.
        /// </summary>
        private void OnDownedChanged(bool previous, bool current)
        {
            if (current)
            {
                // 다운 상태가 됨 -> 다운 이벤트 방송
                downedEvent?.Raise();
            }
            else
            {
                // 다운 상태에서 회복됨 -> 부활 이벤트 방송
                revivedEvent?.Raise();
            }
        }

        /// <summary>
        /// 데미지를 적용합니다.
        /// 서버에서만 호출 가능
        /// </summary>
        public void ApplyDamage(int amount)
        {
            // 서버가 아니거나 이미 다운 상태면 무시
            if (!IsServer || IsDowned.Value)
            {
                return;
            }

            // 체력 감소 (0 이하로 내려가지 않도록)
            CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

            // 체력이 0이면 다운 상태로 전환
            if (CurrentHealth.Value == 0)
            {
                EnterDownedState();
            }
        }

        /// <summary>
        /// 다운 상태로 진입합니다.
        /// 플레이어의 경우 게임 오버 체크를 트리거합니다.
        /// </summary>
        private void EnterDownedState()
        {
            IsDowned.Value = true;
            
            // 서버에서도 이벤트 방송 (순수 서버인 경우)
            if (!IsClient)
            {
                downedEvent?.Raise();
            }

            // 플레이어인 경우 추가 처리
            if (TryGetComponent<NetworkPlayerController>(out var player))
            {
                player.NotifyDowned();                        // 플레이어에게 다운 알림
                NetworkGameManager.Instance?.CheckGameOver(); // 게임 오버 체크
            }
        }

        /// <summary>
        /// 부활시킵니다. (서버에서만 호출)
        /// 웨이브 클리어 시 NetworkGameManager에서 호출됩니다.
        /// </summary>
        public void Revive()
        {
            if (!IsServer || !IsDowned.Value)
            {
                return;
            }

            CurrentHealth.Value = maxHealth;  // 체력 최대치로 복구
            IsDowned.Value = false;           // 다운 상태 해제
            
            // 서버에서도 부활 이벤트 방송
            if (!IsClient)
            {
                revivedEvent?.Raise();
            }
        }

        /// <summary>
        /// 상태를 초기화합니다.
        /// 게임 재시작 시 호출됩니다.
        /// </summary>
        public void ResetState()
        {
            // 서버에서만 실행
            if (!IsServer)
            {
                return;
            }

            CurrentHealth.Value = maxHealth;    // 체력 최대치로 복구
            IsDowned.Value = false;             // 다운 상태 해제
        }

        // ===== HP바 =====

        /// <summary>
        /// HP 변경 콜백 (NetworkVariable 자동 호출)
        /// 모든 클라이언트에서 자동 동기화됨
        /// </summary>
        private void OnHealthChanged(int oldValue, int newValue)
        {
            UpdateHealthBar(newValue);
        }

        /// <summary>
        /// HP바 슬라이더 업데이트 (백분율 계산)
        /// 슬라이더가 할당되지 않았으면 무시됨
        /// </summary>
        /// <param name="currentHealth">현재 HP</param>
        private void UpdateHealthBar(int currentHealth)
        {
            // 슬라이더가 없으면 무시 (선택적 기능)
            if (healthSlider == null) return;
            if (maxHealth <= 0) return;

            // 백분율 계산: (현재HP / 최대HP) * 슬라이더최대값
            float percentage = (float)currentHealth / maxHealth * sliderMaxValue;
            healthSlider.value = percentage;
        }
    }
}
