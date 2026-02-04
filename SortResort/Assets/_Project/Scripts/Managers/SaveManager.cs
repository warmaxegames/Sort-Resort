using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_KEY = "SortResortSaveData";

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

                    // Migrate old save data - check if timerEnabled field existed
                    // JsonUtility sets missing bools to false, but we want true as default
                    if (!json.Contains("timerEnabled"))
                    {
                        currentSaveData.timerEnabled = true;
                        Debug.Log("[SaveManager] Migrated old save: timerEnabled set to true");
                    }

                    Debug.Log("[SaveManager] Game loaded successfully");
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

        // Level Progress
        public void SaveLevelProgress(string worldId, int levelNumber, int stars)
        {
            var worldProgress = GetOrCreateWorldProgress(worldId);

            string levelKey = $"{worldId}_{levelNumber}";
            var existingLevel = worldProgress.levelProgress.Find(l => l.levelKey == levelKey);

            if (existingLevel != null)
            {
                if (stars > existingLevel.starsEarned)
                {
                    existingLevel.starsEarned = stars;
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
                    completionCount = 1
                });
            }

            if (levelNumber > worldProgress.highestLevelCompleted)
            {
                worldProgress.highestLevelCompleted = levelNumber;
            }

            RecalculateTotalStars();
            SaveGame();

            GameEvents.InvokeStarsEarned(currentSaveData.totalStars);
        }

        public int GetLevelStars(string worldId, int levelNumber)
        {
            var worldProgress = GetWorldProgress(worldId);
            if (worldProgress == null) return 0;

            string levelKey = $"{worldId}_{levelNumber}";
            var level = worldProgress.levelProgress.Find(l => l.levelKey == levelKey);
            return level?.starsEarned ?? 0;
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

        public bool IsWorldUnlocked(string worldId)
        {
            return currentSaveData.unlockedWorlds.Contains(worldId);
        }

        public int GetWorldCompletedLevelCount(string worldId)
        {
            var worldProgress = GetWorldProgress(worldId);
            return worldProgress?.levelProgress.Count ?? 0;
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

        // Timer Setting
        public bool IsTimerEnabled()
        {
            return currentSaveData.timerEnabled;
        }

        public void SetTimerEnabled(bool enabled)
        {
            currentSaveData.timerEnabled = enabled;
            SaveGame();
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

        // Helper Methods
        private WorldProgress GetWorldProgress(string worldId)
        {
            return currentSaveData.worldProgress.Find(w => w.worldId == worldId);
        }

        private WorldProgress GetOrCreateWorldProgress(string worldId)
        {
            var existing = GetWorldProgress(worldId);
            if (existing != null) return existing;

            var newProgress = new WorldProgress { worldId = worldId };
            currentSaveData.worldProgress.Add(newProgress);
            return newProgress;
        }

        private void RecalculateTotalStars()
        {
            int total = 0;
            foreach (var world in currentSaveData.worldProgress)
            {
                foreach (var level in world.levelProgress)
                {
                    total += level.starsEarned;
                }
            }
            currentSaveData.totalStars = total;
        }
    }

    [Serializable]
    public class SaveData
    {
        public string playerId;
        public int totalStars;
        public List<string> unlockedWorlds = new List<string> { "island" }; // Island unlocked by default
        public List<WorldProgress> worldProgress = new List<WorldProgress>();
        public List<string> seenDialogues = new List<string>();
        public DateTime lastPlayedTime;

        // Settings
        public bool hapticsEnabled = true;
        public bool timerEnabled = true; // Timer countdown feature (can be disabled for relaxed gameplay)
        public bool voiceEnabled = true; // Mascot dialogue voices

        public SaveData()
        {
            playerId = System.Guid.NewGuid().ToString();
            lastPlayedTime = DateTime.Now;
            hapticsEnabled = true;
            timerEnabled = true;
            voiceEnabled = true;
        }
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
    }
}
