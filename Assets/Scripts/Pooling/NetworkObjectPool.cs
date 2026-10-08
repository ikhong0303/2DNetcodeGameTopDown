using System;
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

    /// <summary>Prefab handlers reuse network instances on both server and clients.</summary>
    public class NetworkObjectPool : MonoBehaviour
    {
        public static NetworkObjectPool Instance { get; private set; }
        [SerializeField] private bool dontDestroyOnLoad = true;
        private readonly Dictionary<NetworkObject, Queue<NetworkObject>> pools = new();
        private readonly Dictionary<NetworkObject, NetworkObject> originals = new();
        private readonly Dictionary<NetworkObject, IPooledObject[]> callbacks = new();
        private readonly HashSet<NetworkObject> available = new();
        private readonly Dictionary<NetworkObject, Handler> handlers = new();
        private NetworkManager registeredManager;

        private sealed class Handler : INetworkPrefabInstanceHandler
        {
            private readonly NetworkObjectPool pool;
            private readonly NetworkObject prefab;
            public Handler(NetworkObjectPool pool, NetworkObject prefab)
            { this.pool = pool; this.prefab = prefab; }
            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
                => pool.Take(prefab, position, rotation);
            public void Destroy(NetworkObject instance) => pool.Return(instance);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }

        public void RegisterPrefab(NetworkObject prefab, int prewarmCount)
        {
            if (prefab == null) return;
            if (!pools.ContainsKey(prefab))
            {
                pools.Add(prefab, new Queue<NetworkObject>());
                handlers.Add(prefab, new Handler(this, prefab));
                for (int i = 0; i < prewarmCount; i++) Return(Create(prefab));
            }
            var manager = NetworkManager.Singleton;
            if (manager == null) return;
            if (registeredManager != manager)
            {
                if (registeredManager != null)
                    foreach (var entry in handlers) registeredManager.PrefabHandler.RemoveHandler(entry.Key);
                registeredManager = manager;
            }
            foreach (var entry in handlers) manager.PrefabHandler.AddHandler(entry.Key, entry.Value);
        }

        private NetworkObject Create(NetworkObject prefab)
        {
            // Keep network objects at the scene root: parenting during NGO despawn is invalid.
            var instance = Instantiate(prefab);
            if (dontDestroyOnLoad) DontDestroyOnLoad(instance.gameObject);
            instance.gameObject.SetActive(false);
            originals.Add(instance, prefab);
            var list = new List<IPooledObject>();
            foreach (var component in instance.GetComponents<MonoBehaviour>())
                if (component is IPooledObject pooled) list.Add(pooled);
            callbacks.Add(instance, list.ToArray());
            return instance;
        }

        private NetworkObject Take(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            NetworkObject instance = null;
            var queue = pools[prefab];
            while (queue.Count > 0 && instance == null) instance = queue.Dequeue();
            if (instance == null) instance = Create(prefab);
            available.Remove(instance);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            foreach (var callback in callbacks[instance]) callback.OnSpawned();
            return instance;
        }

        public NetworkObject Spawn(NetworkObject prefab, Vector3 position, Quaternion rotation,
            Action<NetworkObject> initialize = null)
        {
            if (prefab == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return null;
            RegisterPrefab(prefab, 0);
            var instance = Take(prefab, position, rotation);
            try
            {
                // Set initial state before NGO serializes the spawn message.
                initialize?.Invoke(instance);
                instance.Spawn(true);
                return instance;
            }
            catch
            {
                if (!instance.IsSpawned) Return(instance);
                throw;
            }
        }

        public void Despawn(NetworkObject instance)
        {
            if (instance == null || !instance.IsSpawned || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            // true dispatches our handler on each peer instead of destroying the GameObject.
            instance.Despawn(true);
            Return(instance);
        }

        private void Return(NetworkObject instance)
        {
            if (instance == null || !originals.TryGetValue(instance, out var prefab) || !available.Add(instance)) return;
            foreach (var callback in callbacks[instance]) callback.OnDespawned();
            instance.gameObject.SetActive(false);
            pools[prefab].Enqueue(instance);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (registeredManager != null)
                foreach (var entry in handlers) registeredManager.PrefabHandler.RemoveHandler(entry.Key);
            foreach (var entry in originals)
                if (entry.Key != null) Destroy(entry.Key.gameObject);
            Instance = null;
        }
    }
}
