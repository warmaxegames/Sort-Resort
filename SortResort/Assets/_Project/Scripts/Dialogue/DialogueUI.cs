using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace SortResort
{
    /// <summary>
    /// UI component for displaying dialogue boxes
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image dialogueBoxImage;
        [SerializeField] private Image mascotImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject continueIndicator;

        [Header("Settings")]
        [SerializeField] private float showAnimationDuration = 0.3f;
        [SerializeField] private float hideAnimationDuration = 0.2f;

        private CanvasGroup canvasGroup;
        private bool isVisible;
        private Coroutine animationCoroutine;

        // Input System
        private InputAction clickAction;
        private InputAction submitAction;

        // Cached mascot sprites
        private string currentMascotFolder;
        private Sprite defaultMascotSprite;

        private void Awake()
        {
            // Note: When created via AddComponent, fields aren't set yet
            // Initialize() will be called after fields are set via reflection
        }

        /// <summary>
        /// Called after fields are set via reflection in UIManager
        /// </summary>
        public void Initialize()
        {
            if (dialoguePanel != null)
            {
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            // Set up input actions for new Input System
            clickAction = new InputAction("DialogueClick", InputActionType.Button, "<Pointer>/press");
            submitAction = new InputAction("DialogueSubmit", InputActionType.Button, "<Keyboard>/space");

            clickAction.Enable();
            submitAction.Enable();

            // Make sure we're subscribed to DialogueManager events
            TrySubscribe();

            Hide(immediate: true);
        }

        private void OnEnable()
        {
            // Try to subscribe immediately, or wait for Start if DialogueManager isn't ready
            TrySubscribe();
        }

        private void Start()
        {
            // Try again in Start in case DialogueManager wasn't ready in OnEnable
            TrySubscribe();
        }

        private bool isSubscribed = false;

        private void TrySubscribe()
        {
            if (isSubscribed) return;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnTextUpdated += UpdateText;
                DialogueManager.Instance.OnLineStarted += OnLineStarted;
                DialogueManager.Instance.OnLineComplete += OnLineComplete;
                DialogueManager.Instance.OnDialogueComplete += OnDialogueComplete;
                DialogueManager.Instance.OnMascotChanged += OnMascotChanged;
                isSubscribed = true;

                // Check if dialogue is already playing (we may have missed OnLineStarted)
                if (DialogueManager.Instance.IsDialogueActive)
                {
                    Show();
                }
            }
        }

        private void OnDisable()
        {
            // Don't unsubscribe here - we want to stay subscribed even when hidden
            // so we can receive events and show the panel when dialogue starts
        }

        private void OnDestroy()
        {
            // Clean up input actions
            clickAction?.Dispose();
            submitAction?.Dispose();

            // Only unsubscribe when actually being destroyed
            if (isSubscribed && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnTextUpdated -= UpdateText;
                DialogueManager.Instance.OnLineStarted -= OnLineStarted;
                DialogueManager.Instance.OnLineComplete -= OnLineComplete;
                DialogueManager.Instance.OnDialogueComplete -= OnDialogueComplete;
                DialogueManager.Instance.OnMascotChanged -= OnMascotChanged;
                isSubscribed = false;
            }
        }

        private void Update()
        {
            // Handle player input to advance dialogue (using new Input System)
            if (isVisible && (clickAction?.WasPressedThisFrame() == true || submitAction?.WasPressedThisFrame() == true))
            {
                DialogueManager.Instance?.OnPlayerInput();
            }
        }

        private void OnLineStarted(DialogueLine line)
        {
            Show();

            // Update mascot expression
            UpdateMascotExpression(line.expression);

            // Clear text (typewriter will fill it)
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }

            // Hide continue indicator while typing
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(false);
            }
        }

        private void UpdateText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
        }

        private void OnLineComplete()
        {
            // Show continue indicator
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }
        }

        private void OnDialogueComplete()
        {
            Hide();
        }

        private void OnMascotChanged(MascotData mascot)
        {
            // Update name
            if (nameText != null)
            {
                nameText.text = mascot.displayName;
            }

            // Load mascot sprite folder
            currentMascotFolder = mascot.spriteFolder;
            LoadMascotDefaultSprite();

            // Load world-specific dialogue box if available
            LoadDialogueBoxForWorld(mascot.worldId);
        }

        private void LoadMascotDefaultSprite()
        {
            if (string.IsNullOrEmpty(currentMascotFolder)) return;

            // Sprite format: {spriteFolder}_neutral (e.g., Sprites/Mascots/island_whiskers_neutral)
            defaultMascotSprite = Resources.Load<Sprite>($"{currentMascotFolder}_neutral");
            if (defaultMascotSprite == null)
            {
                // Try loading with "default" suffix as fallback
                defaultMascotSprite = Resources.Load<Sprite>($"{currentMascotFolder}_default");
            }

            if (mascotImage != null && defaultMascotSprite != null)
            {
                mascotImage.sprite = defaultMascotSprite;
                mascotImage.enabled = true;
            }
            else if (mascotImage != null && defaultMascotSprite == null)
            {
                Debug.LogWarning($"[DialogueUI] Failed to load mascot sprite at {currentMascotFolder}_neutral");
            }
        }

        private void UpdateMascotExpression(string expression)
        {
            if (string.IsNullOrEmpty(currentMascotFolder) || mascotImage == null) return;

            Sprite expressionSprite = null;

            // Map common expressions to file naming
            string mappedExpression = expression;
            if (expression == "default") mappedExpression = "neutral";

            if (!string.IsNullOrEmpty(mappedExpression))
            {
                // Sprite format: {spriteFolder}_{expression} (e.g., Sprites/Mascots/island_whiskers_happy)
                expressionSprite = Resources.Load<Sprite>($"{currentMascotFolder}_{mappedExpression}");
            }

            mascotImage.sprite = expressionSprite ?? defaultMascotSprite;
            mascotImage.enabled = true;
        }

        private void LoadDialogueBoxForWorld(string worldId)
        {
            if (dialogueBoxImage == null || string.IsNullOrEmpty(worldId)) return;

            var worldBox = Resources.Load<Sprite>($"Sprites/UI/Dialogue/dialoguebox_{worldId}");
            if (worldBox != null)
            {
                dialogueBoxImage.sprite = worldBox;
            }
        }

        public void Show(bool immediate = false)
        {
            if (dialoguePanel == null) return;

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            dialoguePanel.SetActive(true);
            isVisible = true;

            if (immediate || showAnimationDuration <= 0)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
                dialoguePanel.transform.localScale = Vector3.one;
            }
            else
            {
                animationCoroutine = StartCoroutine(AnimateShow());
            }
        }

        public void Hide(bool immediate = false)
        {
            if (dialoguePanel == null) return;

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            if (immediate || hideAnimationDuration <= 0)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
                dialoguePanel.SetActive(false);
                isVisible = false;
            }
            else
            {
                animationCoroutine = StartCoroutine(AnimateHide());
            }
        }

        private IEnumerator AnimateShow()
        {
            float elapsed = 0f;

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            dialoguePanel.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            while (elapsed < showAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / showAnimationDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = eased;
                }
                dialoguePanel.transform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, eased);

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            dialoguePanel.transform.localScale = Vector3.one;
        }

        private IEnumerator AnimateHide()
        {
            float elapsed = 0f;

            while (elapsed < hideAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / hideAnimationDuration;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - t;
                }
                dialoguePanel.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.9f, 0.9f, 1f), t);

                yield return null;
            }

            dialoguePanel.SetActive(false);
            isVisible = false;
        }
    }
}
