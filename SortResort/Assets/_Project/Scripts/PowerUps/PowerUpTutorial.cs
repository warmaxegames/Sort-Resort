using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort
{
    public class PowerUpTutorial : MonoBehaviour
    {
        public static PowerUpTutorial Instance { get; private set; }

        private Canvas tutorialCanvas;
        private GameObject overlayPanel;
        private Image dimBackground;
        private Image backdropImage;
        private Image powerUpImage;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI descriptionText;
        private TextMeshProUGUI tapToDismissText;
        private Image glowImage;
        private Coroutine glowPulseCoroutine;
        private bool isShowing;
        private PowerUpType currentType;
        private System.Action onDismissed;
        private static Texture2D cachedGlowTexture;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Check if any power-ups should be unlocked at the start of a level.
        /// Called from LevelManager or UIManager after level loads.
        /// </summary>
        public void CheckUnlocks(int levelNumber, GameMode mode)
        {
            if (SaveManager.Instance == null) return;

            for (int i = 0; i < PowerUpData.POWER_UP_COUNT; i++)
            {
                var type = (PowerUpType)i;
                if (SaveManager.Instance.IsPowerUpUnlocked(type)) continue;

                if (PowerUpData.ShouldUnlockAtLevel(type, levelNumber, mode))
                {
                    StartCoroutine(ShowTutorialSequence(type));
                    return; // Only show one tutorial at a time
                }
            }
        }

        private IEnumerator ShowTutorialSequence(PowerUpType type)
        {
            // Wait a frame for level to fully set up
            yield return new WaitForSecondsRealtime(0.5f);

            // Pause gameplay
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                Time.timeScale = 0f;
            }

            isShowing = true;
            currentType = type;

            CreateTutorialUI(type);
            AudioManager.Instance?.PlaySpecialItemUnlockSound();

            // Wait for tap to dismiss (using unscaled time since game is paused)
            yield return new WaitUntil(() => !isShowing);

            // Unlock and grant charges
            SaveManager.Instance.UnlockPowerUp(type);
            PowerUpManager.Instance?.AddCharge(type, PowerUpData.INITIAL_GRANT);
            GameEvents.InvokePowerUpUnlocked(type);

            // Animate button from center to bottom bar position
            yield return StartCoroutine(AnimateButtonToBar(type));

            // Resume gameplay
            Time.timeScale = 1f;

            // Reconfigure the power-up bar to show the newly unlocked button
            if (UIManager.Instance != null)
            {
                var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
                var levelData = LevelManager.Instance?.CurrentLevel;
                // The bar will be reconfigured, showing the new button
            }

            // Check if there are more unlocks pending
            var currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            int levelNumber = GameManager.Instance?.CurrentLevelNumber ?? 0;
            for (int i = (int)type + 1; i < PowerUpData.POWER_UP_COUNT; i++)
            {
                var nextType = (PowerUpType)i;
                if (SaveManager.Instance.IsPowerUpUnlocked(nextType)) continue;
                if (PowerUpData.ShouldUnlockAtLevel(nextType, levelNumber, currentMode))
                {
                    StartCoroutine(ShowTutorialSequence(nextType));
                    break;
                }
            }
        }

        private void CreateTutorialUI(PowerUpType type)
        {
            var config = PowerUpData.GetConfig(type);

            // Create overlay canvas
            if (tutorialCanvas == null)
            {
                var canvasGO = new GameObject("PowerUpTutorialCanvas");
                canvasGO.transform.SetParent(transform);
                tutorialCanvas = canvasGO.AddComponent<Canvas>();
                tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                tutorialCanvas.sortingOrder = 6000; // Above everything
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Dim background
            overlayPanel = new GameObject("TutorialOverlay");
            overlayPanel.transform.SetParent(tutorialCanvas.transform, false);
            var overlayRect = overlayPanel.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            dimBackground = overlayPanel.AddComponent<Image>();
            dimBackground.color = new Color(0, 0, 0, 0.7f);

            // Make entire overlay tappable to dismiss
            var dismissBtn = overlayPanel.AddComponent<Button>();
            dismissBtn.targetGraphic = dimBackground;
            var colors = dismissBtn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            dismissBtn.colors = colors;
            dismissBtn.onClick.AddListener(DismissTutorial);

            // Backdrop board (860x701) - centered, positioned so icon sits on ribbon center
            // Board center at Y=0.48, top edge at ~0.663, ribbon center at ~0.63 (matches icon)
            var backdropGO = new GameObject("Backdrop");
            backdropGO.transform.SetParent(overlayPanel.transform, false);
            var backdropRect = backdropGO.AddComponent<RectTransform>();
            backdropRect.anchorMin = new Vector2(0.5f, 0.48f);
            backdropRect.anchorMax = new Vector2(0.5f, 0.48f);
            backdropRect.pivot = new Vector2(0.5f, 0.5f);
            backdropRect.anchoredPosition = Vector2.zero;
            backdropRect.sizeDelta = new Vector2(860, 701);
            backdropImage = backdropGO.AddComponent<Image>();
            backdropImage.preserveAspect = true;
            backdropImage.raycastTarget = false;
            var backdropTex = Resources.Load<Texture2D>("Sprites/UI/PowerUps/powerup_backdrop");
            if (backdropTex != null)
            {
                backdropImage.sprite = Sprite.Create(backdropTex,
                    new Rect(0, 0, backdropTex.width, backdropTex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }

            // Radial glow halo behind icon - raised above board
            var glowGO = new GameObject("GlowHalo");
            glowGO.transform.SetParent(overlayPanel.transform, false);
            var glowRect = glowGO.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.63f);
            glowRect.anchorMax = new Vector2(0.5f, 0.63f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(450, 450);
            glowImage = glowGO.AddComponent<Image>();
            glowImage.raycastTarget = false;
            glowImage.sprite = CreateGlowSprite();
            glowImage.color = new Color(1f, 0.95f, 0.7f, 0.6f); // warm golden white
            glowPulseCoroutine = StartCoroutine(PulseGlow());

            // Power-up icon (large) - raised above board, overlapping ribbon top
            var iconGO = new GameObject("PowerUpIcon");
            iconGO.transform.SetParent(overlayPanel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.63f);
            iconRect.anchorMax = new Vector2(0.5f, 0.63f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(250, 250);
            powerUpImage = iconGO.AddComponent<Image>();
            powerUpImage.preserveAspect = true;

            // Use intro sprite (no white circle badge) for the tutorial display
            var introTex = Resources.Load<Texture2D>($"Sprites/UI/PowerUps/{config.introSpriteName}");
            var tex = introTex ?? Resources.Load<Texture2D>($"Sprites/UI/PowerUps/{config.spriteName}");
            if (tex != null)
            {
                powerUpImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }

            // Title text - below the red ribbon on the wooden area
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(overlayPanel.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.15f, 0.488f);
            titleRect.anchorMax = new Vector2(0.85f, 0.538f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = config.displayName;
            titleText.fontSize = 52;
            titleText.font = FontManager.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            // Description text - on the wooden area, width = backdrop (860) minus 30px padding each side
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(overlayPanel.transform, false);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.5f, 0.38f);
            descRect.anchorMax = new Vector2(0.5f, 0.50f);
            descRect.pivot = new Vector2(0.5f, 0.5f);
            descRect.anchoredPosition = Vector2.zero;
            descRect.sizeDelta = new Vector2(620f, 0f); // 860 - 120px padding each side
            descriptionText = descGO.AddComponent<TextMeshProUGUI>();
            descriptionText.text = config.description;
            descriptionText.fontSize = 36;
            descriptionText.font = FontManager.SemiBold;
            descriptionText.alignment = TextAlignmentOptions.Center;
            descriptionText.textWrappingMode = TextWrappingModes.Normal;
            descriptionText.color = new Color(1f, 1f, 1f, 0.9f);
            descriptionText.raycastTarget = false;

            // "Tap to continue" text - near bottom of the board
            var tapGO = new GameObject("TapToDismiss");
            tapGO.transform.SetParent(overlayPanel.transform, false);
            var tapRect = tapGO.AddComponent<RectTransform>();
            tapRect.anchorMin = new Vector2(0.2f, 0.318f);
            tapRect.anchorMax = new Vector2(0.8f, 0.358f);
            tapRect.offsetMin = Vector2.zero;
            tapRect.offsetMax = Vector2.zero;
            tapToDismissText = tapGO.AddComponent<TextMeshProUGUI>();
            tapToDismissText.text = "Tap to continue";
            tapToDismissText.fontSize = 30;
            tapToDismissText.font = FontManager.Medium;
            tapToDismissText.alignment = TextAlignmentOptions.Center;
            tapToDismissText.color = new Color(1f, 1f, 1f, 0.5f);
            tapToDismissText.raycastTarget = false;

            // Pulse the tap text
            StartCoroutine(PulseTapText());
        }

        private Sprite CreateGlowSprite()
        {
            if (cachedGlowTexture == null)
            {
                int size = 128;
                cachedGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                cachedGlowTexture.wrapMode = TextureWrapMode.Clamp;
                float center = (size - 1) * 0.5f;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x - center) / center;
                        float dy = (y - center) / center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        // Smooth falloff: solid center, soft fade to transparent
                        float alpha = Mathf.Clamp01(1f - dist);
                        alpha = alpha * alpha; // quadratic for softer edge
                        byte a = (byte)(alpha * 255);
                        pixels[y * size + x] = new Color32(255, 255, 255, a);
                    }
                }
                cachedGlowTexture.SetPixels32(pixels);
                cachedGlowTexture.Apply();
            }
            return Sprite.Create(cachedGlowTexture,
                new Rect(0, 0, cachedGlowTexture.width, cachedGlowTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private IEnumerator PulseGlow()
        {
            if (glowImage == null) yield break;
            var baseColor = glowImage.color;
            var rt = glowImage.rectTransform;
            while (isShowing && glowImage != null)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 1.2f, 1f);
                // Smooth sine for natural pulse
                float ease = Mathf.Sin(t * Mathf.PI);
                float scale = Mathf.Lerp(0.85f, 1.15f, ease);
                float alpha = Mathf.Lerp(0.35f, 0.7f, ease);
                rt.localScale = Vector3.one * scale;
                glowImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        private IEnumerator PulseTapText()
        {
            while (isShowing && tapToDismissText != null)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
                float alpha = Mathf.Lerp(0.3f, 0.7f, t);
                if (tapToDismissText != null)
                    tapToDismissText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }

        private void DismissTutorial()
        {
            isShowing = false;
            AudioManager.Instance?.PlayButtonClick();
        }

        private IEnumerator AnimateButtonToBar(PowerUpType type)
        {
            if (overlayPanel == null) yield break;

            // Get the power-up bar button position
            var barUI = FindAnyObjectByType<PowerUpBarUI>();
            if (barUI == null)
            {
                CleanupTutorialUI();
                yield break;
            }

            // Reconfigure bar to show the newly unlocked button
            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            var levelData = LevelManager.Instance?.CurrentLevel;
            barUI.ConfigureForLevel(mode, levelData);

            // Get target button world position
            var targetButton = barUI.GetButtonRoot(type);
            if (targetButton == null)
            {
                CleanupTutorialUI();
                yield break;
            }

            // Hide the actual bar button until the tween arrives there
            targetButton.SetActive(false);

            // Fade out overlay background
            var startColor = dimBackground.color;
            float elapsed = 0f;
            float fadeDuration = 0.3f;

            // Hide text elements and backdrop
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (descriptionText != null) descriptionText.gameObject.SetActive(false);
            if (tapToDismissText != null) tapToDismissText.gameObject.SetActive(false);
            if (backdropImage != null) backdropImage.gameObject.SetActive(false);
            if (glowImage != null) glowImage.gameObject.SetActive(false);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                if (dimBackground != null)
                    dimBackground.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
                yield return null;
            }

            // Tween the icon from center to the button position
            if (powerUpImage != null && targetButton != null)
            {
                var iconRect = powerUpImage.rectTransform;

                // Convert target button screen position to tutorial canvas space
                Vector3 targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, targetButton.transform.position);
                Vector2 targetLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayPanel.GetComponent<RectTransform>(), targetScreenPos, null, out targetLocalPos);

                Vector2 startPos = iconRect.anchoredPosition;
                Vector2 startSize = iconRect.sizeDelta;
                Vector2 targetSize = new Vector2(150, 150);

                elapsed = 0f;
                float tweenDuration = 0.6f;

                while (elapsed < tweenDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / tweenDuration);

                    if (iconRect != null)
                    {
                        // Reset anchor to center for tweening
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.anchoredPosition = Vector2.Lerp(startPos, targetLocalPos, t);
                        iconRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
                    }
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.1f);

            // Hide the tweened intro icon and show the real bar button in the same frame
            // The bar button has the white circle badge, creating a seamless "circle appears" effect
            if (powerUpImage != null)
                powerUpImage.gameObject.SetActive(false);
            if (targetButton != null)
                targetButton.SetActive(true);

            CleanupTutorialUI();
        }

        private void CleanupTutorialUI()
        {
            if (glowPulseCoroutine != null)
            {
                StopCoroutine(glowPulseCoroutine);
                glowPulseCoroutine = null;
            }
            if (overlayPanel != null)
                Destroy(overlayPanel);
            overlayPanel = null;
            powerUpImage = null;
            backdropImage = null;
            glowImage = null;
            titleText = null;
            descriptionText = null;
            tapToDismissText = null;
        }
    }
}
