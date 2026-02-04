/// =============================================================================
/// PlayerInputHandler.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 플레이어 입력 처리를 담당하는 컴포넌트입니다.
/// - NetworkPlayerController에서 분리된 단일 책임 클래스입니다.
/// - Input System 액션 구독/해제, 입력값 관리를 담당합니다.
/// - SRP (단일 책임 원칙) 준수
/// =============================================================================

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter.Networking
{
    /// <summary>
    /// 플레이어 입력 처리기
    /// 입력 시스템과의 통신을 담당
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        // ===== 입력 액션 참조 =====
        
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private InputActionReference interactAction;

        // ===== 상태 =====
        
        private bool inputEnabled = false;

        // ===== 입력값 =====
        
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        // ===== 이벤트 =====
        
        /// <summary>공격 입력 시 호출</summary>
        public event Action OnAttackPressed;
        
        /// <summary>상호작용 시작 시 호출</summary>
        public event Action OnInteractStarted;
        
        /// <summary>상호작용 취소 시 호출</summary>
        public event Action OnInteractCanceled;

        // ===== 프로퍼티 =====
        
        public bool IsInputEnabled => inputEnabled;

        // ===== 공개 메서드 =====

        /// <summary>
        /// 입력 시스템 활성화/비활성화
        /// </summary>
        /// <param name="enable">활성화 여부</param>
        /// <param name="clientId">디버그용 클라이언트 ID</param>
        public void EnableInput(bool enable, ulong clientId = 0)
        {
            if (enable == inputEnabled) return;
            inputEnabled = enable;

            // 이동 입력 설정
            SetupAction(moveAction, enable, OnMove, InputActionPhase.Performed | InputActionPhase.Canceled);
            
            // 조준 입력 설정
            SetupAction(lookAction, enable, OnLook, InputActionPhase.Performed | InputActionPhase.Canceled);
            
            // 공격 입력 설정
            SetupAction(attackAction, enable, OnAttackCallback, InputActionPhase.Performed);
            
            // 상호작용 입력 설정
            SetupInteractAction(enable);
        }

        /// <summary>
        /// 입력값 리셋 (다운 상태 등에서 사용)
        /// </summary>
        public void ResetInput()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
        }

        // ===== 내부 메서드 =====

        private void SetupAction(InputActionReference actionRef, bool enable, 
            Action<InputAction.CallbackContext> callback, InputActionPhase phases)
        {
            if (actionRef == null || actionRef.action == null)
            {
                return;
            }

            if (enable)
            {
                actionRef.action.Enable();
                
                if ((phases & InputActionPhase.Performed) != 0)
                    actionRef.action.performed += callback;
                if ((phases & InputActionPhase.Canceled) != 0)
                    actionRef.action.canceled += callback;
            }
            else
            {
                if ((phases & InputActionPhase.Performed) != 0)
                    actionRef.action.performed -= callback;
                if ((phases & InputActionPhase.Canceled) != 0)
                    actionRef.action.canceled -= callback;
            }
        }

        private void SetupInteractAction(bool enable)
        {
            if (interactAction == null || interactAction.action == null) return;

            if (enable)
            {
                interactAction.action.Enable();
                interactAction.action.started += OnInteractStartedCallback;
                interactAction.action.canceled += OnInteractCanceledCallback;
            }
            else
            {
                interactAction.action.started -= OnInteractStartedCallback;
                interactAction.action.canceled -= OnInteractCanceledCallback;
            }
        }

        // ===== 입력 콜백 =====

        private void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }

        private void OnAttackCallback(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnAttackPressed?.Invoke();
            }
        }

        private void OnInteractStartedCallback(InputAction.CallbackContext context)
        {
            OnInteractStarted?.Invoke();
        }

        private void OnInteractCanceledCallback(InputAction.CallbackContext context)
        {
            OnInteractCanceled?.Invoke();
        }

        // ===== Unity 라이프사이클 =====

        private void OnDisable()
        {
            if (inputEnabled)
            {
                EnableInput(false);
            }
        }
    }
}
