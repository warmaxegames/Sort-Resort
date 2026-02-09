using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Manages dialogue playback with typewriter effect and Animal Crossing-style voice
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Voice Settings")]
        [SerializeField] private float defaultPitch = 1.0f;
        [SerializeField] private float pitchVariation = 0.05f; // Random variation per letter
        [SerializeField] private float defaultLettersPerSecond = 24f;

        [Header("Audio")]
        [SerializeField] private AudioSource voiceAudioSource;

        // Letter audio clips (A-Z)
        private Dictionary<char, AudioClip> letterClips = new Dictionary<char, AudioClip>();

        // Current dialogue state
        private DialogueSequence currentSequence;
        private int currentLineIndex;
        private MascotData currentMascot;
        private Coroutine typewriterCoroutine;
        private bool isTyping;
        private bool skipRequested;
        private string fullText;

        // Events
        public event Action<string> OnTextUpdated;         // Partial text during typewriter
        public event Action<DialogueLine> OnLineStarted;   // New line begins
        public event Action OnLineComplete;                // Line finished typing
        public event Action OnDialogueComplete;            // All lines done
        public event Action<MascotData> OnMascotChanged;   // Mascot switched

        public bool IsDialogueActive => currentSequence != null;
        public bool IsTyping => isTyping;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadLetterClips();
            SetupAudioSource();
        }

        private void OnEnable()
        {
            // Subscribe to game events to trigger dialogues
            GameEvents.OnLevelStarted += OnLevelStarted;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnGameModeChanged += OnGameModeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= OnLevelStarted;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnGameModeChanged -= OnGameModeChanged;
        }

        private void OnLevelStarted(int levelNumber)
        {
            string worldId = GameManager.Instance?.CurrentWorldId;
            Debug.Log($"[DialogueManager] OnLevelStarted: level {levelNumber}, world {worldId}");

            if (string.IsNullOrEmpty(worldId)) return;

            // Check level start triggers (trigger's own levelNumber field handles filtering)
            CheckTriggers(DialogueTrigger.TriggerType.WorldFirstLevel, worldId, levelNumber);
        }

        private void OnLevelCompleted(int levelNumber, int starsEarned)
        {
            string worldId = GameManager.Instance?.CurrentWorldId;
            if (string.IsNullOrEmpty(worldId)) return;

            CheckTriggers(DialogueTrigger.TriggerType.LevelComplete, worldId, levelNumber);
        }

        private void OnGameModeChanged(GameMode mode)
        {
            Debug.Log($"[DialogueManager] OnGameModeChanged: {mode}");

            // Check mode first play triggers (playOnce ensures it only fires once per mode)
            CheckTriggers(DialogueTrigger.TriggerType.ModeFirstPlay, null, 0, 0, (int)mode);
        }

        /// <summary>
        /// Call this after a level is saved to check if Hard Mode just unlocked for the world.
        /// Should be called from SaveManager or LevelManager after saving progress.
        /// </summary>
        public void CheckHardModeUnlock(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return;
            if (SaveManager.Instance == null) return;

            if (SaveManager.Instance.IsHardModeUnlocked(worldId))
            {
                Debug.Log($"[DialogueManager] Hard Mode is unlocked for {worldId}, checking triggers");
                CheckTriggers(DialogueTrigger.TriggerType.HardModeUnlock, worldId);
            }
        }

        private void LoadLetterClips()
        {
            for (char c = 'A'; c <= 'Z'; c++)
            {
                var clip = Resources.Load<AudioClip>($"Audio/Dialogue/Letters/{c}");
                if (clip != null)
                {
                    letterClips[c] = clip;
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] Missing letter clip: {c}");
                }
            }

            Debug.Log($"[DialogueManager] Loaded {letterClips.Count} letter clips");
        }

        private void SetupAudioSource()
        {
            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }

            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            voiceAudioSource.spatialBlend = 0f; // 2D sound
            voiceAudioSource.volume = 0.7f;
        }

        /// <summary>
        /// Start playing a dialogue sequence
        /// </summary>
        public bool StartDialogue(string dialogueId)
        {
            var sequence = DialogueDataLoader.GetDialogue(dialogueId);
            if (sequence == null)
            {
                Debug.LogWarning($"[DialogueManager] Dialogue not found: {dialogueId}");
                return false;
            }

            return StartDialogue(sequence);
        }

        /// <summary>
        /// Start playing a dialogue sequence. Returns true if dialogue actually started.
        /// </summary>
        public bool StartDialogue(DialogueSequence sequence)
        {
            if (sequence == null || sequence.lines.Count == 0)
            {
                Debug.LogWarning("[DialogueManager] Empty or null dialogue sequence");
                return false;
            }

            // Check if already played (for playOnce dialogues)
            bool alreadyPlayed = HasDialogueBeenPlayed(sequence.id);
            Debug.Log($"[DialogueManager] Dialogue '{sequence.id}': playOnce={sequence.playOnce}, alreadyPlayed={alreadyPlayed}");
            if (sequence.playOnce && alreadyPlayed)
            {
                Debug.Log($"[DialogueManager] Skipping already-played dialogue: {sequence.id}");
                return false;
            }

            currentSequence = sequence;
            currentLineIndex = 0;

            Debug.Log($"[DialogueManager] Starting dialogue: {sequence.id} ({sequence.lines.Count} lines)");

            PlayCurrentLine();
            return true;
        }

        /// <summary>
        /// Quick dialogue with a single line
        /// </summary>
        public void SayLine(string mascotId, string text, string expression = "default")
        {
            var sequence = new DialogueSequence
            {
                id = "quick_" + Time.time,
                playOnce = false,
                lines = new List<DialogueLine>
                {
                    new DialogueLine(mascotId, text, expression)
                }
            };

            StartDialogue(sequence);
        }

        private void PlayCurrentLine()
        {
            if (currentSequence == null || currentLineIndex >= currentSequence.lines.Count)
            {
                EndDialogue();
                return;
            }

            var line = currentSequence.lines[currentLineIndex];

            // Update mascot if changed
            var mascot = DialogueDataLoader.GetMascot(line.mascotId);
            if (mascot != null && (currentMascot == null || currentMascot.id != mascot.id))
            {
                currentMascot = mascot;
                OnMascotChanged?.Invoke(mascot);
            }

            // Use default mascot settings if not found
            if (currentMascot == null)
            {
                currentMascot = new MascotData
                {
                    id = line.mascotId,
                    basePitch = defaultPitch,
                    speakSpeed = defaultLettersPerSecond
                };
            }

            int listenerCount = OnLineStarted?.GetInvocationList()?.Length ?? 0;
            Debug.Log($"[DialogueManager] Firing OnLineStarted for '{line.text}' ({listenerCount} listeners)");
            OnLineStarted?.Invoke(line);

            // Start typewriter effect
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            fullText = line.text;
            skipRequested = false;
            typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text, currentMascot));
        }

        private IEnumerator TypewriterEffect(string text, MascotData mascot)
        {
            isTyping = true;
            string displayedText = "";
            float lettersPerSecond = mascot?.speakSpeed ?? defaultLettersPerSecond;
            float basePitch = mascot?.basePitch ?? defaultPitch;
            float delay = 1f / lettersPerSecond;

            for (int i = 0; i < text.Length; i++)
            {
                // Check for skip
                if (skipRequested)
                {
                    displayedText = text;
                    OnTextUpdated?.Invoke(displayedText);
                    break;
                }

                char c = text[i];
                displayedText += c;
                OnTextUpdated?.Invoke(displayedText);

                // Play sound for letters
                if (char.IsLetter(c))
                {
                    PlayLetterSound(char.ToUpper(c), basePitch);
                    yield return new WaitForSecondsRealtime(delay);
                }
                else if (c == ' ')
                {
                    // Small pause for spaces
                    yield return new WaitForSecondsRealtime(delay * 0.5f);
                }
                else if (c == '.' || c == '!' || c == '?')
                {
                    // Longer pause for sentence endings
                    yield return new WaitForSecondsRealtime(delay * 3f);
                }
                else if (c == ',')
                {
                    // Medium pause for commas
                    yield return new WaitForSecondsRealtime(delay * 1.5f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(delay * 0.3f);
                }
            }

            isTyping = false;
            OnLineComplete?.Invoke();
        }

        private void PlayLetterSound(char letter, float basePitch)
        {
            // Check if voices are disabled in settings
            if (SaveManager.Instance != null && !SaveManager.Instance.IsVoiceEnabled())
                return;

            if (!letterClips.TryGetValue(letter, out var clip))
            {
                return;
            }

            // Add slight random variation to pitch for natural feel
            float pitch = basePitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            voiceAudioSource.pitch = pitch;
            voiceAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Called when player taps/clicks to advance
        /// </summary>
        public void OnPlayerInput()
        {
            if (!IsDialogueActive) return;

            if (isTyping)
            {
                // Skip to end of current line
                skipRequested = true;
            }
            else
            {
                // Advance to next line
                AdvanceToNextLine();
            }
        }

        /// <summary>
        /// Advance to the next line of dialogue
        /// </summary>
        public void AdvanceToNextLine()
        {
            currentLineIndex++;

            if (currentLineIndex >= currentSequence.lines.Count)
            {
                EndDialogue();
            }
            else
            {
                PlayCurrentLine();
            }
        }

        /// <summary>
        /// Skip the entire dialogue sequence
        /// </summary>
        public void SkipDialogue()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            EndDialogue();
        }

        private void EndDialogue()
        {
            if (currentSequence != null && currentSequence.playOnce)
            {
                MarkDialogueAsPlayed(currentSequence.id);
            }

            currentSequence = null;
            currentLineIndex = 0;
            currentMascot = null;
            isTyping = false;

            OnDialogueComplete?.Invoke();
            Debug.Log("[DialogueManager] Dialogue complete");
        }

        #region Persistence

        private const string PLAYED_DIALOGUES_KEY = "PlayedDialogues";

        private bool HasDialogueBeenPlayed(string dialogueId)
        {
            string played = PlayerPrefs.GetString(PLAYED_DIALOGUES_KEY, "");
            bool result = played.Contains($"|{dialogueId}|");
            Debug.Log($"[DialogueManager] HasDialogueBeenPlayed('{dialogueId}'): {result} (stored: '{played}')");
            return result;
        }

        private void MarkDialogueAsPlayed(string dialogueId)
        {
            string played = PlayerPrefs.GetString(PLAYED_DIALOGUES_KEY, "");
            if (!played.Contains($"|{dialogueId}|"))
            {
                played += $"|{dialogueId}|";
                PlayerPrefs.SetString(PLAYED_DIALOGUES_KEY, played);
                PlayerPrefs.Save();
            }
        }

        public void ResetPlayedDialogues()
        {
            PlayerPrefs.DeleteKey(PLAYED_DIALOGUES_KEY);
            PlayerPrefs.Save();
            Debug.Log("[DialogueManager] Played dialogues reset");
        }

        #endregion

        #region Trigger Checking

        /// <summary>
        /// Check if any dialogue should trigger for the given event
        /// </summary>
        public void CheckTriggers(DialogueTrigger.TriggerType triggerType, string worldId = null, int levelNumber = 0, int value = 0, int gameMode = -1)
        {
            var db = DialogueDataLoader.LoadDatabase();
            Debug.Log($"[DialogueManager] CheckTriggers: type={triggerType}, world={worldId}, level={levelNumber}, gameMode={gameMode}");

            if (db.triggers == null || db.triggers.Count == 0)
            {
                Debug.LogWarning("[DialogueManager] No triggers loaded from database!");
                return;
            }

            Debug.Log($"[DialogueManager] Checking {db.triggers.Count} triggers...");

            foreach (var trigger in db.triggers)
            {
                Debug.Log($"[DialogueManager] Trigger {trigger.id}: type={trigger.type}, world={trigger.worldId}, level={trigger.levelNumber}, gameMode={trigger.gameMode}");

                if (trigger.type != triggerType) continue;

                // Check world match
                if (!string.IsNullOrEmpty(trigger.worldId) && trigger.worldId != worldId) continue;

                // Check level match
                if (trigger.levelNumber > 0 && trigger.levelNumber != levelNumber) continue;

                // Check threshold
                if (trigger.threshold > 0 && value < trigger.threshold) continue;

                // Check game mode match
                if (trigger.gameMode >= 0 && trigger.gameMode != gameMode) continue;

                // Trigger matched - try to start dialogue
                Debug.Log($"[DialogueManager] Trigger MATCHED! Starting dialogue: {trigger.dialogueId}");
                if (StartDialogue(trigger.dialogueId))
                {
                    break; // Only trigger one dialogue at a time
                }
            }
        }

        #endregion

        #region Testing

        /// <summary>
        /// Test the voice for a mascot
        /// </summary>
        public void TestMascotVoice(string mascotId, string testText = "Hello! Welcome to Sort Resort!")
        {
            SayLine(mascotId, testText);
        }

        /// <summary>
        /// Test with custom pitch
        /// </summary>
        public void TestVoiceWithPitch(float pitch, string testText = "Testing voice pitch!")
        {
            var tempMascot = new MascotData
            {
                id = "test",
                basePitch = pitch,
                speakSpeed = defaultLettersPerSecond
            };

            currentMascot = tempMascot;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            typewriterCoroutine = StartCoroutine(TypewriterEffect(testText, tempMascot));
        }

        #endregion
    }
}
