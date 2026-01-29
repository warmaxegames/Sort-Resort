using System.Collections;
using UnityEngine;

namespace SortResort
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

        [Header("Crossfade Settings")]
        [SerializeField] private float crossfadeDuration = 2f;

        [Header("Common Sound Effects")]
        [SerializeField] private AudioClip itemDragClip;
        [SerializeField] private AudioClip itemDropClip;
        [SerializeField] private AudioClip matchClip;
        [SerializeField] private AudioClip unlockClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip starEarnedClip;

        private bool isUsingSourceA = true;
        private Coroutine crossfadeCoroutine;
        private float saveDebounceTime = 0.5f;
        private Coroutine saveDebounceCoroutine;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public float UIVolume => uiVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadVolumeSettings();
        }

        private void InitializeAudioSources()
        {
            if (musicSourceA == null)
            {
                var musicObjA = new GameObject("MusicSourceA");
                musicObjA.transform.SetParent(transform);
                musicSourceA = musicObjA.AddComponent<AudioSource>();
                musicSourceA.loop = true;
                musicSourceA.playOnAwake = false;
            }

            if (musicSourceB == null)
            {
                var musicObjB = new GameObject("MusicSourceB");
                musicObjB.transform.SetParent(transform);
                musicSourceB = musicObjB.AddComponent<AudioSource>();
                musicSourceB.loop = true;
                musicSourceB.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                var uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnMasterVolumeChanged += SetMasterVolume;
            GameEvents.OnMusicVolumeChanged += SetMusicVolume;
            GameEvents.OnSFXVolumeChanged += SetSFXVolume;
        }

        private void OnDisable()
        {
            GameEvents.OnMasterVolumeChanged -= SetMasterVolume;
            GameEvents.OnMusicVolumeChanged -= SetMusicVolume;
            GameEvents.OnSFXVolumeChanged -= SetSFXVolume;
        }

        // Volume Control
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            DebounceSaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
            DebounceSaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateSFXVolume();
            DebounceSaveSettings();
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            UpdateUIVolume();
            DebounceSaveSettings();
        }

        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
            UpdateSFXVolume();
            UpdateUIVolume();
        }

        private void UpdateMusicVolume()
        {
            float effectiveVolume = masterVolume * musicVolume;
            if (isUsingSourceA)
            {
                musicSourceA.volume = effectiveVolume;
            }
            else
            {
                musicSourceB.volume = effectiveVolume;
            }
        }

        private void UpdateSFXVolume()
        {
            sfxSource.volume = masterVolume * sfxVolume;
        }

        private void UpdateUIVolume()
        {
            uiSource.volume = masterVolume * uiVolume;
        }

        // Music Playback
        public void PlayMusic(AudioClip clip, bool instant = false)
        {
            if (clip == null) return;

            if (instant || crossfadeCoroutine == null && !GetCurrentMusicSource().isPlaying)
            {
                StopAllMusic();
                var source = GetCurrentMusicSource();
                source.clip = clip;
                source.volume = masterVolume * musicVolume;
                source.Play();
            }
            else
            {
                CrossfadeToMusic(clip);
            }
        }

        public void CrossfadeToMusic(AudioClip newClip, float duration = -1f)
        {
            if (newClip == null) return;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            float fadeDuration = duration > 0 ? duration : crossfadeDuration;
            crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(newClip, fadeDuration));
        }

        private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
        {
            var fadeOutSource = isUsingSourceA ? musicSourceA : musicSourceB;
            var fadeInSource = isUsingSourceA ? musicSourceB : musicSourceA;

            fadeInSource.clip = newClip;
            fadeInSource.volume = 0f;
            fadeInSource.Play();

            float targetVolume = masterVolume * musicVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                fadeOutSource.volume = Mathf.Lerp(targetVolume, 0f, t);
                fadeInSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            fadeOutSource.Stop();
            fadeOutSource.volume = 0f;
            fadeInSource.volume = targetVolume;

            isUsingSourceA = !isUsingSourceA;
            crossfadeCoroutine = null;
        }

        public void StopMusic(bool fade = true)
        {
            if (fade && crossfadeCoroutine == null)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                StopAllMusic();
            }
        }

        private IEnumerator FadeOutMusic()
        {
            var source = GetCurrentMusicSource();
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }

        private void StopAllMusic()
        {
            musicSourceA.Stop();
            musicSourceB.Stop();
        }

        private AudioSource GetCurrentMusicSource()
        {
            return isUsingSourceA ? musicSourceA : musicSourceB;
        }

        // SFX Playback
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
        }

        public void PlayUI(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            uiSource.PlayOneShot(clip, masterVolume * uiVolume * volumeScale);
        }

        // Convenience Methods for Common Sounds
        public void PlayDragSound() => PlaySFX(itemDragClip);
        public void PlayDropSound() => PlaySFX(itemDropClip);
        public void PlayMatchSound() => PlaySFX(matchClip);
        public void PlayUnlockSound() => PlaySFX(unlockClip);
        public void PlayVictorySound() => PlaySFX(victoryClip);
        public void PlayFailureSound() => PlaySFX(failureClip);
        public void PlayButtonClick() => PlayUI(buttonClickClip);
        public void PlayStarEarned() => PlaySFX(starEarnedClip);

        // Settings Persistence
        private void DebounceSaveSettings()
        {
            if (saveDebounceCoroutine != null)
            {
                StopCoroutine(saveDebounceCoroutine);
            }
            saveDebounceCoroutine = StartCoroutine(SaveSettingsDebounced());
        }

        private IEnumerator SaveSettingsDebounced()
        {
            yield return new WaitForSecondsRealtime(saveDebounceTime);
            SaveVolumeSettings();
            saveDebounceCoroutine = null;
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
            UpdateAllVolumes();
        }
    }
}
