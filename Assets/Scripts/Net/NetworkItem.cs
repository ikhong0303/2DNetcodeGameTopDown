using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public enum ItemType
    {
        HealthPotion,
        SpeedBoost,
        DamageBoost,
        FireRateBoost
    }

    [RequireComponent(typeof(NetworkObject))]
    public class NetworkItem : NetworkBehaviour
    {
        [Header("Item Settings")]
        [SerializeField] private ItemType itemType = ItemType.HealthPotion;
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private bool autoDestroy = true;

        [Header("Effects")]
        [SerializeField] private int healthRestore = 2;
        [SerializeField] private float speedMultiplier = 1.5f;
        [SerializeField] private float damageMultiplier = 1.5f;
        [SerializeField] private float fireRateMultiplier = 1.5f;
        [SerializeField] private float powerupDuration = 10f;

        private float _spawnTime;

        private void Start()
        {
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (!IsServer || !autoDestroy) return;

            if (Time.time - _spawnTime >= lifetime)
            {
                NetworkObject.Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsServer) return;

            var playerController = collision.GetComponentInParent<NetworkPlayerController2D>();
            if (playerController == null) return;

            ApplyEffect(playerController.gameObject);
            ShowPickupEffectsClientRpc(transform.position);
            NetworkObject.Despawn();
        }

        [ClientRpc]
        private void ShowPickupEffectsClientRpc(Vector3 position)
        {
            if (VisualEffectsManager.Instance != null)
            {
                VisualEffectsManager.Instance.PlayItemPickup(position);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("item_pickup");
            }
        }

        private void ApplyEffect(GameObject player)
        {
            switch (itemType)
            {
                case ItemType.HealthPotion:
                    var health = player.GetComponent<NetworkHealth>();
                    if (health != null)
                    {
                        health.Heal(healthRestore);
                        Debug.Log($"Player healed for {healthRestore} HP");
                    }
                    break;

                case ItemType.SpeedBoost:
                    var speedBoost = player.GetComponent<PlayerPowerups>();
                    if (speedBoost == null)
                    {
                        speedBoost = player.AddComponent<PlayerPowerups>();
                    }
                    speedBoost.ApplySpeedBoost(speedMultiplier, powerupDuration);
                    Debug.Log($"Speed boost applied: {speedMultiplier}x for {powerupDuration}s");
                    break;

                case ItemType.DamageBoost:
                    var damageBoost = player.GetComponent<PlayerPowerups>();
                    if (damageBoost == null)
                    {
                        damageBoost = player.AddComponent<PlayerPowerups>();
                    }
                    damageBoost.ApplyDamageBoost(damageMultiplier, powerupDuration);
                    Debug.Log($"Damage boost applied: {damageMultiplier}x for {powerupDuration}s");
                    break;

                case ItemType.FireRateBoost:
                    var fireRateBoost = player.GetComponent<PlayerPowerups>();
                    if (fireRateBoost == null)
                    {
                        fireRateBoost = player.AddComponent<PlayerPowerups>();
                    }
                    fireRateBoost.ApplyFireRateBoost(fireRateMultiplier, powerupDuration);
                    Debug.Log($"Fire rate boost applied: {fireRateMultiplier}x for {powerupDuration}s");
                    break;
            }
        }
    }
}
