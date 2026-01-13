using UnityEngine;
using System.Collections.Generic;

namespace IsaacLike.Net
{
    [System.Serializable]
    public class PermanentUpgrade
    {
        public string id;
        public string name;
        public string description;
        public int cost;
        public int maxLevel;
        public int currentLevel;
        public UpgradeType type;
    }

    public enum UpgradeType
    {
        MaxHealth,
        MoveSpeed,
        Damage,
        FireRate,
        StartingGold
    }

    [System.Serializable]
    public class Unlockable
    {
        public string id;
        public string name;
        public string description;
        public int unlockCost;
        public bool isUnlocked;
        public UnlockableType type;
    }

    public enum UnlockableType
    {
        Character,
        Weapon,
        GameMode,
        Map
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int score;
        public int wave;
        public float timeElapsed;
        public string date;
    }

    public class ProgressionSystem : MonoBehaviour
    {
        public static ProgressionSystem Instance { get; private set; }

        [Header("Currency")]
        [SerializeField] private int totalCoins;

        [Header("Upgrades")]
        [SerializeField] private List<PermanentUpgrade> permanentUpgrades = new List<PermanentUpgrade>();

        [Header("Unlockables")]
        [SerializeField] private List<Unlockable> unlockables = new List<Unlockable>();

        [Header("Leaderboard")]
        [SerializeField] private List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();
        [SerializeField] private int maxLeaderboardEntries = 10;

        private const string SAVE_KEY = "GameProgress";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDefaultUpgrades();
            InitializeDefaultUnlockables();
            LoadProgress();
        }

        private void InitializeDefaultUpgrades()
        {
            if (permanentUpgrades.Count == 0)
            {
                permanentUpgrades.Add(new PermanentUpgrade
                {
                    id = "max_health",
                    name = "Max Health",
                    description = "Increase maximum health",
                    cost = 100,
                    maxLevel = 5,
                    currentLevel = 0,
                    type = UpgradeType.MaxHealth
                });

                permanentUpgrades.Add(new PermanentUpgrade
                {
                    id = "move_speed",
                    name = "Move Speed",
                    description = "Increase movement speed",
                    cost = 150,
                    maxLevel = 5,
                    currentLevel = 0,
                    type = UpgradeType.MoveSpeed
                });

                permanentUpgrades.Add(new PermanentUpgrade
                {
                    id = "damage",
                    name = "Damage",
                    description = "Increase damage",
                    cost = 200,
                    maxLevel = 5,
                    currentLevel = 0,
                    type = UpgradeType.Damage
                });

                permanentUpgrades.Add(new PermanentUpgrade
                {
                    id = "fire_rate",
                    name = "Fire Rate",
                    description = "Increase fire rate",
                    cost = 150,
                    maxLevel = 5,
                    currentLevel = 0,
                    type = UpgradeType.FireRate
                });
            }
        }

        private void InitializeDefaultUnlockables()
        {
            if (unlockables.Count == 0)
            {
                unlockables.Add(new Unlockable
                {
                    id = "survival_mode",
                    name = "Survival Mode",
                    description = "Endless waves of enemies",
                    unlockCost = 500,
                    isUnlocked = false,
                    type = UnlockableType.GameMode
                });

                unlockables.Add(new Unlockable
                {
                    id = "time_attack",
                    name = "Time Attack",
                    description = "Race against the clock",
                    unlockCost = 500,
                    isUnlocked = false,
                    type = UnlockableType.GameMode
                });

                unlockables.Add(new Unlockable
                {
                    id = "boss_rush",
                    name = "Boss Rush",
                    description = "Fight all bosses in sequence",
                    unlockCost = 1000,
                    isUnlocked = false,
                    type = UnlockableType.GameMode
                });
            }
        }

        public bool PurchaseUpgrade(string upgradeId)
        {
            PermanentUpgrade upgrade = permanentUpgrades.Find(u => u.id == upgradeId);

            if (upgrade == null)
            {
                Debug.LogWarning($"Upgrade {upgradeId} not found!");
                return false;
            }

            if (upgrade.currentLevel >= upgrade.maxLevel)
            {
                Debug.LogWarning($"Upgrade {upgradeId} already at max level!");
                return false;
            }

            int cost = upgrade.cost * (upgrade.currentLevel + 1);

            if (totalCoins < cost)
            {
                Debug.LogWarning($"Not enough coins! Need {cost}, have {totalCoins}");
                return false;
            }

            totalCoins -= cost;
            upgrade.currentLevel++;

            SaveProgress();
            Debug.Log($"Purchased {upgrade.name} level {upgrade.currentLevel}");
            return true;
        }

        public bool Unlock(string unlockableId)
        {
            Unlockable unlockable = unlockables.Find(u => u.id == unlockableId);

            if (unlockable == null)
            {
                Debug.LogWarning($"Unlockable {unlockableId} not found!");
                return false;
            }

            if (unlockable.isUnlocked)
            {
                Debug.LogWarning($"Already unlocked: {unlockable.name}");
                return false;
            }

            if (totalCoins < unlockable.unlockCost)
            {
                Debug.LogWarning($"Not enough coins! Need {unlockable.unlockCost}, have {totalCoins}");
                return false;
            }

            totalCoins -= unlockable.unlockCost;
            unlockable.isUnlocked = true;

            SaveProgress();
            Debug.Log($"Unlocked: {unlockable.name}");
            return true;
        }

        public void AddCoins(int amount)
        {
            totalCoins += amount;
            SaveProgress();
        }

        public int GetTotalCoins()
        {
            return totalCoins;
        }

        public int GetUpgradeLevel(UpgradeType type)
        {
            PermanentUpgrade upgrade = permanentUpgrades.Find(u => u.type == type);
            return upgrade?.currentLevel ?? 0;
        }

        public void AddLeaderboardEntry(string playerName, int score, int wave, float timeElapsed)
        {
            LeaderboardEntry entry = new LeaderboardEntry
            {
                playerName = playerName,
                score = score,
                wave = wave,
                timeElapsed = timeElapsed,
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            leaderboard.Add(entry);
            leaderboard.Sort((a, b) => b.score.CompareTo(a.score));

            if (leaderboard.Count > maxLeaderboardEntries)
            {
                leaderboard.RemoveRange(maxLeaderboardEntries, leaderboard.Count - maxLeaderboardEntries);
            }

            SaveProgress();
        }

        public List<LeaderboardEntry> GetLeaderboard()
        {
            return new List<LeaderboardEntry>(leaderboard);
        }

        private void SaveProgress()
        {
            ProgressData data = new ProgressData
            {
                coins = totalCoins,
                upgrades = permanentUpgrades,
                unlockables = unlockables,
                leaderboard = leaderboard
            };

            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                ProgressData data = JsonUtility.FromJson<ProgressData>(json);

                totalCoins = data.coins;

                foreach (var savedUpgrade in data.upgrades)
                {
                    PermanentUpgrade upgrade = permanentUpgrades.Find(u => u.id == savedUpgrade.id);
                    if (upgrade != null)
                    {
                        upgrade.currentLevel = savedUpgrade.currentLevel;
                    }
                }

                foreach (var savedUnlockable in data.unlockables)
                {
                    Unlockable unlockable = unlockables.Find(u => u.id == savedUnlockable.id);
                    if (unlockable != null)
                    {
                        unlockable.isUnlocked = savedUnlockable.isUnlocked;
                    }
                }

                leaderboard = data.leaderboard ?? new List<LeaderboardEntry>();
            }
        }

        [System.Serializable]
        private class ProgressData
        {
            public int coins;
            public List<PermanentUpgrade> upgrades;
            public List<Unlockable> unlockables;
            public List<LeaderboardEntry> leaderboard;
        }
    }
}
