using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Access parent namespace types
using SortResort;

namespace SortResort.UI
{
    public class SettingsScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button closeButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Volume Labels")]
        [SerializeField] private TextMeshProUGUI masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI musicVolumeLabel;
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel;

        [Header("Toggle Settings")]
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Image hapticsCheckmark;
        [SerializeField] private Toggle voiceToggle;
        [SerializeField] private Image voiceCheckmark;

        [Header("Buttons")]
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Button creditsButton;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        private TextMeshProUGUI confirmTitleText;
        private TextMeshProUGUI confirmMessageText;

        // Pending action tracking
        private enum PendingAction { None, ResetProgress }
        private PendingAction pendingAction = PendingAction.None;

        [Header("Credits Panel")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsCloseButton;

        [Header("Background")]
        [SerializeField] private Image dimBackground;
        [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.7f);

        // Events
        public event Action OnCreditsClicked;
        public event Action OnResetConfirmed;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SetupSliders();
            SetupToggles();

            if (dimBackground != null)
            {
                dimBackground.color = dimColor;
            }

            // Hide dialogs initially
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);
            if (creditsPanel != null)
                creditsPanel.SetActive(false);
        }

        private void SetupButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (resetProgressButton != null)
            {
                resetProgressButton.onClick.AddListener(OnResetProgressClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnCreditsButtonClicked);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.AddListener(OnConfirmNo);
            }

            if (creditsCloseButton != null)
            {
                creditsCloseButton.onClick.AddListener(OnCreditsCloseClicked);
            }
        }

        private void SetupSliders()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        private void SetupToggles()
        {
            if (hapticsToggle != null)
            {
                hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);
            }
            if (voiceToggle != null)
            {
                voiceToggle.onValueChanged.AddListener(OnVoiceToggled);
            }
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            GameEvents.OnSettingsOpened += OnSettingsOpened;
        }

        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            GameEvents.OnSettingsOpened -= OnSettingsOpened;
        }

        private void OnSettingsOpened()
        {
            LoadCurrentSettings();
            Show();
        }

        protected override void OnShowComplete()
        {
            base.OnShowComplete();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Load audio settings
            if (AudioManager.Instance != null)
            {
                if (masterVolumeSlider != null)
                {
                    masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
                    UpdateVolumeLabel(masterVolumeLabel, AudioManager.Instance.MasterVolume);
                }

                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
                    UpdateVolumeLabel(musicVolumeLabel, AudioManager.Instance.MusicVolume);
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
                    UpdateVolumeLabel(sfxVolumeLabel, AudioManager.Instance.SFXVolume);
                }
            }

            // Load haptics setting
            if (SaveManager.Instance != null && hapticsToggle != null)
            {
                bool hapticsEnabled = SaveManager.Instance.IsHapticsEnabled();
                hapticsToggle.SetIsOnWithoutNotify(hapticsEnabled);
                UpdateHapticsCheckmark(hapticsEnabled);
            }

            // Load voice setting
            if (SaveManager.Instance != null && voiceToggle != null)
            {
                bool voiceEnabled = SaveManager.Instance.IsVoiceEnabled();
                voiceToggle.SetIsOnWithoutNotify(voiceEnabled);
                UpdateVoiceCheckmark(voiceEnabled);
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMasterVolume(value);
            UpdateVolumeLabel(masterVolumeLabel, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
            UpdateVolumeLabel(musicVolumeLabel, value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
            UpdateVolumeLabel(sfxVolumeLabel, value);

            // Play preview sound
            AudioManager.Instance?.PlayButtonClick();
        }

        private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void OnHapticsToggled(bool isOn)
        {
            SaveManager.Instance?.SetHapticsEnabled(isOn);
            UpdateHapticsCheckmark(isOn);
            AudioManager.Instance?.PlayButtonClick();

            // Trigger haptic feedback as demo if enabled
            if (isOn)
            {
                TriggerHapticFeedback();
            }
        }

        private void UpdateHapticsCheckmark(bool isOn)
        {
            if (hapticsCheckmark != null && hapticsToggle != null)
            {
                var switchBehavior = hapticsToggle.GetComponent<GoogleSwitchBehavior>();
                if (switchBehavior == null)
                {
                    hapticsCheckmark.enabled = isOn;
                }
            }
        }

        private void OnVoiceToggled(bool isOn)
        {
            SaveManager.Instance?.SetVoiceEnabled(isOn);
            UpdateVoiceCheckmark(isOn);
            AudioManager.Instance?.PlayButtonClick();
        }

        private void UpdateVoiceCheckmark(bool isOn)
        {
            if (voiceCheckmark != null && voiceToggle != null)
            {
                var switchBehavior = voiceToggle.GetComponent<GoogleSwitchBehavior>();
                if (switchBehavior == null)
                {
                    voiceCheckmark.enabled = isOn;
                }
            }
        }

        /// <summary>
        /// Trigger haptic feedback on mobile devices
        /// </summary>
        public static void TriggerHapticFeedback()
        {
            if (SaveManager.Instance != null && !SaveManager.Instance.IsHapticsEnabled())
                return;

#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Trigger light haptic feedback (for item interactions)
        /// </summary>
        public static void TriggerLightHaptic()
        {
            if (SaveManager.Instance != null && !SaveManager.Instance.IsHapticsEnabled())
                return;

#if UNITY_IOS
            // iOS haptic feedback would require native plugin for fine-grained control
#elif UNITY_ANDROID
            // Android can use shorter vibrations with native plugins
#endif
        }

        private void OnResetProgressClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            pendingAction = PendingAction.ResetProgress;
            ShowConfirmationDialog(
                "Reset Progress?",
                "This will delete all your progress.\nThis action cannot be undone!"
            );
        }

        private void ShowConfirmationDialog(string title, string message)
        {
            if (confirmationDialog != null)
            {
                if (confirmTitleText != null)
                    confirmTitleText.text = title;
                if (confirmMessageText != null)
                    confirmMessageText.text = message;

                confirmationDialog.SetActive(true);
            }
        }

        private void HideConfirmationDialog()
        {
            if (confirmationDialog != null)
            {
                confirmationDialog.SetActive(false);
            }
            pendingAction = PendingAction.None;
        }

        private void OnConfirmYes()
        {
            AudioManager.Instance?.PlayButtonClick();

            switch (pendingAction)
            {
                case PendingAction.ResetProgress:
                    SaveManager.Instance?.ResetAllProgress();
                    OnResetConfirmed?.Invoke();
                    Debug.Log("[SettingsScreen] Progress reset confirmed");
                    break;
            }

            HideConfirmationDialog();
        }

        private void OnConfirmNo()
        {
            AudioManager.Instance?.PlayButtonClick();
            HideConfirmationDialog();
        }

        private void OnCreditsButtonClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            OnCreditsClicked?.Invoke();

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }
        }

        private void OnCreditsCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            Hide();
            GameEvents.InvokeSettingsClosed();
        }

        /// <summary>
        /// Initialize references for runtime-created UI
        /// </summary>
        public void Initialize(
            Button close,
            Slider masterSlider, Slider musicSlider, Slider sfxSlider,
            TextMeshProUGUI masterLabel, TextMeshProUGUI musicLabel, TextMeshProUGUI sfxLabel,
            Toggle haptics, Image hapticsCheck,
            Toggle voice, Image voiceCheck,
            Button resetBtn, Button creditsBtn,
            GameObject confirmDialog, Button confirmYes, Button confirmNo,
            GameObject credits, Button creditsClose,
            Image dimBg)
        {
            closeButton = close;
            masterVolumeSlider = masterSlider;
            musicVolumeSlider = musicSlider;
            sfxVolumeSlider = sfxSlider;
            masterVolumeLabel = masterLabel;
            musicVolumeLabel = musicLabel;
            sfxVolumeLabel = sfxLabel;
            hapticsToggle = haptics;
            hapticsCheckmark = hapticsCheck;
            voiceToggle = voice;
            voiceCheckmark = voiceCheck;
            resetProgressButton = resetBtn;
            creditsButton = creditsBtn;
            confirmationDialog = confirmDialog;
            confirmYesButton = confirmYes;
            confirmNoButton = confirmNo;

            // Find confirmation dialog text elements
            if (confirmationDialog != null)
            {
                var dialogBox = confirmationDialog.transform.Find("DialogBox");
                if (dialogBox != null)
                {
                    var titleTransform = dialogBox.Find("Title");
                    var messageTransform = dialogBox.Find("Message");
                    if (titleTransform != null)
                        confirmTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
                    if (messageTransform != null)
                        confirmMessageText = messageTransform.GetComponent<TextMeshProUGUI>();
                }
            }
            creditsPanel = credits;
            creditsCloseButton = creditsClose;
            dimBackground = dimBg;

            SetupButtons();
            SetupSliders();
            SetupToggles();

            if (dimBackground != null)
            {
                dimBackground.color = dimColor;
            }

            // Hide dialogs
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);
            if (creditsPanel != null)
                creditsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            if (hapticsToggle != null) hapticsToggle.onValueChanged.RemoveAllListeners();
            if (voiceToggle != null) voiceToggle.onValueChanged.RemoveAllListeners();
            if (resetProgressButton != null) resetProgressButton.onClick.RemoveAllListeners();
            if (creditsButton != null) creditsButton.onClick.RemoveAllListeners();
            if (confirmYesButton != null) confirmYesButton.onClick.RemoveAllListeners();
            if (confirmNoButton != null) confirmNoButton.onClick.RemoveAllListeners();
            if (creditsCloseButton != null) creditsCloseButton.onClick.RemoveAllListeners();
        }
    }
}
