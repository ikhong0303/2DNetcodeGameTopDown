using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Core;

namespace TopDownShooter.Networking
{
    public class NetworkHealth : NetworkBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private float reviveDuration = 3f;
        [SerializeField] private int reviveHealth = 3;
        [SerializeField] private float reviveRange = 2f;
        [SerializeField] private GameEventChannelSO downedEvent;
        [SerializeField] private GameEventChannelSO revivedEvent;

        public NetworkVariable<int> CurrentHealth { get; } = new();
        public NetworkVariable<bool> IsDowned { get; } = new(false);

        private Coroutine reviveRoutine;
        private NetworkObject reviver;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
                IsDowned.Value = false;
            }

            if (IsClient)
            {
                IsDowned.OnValueChanged += OnDownedChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                IsDowned.OnValueChanged -= OnDownedChanged;
            }
        }

        private void OnDownedChanged(bool previous, bool current)
        {
            if (current)
            {
                downedEvent?.Raise();
            }
            else
            {
                revivedEvent?.Raise();
            }
        }

        public void ApplyDamage(int amount)
        {
            if (!IsServer || IsDowned.Value)
            {
                return;
            }

            CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

            if (CurrentHealth.Value == 0)
            {
                EnterDownedState();
            }
        }

        private void EnterDownedState()
        {
            IsDowned.Value = true;
            if (!IsClient)
            {
                downedEvent?.Raise();
            }

            if (TryGetComponent<NetworkPlayerController>(out var player))
            {
                player.NotifyDowned();
                NetworkGameManager.Instance?.CheckGameOver();
            }
        }

        public void BeginRevive(NetworkObject reviverObject)
        {
            if (!IsDowned.Value || reviverObject == null)
            {
                return;
            }

            if (reviveRoutine != null)
            {
                return;
            }

            reviver = reviverObject;
            reviveRoutine = StartCoroutine(ReviveRoutine());
        }

        public void CancelRevive()
        {
            if (reviveRoutine == null)
            {
                return;
            }

            StopCoroutine(reviveRoutine);
            reviveRoutine = null;
            reviver = null;
        }

        private IEnumerator ReviveRoutine()
        {
            float elapsed = 0f;

            while (elapsed < reviveDuration)
            {
                if (reviver == null || !IsDowned.Value)
                {
                    CancelRevive();
                    yield break;
                }

                if (Vector3.Distance(reviver.transform.position, transform.position) > reviveRange)
                {
                    CancelRevive();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            CurrentHealth.Value = Mathf.Min(maxHealth, reviveHealth);
            IsDowned.Value = false;
            reviveRoutine = null;
            reviver = null;
            if (!IsClient)
            {
                revivedEvent?.Raise();
            }
        }
    }
}
