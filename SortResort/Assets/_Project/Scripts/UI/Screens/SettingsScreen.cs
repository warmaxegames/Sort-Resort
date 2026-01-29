using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        [Header("Background")]
        [SerializeField] private Image dimBackground;
        [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.7f);

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SetupSliders();

            if (dimBackground != null)
            {
                dimBackground.color = dimColor;
            }
        }

        private void SetupButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
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
            if (AudioManager.Instance == null) return;

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

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            Hide();
            GameEvents.InvokeSettingsClosed();
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        }
    }
}
