using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsaacLike.Net
{
    public class SessionManagerUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button shutdownButton;
        [SerializeField] private TMP_Text statusText;

        private void Awake()
        {
            if (hostButton != null)
            {
                hostButton.onClick.AddListener(OnClickHost);
            }

            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnClickJoin);
            }

            if (shutdownButton != null)
            {
                shutdownButton.onClick.AddListener(OnClickShutdown);
            }

            SetStatus("Ready.");
        }

        private void Start()
        {
            ValidateBindings();
        }

        private async void OnClickHost()
        {
            try
            {
                if (SessionConnector.Instance == null)
                {
                    SetStatus("SessionConnector is missing in the scene.");
                    return;
                }

                string code = GetRoomCodeOrDefault();
                SetStatus($"Starting Host... (RoomCode: {code})");

                await SessionConnector.Instance.StartHostAsync(code);

                SetStatus("Host started.");
            }
            catch (Exception e)
            {
                SetStatus($"Host failed: {e.Message}");
            }
        }

        private async void OnClickJoin()
        {
            try
            {
                if (SessionConnector.Instance == null)
                {
                    SetStatus("SessionConnector is missing in the scene.");
                    return;
                }

                string code = GetRoomCodeOrDefault();
                SetStatus($"Joining... (RoomCode: {code})");

                await SessionConnector.Instance.JoinAsync(code);

                SetStatus("Client started.");
            }
            catch (Exception e)
            {
                SetStatus($"Join failed: {e.Message}");
            }
        }

        private void OnClickShutdown()
        {
            if (SessionConnector.Instance == null)
            {
                return;
            }

            SessionConnector.Instance.Shutdown();
            SetStatus("Shutdown.");
        }

        private string GetRoomCodeOrDefault()
        {
            string code = roomCodeInput != null ? roomCodeInput.text : string.Empty;
            code = code?.Trim();

            if (string.IsNullOrEmpty(code))
            {
                code = "ROOM001";
            }

            return code;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
            {
                statusText.text = msg;
            }

            Debug.Log(msg);
        }

        private void ValidateBindings()
        {
            bool hasMissingBinding = false;

            if (roomCodeInput == null)
            {
                hasMissingBinding = true;
                Debug.LogWarning("[SessionManagerUI] RoomCode input is not assigned.");
            }

            if (hostButton == null)
            {
                hasMissingBinding = true;
                Debug.LogWarning("[SessionManagerUI] Host button is not assigned.");
            }

            if (joinButton == null)
            {
                hasMissingBinding = true;
                Debug.LogWarning("[SessionManagerUI] Join button is not assigned.");
            }

            if (statusText == null)
            {
                hasMissingBinding = true;
                Debug.LogWarning("[SessionManagerUI] Status text is not assigned.");
            }

            bool hasSessionConnector = FindObjectOfType<SessionConnector>(true) != null;
            if (!hasSessionConnector)
            {
                hasMissingBinding = true;
                Debug.LogWarning("[SessionManagerUI] SessionConnector is missing in the scene.");
            }

            if (hasMissingBinding)
            {
                SetStatus("Missing UI bindings or SessionConnector. Check Inspector.");
            }
        }

        
    }
}
