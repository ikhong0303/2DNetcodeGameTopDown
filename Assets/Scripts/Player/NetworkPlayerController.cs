/// =============================================================================
/// NetworkPlayerController.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크 멀티플레이어 환경에서 플레이어 캐릭터를 제어하는 핵심 컴포넌트입니다.
/// - Input System을 사용하여 이동(WASD), 조준(마우스), 공격, 상호작용 입력을 처리합니다.
/// - 오직 로컬 플레이어(IsOwner)만 입력을 받고 처리합니다.
/// - 투사체 발사는 ServerRpc를 통해 서버에서 처리됩니다.
/// - 부활 시스템: 다운된 플레이어를 상호작용 키로 부활시킬 수 있습니다.
/// - Cinemachine 카메라를 로컬 플레이어에게만 활성화합니다.
/// - 걷기/발사 시 사운드 매니저를 통해 효과음을 재생합니다.
/// - 점수 시스템을 관리하며 NetworkVariable로 동기화됩니다.
/// =============================================================================

using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;
using TopDownShooter.Managers;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 네트워크 플레이어 컨트롤러
    /// Rigidbody2D 필수 (물리 이동용)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        // ===== 입력 처리기 (SRP 분리) =====
        
        [Header("Input")]
        [SerializeField] private PlayerInputHandler inputHandler;  // 입력 처리 컴포넌트

        // ===== 이동 설정 =====
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;     // 이동 속도
        [SerializeField] private float stepRate = 0.35f;   // 발소리 간격

        // ===== 전투 설정 =====
        
        [Header("Combat")]
        [SerializeField] private Transform firePoint;               // 투사체 발사 위치
        [SerializeField] private float fireRate = 0.2f;             // 발사 간격 (초)
        [SerializeField] private ProjectileConfigSO projectileConfig;  // 투사체 설정


        // ===== 카메라 설정 =====
        
        [Header("Camera")]
        [SerializeField] private CinemachineCamera playerCamera;    // 플레이어 카메라

        // ===== 이벤트 =====
        
        [Header("Events")]
        [SerializeField] private GameEventChannelSO playerDownedEvent;  // 다운 이벤트

        // ===== 네트워크 동기화 변수 =====
        
        /// <summary>플레이어 점수 (네트워크 동기화)</summary>
        private readonly NetworkVariable<int> score = new(0);

        // ===== 컴포넌트 캐시 =====
        
        private Rigidbody2D body;              // 물리 컴포넌트
        private NetworkHealth health;          // 체력 컴포넌트
        
        // ===== 상태 변수 =====
        
        private float lastFireTime;            // 마지막 발사 시간
        private float lastStepTime;            // 마지막 발소리 시간

        /// <summary>점수 프로퍼티 (읽기 전용)</summary>
        public NetworkVariable<int> Score => score;

        /// <summary>
        /// 네트워크 스폰 시 호출
        /// 컴포넌트 초기화 및 입력 활성화
        /// </summary>
        public override void OnNetworkSpawn()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<NetworkHealth>();
            
            // InputHandler 검증
            if (inputHandler == null)
            {
                inputHandler = GetComponent<PlayerInputHandler>();
            }

            // 카메라 설정
            ConfigureCamera();

            // 소유자(로컬 플레이어)만 입력 활성화
            if (IsOwner)
            {
                EnableInput(true);
            }
        }

        /// <summary>
        /// 네트워크 디스폰 시 호출
        /// 입력 비활성화
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                EnableInput(false);
            }
        }

        /// <summary>
        /// OnEnable: 오브젝트 활성화 시
        /// 이미 스폰되어 있고 소유자면 입력 활성화
        /// </summary>
        private void OnEnable()
        {
            if (IsSpawned && IsOwner && inputHandler != null && !inputHandler.IsInputEnabled)
            {
                EnableInput(true);
            }
        }

        /// <summary>
        /// OnDisable: 오브젝트 비활성화 시
        /// 입력 비활성화
        /// </summary>
        private void OnDisable()
        {
            if (inputHandler != null && inputHandler.IsInputEnabled)
            {
                EnableInput(false);
            }
        }

        /// <summary>
        /// 입력 시스템 활성화/비활성화 (PlayerInputHandler에 위임)
        /// </summary>
        /// <param name="enable">활성화 여부</param>
        private void EnableInput(bool enable)
        {
            if (inputHandler == null) return;
            
            if (enable)
            {
                inputHandler.EnableInput(true, OwnerClientId);
                inputHandler.OnAttackPressed += HandleAttack;
            }
            else
            {
                inputHandler.OnAttackPressed -= HandleAttack;
                inputHandler.EnableInput(false, OwnerClientId);
            }
        }

        /// <summary>
        /// 카메라 설정
        /// 로컬 플레이어에게만 카메라 활성화
        /// </summary>
        private void ConfigureCamera()
        {
            // 자식에서 카메라 컴포넌트 찾기
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<CinemachineCamera>();
            }

            if (playerCamera != null)
            {
                if (IsOwner)
                {
                    // 로컬 플레이어: 카메라가 이 플레이어를 따라감
                    playerCamera.Follow = transform;
                    playerCamera.LookAt = transform;
                    playerCamera.gameObject.SetActive(true);
                }
                else
                {
                    // 다른 플레이어의 카메라는 비활성화
                    playerCamera.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 공격 입력 콜백 (PlayerInputHandler 이벤트)
        /// </summary>
        private void HandleAttack()
        {
            // 소유자가 아니면 무시
            if (!IsOwner) return;

            // 다운 상태면 공격 불가
            if (health != null && health.IsDowned.Value) return;

            // 발사 쿨다운 체크
            if (Time.time - lastFireTime < fireRate) return;

            // 마우스 조준 방향 계산
            Vector2 aimDirection = CalculateAimDirection();

            lastFireTime = Time.time;

            // 발사 효과음
            SoundManager.Instance?.PlaySfx("Shoot");

            // 서버에 발사 요청
            FireServerRpc(aimDirection);
        }

        /// <summary>
        /// 마우스 위치로 조준 방향 계산
        /// </summary>
        private Vector2 CalculateAimDirection()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null || Mouse.current == null)
            {
                return Vector2.right;
            }

            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = mainCam.nearClipPlane;
            Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
            
            Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
            
            return direction.sqrMagnitude < 0.01f ? Vector2.right : direction;
        }

        /// <summary>
        /// FixedUpdate: 물리 기반 이동 처리
        /// 매 물리 프레임마다 호출
        /// </summary>
        private void FixedUpdate()
        {
            // 소유자만 이동 처리
            if (!IsOwner) return;

            // 다운 상태면 이동 불가
            if (health == null || health.IsDowned.Value)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            // PlayerInputHandler에서 입력값 가져오기
            Vector2 moveInput = inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
            
            // 이동 처리
            body.linearVelocity = moveInput * moveSpeed;

            // 이동 중일 때 발소리 효과음
            if (moveInput.sqrMagnitude > 0.01f && Time.time - lastStepTime > stepRate)
            {
                lastStepTime = Time.time;
                SoundManager.Instance?.PlaySfx("Walk");
            }
        }

        /// <summary>
        /// 투사체 발사 (서버에서 실행)
        /// [ServerRpc]: 클라이언트에서 호출하면 서버에서 실행됨
        /// </summary>
        /// <param name="direction">발사 방향</param>
        [ServerRpc]
        private void FireServerRpc(Vector2 direction, ServerRpcParams rpcParams = default)
        {
            // 설정 검증
            if (projectileConfig == null || firePoint == null)
            {
                return;
            }

            var projectilePrefab = projectileConfig.ProjectilePrefab;
            if (projectilePrefab == null)
            {
                return;
            }

            // 프리팹에서 NetworkObject 가져오기 (프리팹에서는 .NetworkObject가 null)
            var prefabNetworkObject = projectilePrefab.GetComponent<NetworkObject>();
            if (prefabNetworkObject == null)
            {
                return;
            }

            // 오브젝트 풀에서 투사체 스폰
            var projectileObject = NetworkObjectPool.Instance.Spawn(prefabNetworkObject, firePoint.position, Quaternion.identity);
            
            if (projectileObject == null)
            {
                return;
            }

            // 투사체 초기화
            var projectile = projectileObject.GetComponent<NetworkProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(direction, projectileConfig.Speed, projectileConfig.Damage, projectileConfig.Lifetime, OwnerClientId);
            }
            else
            {
            }
        }

        // ===== 부활 기능 제거됨 =====
        // 웨이브 클리어 시 NetworkGameManager에서 자동 부활 처리

        /// <summary>
        /// 점수 추가 (서버에서만 호출)
        /// </summary>
        /// <param name="amount">추가할 점수</param>
        public void AddScore(int amount)
        {
            if (!IsServer)
            {
                return;
            }

            score.Value += amount;
        }

        /// <summary>
        /// 다운 상태 알림
        /// 다운 이벤트 방송
        /// </summary>
        public void NotifyDowned()
        {
            playerDownedEvent?.Raise();
        }

        /// <summary>
        /// 위치 리셋 (게임 재시작 시)
        /// </summary>
        public void ResetPosition()
        {
            if (IsServer)
            {
                // 속도 리셋
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }
                
                // 위치 리셋 (원점으로)
                // 참고: ClientNetworkTransform 사용 시 서버와 클라이언트가 충돌할 수 있음
                transform.position = Vector3.zero; 
                
                // 클라이언트에도 위치 리셋 전파
                ResetPositionClientRpc(Vector3.zero);
            }
        }

        /// <summary>
        /// 위치 리셋 (클라이언트에서 실행)
        /// [ClientRpc]: 서버에서 호출하면 모든 클라이언트에서 실행됨
        /// </summary>
        /// <param name="position">리셋할 위치</param>
        [ClientRpc]
        private void ResetPositionClientRpc(Vector3 position)
        {
            // 소유자만 위치 리셋 (ClientNetworkTransform 사용 시)
            if (IsOwner)
            {
                transform.position = position;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }
            }
        }
    }
}
