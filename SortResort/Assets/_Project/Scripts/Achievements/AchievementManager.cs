using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Manages all achievements, tracking progress and unlocking.
    /// Designed to be extensible - new worlds automatically get achievements generated.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        // Registered worlds - add new worlds here and achievements are auto-generated
        private static readonly string[] RegisteredWorlds = new string[]
        {
            "island",
            "supermarket",
            "farm",
            "tavern",
            "space"
            // Add new worlds here as they're created
        };

        // Levels per world (can be configured per world if needed)
        private const int LEVELS_PER_WORLD = 100;

        // All defined achievements
        private Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();

        // Player progress on achievements
        private Dictionary<string, AchievementProgress> progress = new Dictionary<string, AchievementProgress>();

        // All available tabs (dynamically built from worlds)
        private List<string> availableTabs = new List<string>();

        // Player resources (from achievement rewards)
        private int coins;
        private int undoTokens;
        private int skipTokens;
        private int unlockKeys;
        private int freezeTokens;
        private int timerFreezeTokens;
        private List<string> unlockedCosmetics = new List<string>();
        private List<string> unlockedTrophies = new List<string>();
        private HashSet<string> visitedWorlds = new HashSet<string>();

        // Statistics tracking
        private int totalMatchesMade;
        private int totalMovesMade;
        private int totalStarsEarned;
        private float totalPlaytimeSeconds;

        // Events
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<Achievement, int, int> OnAchievementProgress; // achievement, current, target
        public event Action<RewardType, int> OnRewardEarned;

        // Properties
        public int Coins => coins;
        public int UndoTokens => undoTokens;
        public int SkipTokens => skipTokens;
        public int UnlockKeys => unlockKeys;
        public int FreezeTokens => freezeTokens;
        public int TimerFreezeTokens => timerFreezeTokens;
        public IReadOnlyList<string> AvailableTabs => availableTabs;
        public static IReadOnlyList<string> Worlds => RegisteredWorlds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildAvailableTabs();
            InitializeAchievements();
            LoadProgress();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this)
                Instance = null;
        }

        #region Tab Management

        private void BuildAvailableTabs()
        {
            availableTabs.Clear();
            availableTabs.Add(Achievement.TAB_ALL);
            availableTabs.Add(Achievement.TAB_GENERAL);
            foreach (var world in RegisteredWorlds)
            {
                availableTabs.Add(world);
            }
        }

        #endregion

        #region Achievement Definitions

        private void InitializeAchievements()
        {
            // ==========================================
            // GENERAL - Global Level Completion
            // ==========================================
            CreateLevelCompletionAchievements();

            // ==========================================
            // GENERAL - Match Milestones
            // ==========================================
            CreateMatchMilestoneAchievements();

            // ==========================================
            // GENERAL - Star Collection
            // ==========================================
            CreateStarCollectionAchievements();

            // ==========================================
            // GENERAL - World Exploration
            // ==========================================
            CreateExplorationAchievements();

            // ==========================================
            // PER-WORLD - Generated for each world
            // ==========================================
            foreach (var worldId in RegisteredWorlds)
            {
                CreateWorldAchievements(worldId);
            }

            Debug.Log($"[AchievementManager] Initialized {achievements.Count} achievements for {RegisteredWorlds.Length} worlds");
        }

        /// <summary>
        /// Global level completion achievements (count across all worlds)
        /// </summary>
        private void CreateLevelCompletionAchievements()
        {
            var milestones = new[] { 1, 5, 10, 25, 50, 100, 250, 500 };
            var names = new[] { "First Steps", "Getting Started", "On Your Way", "Dedicated Player",
                               "Halfway Hero", "Century Club", "Sorting Expert", "Sorting Master" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Bronze, AchievementTier.Silver,
                               AchievementTier.Silver, AchievementTier.Gold, AchievementTier.Gold,
                               AchievementTier.Platinum, AchievementTier.Platinum };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"levels_total_{milestones[i]}", names[i], $"Complete {milestones[i]} level{(milestones[i] > 1 ? "s" : "")}",
                    AchievementCategory.Progression, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 5) },
                    AchievementTrackingType.Unique,
                    groupId: "levels_total", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        /// <summary>
        /// Match milestone achievements (total matches made)
        /// </summary>
        private void CreateMatchMilestoneAchievements()
        {
            var milestones = new[] { 10, 50, 100, 500, 1000, 5000 };
            var names = new[] { "First Matches", "Match Maker", "Sorting Spree",
                               "Match Maniac", "Thousand Sorts", "Sorting Legend" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Bronze, AchievementTier.Silver,
                               AchievementTier.Gold, AchievementTier.Gold, AchievementTier.Platinum };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"matches_total_{milestones[i]}", names[i], $"Make {milestones[i]} matches",
                    AchievementCategory.Milestone, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] / 2) },
                    AchievementTrackingType.Total,
                    groupId: "matches_total", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        /// <summary>
        /// Star collection achievements (total stars earned)
        /// </summary>
        private void CreateStarCollectionAchievements()
        {
            var milestones = new[] { 10, 50, 100, 250, 500, 1000 };
            var names = new[] { "Stargazer", "Rising Star", "Star Collector",
                               "Stellar Performance", "Galaxy of Stars", "Supernova" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Silver,
                               AchievementTier.Gold, AchievementTier.Gold, AchievementTier.Platinum };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"stars_total_{milestones[i]}", names[i], $"Earn {milestones[i]} stars",
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i]) },
                    AchievementTrackingType.Total,
                    groupId: "stars_total", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        /// <summary>
        /// World exploration achievements
        /// </summary>
        private void CreateExplorationAchievements()
        {
            // Visit X different worlds
            AddAchievement(new Achievement(
                "worlds_visited_2", "World Traveler", "Visit 2 different worlds",
                AchievementCategory.Exploration, AchievementTier.Bronze, 2,
                new[] { new AchievementReward(RewardType.Coins, 25) },
                AchievementTrackingType.Unique,
                groupId: "world_explorer", groupOrder: 1, tab: Achievement.TAB_GENERAL
            ));

            AddAchievement(new Achievement(
                "worlds_visited_3", "Globe Trotter", "Visit 3 different worlds",
                AchievementCategory.Exploration, AchievementTier.Silver, 3,
                new[] { new AchievementReward(RewardType.Coins, 50) },
                AchievementTrackingType.Unique,
                groupId: "world_explorer", groupOrder: 2, tab: Achievement.TAB_GENERAL
            ));

            AddAchievement(new Achievement(
                "worlds_visited_all", "World Champion", $"Visit all {RegisteredWorlds.Length} worlds",
                AchievementCategory.Exploration, AchievementTier.Gold, RegisteredWorlds.Length,
                new[] { new AchievementReward(RewardType.Coins, 100) },
                AchievementTrackingType.Unique,
                groupId: "world_explorer", groupOrder: 3, tab: Achievement.TAB_GENERAL
            ));
        }

        /// <summary>
        /// Create all achievements for a specific world
        /// </summary>
        private void CreateWorldAchievements(string worldId)
        {
            string worldName = Achievement.GetTabDisplayName(worldId);

            // Level completion in this world
            var levelMilestones = new[] { 1, 5, 10, 25, 50, 100 };
            var levelNames = new[] { "First Visit", "Getting Comfortable", "Making Progress",
                                     "Dedicated Explorer", "Halfway There", "World Complete" };
            var levelTiers = new[] { AchievementTier.Bronze, AchievementTier.Bronze, AchievementTier.Silver,
                                     AchievementTier.Silver, AchievementTier.Gold, AchievementTier.Platinum };

            for (int i = 0; i < levelMilestones.Length; i++)
            {
                // Skip 100-level achievement if LEVELS_PER_WORLD is less
                if (levelMilestones[i] > LEVELS_PER_WORLD) continue;

                string desc = levelMilestones[i] == 1
                    ? $"Complete your first level in {worldName}"
                    : $"Complete {levelMilestones[i]} levels in {worldName}";

                AddAchievement(new Achievement(
                    $"{worldId}_levels_{levelMilestones[i]}", $"{worldName}: {levelNames[i]}", desc,
                    AchievementCategory.Progression, levelTiers[i], levelMilestones[i],
                    new[] { new AchievementReward(RewardType.Coins, levelMilestones[i] * 3) },
                    AchievementTrackingType.Unique,
                    targetWorldId: worldId,
                    groupId: $"{worldId}_levels", groupOrder: i + 1, tab: worldId
                ));
            }

            // Stars in this world
            var starMilestones = new[] { 10, 25, 50, 100, 150, 300 };
            var starNames = new[] { "Shining Start", "Star Seeker", "Star Gatherer",
                                    "Star Hoarder", "Star Master", "Perfect World" };
            var starTiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Silver,
                                    AchievementTier.Gold, AchievementTier.Gold, AchievementTier.Platinum };

            for (int i = 0; i < starMilestones.Length; i++)
            {
                // Skip if milestone exceeds max possible stars (3 per level)
                if (starMilestones[i] > LEVELS_PER_WORLD * 3) continue;

                AddAchievement(new Achievement(
                    $"{worldId}_stars_{starMilestones[i]}", $"{worldName}: {starNames[i]}",
                    $"Earn {starMilestones[i]} stars in {worldName}",
                    AchievementCategory.Mastery, starTiers[i], starMilestones[i],
                    new[] { new AchievementReward(RewardType.Coins, starMilestones[i]) },
                    AchievementTrackingType.Total,
                    targetWorldId: worldId,
                    groupId: $"{worldId}_stars", groupOrder: i + 1, tab: worldId
                ));
            }

            // First 3-star in this world
            AddAchievement(new Achievement(
                $"{worldId}_first_3star", $"{worldName}: Perfect Start",
                $"Earn 3 stars on any level in {worldName}",
                AchievementCategory.Mastery, AchievementTier.Bronze, 1,
                new[] { new AchievementReward(RewardType.Coins, 15) },
                AchievementTrackingType.OneTime,
                targetWorldId: worldId, tab: worldId
            ));
        }

        private void AddAchievement(Achievement achievement)
        {
            achievements[achievement.id] = achievement;
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnMatchMade += OnMatchMade;
            GameEvents.OnLevelStarted += OnLevelStarted;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnMatchMade -= OnMatchMade;
            GameEvents.OnLevelStarted -= OnLevelStarted;
        }

        #endregion

        #region Event Handlers

        private void OnLevelStarted(int levelNumber)
        {
            // Get world from LevelManager
            string worldId = LevelManager.Instance?.CurrentWorldId;

            // Track world visits for exploration achievements
            if (!string.IsNullOrEmpty(worldId) && !visitedWorlds.Contains(worldId))
            {
                visitedWorlds.Add(worldId);
                SaveProgress();

                // Update world exploration achievements
                IncrementProgressUnique("worlds_visited_2", worldId);
                IncrementProgressUnique("worlds_visited_3", worldId);
                IncrementProgressUnique("worlds_visited_all", worldId);

                Debug.Log($"[AchievementManager] First visit to world: {worldId}");
            }
        }

        private void OnLevelCompleted(int levelNumber, int stars)
        {
            string worldId = LevelManager.Instance?.CurrentWorldId ?? "unknown";
            string levelIdentifier = $"{worldId}_{levelNumber}";

            // ==========================================
            // GLOBAL level completion achievements
            // ==========================================
            foreach (var a in achievements.Values)
            {
                if (a.groupId == "levels_total" && a.trackingType == AchievementTrackingType.Unique)
                {
                    IncrementProgressUnique(a.id, levelIdentifier);
                }
            }

            // ==========================================
            // WORLD-SPECIFIC level completion achievements
            // ==========================================
            foreach (var a in achievements.Values)
            {
                if (a.groupId == $"{worldId}_levels" && a.trackingType == AchievementTrackingType.Unique)
                {
                    IncrementProgressUnique(a.id, levelIdentifier);
                }
            }

            // ==========================================
            // STAR achievements
            // ==========================================
            if (stars > 0)
            {
                // Global star achievements
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "stars_total")
                    {
                        IncrementProgress(a.id, stars);
                    }
                }

                // World-specific star achievements
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == $"{worldId}_stars")
                    {
                        IncrementProgress(a.id, stars);
                    }
                }

                // First 3-star in world
                if (stars == 3)
                {
                    TriggerOneTime($"{worldId}_first_3star");
                }

                totalStarsEarned += stars;
            }

            Debug.Log($"[AchievementManager] Level completed ({levelIdentifier}, {stars} stars)");
        }

        private void OnMatchMade(string itemId)
        {
            totalMatchesMade++;

            // Update match milestone achievements
            foreach (var a in achievements.Values)
            {
                if (a.groupId == "matches_total")
                {
                    IncrementProgress(a.id, 1);
                }
            }
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// For Unique tracking: increments progress only if the identifier hasn't been counted before.
        /// Use for achievements like "Complete 10 unique levels" where replaying the same level doesn't count.
        /// </summary>
        public void IncrementProgressUnique(string achievementId, string uniqueIdentifier)
        {
            if (!achievements.TryGetValue(achievementId, out var achievement))
                return;

            // Verify this is a Unique tracking achievement
            if (achievement.trackingType != AchievementTrackingType.Unique)
            {
                Debug.LogWarning($"[AchievementManager] IncrementProgressUnique called on non-Unique achievement: {achievementId}");
                return;
            }

            // Skip timer achievements if timer is disabled
            var currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (achievement.requiresTimer && currentMode != GameMode.TimerMode && currentMode != GameMode.HardMode)
                return;

            var prog = GetOrCreateProgress(achievementId);
            if (prog.isUnlocked)
                return;

            // Only increment if this identifier is new
            if (!prog.TryAddUniqueIdentifier(uniqueIdentifier))
            {
                Debug.Log($"[AchievementManager] Identifier '{uniqueIdentifier}' already counted for {achievementId}");
                return;
            }

            prog.currentValue = prog.uniqueIdentifiers.Count;

            OnAchievementProgress?.Invoke(achievement, prog.currentValue, achievement.targetValue);

            if (prog.currentValue >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }

            SaveProgress();
        }

        /// <summary>
        /// For OneTime tracking: unlocks the achievement immediately on first trigger.
        /// Use for achievements like "First Match" that trigger once and never again.
        /// </summary>
        public void TriggerOneTime(string achievementId)
        {
            if (!achievements.TryGetValue(achievementId, out var achievement))
                return;

            // Verify this is a OneTime tracking achievement
            if (achievement.trackingType != AchievementTrackingType.OneTime)
            {
                Debug.LogWarning($"[AchievementManager] TriggerOneTime called on non-OneTime achievement: {achievementId}");
                return;
            }

            // Skip timer achievements if timer is disabled
            var currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (achievement.requiresTimer && currentMode != GameMode.TimerMode && currentMode != GameMode.HardMode)
                return;

            var prog = GetOrCreateProgress(achievementId);
            if (prog.isUnlocked)
                return;

            // OneTime achievements unlock immediately
            prog.currentValue = achievement.targetValue;
            UnlockAchievement(achievementId);
        }

        /// <summary>
        /// For Total tracking: increments progress by the given amount.
        /// Use for cumulative achievements like "Make 100 matches".
        /// </summary>
        public void IncrementProgress(string achievementId, int amount)
        {
            if (!achievements.TryGetValue(achievementId, out var achievement))
                return;

            // Skip timer achievements if timer is disabled
            var currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (achievement.requiresTimer && currentMode != GameMode.TimerMode && currentMode != GameMode.HardMode)
                return;

            var prog = GetOrCreateProgress(achievementId);
            if (prog.isUnlocked)
                return;

            prog.currentValue += amount;

            OnAchievementProgress?.Invoke(achievement, prog.currentValue, achievement.targetValue);

            if (prog.currentValue >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }

            SaveProgress();
        }

        public void SetProgress(string achievementId, int value)
        {
            if (!achievements.TryGetValue(achievementId, out var achievement))
                return;

            var currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (achievement.requiresTimer && currentMode != GameMode.TimerMode && currentMode != GameMode.HardMode)
                return;

            var prog = GetOrCreateProgress(achievementId);
            if (prog.isUnlocked)
                return;

            prog.currentValue = value;

            OnAchievementProgress?.Invoke(achievement, prog.currentValue, achievement.targetValue);

            if (prog.currentValue >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }

            SaveProgress();
        }

        public void ResetProgress(string achievementId)
        {
            if (progress.TryGetValue(achievementId, out var prog))
            {
                if (!prog.isUnlocked)
                {
                    prog.currentValue = 0;
                    SaveProgress();
                }
            }
        }

        private AchievementProgress GetOrCreateProgress(string achievementId)
        {
            if (!progress.TryGetValue(achievementId, out var prog))
            {
                prog = new AchievementProgress(achievementId);
                progress[achievementId] = prog;
            }
            return prog;
        }

        private void UnlockAchievement(string achievementId)
        {
            if (!achievements.TryGetValue(achievementId, out var achievement))
                return;

            var prog = GetOrCreateProgress(achievementId);
            if (prog.isUnlocked)
                return;

            prog.isUnlocked = true;
            prog.unlockedAt = DateTime.Now;

            Debug.Log($"[AchievementManager] Achievement unlocked: {achievement.name}");

            // Grant rewards
            foreach (var reward in achievement.rewards)
            {
                GrantReward(reward);
            }

            OnAchievementUnlocked?.Invoke(achievement);
            SaveProgress();
        }

        private void GrantReward(AchievementReward reward)
        {
            switch (reward.type)
            {
                case RewardType.Coins:
                    coins += reward.amount;
                    break;
                case RewardType.UndoToken:
                    undoTokens += reward.amount;
                    break;
                case RewardType.SkipToken:
                    skipTokens += reward.amount;
                    break;
                case RewardType.UnlockKey:
                    unlockKeys += reward.amount;
                    break;
                case RewardType.FreezeToken:
                    freezeTokens += reward.amount;
                    break;
                case RewardType.TimerFreeze:
                    timerFreezeTokens += reward.amount;
                    break;
                case RewardType.Cosmetic:
                    if (!string.IsNullOrEmpty(reward.cosmeticId))
                        unlockedCosmetics.Add(reward.cosmeticId);
                    break;
                case RewardType.Trophy:
                    unlockedTrophies.Add($"trophy_{achievementCount}");
                    break;
            }

            OnRewardEarned?.Invoke(reward.type, reward.amount);
            Debug.Log($"[AchievementManager] Reward granted: {reward.type} x{reward.amount}");
        }

        private int achievementCount => GetUnlockedCount();

        #endregion

        #region Public API

        public Achievement GetAchievement(string id)
        {
            return achievements.TryGetValue(id, out var a) ? a : null;
        }

        public AchievementProgress GetProgress(string achievementId)
        {
            return progress.TryGetValue(achievementId, out var p) ? p : null;
        }

        public List<Achievement> GetAllAchievements()
        {
            return new List<Achievement>(achievements.Values);
        }

        public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
        {
            var result = new List<Achievement>();
            foreach (var a in achievements.Values)
            {
                if (a.category == category)
                    result.Add(a);
            }
            return result;
        }

        public int GetUnlockedCount()
        {
            int count = 0;
            foreach (var p in progress.Values)
            {
                if (p.isUnlocked)
                    count++;
            }
            return count;
        }

        public int GetTotalCount()
        {
            return achievements.Count;
        }

        /// <summary>
        /// Get total points earned from unlocked achievements
        /// </summary>
        public int GetEarnedPoints()
        {
            int points = 0;
            foreach (var a in achievements.Values)
            {
                if (IsUnlocked(a.id))
                {
                    points += a.points;
                }
            }
            return points;
        }

        /// <summary>
        /// Get total possible points from all achievements
        /// </summary>
        public int GetTotalPoints()
        {
            int points = 0;
            foreach (var a in achievements.Values)
            {
                points += a.points;
            }
            return points;
        }

        /// <summary>
        /// Get achievements filtered by tab (string-based for extensibility)
        /// </summary>
        public List<Achievement> GetAchievementsByTab(string tab)
        {
            var result = new List<Achievement>();
            foreach (var a in achievements.Values)
            {
                if (tab == Achievement.TAB_ALL || a.tab == tab)
                    result.Add(a);
            }
            return result;
        }

        /// <summary>
        /// Get all unique group IDs
        /// </summary>
        public List<string> GetAllGroupIds()
        {
            var groups = new HashSet<string>();
            foreach (var a in achievements.Values)
            {
                if (!string.IsNullOrEmpty(a.groupId))
                    groups.Add(a.groupId);
            }
            return new List<string>(groups);
        }

        /// <summary>
        /// Get all achievements in a group, sorted by groupOrder
        /// </summary>
        public List<Achievement> GetAchievementsByGroup(string groupId)
        {
            var result = new List<Achievement>();
            foreach (var a in achievements.Values)
            {
                if (a.groupId == groupId)
                    result.Add(a);
            }
            result.Sort((a, b) => a.groupOrder.CompareTo(b.groupOrder));
            return result;
        }

        /// <summary>
        /// Get group name for display (derived from first achievement in group)
        /// </summary>
        public string GetGroupDisplayName(string groupId)
        {
            switch (groupId)
            {
                case "level_completion": return "Level Completion";
                case "star_collection": return "Star Collection";
                case "match_mastery": return "Match Mastery";
                default: return groupId.Replace("_", " ");
            }
        }

        public bool IsUnlocked(string achievementId)
        {
            return progress.TryGetValue(achievementId, out var p) && p.isUnlocked;
        }

        // Resource usage
        public bool UseUndoToken()
        {
            if (undoTokens > 0)
            {
                undoTokens--;
                SaveProgress();
                return true;
            }
            return false;
        }

        public bool UseSkipToken()
        {
            if (skipTokens > 0)
            {
                skipTokens--;
                SaveProgress();
                return true;
            }
            return false;
        }

        public bool UseFreezeToken()
        {
            if (freezeTokens > 0)
            {
                freezeTokens--;
                SaveProgress();
                return true;
            }
            return false;
        }

        public bool UseTimerFreezeToken()
        {
            if (timerFreezeTokens > 0)
            {
                timerFreezeTokens--;
                SaveProgress();
                return true;
            }
            return false;
        }

        #endregion

        #region Save/Load

        private const string ACHIEVEMENT_SAVE_KEY = "SortResortAchievements";

        private void SaveProgress()
        {
            var saveData = new AchievementSaveData
            {
                progress = new List<AchievementProgress>(progress.Values),
                coins = coins,
                undoTokens = undoTokens,
                skipTokens = skipTokens,
                unlockKeys = unlockKeys,
                freezeTokens = freezeTokens,
                timerFreezeTokens = timerFreezeTokens,
                unlockedCosmetics = unlockedCosmetics,
                unlockedTrophies = unlockedTrophies,
                visitedWorlds = new List<string>(visitedWorlds)
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(ACHIEVEMENT_SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            if (!PlayerPrefs.HasKey(ACHIEVEMENT_SAVE_KEY))
                return;

            try
            {
                string json = PlayerPrefs.GetString(ACHIEVEMENT_SAVE_KEY);
                var saveData = JsonUtility.FromJson<AchievementSaveData>(json);

                if (saveData.progress != null)
                {
                    foreach (var p in saveData.progress)
                    {
                        progress[p.achievementId] = p;
                    }
                }

                coins = saveData.coins;
                undoTokens = saveData.undoTokens;
                skipTokens = saveData.skipTokens;
                unlockKeys = saveData.unlockKeys;
                freezeTokens = saveData.freezeTokens;
                timerFreezeTokens = saveData.timerFreezeTokens;

                if (saveData.unlockedCosmetics != null)
                    unlockedCosmetics = saveData.unlockedCosmetics;
                if (saveData.unlockedTrophies != null)
                    unlockedTrophies = saveData.unlockedTrophies;
                if (saveData.visitedWorlds != null)
                    visitedWorlds = new HashSet<string>(saveData.visitedWorlds);

                Debug.Log($"[AchievementManager] Loaded {progress.Count} achievement progress entries");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] Failed to load achievements: {e.Message}");
            }
        }

        public void ResetAllAchievements()
        {
            progress.Clear();
            coins = 0;
            undoTokens = 0;
            skipTokens = 0;
            unlockKeys = 0;
            freezeTokens = 0;
            timerFreezeTokens = 0;
            unlockedCosmetics.Clear();
            unlockedTrophies.Clear();
            visitedWorlds.Clear();

            PlayerPrefs.DeleteKey(ACHIEVEMENT_SAVE_KEY);
            Debug.Log("[AchievementManager] All achievements reset");
        }

        [Serializable]
        private class AchievementSaveData
        {
            public List<AchievementProgress> progress;
            public int coins;
            public int undoTokens;
            public int skipTokens;
            public int unlockKeys;
            public int freezeTokens;
            public int timerFreezeTokens;
            public List<string> unlockedCosmetics;
            public List<string> unlockedTrophies;
            public List<string> visitedWorlds;
        }

        #endregion
    }
}
