using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Tracks consecutive match-move streaks and spawns combo text effects.
    /// Static utility - no MonoBehaviour needed.
    /// </summary>
    public static class ComboTracker
    {
        private static int dropSequenceId;
        private static int lastDropWithMatch;
        private static int comboStreak;

        /// <summary>
        /// Call at the start of each item drop (before placing in slot).
        /// </summary>
        public static void NotifyDropStarted()
        {
            dropSequenceId++;
        }

        /// <summary>
        /// Call when a match occurs. Spawns combo text if streak >= 2.
        /// </summary>
        public static void NotifyMatch(Vector3 matchCenter)
        {
            // Chain match on same drop - don't increment combo again
            if (dropSequenceId == lastDropWithMatch)
                return;

            // Consecutive match: this drop immediately follows a previous match drop
            if (dropSequenceId == lastDropWithMatch + 1)
            {
                comboStreak++;
            }
            else
            {
                comboStreak = 1;
            }

            lastDropWithMatch = dropSequenceId;

            if (comboStreak >= 2)
            {
                ComboTextEffect.Spawn(matchCenter, comboStreak);
                ComboTimerBonus.Spawn(comboStreak);
            }
        }

        /// <summary>
        /// Reset combo state (call on level load/restart).
        /// </summary>
        public static void Reset()
        {
            dropSequenceId = 0;
            lastDropWithMatch = -1;
            comboStreak = 0;
        }
    }
}
