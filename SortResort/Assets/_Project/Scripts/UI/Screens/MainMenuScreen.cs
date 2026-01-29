using UnityEngine;
using UnityEngine.UI;

namespace SortResort.UI
{
    public class MainMenuScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Optional")]
        [SerializeField] private Image logoImage;
        [SerializeField] private Animator logoAnimator;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        protected override void OnShowComplete()
        {
            base.OnShowComplete();

            // Play logo animation if available
            if (logoAnimator != null)
            {
                logoAnimator.SetTrigger("Show");
            }
        }

        private void OnPlayClicked()
        {
            PlayButtonSound();
            GameManager.Instance?.GoToWorldSelection();
        }

        private void OnSettingsClicked()
        {
            PlayButtonSound();
            GameEvents.InvokeSettingsOpened();
        }

        private void OnQuitClicked()
        {
            PlayButtonSound();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
