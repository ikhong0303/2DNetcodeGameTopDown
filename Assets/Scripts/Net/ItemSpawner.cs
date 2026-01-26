using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public class ItemSpawner : NetworkBehaviour
    {
        public static ItemSpawner Instance { get; private set; }

        [Header("Item Prefabs")]
        [SerializeField] private NetworkObject healthPotionPrefab;
        [SerializeField] private NetworkObject speedBoostPrefab;
        [SerializeField] private NetworkObject damageBoostPrefab;
        [SerializeField] private NetworkObject fireRateBoostPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public static void SpawnRandomItem(Vector3 position)
        {
            if (Instance == null)
            {
                Debug.LogWarning("ItemSpawner instance not found!");
                return;
            }

            Instance.SpawnRandomItemInternal(position);
        }

        private void SpawnRandomItemInternal(Vector3 position)
        {
            if (!IsServer)
            {
                Debug.LogWarning("SpawnRandomItem should only be called on server!");
                return;
            }

            NetworkObject[] itemPrefabs = {
                healthPotionPrefab,
                speedBoostPrefab,
                damageBoostPrefab,
                fireRateBoostPrefab
            };

            NetworkObject selectedPrefab = null;
            int attempts = 0;
            while (selectedPrefab == null && attempts < 10)
            {
                var candidate = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
                if (candidate != null)
                {
                    selectedPrefab = candidate;
                }
                attempts++;
            }

            if (selectedPrefab == null)
            {
                Debug.LogWarning("No valid item prefab found!");
                return;
            }

            NetworkObject itemObj = Instantiate(selectedPrefab, position, Quaternion.identity);
            itemObj.Spawn(true);
        }

        public void SpawnSpecificItem(ItemType itemType, Vector3 position)
        {
            if (!IsServer) return;

            NetworkObject prefab = itemType switch
            {
                ItemType.HealthPotion => healthPotionPrefab,
                ItemType.SpeedBoost => speedBoostPrefab,
                ItemType.DamageBoost => damageBoostPrefab,
                ItemType.FireRateBoost => fireRateBoostPrefab,
                _ => null
            };

            if (prefab == null)
            {
                Debug.LogWarning($"Item prefab for {itemType} not found!");
                return;
            }

            NetworkObject itemObj = Instantiate(prefab, position, Quaternion.identity);
            itemObj.Spawn(true);
        }
    }
}
