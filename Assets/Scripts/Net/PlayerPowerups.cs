using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public class PlayerPowerups : NetworkBehaviour
    {
        public NetworkVariable<float> SpeedMultiplier { get; private set; }
        public NetworkVariable<float> DamageMultiplier { get; private set; }
        public NetworkVariable<float> FireRateMultiplier { get; private set; }

        private float _speedBoostEndTime;
        private float _damageBoostEndTime;
        private float _fireRateBoostEndTime;

        private void Awake()
        {
            SpeedMultiplier = new NetworkVariable<float>(
                1f,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            DamageMultiplier = new NetworkVariable<float>(
                1f,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            FireRateMultiplier = new NetworkVariable<float>(
                1f,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        private void Update()
        {
            if (!IsServer) return;

            if (Time.time >= _speedBoostEndTime && SpeedMultiplier.Value != 1f)
            {
                SpeedMultiplier.Value = 1f;
                Debug.Log("Speed boost expired");
            }

            if (Time.time >= _damageBoostEndTime && DamageMultiplier.Value != 1f)
            {
                DamageMultiplier.Value = 1f;
                Debug.Log("Damage boost expired");
            }

            if (Time.time >= _fireRateBoostEndTime && FireRateMultiplier.Value != 1f)
            {
                FireRateMultiplier.Value = 1f;
                Debug.Log("Fire rate boost expired");
            }
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            if (!IsServer) return;

            SpeedMultiplier.Value = multiplier;
            _speedBoostEndTime = Time.time + duration;
        }

        public void ApplyDamageBoost(float multiplier, float duration)
        {
            if (!IsServer) return;

            DamageMultiplier.Value = multiplier;
            _damageBoostEndTime = Time.time + duration;
        }

        public void ApplyFireRateBoost(float multiplier, float duration)
        {
            if (!IsServer) return;

            FireRateMultiplier.Value = multiplier;
            _fireRateBoostEndTime = Time.time + duration;
        }
    }
}
