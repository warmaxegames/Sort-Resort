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
        [SerializeField] private AudioSource ambientSource;  // For background/ambient loops
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

        [Header("Crossfade Settings")]
        [SerializeField] private float crossfadeDuration = 0.5f;

        [Header("Common Sound Effects")]
        [SerializeField] private AudioClip itemDragClip;
        [SerializeField] private AudioClip itemDropClip;
        [SerializeField] private AudioClip matchClip;
        [SerializeField] private AudioClip unlockClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip starEarnedClip;
        [SerializeField] private AudioClip star1Clip;
        [SerializeField] private AudioClip star2Clip;
        [SerializeField] private AudioClip star3Clip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField] private AudioClip timerCountUpClip;
        [SerializeField] private AudioClip warpClip;
        [SerializeField] private AudioClip achievementClip;
        [SerializeField] private AudioClip portalClip;

        private bool isUsingSourceA = true;
        private Coroutine crossfadeCoroutine;
        private Coroutine ambientFadeCoroutine;
        private float saveDebounceTime = 0.5f;
        private Coroutine saveDebounceCoroutine;

        // Track current world to avoid restarting music on next level
        private string currentPlayingWorld = "";
        private bool isPlayingGameplayAudio = false;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float AmbientVolume => ambientVolume;
        public float SFXVolume => sfxVolume;
        public float UIVolume => uiVolume;
        public string CurrentPlayingWorld => currentPlayingWorld;

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
            LoadAudioClipsFromResources();
            LoadVolumeSettings();
        }

        /// <summary>
        /// Auto-load audio clips from Resources if not assigned in inspector.
        /// Audio files should be in Assets/_Project/Resources/Audio/
        /// </summary>
        private void LoadAudioClipsFromResources()
        {
            // SFX
            if (itemDragClip == null)
                itemDragClip = Resources.Load<AudioClip>("Audio/SFX/drag");
            if (itemDropClip == null)
                itemDropClip = Resources.Load<AudioClip>("Audio/SFX/drop");
            if (matchClip == null)
                matchClip = Resources.Load<AudioClip>("Audio/SFX/match");
            if (unlockClip == null)
                unlockClip = Resources.Load<AudioClip>("Audio/SFX/unlock_sound");
            if (victoryClip == null)
                victoryClip = Resources.Load<AudioClip>("Audio/SFX/victory");
            if (starEarnedClip == null)
                starEarnedClip = Resources.Load<AudioClip>("Audio/SFX/3star");
            if (star1Clip == null)
                star1Clip = Resources.Load<AudioClip>("Audio/SFX/1star");
            if (star2Clip == null)
                star2Clip = Resources.Load<AudioClip>("Audio/SFX/2star");
            if (star3Clip == null)
                star3Clip = Resources.Load<AudioClip>("Audio/SFX/3star");
            if (levelCompleteClip == null)
                levelCompleteClip = Resources.Load<AudioClip>("Audio/SFX/levelcompletesound");
            if (timerCountUpClip == null)
                timerCountUpClip = Resources.Load<AudioClip>("Audio/SFX/timer");

            // UI
            if (buttonClickClip == null)
                buttonClickClip = Resources.Load<AudioClip>("Audio/UI/button_click");
            if (warpClip == null)
                warpClip = Resources.Load<AudioClip>("Audio/UI/warp");
            if (achievementClip == null)
                achievementClip = Resources.Load<AudioClip>("Audio/SFX/achievement_sound");
            if (portalClip == null)
                portalClip = Resources.Load<AudioClip>("Audio/SFX/portal");

            Debug.Log($"[AudioManager] Audio clips loaded - match:{matchClip != null}, unlock:{unlockClip != null}, victory:{victoryClip != null}, button:{buttonClickClip != null}, warp:{warpClip != null}, achievement:{achievementClip != null}, portal:{portalClip != null}");
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

            if (ambientSource == null)
            {
                var ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
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

        #region Volume Control

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

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            UpdateAmbientVolume();
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
            UpdateAmbientVolume();
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

        private void UpdateAmbientVolume()
        {
            if (ambientSource != null)
            {
                ambientSource.volume = masterVolume * ambientVolume;
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

        #endregion

        #region Music Playback

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

        #endregion

        #region Ambient/Background Sound Playback

        public void PlayAmbient(AudioClip clip, bool fade = true)
        {
            if (clip == null) return;

            if (ambientFadeCoroutine != null)
            {
                StopCoroutine(ambientFadeCoroutine);
                ambientFadeCoroutine = null;
            }

            if (fade && ambientSource.isPlaying)
            {
                ambientFadeCoroutine = StartCoroutine(CrossfadeAmbient(clip));
            }
            else
            {
                ambientSource.clip = clip;
                ambientSource.volume = masterVolume * ambientVolume;
                ambientSource.Play();
            }
        }

        private IEnumerator CrossfadeAmbient(AudioClip newClip)
        {
            float startVolume = ambientSource.volume;
            float targetVolume = masterVolume * ambientVolume;
            float elapsed = 0f;
            float duration = crossfadeDuration * 0.5f;

            // Fade out
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            // Switch clip
            ambientSource.Stop();
            ambientSource.clip = newClip;
            ambientSource.Play();

            // Fade in
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ambientSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }

            ambientSource.volume = targetVolume;
            ambientFadeCoroutine = null;
        }

        public void StopAmbient(bool fade = true)
        {
            if (ambientFadeCoroutine != null)
            {
                StopCoroutine(ambientFadeCoroutine);
                ambientFadeCoroutine = null;
            }

            if (fade)
            {
                StartCoroutine(FadeOutAmbient());
            }
            else
            {
                ambientSource.Stop();
            }
        }

        private IEnumerator FadeOutAmbient()
        {
            float startVolume = ambientSource.volume;
            float elapsed = 0f;
            float duration = crossfadeDuration * 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            ambientSource.Stop();
            ambientSource.volume = 0f;
        }

        #endregion

        #region World-Specific Audio

        /// <summary>
        /// Play worldmap music (for level select screen)
        /// </summary>
        public void PlayWorldmapMusic()
        {
            isPlayingGameplayAudio = false;
            currentPlayingWorld = "";

            var clip = Resources.Load<AudioClip>("Audio/Music/worldmap_music");
            if (clip != null)
            {
                PlayMusic(clip);
                Debug.Log("[AudioManager] Playing worldmap music");
            }
            else
            {
                Debug.LogWarning("[AudioManager] worldmap_music not found");
            }

            // Stop any ambient sounds from gameplay
            StopAmbient();
        }

        /// <summary>
        /// Play world-specific gameplay audio (music + ambient background)
        /// Only restarts if world changes
        /// </summary>
        public void PlayWorldGameplayAudio(string worldId)
        {
            // Don't restart if already playing this world's audio
            if (isPlayingGameplayAudio && currentPlayingWorld == worldId)
            {
                Debug.Log($"[AudioManager] Already playing {worldId} gameplay audio, continuing");
                return;
            }

            currentPlayingWorld = worldId;
            isPlayingGameplayAudio = true;

            // World ID is used directly for audio file paths
            string audioWorldId = worldId;

            // Load and play gameplay music
            string[] musicPaths = new string[]
            {
                $"Audio/Music/{audioWorldId}_gameplay_music",
                $"Audio/Music/{worldId}_gameplay_music",
                "Audio/Music/island_gameplay_music" // Fallback
            };

            AudioClip musicClip = null;
            foreach (var path in musicPaths)
            {
                musicClip = Resources.Load<AudioClip>(path);
                if (musicClip != null)
                {
                    Debug.Log($"[AudioManager] Loaded gameplay music: {path}");
                    break;
                }
            }

            if (musicClip != null)
            {
                PlayMusic(musicClip);
            }

            // Load and play ambient/background sounds
            string[] ambientPaths = new string[]
            {
                $"Audio/Music/{audioWorldId}_background",
                $"Audio/Music/{worldId}_background",
                $"Audio/Music/{audioWorldId}_background_music",
                $"Audio/Music/{worldId}_background_music"
            };

            AudioClip ambientClip = null;
            foreach (var path in ambientPaths)
            {
                ambientClip = Resources.Load<AudioClip>(path);
                if (ambientClip != null)
                {
                    Debug.Log($"[AudioManager] Loaded ambient: {path}");
                    break;
                }
            }

            if (ambientClip != null)
            {
                PlayAmbient(ambientClip);
            }
        }

        /// <summary>
        /// Stop all gameplay audio (music and ambient) - call before victory
        /// </summary>
        public void StopGameplayAudio(bool fade = true)
        {
            isPlayingGameplayAudio = false;
            StopMusic(fade);
            StopAmbient(fade);
        }

        /// <summary>
        /// Stop all audio immediately
        /// </summary>
        public void StopAllAudio()
        {
            isPlayingGameplayAudio = false;
            currentPlayingWorld = "";
            StopAllMusic();
            ambientSource.Stop();
        }

        #endregion

        #region SFX Playback

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
        public void PlayUnlockSound()
        {
            // Play at higher volume (2.5x) since unlock_sound.mp3 has low internal audio levels
            PlaySFX(unlockClip, 2.5f);
        }
        public void PlayFailureSound() => PlaySFX(failureClip);
        public void PlayButtonClick() => PlayUI(buttonClickClip);
        public void PlayStarEarned() => PlaySFX(starEarnedClip);
        public void PlayLevelCompleteSound() => PlaySFX(levelCompleteClip);
        public void PlayTimerCountUp() => PlaySFX(timerCountUpClip);
        public void PlayStarEarned(int starNumber)
        {
            switch (starNumber)
            {
                case 1: PlaySFX(star1Clip); break;
                case 2: PlaySFX(star2Clip); break;
                case 3: PlaySFX(star3Clip); break;
                default: PlaySFX(starEarnedClip); break;
            }
        }
        public void PlayWarpSound() => PlayUI(portalClip ?? warpClip);
        public void PlayAchievementSound() => PlaySFX(achievementClip, 1.3f);
        public void PlayPortalSound() => PlaySFX(portalClip);

        /// <summary>
        /// Play victory sound (stops gameplay audio first)
        /// </summary>
        public void PlayVictorySound()
        {
            // Stop gameplay music and ambient before victory
            StopGameplayAudio(fade: false);
            PlaySFX(victoryClip);
        }

        #endregion

        #region Settings Persistence

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
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
            UpdateAllVolumes();
        }

        #endregion
    }
}
