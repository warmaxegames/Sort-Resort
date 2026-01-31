using System;
using UnityEngine;
using UnityEngine.UI;

// Access parent namespace types
using SortResort;

namespace SortResort.UI
{
    public class PauseMenuScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Background")]
        [SerializeField] private Image dimBackground;
        [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.5f);

        // Events
        public event Action OnSettingsRequested;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();

            if (dimBackground != null)
            {
                dimBackground.color = dimColor;
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            GameEvents.OnGamePaused += OnGamePaused;
            GameEvents.OnGameResumed += OnGameResumed;
            GameEvents.OnSettingsClosed += OnSettingsClosed;
        }

        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnGameResumed -= OnGameResumed;
            GameEvents.OnSettingsClosed -= OnSettingsClosed;
        }

        private void OnGamePaused()
        {
            Show();
        }

        private void OnGameResumed()
        {
            Hide();
        }

        private void OnSettingsClosed()
        {
            // Re-show pause menu when settings is closed (if game is still paused)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            {
                Show();
            }
        }

        private void OnResumeClicked()
        {
            PlayButtonSound();
            GameManager.Instance?.ResumeGame();
        }

        private void OnRestartClicked()
        {
            PlayButtonSound();
            Hide(true);
            GameManager.Instance?.ResumeGame(); // Unpause first
            GameManager.Instance?.SetState(GameState.Playing);
            LevelManager.Instance?.RestartLevel();
        }

        private void OnSettingsClicked()
        {
            PlayButtonSound();
            Hide(true); // Hide pause menu while settings is open
            OnSettingsRequested?.Invoke();
            GameEvents.InvokeSettingsOpened();
        }

        private void OnExitClicked()
        {
            PlayButtonSound();
            Hide(true);
            GameManager.Instance?.ResumeGame(); // Unpause first
            GameManager.Instance?.GoToLevelSelection(GameManager.Instance?.CurrentWorldId ?? "island");
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        /// <summary>
        /// Initialize for runtime-created UI
        /// </summary>
        public void Initialize(Button resume, Button restart, Button settings, Button exit, Image dimBg)
        {
            resumeButton = resume;
            restartButton = restart;
            settingsButton = settings;
            exitButton = exit;
            dimBackground = dimBg;

            SetupButtons();

            if (dimBackground != null)
            {
                dimBackground.color = dimColor;
            }
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
