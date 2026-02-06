using System;
using UnityEngine;

namespace SortResort
{
    [Serializable]
    public struct LevelCompletionData
    {
        public int levelNumber;
        public int starsEarned;
        public float timeTaken;
        public GameMode mode;
        public bool isNewBestTime;
    }

    public static class GameEvents
    {
        // Game State Events
        public static event Action<GameState> OnGameStateChanged;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

        // Game Mode Events
        public static event Action<GameMode> OnGameModeChanged;

        // Level Events
        public static event Action<int> OnLevelStarted;
        public static event Action<int, int> OnLevelCompleted; // levelNumber, starsEarned
        public static event Action<LevelCompletionData> OnLevelCompletedDetailed; // full completion data with mode, time, etc.
        public static event Action<int, string> OnLevelFailed; // levelNumber, reason
        public static event Action OnLevelRestarted;

        // Gameplay Events
        public static event Action<int> OnMoveUsed; // currentMoveCount
        public static event Action<string> OnMatchMade; // itemId that was matched
        public static event Action<int> OnMatchCountChanged; // totalMatches in current level
        public static event Action<string> OnContainerUnlocked; // containerId

        // Progression Events
        public static event Action<string> OnWorldUnlocked; // worldId
        public static event Action<int> OnStarsEarned; // totalStars
        public static event Action<string, int> OnWorldSelected; // worldId, levelNumber
        public static event Action OnProgressReset; // when all progress is reset

        // UI Events
        public static event Action OnSettingsOpened;
        public static event Action OnSettingsClosed;
        public static event Action<float> OnMasterVolumeChanged;
        public static event Action<float> OnMusicVolumeChanged;
        public static event Action<float> OnSFXVolumeChanged;

        // Dialogue Events
        public static event Action<string> OnDialogueStarted; // dialogueId
        public static event Action<string> OnDialogueCompleted; // dialogueId
        public static event Action OnDialogueAdvanced;

        // Item Events
        public static event Action<GameObject> OnItemPickedUp;
        public static event Action<GameObject> OnItemDropped;
        public static event Action<GameObject> OnItemReturnedToOrigin;

        // Timer Events
        public static event Action<float> OnTimerUpdated; // timeRemaining
        public static event Action OnTimerExpired;
        public static event Action<bool> OnTimerFrozen; // isFrozen (for timer freeze power-up)

        // Invoke Methods - Game State
        public static void InvokeGameStateChanged(GameState newState) => OnGameStateChanged?.Invoke(newState);
        public static void InvokeGamePaused() => OnGamePaused?.Invoke();
        public static void InvokeGameResumed() => OnGameResumed?.Invoke();

        // Invoke Methods - Game Mode
        public static void InvokeGameModeChanged(GameMode mode) => OnGameModeChanged?.Invoke(mode);

        // Invoke Methods - Level
        public static void InvokeLevelStarted(int levelNumber) => OnLevelStarted?.Invoke(levelNumber);
        public static void InvokeLevelCompleted(int levelNumber, int stars) => OnLevelCompleted?.Invoke(levelNumber, stars);
        public static void InvokeLevelCompletedDetailed(LevelCompletionData data) => OnLevelCompletedDetailed?.Invoke(data);
        public static void InvokeLevelFailed(int levelNumber, string reason = null) => OnLevelFailed?.Invoke(levelNumber, reason);
        public static void InvokeLevelRestarted() => OnLevelRestarted?.Invoke();

        // Invoke Methods - Gameplay
        public static void InvokeMoveUsed(int moveCount) => OnMoveUsed?.Invoke(moveCount);
        public static void InvokeMatchMade(string itemId) => OnMatchMade?.Invoke(itemId);
        public static void InvokeMatchCountChanged(int totalMatches) => OnMatchCountChanged?.Invoke(totalMatches);
        public static void InvokeContainerUnlocked(string containerId) => OnContainerUnlocked?.Invoke(containerId);

        // Invoke Methods - Progression
        public static void InvokeWorldUnlocked(string worldId) => OnWorldUnlocked?.Invoke(worldId);
        public static void InvokeStarsEarned(int totalStars) => OnStarsEarned?.Invoke(totalStars);
        public static void InvokeWorldSelected(string worldId, int levelNumber) => OnWorldSelected?.Invoke(worldId, levelNumber);
        public static void InvokeProgressReset() => OnProgressReset?.Invoke();

        // Invoke Methods - UI
        public static void InvokeSettingsOpened() => OnSettingsOpened?.Invoke();
        public static void InvokeSettingsClosed() => OnSettingsClosed?.Invoke();
        public static void InvokeMasterVolumeChanged(float volume) => OnMasterVolumeChanged?.Invoke(volume);
        public static void InvokeMusicVolumeChanged(float volume) => OnMusicVolumeChanged?.Invoke(volume);
        public static void InvokeSFXVolumeChanged(float volume) => OnSFXVolumeChanged?.Invoke(volume);

        // Invoke Methods - Dialogue
        public static void InvokeDialogueStarted(string dialogueId) => OnDialogueStarted?.Invoke(dialogueId);
        public static void InvokeDialogueCompleted(string dialogueId) => OnDialogueCompleted?.Invoke(dialogueId);
        public static void InvokeDialogueAdvanced() => OnDialogueAdvanced?.Invoke();

        // Invoke Methods - Item
        public static void InvokeItemPickedUp(GameObject item) => OnItemPickedUp?.Invoke(item);
        public static void InvokeItemDropped(GameObject item) => OnItemDropped?.Invoke(item);
        public static void InvokeItemReturnedToOrigin(GameObject item) => OnItemReturnedToOrigin?.Invoke(item);

        // Invoke Methods - Timer
        public static void InvokeTimerUpdated(float timeRemaining) => OnTimerUpdated?.Invoke(timeRemaining);
        public static void InvokeTimerExpired() => OnTimerExpired?.Invoke();
        public static void InvokeTimerFrozen(bool isFrozen) => OnTimerFrozen?.Invoke(isFrozen);

        // Cleanup - call when changing scenes or resetting
        public static void ClearAllListeners()
        {
            OnGameStateChanged = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameModeChanged = null;
            OnLevelStarted = null;
            OnLevelCompleted = null;
            OnLevelCompletedDetailed = null;
            OnLevelFailed = null;
            OnLevelRestarted = null;
            OnMoveUsed = null;
            OnMatchMade = null;
            OnMatchCountChanged = null;
            OnContainerUnlocked = null;
            OnWorldUnlocked = null;
            OnStarsEarned = null;
            OnWorldSelected = null;
            OnProgressReset = null;
            OnSettingsOpened = null;
            OnSettingsClosed = null;
            OnMasterVolumeChanged = null;
            OnMusicVolumeChanged = null;
            OnSFXVolumeChanged = null;
            OnDialogueStarted = null;
            OnDialogueCompleted = null;
            OnDialogueAdvanced = null;
            OnItemPickedUp = null;
            OnItemDropped = null;
            OnItemReturnedToOrigin = null;
            OnTimerUpdated = null;
            OnTimerExpired = null;
            OnTimerFrozen = null;
        }
    }
}
