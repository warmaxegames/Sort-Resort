using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SortResort
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_KEY = "SortResortSaveData";
        private const int CURRENT_SAVE_VERSION = 2;

        [SerializeField] private SaveData currentSaveData;

        public SaveData CurrentSave => currentSaveData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadGame();
        }

        // Save/Load Operations
        public void SaveGame()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSaveData);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Game saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save game: {e.Message}");
            }
        }

        public void LoadGame()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    currentSaveData = JsonUtility.FromJson<SaveData>(json);

                    // If save version is old or missing, wipe to fresh data
                    if (currentSaveData.saveVersion < CURRENT_SAVE_VERSION)
                    {
                        Debug.Log($"[SaveManager] Old save version {currentSaveData.saveVersion}, resetting to v{CURRENT_SAVE_VERSION}");
                        currentSaveData = new SaveData();
                        SaveGame();
                    }
                    else
                    {
                        Debug.Log("[SaveManager] Game loaded successfully");
                    }
                }
                else
                {
                    currentSaveData = new SaveData();
                    Debug.Log("[SaveManager] No save found, created new save data");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load game: {e.Message}");
                currentSaveData = new SaveData();
            }
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            currentSaveData = new SaveData();
            Debug.Log("[SaveManager] Save data deleted");
        }

        // Game Mode
        public GameMode GetActiveGameMode()
        {
            return currentSaveData.activeGameMode;
        }

        public void SetActiveGameMode(GameMode mode)
        {
            if (currentSaveData.activeGameMode != mode)
            {
                currentSaveData.activeGameMode = mode;
                SaveGame();
                GameEvents.InvokeGameModeChanged(mode);
            }
        }

        // Level Progress (operates on active game mode)
        public void SaveLevelProgress(string worldId, int levelNumber, int stars, float timeTaken = 0f)
        {
            var worldProgress = GetOrCreateWorldProgress(worldId);

            string levelKey = $"{worldId}_{levelNumber}";
            var existingLevel = worldProgress.levelProgress.Find(l => l.levelKey == levelKey);

            bool isNewBestTime = false;
            bool isNewBestStars = false;

            if (existingLevel != null)
            {
                if (stars > existingLevel.starsEarned)
                {
                    isNewBestStars = true;
                    existingLevel.starsEarned = stars;
                }
                if (timeTaken > 0 && (existingLevel.bestTime <= 0 || timeTaken < existingLevel.bestTime))
                {
                    existingLevel.bestTime = timeTaken;
                    isNewBestTime = true;
                }
                existingLevel.completionCount++;
            }
            else
            {
                worldProgress.levelProgress.Add(new LevelProgress
                {
                    levelKey = levelKey,
                    levelNumber = levelNumber,
                    starsEarned = stars,
                    isCompleted = true,
                    completionCount = 1,
                    bestTime = timeTaken
                });
                if (timeTaken > 0) isNewBestTime = true;
                if (stars > 0) isNewBestStars = true;
            }

            if (levelNumber > worldProgress.highestLevelCompleted)
            {
                worldProgress.highestLevelCompleted = levelNumber;
            }

            RecalculateTotalStars();
            SaveGame();

            GameEvents.InvokeStarsEarned(currentSaveData.totalStars);

            // Fire detailed completion event
            var mode = currentSaveData.activeGameMode;
            GameEvents.InvokeLevelCompletedDetailed(new LevelCompletionData
            {
                levelNumber = levelNumber,
                starsEarned = stars,
                timeTaken = timeTaken,
                mode = mode,
                isNewBestTime = isNewBestTime,
                isNewBestStars = isNewBestStars
            });
        }

        public int GetLevelStars(string worldId, int levelNumber)
        {
            var worldProgress = GetWorldProgress(worldId);
            if (worldProgress == null) return 0;

            string levelKey = $"{worldId}_{levelNumber}";
            var level = worldProgress.levelProgress.Find(l => l.levelKey == levelKey);
            return level?.starsEarned ?? 0;
        }

        public float GetLevelBestTime(string worldId, int levelNumber)
        {
            var worldProgress = GetWorldProgress(worldId);
            if (worldProgress == null) return 0f;

            string levelKey = $"{worldId}_{levelNumber}";
            var level = worldProgress.levelProgress.Find(l => l.levelKey == levelKey);
            return level?.bestTime ?? 0f;
        }

        public bool IsLevelCompleted(string worldId, int levelNumber)
        {
            var worldProgress = GetWorldProgress(worldId);
            if (worldProgress == null) return false;

            string levelKey = $"{worldId}_{levelNumber}";
            var level = worldProgress.levelProgress.Find(l => l.levelKey == levelKey);
            return level?.isCompleted ?? false;
        }

        public bool IsLevelUnlocked(string worldId, int levelNumber)
        {
            if (levelNumber == 1) return true;

            return IsLevelCompleted(worldId, levelNumber - 1);
        }

        public int GetHighestLevelCompleted(string worldId)
        {
            var worldProgress = GetWorldProgress(worldId);
            return worldProgress?.highestLevelCompleted ?? 0;
        }

        // World Progress
        public void UnlockWorld(string worldId)
        {
            if (!currentSaveData.unlockedWorlds.Contains(worldId))
            {
                currentSaveData.unlockedWorlds.Add(worldId);
                SaveGame();
                GameEvents.InvokeWorldUnlocked(worldId);
            }
        }

        public void LockWorld(string worldId)
        {
            if (currentSaveData.unlockedWorlds.Contains(worldId))
            {
                currentSaveData.unlockedWorlds.Remove(worldId);
                SaveGame();
                Debug.Log($"[SaveManager] World locked: {worldId}");
            }
        }

        public bool IsWorldUnlocked(string worldId)
        {
            return currentSaveData.unlockedWorlds.Contains(worldId);
        }

        public int GetWorldCompletedLevelCount(string worldId)
        {
            var worldProgress = GetWorldProgress(worldId);
            return worldProgress?.levelProgress.Count ?? 0;
        }

        /// <summary>
        /// Count unique completed levels across ALL modes for a world.
        /// Used for world unlock checks (shared across modes).
        /// </summary>
        public int GetWorldCompletedLevelCountAnyMode(string worldId)
        {
            var completedLevels = new HashSet<int>();
            foreach (var modeProg in currentSaveData.modeProgress)
            {
                var wp = modeProg.worldProgress.Find(w => w.worldId == worldId);
                if (wp != null)
                {
                    foreach (var lp in wp.levelProgress)
                    {
                        if (lp.isCompleted) completedLevels.Add(lp.levelNumber);
                    }
                }
            }
            return completedLevels.Count;
        }

        public int GetWorldTotalStars(string worldId)
        {
            var worldProgress = GetWorldProgress(worldId);
            if (worldProgress == null) return 0;

            int total = 0;
            foreach (var level in worldProgress.levelProgress)
            {
                total += level.starsEarned;
            }
            return total;
        }

        // Hard Mode unlock check
        public bool IsHardModeUnlocked(string worldId)
        {
            var starProgress = GetModeWorldProgress(GameMode.StarMode, worldId);
            int starCompleted = starProgress?.levelProgress.Count(l => l.isCompleted) ?? 0;

            var timerProgress = GetModeWorldProgress(GameMode.TimerMode, worldId);
            int timerCompleted = timerProgress?.levelProgress.Count(l => l.isCompleted) ?? 0;

            return starCompleted >= 100 && timerCompleted >= 100;
        }

        // Dialogue Tracking
        public void MarkDialogueSeen(string dialogueId)
        {
            if (!currentSaveData.seenDialogues.Contains(dialogueId))
            {
                currentSaveData.seenDialogues.Add(dialogueId);
                SaveGame();
            }
        }

        public bool HasSeenDialogue(string dialogueId)
        {
            return currentSaveData.seenDialogues.Contains(dialogueId);
        }

        // Haptics Setting
        public bool IsHapticsEnabled()
        {
            return currentSaveData.hapticsEnabled;
        }

        public void SetHapticsEnabled(bool enabled)
        {
            currentSaveData.hapticsEnabled = enabled;
            SaveGame();
        }

        // Timer Setting (kept for backward compatibility, now derived from mode)
        public bool IsTimerEnabled()
        {
            var mode = currentSaveData.activeGameMode;
            return mode == GameMode.TimerMode || mode == GameMode.HardMode;
        }

        // Voice Setting
        public bool IsVoiceEnabled()
        {
            return currentSaveData.voiceEnabled;
        }

        public void SetVoiceEnabled(bool enabled)
        {
            currentSaveData.voiceEnabled = enabled;
            SaveGame();
        }

        // Reset All Progress
        public void ResetAllProgress()
        {
            currentSaveData = new SaveData();
            SaveGame();

            // Also reset played dialogues (stored separately in PlayerPrefs)
            DialogueManager.Instance?.ResetPlayedDialogues();

            Debug.Log("[SaveManager] All progress has been reset (including dialogues)");
            GameEvents.InvokeProgressReset();
        }

        // Helper Methods - operate on active mode
        private WorldProgress GetWorldProgress(string worldId)
        {
            var modeProgress = GetOrCreateModeProgress(currentSaveData.activeGameMode);
            return modeProgress.worldProgress.Find(w => w.worldId == worldId);
        }

        private WorldProgress GetOrCreateWorldProgress(string worldId)
        {
            var modeProgress = GetOrCreateModeProgress(currentSaveData.activeGameMode);
            var existing = modeProgress.worldProgress.Find(w => w.worldId == worldId);
            if (existing != null) return existing;

            var newProgress = new WorldProgress { worldId = worldId };
            modeProgress.worldProgress.Add(newProgress);
            return newProgress;
        }

        /// <summary>
        /// Get world progress for a specific mode (not necessarily the active mode).
        /// </summary>
        public WorldProgress GetModeWorldProgress(GameMode mode, string worldId)
        {
            var modeProgress = currentSaveData.modeProgress.Find(m => m.mode == mode);
            return modeProgress?.worldProgress.Find(w => w.worldId == worldId);
        }

        private ModeProgress GetOrCreateModeProgress(GameMode mode)
        {
            var existing = currentSaveData.modeProgress.Find(m => m.mode == mode);
            if (existing != null) return existing;

            var newProgress = new ModeProgress { mode = mode };
            currentSaveData.modeProgress.Add(newProgress);
            return newProgress;
        }

        /// <summary>
        /// Clear all progress for a specific mode and world (used by debug tools).
        /// </summary>
        public void ClearModeWorldProgress(GameMode mode, string worldId)
        {
            var modeProgress = currentSaveData.modeProgress.Find(m => m.mode == mode);
            var worldProgress = modeProgress?.worldProgress.Find(w => w.worldId == worldId);
            if (worldProgress != null)
            {
                worldProgress.levelProgress.Clear();
                worldProgress.highestLevelCompleted = 0;
            }
            RecalculateTotalStars();
            SaveGame();
        }

        private void RecalculateTotalStars()
        {
            int total = 0;
            // Only count stars from modes that track them (StarMode and HardMode)
            foreach (var modeProg in currentSaveData.modeProgress)
            {
                if (modeProg.mode == GameMode.StarMode || modeProg.mode == GameMode.HardMode)
                {
                    foreach (var world in modeProg.worldProgress)
                    {
                        foreach (var level in world.levelProgress)
                        {
                            total += level.starsEarned;
                        }
                    }
                }
            }
            currentSaveData.totalStars = total;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int saveVersion = 2;
        public string playerId;
        public int totalStars;
        public GameMode activeGameMode = GameMode.FreePlay;
        public List<string> unlockedWorlds = new List<string> { "island" };
        public List<ModeProgress> modeProgress = new List<ModeProgress>();
        public List<string> seenDialogues = new List<string>();
        public DateTime lastPlayedTime;

        // Settings
        public bool hapticsEnabled = true;
        public bool voiceEnabled = true;

        public SaveData()
        {
            saveVersion = 2;
            playerId = System.Guid.NewGuid().ToString();
            lastPlayedTime = DateTime.Now;
            activeGameMode = GameMode.FreePlay;
            hapticsEnabled = true;
            voiceEnabled = true;
        }
    }

    [Serializable]
    public class ModeProgress
    {
        public GameMode mode;
        public List<WorldProgress> worldProgress = new List<WorldProgress>();
    }

    [Serializable]
    public class WorldProgress
    {
        public string worldId;
        public int highestLevelCompleted;
        public List<LevelProgress> levelProgress = new List<LevelProgress>();
    }

    [Serializable]
    public class LevelProgress
    {
        public string levelKey;
        public int levelNumber;
        public int starsEarned;
        public bool isCompleted;
        public int completionCount;
        public float bestTime; // Best completion time in seconds (Timer/Hard modes)
    }
}
