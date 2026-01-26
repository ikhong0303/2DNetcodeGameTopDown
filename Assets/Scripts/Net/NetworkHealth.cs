using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace IsaacLike.Net
{
    public class NetworkHealth : NetworkBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHp = 6;

        [Header("Respawn (Players only)")]
        [SerializeField] private bool canRespawn = false;
        [SerializeField] private float respawnDelay = 2f;
        [SerializeField] private bool useSpawnPointManager = true;
        [SerializeField] private Vector2 fallbackRespawnPosition = Vector2.zero;

        [Header("UI (optional)")]
        [SerializeField] private TMP_Text hpText;

        public NetworkVariable<int> CurrentHp { get; private set; }
        private bool _isRespawning;

        private void Awake()
        {
            CurrentHp = new NetworkVariable<int>(
                maxHp,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentHp.Value = maxHp;
            }

            CurrentHp.OnValueChanged += OnHpChanged;
            RefreshText(CurrentHp.Value);
        }

        public override void OnNetworkDespawn()
        {
            CurrentHp.OnValueChanged -= OnHpChanged;
        }

        private void OnHpChanged(int previous, int next)
        {
            RefreshText(next);
        }

        private void RefreshText(int hp)
        {
            if (hpText != null)
            {
                hpText.text = hp.ToString();
            }
        }

        public void SetHpText(TMP_Text text)
        {
            hpText = text;
            RefreshText(CurrentHp.Value);
        }

        public int GetMaxHp()
        {
            return maxHp;
        }

        private void OnDeath()
        {
            if (!IsServer) return;

            bool isEnemy = GetComponent<NetworkEnemyChaser>() != null;
            bool isBoss = GetComponent<NetworkBossEnemy>() != null;

            if (ScoreManager.Instance != null)
            {
                if (isBoss)
                {
                    ScoreManager.Instance.AddBossKillServerRpc();
                }
                else if (isEnemy)
                {
                    ScoreManager.Instance.AddEnemyKillServerRpc();
                }
            }

            if (isEnemy || isBoss)
            {
                float dropChance = isBoss ? 0.8f : 0.2f;
                if (Random.value < dropChance)
                {
                    ItemSpawner.SpawnRandomItem(transform.position);
                }
            }
        }

        public void ApplyDamage(int damage)
        {
            if (!IsServer)
            {
                return;
            }

            if (_isRespawning)
            {
                return;
            }

            int next = Mathf.Clamp(CurrentHp.Value - damage, 0, maxHp);
            CurrentHp.Value = next;

            ShowDamageEffectsClientRpc(damage, transform.position);

            if (CurrentHp.Value <= 0)
            {
                if (canRespawn)
                {
                    StartCoroutine(RespawnCoroutine());
                }
                else
                {
                    OnDeath();
                    NetworkObject.Despawn();
                }

                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.CheckGameOver();
                }
            }
        }

        private System.Collections.IEnumerator RespawnCoroutine()
        {
            _isRespawning = true;

            var playerController = GetComponent<NetworkPlayerController2D>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            var colliders = GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            yield return new UnityEngine.WaitForSeconds(respawnDelay);

            Vector3 spawnPos = fallbackRespawnPosition;

            if (useSpawnPointManager && SpawnPointManager.Instance != null)
            {
                spawnPos = SpawnPointManager.Instance.GetSafePlayerSpawnPosition();
            }

            transform.position = spawnPos;
            CurrentHp.Value = maxHp;

            foreach (var col in colliders)
            {
                col.enabled = true;
            }

            if (playerController != null)
            {
                playerController.enabled = true;
            }

            _isRespawning = false;
        }

        public void Heal(int amount)
        {
            if (!IsServer)
            {
                return;
            }

            int next = Mathf.Clamp(CurrentHp.Value + amount, 0, maxHp);
            CurrentHp.Value = next;
        }

        [ClientRpc]
        private void ShowDamageEffectsClientRpc(int damage, Vector3 position)
        {
            bool isPlayer = GetComponent<NetworkPlayerController2D>() != null;

            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamage(position, damage, isPlayer);
            }

            if (VisualEffectsManager.Instance != null)
            {
                if (isPlayer)
                {
                    VisualEffectsManager.Instance.PlayPlayerHit(position);
                }
                else
                {
                    VisualEffectsManager.Instance.PlayProjectileHit(position);
                }
            }

            if (CameraShake.Instance != null && isPlayer)
            {
                CameraShake.Instance.ShakeSmall();
            }

            if (AudioManager.Instance != null)
            {
                string sfxName = isPlayer ? "player_hit" : "enemy_hit";
                AudioManager.Instance.PlaySFX(sfxName);
            }
        }
    }
}
