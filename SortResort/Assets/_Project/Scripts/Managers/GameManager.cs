using UnityEngine;
using UnityEngine.SceneManagement;

namespace SortResort
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.Loading;

        [Header("Current Session")]
        [SerializeField] private string currentWorldId;
        [SerializeField] private int currentLevelNumber;
        [SerializeField] private int currentMoveCount;
        [SerializeField] private int currentMatchCount;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string worldSelectionSceneName = "WorldSelection";
        [SerializeField] private string levelSelectionSceneName = "LevelSelection";
        [SerializeField] private string gameplaySceneName = "Gameplay";

        public GameState CurrentState => currentState;
        public string CurrentWorldId => currentWorldId;
        public int CurrentLevelNumber => currentLevelNumber;
        public int CurrentMoveCount => currentMoveCount;
        public int CurrentMatchCount => currentMatchCount;
        public GameMode CurrentGameMode => SaveManager.Instance?.GetActiveGameMode() ?? GameMode.FreePlay;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            SetState(GameState.Loading);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameEvents.OnLevelStarted += OnLevelStarted;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEvents.OnLevelStarted -= OnLevelStarted;
        }

        private void OnLevelStarted(int levelNumber)
        {
            // Reset counters when any level starts (ensures HUD displays correctly)
            currentLevelNumber = levelNumber;
            currentMoveCount = 0;
            currentMatchCount = 0;
            Debug.Log($"[GameManager] Level started: {currentWorldId} #{levelNumber}, counters reset");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene loaded: {scene.name}");

            if (scene.name == mainMenuSceneName)
            {
                SetState(GameState.MainMenu);
            }
            else if (scene.name == worldSelectionSceneName)
            {
                SetState(GameState.WorldSelection);
            }
            else if (scene.name == levelSelectionSceneName)
            {
                SetState(GameState.LevelSelection);
            }
            else if (scene.name == gameplaySceneName)
            {
                SetState(GameState.Playing);
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            var previousState = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] State changed: {previousState} -> {newState}");

            GameEvents.InvokeGameStateChanged(newState);

            HandleStateChange(previousState, newState);
        }

        private void HandleStateChange(GameState previousState, GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.LevelComplete:
                case GameState.LevelFailed:
                    Time.timeScale = 0f;
                    break;
                default:
                    Time.timeScale = 1f;
                    break;
            }
        }

        // Scene Navigation
        public void GoToMainMenu()
        {
            TransitionManager.Instance?.LoadScene(mainMenuSceneName);
        }

        public void GoToWorldSelection()
        {
            TransitionManager.Instance?.LoadScene(worldSelectionSceneName);
        }

        public void GoToLevelSelection(string worldId)
        {
            currentWorldId = worldId;
            TransitionManager.Instance?.LoadScene(levelSelectionSceneName);
        }

        public void StartLevel(string worldId, int levelNumber)
        {
            currentWorldId = worldId;
            currentLevelNumber = levelNumber;
            currentMoveCount = 0;
            currentMatchCount = 0;

            TransitionManager.Instance?.LoadScene(gameplaySceneName);
        }

        // Gameplay Methods
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
                GameEvents.InvokeGamePaused();
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                GameEvents.InvokeGameResumed();
            }
        }

        public void IncrementMoveCount()
        {
            currentMoveCount++;
            GameEvents.InvokeMoveUsed(currentMoveCount);

            // Only enforce move limits in modes that track stars (StarMode and HardMode)
            var mode = CurrentGameMode;
            bool enforcesMoveLimits = (mode == GameMode.StarMode || mode == GameMode.HardMode);
            if (!enforcesMoveLimits) return;

            // Check if player has used their second-to-last move without completing
            // This prevents the awkward case where the final move makes a match but still fails
            // Delay the check to allow any match animation to complete first (0.6s covers the 0.5s animation)
            var levelData = LevelManager.Instance?.CurrentLevel;
            if (levelData != null && currentMoveCount >= levelData.FailThreshold - 1)
            {
                StartCoroutine(DelayedFailureCheck());
            }
        }

        private System.Collections.IEnumerator DelayedFailureCheck()
        {
            // Wait for match animation to complete (0.5s animation + small buffer)
            yield return new WaitForSeconds(0.6f);

            // Re-check conditions after the delay (level might have been completed by a match)
            var levelData = LevelManager.Instance?.CurrentLevel;
            if (levelData != null && currentMoveCount >= levelData.FailThreshold - 1)
            {
                int itemsRemaining = LevelManager.Instance?.ItemsRemaining ?? 0;
                if (itemsRemaining > 0 && currentState == GameState.Playing)
                {
                    Debug.Log($"[GameManager] Level failed - out of moves ({currentMoveCount} >= {levelData.FailThreshold - 1}, items remaining: {itemsRemaining})");
                    FailLevel("Out of Moves");
                }
            }
        }

        public void DecrementMoveCount()
        {
            if (currentMoveCount > 0)
            {
                currentMoveCount--;
                GameEvents.InvokeMoveUsed(currentMoveCount);
            }
        }

        public void IncrementMatchCount(string itemId)
        {
            currentMatchCount++;
            GameEvents.InvokeMatchMade(itemId);
            GameEvents.InvokeMatchCountChanged(currentMatchCount);
        }

        public void CompleteLevel(int starsEarned, float timeTaken = 0f)
        {
            ComboTextEffect.DestroyAll();
            SetState(GameState.LevelComplete);
            SaveManager.Instance?.SaveLevelProgress(currentWorldId, currentLevelNumber, starsEarned, timeTaken);
            GameEvents.InvokeLevelCompleted(currentLevelNumber, starsEarned);

            // Check if this completion just unlocked Hard Mode for the world
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.CheckHardModeUnlock(currentWorldId);
            }
        }

        public void FailLevel(string reason = null)
        {
            ComboTextEffect.DestroyAll();
            SetState(GameState.LevelFailed);
            GameEvents.InvokeLevelFailed(currentLevelNumber, reason);
        }

        public void RestartLevel()
        {
            currentMoveCount = 0;
            currentMatchCount = 0;
            GameEvents.InvokeLevelRestarted();
            TransitionManager.Instance?.LoadScene(gameplaySceneName);
        }

        public void GoToNextLevel()
        {
            currentLevelNumber++;
            currentMoveCount = 0;
            currentMatchCount = 0;
            TransitionManager.Instance?.LoadScene(gameplaySceneName);
        }

        // Calculate stars based on move thresholds
        public int CalculateStars(int movesUsed, int[] thresholds)
        {
            if (thresholds == null || thresholds.Length < 3) return 1;

            if (movesUsed <= thresholds[0]) return 3;
            if (movesUsed <= thresholds[1]) return 2;
            if (movesUsed <= thresholds[2]) return 1;

            return 0; // Failed
        }
    }
}
