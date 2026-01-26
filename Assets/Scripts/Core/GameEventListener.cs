using UnityEngine;
using UnityEngine.Events;

namespace TopDownShooter.Core
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEventChannelSO channel;
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            if (channel != null)
            {
                channel.EventRaised += OnEventRaised;
                DevLogger.Log(nameof(GameEventListener), $"Subscribed to {channel.name}.", channel);
            }
        }

        private void OnDisable()
        {
            if (channel != null)
            {
                channel.EventRaised -= OnEventRaised;
                DevLogger.Log(nameof(GameEventListener), $"Unsubscribed from {channel.name}.", channel);
            }
        }

        private void OnEventRaised()
        {
            DevLogger.Log(nameof(GameEventListener), $"Received event from {channel?.name}.", channel);
            response?.Invoke();
        }
    }
}
