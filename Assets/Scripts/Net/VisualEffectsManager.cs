using UnityEngine;

namespace IsaacLike.Net
{
    public class VisualEffectsManager : MonoBehaviour
    {
        public static VisualEffectsManager Instance { get; private set; }

        [Header("Particle Prefabs")]
        [SerializeField] private GameObject projectileHitEffect;
        [SerializeField] private GameObject enemyDeathEffect;
        [SerializeField] private GameObject itemPickupEffect;
        [SerializeField] private GameObject playerHitEffect;
        [SerializeField] private GameObject explosionEffect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void PlayProjectileHit(Vector3 position)
        {
            PlayEffect(projectileHitEffect, position);
        }

        public void PlayEnemyDeath(Vector3 position)
        {
            PlayEffect(enemyDeathEffect, position);
        }

        public void PlayItemPickup(Vector3 position)
        {
            PlayEffect(itemPickupEffect, position);
        }

        public void PlayPlayerHit(Vector3 position)
        {
            PlayEffect(playerHitEffect, position);
        }

        public void PlayExplosion(Vector3 position)
        {
            PlayEffect(explosionEffect, position);
        }

        private void PlayEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null)
            {
                return;
            }

            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 2f);
            }
        }

        public void CreateSimpleParticle(Vector3 position, Color color, int count = 10)
        {
            GameObject particleObj = new GameObject("TempParticle");
            particleObj.transform.position = position;

            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startSpeed = 5f;
            main.startSize = 0.2f;
            main.startLifetime = 0.5f;
            main.maxParticles = count;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, count)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ps.Play();
            Destroy(particleObj, 1f);
        }
    }
}
