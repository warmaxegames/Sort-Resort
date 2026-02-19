using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Categories of achievements
    /// </summary>
    public enum AchievementCategory
    {
        Progression,    // Complete levels, worlds
        Mastery,        // 3-star levels, perfect runs
        Speed,          // Timer-based (only when timer enabled)
        Efficiency,     // Low move counts
        Exploration,    // Play different worlds, find secrets
        Challenge,      // Special conditions
        Collection,     // Match specific items
        Milestone       // Total stats milestones
    }

    /// <summary>
    /// Reward tiers for achievements
    /// </summary>
    public enum AchievementTier
    {
        Bronze,     // Easy achievements - small rewards
        Silver,     // Medium achievements - moderate rewards
        Gold,       // Hard achievements - good rewards
        Platinum    // Very hard achievements - best rewards
    }

    /// <summary>
    /// How progress is tracked for an achievement
    /// </summary>
    public enum AchievementTrackingType
    {
        /// <summary>
        /// Counts total occurrences (e.g., "Make 100 matches" - same match can count multiple times)
        /// </summary>
        Total,

        /// <summary>
        /// Counts unique identifiers (e.g., "Complete 10 levels" - each level only counts once)
        /// </summary>
        Unique,

        /// <summary>
        /// One-time trigger (e.g., "First Match" - unlocks immediately on first occurrence)
        /// </summary>
        OneTime
    }

    /// <summary>
    /// Types of rewards that can be given
    /// </summary>
    public enum RewardType
    {
        Coins,          // Premium currency
        UndoToken,      // +1 undo use
        SkipToken,      // Skip a level
        UnlockKey,      // Unlock a locked container
        FreezeToken,    // Freeze conveyors
        TimerFreeze,    // Freeze timer
        Cosmetic,       // Unlock cosmetic item
        Trophy          // Trophy for trophy room
    }

    /// <summary>
    /// Defines a reward for completing an achievement
    /// </summary>
    [Serializable]
    public class AchievementReward
    {
        public RewardType type;
        public int amount;
        public string cosmeticId; // For cosmetic rewards

        public AchievementReward(RewardType type, int amount, string cosmeticId = null)
        {
            this.type = type;
            this.amount = amount;
            this.cosmeticId = cosmeticId;
        }
    }

    /// <summary>
    /// Defines an achievement
    /// </summary>
    [Serializable]
    public class Achievement
    {
        // Reserved tab names
        public const string TAB_ALL = "all";
        public const string TAB_RECENT = "recent";
        public const string TAB_GENERAL = "general";

        public string id;
        public string name;
        public string description;
        public AchievementCategory category;
        public AchievementTier tier;
        public AchievementTrackingType trackingType; // How progress is counted
        public bool requiresTimer;      // Only achievable when timer is enabled
        public bool isHidden;           // Don't show until unlocked
        public string iconPath;         // Path to achievement icon

        // Requirements
        public int targetValue;         // Target to reach (e.g., 10 levels, 100 matches)
        public string targetWorldId;    // Specific world requirement (null = any)
        public string targetItemId;     // Specific item requirement (null = any)

        // Grouping - achievements with same groupId are displayed together
        public string groupId;          // Group ID for related achievements (e.g., "levels" for complete 1,2,3 levels)
        public int groupOrder;          // Order within group (1, 2, 3...)

        // Points
        public int points;              // Achievement points earned when unlocked

        // UI Tab - string-based for extensibility (use worldId for world-specific, or TAB_GENERAL)
        public string tab;              // Which tab this achievement appears under

        // Rewards
        public AchievementReward[] rewards;

        public Achievement(
            string id,
            string name,
            string description,
            AchievementCategory category,
            AchievementTier tier,
            int targetValue,
            AchievementReward[] rewards,
            AchievementTrackingType trackingType = AchievementTrackingType.Total,
            bool requiresTimer = false,
            bool isHidden = false,
            string targetWorldId = null,
            string targetItemId = null,
            string groupId = null,
            int groupOrder = 0,
            int points = -1,  // -1 means auto-calculate from tier
            string tab = TAB_GENERAL)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.category = category;
            this.tier = tier;
            this.targetValue = targetValue;
            this.rewards = rewards;
            this.trackingType = trackingType;
            this.requiresTimer = requiresTimer;
            this.isHidden = isHidden;
            this.targetWorldId = targetWorldId;
            this.targetItemId = targetItemId;
            this.groupId = groupId;
            this.groupOrder = groupOrder;
            this.tab = tab;
            this.iconPath = $"Sprites/Achievements/{id}";

            // Auto-calculate points from tier if not specified
            if (points < 0)
            {
                this.points = GetPointsForTier(tier);
            }
            else
            {
                this.points = points;
            }
        }

        public static int GetPointsForTier(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Bronze: return 10;
                case AchievementTier.Silver: return 25;
                case AchievementTier.Gold: return 50;
                case AchievementTier.Platinum: return 100;
                default: return 10;
            }
        }

        /// <summary>
        /// Get display name for a tab (converts world IDs to proper names)
        /// </summary>
        public static string GetTabDisplayName(string tab)
        {
            if (string.IsNullOrEmpty(tab)) return "General";
            if (tab == TAB_ALL) return "All";
            if (tab == TAB_RECENT) return "Recent";
            if (tab == TAB_GENERAL) return "General";
            // Capitalize first letter for world names
            return char.ToUpper(tab[0]) + tab.Substring(1).ToLower();
        }

        /// <summary>
        /// Get tab label for the vertical sprite tabs (uses world display names)
        /// </summary>
        public static string GetTabShortName(string tab)
        {
            if (string.IsNullOrEmpty(tab) || tab == TAB_GENERAL) return "GENERAL";
            switch (tab)
            {
                case "island": return "ST. GAMES\nISLAND";
                case "supermarket": return "SUPER\nSTORE";
                case "farm": return "WILTY\nACRES";
                case "tavern": return "THE OINK\n& ANCHOR";
                case "space": return "SPACE\nSTATION";
                default: return tab.ToUpper();
            }
        }
    }

    /// <summary>
    /// Tracks player progress on a specific achievement
    /// </summary>
    [Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public int currentValue;
        public bool isUnlocked;
        public DateTime unlockedAt;

        // For Unique tracking type - stores identifiers that have been counted
        // (e.g., level IDs for "complete 10 unique levels")
        public List<string> uniqueIdentifiers;

        public AchievementProgress(string achievementId)
        {
            this.achievementId = achievementId;
            this.currentValue = 0;
            this.isUnlocked = false;
            this.uniqueIdentifiers = new List<string>();
        }

        public float GetProgressPercent(int targetValue)
        {
            if (targetValue <= 0) return isUnlocked ? 1f : 0f;
            return Mathf.Clamp01((float)currentValue / targetValue);
        }

        /// <summary>
        /// For Unique tracking: tries to add an identifier. Returns true if new, false if already counted.
        /// </summary>
        public bool TryAddUniqueIdentifier(string identifier)
        {
            if (uniqueIdentifiers == null)
                uniqueIdentifiers = new List<string>();

            if (uniqueIdentifiers.Contains(identifier))
                return false;

            uniqueIdentifiers.Add(identifier);
            return true;
        }

        /// <summary>
        /// For Unique tracking: checks if an identifier has already been counted.
        /// </summary>
        public bool HasUniqueIdentifier(string identifier)
        {
            return uniqueIdentifiers != null && uniqueIdentifiers.Contains(identifier);
        }
    }
}
