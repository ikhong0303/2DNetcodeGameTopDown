using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TopDownShooter.Pooling;

namespace TopDownShooter.Networking
{
    public class NetworkEffect : NetworkBehaviour, IPooledObject
    {
        private Coroutine lifeRoutine;

        public void Play(float lifetime)
        {
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
            }

            lifeRoutine = StartCoroutine(DespawnAfter(lifetime));
        }

        private IEnumerator DespawnAfter(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            NetworkObjectPool.Instance.Despawn(NetworkObject);
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
                lifeRoutine = null;
            }
        }
    }
}
