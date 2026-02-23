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
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

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

            // Work on a copy so we don't modify cached JSON data
            var workingSequence = new DialogueSequence
            {
                id = sequence.id,
                triggerId = sequence.triggerId,
                playOnce = sequence.playOnce,
                lines = new List<DialogueLine>(sequence.lines)
            };

            // Auto-split any lines that overflow the dialogue box
            PreSplitOverflowingLines(workingSequence);

            currentSequence = workingSequence;
            currentLineIndex = 0;

            Debug.Log($"[DialogueManager] Starting dialogue: {workingSequence.id} ({workingSequence.lines.Count} lines)");

            PlayCurrentLine();
            return true;
        }

        /// <summary>
        /// Merges consecutive same-mascot/expression lines into single text blocks,
        /// then auto-splits any that overflow the dialogue box into evenly-filled pages.
        /// </summary>
        private void PreSplitOverflowingLines(DialogueSequence sequence)
        {
            if (DialogueUI.Instance == null) return;

            // Step 1: Merge consecutive lines with same mascot+expression
            var merged = new List<DialogueLine>();
            foreach (var line in sequence.lines)
            {
                if (merged.Count > 0)
                {
                    var prev = merged[merged.Count - 1];
                    if (prev.mascotId == line.mascotId && prev.expression == line.expression)
                    {
                        // Append to previous line with a space
                        prev.text = prev.text.TrimEnd() + " " + line.text.TrimStart();
                        continue;
                    }
                }
                merged.Add(new DialogueLine(line.mascotId, line.text, line.expression));
            }

            // Step 2: Split any overflowing merged lines at word boundaries
            // Accounts for "..." continuation markers by measuring with them included
            var splitLines = new List<DialogueLine>();
            foreach (var line in merged)
            {
                string remaining = line.text;
                var parts = new List<string>();

                while (remaining.Length > 0)
                {
                    // If this will be a continuation box, measure with "..." prefix
                    string testText = parts.Count > 0 ? ("..." + remaining) : remaining;
                    int prefixLen = parts.Count > 0 ? 3 : 0;

                    int visible = DialogueUI.Instance.MeasureVisibleCharacters(testText);
                    if (visible >= testText.Length)
                    {
                        parts.Add(testText);
                        break;
                    }

                    // Reserve room for "..." suffix by measuring chunk + "..."
                    // Reduce effective split zone by a few chars
                    int effectiveVisible = Mathf.Max(prefixLen + 1, visible - 3);

                    int splitAt = testText.LastIndexOf(' ', Mathf.Min(effectiveVisible, testText.Length - 1));
                    if (splitAt <= prefixLen) splitAt = effectiveVisible;

                    string chunk = testText.Substring(0, splitAt).TrimEnd();
                    // remaining is raw text without prefix for next iteration
                    remaining = testText.Substring(splitAt).TrimStart();
                    // Strip "..." prefix from remaining since next iteration re-adds it
                    if (prefixLen > 0 && remaining.StartsWith("..."))
                        remaining = remaining.Substring(3).TrimStart();

                    // Prevent orphaned short text: pull words back until remainder is substantial
                    const int MIN_CHARS_NEXT_BOX = 40;
                    while (remaining.Length > 0 && remaining.Length < MIN_CHARS_NEXT_BOX)
                    {
                        int lastSpace = chunk.LastIndexOf(' ');
                        if (lastSpace <= prefixLen) break;
                        string pulled = chunk.Substring(lastSpace);
                        chunk = chunk.Substring(0, lastSpace).TrimEnd();
                        remaining = pulled.TrimStart() + " " + remaining;
                    }

                    // Add "..." suffix to indicate continuation
                    parts.Add(chunk + "...");
                }

                foreach (var part in parts)
                    splitLines.Add(new DialogueLine(line.mascotId, part, line.expression));
            }

            if (splitLines.Count != sequence.lines.Count)
            {
                Debug.Log($"[DialogueManager] Pre-split: {sequence.lines.Count} original -> {merged.Count} merged -> {splitLines.Count} final lines");
            }
            sequence.lines = splitLines;
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

            // Scale by master and SFX volume
            float vol = 1f;
            if (AudioManager.Instance != null)
                vol = AudioManager.Instance.MasterVolume * AudioManager.Instance.SFXVolume;
            voiceAudioSource.PlayOneShot(clip, vol);
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
