using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TopDownShooter.Pooling
{
    public interface IPooledObject
    {
        void OnSpawned();
        void OnDespawned();
    }

    public class NetworkObjectPool : MonoBehaviour
    {
        private static NetworkObjectPool instance;

        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly Dictionary<NetworkObject, Queue<NetworkObject>> poolLookup = new();
        private readonly Dictionary<NetworkObject, NetworkObject> prefabLookup = new();

        public static NetworkObjectPool Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public void RegisterPrefab(NetworkObject prefab, int prewarmCount)
        {
            if (prefab == null || poolLookup.ContainsKey(prefab))
            {
                return;
            }

            var queue = new Queue<NetworkObject>();
            poolLookup.Add(prefab, queue);

            for (var i = 0; i < prewarmCount; i++)
            {
                var instance = Instantiate(prefab);
                instance.gameObject.SetActive(false);
                queue.Enqueue(instance);
                prefabLookup.Add(instance, prefab);
            }
        }

        public NetworkObject Spawn(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[NetworkObjectPool] Spawn failed: prefab is null! Check your ProjectileConfigSO asset.");
                return null;
            }

            if (!poolLookup.TryGetValue(prefab, out var queue))
            {
                RegisterPrefab(prefab, 0);
                queue = poolLookup[prefab];
            }

            NetworkObject instance = null;
            while (queue.Count > 0)
            {
                instance = queue.Dequeue();
                if (instance != null)
                {
                    break;
                }
            }

            if (instance == null)
            {
                instance = Instantiate(prefab);
            }

            if (!prefabLookup.ContainsKey(instance))
            {
                prefabLookup.Add(instance, prefab);
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.Spawn(true);

            if (instance.TryGetComponent<IPooledObject>(out var pooledObject))
            {
                pooledObject.OnSpawned();
            }

            return instance;
        }

        public void Despawn(NetworkObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.TryGetComponent<IPooledObject>(out var pooledObject))
            {
                pooledObject.OnDespawned();
            }

            instance.Despawn(true);
            instance.gameObject.SetActive(false);

            if (prefabLookup.TryGetValue(instance, out var prefab))
            {
                poolLookup[prefab].Enqueue(instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }
    }
}
