using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        [Header("Player Spawn Points")]
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField] private bool randomizePlayerSpawn = true;

        [Header("Enemy Spawn Points")]
        [SerializeField] private Transform[] enemySpawnPoints;

        private int _lastPlayerSpawnIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                Debug.LogWarning("No player spawn points configured! Using Vector3.zero");
                return Vector3.zero;
            }

            if (randomizePlayerSpawn)
            {
                int index = Random.Range(0, playerSpawnPoints.Length);
                return playerSpawnPoints[index].position;
            }
            else
            {
                _lastPlayerSpawnIndex = (_lastPlayerSpawnIndex + 1) % playerSpawnPoints.Length;
                return playerSpawnPoints[_lastPlayerSpawnIndex].position;
            }
        }

        public Vector3 GetSafePlayerSpawnPosition()
        {
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                return Vector3.zero;
            }

            Vector3 bestPosition = playerSpawnPoints[0].position;
            float bestDistance = 0f;

            var enemies = FindObjectsOfType<NetworkEnemyChaser>();

            foreach (var spawnPoint in playerSpawnPoints)
            {
                float minDistanceToEnemy = float.MaxValue;

                foreach (var enemy in enemies)
                {
                    float distance = Vector3.Distance(spawnPoint.position, enemy.transform.position);
                    if (distance < minDistanceToEnemy)
                    {
                        minDistanceToEnemy = distance;
                    }
                }

                if (minDistanceToEnemy > bestDistance)
                {
                    bestDistance = minDistanceToEnemy;
                    bestPosition = spawnPoint.position;
                }
            }

            return bestPosition;
        }

        public Vector3 GetEnemySpawnPosition()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                Debug.LogWarning("No enemy spawn points configured! Using Vector3.zero");
                return Vector3.zero;
            }

            int index = Random.Range(0, enemySpawnPoints.Length);
            return enemySpawnPoints[index].position;
        }

        public Transform[] GetEnemySpawnPoints()
        {
            return enemySpawnPoints;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (playerSpawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in playerSpawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                        Gizmos.DrawLine(point.position, point.position + Vector3.up);
                    }
                }
            }

            if (enemySpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var point in enemySpawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireCube(point.position, Vector3.one * 0.5f);
                    }
                }
            }
        }
#endif
    }
}
