using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.7f);

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
        }

        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnGameResumed -= OnGameResumed;
        }

        private void OnGamePaused()
        {
            Show();
        }

        private void OnGameResumed()
        {
            Hide();
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
            GameManager.Instance?.RestartLevel();
        }

        private void OnSettingsClicked()
        {
            PlayButtonSound();
            GameEvents.InvokeSettingsOpened();
        }

        private void OnExitClicked()
        {
            PlayButtonSound();
            Hide(true);
            GameManager.Instance?.GoToLevelSelection(GameManager.Instance.CurrentWorldId);
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
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
