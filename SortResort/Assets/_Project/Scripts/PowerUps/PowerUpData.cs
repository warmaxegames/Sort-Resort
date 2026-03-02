using System.Collections.Generic;

namespace SortResort
{
    public enum PowerUpType
    {
        SwapItems = 0,
        DestroyLocker = 1,
        MoveFreeze = 2,
        TimeFreeze = 3
    }

    public static class PowerUpData
    {
        public const int POWER_UP_COUNT = 4;
        public const int INITIAL_GRANT = 3;

        public struct PowerUpConfig
        {
            public PowerUpType type;
            public string spriteName;
            public string pressedSpriteName;
            public string displayName;
            public string description;
            public GameMode[] availableModes;
            public int unlockLevel;
            public bool requiresLockedContainers;
        }

        private static readonly PowerUpConfig[] configs = new PowerUpConfig[]
        {
            new PowerUpConfig
            {
                type = PowerUpType.SwapItems,
                spriteName = "swap_items",
                pressedSpriteName = "swap_items_pressed",
                displayName = "Swap Items",
                description = "Select two front-row items to swap their positions. Doesn't count as a move!",
                availableModes = new[] { GameMode.FreePlay, GameMode.StarMode, GameMode.TimerMode, GameMode.HardMode },
                unlockLevel = 5,
                requiresLockedContainers = false
            },
            new PowerUpConfig
            {
                type = PowerUpType.DestroyLocker,
                spriteName = "destroy_locker",
                pressedSpriteName = "destroy_locker_pressed",
                displayName = "Destroy Locker",
                description = "Tap a locked container to instantly unlock it!",
                availableModes = new[] { GameMode.FreePlay, GameMode.StarMode, GameMode.TimerMode, GameMode.HardMode },
                unlockLevel = 11,
                requiresLockedContainers = true
            },
            new PowerUpConfig
            {
                type = PowerUpType.MoveFreeze,
                spriteName = "moves_freeze",
                pressedSpriteName = "moves_freeze_pressed",
                displayName = "Moves Freeze",
                description = "Freeze the move counter for 5 seconds! Make moves without them counting.",
                availableModes = new[] { GameMode.StarMode, GameMode.HardMode },
                unlockLevel = 5,
                requiresLockedContainers = false
            },
            new PowerUpConfig
            {
                type = PowerUpType.TimeFreeze,
                spriteName = "time_freeze",
                pressedSpriteName = "time_freeze_pressed",
                displayName = "Time Freeze",
                description = "Freeze the timer for 10 seconds!",
                availableModes = new[] { GameMode.TimerMode, GameMode.HardMode },
                unlockLevel = 5,
                requiresLockedContainers = false
            }
        };

        public static PowerUpConfig GetConfig(PowerUpType type)
        {
            return configs[(int)type];
        }

        public static bool IsAvailableInMode(PowerUpType type, GameMode mode)
        {
            var config = GetConfig(type);
            foreach (var m in config.availableModes)
            {
                if (m == mode) return true;
            }
            return false;
        }

        public static bool IsAvailableForLevel(PowerUpType type, GameMode mode, LevelData levelData)
        {
            if (!IsAvailableInMode(type, mode)) return false;

            var config = GetConfig(type);

            // Destroy Locker only available on levels with locked containers
            if (config.requiresLockedContainers && levelData != null)
            {
                bool hasLocked = false;
                if (levelData.containers != null)
                {
                    foreach (var container in levelData.containers)
                    {
                        if (container.is_locked)
                        {
                            hasLocked = true;
                            break;
                        }
                    }
                }
                if (!hasLocked) return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a power-up should be unlocked at this level.
        /// Unlock conditions are mode-specific for MoveFreeze and TimeFreeze.
        /// </summary>
        public static bool ShouldUnlockAtLevel(PowerUpType type, int levelNumber, GameMode mode)
        {
            var config = GetConfig(type);
            if (levelNumber < config.unlockLevel) return false;

            // MoveFreeze unlocks only in StarMode or HardMode
            if (type == PowerUpType.MoveFreeze)
                return mode == GameMode.StarMode || mode == GameMode.HardMode;

            // TimeFreeze unlocks only in TimerMode or HardMode
            if (type == PowerUpType.TimeFreeze)
                return mode == GameMode.TimerMode || mode == GameMode.HardMode;

            // SwapItems and DestroyLocker unlock in any mode
            return true;
        }
    }
}
