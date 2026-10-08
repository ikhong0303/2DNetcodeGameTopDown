using System.Collections.Generic;
using UnityEngine;

namespace TopDownShooter.Networking
{
    /// <summary>Owner-only cosmetic bullets: no NetworkObject, physics, or damage authority.</summary>
    public sealed class ProjectilePredictionPool
    {
        private sealed class View
        {
            public GameObject Object;
            public Vector3 Origin;
            public Vector3 Velocity;
            public float Started;
            public float Lifetime;
            public uint Sequence;
        }
        private readonly Dictionary<uint, View> active = new();
        private readonly Queue<View> free = new();
        private readonly List<View> all = new();
        private readonly List<uint> expired = new();
        private readonly SpriteRenderer source;

        public ProjectilePredictionPool(NetworkProjectile prefab, int prewarm)
        {
            source = prefab.GetComponent<SpriteRenderer>();
            for (int i = 0; i < prewarm; i++) free.Enqueue(Create());
        }

        private View Create()
        {
            var go = new GameObject("Predicted bullet (visual only)");
            var renderer = go.AddComponent<SpriteRenderer>();
            if (source != null)
            {
                renderer.sprite = source.sprite;
                renderer.color = source.color;
                renderer.sharedMaterial = source.sharedMaterial;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder;
                renderer.flipX = source.flipX;
                renderer.flipY = source.flipY;
                go.transform.localScale = source.transform.localScale;
            }
            go.SetActive(false);
            var view = new View { Object = go };
            all.Add(view);
            return view;
        }

        public void Predict(uint sequence, Vector3 origin, Vector2 direction, float speed, float lifetime)
        {
            Release(sequence);
            var view = free.Count > 0 ? free.Dequeue() : Create();
            view.Origin = origin;
            view.Velocity = direction * speed;
            view.Started = Time.time;
            view.Lifetime = lifetime;
            view.Sequence = sequence;
            view.Object.transform.position = origin;
            view.Object.SetActive(true);
            active.Add(sequence, view);
        }

        public bool Consume(uint sequence, out Vector3 position, out float age)
        {
            position = default;
            age = 0;
            if (!active.TryGetValue(sequence, out var view)) return false;
            age = Time.time - view.Started;
            position = view.Origin + view.Velocity * age;
            Release(sequence);
            return true;
        }

        public void Release(uint sequence)
        {
            if (!active.Remove(sequence, out var view)) return;
            view.Object.SetActive(false);
            free.Enqueue(view);
        }

        public void Tick()
        {
            expired.Clear();
            foreach (var entry in active)
            {
                var view = entry.Value;
                float age = Time.time - view.Started;
                if (age >= view.Lifetime) expired.Add(entry.Key);
                else view.Object.transform.position = view.Origin + view.Velocity * age;
            }
            foreach (var sequence in expired) Release(sequence);
        }

        public void Dispose()
        {
            foreach (var view in all) if (view.Object != null) Object.Destroy(view.Object);
            active.Clear(); free.Clear(); all.Clear();
        }
    }
}
