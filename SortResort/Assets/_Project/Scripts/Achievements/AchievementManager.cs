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

        // Persistent counters for new achievements
        private int totalCombos;
        private int totalPowerUpsUsed;
        private int totalSimultaneousPowerUps;
        private int totalComboTimerBonus; // stored as integer seconds (truncated)
        private int totalPhotoFinishes;
        private int totalNegativeTimeCompletions;
        private int bestPowerUpsInOneLevel;
        private int bestComboTimerChain;

        // Persistent streaks (survive app close)
        private int threeStarStreak;
        private int hardModeStreak;

        // Stats screen counters
        private int totalLevelsFailed;
        private int totalLevelsRestarted;
        private int totalGoodCombos;    // 2-streak
        private int totalAmazingCombos; // 3-streak
        private int totalPerfectCombos; // 4+-streak
        private int bestComboStreak;
        private int powerUpsUsedSwap;
        private int powerUpsUsedDestroyLocker;
        private int powerUpsUsedMoveFreeze;
        private int powerUpsUsedTimeFreeze;
        private int dailyLoginCount;
        private string lastLoginDate; // yyyy-MM-dd

        // Per-level session stats (reset on level start)
        private int powerUpsUsedThisLevel;
        private float comboTimerBonusThisLevel;
        private int comboTimerChainThisLevel;

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

        // Stats screen getters
        public int TotalMatchesMade => totalMatchesMade;
        public int TotalMovesMade => totalMovesMade;
        public int TotalStarsEarned => totalStarsEarned;
        public float TotalPlaytimeSeconds => totalPlaytimeSeconds;
        public int TotalCombos => totalCombos;
        public int TotalPowerUpsUsed => totalPowerUpsUsed;
        public int TotalPhotoFinishes => totalPhotoFinishes;
        public int TotalNegativeTimeCompletions => totalNegativeTimeCompletions;
        public int ThreeStarStreak => threeStarStreak;
        public int HardModeStreak => hardModeStreak;
        public int BestComboTimerChain => bestComboTimerChain;
        public int TotalLevelsFailed => totalLevelsFailed;
        public int TotalLevelsRestarted => totalLevelsRestarted;
        public int TotalGoodCombos => totalGoodCombos;
        public int TotalAmazingCombos => totalAmazingCombos;
        public int TotalPerfectCombos => totalPerfectCombos;
        public int BestComboStreak => bestComboStreak;
        public int PowerUpsUsedSwap => powerUpsUsedSwap;
        public int PowerUpsUsedDestroyLocker => powerUpsUsedDestroyLocker;
        public int PowerUpsUsedMoveFreeze => powerUpsUsedMoveFreeze;
        public int PowerUpsUsedTimeFreeze => powerUpsUsedTimeFreeze;
        public int DailyLoginCount => dailyLoginCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            BuildAvailableTabs();
            InitializeAchievements();
            LoadProgress();
            CheckDailyLogin();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // Track playtime only while actively playing a level
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                totalPlaytimeSeconds += Time.unscaledDeltaTime;
            }
        }

        private void CheckDailyLogin()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (lastLoginDate != today)
            {
                dailyLoginCount++;
                lastLoginDate = today;
                SaveProgress();
            }
        }

        #region Tab Management

        private void BuildAvailableTabs()
        {
            availableTabs.Clear();
            availableTabs.Add(Achievement.TAB_GENERAL);
            availableTabs.Add(Achievement.TAB_STAR_MODE);
            availableTabs.Add(Achievement.TAB_TIMER_MODE);
            availableTabs.Add(Achievement.TAB_HARD_MODE);
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
            // GENERAL - 3-Star Levels (NEW)
            // ==========================================
            CreateThreeStarLevelAchievements();

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
            // GENERAL - Combos, Power-Ups
            // ==========================================
            CreateComboMasterAchievements();
            CreatePowerUserAchievements();
            CreatePowerFrenzyAchievements();
            CreateDoublePowerAchievements();

            // ==========================================
            // STAR MODE
            // ==========================================
            CreateStarStreakAchievements();
            CreatePerfectWorldAchievements();
            CreateUnderParAchievements();

            // ==========================================
            // TIMER MODE
            // ==========================================
            CreateTimerLevelAchievements();
            CreateSpeedDemonAchievements();
            CreateTimeSaverAchievements();
            CreateComboTimerAchievements();
            CreatePhotoFinishAchievements();
            CreateNegativeTimeAchievements();
            CreateComboChainAchievements();

            // ==========================================
            // HARD MODE
            // ==========================================
            CreateHardLevelAchievements();
            CreateHardPerfectionAchievements();
            CreateHardUnlockAchievements();
            CreateHardIroncladAchievements();
            CreateHardWorldAchievements();

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
        /// 3-Star level achievements (unique levels where player earned 3 stars)
        /// </summary>
        private void CreateThreeStarLevelAchievements()
        {
            var milestones = new[] { 100, 250, 500 };
            var names = new[] { "Star Student", "Star Scholar", "Star Savant" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"3star_levels_total_{milestones[i]}", names[i], $"3 Star {milestones[i]} Levels",
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 2) },
                    AchievementTrackingType.Unique,
                    groupId: "3star_levels_total", groupOrder: i + 1, tab: Achievement.TAB_STAR_MODE
                ));
            }
        }

        /// <summary>
        /// Global level completion achievements (count across all worlds)
        /// </summary>
        private void CreateLevelCompletionAchievements()
        {
            var milestones = new[] { 10, 100, 500 };
            var names = new[] { "Getting Started", "Century Club", "Sorting Master" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"levels_total_{milestones[i]}", names[i], $"Complete {milestones[i]} Levels",
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
            var milestones = new[] { 100, 1000, 5000 };
            var names = new[] { "Match Maker", "Thousand Sorts", "Sorting Legend" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"matches_total_{milestones[i]}", names[i], $"Make {milestones[i]} Matches",
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
            var milestones = new[] { 50, 500, 1000 };
            var names = new[] { "Rising Star", "Galaxy of Stars", "Supernova" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"stars_total_{milestones[i]}", names[i], $"Earn {milestones[i]} Stars",
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i]) },
                    AchievementTrackingType.Total,
                    groupId: "stars_total", groupOrder: i + 1, tab: Achievement.TAB_STAR_MODE
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
                "worlds_visited_1", "First Explorer", "Visit 1 World",
                AchievementCategory.Exploration, AchievementTier.Bronze, 1,
                new[] { new AchievementReward(RewardType.Coins, 10) },
                AchievementTrackingType.Unique,
                groupId: "world_explorer", groupOrder: 1, tab: Achievement.TAB_GENERAL
            ));

            AddAchievement(new Achievement(
                "worlds_visited_3", "Globe Trotter", "Visit 3 Worlds",
                AchievementCategory.Exploration, AchievementTier.Silver, 3,
                new[] { new AchievementReward(RewardType.Coins, 50) },
                AchievementTrackingType.Unique,
                groupId: "world_explorer", groupOrder: 2, tab: Achievement.TAB_GENERAL
            ));

            AddAchievement(new Achievement(
                "worlds_visited_all", "World Champion", $"Visit All {RegisteredWorlds.Length} Worlds",
                AchievementCategory.Exploration, AchievementTier.Gold, RegisteredWorlds.Length,
                new[] { new AchievementReward(RewardType.Coins, 100) },
                AchievementTrackingType.Unique,
                groupId: "world_explorer", groupOrder: 3, tab: Achievement.TAB_GENERAL
            ));
        }

        // ==========================================
        // NEW GENERAL ACHIEVEMENTS
        // ==========================================

        /// <summary>
        /// Combo achievements - total combos earned across all gameplay
        /// </summary>
        private void CreateComboMasterAchievements()
        {
            var milestones = new[] { 25, 100, 500 };
            var names = new[] { "Nice Combo", "Combo King", "Combo Legend" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"combo_master_{milestones[i]}", names[i], $"Get {milestones[i]} Combos",
                    AchievementCategory.Milestone, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 2) },
                    AchievementTrackingType.Total,
                    groupId: "combo_master", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        /// <summary>
        /// Power-up usage achievements - total power-ups used
        /// </summary>
        private void CreatePowerUserAchievements()
        {
            var milestones = new[] { 10, 50, 200 };
            var names = new[] { "Power Up", "Power Player", "Power Legend" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"power_user_{milestones[i]}", names[i], $"Use {milestones[i]} Power-Ups",
                    AchievementCategory.Milestone, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 3) },
                    AchievementTrackingType.Total,
                    groupId: "power_user", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        /// <summary>
        /// Power frenzy - most power-ups used in a single level
        /// </summary>
        private void CreatePowerFrenzyAchievements()
        {
            var milestones = new[] { 3, 5, 7 };
            var names = new[] { "Helping Hand", "Power Frenzy", "Power Overload" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"power_frenzy_{milestones[i]}", names[i], $"Use {milestones[i]} Power-Ups in One Level",
                    AchievementCategory.Challenge, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 20) },
                    AchievementTrackingType.BestValue,
                    groupId: "power_frenzy", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        /// <summary>
        /// Simultaneous power-up usage achievements
        /// </summary>
        private void CreateDoublePowerAchievements()
        {
            var milestones = new[] { 1, 10, 25 };
            var names = new[] { "Double Trouble", "Dual Wielder", "Overcharged" };
            var descs = new[] { "Use 2 Power-Ups at the Same Time", "Use 2 Power-Ups at the Same Time 10 Times", "Use 2 Power-Ups at the Same Time 25 Times" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"double_power_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Challenge, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 15) },
                    AchievementTrackingType.Total,
                    groupId: "double_power", groupOrder: i + 1, tab: Achievement.TAB_GENERAL
                ));
            }
        }

        // ==========================================
        // NEW STAR MODE ACHIEVEMENTS
        // ==========================================

        /// <summary>
        /// 3-star streak achievements - consecutive 3-star completions
        /// </summary>
        private void CreateStarStreakAchievements()
        {
            var milestones = new[] { 5, 10, 25 };
            var names = new[] { "Hot Streak", "On Fire", "Untouchable" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"star_streak_{milestones[i]}", names[i], $"3 Star {milestones[i]} Levels in a Row",
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 10) },
                    AchievementTrackingType.BestValue,
                    groupId: "star_streak", groupOrder: i + 1, tab: Achievement.TAB_STAR_MODE
                ));
            }
        }

        /// <summary>
        /// Perfect world - 3-star every level in a world
        /// </summary>
        private void CreatePerfectWorldAchievements()
        {
            var milestones = new[] { 1, 3, 5 };
            var names = new[] { "World Perfectionist", "Triple Perfectionist", "Universal Perfection" };
            var descs = new[] { "3 Star Every Level in 1 World", "3 Star Every Level in 3 Worlds", "3 Star Every Level in 5 Worlds" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"perfect_world_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 100) },
                    AchievementTrackingType.Unique,
                    groupId: "perfect_world", groupOrder: i + 1, tab: Achievement.TAB_STAR_MODE
                ));
            }
        }

        /// <summary>
        /// Under par - beat the 3-star score by 2+ moves
        /// </summary>
        private void CreateUnderParAchievements()
        {
            var milestones = new[] { 1, 10, 50 };
            var names = new[] { "Overachiever", "Efficiency Expert", "Sorting Prodigy" };
            var descs = new[] { "Beat the 3 Star Score by 2+ Moves", "Beat the 3 Star Score by 2+ Moves 10 Times", "Beat the 3 Star Score by 2+ Moves 50 Times" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"under_par_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Efficiency, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 20) },
                    AchievementTrackingType.Unique,
                    groupId: "under_par", groupOrder: i + 1, tab: Achievement.TAB_STAR_MODE
                ));
            }
        }

        // ==========================================
        // NEW TIMER MODE ACHIEVEMENTS
        // ==========================================

        /// <summary>
        /// Timer mode level completion milestones
        /// </summary>
        private void CreateTimerLevelAchievements()
        {
            var milestones = new[] { 25, 100, 250 };
            var names = new[] { "Ticking Along", "Clockwork", "Time Lord" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"timer_levels_{milestones[i]}", names[i], $"Complete {milestones[i]} Levels in Timer Mode",
                    AchievementCategory.Progression, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 3) },
                    AchievementTrackingType.Unique,
                    requiresTimer: true,
                    groupId: "timer_levels", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        /// <summary>
        /// Speed demon - complete a level in under X seconds
        /// </summary>
        private void CreateSpeedDemonAchievements()
        {
            var milestones = new[] { 10, 5, 1 };
            var names = new[] { "Quick Sort", "Speed Demon", "Lightning Fast" };
            var descs = new[] { "Complete a Level in Under 10 Seconds", "Complete a Level in Under 5 Seconds", "Complete a Level in Under 1 Second" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"speed_demon_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Speed, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, (31 - milestones[i]) * 10) },
                    AchievementTrackingType.BestValue,
                    requiresTimer: true,
                    groupId: "speed_demon", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        /// <summary>
        /// Time saver - finish with X% time remaining
        /// </summary>
        private void CreateTimeSaverAchievements()
        {
            var milestones = new[] { 85, 95, 99 };
            var names = new[] { "Ahead of Schedule", "Time Banker", "Time Hoarder" };
            var descs = new[] { "Finish with 85% Time Remaining", "Finish with 95% Time Remaining", "Finish with 99% Time Remaining" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"time_saver_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Speed, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 2) },
                    AchievementTrackingType.BestValue,
                    requiresTimer: true,
                    groupId: "time_saver", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        /// <summary>
        /// Combo timer - bonus seconds earned from combos
        /// </summary>
        private void CreateComboTimerAchievements()
        {
            var milestones = new[] { 50, 200, 500 };
            var names = new[] { "Bonus Time", "Time Thief", "Chrono Master" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"combo_timer_{milestones[i]}", names[i], $"Earn {milestones[i]} Seconds from Matches",
                    AchievementCategory.Milestone, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i]) },
                    AchievementTrackingType.Total,
                    requiresTimer: true,
                    groupId: "combo_timer", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        /// <summary>
        /// Photo finish - complete with less than 1 second left
        /// </summary>
        private void CreatePhotoFinishAchievements()
        {
            var milestones = new[] { 1, 5, 25 };
            var names = new[] { "Photo Finish", "Clutch Player", "Edge Runner" };
            var descs = new[] {
                "Complete a Level with <1 Second Left",
                "Complete 5 Levels with <1 Second Left",
                "Complete 25 Levels with <1 Second Left"
            };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"photo_finish_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Challenge, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 25) },
                    AchievementTrackingType.Total,
                    requiresTimer: true,
                    groupId: "photo_finish", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        /// <summary>
        /// Negative time - complete with negative time (combo bonuses exceeded elapsed time)
        /// </summary>
        private void CreateNegativeTimeAchievements()
        {
            var milestones = new[] { 1, 5, 10 };
            var names = new[] { "Time Traveler", "Temporal Anomaly", "Chrono Breaker" };
            var descs = new[] {
                "Complete a Level with Negative Time",
                "Complete 5 Levels with Negative Time",
                "Complete 10 Levels with Negative Time"
            };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"negative_time_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Challenge, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 50) },
                    AchievementTrackingType.Total,
                    requiresTimer: true,
                    groupId: "negative_time", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        /// <summary>
        /// Combo chain - consecutive timer extensions in a row
        /// </summary>
        private void CreateComboChainAchievements()
        {
            var milestones = new[] { 4, 6, 8 };
            var names = new[] { "Chain Reaction", "Combo Blitz", "Endless Chain" };
            var descs = new[] {
                "Get 4 Timer Extensions in a Row",
                "Get 6 Timer Extensions in a Row",
                "Get 8 Timer Extensions in a Row"
            };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"combo_chain_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Challenge, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 15) },
                    AchievementTrackingType.BestValue,
                    requiresTimer: true,
                    groupId: "combo_chain", groupOrder: i + 1, tab: Achievement.TAB_TIMER_MODE
                ));
            }
        }

        // ==========================================
        // NEW HARD MODE ACHIEVEMENTS
        // ==========================================

        /// <summary>
        /// Hard mode level completion milestones
        /// </summary>
        private void CreateHardLevelAchievements()
        {
            var milestones = new[] { 10, 50, 100 };
            var names = new[] { "Hard Hitter", "Tough Cookie", "Hard as Nails" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"hard_levels_{milestones[i]}", names[i], $"Complete {milestones[i]} Levels in Hard Mode",
                    AchievementCategory.Progression, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 5) },
                    AchievementTrackingType.Unique,
                    groupId: "hard_levels", groupOrder: i + 1, tab: Achievement.TAB_HARD_MODE
                ));
            }
        }

        /// <summary>
        /// Hard mode 3-star achievements
        /// </summary>
        private void CreateHardPerfectionAchievements()
        {
            var milestones = new[] { 10, 50, 100 };
            var names = new[] { "Hard Brilliance", "Hard Prodigy", "Hard Flawless" };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"hard_perfection_{milestones[i]}", names[i], $"3 Star {milestones[i]} Levels in Hard Mode",
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 8) },
                    AchievementTrackingType.Unique,
                    groupId: "hard_perfection", groupOrder: i + 1, tab: Achievement.TAB_HARD_MODE
                ));
            }
        }

        /// <summary>
        /// Hard mode unlock achievements - unlock hard mode in X worlds
        /// </summary>
        private void CreateHardUnlockAchievements()
        {
            var milestones = new[] { 1, 3, 5 };
            var names = new[] { "Challenge Accepted", "Challenger", "Ultimate Challenger" };
            var descs = new[] {
                "Unlock Hard Mode in 1 World",
                "Unlock Hard Mode in 3 Worlds",
                "Unlock Hard Mode in 5 Worlds"
            };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"hard_unlock_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Progression, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 50) },
                    AchievementTrackingType.Unique,
                    groupId: "hard_unlock", groupOrder: i + 1, tab: Achievement.TAB_HARD_MODE
                ));
            }
        }

        /// <summary>
        /// Hard mode ironclad - complete X hard mode levels without failing
        /// </summary>
        private void CreateHardIroncladAchievements()
        {
            var milestones = new[] { 5, 10, 25 };
            var names = new[] { "Ironclad", "Unbreakable", "Invincible" };
            var descs = new[] {
                "Complete 5 Hard Modes in a Row",
                "Complete 10 Hard Modes in a Row",
                "Complete 25 Hard Modes in a Row"
            };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"hard_ironclad_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Challenge, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 15) },
                    AchievementTrackingType.BestValue,
                    groupId: "hard_ironclad", groupOrder: i + 1, tab: Achievement.TAB_HARD_MODE
                ));
            }
        }

        /// <summary>
        /// Hard mode world completion - complete all 100 levels in a world on hard mode
        /// </summary>
        private void CreateHardWorldAchievements()
        {
            var milestones = new[] { 1, 3, 5 };
            var names = new[] { "Hard World Beaten", "Hard Conqueror", "Hard Dominator" };
            var descs = new[] {
                "Complete All Hard Modes in a World",
                "Complete All Hard Modes in 3 Worlds",
                "Complete All Hard Modes in 5 Worlds"
            };
            var tiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < milestones.Length; i++)
            {
                AddAchievement(new Achievement(
                    $"hard_world_{milestones[i]}", names[i], descs[i],
                    AchievementCategory.Mastery, tiers[i], milestones[i],
                    new[] { new AchievementReward(RewardType.Coins, milestones[i] * 100) },
                    AchievementTrackingType.Unique,
                    groupId: "hard_world", groupOrder: i + 1, tab: Achievement.TAB_HARD_MODE
                ));
            }
        }

        /// <summary>
        /// Create all achievements for a specific world
        /// </summary>
        private void CreateWorldAchievements(string worldId)
        {
            string worldName = worldId == "supermarket" ? "Market" : Achievement.GetTabDisplayName(worldId);

            // Level completion in this world (3 milestones)
            var levelMilestones = new[] { 25, 50, 100 };
            var levelNames = new[] { "Making Progress", "Halfway There", "World Complete" };
            var levelTiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < levelMilestones.Length; i++)
            {
                if (levelMilestones[i] > LEVELS_PER_WORLD) continue;

                AddAchievement(new Achievement(
                    $"{worldId}_levels_{levelMilestones[i]}", $"{worldName}: {levelNames[i]}",
                    $"Complete {levelMilestones[i]} Levels in this World",
                    AchievementCategory.Progression, levelTiers[i], levelMilestones[i],
                    new[] { new AchievementReward(RewardType.Coins, levelMilestones[i] * 3) },
                    AchievementTrackingType.Unique,
                    targetWorldId: worldId,
                    groupId: $"{worldId}_levels", groupOrder: i + 1, tab: worldId
                ));
            }

            // Stars in this world (3 milestones)
            var starMilestones = new[] { 100, 200, 300 };
            var starNames = new[] { "Star Hoarder", "Star Master", "Perfect World" };
            var starTiers = new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold };

            for (int i = 0; i < starMilestones.Length; i++)
            {
                if (starMilestones[i] > LEVELS_PER_WORLD * 3) continue;

                AddAchievement(new Achievement(
                    $"{worldId}_stars_{starMilestones[i]}", $"{worldName}: {starNames[i]}",
                    $"Earn {starMilestones[i]} Stars in this World",
                    AchievementCategory.Mastery, starTiers[i], starMilestones[i],
                    new[] { new AchievementReward(RewardType.Coins, starMilestones[i]) },
                    AchievementTrackingType.Total,
                    targetWorldId: worldId,
                    groupId: $"{worldId}_stars", groupOrder: i + 1, tab: worldId
                ));
            }
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
            GameEvents.OnLevelCompletedDetailed += OnLevelCompletedDetailed;
            GameEvents.OnMatchMade += OnMatchMade;
            GameEvents.OnMoveUsed += OnMoveUsed;
            GameEvents.OnLevelStarted += OnLevelStarted;
            GameEvents.OnGamePaused += OnGamePaused;
            GameEvents.OnLevelFailed += OnLevelFailed;
            GameEvents.OnLevelRestarted += OnLevelRestarted;
            GameEvents.OnPowerUpUsed += OnPowerUpUsed;
            GameEvents.OnComboTriggered += OnComboTriggered;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnLevelCompletedDetailed -= OnLevelCompletedDetailed;
            GameEvents.OnMatchMade -= OnMatchMade;
            GameEvents.OnMoveUsed -= OnMoveUsed;
            GameEvents.OnLevelStarted -= OnLevelStarted;
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnLevelFailed -= OnLevelFailed;
            GameEvents.OnLevelRestarted -= OnLevelRestarted;
            GameEvents.OnPowerUpUsed -= OnPowerUpUsed;
            GameEvents.OnComboTriggered -= OnComboTriggered;
        }

        #endregion

        #region Event Handlers

        private void OnLevelStarted(int levelNumber)
        {
            // Reset per-level session stats
            powerUpsUsedThisLevel = 0;
            comboTimerBonusThisLevel = 0f;
            comboTimerChainThisLevel = 0;

            // Get world from LevelManager
            string worldId = LevelManager.Instance?.CurrentWorldId;

            // Track world visits for exploration achievements
            if (!string.IsNullOrEmpty(worldId) && !visitedWorlds.Contains(worldId))
            {
                visitedWorlds.Add(worldId);
                SaveProgress();

                // Update world exploration achievements
                IncrementProgressUnique("worlds_visited_1", worldId);
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

                // Track 3-star level completions
                if (stars == 3)
                {
                    foreach (var a in achievements.Values)
                    {
                        if (a.groupId == "3star_levels_total")
                            IncrementProgressUnique(a.id, levelIdentifier);
                    }
                }

                totalStarsEarned += stars;
            }

            Debug.Log($"[AchievementManager] Level completed ({levelIdentifier}, {stars} stars)");
        }

        private void OnMoveUsed(int currentMoveCount)
        {
            totalMovesMade++;
        }

        private void OnGamePaused()
        {
            // Save stats (playtime, moves) on pause so they aren't lost if app is killed
            SaveProgress();
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

        /// <summary>
        /// Detailed level completion handler for mode-specific achievements
        /// </summary>
        private void OnLevelCompletedDetailed(LevelCompletionData data)
        {
            string worldId = LevelManager.Instance?.CurrentWorldId ?? "unknown";
            string levelIdentifier = $"{worldId}_{data.levelNumber}";
            int movesUsed = GameManager.Instance?.CurrentMoveCount ?? 0;

            // ==========================================
            // POWER FRENZY - best power-ups in one level
            // ==========================================
            if (powerUpsUsedThisLevel > 0)
            {
                if (powerUpsUsedThisLevel > bestPowerUpsInOneLevel)
                    bestPowerUpsInOneLevel = powerUpsUsedThisLevel;

                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "power_frenzy")
                        UpdateBestValue(a.id, powerUpsUsedThisLevel);
                }
            }

            // ==========================================
            // COMBO CHAIN - best consecutive timer extensions
            // ==========================================
            if (comboTimerChainThisLevel > 0)
            {
                if (comboTimerChainThisLevel > bestComboTimerChain)
                    bestComboTimerChain = comboTimerChainThisLevel;

                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "combo_chain")
                        UpdateBestValue(a.id, comboTimerChainThisLevel);
                }
            }

            // ==========================================
            // STAR MODE ACHIEVEMENTS
            // ==========================================
            if (data.mode == GameMode.StarMode || data.mode == GameMode.HardMode)
            {
                // Star streak tracking
                if (data.starsEarned == 3)
                {
                    threeStarStreak++;
                    foreach (var a in achievements.Values)
                    {
                        if (a.groupId == "star_streak")
                            UpdateBestValue(a.id, threeStarStreak);
                    }

                    // Perfect world check - see if all 100 levels in this world have 3 stars
                    if (SaveManager.Instance != null)
                    {
                        bool allThreeStars = true;
                        for (int lvl = 1; lvl <= LEVELS_PER_WORLD; lvl++)
                        {
                            if (SaveManager.Instance.GetLevelStars(worldId, lvl) < 3)
                            {
                                allThreeStars = false;
                                break;
                            }
                        }
                        if (allThreeStars)
                        {
                            foreach (var a in achievements.Values)
                            {
                                if (a.groupId == "perfect_world")
                                    IncrementProgressUnique(a.id, worldId);
                            }
                        }
                    }
                }
                else
                {
                    // Non-3-star resets the streak
                    threeStarStreak = 0;
                }

                // Under par check (beat 3-star threshold by 2+ moves)
                var thresholds = LevelManager.Instance?.StarThresholds;
                if (thresholds != null && thresholds.Length > 0 && movesUsed <= thresholds[0] - 2)
                {
                    foreach (var a in achievements.Values)
                    {
                        if (a.groupId == "under_par")
                            IncrementProgressUnique(a.id, levelIdentifier);
                    }
                }
            }

            // ==========================================
            // TIMER MODE ACHIEVEMENTS
            // ==========================================
            if (data.mode == GameMode.TimerMode || data.mode == GameMode.HardMode)
            {
                // Timer level completion count
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "timer_levels")
                        IncrementProgressUnique(a.id, $"{data.mode}_{levelIdentifier}");
                }

                // Speed demon & time saver - skip Island level 1 (2-move tutorial is too trivial)
                float completionTime = data.timeTaken;
                if (!(worldId == "island" && data.levelNumber == 1))
                {
                    // Speed demon - check elapsed time against thresholds
                    // For "lower is better": we check if time is under each threshold
                    int[] speedThresholds = { 10, 5, 1 };
                    foreach (int threshold in speedThresholds)
                    {
                        if (completionTime < threshold && completionTime >= 0)
                        {
                            string achId = $"speed_demon_{threshold}";
                            if (achievements.ContainsKey(achId))
                            {
                                var prog = GetOrCreateProgress(achId);
                                if (!prog.isUnlocked)
                                {
                                    prog.currentValue = threshold; // Met the threshold
                                    UnlockAchievement(achId);
                                }
                            }
                        }
                    }

                    // Time saver - percentage of time remaining
                    float totalTimeLimit = LevelManager.Instance?.TotalTimeLimit ?? 0f;
                    if (totalTimeLimit > 0)
                    {
                        float timeRemaining = totalTimeLimit - completionTime;
                        int percentRemaining = Mathf.RoundToInt((timeRemaining / totalTimeLimit) * 100f);

                        foreach (var a in achievements.Values)
                        {
                            if (a.groupId == "time_saver")
                                UpdateBestValue(a.id, percentRemaining);
                        }
                    }
                }

                // Photo finish - completed with less than 1 second remaining
                float finalTimeRemaining = LevelManager.Instance?.TimeRemaining ?? float.MaxValue;
                if (finalTimeRemaining >= 0f && finalTimeRemaining < 1f)
                {
                    totalPhotoFinishes++;
                    foreach (var a in achievements.Values)
                    {
                        if (a.groupId == "photo_finish")
                            IncrementProgress(a.id, 1);
                    }
                }

                // Negative time - completed with negative elapsed time (combo bonuses exceeded real time)
                if (completionTime < 0f)
                {
                    totalNegativeTimeCompletions++;
                    foreach (var a in achievements.Values)
                    {
                        if (a.groupId == "negative_time")
                            IncrementProgress(a.id, 1);
                    }
                }
            }

            // ==========================================
            // HARD MODE ACHIEVEMENTS
            // ==========================================
            if (data.mode == GameMode.HardMode)
            {
                // Hard level completion count
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "hard_levels")
                        IncrementProgressUnique(a.id, levelIdentifier);
                }

                // Hard perfection - 3-star in hard mode
                if (data.starsEarned == 3)
                {
                    foreach (var a in achievements.Values)
                    {
                        if (a.groupId == "hard_perfection")
                            IncrementProgressUnique(a.id, levelIdentifier);
                    }
                }

                // Hard ironclad streak
                hardModeStreak++;
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "hard_ironclad")
                        UpdateBestValue(a.id, hardModeStreak);
                }

                // Hard world completion - check if all 100 levels done in hard mode
                // Active mode is HardMode here, so SaveManager.IsLevelCompleted checks hard mode progress
                if (SaveManager.Instance != null)
                {
                    int hardHighest = SaveManager.Instance.GetHighestLevelCompleted(worldId);
                    if (hardHighest >= LEVELS_PER_WORLD)
                    {
                        foreach (var a in achievements.Values)
                        {
                            if (a.groupId == "hard_world")
                                IncrementProgressUnique(a.id, worldId);
                        }
                    }
                }
            }

            // Hard mode unlock tracking (fires when HardMode is unlocked for any world)
            // This is handled separately via CheckHardModeUnlocks()

            SaveProgress();
        }

        /// <summary>
        /// Power-up usage handler
        /// </summary>
        private void OnPowerUpUsed(PowerUpType type)
        {
            // Check for simultaneous power-ups BEFORE incrementing
            bool wasAlreadyFrozen = (LevelManager.Instance?.IsTimerFrozen ?? false) ||
                                    (PowerUpManager.Instance?.IsMovesFrozen ?? false);

            powerUpsUsedThisLevel++;
            totalPowerUpsUsed++;

            // Per-type tracking for stats screen
            switch (type)
            {
                case PowerUpType.SwapItems: powerUpsUsedSwap++; break;
                case PowerUpType.DestroyLocker: powerUpsUsedDestroyLocker++; break;
                case PowerUpType.MoveFreeze: powerUpsUsedMoveFreeze++; break;
                case PowerUpType.TimeFreeze: powerUpsUsedTimeFreeze++; break;
            }

            // Total power-up usage achievements
            foreach (var a in achievements.Values)
            {
                if (a.groupId == "power_user")
                    IncrementProgress(a.id, 1);
            }

            // Simultaneous power-up detection
            // If a freeze was already active when this power-up was used, it's simultaneous
            if (wasAlreadyFrozen)
            {
                totalSimultaneousPowerUps++;
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "double_power")
                        IncrementProgress(a.id, 1);
                }
            }

            SaveProgress();
        }

        /// <summary>
        /// Combo triggered handler
        /// </summary>
        private void OnComboTriggered(int comboStreak)
        {
            totalCombos++;

            // Per-tier combo tracking for stats screen
            if (comboStreak == 2) totalGoodCombos++;
            else if (comboStreak == 3) totalAmazingCombos++;
            else if (comboStreak >= 4) totalPerfectCombos++;

            // Best combo streak
            if (comboStreak > bestComboStreak)
                bestComboStreak = comboStreak;

            // Combo master achievements (total combos)
            foreach (var a in achievements.Values)
            {
                if (a.groupId == "combo_master")
                    IncrementProgress(a.id, 1);
            }

            // Timer combo chain tracking (only in timer modes)
            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (mode == GameMode.TimerMode || mode == GameMode.HardMode)
            {
                // Combo timer bonus seconds: streak 2 = 1s, 3 = 3s, 4+ = 5s
                float bonusSeconds = comboStreak == 2 ? 1f : comboStreak == 3 ? 3f : 5f;
                comboTimerBonusThisLevel += bonusSeconds;
                totalComboTimerBonus += Mathf.RoundToInt(bonusSeconds);

                // Combo timer total bonus achievements
                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "combo_timer")
                        IncrementProgress(a.id, Mathf.RoundToInt(bonusSeconds));
                }

                // Combo chain tracking (consecutive timer extensions)
                comboTimerChainThisLevel++;
                // Best chain is checked at level completion
            }

            SaveProgress();
        }

        /// <summary>
        /// Level failed handler - resets streaks
        /// </summary>
        private void OnLevelFailed(int levelNumber, string reason)
        {
            totalLevelsFailed++;

            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;

            if (mode == GameMode.HardMode)
            {
                hardModeStreak = 0;
            }

            if (mode == GameMode.StarMode || mode == GameMode.HardMode)
            {
                threeStarStreak = 0;
            }

            // Reset combo chain on failure
            comboTimerChainThisLevel = 0;

            SaveProgress();
        }

        /// <summary>
        /// Level restarted handler - resets hard mode ironclad streak
        /// </summary>
        private void OnLevelRestarted()
        {
            totalLevelsRestarted++;

            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;

            if (mode == GameMode.HardMode)
            {
                hardModeStreak = 0;
            }

            // Reset combo chain on restart
            comboTimerChainThisLevel = 0;

            SaveProgress();
        }

        /// <summary>
        /// Called when a combo chain breaks (match without consecutive combo).
        /// Saves the best chain seen before resetting.
        /// </summary>
        public void OnComboChainBroken()
        {
            if (comboTimerChainThisLevel > 0)
            {
                // Save best chain before resetting
                if (comboTimerChainThisLevel > bestComboTimerChain)
                    bestComboTimerChain = comboTimerChainThisLevel;

                foreach (var a in achievements.Values)
                {
                    if (a.groupId == "combo_chain")
                        UpdateBestValue(a.id, comboTimerChainThisLevel);
                }

                comboTimerChainThisLevel = 0;
            }
        }

        /// <summary>
        /// Call when Hard Mode is unlocked for a world (from DialogueManager or GameManager)
        /// </summary>
        public void NotifyHardModeUnlocked(string worldId)
        {
            foreach (var a in achievements.Values)
            {
                if (a.groupId == "hard_unlock")
                    IncrementProgressUnique(a.id, worldId);
            }
            SaveProgress();
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

        /// <summary>
        /// For BestValue tracking: updates progress if the new value is higher than the current best.
        /// Use for achievements like "3 Star 5 Levels in a Row" where only the best streak matters.
        /// </summary>
        public void UpdateBestValue(string achievementId, int value)
        {
            if (!achievements.TryGetValue(achievementId, out var achievement))
                return;

            if (achievement.trackingType != AchievementTrackingType.BestValue)
            {
                Debug.LogWarning($"[AchievementManager] UpdateBestValue called on non-BestValue achievement: {achievementId}");
                return;
            }

            var currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (achievement.requiresTimer && currentMode != GameMode.TimerMode && currentMode != GameMode.HardMode)
                return;

            var prog = GetOrCreateProgress(achievementId);
            if (prog.isUnlocked)
                return;

            if (value <= prog.bestValue)
                return;

            prog.bestValue = value;
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
                if (tab == Achievement.TAB_ALL || tab == Achievement.TAB_RECENT || a.tab == tab)
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

        // Map from groupId to the icon resource name (loaded from Sprites/UI/Achievements/Icons/)
        private static readonly Dictionary<string, string> groupArtKeys = new Dictionary<string, string>
        {
            // General
            { "3star_levels_total", "3star_levels" },
            { "levels_total", "levels" },
            { "matches_total", "matches" },
            { "stars_total", "stars" },
            { "world_explorer", "visit_worlds" },
            { "combo_master", "combos" },
            { "power_user", "power_ups" },
            { "power_frenzy", "power_frenzy" },
            { "double_power", "double_power" },
            // Star Mode
            { "star_streak", "star_streak" },
            { "perfect_world", "perfect_world" },
            { "under_par", "under_par" },
            // Timer Mode
            { "timer_levels", "timer_levels" },
            { "speed_demon", "speed_demon" },
            { "time_saver", "time_saver" },
            { "combo_timer", "combo_timer" },
            { "photo_finish", "photo_finish" },
            { "negative_time", "negative_time" },
            { "combo_chain", "combo_chain" },
            // Hard Mode
            { "hard_levels", "hard_levels" },
            { "hard_perfection", "hard_perfection" },
            { "hard_unlock", "hard_unlock" },
            { "hard_ironclad", "hard_ironclad" },
            { "hard_world", "hard_world" },
        };

        /// <summary>
        /// Get the art resource key for a group (e.g., "levels", "matches", "world_levels")
        /// </summary>
        public string GetGroupArtKey(string groupId)
        {
            if (groupArtKeys.TryGetValue(groupId, out var key))
                return key;

            // Per-world groups: "{worldId}_levels" -> "world_levels", "{worldId}_stars" -> "world_stars"
            foreach (var worldId in RegisteredWorlds)
            {
                if (groupId == $"{worldId}_levels") return "world_levels";
                if (groupId == $"{worldId}_stars") return "world_stars";
            }

            return groupId;
        }

        /// <summary>
        /// Get current tier for a group based on unlocked milestones (null if none unlocked = grey)
        /// </summary>
        public AchievementTier? GetGroupCurrentTier(string groupId)
        {
            var group = GetAchievementsByGroup(groupId);
            AchievementTier? highestTier = null;

            // Walk in order; the last unlocked milestone determines the tier
            for (int i = group.Count - 1; i >= 0; i--)
            {
                var prog = GetProgress(group[i].id);
                if (prog != null && prog.isUnlocked)
                {
                    highestTier = group[i].tier;
                    break;
                }
            }

            return highestTier;
        }

        /// <summary>
        /// Get next uncompleted milestone in a group (or null if all complete)
        /// </summary>
        public Achievement GetNextMilestone(string groupId)
        {
            var group = GetAchievementsByGroup(groupId);
            foreach (var a in group)
            {
                var prog = GetProgress(a.id);
                if (prog == null || !prog.isUnlocked)
                    return a;
            }
            return null;
        }

        /// <summary>
        /// Get the current progress value for the group (shared across milestones in group)
        /// </summary>
        public int GetGroupProgress(string groupId)
        {
            var group = GetAchievementsByGroup(groupId);
            int maxProgress = 0;
            foreach (var a in group)
            {
                var prog = GetProgress(a.id);
                if (prog != null && prog.currentValue > maxProgress)
                    maxProgress = prog.currentValue;
            }
            return maxProgress;
        }

        /// <summary>
        /// Get date of most recently unlocked milestone in group
        /// </summary>
        public DateTime? GetGroupLastUnlockDate(string groupId)
        {
            var group = GetAchievementsByGroup(groupId);
            DateTime? latest = null;
            foreach (var a in group)
            {
                var prog = GetProgress(a.id);
                if (prog != null && prog.isUnlocked)
                {
                    if (latest == null || prog.unlockedAt > latest.Value)
                        latest = prog.unlockedAt;
                }
            }
            return latest;
        }

        /// <summary>
        /// Get all recently unlocked achievements sorted by date descending
        /// </summary>
        public List<(Achievement achievement, AchievementProgress progress)> GetRecentlyUnlocked(int maxCount = 50)
        {
            var result = new List<(Achievement, AchievementProgress)>();
            foreach (var a in achievements.Values)
            {
                var prog = GetProgress(a.id);
                if (prog != null && prog.isUnlocked)
                {
                    result.Add((a, prog));
                }
            }
            result.Sort((a, b) => b.Item2.unlockedAt.CompareTo(a.Item2.unlockedAt));
            if (result.Count > maxCount)
                result.RemoveRange(maxCount, result.Count - maxCount);
            return result;
        }

        /// <summary>
        /// Get all unique group IDs for a specific tab
        /// </summary>
        public List<string> GetGroupIdsForTab(string tab)
        {
            var groups = new List<string>();
            var seen = new HashSet<string>();
            foreach (var a in achievements.Values)
            {
                if (!string.IsNullOrEmpty(a.groupId) && a.tab == tab && seen.Add(a.groupId))
                {
                    groups.Add(a.groupId);
                }
            }
            return groups;
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
                visitedWorlds = new List<string>(visitedWorlds),
                // New persistent counters
                totalCombos = totalCombos,
                totalPowerUpsUsed = totalPowerUpsUsed,
                totalSimultaneousPowerUps = totalSimultaneousPowerUps,
                totalComboTimerBonus = totalComboTimerBonus,
                totalPhotoFinishes = totalPhotoFinishes,
                totalNegativeTimeCompletions = totalNegativeTimeCompletions,
                bestPowerUpsInOneLevel = bestPowerUpsInOneLevel,
                bestComboTimerChain = bestComboTimerChain,
                threeStarStreak = threeStarStreak,
                hardModeStreak = hardModeStreak,
                // Original stats (now persisted)
                totalMatchesMade = totalMatchesMade,
                totalMovesMade = totalMovesMade,
                totalStarsEarned = totalStarsEarned,
                totalPlaytimeSeconds = totalPlaytimeSeconds,
                // Stats screen counters
                totalLevelsFailed = totalLevelsFailed,
                totalLevelsRestarted = totalLevelsRestarted,
                totalGoodCombos = totalGoodCombos,
                totalAmazingCombos = totalAmazingCombos,
                totalPerfectCombos = totalPerfectCombos,
                bestComboStreak = bestComboStreak,
                powerUpsUsedSwap = powerUpsUsedSwap,
                powerUpsUsedDestroyLocker = powerUpsUsedDestroyLocker,
                powerUpsUsedMoveFreeze = powerUpsUsedMoveFreeze,
                powerUpsUsedTimeFreeze = powerUpsUsedTimeFreeze,
                dailyLoginCount = dailyLoginCount,
                lastLoginDate = lastLoginDate
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

                // Load new persistent counters (default to 0 for backward compatibility)
                totalCombos = saveData.totalCombos;
                totalPowerUpsUsed = saveData.totalPowerUpsUsed;
                totalSimultaneousPowerUps = saveData.totalSimultaneousPowerUps;
                totalComboTimerBonus = saveData.totalComboTimerBonus;
                totalPhotoFinishes = saveData.totalPhotoFinishes;
                totalNegativeTimeCompletions = saveData.totalNegativeTimeCompletions;
                bestPowerUpsInOneLevel = saveData.bestPowerUpsInOneLevel;
                bestComboTimerChain = saveData.bestComboTimerChain;
                threeStarStreak = saveData.threeStarStreak;
                hardModeStreak = saveData.hardModeStreak;

                // Original stats (now persisted, default to 0 for backward compatibility)
                totalMatchesMade = saveData.totalMatchesMade;
                totalMovesMade = saveData.totalMovesMade;
                totalStarsEarned = saveData.totalStarsEarned;
                totalPlaytimeSeconds = saveData.totalPlaytimeSeconds;

                // Stats screen counters (default to 0 for backward compatibility)
                totalLevelsFailed = saveData.totalLevelsFailed;
                totalLevelsRestarted = saveData.totalLevelsRestarted;
                totalGoodCombos = saveData.totalGoodCombos;
                totalAmazingCombos = saveData.totalAmazingCombos;
                totalPerfectCombos = saveData.totalPerfectCombos;
                bestComboStreak = saveData.bestComboStreak;
                powerUpsUsedSwap = saveData.powerUpsUsedSwap;
                powerUpsUsedDestroyLocker = saveData.powerUpsUsedDestroyLocker;
                powerUpsUsedMoveFreeze = saveData.powerUpsUsedMoveFreeze;
                powerUpsUsedTimeFreeze = saveData.powerUpsUsedTimeFreeze;
                dailyLoginCount = saveData.dailyLoginCount;
                lastLoginDate = saveData.lastLoginDate ?? "";

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
            totalMatchesMade = 0;
            totalMovesMade = 0;
            totalStarsEarned = 0;
            totalPlaytimeSeconds = 0;
            totalCombos = 0;
            totalPowerUpsUsed = 0;
            totalSimultaneousPowerUps = 0;
            totalComboTimerBonus = 0;
            totalPhotoFinishes = 0;
            totalNegativeTimeCompletions = 0;
            bestPowerUpsInOneLevel = 0;
            bestComboTimerChain = 0;
            threeStarStreak = 0;
            hardModeStreak = 0;
            totalLevelsFailed = 0;
            totalLevelsRestarted = 0;
            totalGoodCombos = 0;
            totalAmazingCombos = 0;
            totalPerfectCombos = 0;
            bestComboStreak = 0;
            powerUpsUsedSwap = 0;
            powerUpsUsedDestroyLocker = 0;
            powerUpsUsedMoveFreeze = 0;
            powerUpsUsedTimeFreeze = 0;
            dailyLoginCount = 0;
            lastLoginDate = "";

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
            // Original stats (now persisted)
            public int totalMatchesMade;
            public int totalMovesMade;
            public int totalStarsEarned;
            public float totalPlaytimeSeconds;
            // New persistent counters (default to 0 for backward compatibility)
            public int totalCombos;
            public int totalPowerUpsUsed;
            public int totalSimultaneousPowerUps;
            public int totalComboTimerBonus;
            public int totalPhotoFinishes;
            public int totalNegativeTimeCompletions;
            public int bestPowerUpsInOneLevel;
            public int bestComboTimerChain;
            public int threeStarStreak;
            public int hardModeStreak;
            // Stats screen counters
            public int totalLevelsFailed;
            public int totalLevelsRestarted;
            public int totalGoodCombos;
            public int totalAmazingCombos;
            public int totalPerfectCombos;
            public int bestComboStreak;
            public int powerUpsUsedSwap;
            public int powerUpsUsedDestroyLocker;
            public int powerUpsUsedMoveFreeze;
            public int powerUpsUsedTimeFreeze;
            public int dailyLoginCount;
            public string lastLoginDate;
        }

        #endregion
    }
}
