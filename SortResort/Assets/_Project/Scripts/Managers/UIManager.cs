using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
using SortResort.UI;

namespace SortResort
{
    /// <summary>
    /// Manages all UI screens and provides runtime UI creation for testing.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// Creates a rounded rectangle texture at runtime.
        /// </summary>
        private static Texture2D CreateRoundedRectTexture(int width, int height, int cornerRadius, Color fillColor)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Check if pixel is inside the rounded rectangle
                    bool inside = true;

                    // Check corners
                    if (x < cornerRadius && y < cornerRadius)
                    {
                        // Bottom-left corner
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
                        inside = dist <= cornerRadius;
                    }
                    else if (x >= width - cornerRadius && y < cornerRadius)
                    {
                        // Bottom-right corner
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, cornerRadius));
                        inside = dist <= cornerRadius;
                    }
                    else if (x < cornerRadius && y >= height - cornerRadius)
                    {
                        // Top-left corner
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, height - cornerRadius - 1));
                        inside = dist <= cornerRadius;
                    }
                    else if (x >= width - cornerRadius && y >= height - cornerRadius)
                    {
                        // Top-right corner
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, height - cornerRadius - 1));
                        inside = dist <= cornerRadius;
                    }

                    pixels[y * width + x] = inside ? fillColor : transparent;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a sprite from a rounded rectangle texture.
        /// </summary>
        private static Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius, Color fillColor)
        {
            var texture = CreateRoundedRectTexture(width, height, cornerRadius, fillColor);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static UIManager Instance { get; private set; }

        [Header("Screen References")]
        [SerializeField] private GameHUDScreen hudScreen;
        [SerializeField] private LevelCompleteScreen levelCompleteScreen;
        [SerializeField] private PauseMenuScreen pauseMenuScreen;

        [Header("Canvas Settings")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasScaler canvasScaler;

        // Runtime created UI elements
        private GameObject splashPanel;
        private SplashScreen splashScreen;
        private GameObject hudPanel;
        private GameObject levelCompletePanel;
        private AnimatedLevelComplete animatedLevelComplete;
        private Image levelCompleteGreyStarsImage;
        private Image levelCompleteStar1Image;
        private Image levelCompleteStar2Image;
        private Image levelCompleteStar3Image;
        private Sprite[] star1Frames;
        private Sprite[] star2Frames;
        private Sprite[] star3Frames;
        private TextMeshProUGUI levelCompleteLevelNumberText;
        private TextMeshProUGUI levelCompleteMoveCountText;
        private CanvasGroup greyStarsCanvasGroup;
        private Image dancingStarsImage;
        private Sprite[] dancingStarsFrames;
        private Coroutine dancingStarsCoroutine;
        private Image bottomBoardImage;
        private Sprite[] bottomBoardFrames;
        private GameObject buttonsContainerGO;
        private Coroutine levelCompleteSequence;
        private Image levelCompleteMascotImage;
        private MascotAnimator levelCompleteMascotAnimator;
        private Image levelCompleteTextImage;
        private static Sprite[] cachedLevelCompleteTextFrames;
        private float levelCompleteTextFrameRate = 24f;
        private LevelCompletionData lastCompletionData;
        private Coroutine newRecordPulseCoroutine;

        // New level complete screen elements
        private Image dimOverlayImage;
        private CanvasGroup dimOverlayCanvasGroup;
        private Image victoryBoardImage;
        private CanvasGroup victoryBoardCanvasGroup;
        private Image levelUIImage;
        private CanvasGroup levelUICanvasGroup;
        private Image movesUIImage;
        private CanvasGroup movesUICanvasGroup;
        private Image timerIconImage;
        private CanvasGroup timerIconCanvasGroup;
        private Image freeOverlayImage;
        private CanvasGroup freeOverlayCanvasGroup;
        private Image hardOverlayImage;
        private CanvasGroup hardOverlayCanvasGroup;
        private Image timerBarUIImage;
        private CanvasGroup timerBarUICanvasGroup;
        private TextMeshProUGUI timerBarText;
        private Image newRecordUIImage;
        private CanvasGroup newRecordUICanvasGroup;

        // Mode-specific HUD overlay
        private GameObject hudModeOverlay;
        private Image hudOverlayBgImage;
        private Image hudWorldIconImage;
        private TextMeshProUGUI overlayLevelText;
        private TextMeshProUGUI overlayMovesText;
        private TextMeshProUGUI overlayTimerText;
        private Image hudPanelBgImage;
        private GameObject statsContainerGO;
        private GameObject hudSettingsOverlay;
        private Button undoSpriteButton;
        private CanvasGroup undoSpriteCanvasGroup;

        private GameObject levelFailedPanel;
        private TextMeshProUGUI levelFailedReasonText;
        private Image failedBottomBoardImage;
        private Sprite[] failedBottomBoardFrames;
        private GameObject failedButtonsContainerGO;
        private Coroutine failedScreenSequence;
        private GameObject levelSelectPanel;
        private LevelSelectScreen levelSelectScreen;
        private GameObject settingsPanel;
        private SettingsScreen settingsScreen;
        private GameObject pauseMenuPanel;
        private TextMeshProUGUI levelTitleText;
        private TextMeshProUGUI moveCountText;
        private TextMeshProUGUI matchCountText;
        private TextMeshProUGUI itemsRemainingText;
        private TextMeshProUGUI timerText;
        private GameObject timerContainer;
        private Image[] starImages;
        private GameObject starDisplayGO;
        private Button undoButton;
        private CanvasGroup undoButtonCanvasGroup;
#if UNITY_EDITOR
        private Button autoSolveButton;
        private Button recordButton;
        private bool isRecordingMoves = false;
        private Button devPrevLevelButton;
        private Button devNextLevelButton;
#endif

        // Achievements screen
        private GameObject achievementsPanel;
        private Transform achievementsListContent;
        private string currentAchievementTab = Achievement.TAB_GENERAL;
        private Dictionary<string, Button> achievementTabButtons = new Dictionary<string, Button>();
        private Dictionary<string, Image> achievementTabSpriteImages = new Dictionary<string, Image>();
        private Transform achievementTabsContainer;
        private Image achievementWorldIconImage;
        private TextMeshProUGUI achievementTitleBarText;
        private TextMeshProUGUI achievementPointsText;
        private Sprite achvTabSprite;
        private Sprite achvTabPressedSprite;

        // Achievement notification
        private GameObject achievementNotificationPanel;
        private GameObject achievementDetailPanel;
        private TextMeshProUGUI achievementNameText;
        private TextMeshProUGUI achievementDescText;
        private TextMeshProUGUI achievementRewardText;
        private TextMeshProUGUI achievementDetailNameText;
        private TextMeshProUGUI achievementDetailDescText;
        private TextMeshProUGUI achievementDetailRewardText;
        private TextMeshProUGUI achievementDetailTierText;
        private Image achievementIconImage;
        private Image achievementTierBg;
        private Image achievementDetailTierBg;
        private Queue<Achievement> achievementQueue = new Queue<Achievement>();
        private bool isShowingAchievement = false;
        private bool isShowingAchievementDetail = false;
        private Coroutine achievementCoroutine;
        private Achievement currentAchievement;

        // Dialogue system
        private GameObject dialoguePanel;
        private DialogueUI dialogueUI;
        private Image dialogueBoxImage;
        private Image dialogueMascotImage;
        private TextMeshProUGUI dialogueText;
        private GameObject dialogueContinueIndicator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelStarted += OnLevelStarted;
            GameEvents.OnMoveUsed += OnMoveUsed;
            GameEvents.OnMatchCountChanged += OnMatchCountChanged;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnLevelCompletedDetailed += OnLevelCompletedDetailed;
            GameEvents.OnLevelRestarted += OnLevelRestarted;
            GameEvents.OnGamePaused += OnGamePausedUI;
            GameEvents.OnGameResumed += OnGameResumedUI;
            GameEvents.OnSettingsClosed += OnSettingsClosedUI;
            GameEvents.OnTimerUpdated += OnTimerUpdated;
            GameEvents.OnTimerFrozen += OnTimerFrozen;
            GameEvents.OnLevelFailed += OnLevelFailed;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged += OnItemsRemainingChanged;
                LevelManager.Instance.OnUndoAvailableChanged += OnUndoAvailableChanged;
            }

            if (AchievementManager.Instance != null)
            {
                // Unsubscribe first to prevent duplicates
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
                AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= OnLevelStarted;
            GameEvents.OnMoveUsed -= OnMoveUsed;
            GameEvents.OnMatchCountChanged -= OnMatchCountChanged;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnLevelCompletedDetailed -= OnLevelCompletedDetailed;
            GameEvents.OnLevelRestarted -= OnLevelRestarted;
            GameEvents.OnGamePaused -= OnGamePausedUI;
            GameEvents.OnGameResumed -= OnGameResumedUI;
            GameEvents.OnSettingsClosed -= OnSettingsClosedUI;
            GameEvents.OnTimerUpdated -= OnTimerUpdated;
            GameEvents.OnTimerFrozen -= OnTimerFrozen;
            GameEvents.OnLevelFailed -= OnLevelFailed;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged -= OnItemsRemainingChanged;
                LevelManager.Instance.OnUndoAvailableChanged -= OnUndoAvailableChanged;
            }

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
            }
        }

        private void OnGamePausedUI()
        {
            Debug.Log("[UIManager] Game paused - showing pause menu");
            ShowPauseMenu();
        }

        private void OnGameResumedUI()
        {
            Debug.Log("[UIManager] Game resumed - hiding pause menu");
            HidePauseMenu();
        }

        private void OnSettingsClosedUI()
        {
            // Re-show pause menu if game is still paused when settings closes
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            {
                Debug.Log("[UIManager] Settings closed while paused - re-showing pause menu");
                ShowPauseMenu();
            }
        }

        private void Start()
        {
            // Subscribe to LevelManager after it's initialized
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged += OnItemsRemainingChanged;
                LevelManager.Instance.OnUndoAvailableChanged += OnUndoAvailableChanged;
            }
        }

        /// <summary>
        /// Create runtime UI for testing without prefabs
        /// </summary>
        public void CreateRuntimeUI()
        {
            CreateCanvas();
            CreateSplashPanel();
            CreateLevelSelectPanel();
            CreateHUDPanel();
            CreateHUDModeOverlay();
            CreateLevelCompletePanel();
            CreateLevelFailedPanel();
            CreateSettingsPanel();
            CreatePauseMenuPanel();
            CreateAchievementNotificationPanel();
            CreateAchievementsPanel();
            CreateDialoguePanel();

            // Hide all panels initially
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
            if (levelFailedPanel != null)
                levelFailedPanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (hudModeOverlay != null)
                hudModeOverlay.SetActive(false);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(false);
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            if (achievementNotificationPanel != null)
                achievementNotificationPanel.SetActive(false);
            if (achievementDetailPanel != null)
                achievementDetailPanel.SetActive(false);
            if (achievementsPanel != null)
                achievementsPanel.SetActive(false);
            // Note: dialoguePanel stays active - DialogueUI manages its own visibility via CanvasGroup

            // Show splash screen first
            if (splashPanel != null)
                splashPanel.SetActive(true);

            Debug.Log("[UIManager] Runtime UI created - showing splash screen");
        }

        private IEnumerator PlayWorldmapMusicDelayed()
        {
            yield return null; // Wait one frame

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWorldmapMusic();
                Debug.Log("[UIManager] Started worldmap music (delayed)");
            }
            else
            {
                Debug.LogWarning("[UIManager] AudioManager not available for worldmap music");
            }
        }

        /// <summary>
        /// Show splash screen (called on game start)
        /// </summary>
        public void ShowSplash()
        {
            if (splashPanel != null)
                splashPanel.SetActive(true);
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (hudModeOverlay != null)
                hudModeOverlay.SetActive(false);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(false);
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
        }

        /// <summary>
        /// Show level select and hide gameplay UI
        /// </summary>
        public void ShowLevelSelect()
        {
            if (splashPanel != null)
                splashPanel.SetActive(false);
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(true);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (hudModeOverlay != null)
                hudModeOverlay.SetActive(false);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(false);
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            // Notify level select it's being shown (triggers first-play dialogue once)
            levelSelectScreen?.OnShow();

            // Refresh level select to show updated stars
            levelSelectScreen?.RefreshDisplay();

            // Play worldmap music
            AudioManager.Instance?.PlayWorldmapMusic();
        }

        /// <summary>
        /// Show gameplay UI and hide level select
        /// </summary>
        public void ShowGameplay()
        {
            if (splashPanel != null)
                splashPanel.SetActive(false);
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(true);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(true);
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
        }

        private void CreateCanvas()
        {
            if (mainCanvas != null) return;

            var canvasGO = new GameObject("UI Canvas");
            canvasGO.transform.SetParent(transform);

            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;

            canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists (required for UI interaction)
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Use InputSystemUIInputModule for new Input System
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[UIManager] Created EventSystem with InputSystemUIInputModule");
            }
        }

        private void CreateSplashPanel()
        {
            splashPanel = new GameObject("Splash Panel");
            splashPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = splashPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Add CanvasGroup for fading
            var canvasGroup = splashPanel.AddComponent<CanvasGroup>();

            // Load splash screen background sprite
            var splashSprite = Resources.Load<Sprite>("Sprites/UI/Backgrounds/splashscreen");
            if (splashSprite == null)
            {
                // Try alternate path
                splashSprite = Resources.Load<Sprite>("Art/UI/Backgrounds/splashscreen");
            }

            // Background image
            var bgImage = splashPanel.AddComponent<Image>();
            if (splashSprite != null)
            {
                bgImage.sprite = splashSprite;
                bgImage.preserveAspect = false; // Fill entire screen
                bgImage.type = Image.Type.Simple;
            }
            else
            {
                // Fallback: solid color background
                bgImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
                Debug.LogWarning("[UIManager] Splash screen sprite not found, using fallback color");
            }

            // Load play button sprites
            var playNormalSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/play_button");
            var playPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/play_button_pressed");
            if (playNormalSprite == null)
            {
                playNormalSprite = Resources.Load<Sprite>("Art/UI/Buttons/play_button");
                playPressedSprite = Resources.Load<Sprite>("Art/UI/Buttons/play_button_pressed");
            }

            // Play button container (for positioning)
            var playBtnContainer = new GameObject("PlayButtonContainer");
            playBtnContainer.transform.SetParent(splashPanel.transform, false);
            var containerRect = playBtnContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 200); // 200 pixels from bottom
            containerRect.sizeDelta = new Vector2(366, 114); // Match Godot dimensions

            // Play button
            var playBtnGO = new GameObject("PlayButton");
            playBtnGO.transform.SetParent(playBtnContainer.transform, false);
            var playBtnRect = playBtnGO.AddComponent<RectTransform>();
            playBtnRect.anchorMin = Vector2.zero;
            playBtnRect.anchorMax = Vector2.one;
            playBtnRect.offsetMin = Vector2.zero;
            playBtnRect.offsetMax = Vector2.zero;

            var playBtnImage = playBtnGO.AddComponent<Image>();
            if (playNormalSprite != null)
            {
                playBtnImage.sprite = playNormalSprite;
                playBtnImage.type = Image.Type.Simple;
                playBtnImage.preserveAspect = true;
            }
            else
            {
                // Fallback: colored button
                playBtnImage.color = new Color(0.4f, 0.8f, 0.9f, 1f);
                Debug.LogWarning("[UIManager] Play button sprites not found, using fallback");

                // Add text for fallback button
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(playBtnGO.transform, false);
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = "PLAY";
                tmp.fontSize = 48;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            var playBtn = playBtnGO.AddComponent<Button>();
            playBtn.targetGraphic = playBtnImage;

            // Setup sprite swap for pressed state
            if (playNormalSprite != null && playPressedSprite != null)
            {
                playBtn.transition = Selectable.Transition.SpriteSwap;
                var spriteState = new SpriteState
                {
                    pressedSprite = playPressedSprite,
                    highlightedSprite = playNormalSprite,
                    selectedSprite = playNormalSprite,
                    disabledSprite = playNormalSprite
                };
                playBtn.spriteState = spriteState;
            }
            else
            {
                // Fallback: color tint
                playBtn.transition = Selectable.Transition.ColorTint;
                var colors = playBtn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                playBtn.colors = colors;
            }

            // Add SplashScreen component and initialize it
            splashScreen = splashPanel.AddComponent<SplashScreen>();

            // Set canvasGroup via reflection (needed for BaseScreen)
            var screenType = typeof(SplashScreen);
            var baseType = typeof(BaseScreen);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            baseType.GetField("canvasGroup", flags)?.SetValue(splashScreen, canvasGroup);

            // Use the Initialize method to properly set up the button
            splashScreen.Initialize(playBtn, bgImage, playNormalSprite, playPressedSprite);

            // Subscribe to play button click
            splashScreen.OnPlayClicked += OnSplashPlayClicked;

            Debug.Log($"[UIManager] Splash panel created - bgSprite:{splashSprite != null}, playNormal:{playNormalSprite != null}, playPressed:{playPressedSprite != null}");
        }

        private void OnSplashPlayClicked()
        {
            Debug.Log("[UIManager] Play clicked on splash screen");

            // Update game state
            GameManager.Instance?.SetState(GameState.LevelSelection);

            // Use TransitionManager if available, otherwise just switch
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.FadeOut(() =>
                {
                    ShowLevelSelect();
                    TransitionManager.Instance.FadeIn();
                });
            }
            else
            {
                // Simple immediate transition
                ShowLevelSelect();
            }
        }

        private void CreateLevelSelectPanel()
        {
            levelSelectPanel = new GameObject("Level Select Panel");
            levelSelectPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = levelSelectPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background - use base_world_background sprite
            var bg = levelSelectPanel.AddComponent<Image>();
            var bgSprite = Resources.Load<Sprite>("Sprites/UI/Backgrounds/base_world_background");
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = false;
            }
            else
            {
                bg.color = new Color(0.64f, 0.87f, 0.87f, 1f); // Fallback cyan
            }

            // ============================================
            // TOP BAR - Wood plank background with settings button
            // ============================================
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(levelSelectPanel.transform, false);
            var topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(0, 120);

            // Wood plank background (blank_topbar)
            var topBarBg = topBar.AddComponent<Image>();
            var topBarSprite = Resources.Load<Sprite>("Sprites/UI/Overlays/blank_topbar");
            if (topBarSprite != null)
            {
                topBarBg.sprite = topBarSprite;
                topBarBg.type = Image.Type.Sliced;
                topBarBg.preserveAspect = false;
            }
            else
            {
                topBarBg.color = new Color(0.55f, 0.35f, 0.2f, 1f);
            }

            // Settings button - centered vertically in topbar
            var settingsBtnGO = new GameObject("SettingsButton");
            settingsBtnGO.transform.SetParent(topBar.transform, false);
            var settingsBtnRect = settingsBtnGO.AddComponent<RectTransform>();
            settingsBtnRect.anchorMin = new Vector2(1, 0.5f);
            settingsBtnRect.anchorMax = new Vector2(1, 0.5f);
            settingsBtnRect.pivot = new Vector2(1, 0.5f);
            settingsBtnRect.anchoredPosition = new Vector2(-15, 0); // Centered vertically (no Y offset)
            settingsBtnRect.sizeDelta = new Vector2(100, 100);

            var settingsBtnImg = settingsBtnGO.AddComponent<Image>();
            var settingsSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/settings_button");
            if (settingsSprite != null)
            {
                settingsBtnImg.sprite = settingsSprite;
                settingsBtnImg.preserveAspect = true;
            }
            else
            {
                settingsBtnImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            var settingsBtn = settingsBtnGO.AddComponent<Button>();
            settingsBtn.targetGraphic = settingsBtnImg;
            settingsBtn.onClick.AddListener(OnSettingsClicked);

            // Reset Achievements button (for testing) - small button to left of settings
            var resetAchBtnGO = new GameObject("ResetAchievementsButton");
            resetAchBtnGO.transform.SetParent(topBar.transform, false);
            var resetAchBtnRect = resetAchBtnGO.AddComponent<RectTransform>();
            resetAchBtnRect.anchorMin = new Vector2(1, 0.5f);
            resetAchBtnRect.anchorMax = new Vector2(1, 0.5f);
            resetAchBtnRect.pivot = new Vector2(1, 0.5f);
            resetAchBtnRect.anchoredPosition = new Vector2(-125, 0); // To the left of settings
            resetAchBtnRect.sizeDelta = new Vector2(90, 50);

            var resetAchBtnImg = resetAchBtnGO.AddComponent<Image>();
            resetAchBtnImg.color = new Color(0.8f, 0.3f, 0.3f, 1f); // Red for danger/reset

            var resetAchBtn = resetAchBtnGO.AddComponent<Button>();
            resetAchBtn.targetGraphic = resetAchBtnImg;
            resetAchBtn.onClick.AddListener(OnResetAchievementsClicked);

            // Button text
            var resetAchTextGO = new GameObject("Text");
            resetAchTextGO.transform.SetParent(resetAchBtnGO.transform, false);
            var resetAchTextRect = resetAchTextGO.AddComponent<RectTransform>();
            resetAchTextRect.anchorMin = Vector2.zero;
            resetAchTextRect.anchorMax = Vector2.one;
            resetAchTextRect.offsetMin = new Vector2(3, 3);
            resetAchTextRect.offsetMax = new Vector2(-3, -3);
            var resetAchText = resetAchTextGO.AddComponent<TextMeshProUGUI>();
            resetAchText.text = "RST\nACH";
            resetAchText.fontSize = 16;
            resetAchText.fontStyle = FontStyles.Bold;
            resetAchText.alignment = TextAlignmentOptions.Center;
            resetAchText.color = Color.white;

            // Achievements/Trophy button - to the left of reset button
            var trophyBtnGO = new GameObject("AchievementsButton");
            trophyBtnGO.transform.SetParent(topBar.transform, false);
            var trophyBtnRect = trophyBtnGO.AddComponent<RectTransform>();
            trophyBtnRect.anchorMin = new Vector2(1, 0.5f);
            trophyBtnRect.anchorMax = new Vector2(1, 0.5f);
            trophyBtnRect.pivot = new Vector2(1, 0.5f);
            trophyBtnRect.anchoredPosition = new Vector2(-225, 0); // To the left of reset button
            trophyBtnRect.sizeDelta = new Vector2(90, 90);

            var trophyBtnImg = trophyBtnGO.AddComponent<Image>();
            trophyBtnImg.color = new Color(0.9f, 0.75f, 0.2f, 1f); // Gold color for trophy

            var trophyBtn = trophyBtnGO.AddComponent<Button>();
            trophyBtn.targetGraphic = trophyBtnImg;
            trophyBtn.onClick.AddListener(OnAchievementsClicked);

            // Trophy icon text (placeholder)
            var trophyTextGO = new GameObject("Text");
            trophyTextGO.transform.SetParent(trophyBtnGO.transform, false);
            var trophyTextRect = trophyTextGO.AddComponent<RectTransform>();
            trophyTextRect.anchorMin = Vector2.zero;
            trophyTextRect.anchorMax = Vector2.one;
            trophyTextRect.offsetMin = Vector2.zero;
            trophyTextRect.offsetMax = Vector2.zero;
            var trophyText = trophyTextGO.AddComponent<TextMeshProUGUI>();
            trophyText.text = "ACH";
            trophyText.fontSize = 28;
            trophyText.fontStyle = FontStyles.Bold;
            trophyText.alignment = TextAlignmentOptions.Center;
            trophyText.color = new Color(0.4f, 0.25f, 0f, 1f); // Dark brown

            // Profile overlay - LARGER, positioned to hang OVER the topbar onto the background
            // Parent to levelSelectPanel so it can extend beyond topbar
            var profileGO = new GameObject("ProfileOverlay");
            profileGO.transform.SetParent(levelSelectPanel.transform, false);
            var profileRect = profileGO.AddComponent<RectTransform>();
            profileRect.anchorMin = new Vector2(0, 1);
            profileRect.anchorMax = new Vector2(0, 1);
            profileRect.pivot = new Vector2(0, 1);
            profileRect.anchoredPosition = new Vector2(5, -15); // Move right and down from edge
            profileRect.sizeDelta = new Vector2(700, 240); // Even larger

            var profileImg = profileGO.AddComponent<Image>();
            profileImg.raycastTarget = false;
            var profileSprite = Resources.Load<Sprite>("Sprites/UI/Overlays/profile_overlay");
            if (profileSprite != null)
            {
                profileImg.sprite = profileSprite;
                profileImg.preserveAspect = true;
            }
            else
            {
                profileImg.color = new Color(0.9f, 0.7f, 0.5f, 1f);
            }

            // Player name text - moved right to not overlap mascot
            var playerNameGO = new GameObject("PlayerName");
            playerNameGO.transform.SetParent(profileGO.transform, false);
            var playerNameRect = playerNameGO.AddComponent<RectTransform>();
            playerNameRect.anchorMin = new Vector2(0.38f, 0.38f); // Moved further right
            playerNameRect.anchorMax = new Vector2(0.95f, 0.62f);
            playerNameRect.offsetMin = Vector2.zero;
            playerNameRect.offsetMax = Vector2.zero;

            var playerNameText = playerNameGO.AddComponent<TextMeshProUGUI>();
            playerNameText.text = "PLAYER";
            playerNameText.fontSize = 32;
            playerNameText.fontStyle = FontStyles.Bold;
            playerNameText.alignment = TextAlignmentOptions.MidlineLeft;
            playerNameText.color = Color.white;
            playerNameText.raycastTarget = false;

            // ============================================
            // WORLD DISPLAY AREA - Large world image with navigation arrows
            // ============================================
            var worldArea = new GameObject("WorldArea");
            worldArea.transform.SetParent(levelSelectPanel.transform, false);
            var worldAreaRect = worldArea.AddComponent<RectTransform>();
            worldAreaRect.anchorMin = new Vector2(0, 0.52f);
            worldAreaRect.anchorMax = new Vector2(1, 0.92f);
            worldAreaRect.offsetMin = Vector2.zero;
            worldAreaRect.offsetMax = Vector2.zero;

            // Prev world button (larger)
            var prevBtnGO = new GameObject("PrevWorld Button");
            prevBtnGO.transform.SetParent(worldArea.transform, false);
            var prevBtnRect = prevBtnGO.AddComponent<RectTransform>();
            prevBtnRect.anchorMin = new Vector2(0, 0.5f);
            prevBtnRect.anchorMax = new Vector2(0, 0.5f);
            prevBtnRect.pivot = new Vector2(0, 0.5f);
            prevBtnRect.anchoredPosition = new Vector2(10, 0);
            prevBtnRect.sizeDelta = new Vector2(170, 195);

            var prevBtnImg = prevBtnGO.AddComponent<Image>();
            var prevSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left");
            var prevPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left_pressed");
            if (prevSprite != null)
            {
                prevBtnImg.sprite = prevSprite;
                prevBtnImg.preserveAspect = true;
            }
            else
            {
                prevBtnImg.color = new Color(0.5f, 0.3f, 0.7f, 1f);
            }

            var prevBtn = prevBtnGO.AddComponent<Button>();
            prevBtn.targetGraphic = prevBtnImg;
            if (prevSprite != null && prevPressedSprite != null)
            {
                prevBtn.transition = Selectable.Transition.SpriteSwap;
                var spriteState = new SpriteState { pressedSprite = prevPressedSprite };
                prevBtn.spriteState = spriteState;
            }

            // World sprite display (center, large)
            var worldSpriteGO = new GameObject("WorldSprite");
            worldSpriteGO.transform.SetParent(worldArea.transform, false);
            var worldSpriteRect = worldSpriteGO.AddComponent<RectTransform>();
            worldSpriteRect.anchorMin = new Vector2(0.12f, 0);
            worldSpriteRect.anchorMax = new Vector2(0.88f, 1);
            worldSpriteRect.offsetMin = Vector2.zero;
            worldSpriteRect.offsetMax = Vector2.zero;

            var worldSpriteImg = worldSpriteGO.AddComponent<Image>();
            worldSpriteImg.preserveAspect = true;
            var defaultWorldSprite = Resources.Load<Sprite>("Sprites/UI/Worlds/island_world");
            if (defaultWorldSprite != null)
                worldSpriteImg.sprite = defaultWorldSprite;

            // Next world button (larger)
            var nextBtnGO = new GameObject("NextWorld Button");
            nextBtnGO.transform.SetParent(worldArea.transform, false);
            var nextBtnRect = nextBtnGO.AddComponent<RectTransform>();
            nextBtnRect.anchorMin = new Vector2(1, 0.5f);
            nextBtnRect.anchorMax = new Vector2(1, 0.5f);
            nextBtnRect.pivot = new Vector2(1, 0.5f);
            nextBtnRect.anchoredPosition = new Vector2(-10, 0);
            nextBtnRect.sizeDelta = new Vector2(170, 195);

            var nextBtnImg = nextBtnGO.AddComponent<Image>();
            var nextSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right");
            var nextPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right_pressed");
            if (nextSprite != null)
            {
                nextBtnImg.sprite = nextSprite;
                nextBtnImg.preserveAspect = true;
            }
            else
            {
                nextBtnImg.color = new Color(0.5f, 0.3f, 0.7f, 1f);
            }

            var nextBtn = nextBtnGO.AddComponent<Button>();
            nextBtn.targetGraphic = nextBtnImg;
            if (nextSprite != null && nextPressedSprite != null)
            {
                nextBtn.transition = Selectable.Transition.SpriteSwap;
                var spriteState = new SpriteState { pressedSprite = nextPressedSprite };
                nextBtn.spriteState = spriteState;
            }

            // ============================================
            // MODE TABS ROW - Between world area and level grid
            // ============================================
            var modeTabContainerGO = new GameObject("ModeTabContainer");
            modeTabContainerGO.transform.SetParent(levelSelectPanel.transform, false);
            var modeTabRect = modeTabContainerGO.AddComponent<RectTransform>();
            modeTabRect.anchorMin = new Vector2(0, 0.465f);
            modeTabRect.anchorMax = new Vector2(1, 0.51f);
            modeTabRect.offsetMin = new Vector2(20, 0);
            modeTabRect.offsetMax = new Vector2(-20, 0);

            var modeTabLayout = modeTabContainerGO.AddComponent<HorizontalLayoutGroup>();
            modeTabLayout.spacing = 8;
            modeTabLayout.childAlignment = TextAnchor.MiddleCenter;
            modeTabLayout.childForceExpandWidth = true;
            modeTabLayout.childForceExpandHeight = true;
            modeTabLayout.padding = new RectOffset(5, 5, 4, 4);

            // ============================================
            // LEVEL GRID SCROLL AREA - Rounded corners, larger scrollbar
            // ============================================
            var scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(levelSelectPanel.transform, false);
            var scrollAreaRect = scrollArea.AddComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0, 0);
            scrollAreaRect.anchorMax = new Vector2(1, 0.46f);
            scrollAreaRect.offsetMin = new Vector2(30, 25);
            scrollAreaRect.offsetMax = new Vector2(-30, 0);

            // Dark gray/olive background with rounded corners
            var scrollBg = scrollArea.AddComponent<Image>();
            var roundedBgSprite = CreateRoundedRectSprite(256, 256, 30, new Color(0.34f, 0.37f, 0.32f, 1f));
            scrollBg.sprite = roundedBgSprite;
            scrollBg.type = Image.Type.Sliced;
            // Set 9-slice border for proper scaling
            scrollBg.sprite = Sprite.Create(
                roundedBgSprite.texture,
                new Rect(0, 0, 256, 256),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(35, 35, 35, 35) // Border for 9-slice (L, B, R, T)
            );
            scrollBg.type = Image.Type.Sliced;

            // Use RectMask2D for content clipping
            var rectMask = scrollArea.AddComponent<RectMask2D>();
            rectMask.softness = new Vector2Int(25, 25); // Slight softness for edge blending

            // Content container - extra padding to prevent portal glow clipping
            var content = new GameObject("Content");
            content.transform.SetParent(scrollArea.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(-40, 0); // Room for scrollbar

            // Grid layout - 3 columns, cells sized to fit with glow padding
            var gridLayout = content.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(300, 300); // Larger cells for portal + glow
            gridLayout.spacing = new Vector2(8, 8); // Minimal spacing since glow provides visual separation
            gridLayout.padding = new RectOffset(25, 25, 25, 25); // Padding to prevent edge clipping
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect
            var scrollView = scrollArea.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Elastic;
            scrollView.content = contentRect;
            scrollView.viewport = scrollAreaRect;
            scrollView.scrollSensitivity = 50f; // Increase scroll wheel sensitivity

            // Vertical scrollbar - LARGER for touch, with rounded appearance
            var scrollbarGO = new GameObject("Scrollbar");
            scrollbarGO.transform.SetParent(scrollArea.transform, false);
            var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.anchoredPosition = new Vector2(-8, 0);
            scrollbarRect.sizeDelta = new Vector2(30, -20); // Wider for touch

            // Rounded scrollbar track
            var scrollbarImg = scrollbarGO.AddComponent<Image>();
            var scrollbarTrackSprite = CreateRoundedRectSprite(64, 256, 15, new Color(0.25f, 0.27f, 0.23f, 0.8f));
            scrollbarImg.sprite = Sprite.Create(
                scrollbarTrackSprite.texture,
                new Rect(0, 0, 64, 256),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(18, 18, 18, 18)
            );
            scrollbarImg.type = Image.Type.Sliced;

            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Scrollbar handle - rounded
            var handleArea = new GameObject("Handle Area");
            handleArea.transform.SetParent(scrollbarGO.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(3, 5);
            handleAreaRect.offsetMax = new Vector2(-3, -5);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            // Rounded handle
            var handleImg = handle.AddComponent<Image>();
            var handleSprite = CreateRoundedRectSprite(48, 128, 12, new Color(0.85f, 0.85f, 0.85f, 1f));
            handleImg.sprite = Sprite.Create(
                handleSprite.texture,
                new Rect(0, 0, 48, 128),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(15, 15, 15, 15)
            );
            handleImg.type = Image.Type.Sliced;

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImg;
            scrollView.verticalScrollbar = scrollbar;
            scrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent; // Always visible

            // Add LevelSelectScreen component
            levelSelectScreen = levelSelectPanel.AddComponent<LevelSelectScreen>();

            // Wire up references using reflection
            var screenType = typeof(LevelSelectScreen);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            screenType.GetField("prevWorldButton", flags)?.SetValue(levelSelectScreen, prevBtn);
            screenType.GetField("nextWorldButton", flags)?.SetValue(levelSelectScreen, nextBtn);
            screenType.GetField("worldImage", flags)?.SetValue(levelSelectScreen, worldSpriteImg);
            screenType.GetField("levelGridParent", flags)?.SetValue(levelSelectScreen, content.transform);
            screenType.GetField("levelScrollRect", flags)?.SetValue(levelSelectScreen, scrollView);

            // Wire mode tab container and topBar reference
            levelSelectScreen.SetModeTabContainer(modeTabContainerGO.transform);
            levelSelectScreen.SetTopBar(topBar.transform);

            // Initialize level select screen (creates grid, loads portals, etc.)
            levelSelectScreen.Initialize();

            // Connect level selected callback
            levelSelectScreen.OnLevelSelected += OnLevelSelectedFromMenu;

            Debug.Log("[UIManager] Level select panel created (Godot style)");
        }

        private void OnCloseFromLevelSelect()
        {
            Debug.Log("[UIManager] Close button clicked from level select");
            // Return to splash screen
            ShowSplash();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[UIManager] Settings button clicked");
            AudioManager.Instance?.PlayButtonClick();
            ShowSettings();
        }

        private void OnResetAchievementsClicked()
        {
            Debug.Log("[UIManager] Reset Achievements button clicked");
            AudioManager.Instance?.PlayButtonClick();

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.ResetAllAchievements();
                Debug.Log("[UIManager] All achievements have been reset!");
                // Refresh achievements panel if open
                RefreshAchievementsPanel();
            }
        }

        private void OnAchievementsClicked()
        {
            Debug.Log("[UIManager] Achievements button clicked");
            AudioManager.Instance?.PlayButtonClick();
            ShowAchievements();
        }

        private void OnLevelSelectedFromMenu(string worldId, int levelNumber)
        {
            Debug.Log($"[UIManager] Level selected: {worldId} #{levelNumber}");

            TransitionManager.Instance?.FadeOut(() =>
            {
                // Update GameManager state
                if (GameManager.Instance != null)
                {
                    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    typeof(GameManager).GetField("currentWorldId", flags)?.SetValue(GameManager.Instance, worldId);
                    typeof(GameManager).GetField("currentLevelNumber", flags)?.SetValue(GameManager.Instance, levelNumber);

                    // Set game state to Playing so DragDropManager works
                    GameManager.Instance.SetState(GameState.Playing);
                }

                // Show gameplay UI
                ShowGameplay();

                // Load the level
                LevelManager.Instance?.LoadLevel(worldId, levelNumber);

                TransitionManager.Instance?.FadeIn();
            });
        }

        private void CreateHUDPanel()
        {
            hudPanel = new GameObject("HUD Panel");
            hudPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = hudPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(0, 200);

            // Background
            var bg = hudPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            hudPanelBgImage = bg;

            // Add CanvasGroup for fading
            hudPanel.AddComponent<CanvasGroup>();

            // Create HUD content
            CreateHUDContent();
        }

        private void CreateHUDContent()
        {
            // Title/Level name
            var titleGO = CreateTextElement(hudPanel.transform, "Level Title", "SORT RESORT", 36,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20));
            levelTitleText = titleGO.GetComponent<TextMeshProUGUI>();

            // Stats container
            var statsGO = new GameObject("Stats");
            statsGO.transform.SetParent(hudPanel.transform, false);
            statsContainerGO = statsGO;
            var statsRect = statsGO.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0.5f);
            statsRect.anchorMax = new Vector2(1, 0.5f);
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta = new Vector2(-40, 100);

            var layout = statsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 50;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Moves
            moveCountText = CreateStatElement(statsGO.transform, "Moves", "0");

            // Matches
            matchCountText = CreateStatElement(statsGO.transform, "Matches", "0");

            // Items Remaining
            itemsRemainingText = CreateStatElement(statsGO.transform, "Items", "0");

            // Timer (hidden by default, shown when level has time limit)
            timerContainer = CreateTimerElement(statsGO.transform);

            // Star display
            CreateStarDisplay();

            // Buttons row
            CreateHUDButtons();
        }

        private TextMeshProUGUI CreateStatElement(Transform parent, string label, string value)
        {
            var container = new GameObject(label + " Stat");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 80);

            var vlayout = container.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(container.transform, false);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.gray;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 25);

            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(container.transform, false);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 32;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = Color.white;
            var valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(150, 40);

            return valueText;
        }

        private GameObject CreateTimerElement(Transform parent)
        {
            var container = new GameObject("Timer Stat");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 80);

            var vlayout = container.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(container.transform, false);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = "Time";
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.gray;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 25);

            // Value (timer display)
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(container.transform, false);
            timerText = valueGO.AddComponent<TextMeshProUGUI>();
            timerText.text = "0:00.00";
            timerText.fontSize = 32;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = Color.white;
            var valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(150, 40);

            // Start hidden
            container.SetActive(false);

            return container;
        }

        private void CreateStarDisplay()
        {
            var starsGO = new GameObject("Stars");
            starsGO.transform.SetParent(hudPanel.transform, false);
            starDisplayGO = starsGO;

            var rect = starsGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 10);
            rect.sizeDelta = new Vector2(200, 50);

            var layout = starsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;

            starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var starGO = new GameObject($"Star_{i}");
                starGO.transform.SetParent(starsGO.transform, false);

                var starRect = starGO.AddComponent<RectTransform>();
                starRect.sizeDelta = new Vector2(40, 40);

                var starImg = starGO.AddComponent<Image>();
                starImg.color = Color.yellow;
                starImages[i] = starImg;
            }
        }

        private void CreateHUDButtons()
        {
            var buttonsGO = new GameObject("Buttons");
            buttonsGO.transform.SetParent(hudPanel.transform, false);

            var rect = buttonsGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-120, -20);
            rect.sizeDelta = new Vector2(200, 50);

            var layout = buttonsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childForceExpandWidth = false;

#if UNITY_EDITOR
            // Record button (debug feature) - records moves for comparison with solver
            recordButton = CreateButton(buttonsGO.transform, "Record", "Rec", OnRecordClicked, 50, 50);
            recordButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.5f, 0.5f); // Red tint

            // Auto-Solve button (debug feature) - solves level automatically
            autoSolveButton = CreateButton(buttonsGO.transform, "AutoSolve", "Solve", OnAutoSolveClicked, 60, 50);
            autoSolveButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5f, 1f, 0.5f); // Green tint

            // Dev level navigation buttons - bottom of screen
            CreateDevNavButtons();
#endif

            // Settings/Undo overlay - sibling of hudPanel, renders on top
            hudSettingsOverlay = new GameObject("HUD Settings Overlay");
            hudSettingsOverlay.transform.SetParent(mainCanvas.transform, false);
            var settingsOvRect = hudSettingsOverlay.AddComponent<RectTransform>();
            settingsOvRect.anchorMin = Vector2.zero;
            settingsOvRect.anchorMax = Vector2.one;
            settingsOvRect.offsetMin = Vector2.zero;
            settingsOvRect.offsetMax = Vector2.zero;

            // Place just above hudPanel so buttons render on top
            if (hudPanel != null)
                hudSettingsOverlay.transform.SetSiblingIndex(hudPanel.transform.GetSiblingIndex() + 1);

            // Settings gear - 116x116 sprite button at anchor (0.919, 0.957)
            var gearBtnGO = new GameObject("Settings Gear Button");
            gearBtnGO.transform.SetParent(hudSettingsOverlay.transform, false);
            var gearBtnRect = gearBtnGO.AddComponent<RectTransform>();
            gearBtnRect.anchorMin = new Vector2(0.919f, 0.957f);
            gearBtnRect.anchorMax = new Vector2(0.919f, 0.957f);
            gearBtnRect.pivot = new Vector2(0.5f, 0.5f);
            gearBtnRect.anchoredPosition = Vector2.zero;
            gearBtnRect.sizeDelta = new Vector2(116, 116);
            var gearBtnImg = gearBtnGO.AddComponent<Image>();
            gearBtnImg.preserveAspect = true;
            var gearTex = Resources.Load<Texture2D>("Sprites/UI/HUD/settings_button");
            if (gearTex != null)
            {
                gearBtnImg.sprite = Sprite.Create(gearTex,
                    new Rect(0, 0, gearTex.width, gearTex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            var gearBtn = gearBtnGO.AddComponent<Button>();
            gearBtn.targetGraphic = gearBtnImg;
            // Pressed state sprite
            var gearPressedTex = Resources.Load<Texture2D>("Sprites/UI/HUD/settings_button_pressed");
            if (gearPressedTex != null)
            {
                var spriteState = new SpriteState();
                spriteState.pressedSprite = Sprite.Create(gearPressedTex,
                    new Rect(0, 0, gearPressedTex.width, gearPressedTex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                gearBtn.spriteState = spriteState;
                gearBtn.transition = Selectable.Transition.SpriteSwap;
            }
            gearBtn.onClick.AddListener(OnPauseClicked);

            // Undo button - 142x62 sprite button at anchor (0.919, 0.906), below settings gear
            var undoBtnGO = new GameObject("Undo Sprite Button");
            undoBtnGO.transform.SetParent(hudSettingsOverlay.transform, false);
            var undoBtnRect = undoBtnGO.AddComponent<RectTransform>();
            undoBtnRect.anchorMin = new Vector2(0.919f, 0.906f);
            undoBtnRect.anchorMax = new Vector2(0.919f, 0.906f);
            undoBtnRect.pivot = new Vector2(0.5f, 0.5f);
            undoBtnRect.anchoredPosition = Vector2.zero;
            undoBtnRect.sizeDelta = new Vector2(142, 62);
            var undoBtnImg = undoBtnGO.AddComponent<Image>();
            undoBtnImg.preserveAspect = true;
            var undoTex = Resources.Load<Texture2D>("Sprites/UI/HUD/undo_button");
            if (undoTex != null)
            {
                undoBtnImg.sprite = Sprite.Create(undoTex,
                    new Rect(0, 0, undoTex.width, undoTex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            undoSpriteButton = undoBtnGO.AddComponent<Button>();
            undoSpriteButton.targetGraphic = undoBtnImg;
            // Pressed state sprite
            var undoPressedTex = Resources.Load<Texture2D>("Sprites/UI/HUD/undo_button_pressed");
            if (undoPressedTex != null)
            {
                var spriteState2 = new SpriteState();
                spriteState2.pressedSprite = Sprite.Create(undoPressedTex,
                    new Rect(0, 0, undoPressedTex.width, undoPressedTex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                undoSpriteButton.spriteState = spriteState2;
                undoSpriteButton.transition = Selectable.Transition.SpriteSwap;
            }
            undoSpriteButton.onClick.AddListener(OnUndoClicked);
            undoSpriteCanvasGroup = undoBtnGO.AddComponent<CanvasGroup>();
            // Start disabled (no moves to undo)
            SetUndoButtonEnabled(false);

            hudSettingsOverlay.SetActive(false);
        }

        private void CreateHUDModeOverlay()
        {
            // Fullscreen overlay panel - sibling of hudPanel, renders behind it
            hudModeOverlay = new GameObject("HUD Mode Overlay");
            hudModeOverlay.transform.SetParent(mainCanvas.transform, false);

            var rect = hudModeOverlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Place behind hudPanel in sibling order so buttons on hudPanel remain clickable
            if (hudPanel != null)
                hudModeOverlay.transform.SetSiblingIndex(hudPanel.transform.GetSiblingIndex());

            // Background image (mode-specific bar)
            var bgGO = new GameObject("Overlay Background");
            bgGO.transform.SetParent(hudModeOverlay.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            hudOverlayBgImage = bgGO.AddComponent<Image>();
            hudOverlayBgImage.preserveAspect = true;
            hudOverlayBgImage.raycastTarget = false;

            // World icon image (overlaid on the bar)
            var iconGO = new GameObject("World Icon");
            iconGO.transform.SetParent(hudModeOverlay.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            hudWorldIconImage = iconGO.AddComponent<Image>();
            hudWorldIconImage.preserveAspect = true;
            hudWorldIconImage.raycastTarget = false;

            // Counter text elements - positions will be set per-mode at level start
            // Bubble diameter: ~120px (small) / ~174px (timer), centered at anchor Y=0.9188
            overlayLevelText = CreateOverlayCounterText(hudModeOverlay.transform, "Overlay Level",
                new Vector2(0.5f, 0.9188f), 120f, 120f, 72);

            overlayMovesText = CreateOverlayCounterText(hudModeOverlay.transform, "Overlay Moves",
                new Vector2(0.5f, 0.9188f), 120f, 120f, 72);

            overlayTimerText = CreateOverlayCounterText(hudModeOverlay.transform, "Overlay Timer",
                new Vector2(0.5f, 0.9188f), 174f, 120f, 45);

            hudModeOverlay.SetActive(false);
        }

        /// <summary>
        /// Positions the overlay counter texts based on which counters the current mode uses.
        /// </summary>
        private void PositionOverlayCounters(GameMode mode)
        {
            // Hide all first
            if (overlayLevelText != null) overlayLevelText.gameObject.SetActive(false);
            if (overlayMovesText != null) overlayMovesText.gameObject.SetActive(false);
            if (overlayTimerText != null) overlayTimerText.gameObject.SetActive(false);

            switch (mode)
            {
                case GameMode.FreePlay:
                    // Level only, centered
                    SetOverlayTextAnchor(overlayLevelText, new Vector2(0.500f, 0.9188f));
                    if (overlayLevelText != null) overlayLevelText.gameObject.SetActive(true);
                    break;

                case GameMode.StarMode:
                    // Level + Moves
                    SetOverlayTextAnchor(overlayLevelText, new Vector2(0.389f, 0.9188f));
                    SetOverlayTextAnchor(overlayMovesText, new Vector2(0.614f, 0.9188f));
                    if (overlayLevelText != null) overlayLevelText.gameObject.SetActive(true);
                    if (overlayMovesText != null) overlayMovesText.gameObject.SetActive(true);
                    break;

                case GameMode.TimerMode:
                    // Level + Timer
                    SetOverlayTextAnchor(overlayLevelText, new Vector2(0.381f, 0.9188f));
                    SetOverlayTextAnchor(overlayTimerText, new Vector2(0.621f, 0.9188f));
                    if (overlayLevelText != null) overlayLevelText.gameObject.SetActive(true);
                    if (overlayTimerText != null) overlayTimerText.gameObject.SetActive(true);
                    break;

                case GameMode.HardMode:
                    // Level + Moves + Timer
                    SetOverlayTextAnchor(overlayLevelText, new Vector2(0.276f, 0.9188f));
                    SetOverlayTextAnchor(overlayMovesText, new Vector2(0.500f, 0.9188f));
                    SetOverlayTextAnchor(overlayTimerText, new Vector2(0.732f, 0.9188f));
                    if (overlayLevelText != null) overlayLevelText.gameObject.SetActive(true);
                    if (overlayMovesText != null) overlayMovesText.gameObject.SetActive(true);
                    if (overlayTimerText != null) overlayTimerText.gameObject.SetActive(true);
                    break;
            }
        }

        private void SetOverlayTextAnchor(TextMeshProUGUI text, Vector2 anchor)
        {
            if (text == null) return;
            var rt = text.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
        }

        private string GetOverlayBarPath(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.FreePlay:  return "Sprites/UI/HUD/free_ui_top";
                case GameMode.StarMode:  return "Sprites/UI/HUD/stars_ui_top";
                case GameMode.TimerMode: return "Sprites/UI/HUD/timer_ui_top";
                case GameMode.HardMode:  return "Sprites/UI/HUD/hard_mode_UI_top";
                default:                 return "Sprites/UI/HUD/free_ui_top";
            }
        }

        private TextMeshProUGUI CreateOverlayCounterText(Transform parent, string name,
            Vector2 anchorPos, float width, float height, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "0";
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;

            return text;
        }

        private void SetUndoButtonEnabled(bool enabled)
        {
            if (undoButton != null)
            {
                undoButton.interactable = enabled;
            }
            if (undoButtonCanvasGroup != null)
            {
                undoButtonCanvasGroup.alpha = enabled ? 1f : 0.4f;
            }
            // Sprite-based undo button
            if (undoSpriteButton != null)
            {
                undoSpriteButton.interactable = enabled;
            }
            if (undoSpriteCanvasGroup != null)
            {
                undoSpriteCanvasGroup.alpha = enabled ? 1f : 0.4f;
            }
        }

        private void OnUndoClicked()
        {
            Debug.Log("[UIManager] Undo button clicked");
            AudioManager.Instance?.PlayButtonClick();
            LevelManager.Instance?.UndoLastMove();
        }

        private void OnUndoAvailableChanged(bool available)
        {
            SetUndoButtonEnabled(available);
        }

        private void OnPauseClicked()
        {
            Debug.Log("[UIManager] Pause button clicked");
            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.PauseGame();
        }

#if UNITY_EDITOR
        private void OnRecordClicked()
        {
            Debug.Log("[UIManager] Record button clicked");
            AudioManager.Instance?.PlayButtonClick();

            isRecordingMoves = !isRecordingMoves;

            if (isRecordingMoves)
            {
                // Start recording
                LevelManager.Instance?.StartRecording();
                if (recordButton != null)
                {
                    recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop";
                    recordButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.3f, 0.3f); // Brighter red
                }
            }
            else
            {
                // Stop recording and print moves
                LevelManager.Instance?.StopRecording();
                if (recordButton != null)
                {
                    recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rec";
                    recordButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.5f, 0.5f); // Normal red
                }
            }
        }
#endif

#if UNITY_EDITOR
        private void OnAutoSolveClicked()
        {
            Debug.Log("[UIManager] Auto-Solve button clicked");
            AudioManager.Instance?.PlayButtonClick();

            // Disable button while solving
            if (autoSolveButton != null)
            {
                autoSolveButton.interactable = false;
            }

            // Start the solver
            if (LevelSolverRunner.Instance != null)
            {
                LevelSolverRunner.Instance.OnSolveCompleted += OnAutoSolveCompleted;
                LevelSolverRunner.Instance.StartAutoSolve();
            }
            else
            {
                Debug.LogError("[UIManager] LevelSolverRunner not found!");
                if (autoSolveButton != null)
                {
                    autoSolveButton.interactable = true;
                }
            }
        }

        private void OnAutoSolveCompleted(bool success, int moves, int matches, float timeMs)
        {
            Debug.Log($"[UIManager] Auto-Solve completed: success={success}, moves={moves}, matches={matches}, time={timeMs:F1}ms");

            // Re-enable button
            if (autoSolveButton != null)
            {
                autoSolveButton.interactable = true;
            }

            // Unsubscribe
            if (LevelSolverRunner.Instance != null)
            {
                LevelSolverRunner.Instance.OnSolveCompleted -= OnAutoSolveCompleted;
            }

            if (!success)
            {
                Debug.LogWarning("[UIManager] Auto-Solve failed to complete the level!");
            }
        }

        private void CreateDevNavButtons()
        {
            // Container anchored at bottom-center of screen
            var navGO = new GameObject("Dev Nav Buttons");
            navGO.transform.SetParent(hudPanel.transform, false);

            var navRect = navGO.AddComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0.5f, 0f);
            navRect.anchorMax = new Vector2(0.5f, 0f);
            navRect.pivot = new Vector2(0.5f, 0f);
            navRect.anchoredPosition = new Vector2(0, 20);
            navRect.sizeDelta = new Vector2(300, 50);

            var layout = navGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 160;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;

            // Previous level button (left side)
            devPrevLevelButton = CreateButton(navGO.transform, "DevPrev", "< Prev", OnDevPrevLevelClicked, 70, 44);
            devPrevLevelButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.85f, 0.5f);
            devPrevLevelButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;

            // Next level button (right side)
            devNextLevelButton = CreateButton(navGO.transform, "DevNext", "Next >", OnDevNextLevelClicked, 70, 44);
            devNextLevelButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.85f, 0.5f);
            devNextLevelButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;

            // Disable prev if on level 1
            int currentLevel = GameManager.Instance?.CurrentLevelNumber ?? 1;
            if (currentLevel <= 1 && devPrevLevelButton != null)
                devPrevLevelButton.interactable = false;

            // Disable next if no next level exists
            string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
            if (LevelDataLoader.LoadLevel(worldId, currentLevel + 1) == null && devNextLevelButton != null)
                devNextLevelButton.interactable = false;
        }

        private void DevLoadLevel(int levelNumber)
        {
            string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
            Debug.Log($"[UIManager] Dev nav: loading {worldId} level {levelNumber}");

            TransitionManager.Instance?.FadeOut(() =>
            {
                if (GameManager.Instance != null)
                {
                    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    typeof(GameManager).GetField("currentLevelNumber", flags)?.SetValue(GameManager.Instance, levelNumber);
                    GameManager.Instance.SetState(GameState.Playing);
                }

                ShowGameplay();
                LevelManager.Instance?.LoadLevel(worldId, levelNumber);

                // Update dev nav button states for new level
                if (devPrevLevelButton != null)
                    devPrevLevelButton.interactable = levelNumber > 1;
                if (devNextLevelButton != null)
                    devNextLevelButton.interactable = LevelDataLoader.LoadLevel(worldId, levelNumber + 1) != null;

                TransitionManager.Instance?.FadeIn();
            });
        }

        private void OnDevPrevLevelClicked()
        {
            int currentLevel = GameManager.Instance?.CurrentLevelNumber ?? 1;
            if (currentLevel <= 1) return;
            DevLoadLevel(currentLevel - 1);
        }

        private void OnDevNextLevelClicked()
        {
            int currentLevel = GameManager.Instance?.CurrentLevelNumber ?? 1;
            string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
            int nextLevel = currentLevel + 1;
            if (LevelDataLoader.LoadLevel(worldId, nextLevel) == null)
            {
                Debug.LogWarning($"[UIManager] Dev nav: no level {nextLevel} in {worldId}");
                return;
            }
            DevLoadLevel(nextLevel);
        }
#endif

        private void CreateLevelCompletePanel()
        {
            levelCompletePanel = new GameObject("Level Complete Panel");
            levelCompletePanel.transform.SetParent(mainCanvas.transform, false);

            var rect = levelCompletePanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // === PHASE A ELEMENTS ===

            // 1. Dim Overlay - semi-transparent black for mascot phase
            var dimGO = new GameObject("Dim Overlay");
            dimGO.transform.SetParent(levelCompletePanel.transform, false);
            var dimRect = dimGO.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimOverlayImage = dimGO.AddComponent<Image>();
            dimOverlayImage.color = Color.black;
            dimOverlayImage.raycastTarget = false;
            dimOverlayCanvasGroup = dimGO.AddComponent<CanvasGroup>();
            dimOverlayCanvasGroup.alpha = 0f;

            // 2. Mascot image - fullscreen with preserveAspect (Phase A)
            var mascotGO = new GameObject("Mascot");
            mascotGO.transform.SetParent(levelCompletePanel.transform, false);
            var mascotRect = mascotGO.AddComponent<RectTransform>();
            mascotRect.anchorMin = Vector2.zero;
            mascotRect.anchorMax = Vector2.one;
            mascotRect.offsetMin = Vector2.zero;
            mascotRect.offsetMax = Vector2.zero;
            levelCompleteMascotImage = mascotGO.AddComponent<Image>();
            levelCompleteMascotImage.preserveAspect = true;
            levelCompleteMascotImage.raycastTarget = false;
            levelCompleteMascotImage.enabled = false;

            // 2b. "Level Complete" animated text - fullscreen overlay on top of mascot (Phase A)
            var lcTextGO = new GameObject("Level Complete Text");
            lcTextGO.transform.SetParent(levelCompletePanel.transform, false);
            var lcTextRect = lcTextGO.AddComponent<RectTransform>();
            lcTextRect.anchorMin = Vector2.zero;
            lcTextRect.anchorMax = Vector2.one;
            lcTextRect.offsetMin = Vector2.zero;
            lcTextRect.offsetMax = Vector2.zero;
            levelCompleteTextImage = lcTextGO.AddComponent<Image>();
            levelCompleteTextImage.preserveAspect = true;
            levelCompleteTextImage.raycastTarget = false;
            levelCompleteTextImage.enabled = false;
            LoadLevelCompleteTextFrames();

            // === PHASE B ELEMENTS ===

            // 3. Animated background - Rays (behind everything in Phase B, loops)
            var raysGO = new GameObject("Rays Background");
            raysGO.transform.SetParent(levelCompletePanel.transform, false);
            var raysRect = raysGO.AddComponent<RectTransform>();
            raysRect.anchorMin = Vector2.zero;
            raysRect.anchorMax = Vector2.one;
            raysRect.offsetMin = Vector2.zero;
            raysRect.offsetMax = Vector2.zero;
            var raysImage = raysGO.AddComponent<Image>();
            raysImage.preserveAspect = false;

            // 4. Animated background - Curtains (on top of rays, plays once)
            var curtainsGO = new GameObject("Curtains");
            curtainsGO.transform.SetParent(levelCompletePanel.transform, false);
            var curtainsRect = curtainsGO.AddComponent<RectTransform>();
            curtainsRect.anchorMin = Vector2.zero;
            curtainsRect.anchorMax = Vector2.one;
            curtainsRect.offsetMin = Vector2.zero;
            curtainsRect.offsetMax = Vector2.zero;
            var curtainsImage = curtainsGO.AddComponent<Image>();
            curtainsImage.preserveAspect = false;

            // 4b. Timer Icon - fullscreen overlay with stopwatch for Timer/Hard mode
            var timerIconGO = new GameObject("Timer Icon");
            timerIconGO.transform.SetParent(levelCompletePanel.transform, false);
            var timerIconRect = timerIconGO.AddComponent<RectTransform>();
            timerIconRect.anchorMin = Vector2.zero;
            timerIconRect.anchorMax = Vector2.one;
            timerIconRect.offsetMin = Vector2.zero;
            timerIconRect.offsetMax = Vector2.zero;
            timerIconImage = timerIconGO.AddComponent<Image>();
            timerIconImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/timer_icon");
            timerIconImage.preserveAspect = true;
            timerIconImage.raycastTarget = false;
            timerIconCanvasGroup = timerIconGO.AddComponent<CanvasGroup>();
            timerIconCanvasGroup.alpha = 0f;

            // 4c. Free Mode Overlay - medal overlay for FreePlay mode
            var freeOverlayGO = new GameObject("Free Overlay");
            freeOverlayGO.transform.SetParent(levelCompletePanel.transform, false);
            var freeOverlayRect = freeOverlayGO.AddComponent<RectTransform>();
            freeOverlayRect.anchorMin = Vector2.zero;
            freeOverlayRect.anchorMax = Vector2.one;
            freeOverlayRect.offsetMin = Vector2.zero;
            freeOverlayRect.offsetMax = Vector2.zero;
            freeOverlayImage = freeOverlayGO.AddComponent<Image>();
            freeOverlayImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/free_levelcomplete_overlay");
            freeOverlayImage.preserveAspect = true;
            freeOverlayImage.raycastTarget = false;
            freeOverlayCanvasGroup = freeOverlayGO.AddComponent<CanvasGroup>();
            freeOverlayCanvasGroup.alpha = 0f;

            // 4d. Hard Mode Overlay - medal+stopwatch overlay for HardMode
            var hardOverlayGO = new GameObject("Hard Overlay");
            hardOverlayGO.transform.SetParent(levelCompletePanel.transform, false);
            var hardOverlayRect = hardOverlayGO.AddComponent<RectTransform>();
            hardOverlayRect.anchorMin = Vector2.zero;
            hardOverlayRect.anchorMax = Vector2.one;
            hardOverlayRect.offsetMin = Vector2.zero;
            hardOverlayRect.offsetMax = Vector2.zero;
            hardOverlayImage = hardOverlayGO.AddComponent<Image>();
            hardOverlayImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/hard_levelcomplete_overlay");
            hardOverlayImage.preserveAspect = true;
            hardOverlayImage.raycastTarget = false;
            hardOverlayCanvasGroup = hardOverlayGO.AddComponent<CanvasGroup>();
            hardOverlayCanvasGroup.alpha = 0f;

            // 5. Victory Board - per-world fullscreen overlay
            var victoryBoardGO = new GameObject("Victory Board");
            victoryBoardGO.transform.SetParent(levelCompletePanel.transform, false);
            var victoryBoardRect = victoryBoardGO.AddComponent<RectTransform>();
            victoryBoardRect.anchorMin = Vector2.zero;
            victoryBoardRect.anchorMax = Vector2.one;
            victoryBoardRect.offsetMin = Vector2.zero;
            victoryBoardRect.offsetMax = Vector2.zero;
            victoryBoardImage = victoryBoardGO.AddComponent<Image>();
            victoryBoardImage.preserveAspect = true;
            victoryBoardImage.raycastTarget = false;
            victoryBoardCanvasGroup = victoryBoardGO.AddComponent<CanvasGroup>();
            victoryBoardCanvasGroup.alpha = 0f;

            // 6. level_UI - fullscreen overlay with brown circle for level number
            var levelUIGO = new GameObject("Level UI");
            levelUIGO.transform.SetParent(levelCompletePanel.transform, false);
            var levelUIRect = levelUIGO.AddComponent<RectTransform>();
            levelUIRect.anchorMin = Vector2.zero;
            levelUIRect.anchorMax = Vector2.one;
            levelUIRect.offsetMin = Vector2.zero;
            levelUIRect.offsetMax = Vector2.zero;
            levelUIImage = levelUIGO.AddComponent<Image>();
            levelUIImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/level_UI");
            levelUIImage.preserveAspect = true;
            levelUIImage.raycastTarget = false;
            levelUICanvasGroup = levelUIGO.AddComponent<CanvasGroup>();
            levelUICanvasGroup.alpha = 0f;

            // Level Number text - child of level_UI, positioned on its brown circle
            var levelNumGO = new GameObject("Level Number Text");
            levelNumGO.transform.SetParent(levelUIGO.transform, false);
            var levelNumRect = levelNumGO.AddComponent<RectTransform>();
            levelNumRect.anchorMin = new Vector2(0.4176f, 0.4510f);
            levelNumRect.anchorMax = new Vector2(0.4176f, 0.4510f);
            levelNumRect.pivot = new Vector2(0.5f, 0.5f);
            levelNumRect.anchoredPosition = Vector2.zero;
            levelNumRect.sizeDelta = new Vector2(120, 80);
            levelCompleteLevelNumberText = levelNumGO.AddComponent<TextMeshProUGUI>();
            levelCompleteLevelNumberText.text = "";
            levelCompleteLevelNumberText.fontSize = 52;
            levelCompleteLevelNumberText.fontStyle = FontStyles.Bold;
            levelCompleteLevelNumberText.alignment = TextAlignmentOptions.Center;
            levelCompleteLevelNumberText.color = Color.black;
            levelCompleteLevelNumberText.enableWordWrapping = false;
            levelCompleteLevelNumberText.overflowMode = TextOverflowModes.Overflow;

            // 7. moves_UI - fullscreen overlay with brown circle for move count
            var movesUIGO = new GameObject("Moves UI");
            movesUIGO.transform.SetParent(levelCompletePanel.transform, false);
            var movesUIRect = movesUIGO.AddComponent<RectTransform>();
            movesUIRect.anchorMin = Vector2.zero;
            movesUIRect.anchorMax = Vector2.one;
            movesUIRect.offsetMin = Vector2.zero;
            movesUIRect.offsetMax = Vector2.zero;
            movesUIImage = movesUIGO.AddComponent<Image>();
            movesUIImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/moves_UI");
            movesUIImage.preserveAspect = true;
            movesUIImage.raycastTarget = false;
            movesUICanvasGroup = movesUIGO.AddComponent<CanvasGroup>();
            movesUICanvasGroup.alpha = 0f;

            // Moves Count text - child of moves_UI, positioned on its brown circle
            var movesCountGO = new GameObject("Moves Count Text");
            movesCountGO.transform.SetParent(movesUIGO.transform, false);
            var movesCountRect = movesCountGO.AddComponent<RectTransform>();
            movesCountRect.anchorMin = new Vector2(0.4130f, 0.3453f);
            movesCountRect.anchorMax = new Vector2(0.4130f, 0.3453f);
            movesCountRect.pivot = new Vector2(0.5f, 0.5f);
            movesCountRect.anchoredPosition = Vector2.zero;
            movesCountRect.sizeDelta = new Vector2(120, 80);
            levelCompleteMoveCountText = movesCountGO.AddComponent<TextMeshProUGUI>();
            levelCompleteMoveCountText.text = "";
            levelCompleteMoveCountText.fontSize = 52;
            levelCompleteMoveCountText.fontStyle = FontStyles.Bold;
            levelCompleteMoveCountText.alignment = TextAlignmentOptions.Center;
            levelCompleteMoveCountText.color = Color.black;
            levelCompleteMoveCountText.enableWordWrapping = false;
            levelCompleteMoveCountText.overflowMode = TextOverflowModes.Overflow;

            // 8. Star Ribbon layer (full screen, on top of curtains)
            var starRibbonGO = new GameObject("Star Ribbon");
            starRibbonGO.transform.SetParent(levelCompletePanel.transform, false);
            var starRibbonRect = starRibbonGO.AddComponent<RectTransform>();
            starRibbonRect.anchorMin = Vector2.zero;
            starRibbonRect.anchorMax = Vector2.one;
            starRibbonRect.offsetMin = Vector2.zero;
            starRibbonRect.offsetMax = Vector2.zero;
            var starRibbonImage = starRibbonGO.AddComponent<Image>();
            starRibbonImage.preserveAspect = true;
            starRibbonImage.raycastTarget = false;

            // Add and initialize the animation controller
            animatedLevelComplete = levelCompletePanel.AddComponent<AnimatedLevelComplete>();
            animatedLevelComplete.Initialize(raysImage, curtainsImage);
            animatedLevelComplete.SetStarRibbonImage(starRibbonImage);

            // 9. Grey Stars - static fullscreen, always visible as baseline
            var greyStarsGO = new GameObject("Grey Stars");
            greyStarsGO.transform.SetParent(levelCompletePanel.transform, false);
            var greyStarsRect = greyStarsGO.AddComponent<RectTransform>();
            greyStarsRect.anchorMin = Vector2.zero;
            greyStarsRect.anchorMax = Vector2.one;
            greyStarsRect.offsetMin = Vector2.zero;
            greyStarsRect.offsetMax = Vector2.zero;
            levelCompleteGreyStarsImage = greyStarsGO.AddComponent<Image>();
            var greyStarsSprite = Resources.Load<Sprite>("Sprites/UI/LevelComplete/grey_stars");
            if (greyStarsSprite == null)
            {
                var greyStarsTex = Resources.Load<Texture2D>("Sprites/UI/LevelComplete/grey_stars");
                if (greyStarsTex != null)
                    greyStarsSprite = Sprite.Create(greyStarsTex, new Rect(0, 0, greyStarsTex.width, greyStarsTex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            levelCompleteGreyStarsImage.sprite = greyStarsSprite;
            levelCompleteGreyStarsImage.preserveAspect = true;
            levelCompleteGreyStarsImage.raycastTarget = false;
            greyStarsCanvasGroup = greyStarsGO.AddComponent<CanvasGroup>();
            greyStarsCanvasGroup.alpha = 0f;

            // Load star animation frames
            star1Frames = LoadStarFrames("Sprites/UI/LevelComplete/Star1", "star1");
            star2Frames = LoadStarFrames("Sprites/UI/LevelComplete/Star2", "star2");
            star3Frames = LoadStarFrames("Sprites/UI/LevelComplete/Star3", "star3");

            // 10. Star 1 Image (left star) - fullscreen, starts hidden
            var star1GO = new GameObject("Star 1");
            star1GO.transform.SetParent(levelCompletePanel.transform, false);
            var star1Rect = star1GO.AddComponent<RectTransform>();
            star1Rect.anchorMin = Vector2.zero;
            star1Rect.anchorMax = Vector2.one;
            star1Rect.offsetMin = Vector2.zero;
            star1Rect.offsetMax = Vector2.zero;
            levelCompleteStar1Image = star1GO.AddComponent<Image>();
            levelCompleteStar1Image.preserveAspect = true;
            levelCompleteStar1Image.raycastTarget = false;
            star1GO.SetActive(false);

            // Star 2 Image (right star) - fullscreen, starts hidden
            var star2GO = new GameObject("Star 2");
            star2GO.transform.SetParent(levelCompletePanel.transform, false);
            var star2Rect = star2GO.AddComponent<RectTransform>();
            star2Rect.anchorMin = Vector2.zero;
            star2Rect.anchorMax = Vector2.one;
            star2Rect.offsetMin = Vector2.zero;
            star2Rect.offsetMax = Vector2.zero;
            levelCompleteStar2Image = star2GO.AddComponent<Image>();
            levelCompleteStar2Image.preserveAspect = true;
            levelCompleteStar2Image.raycastTarget = false;
            star2GO.SetActive(false);

            // Star 3 Image (center/large star) - fullscreen, starts hidden
            var star3GO = new GameObject("Star 3");
            star3GO.transform.SetParent(levelCompletePanel.transform, false);
            var star3Rect = star3GO.AddComponent<RectTransform>();
            star3Rect.anchorMin = Vector2.zero;
            star3Rect.anchorMax = Vector2.one;
            star3Rect.offsetMin = Vector2.zero;
            star3Rect.offsetMax = Vector2.zero;
            levelCompleteStar3Image = star3GO.AddComponent<Image>();
            levelCompleteStar3Image.preserveAspect = true;
            levelCompleteStar3Image.raycastTarget = false;
            star3GO.SetActive(false);

            // 11. Dancing Stars - fullscreen, loops when 3 stars earned, starts hidden
            var dancingStarsGO = new GameObject("Dancing Stars");
            dancingStarsGO.transform.SetParent(levelCompletePanel.transform, false);
            var dancingStarsRect = dancingStarsGO.AddComponent<RectTransform>();
            dancingStarsRect.anchorMin = Vector2.zero;
            dancingStarsRect.anchorMax = Vector2.one;
            dancingStarsRect.offsetMin = Vector2.zero;
            dancingStarsRect.offsetMax = Vector2.zero;
            dancingStarsImage = dancingStarsGO.AddComponent<Image>();
            dancingStarsImage.preserveAspect = true;
            dancingStarsImage.raycastTarget = false;
            dancingStarsGO.SetActive(false);

            // Load dancing stars animation frames
            dancingStarsFrames = LoadStarFrames("Sprites/UI/LevelComplete/DancingStars", "dancing stars");

            // 12. timer_bar_UI - fullscreen overlay with teal bar for timer text
            var timerBarGO = new GameObject("Timer Bar UI");
            timerBarGO.transform.SetParent(levelCompletePanel.transform, false);
            var timerBarRect = timerBarGO.AddComponent<RectTransform>();
            timerBarRect.anchorMin = Vector2.zero;
            timerBarRect.anchorMax = Vector2.one;
            timerBarRect.offsetMin = Vector2.zero;
            timerBarRect.offsetMax = Vector2.zero;
            timerBarUIImage = timerBarGO.AddComponent<Image>();
            timerBarUIImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/timer_bar_UI");
            timerBarUIImage.preserveAspect = true;
            timerBarUIImage.raycastTarget = false;
            timerBarUICanvasGroup = timerBarGO.AddComponent<CanvasGroup>();
            timerBarUICanvasGroup.alpha = 0f;

            // Timer text - child of timer_bar_UI, positioned on the teal bar center
            var timerTextGO = new GameObject("Timer Bar Text");
            timerTextGO.transform.SetParent(timerBarGO.transform, false);
            var timerTextRect = timerTextGO.AddComponent<RectTransform>();
            timerTextRect.anchorMin = new Vector2(0.4991f, 0.5802f);
            timerTextRect.anchorMax = new Vector2(0.4991f, 0.5802f);
            timerTextRect.pivot = new Vector2(0.5f, 0.5f);
            timerTextRect.anchoredPosition = Vector2.zero;
            timerTextRect.sizeDelta = new Vector2(400, 80);
            timerBarText = timerTextGO.AddComponent<TextMeshProUGUI>();
            timerBarText.text = "0:00.00";
            timerBarText.fontSize = 56;
            timerBarText.fontStyle = FontStyles.Bold;
            timerBarText.alignment = TextAlignmentOptions.Center;
            timerBarText.color = Color.white;
            timerBarText.enableWordWrapping = false;
            timerBarText.overflowMode = TextOverflowModes.Overflow;

            // 13. new_record_UI - fullscreen overlay sprite for "New Record!" display
            var newRecordGO = new GameObject("New Record UI");
            newRecordGO.transform.SetParent(levelCompletePanel.transform, false);
            var newRecordRect = newRecordGO.AddComponent<RectTransform>();
            newRecordRect.anchorMin = Vector2.zero;
            newRecordRect.anchorMax = Vector2.one;
            newRecordRect.offsetMin = Vector2.zero;
            newRecordRect.offsetMax = Vector2.zero;
            newRecordUIImage = newRecordGO.AddComponent<Image>();
            newRecordUIImage.sprite = LoadFullRectSprite("Sprites/UI/LevelComplete/new_record_UI");
            newRecordUIImage.preserveAspect = true;
            newRecordUIImage.raycastTarget = false;
            newRecordUICanvasGroup = newRecordGO.AddComponent<CanvasGroup>();
            newRecordUICanvasGroup.alpha = 0f;

            // 14. Bottom Board - animated wooden board that slides up behind buttons
            var bottomBoardGO = new GameObject("Bottom Board");
            bottomBoardGO.transform.SetParent(levelCompletePanel.transform, false);
            var bottomBoardRect = bottomBoardGO.AddComponent<RectTransform>();
            bottomBoardRect.anchorMin = Vector2.zero;
            bottomBoardRect.anchorMax = Vector2.one;
            bottomBoardRect.offsetMin = Vector2.zero;
            bottomBoardRect.offsetMax = Vector2.zero;
            bottomBoardImage = bottomBoardGO.AddComponent<Image>();
            bottomBoardImage.preserveAspect = true;
            bottomBoardImage.raycastTarget = false;
            bottomBoardGO.SetActive(false);

            // Load bottom board animation frames
            bottomBoardFrames = LoadStarFrames("Sprites/UI/LevelComplete/BottomBoard", "bottom board");

            // 15. Buttons container - positioned at bottom of screen
            var buttonsContainer = new GameObject("Buttons");
            buttonsContainer.transform.SetParent(levelCompletePanel.transform, false);
            buttonsContainerGO = buttonsContainer;
            var buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.078f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.078f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.anchoredPosition = Vector2.zero;
            buttonsRect.sizeDelta = new Vector2(1200, 220);
            buttonsContainer.SetActive(false);
            var buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childControlHeight = false;

            // Create sprite-based buttons
            CreateSpriteButton(buttonsContainer.transform, "Return to Map",
                "Sprites/UI/LevelComplete/Buttons/return_to_map",
                "Sprites/UI/LevelComplete/Buttons/return_to_map_pressed",
                OnBackToLevelsClicked, 340, 190);

            CreateSpriteButton(buttonsContainer.transform, "Replay Level",
                "Sprites/UI/LevelComplete/Buttons/replay_level",
                "Sprites/UI/LevelComplete/Buttons/replay_level_pressed",
                OnReplayClicked, 340, 190);

            CreateSpriteButton(buttonsContainer.transform, "Next Level",
                "Sprites/UI/LevelComplete/Buttons/next_level",
                "Sprites/UI/LevelComplete/Buttons/next_level_pressed",
                OnNextLevelFromCompleteClicked, 340, 190);

            Debug.Log("[UIManager] Level complete panel created with two-phase layout");
        }

        private void CreateLevelFailedPanel()
        {
            levelFailedPanel = new GameObject("Level Failed Panel");
            levelFailedPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = levelFailedPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Fullscreen fail screen background image
            var bgImage = levelFailedPanel.AddComponent<Image>();
            var failSprite = LoadSpriteFromTexture("Sprites/UI/LevelFailed/fail_screen");
            bgImage.sprite = failSprite;
            bgImage.preserveAspect = true;
            bgImage.type = Image.Type.Simple;
            bgImage.color = Color.white;

            // Reason text overlay - positioned on the brown board area
            var reasonGO = new GameObject("Reason Text");
            reasonGO.transform.SetParent(levelFailedPanel.transform, false);
            var reasonRect = reasonGO.AddComponent<RectTransform>();
            reasonRect.anchorMin = new Vector2(0.5f, 0.67f);
            reasonRect.anchorMax = new Vector2(0.5f, 0.67f);
            reasonRect.pivot = new Vector2(0.5f, 0.5f);
            reasonRect.anchoredPosition = Vector2.zero;
            reasonRect.sizeDelta = new Vector2(600, 120);
            levelFailedReasonText = reasonGO.AddComponent<TextMeshProUGUI>();
            levelFailedReasonText.text = "Level Failed";
            levelFailedReasonText.fontSize = 52;
            levelFailedReasonText.fontStyle = FontStyles.Bold;
            levelFailedReasonText.color = new Color(1f, 0.85f, 0.85f, 1f);
            levelFailedReasonText.alignment = TextAlignmentOptions.Center;
            levelFailedReasonText.enableWordWrapping = true;

            // Bottom Board - reuse the same animated board from level complete
            var failBottomBoardGO = new GameObject("Bottom Board");
            failBottomBoardGO.transform.SetParent(levelFailedPanel.transform, false);
            var failBoardRect = failBottomBoardGO.AddComponent<RectTransform>();
            failBoardRect.anchorMin = Vector2.zero;
            failBoardRect.anchorMax = Vector2.one;
            failBoardRect.offsetMin = Vector2.zero;
            failBoardRect.offsetMax = Vector2.zero;
            failedBottomBoardImage = failBottomBoardGO.AddComponent<Image>();
            failedBottomBoardImage.preserveAspect = true;
            failedBottomBoardImage.raycastTarget = false;
            failBottomBoardGO.SetActive(false);

            // Load bottom board frames (reuse same frames as level complete)
            failedBottomBoardFrames = LoadStarFrames("Sprites/UI/LevelComplete/BottomBoard", "failed bottom board");

            // Buttons container - positioned at bottom, starts hidden
            var buttonsContainer = new GameObject("Buttons");
            buttonsContainer.transform.SetParent(levelFailedPanel.transform, false);
            failedButtonsContainerGO = buttonsContainer;
            var buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.078f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.078f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.anchoredPosition = Vector2.zero;
            buttonsRect.sizeDelta = new Vector2(900, 150);

            var buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 40;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            CreateSpriteButton(buttonsContainer.transform, "Retry",
                "Sprites/UI/LevelFailed/retry_button",
                "Sprites/UI/LevelFailed/retry_button_pressed",
                OnRetryFromFailedClicked, 366, 114);

            CreateSpriteButton(buttonsContainer.transform, "Level Select",
                "Sprites/UI/LevelFailed/levelselect",
                "Sprites/UI/LevelFailed/levelselect_pressed",
                OnBackToLevelsFromFailedClicked, 366, 114);

            failedButtonsContainerGO.SetActive(false);

            Debug.Log("[UIManager] Level failed panel created");
        }

        private void CreateSettingsPanel()
        {
            settingsPanel = new GameObject("Settings Panel");
            settingsPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = settingsPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Override sorting so settings renders above portal ResultOverlays (sortingOrder 5001)
            var settingsCanvas = settingsPanel.AddComponent<Canvas>();
            settingsCanvas.overrideSorting = true;
            settingsCanvas.sortingOrder = 5200;
            settingsPanel.AddComponent<GraphicRaycaster>();

            // Dark overlay/dim background
            var dimBg = settingsPanel.AddComponent<Image>();
            dimBg.color = new Color(0, 0, 0, 0.7f);

            // Add CanvasGroup for fading
            var canvasGroup = settingsPanel.AddComponent<CanvasGroup>();

            // Load sprites
            var boardSprite = Resources.Load<Sprite>("Sprites/UI/Settings/board");
            var headerSprite = Resources.Load<Sprite>("Sprites/UI/Settings/settings_header");
            var audioPanelSprite = Resources.Load<Sprite>("Sprites/UI/Settings/audio_panel");
            var backNormalSprite = Resources.Load<Sprite>("Sprites/UI/Settings/back_button");
            var backPressedSprite = Resources.Load<Sprite>("Sprites/UI/Settings/back_button_pressed");
            var checkboxSprite = Resources.Load<Sprite>("Sprites/UI/Settings/checkbox");

            // Main content container (centered)
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(settingsPanel.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(900, 1400);

            // Board background
            var boardBg = contentGO.AddComponent<Image>();
            if (boardSprite != null)
            {
                boardBg.sprite = boardSprite;
                boardBg.type = Image.Type.Sliced;
            }
            else
            {
                boardBg.color = new Color(0.45f, 0.30f, 0.15f, 1f); // Brown fallback
            }

            // ============================================
            // HEADER
            // ============================================
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(contentGO.transform, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, 20);
            headerRect.sizeDelta = new Vector2(0, 120);

            var headerImg = headerGO.AddComponent<Image>();
            if (headerSprite != null)
            {
                headerImg.sprite = headerSprite;
                headerImg.preserveAspect = true;
            }
            else
            {
                headerImg.color = new Color(0.55f, 0.35f, 0.2f, 1f);

                // Fallback header text
                var headerTextGO = new GameObject("HeaderText");
                headerTextGO.transform.SetParent(headerGO.transform, false);
                var headerTextRect = headerTextGO.AddComponent<RectTransform>();
                headerTextRect.anchorMin = Vector2.zero;
                headerTextRect.anchorMax = Vector2.one;
                headerTextRect.offsetMin = Vector2.zero;
                headerTextRect.offsetMax = Vector2.zero;
                var headerText = headerTextGO.AddComponent<TextMeshProUGUI>();
                headerText.text = "SETTINGS";
                headerText.fontSize = 48;
                headerText.fontStyle = FontStyles.Bold;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.color = Color.white;
            }

            // ============================================
            // AUDIO PANEL
            // ============================================
            var audioPanelGO = new GameObject("AudioPanel");
            audioPanelGO.transform.SetParent(contentGO.transform, false);
            var audioPanelRect = audioPanelGO.AddComponent<RectTransform>();
            audioPanelRect.anchorMin = new Vector2(0.5f, 1);
            audioPanelRect.anchorMax = new Vector2(0.5f, 1);
            audioPanelRect.pivot = new Vector2(0.5f, 1);
            audioPanelRect.anchoredPosition = new Vector2(0, -130);
            audioPanelRect.sizeDelta = new Vector2(800, 450);

            var audioPanelImg = audioPanelGO.AddComponent<Image>();
            if (audioPanelSprite != null)
            {
                audioPanelImg.sprite = audioPanelSprite;
                audioPanelImg.type = Image.Type.Sliced;
            }
            else
            {
                audioPanelImg.color = new Color(0.95f, 0.85f, 0.75f, 1f); // Beige fallback
            }

            // Audio sliders container
            var slidersContainer = new GameObject("SlidersContainer");
            slidersContainer.transform.SetParent(audioPanelGO.transform, false);
            var slidersRect = slidersContainer.AddComponent<RectTransform>();
            slidersRect.anchorMin = new Vector2(0, 0);
            slidersRect.anchorMax = new Vector2(1, 1);
            slidersRect.offsetMin = new Vector2(40, 40);
            slidersRect.offsetMax = new Vector2(-40, -60);

            var slidersLayout = slidersContainer.AddComponent<VerticalLayoutGroup>();
            slidersLayout.spacing = 25;
            slidersLayout.childAlignment = TextAnchor.MiddleCenter;
            slidersLayout.childForceExpandWidth = true;
            slidersLayout.childForceExpandHeight = false;
            slidersLayout.padding = new RectOffset(20, 20, 20, 20);

            // Create sliders with labels
            var (masterSlider, masterLabel) = CreateVolumeSliderRow(slidersContainer.transform, "Master Volume");
            var (musicSlider, musicLabel) = CreateVolumeSliderRow(slidersContainer.transform, "Music");
            var (sfxSlider, sfxLabel) = CreateVolumeSliderRow(slidersContainer.transform, "Sound Effects");

            // ============================================
            // HAPTICS TOGGLE
            // ============================================
            var hapticsRowGO = new GameObject("HapticsRow");
            hapticsRowGO.transform.SetParent(contentGO.transform, false);
            var hapticsRowRect = hapticsRowGO.AddComponent<RectTransform>();
            hapticsRowRect.anchorMin = new Vector2(0.5f, 1);
            hapticsRowRect.anchorMax = new Vector2(0.5f, 1);
            hapticsRowRect.pivot = new Vector2(0.5f, 1);
            hapticsRowRect.anchoredPosition = new Vector2(0, -610);
            hapticsRowRect.sizeDelta = new Vector2(700, 80);

            var hapticsLayout = hapticsRowGO.AddComponent<HorizontalLayoutGroup>();
            hapticsLayout.spacing = 20;
            hapticsLayout.childAlignment = TextAnchor.MiddleCenter;
            hapticsLayout.childForceExpandWidth = false;
            hapticsLayout.childForceExpandHeight = false;

            // Haptics label
            var hapticsLabelGO = new GameObject("HapticsLabel");
            hapticsLabelGO.transform.SetParent(hapticsRowGO.transform, false);
            var hapticsLabelText = hapticsLabelGO.AddComponent<TextMeshProUGUI>();
            hapticsLabelText.text = "Vibration";
            hapticsLabelText.fontSize = 36;
            hapticsLabelText.fontStyle = FontStyles.Bold;
            hapticsLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            hapticsLabelText.color = Color.white;
            var hapticsLabelLE = hapticsLabelGO.AddComponent<LayoutElement>();
            hapticsLabelLE.preferredWidth = 400;
            hapticsLabelLE.preferredHeight = 60;

            // Haptics toggle (Google-style switch)
            var (hapticsToggle, hapticsCheckmark) = CreateGoogleSwitch(hapticsRowGO.transform);

            // ============================================
            // VOICE TOGGLE
            // ============================================
            var voiceRowGO = new GameObject("VoiceRow");
            voiceRowGO.transform.SetParent(contentGO.transform, false);
            var voiceRowRect = voiceRowGO.AddComponent<RectTransform>();
            voiceRowRect.anchorMin = new Vector2(0.5f, 1);
            voiceRowRect.anchorMax = new Vector2(0.5f, 1);
            voiceRowRect.pivot = new Vector2(0.5f, 1);
            voiceRowRect.anchoredPosition = new Vector2(0, -700);
            voiceRowRect.sizeDelta = new Vector2(700, 80);

            var voiceLayout = voiceRowGO.AddComponent<HorizontalLayoutGroup>();
            voiceLayout.spacing = 20;
            voiceLayout.childAlignment = TextAnchor.MiddleCenter;
            voiceLayout.childForceExpandWidth = false;
            voiceLayout.childForceExpandHeight = false;

            // Voice label
            var voiceLabelGO = new GameObject("VoiceLabel");
            voiceLabelGO.transform.SetParent(voiceRowGO.transform, false);
            var voiceLabelText = voiceLabelGO.AddComponent<TextMeshProUGUI>();
            voiceLabelText.text = "Mascot Voices";
            voiceLabelText.fontSize = 36;
            voiceLabelText.fontStyle = FontStyles.Bold;
            voiceLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            voiceLabelText.color = Color.white;
            var voiceLabelLE = voiceLabelGO.AddComponent<LayoutElement>();
            voiceLabelLE.preferredWidth = 400;
            voiceLabelLE.preferredHeight = 60;

            // Voice toggle (Google-style switch)
            var (voiceToggle, voiceCheckmark) = CreateGoogleSwitch(voiceRowGO.transform);

            // ============================================
            // BUTTONS (Reset Progress, Credits)
            // ============================================
            var buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(contentGO.transform, false);
            var buttonsContainerRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsContainerRect.anchorMin = new Vector2(0.5f, 1);
            buttonsContainerRect.anchorMax = new Vector2(0.5f, 1);
            buttonsContainerRect.pivot = new Vector2(0.5f, 1);
            buttonsContainerRect.anchoredPosition = new Vector2(0, -820);
            buttonsContainerRect.sizeDelta = new Vector2(700, 200);

            var buttonsLayout = buttonsContainer.AddComponent<VerticalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            // Reset Progress button
            var resetBtn = CreateSettingsButton(buttonsContainer.transform, "Reset Progress", new Color(0.8f, 0.3f, 0.3f, 1f));

            // Credits button
            var creditsBtn = CreateSettingsButton(buttonsContainer.transform, "Credits", new Color(0.3f, 0.5f, 0.8f, 1f));

            // ============================================
            // BACK BUTTON
            // ============================================
            var backBtnContainer = new GameObject("BackButtonContainer");
            backBtnContainer.transform.SetParent(contentGO.transform, false);
            var backBtnContainerRect = backBtnContainer.AddComponent<RectTransform>();
            backBtnContainerRect.anchorMin = new Vector2(0.5f, 0);
            backBtnContainerRect.anchorMax = new Vector2(0.5f, 0);
            backBtnContainerRect.pivot = new Vector2(0.5f, 0);
            backBtnContainerRect.anchoredPosition = new Vector2(0, 50);
            backBtnContainerRect.sizeDelta = new Vector2(300, 80);

            var backBtnGO = new GameObject("BackButton");
            backBtnGO.transform.SetParent(backBtnContainer.transform, false);
            var backBtnRect = backBtnGO.AddComponent<RectTransform>();
            backBtnRect.anchorMin = Vector2.zero;
            backBtnRect.anchorMax = Vector2.one;
            backBtnRect.offsetMin = Vector2.zero;
            backBtnRect.offsetMax = Vector2.zero;

            var backBtnImg = backBtnGO.AddComponent<Image>();
            if (backNormalSprite != null)
            {
                backBtnImg.sprite = backNormalSprite;
                backBtnImg.preserveAspect = true;
            }
            else
            {
                backBtnImg.color = new Color(0.4f, 0.7f, 0.9f, 1f);

                // Fallback text
                var backTextGO = new GameObject("Text");
                backTextGO.transform.SetParent(backBtnGO.transform, false);
                var backTextRect = backTextGO.AddComponent<RectTransform>();
                backTextRect.anchorMin = Vector2.zero;
                backTextRect.anchorMax = Vector2.one;
                backTextRect.offsetMin = Vector2.zero;
                backTextRect.offsetMax = Vector2.zero;
                var backText = backTextGO.AddComponent<TextMeshProUGUI>();
                backText.text = "BACK";
                backText.fontSize = 32;
                backText.fontStyle = FontStyles.Bold;
                backText.alignment = TextAlignmentOptions.Center;
                backText.color = Color.white;
            }

            var backBtn = backBtnGO.AddComponent<Button>();
            backBtn.targetGraphic = backBtnImg;
            if (backNormalSprite != null && backPressedSprite != null)
            {
                backBtn.transition = Selectable.Transition.SpriteSwap;
                var spriteState = new SpriteState { pressedSprite = backPressedSprite };
                backBtn.spriteState = spriteState;
            }

            // ============================================
            // CONFIRMATION DIALOG
            // ============================================
            var (confirmDialog, confirmYes, confirmNo) = CreateConfirmationDialog(settingsPanel.transform);

            // ============================================
            // CREDITS PANEL
            // ============================================
            var (creditsPanel, creditsClose) = CreateCreditsPanel(settingsPanel.transform);

            // ============================================
            // ADD SETTINGS SCREEN COMPONENT
            // ============================================
            settingsScreen = settingsPanel.AddComponent<SettingsScreen>();

            // Set canvasGroup via reflection
            var baseType = typeof(BaseScreen);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            baseType.GetField("canvasGroup", flags)?.SetValue(settingsScreen, canvasGroup);

            // Initialize settings screen
            settingsScreen.Initialize(
                backBtn,
                masterSlider, musicSlider, sfxSlider,
                masterLabel, musicLabel, sfxLabel,
                hapticsToggle, hapticsCheckmark,
                voiceToggle, voiceCheckmark,
                resetBtn, creditsBtn,
                confirmDialog, confirmYes, confirmNo,
                creditsPanel, creditsClose,
                dimBg
            );

            // Subscribe to progress reset to refresh level select
            settingsScreen.OnResetConfirmed += () =>
            {
                levelSelectScreen?.RefreshDisplay();
            };

            Debug.Log("[UIManager] Settings panel created");
        }

        private (Slider slider, TextMeshProUGUI label) CreateVolumeSliderRow(Transform parent, string labelText)
        {
            var rowGO = new GameObject(labelText + "Row");
            rowGO.transform.SetParent(parent, false);
            var rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 80);

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 15;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 80;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = labelText;
            labelTMP.fontSize = 28;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = new Color(0.3f, 0.2f, 0.1f, 1f); // Dark brown
            var labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 220;
            labelLE.preferredHeight = 50;

            // Slider
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(rowGO.transform, false);
            var sliderLE = sliderGO.AddComponent<LayoutElement>();
            sliderLE.preferredWidth = 350;
            sliderLE.preferredHeight = 40;

            // Slider background
            var sliderBgGO = new GameObject("Background");
            sliderBgGO.transform.SetParent(sliderGO.transform, false);
            var sliderBgRect = sliderBgGO.AddComponent<RectTransform>();
            sliderBgRect.anchorMin = new Vector2(0, 0.25f);
            sliderBgRect.anchorMax = new Vector2(1, 0.75f);
            sliderBgRect.offsetMin = Vector2.zero;
            sliderBgRect.offsetMax = Vector2.zero;
            var sliderBgImg = sliderBgGO.AddComponent<Image>();
            sliderBgImg.color = new Color(0.6f, 0.5f, 0.4f, 1f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.7f, 0.9f, 1f); // Blue fill

            // Handle
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30, 50);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            // Slider component
            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.7f;

            // Value label
            var valueLabelGO = new GameObject("Value");
            valueLabelGO.transform.SetParent(rowGO.transform, false);
            var valueTMP = valueLabelGO.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "70%";
            valueTMP.fontSize = 28;
            valueTMP.alignment = TextAlignmentOptions.MidlineRight;
            valueTMP.color = new Color(0.3f, 0.2f, 0.1f, 1f);
            var valueLE = valueLabelGO.AddComponent<LayoutElement>();
            valueLE.preferredWidth = 80;
            valueLE.preferredHeight = 50;

            return (slider, valueTMP);
        }

        private (Toggle toggle, Image checkmark) CreateToggle(Transform parent, Sprite checkboxSprite)
        {
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(parent, false);
            var toggleLE = toggleGO.AddComponent<LayoutElement>();
            toggleLE.preferredWidth = 60;
            toggleLE.preferredHeight = 60;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(toggleGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            if (checkboxSprite != null)
            {
                bgImg.sprite = checkboxSprite;
            }
            else
            {
                bgImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            }

            // Checkmark
            var checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(bgGO.transform, false);
            var checkRect = checkGO.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImg = checkGO.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.7f, 0.3f, 1f); // Green checkmark

            // Toggle component
            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = true;

            return (toggle, checkImg);
        }

        private (Toggle toggle, Image handle) CreateGoogleSwitch(Transform parent)
        {
            var toggleGO = new GameObject("Switch");
            toggleGO.transform.SetParent(parent, false);
            var toggleLE = toggleGO.AddComponent<LayoutElement>();
            toggleLE.preferredWidth = 100;
            toggleLE.preferredHeight = 50;

            // Track background (pill shape)
            var trackGO = new GameObject("Track");
            trackGO.transform.SetParent(toggleGO.transform, false);
            var trackRect = trackGO.AddComponent<RectTransform>();
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;

            var trackImg = trackGO.AddComponent<Image>();
            // Create rounded pill shape for track
            var trackSprite = CreateRoundedRectSprite(100, 50, 25, new Color(0.7f, 0.7f, 0.7f, 1f));
            if (trackSprite != null)
            {
                trackImg.sprite = trackSprite;
                trackImg.type = Image.Type.Sliced;
            }
            else
            {
                trackImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            // Handle (circular knob)
            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(trackGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.pivot = new Vector2(0, 0.5f);
            handleRect.anchoredPosition = new Vector2(4, 0);
            handleRect.sizeDelta = new Vector2(42, 42);

            var handleImg = handleGO.AddComponent<Image>();
            // Create circular handle
            var handleSprite = CreateRoundedRectSprite(42, 42, 21, Color.white);
            if (handleSprite != null)
            {
                handleImg.sprite = handleSprite;
            }
            else
            {
                handleImg.color = Color.white;
            }

            // Add shadow effect to handle
            var shadow = handleGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.3f);
            shadow.effectDistance = new Vector2(2, -2);

            // Toggle component
            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = trackImg;
            toggle.isOn = true;

            // Add the switch behavior component
            var switchBehavior = toggleGO.AddComponent<GoogleSwitchBehavior>();
            switchBehavior.Initialize(trackImg, handleRect);

            return (toggle, handleImg);
        }

        private Button CreateSettingsButton(Transform parent, string text, Color bgColor)
        {
            var btnGO = new GameObject(text + "Button");
            btnGO.transform.SetParent(parent, false);
            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 500;
            btnLE.preferredHeight = 70;

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = bgColor;

            // Add rounded corners effect
            var roundedSprite = CreateRoundedRectSprite(256, 64, 20, bgColor);
            if (roundedSprite != null)
            {
                btnImg.sprite = roundedSprite;
                btnImg.type = Image.Type.Sliced;
            }

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text = text;
            textTMP.fontSize = 32;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color = Color.white;

            return btn;
        }

        private (GameObject dialog, Button yesBtn, Button noBtn) CreateConfirmationDialog(Transform parent)
        {
            var dialogGO = new GameObject("ConfirmationDialog");
            dialogGO.transform.SetParent(parent, false);
            var dialogRect = dialogGO.AddComponent<RectTransform>();
            dialogRect.anchorMin = Vector2.zero;
            dialogRect.anchorMax = Vector2.one;
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;

            // Dark overlay
            var overlayImg = dialogGO.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.8f);

            // Dialog box
            var boxGO = new GameObject("DialogBox");
            boxGO.transform.SetParent(dialogGO.transform, false);
            var boxRect = boxGO.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(700, 400);

            var boxImg = boxGO.AddComponent<Image>();
            boxImg.color = new Color(0.95f, 0.9f, 0.85f, 1f);

            var boxLayout = boxGO.AddComponent<VerticalLayoutGroup>();
            boxLayout.padding = new RectOffset(40, 40, 40, 40);
            boxLayout.spacing = 30;
            boxLayout.childAlignment = TextAnchor.MiddleCenter;
            boxLayout.childForceExpandWidth = true;
            boxLayout.childForceExpandHeight = false;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(boxGO.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "Reset Progress?";
            titleTMP.fontSize = 40;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(0.3f, 0.2f, 0.1f, 1f);
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 60;

            // Message
            var msgGO = new GameObject("Message");
            msgGO.transform.SetParent(boxGO.transform, false);
            var msgTMP = msgGO.AddComponent<TextMeshProUGUI>();
            msgTMP.text = "This will delete all your progress.\nThis action cannot be undone!";
            msgTMP.fontSize = 28;
            msgTMP.alignment = TextAlignmentOptions.Center;
            msgTMP.color = new Color(0.4f, 0.3f, 0.2f, 1f);
            var msgLE = msgGO.AddComponent<LayoutElement>();
            msgLE.preferredHeight = 100;

            // Buttons container
            var btnContainerGO = new GameObject("Buttons");
            btnContainerGO.transform.SetParent(boxGO.transform, false);
            var btnContainerLE = btnContainerGO.AddComponent<LayoutElement>();
            btnContainerLE.preferredHeight = 80;

            var btnLayout = btnContainerGO.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 40;
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.childForceExpandWidth = false;
            btnLayout.childForceExpandHeight = false;

            // No button (Cancel)
            var noBtn = CreateDialogButton(btnContainerGO.transform, "Cancel", new Color(0.5f, 0.5f, 0.5f, 1f));

            // Yes button (Confirm)
            var yesBtn = CreateDialogButton(btnContainerGO.transform, "Reset", new Color(0.8f, 0.3f, 0.3f, 1f));

            dialogGO.SetActive(false);
            return (dialogGO, yesBtn, noBtn);
        }

        private Button CreateDialogButton(Transform parent, string text, Color bgColor)
        {
            var btnGO = new GameObject(text + "Button");
            btnGO.transform.SetParent(parent, false);
            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 200;
            btnLE.preferredHeight = 60;

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = bgColor;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text = text;
            textTMP.fontSize = 28;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color = Color.white;

            return btn;
        }

        private (GameObject panel, Button closeBtn) CreateCreditsPanel(Transform parent)
        {
            var panelGO = new GameObject("CreditsPanel");
            panelGO.transform.SetParent(parent, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Dark overlay
            var overlayImg = panelGO.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.85f);

            // Credits content box
            var boxGO = new GameObject("CreditsBox");
            boxGO.transform.SetParent(panelGO.transform, false);
            var boxRect = boxGO.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(800, 1000);

            var boxImg = boxGO.AddComponent<Image>();
            boxImg.color = new Color(0.2f, 0.15f, 0.1f, 0.95f);

            var boxLayout = boxGO.AddComponent<VerticalLayoutGroup>();
            boxLayout.padding = new RectOffset(50, 50, 50, 50);
            boxLayout.spacing = 25;
            boxLayout.childAlignment = TextAnchor.UpperCenter;
            boxLayout.childForceExpandWidth = true;
            boxLayout.childForceExpandHeight = false;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(boxGO.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "CREDITS";
            titleTMP.fontSize = 48;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.85f, 0.5f, 1f); // Gold
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 70;

            // Credits content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(boxGO.transform, false);
            var contentTMP = contentGO.AddComponent<TextMeshProUGUI>();
            contentTMP.text = @"<size=48><b>Sort Resort</b></size>
<size=36>A Casual Puzzle Game</size>


<b>Game Design & Development</b>
Wilson Warmack

<b>Art & Graphics</b>
Filipe Sabino

<b>Music & Sound</b>
Filipe Sabino


<b>Special Thanks</b>
Antonia and Joakim Engfors

<size=20>Version 1.0</size>";
            contentTMP.fontSize = 28;
            contentTMP.alignment = TextAlignmentOptions.Center;
            contentTMP.color = Color.white;
            contentTMP.richText = true;
            var contentLE = contentGO.AddComponent<LayoutElement>();
            contentLE.preferredHeight = 650;

            // Close button
            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(boxGO.transform, false);
            var closeBtnLE = closeBtnGO.AddComponent<LayoutElement>();
            closeBtnLE.preferredWidth = 200;
            closeBtnLE.preferredHeight = 60;

            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            closeBtnImg.color = new Color(0.4f, 0.6f, 0.8f, 1f);

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;

            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
            var closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeTextTMP = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeTextTMP.text = "Close";
            closeTextTMP.fontSize = 28;
            closeTextTMP.fontStyle = FontStyles.Bold;
            closeTextTMP.alignment = TextAlignmentOptions.Center;
            closeTextTMP.color = Color.white;

            panelGO.SetActive(false);
            return (panelGO, closeBtn);
        }

        /// <summary>
        /// Show the settings screen
        /// </summary>
        public void ShowSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.transform.SetAsLastSibling();
                settingsPanel.SetActive(true);
                settingsScreen?.Show();
            }
            GameEvents.InvokeSettingsOpened();
        }

        /// <summary>
        /// Hide the settings screen
        /// </summary>
        public void HideSettings()
        {
            if (settingsScreen != null)
            {
                settingsScreen.Hide();
            }
            else if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void CreatePauseMenuPanel()
        {
            pauseMenuPanel = new GameObject("Pause Menu Panel");
            pauseMenuPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = pauseMenuPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Override sorting so pause menu renders above portal ResultOverlays (sortingOrder 5001)
            var pauseCanvas = pauseMenuPanel.AddComponent<Canvas>();
            pauseCanvas.overrideSorting = true;
            pauseCanvas.sortingOrder = 5200;
            pauseMenuPanel.AddComponent<GraphicRaycaster>();

            // Dark overlay/dim background
            var dimBg = pauseMenuPanel.AddComponent<Image>();
            dimBg.color = new Color(0, 0, 0, 0.5f);

            // Add CanvasGroup for fading
            var canvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();

            // Board background - fullscreen overlay sprite (1080x1920)
            var boardGO = new GameObject("Board");
            boardGO.transform.SetParent(pauseMenuPanel.transform, false);
            var boardRect = boardGO.AddComponent<RectTransform>();
            boardRect.anchorMin = Vector2.zero;
            boardRect.anchorMax = Vector2.one;
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;
            var boardImg = boardGO.AddComponent<Image>();
            boardImg.sprite = LoadFullRectSprite("Sprites/UI/PauseMenu/pause_board");
            boardImg.preserveAspect = true;
            boardImg.raycastTarget = false;

            // Create sprite-based buttons with hit areas at exact pixel positions
            // Button positions from sprite analysis (in 1080x1920 canvas):
            // Resume:   center=(534, 877),  size=458x141
            // Restart:  center=(534, 1055), size=458x141
            // Settings: center=(534, 1229), size=458x142
            // Quit:     center=(534, 1404), size=458x142
            var resumeBtn = CreatePauseMenuSpriteButton(pauseMenuPanel.transform, "Resume",
                "Sprites/UI/PauseMenu/pause_resume", "Sprites/UI/PauseMenu/pause_resume_pressed",
                534f / 1080f, 1f - 877f / 1920f, 458f, 141f);
            var restartBtn = CreatePauseMenuSpriteButton(pauseMenuPanel.transform, "Restart",
                "Sprites/UI/PauseMenu/pause_restart", "Sprites/UI/PauseMenu/pause_restart_pressed",
                534f / 1080f, 1f - 1055f / 1920f, 458f, 141f);
            var settingsBtn = CreatePauseMenuSpriteButton(pauseMenuPanel.transform, "Settings",
                "Sprites/UI/PauseMenu/pause_settings", "Sprites/UI/PauseMenu/pause_settings_pressed",
                534f / 1080f, 1f - 1229f / 1920f, 458f, 142f);
            var exitBtn = CreatePauseMenuSpriteButton(pauseMenuPanel.transform, "Quit",
                "Sprites/UI/PauseMenu/pause_quit", "Sprites/UI/PauseMenu/pause_quit_pressed",
                534f / 1080f, 1f - 1404f / 1920f, 458f, 142f);

            // Add PauseMenuScreen component
            pauseMenuScreen = pauseMenuPanel.AddComponent<PauseMenuScreen>();

            // Set canvasGroup via reflection
            var baseType = typeof(BaseScreen);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            baseType.GetField("canvasGroup", flags)?.SetValue(pauseMenuScreen, canvasGroup);

            // Initialize
            pauseMenuScreen.Initialize(resumeBtn, restartBtn, settingsBtn, exitBtn, dimBg);

            // When settings is requested from pause menu, show settings
            pauseMenuScreen.OnSettingsRequested += () =>
            {
                ShowSettings();
            };

            Debug.Log("[UIManager] Pause menu panel created with sprite-based buttons");
        }

        /// <summary>
        /// Creates a pause menu button using fullscreen sprite overlays with a clickable hit area
        /// at the exact button position within the 1080x1920 canvas.
        /// </summary>
        private Button CreatePauseMenuSpriteButton(Transform parent, string name,
            string normalPath, string pressedPath,
            float anchorX, float anchorY, float hitWidth, float hitHeight)
        {
            // Fullscreen sprite layer for the button visual
            var spriteGO = new GameObject(name + " Sprite");
            spriteGO.transform.SetParent(parent, false);
            var spriteRect = spriteGO.AddComponent<RectTransform>();
            spriteRect.anchorMin = Vector2.zero;
            spriteRect.anchorMax = Vector2.one;
            spriteRect.offsetMin = Vector2.zero;
            spriteRect.offsetMax = Vector2.zero;
            var spriteImg = spriteGO.AddComponent<Image>();
            var normalSprite = LoadFullRectSprite(normalPath);
            var pressedSprite = LoadFullRectSprite(pressedPath);
            spriteImg.sprite = normalSprite;
            spriteImg.preserveAspect = true;
            spriteImg.raycastTarget = false;

            // Clickable hit area positioned at the button's exact location
            var hitGO = new GameObject(name + " HitArea");
            hitGO.transform.SetParent(spriteGO.transform, false);
            var hitRect = hitGO.AddComponent<RectTransform>();
            hitRect.anchorMin = new Vector2(anchorX, anchorY);
            hitRect.anchorMax = new Vector2(anchorX, anchorY);
            hitRect.pivot = new Vector2(0.5f, 0.5f);
            hitRect.anchoredPosition = Vector2.zero;
            // Scale hit area from pixel dimensions to proportional canvas size
            hitRect.sizeDelta = new Vector2(hitWidth / 1080f * Screen.width, hitHeight / 1920f * Screen.height);
            // Use anchor-relative sizing instead for canvas consistency
            hitRect.anchorMin = new Vector2(anchorX - (hitWidth / 1080f) * 0.5f, anchorY - (hitHeight / 1920f) * 0.5f);
            hitRect.anchorMax = new Vector2(anchorX + (hitWidth / 1080f) * 0.5f, anchorY + (hitHeight / 1920f) * 0.5f);
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;

            var hitImg = hitGO.AddComponent<Image>();
            hitImg.color = Color.clear; // Invisible but catches raycasts

            var btn = hitGO.AddComponent<Button>();
            btn.targetGraphic = hitImg;
            btn.transition = Selectable.Transition.None;

            // Swap fullscreen sprite on press via event triggers
            var pressHandler = hitGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { spriteImg.sprite = pressedSprite ?? normalSprite; });
            pressHandler.triggers.Add(pointerDown);

            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { spriteImg.sprite = normalSprite; });
            pressHandler.triggers.Add(pointerUp);

            return btn;
        }

        /// <summary>
        /// Show the pause menu
        /// </summary>
        public void ShowPauseMenu()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.transform.SetAsLastSibling();
                pauseMenuPanel.SetActive(true);
                if (pauseMenuScreen != null)
                {
                    pauseMenuScreen.Show(instant: true);
                }
                Debug.Log("[UIManager] Pause menu shown");
            }
        }

        /// <summary>
        /// Hide the pause menu
        /// </summary>
        public void HidePauseMenu()
        {
            if (pauseMenuScreen != null)
            {
                pauseMenuScreen.Hide(instant: true);
            }
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
            Debug.Log("[UIManager] Pause menu hidden");
        }

        private GameObject CreateTextElement(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float height = 50)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(500, height);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // Add layout element for vertical layout groups
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;

            return go;
        }

        private Button CreateButton(Transform parent, string name, string label, System.Action onClick,
            float width = 60, float height = 50)
        {
            var btnGO = new GameObject(name + " Button");
            btnGO.transform.SetParent(parent, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            // Add LayoutElement to maintain size in layout groups
            var layoutElement = btnGO.AddComponent<LayoutElement>();
            layoutElement.minWidth = width;
            layoutElement.minHeight = height;
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;

            // Background image
            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.8f, 1f); // Blue color

            // Add black outline for visual distinction
            var outline = btnGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            // Set up button colors for feedback
            var colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.5f, 0.8f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.1f, 0.4f, 0.7f, 1f);
            colors.selectedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            btn.colors = colors;

            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick.Invoke());
            }

            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false; // Prevent word wrap
            tmp.overflowMode = TextOverflowModes.Overflow;

            return btn;
        }

        /// <summary>
        /// Loads a texture and creates a sprite covering the full texture rect.
        /// Use for fullscreen overlay sprites (1080x1920) where the import may trim transparent areas.
        /// </summary>
        private static Sprite LoadFullRectSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            Debug.LogWarning($"[UIManager] Failed to load texture for full-rect sprite: {resourcePath}");
            return null;
        }

        private static Sprite LoadSpriteFromTexture(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                Debug.Log($"[UIManager] Loaded sprite directly: {resourcePath} ({sprite.rect.width}x{sprite.rect.height})");
                return sprite;
            }
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                Debug.Log($"[UIManager] Loaded texture fallback: {resourcePath} ({tex.width}x{tex.height})");
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            Debug.LogWarning($"[UIManager] Failed to load sprite or texture: {resourcePath}");
            return null;
        }

        private static Sprite[] LoadStarFrames(string resourcePath, string label)
        {
            var textures = Resources.LoadAll<Texture2D>(resourcePath);
            if (textures.Length == 0)
            {
                Debug.LogWarning($"[UIManager] No star frames found at: {resourcePath}");
                return new Sprite[0];
            }
            System.Array.Sort(textures, (a, b) => a.name.CompareTo(b.name));
            var sprites = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            Debug.Log($"[UIManager] Loaded {sprites.Length} {label} frames");
            return sprites;
        }

        private Button CreateSpriteButton(Transform parent, string name, string normalSpritePath, string pressedSpritePath, System.Action onClick, float width, float height)
        {
            var btnGO = new GameObject(name + " Button");
            btnGO.transform.SetParent(parent, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var layoutElement = btnGO.AddComponent<LayoutElement>();
            layoutElement.minWidth = width;
            layoutElement.minHeight = height;
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;

            var normalSprite = LoadSpriteFromTexture(normalSpritePath);
            var pressedSprite = LoadSpriteFromTexture(pressedSpritePath);

            var img = btnGO.AddComponent<Image>();
            img.sprite = normalSprite;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;

            var spriteState = new SpriteState();
            spriteState.pressedSprite = pressedSprite;
            spriteState.highlightedSprite = normalSprite;
            spriteState.selectedSprite = normalSprite;
            btn.spriteState = spriteState;

            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick.Invoke());
            }

            return btn;
        }


        private IEnumerator PlayStarAnimation(Image starImage, Sprite[] frames, float delay)
        {
            if (starImage == null || frames == null || frames.Length == 0) yield break;

            yield return new WaitForSecondsRealtime(delay);

            starImage.gameObject.SetActive(true);
            float frameTime = 1f / 15f; // 15 fps

            for (int i = 0; i < frames.Length; i++)
            {
                starImage.sprite = frames[i];
                if (i < frames.Length - 1)
                    yield return new WaitForSecondsRealtime(frameTime);
            }
            // Hold on last frame
        }

        private IEnumerator PlayDancingStarsAnimation()
        {
            if (dancingStarsImage == null || dancingStarsFrames == null || dancingStarsFrames.Length == 0)
                yield break;

            // Hide gold and grey stars so they don't show behind the dancing animation
            if (levelCompleteStar1Image != null) levelCompleteStar1Image.gameObject.SetActive(false);
            if (levelCompleteStar2Image != null) levelCompleteStar2Image.gameObject.SetActive(false);
            if (levelCompleteStar3Image != null) levelCompleteStar3Image.gameObject.SetActive(false);
            if (levelCompleteGreyStarsImage != null) levelCompleteGreyStarsImage.gameObject.SetActive(false);

            dancingStarsImage.gameObject.SetActive(true);
            float frameTime = 1f / 24f; // 24 fps

            for (int i = 0; i < dancingStarsFrames.Length; i++)
            {
                dancingStarsImage.sprite = dancingStarsFrames[i];
                if (i < dancingStarsFrames.Length - 1)
                    yield return new WaitForSecondsRealtime(frameTime);
            }
            // Hold on last frame
            dancingStarsCoroutine = null;
        }

        private void ResetStarAnimations()
        {
            if (levelCompleteStar1Image != null) levelCompleteStar1Image.gameObject.SetActive(false);
            if (levelCompleteStar2Image != null) levelCompleteStar2Image.gameObject.SetActive(false);
            if (levelCompleteStar3Image != null) levelCompleteStar3Image.gameObject.SetActive(false);
            if (dancingStarsCoroutine != null)
            {
                StopCoroutine(dancingStarsCoroutine);
                dancingStarsCoroutine = null;
            }
            if (dancingStarsImage != null) dancingStarsImage.gameObject.SetActive(false);
            if (newRecordPulseCoroutine != null)
            {
                StopCoroutine(newRecordPulseCoroutine);
                newRecordPulseCoroutine = null;
            }

            // Reset new elements
            if (dimOverlayCanvasGroup != null) dimOverlayCanvasGroup.alpha = 0f;
            if (victoryBoardCanvasGroup != null) victoryBoardCanvasGroup.alpha = 0f;
            if (levelUICanvasGroup != null) levelUICanvasGroup.alpha = 0f;
            if (movesUICanvasGroup != null) movesUICanvasGroup.alpha = 0f;
            if (timerIconCanvasGroup != null) timerIconCanvasGroup.alpha = 0f;
            if (freeOverlayCanvasGroup != null) freeOverlayCanvasGroup.alpha = 0f;
            if (hardOverlayCanvasGroup != null) hardOverlayCanvasGroup.alpha = 0f;
            if (timerBarUICanvasGroup != null) timerBarUICanvasGroup.alpha = 0f;
            if (newRecordUICanvasGroup != null)
            {
                newRecordUICanvasGroup.alpha = 0f;
                var rt = newRecordUICanvasGroup.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;
            }
            if (levelCompleteMascotImage != null) levelCompleteMascotImage.enabled = false;
            StopLevelCompleteText();
        }

        private void StopLevelCompleteSequence()
        {
            if (levelCompleteSequence != null)
            {
                StopCoroutine(levelCompleteSequence);
                levelCompleteSequence = null;
            }
        }

        // Event handlers
        private void OnLevelStarted(int levelNumber)
        {
            StopLevelCompleteSequence();
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
                animatedLevelComplete?.Hide();
                ResetStarAnimations();
                CleanupMascotAnimator();
                StopLevelCompleteText();
            }
            if (hudPanel != null)
                hudPanel.SetActive(true);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(true);

            // Update title with level info
            string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
            string worldName = char.ToUpper(worldId[0]) + worldId.Substring(1);
            if (levelTitleText != null)
                levelTitleText.text = $"{worldName} - Level {levelNumber}";

            UpdateMoveDisplay(0);
            UpdateMatchDisplay(0);

            // Reset undo button (no moves to undo at level start)
            SetUndoButtonEnabled(false);

            // Mode-aware HUD visibility
            GameMode currentMode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;

            // Stars: show in Star Mode and Hard Mode only
            if (starDisplayGO != null)
                starDisplayGO.SetActive(currentMode == GameMode.StarMode || currentMode == GameMode.HardMode);

            // Timer: hidden initially, shown by OnTimerUpdated for Timer/Hard modes
            if (timerContainer != null)
                timerContainer.SetActive(false);

            // Mode-specific HUD overlay - all modes get their own bar
            string overlayBarPath = GetOverlayBarPath(currentMode);
            string overlayIconPath = $"Sprites/UI/HUD/{worldId}_icon_UI_top";
            var barTex = Resources.Load<Texture2D>(overlayBarPath);
            var iconTex = Resources.Load<Texture2D>(overlayIconPath);

            if (barTex != null)
            {
                // Show overlay
                if (hudModeOverlay != null)
                    hudModeOverlay.SetActive(true);

                // Set bar sprite
                if (hudOverlayBgImage != null)
                {
                    hudOverlayBgImage.sprite = Sprite.Create(barTex,
                        new Rect(0, 0, barTex.width, barTex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }

                // Set world icon sprite (or hide if missing)
                if (hudWorldIconImage != null)
                {
                    if (iconTex != null)
                    {
                        hudWorldIconImage.sprite = Sprite.Create(iconTex,
                            new Rect(0, 0, iconTex.width, iconTex.height),
                            new Vector2(0.5f, 0.5f), 100f);
                        hudWorldIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        hudWorldIconImage.gameObject.SetActive(false);
                    }
                }

                // Position and show/hide counter texts based on mode
                PositionOverlayCounters(currentMode);

                // Set overlay counter text values
                if (overlayLevelText != null)
                    overlayLevelText.text = levelNumber.ToString();
                if (overlayMovesText != null)
                    overlayMovesText.text = "0";
                if (overlayTimerText != null)
                    overlayTimerText.text = "0:00.00";

                // Hide default HUD elements (background, title, stats) but keep buttons + stars
                if (hudPanelBgImage != null)
                    hudPanelBgImage.color = new Color(0, 0, 0, 0);
                if (levelTitleText != null)
                    levelTitleText.gameObject.SetActive(false);
                if (statsContainerGO != null)
                    statsContainerGO.SetActive(false);
            }
            else
            {
                // No bar asset - fallback to default HUD
                if (hudModeOverlay != null)
                    hudModeOverlay.SetActive(false);
                if (hudPanelBgImage != null)
                    hudPanelBgImage.color = new Color(0, 0, 0, 0.7f);
                if (levelTitleText != null)
                    levelTitleText.gameObject.SetActive(true);
                if (statsContainerGO != null)
                    statsContainerGO.SetActive(true);
            }

#if UNITY_EDITOR
            // Reset record button text (recording is stopped when level changes)
            if (recordButton != null)
            {
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rec";
                recordButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.5f, 0.5f); // Normal red
            }
#endif
        }

        private void OnMoveUsed(int moveCount)
        {
            UpdateMoveDisplay(moveCount);
            UpdateStarPreview(moveCount);
        }

        private void OnMatchCountChanged(int matchCount)
        {
            UpdateMatchDisplay(matchCount);
        }

        private void OnItemsRemainingChanged(int remaining)
        {
            if (itemsRemainingText != null)
                itemsRemainingText.text = remaining.ToString();
        }

        private void OnLevelCompletedDetailed(LevelCompletionData data)
        {
            lastCompletionData = data;
        }

        private void OnLevelCompleted(int levelNumber, int stars)
        {
            // Hide overlays during level complete screen
            if (hudModeOverlay != null)
                hudModeOverlay.SetActive(false);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(false);

            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);

                GameMode mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;

                // Set text values (hidden until their layers fade in)
                if (levelCompleteLevelNumberText != null)
                    levelCompleteLevelNumberText.text = levelNumber.ToString();
                if (levelCompleteMoveCountText != null)
                {
                    int moveCount = GameManager.Instance?.CurrentMoveCount ?? 0;
                    levelCompleteMoveCountText.text = moveCount.ToString();
                }

                // Reset everything to initial hidden state
                ResetStarAnimations();
                if (greyStarsCanvasGroup != null)
                {
                    greyStarsCanvasGroup.alpha = 0f;
                    greyStarsCanvasGroup.gameObject.SetActive(true);
                }
                if (bottomBoardImage != null) bottomBoardImage.gameObject.SetActive(false);
                if (buttonsContainerGO != null) buttonsContainerGO.SetActive(false);

                // Load correct victory board for current world
                string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
                var boardSprite = LoadFullRectSprite($"Sprites/UI/LevelComplete/VictoryBoard/{worldId}_board_victory_screen");
                if (boardSprite != null && victoryBoardImage != null)
                    victoryBoardImage.sprite = boardSprite;

                // Start the sequenced animation
                if (levelCompleteSequence != null)
                    StopCoroutine(levelCompleteSequence);
                levelCompleteSequence = StartCoroutine(LevelCompleteAnimationSequence(levelNumber, stars, mode));
            }
        }

        private IEnumerator LevelCompleteAnimationSequence(int levelNumber, int stars, GameMode mode)
        {
            Debug.Log($"[UIManager] LevelComplete sequence: Mode={mode}, Stars={stars}");

            // ===== PHASE A: Mascot Celebration on dimmed background =====

            // Destroy any lingering match effects so they don't bleed through the dim overlay
            foreach (var fx in FindObjectsOfType<MatchEffect>())
                Destroy(fx.gameObject);

            // Load and start mascot FIRST (Resources.LoadAll is expensive/blocking)
            // Mascot is above dim overlay in sibling order, so it's visible immediately
            UpdateLevelCompleteMascot(stars);

            // Play level complete sound alongside mascot animation
            AudioManager.Instance?.PlayLevelCompleteSound();

            // Start "Level Complete" text animation (plays on top of mascot)
            PlayLevelCompleteText();

            // Fade in dim overlay behind the already-playing mascot + text
            yield return StartCoroutine(FadeCanvasGroup(dimOverlayCanvasGroup, 0f, 0.95f, 0.3f));

            // Wait for remaining mascot animation time (subtract the dim fade duration)
            float mascotWait = 2.0f;
            if (levelCompleteMascotAnimator != null && levelCompleteMascotAnimator.Duration > 0)
                mascotWait = levelCompleteMascotAnimator.Duration;
            float remainingWait = Mathf.Max(0f, mascotWait - 0.3f);
            yield return new WaitForSecondsRealtime(remainingWait);

            // Hold on last frame for an extra beat
            yield return new WaitForSecondsRealtime(0.5f);

            // Fade out mascot + text + dim overlay together
            if (levelCompleteMascotImage != null)
                levelCompleteMascotImage.enabled = false;
            StopLevelCompleteText();
            CleanupMascotAnimator();
            yield return StartCoroutine(FadeCanvasGroup(dimOverlayCanvasGroup, 0.95f, 0f, 0.3f));

            // ===== PHASE B: Victory Screen =====

            // Start rays + curtains animation
            if (animatedLevelComplete != null)
                animatedLevelComplete.PlayRaysAndCurtains();

            // Fade in mode-specific overlay on rays+curtains screen
            if (mode == GameMode.FreePlay && freeOverlayCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(freeOverlayCanvasGroup, 0f, 1f, 0.3f));
            else if (mode == GameMode.TimerMode && timerIconCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(timerIconCanvasGroup, 0f, 1f, 0.3f));
            else if (mode == GameMode.HardMode && hardOverlayCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(hardOverlayCanvasGroup, 0f, 1f, 0.3f));

            // Fade in victory board
            yield return StartCoroutine(FadeCanvasGroup(victoryBoardCanvasGroup, 0f, 1f, 0.3f));

            // Fade in level_UI with number text (all modes)
            yield return StartCoroutine(FadeCanvasGroup(levelUICanvasGroup, 0f, 1f, 0.3f));

            // Moves UI: show for Star and Hard modes
            if (mode == GameMode.StarMode || mode == GameMode.HardMode)
            {
                yield return StartCoroutine(FadeCanvasGroup(movesUICanvasGroup, 0f, 1f, 0.3f));
            }

            // Mode-specific middle section
            switch (mode)
            {
                case GameMode.FreePlay:
                    // Free Play: skip stars and timer, go straight to bottom board
                    yield return new WaitForSecondsRealtime(0.3f);
                    break;

                case GameMode.StarMode:
                    // Star Mode: full star ribbon + star animations
                    yield return StartCoroutine(PlayStarSequence(stars));
                    // Show "New Record!" if better stars
                    if (lastCompletionData.isNewBestStars)
                        yield return StartCoroutine(ShowNewRecordAnimation());
                    break;

                case GameMode.TimerMode:
                    // Timer Mode: timer bar with count-up animation
                    yield return new WaitForSecondsRealtime(0.3f);
                    yield return StartCoroutine(PlayTimerCountUpAnimation());
                    // Show "New Record!" if best time
                    if (lastCompletionData.isNewBestTime)
                        yield return StartCoroutine(ShowNewRecordAnimation());
                    break;

                case GameMode.HardMode:
                    // Hard Mode: stars first, then timer
                    yield return StartCoroutine(PlayStarSequence(stars));
                    yield return new WaitForSecondsRealtime(0.2f);
                    yield return StartCoroutine(PlayTimerCountUpAnimation());
                    // Show "New Record!" if better stars or best time
                    if (lastCompletionData.isNewBestStars || lastCompletionData.isNewBestTime)
                        yield return StartCoroutine(ShowNewRecordAnimation());
                    break;
            }

            Debug.Log("[UIManager] LevelComplete sequence: Bottom board + buttons");
            yield return StartCoroutine(PlayBottomBoardAnimation());
            if (buttonsContainerGO != null) buttonsContainerGO.SetActive(true);

            Debug.Log("[UIManager] LevelComplete sequence: COMPLETE");
            levelCompleteSequence = null;
        }

        private IEnumerator PlayStarSequence(int stars)
        {
            // Star ribbon expands from center
            if (animatedLevelComplete != null)
                animatedLevelComplete.PlayStarRibbon();
            while (animatedLevelComplete != null && !animatedLevelComplete.IsStarRibbonComplete)
                yield return null;

            // Grey stars fade in on the ribbon
            yield return StartCoroutine(FadeCanvasGroup(greyStarsCanvasGroup, 0f, 1f, 0.25f));

            // Brief pause, then stars animate sequentially
            yield return new WaitForSecondsRealtime(0.3f);

            if (stars >= 1)
            {
                AudioManager.Instance?.PlayStarEarned(1);
                yield return StartCoroutine(PlayStarAnimation(levelCompleteStar1Image, star1Frames, 0f));
            }
            if (stars >= 2)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                AudioManager.Instance?.PlayStarEarned(2);
                yield return StartCoroutine(PlayStarAnimation(levelCompleteStar2Image, star2Frames, 0f));
            }
            if (stars >= 3)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                AudioManager.Instance?.PlayStarEarned(3);
                yield return StartCoroutine(PlayStarAnimation(levelCompleteStar3Image, star3Frames, 0f));
                dancingStarsCoroutine = StartCoroutine(PlayDancingStarsAnimation());
            }
        }

        private IEnumerator PlayTimerCountUpAnimation()
        {
            float finalTime = lastCompletionData.timeTaken;

            if (finalTime <= 0f)
            {
                // No valid time, skip
                yield break;
            }

            // Initialize timer text and fade in the timer bar UI
            if (timerBarText != null)
                timerBarText.text = "0:00.00";
            yield return StartCoroutine(FadeCanvasGroup(timerBarUICanvasGroup, 0f, 1f, 0.2f));

            // Play timer count-up sound effect (3 seconds to match)
            AudioManager.Instance?.PlayTimerCountUp();

            // Animate counting up from 0 to final time over 3 seconds
            float countDuration = 3.0f;
            float elapsed = 0f;

            while (elapsed < countDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / countDuration);
                // Ease out for a satisfying deceleration
                t = 1f - (1f - t) * (1f - t);
                float displayTime = Mathf.Lerp(0f, finalTime, t);

                int minutes = (int)(displayTime / 60);
                int seconds = (int)(displayTime % 60);
                int centiseconds = (int)((displayTime % 1f) * 100f);

                if (timerBarText != null)
                    timerBarText.text = $"{minutes}:{seconds:D2}.{centiseconds:D2}";

                yield return null;
            }

            // Set final value precisely
            {
                int minutes = (int)(finalTime / 60);
                int seconds = (int)(finalTime % 60);
                int centiseconds = (int)((finalTime % 1f) * 100f);
                if (timerBarText != null)
                    timerBarText.text = $"{minutes}:{seconds:D2}.{centiseconds:D2}";
            }

            yield return new WaitForSecondsRealtime(0.3f);
        }

        private IEnumerator ShowNewRecordAnimation()
        {
            if (newRecordUICanvasGroup == null) yield break;

            yield return new WaitForSecondsRealtime(0.3f);

            var rectTransform = newRecordUICanvasGroup.GetComponent<RectTransform>();

            // Pop-in: scale from 0 to 1.2 then settle to 1.0, while fading in
            float popDuration = 0.35f;
            float popElapsed = 0f;
            Vector3 originalScale = Vector3.one;

            while (popElapsed < popDuration)
            {
                popElapsed += Time.unscaledDeltaTime;
                float bt = Mathf.Clamp01(popElapsed / popDuration);
                float scale;
                if (bt < 0.6f)
                    scale = Mathf.Lerp(0f, 1.2f, bt / 0.6f);
                else
                    scale = Mathf.Lerp(1.2f, 1f, (bt - 0.6f) / 0.4f);

                if (rectTransform != null)
                    rectTransform.localScale = originalScale * scale;
                newRecordUICanvasGroup.alpha = Mathf.Clamp01(bt * 2f); // Fade in over first half
                yield return null;
            }
            if (rectTransform != null)
                rectTransform.localScale = originalScale;
            newRecordUICanvasGroup.alpha = 1f;

            // Start continuous pulsing animation
            newRecordPulseCoroutine = StartCoroutine(PulseNewRecord());
        }

        private IEnumerator PulseNewRecord()
        {
            if (newRecordUICanvasGroup == null) yield break;

            var rectTransform = newRecordUICanvasGroup.GetComponent<RectTransform>();
            if (rectTransform == null) yield break;

            Vector3 baseScale = Vector3.one;
            float time = 0f;

            while (true)
            {
                time += Time.unscaledDeltaTime;

                // Scale pulse: oscillate between 0.95 and 1.12
                float scaleFactor = 1f + 0.085f * Mathf.Sin(time * 5f);
                rectTransform.localScale = baseScale * scaleFactor;

                yield return null;
            }
        }

        private IEnumerator PlayBottomBoardAnimation()
        {
            if (bottomBoardImage == null || bottomBoardFrames == null || bottomBoardFrames.Length == 0)
                yield break;

            bottomBoardImage.gameObject.SetActive(true);
            float frameTime = 1f / 24f; // 24 fps

            for (int i = 0; i < bottomBoardFrames.Length; i++)
            {
                bottomBoardImage.sprite = bottomBoardFrames[i];
                if (i < bottomBoardFrames.Length - 1)
                    yield return new WaitForSecondsRealtime(frameTime);
            }
            // Hold on last frame
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private void OnLevelFailed(int levelNumber, string reason)
        {
            ShowLevelFailedScreen(reason ?? "Level Failed");
        }

        private void ShowLevelFailedScreen(string reason)
        {
            // Hide HUD
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (hudModeOverlay != null)
                hudModeOverlay.SetActive(false);
            if (hudSettingsOverlay != null)
                hudSettingsOverlay.SetActive(false);

            if (levelFailedPanel != null)
            {
                // Update reason text
                if (levelFailedReasonText != null)
                {
                    levelFailedReasonText.text = reason;
                }

                // Reset animated elements
                if (failedBottomBoardImage != null) failedBottomBoardImage.gameObject.SetActive(false);
                if (failedButtonsContainerGO != null) failedButtonsContainerGO.SetActive(false);

                levelFailedPanel.SetActive(true);
                Debug.Log($"[UIManager] Showing level failed screen: {reason}");

                // Start the animation sequence
                if (failedScreenSequence != null)
                    StopCoroutine(failedScreenSequence);
                failedScreenSequence = StartCoroutine(FailedScreenAnimationSequence());
            }
        }

        private IEnumerator FailedScreenAnimationSequence()
        {
            // Brief pause to let the screen appear
            yield return new WaitForSecondsRealtime(0.5f);

            // Play bottom board animation
            if (failedBottomBoardImage != null && failedBottomBoardFrames != null && failedBottomBoardFrames.Length > 0)
            {
                failedBottomBoardImage.gameObject.SetActive(true);
                float frameTime = 1f / 24f;
                for (int i = 0; i < failedBottomBoardFrames.Length; i++)
                {
                    failedBottomBoardImage.sprite = failedBottomBoardFrames[i];
                    if (i < failedBottomBoardFrames.Length - 1)
                        yield return new WaitForSecondsRealtime(frameTime);
                }
            }

            // Show buttons
            if (failedButtonsContainerGO != null) failedButtonsContainerGO.SetActive(true);

            failedScreenSequence = null;
        }

        private void StopFailedScreenSequence()
        {
            if (failedScreenSequence != null)
            {
                StopCoroutine(failedScreenSequence);
                failedScreenSequence = null;
            }
        }

        private void OnBackToLevelsFromFailedClicked()
        {
            Debug.Log("[UIManager] Back to Levels from failed clicked");
            AudioManager.Instance?.PlayButtonClick();
            StopFailedScreenSequence();

            TransitionManager.Instance?.FadeOut(() =>
            {
                levelFailedPanel?.SetActive(false);

                // Set state back to level selection
                GameManager.Instance?.SetState(GameState.LevelSelection);

                ShowLevelSelect();

                // Clear the current level
                LevelManager.Instance?.ClearLevel();

                TransitionManager.Instance?.FadeIn();
            });
        }

        private void OnRetryFromFailedClicked()
        {
            Debug.Log("[UIManager] Retry from failed clicked");
            AudioManager.Instance?.PlayButtonClick();
            StopFailedScreenSequence();
            levelFailedPanel?.SetActive(false);

            // Ensure state is Playing
            GameManager.Instance?.SetState(GameState.Playing);

            // Fade out, restart level, then fade in
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.FadeOut(() =>
                {
                    LevelManager.Instance?.RestartLevel();
                    TransitionManager.Instance.FadeIn();
                });
            }
            else
            {
                LevelManager.Instance?.RestartLevel();
            }
        }

        private void OnLevelRestarted()
        {
            StopLevelCompleteSequence();
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
                animatedLevelComplete?.Hide();
                ResetStarAnimations();
                StopLevelCompleteText();
            }
            if (levelFailedPanel != null)
                levelFailedPanel.SetActive(false);

            // Clean up mascot animator
            CleanupMascotAnimator();

            UpdateMoveDisplay(0);
            UpdateMatchDisplay(0);

            // Reset timer display (will be shown again via OnTimerUpdated if level has timer)
            if (timerContainer != null)
            {
                timerContainer.SetActive(false);
            }
            if (timerText != null)
            {
                timerText.color = Color.white;
            }
            if (overlayTimerText != null)
            {
                overlayTimerText.text = "0:00.00";
                overlayTimerText.color = Color.white;
            }
        }

        private void UpdateLevelCompleteMascot(int stars)
        {
            if (levelCompleteMascotImage == null) return;

            string worldId = GameManager.Instance?.CurrentWorldId;
            var world = WorldProgressionManager.Instance?.GetWorldData(worldId);

            // Check if we have a victory animation for this world (all modes, regardless of stars)
            string animationName = MascotAnimator.GetVictoryAnimationName(worldId);
            string worldFolder = MascotAnimator.GetWorldFolderName(worldId);

            Debug.Log($"[UIManager] UpdateLevelCompleteMascot: world={worldId}, folder={worldFolder}, anim={animationName}, stars={stars}");

            if (!string.IsNullOrEmpty(animationName) && !string.IsNullOrEmpty(worldFolder))
            {
                // Clean up existing animator
                CleanupMascotAnimator();

                // Try to play animated mascot
                levelCompleteMascotAnimator = levelCompleteMascotImage.gameObject.AddComponent<MascotAnimator>();
                levelCompleteMascotAnimator.FrameRate = MascotAnimator.GetVictoryAnimationFPS(worldId);

                if (levelCompleteMascotAnimator.LoadFrames(worldFolder, animationName))
                {
                    Debug.Log($"[UIManager] Playing mascot animation with {levelCompleteMascotAnimator.Duration}s duration");
                    levelCompleteMascotImage.enabled = true;
                    levelCompleteMascotAnimator.Play();
                    return;
                }
                else
                {
                    Debug.LogWarning("[UIManager] Failed to load mascot animation frames");
                    // Clean up failed animator
                    Destroy(levelCompleteMascotAnimator);
                    levelCompleteMascotAnimator = null;
                }
            }

            // Fallback to static sprite
            if (world != null)
            {
                levelCompleteMascotImage.sprite = (stars >= 2) ? world.mascotHappy : (world.mascotHappy ?? world.mascotIdle);
                levelCompleteMascotImage.enabled = levelCompleteMascotImage.sprite != null;
                Debug.Log($"[UIManager] Using static mascot sprite: {(levelCompleteMascotImage.sprite != null ? levelCompleteMascotImage.sprite.name : "null")}");
            }
            else
            {
                levelCompleteMascotImage.enabled = false;
                Debug.LogWarning($"[UIManager] No world data found for worldId: {worldId}");
            }
        }

        private void CleanupMascotAnimator()
        {
            if (levelCompleteMascotAnimator != null)
            {
                levelCompleteMascotAnimator.Stop();
                Destroy(levelCompleteMascotAnimator);
                levelCompleteMascotAnimator = null;
            }
        }

        private void LoadLevelCompleteTextFrames()
        {
            if (cachedLevelCompleteTextFrames != null) return;

            var textures = Resources.LoadAll<Texture2D>("Sprites/UI/LevelComplete/LevelCompleteText");
            if (textures.Length == 0)
            {
                Debug.LogWarning("[UIManager] No level complete text frames found");
                return;
            }

            System.Array.Sort(textures, (a, b) => a.name.CompareTo(b.name));

            cachedLevelCompleteTextFrames = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                cachedLevelCompleteTextFrames[i] = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
            Debug.Log($"[UIManager] Loaded {cachedLevelCompleteTextFrames.Length} level complete text frames");
        }

        private Coroutine levelCompleteTextCoroutine;

        private void PlayLevelCompleteText()
        {
            if (cachedLevelCompleteTextFrames == null || cachedLevelCompleteTextFrames.Length == 0 || levelCompleteTextImage == null)
                return;

            StopLevelCompleteText();
            levelCompleteTextImage.sprite = cachedLevelCompleteTextFrames[0];
            levelCompleteTextImage.enabled = true;
            levelCompleteTextCoroutine = StartCoroutine(AnimateLevelCompleteText());
        }

        private IEnumerator AnimateLevelCompleteText()
        {
            int frame = 0;
            float timer = 0f;
            float frameTime = 1f / levelCompleteTextFrameRate;

            while (frame < cachedLevelCompleteTextFrames.Length - 1)
            {
                timer += Time.unscaledDeltaTime;
                if (timer >= frameTime)
                {
                    timer -= frameTime;
                    frame++;
                    levelCompleteTextImage.sprite = cachedLevelCompleteTextFrames[frame];
                }
                yield return null;
            }

            // Hold on last frame until stopped
            levelCompleteTextCoroutine = null;
        }

        private void StopLevelCompleteText()
        {
            if (levelCompleteTextCoroutine != null)
            {
                StopCoroutine(levelCompleteTextCoroutine);
                levelCompleteTextCoroutine = null;
            }
            if (levelCompleteTextImage != null)
                levelCompleteTextImage.enabled = false;
        }

        private void OnTimerUpdated(float timeRemaining)
        {
            UpdateTimerDisplay(timeRemaining);

            // Show timer container if level has timer
            if (timerContainer != null && !timerContainer.activeSelf && LevelManager.Instance?.IsTimerActive == true)
            {
                timerContainer.SetActive(true);
            }
        }

        private void OnTimerFrozen(bool isFrozen)
        {
            // Visual feedback for frozen timer (e.g., color change)
            if (timerText != null)
                timerText.color = isFrozen ? Color.cyan : Color.white;
            // Overlay timer: use cyan for frozen, white for normal
            if (overlayTimerText != null)
                overlayTimerText.color = isFrozen ? Color.cyan : Color.white;
        }

        private void UpdateTimerDisplay(float timeRemaining)
        {
            // Format as M:SS.CC
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            int centiseconds = Mathf.FloorToInt((timeRemaining % 1f) * 100f);
            string formatted = $"{minutes}:{seconds:D2}.{centiseconds:D2}";

            if (timerText != null)
                timerText.text = formatted;

            // Update overlay timer
            if (overlayTimerText != null)
                overlayTimerText.text = formatted;

            // Change color when time is low (under 10 seconds)
            if (timeRemaining <= 10f && !LevelManager.Instance?.IsTimerFrozen == true)
            {
                // Flash red when low on time
                float flash = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
                Color flashColor = Color.Lerp(Color.red, Color.white, flash);
                if (timerText != null) timerText.color = flashColor;
                // On overlay, flash between red and white
                Color overlayFlash = Color.Lerp(Color.red, Color.white, flash);
                if (overlayTimerText != null) overlayTimerText.color = overlayFlash;
            }
            else if (!LevelManager.Instance?.IsTimerFrozen == true)
            {
                if (timerText != null) timerText.color = Color.white;
                if (overlayTimerText != null) overlayTimerText.color = Color.white;
            }
        }

        private void UpdateMoveDisplay(int moves)
        {
            if (moveCountText != null)
                moveCountText.text = moves.ToString();
            if (overlayMovesText != null)
                overlayMovesText.text = moves.ToString();
        }

        private void UpdateMatchDisplay(int matches)
        {
            if (matchCountText != null)
                matchCountText.text = matches.ToString();
        }

        private void UpdateStarPreview(int moves)
        {
            if (starImages == null || LevelManager.Instance?.CurrentLevel == null) return;

            var thresholds = LevelManager.Instance.CurrentLevel.star_move_thresholds;
            if (thresholds == null || thresholds.Length < 3) return;

            int potentialStars = 3;
            if (moves > thresholds[0]) potentialStars = 2;
            if (moves > thresholds[1]) potentialStars = 1;
            if (moves > thresholds[2]) potentialStars = 0;

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].color = i < potentialStars ? Color.yellow : Color.gray;
                }
            }
        }

        private void OnBackToLevelsClicked()
        {
            Debug.Log("[UIManager] Back to Levels clicked");

            TransitionManager.Instance?.FadeOut(() =>
            {
                levelCompletePanel?.SetActive(false);
                animatedLevelComplete?.Hide();

                // Set state back to level selection
                GameManager.Instance?.SetState(GameState.LevelSelection);

                ShowLevelSelect();

                // Clear the current level
                LevelManager.Instance?.ClearLevel();

                TransitionManager.Instance?.FadeIn();
            });
        }

        private void OnReplayClicked()
        {
            Debug.Log("[UIManager] Replay clicked");

            TransitionManager.Instance?.FadeOut(() =>
            {
                levelCompletePanel?.SetActive(false);
                animatedLevelComplete?.Hide();

                // Ensure state is Playing
                GameManager.Instance?.SetState(GameState.Playing);

                LevelManager.Instance?.RestartLevel();

                TransitionManager.Instance?.FadeIn();
            });
        }

        private void OnNextLevelFromCompleteClicked()
        {
            Debug.Log("[UIManager] Next Level clicked");

            TransitionManager.Instance?.FadeOut(() =>
            {
                levelCompletePanel?.SetActive(false);
                animatedLevelComplete?.Hide();

                // Load next level directly
                int currentLevel = GameManager.Instance?.CurrentLevelNumber ?? 1;
                string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
                int nextLevel = currentLevel + 1;

                // Update GameManager state
                if (GameManager.Instance != null)
                {
                    var field = typeof(GameManager).GetField("currentLevelNumber",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(GameManager.Instance, nextLevel);

                    // Ensure state is Playing
                    GameManager.Instance.SetState(GameState.Playing);
                }

                // Load the level
                LevelManager.Instance?.LoadLevel(worldId, nextLevel);

                TransitionManager.Instance?.FadeIn();
            });
        }

        #region Achievement Notification

        private void CreateAchievementNotificationPanel()
        {
            // ==========================================
            // NOTIFICATION BANNER (clickable)
            // ==========================================
            achievementNotificationPanel = new GameObject("Achievement Notification Panel");
            achievementNotificationPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = achievementNotificationPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, 200); // Start off screen (will animate in)
            rect.sizeDelta = new Vector2(700, 180);

            var canvasGroup = achievementNotificationPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Background with gradient-like appearance
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(achievementNotificationPanel.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            achievementTierBg = bgGO.AddComponent<Image>();
            achievementTierBg.color = new Color(0.2f, 0.15f, 0.4f, 0.95f); // Default purple

            // Make the whole panel clickable
            var notificationBtn = achievementNotificationPanel.AddComponent<Button>();
            notificationBtn.targetGraphic = achievementTierBg;
            notificationBtn.onClick.AddListener(OnAchievementNotificationClicked);

            // Gold border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(achievementNotificationPanel.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderOutline = borderGO.AddComponent<Outline>();
            borderOutline.effectColor = new Color(1f, 0.85f, 0.3f, 1f); // Gold
            borderOutline.effectDistance = new Vector2(4, -4);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = Color.clear;
            borderImg.raycastTarget = false;

            // Content layout
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(achievementNotificationPanel.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 15);
            contentRect.offsetMax = new Vector2(-20, -15);

            var contentLayout = contentGO.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 20;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.padding = new RectOffset(10, 10, 5, 5);

            // Trophy/Icon placeholder
            var iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(contentGO.transform, false);
            var iconContainerLE = iconContainer.AddComponent<LayoutElement>();
            iconContainerLE.preferredWidth = 100;
            iconContainerLE.preferredHeight = 100;

            var iconBg = new GameObject("IconBg");
            iconBg.transform.SetParent(iconContainer.transform, false);
            var iconBgRect = iconBg.AddComponent<RectTransform>();
            iconBgRect.anchorMin = Vector2.zero;
            iconBgRect.anchorMax = Vector2.one;
            iconBgRect.offsetMin = Vector2.zero;
            iconBgRect.offsetMax = Vector2.zero;
            var iconBgImg = iconBg.AddComponent<Image>();
            iconBgImg.color = new Color(1f, 0.85f, 0.3f, 0.3f); // Gold tint
            iconBgImg.raycastTarget = false;

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(iconContainer.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            achievementIconImage = iconGO.AddComponent<Image>();
            achievementIconImage.raycastTarget = false;
            // Load trophy sprite or use default
            var trophySprite = Resources.Load<Sprite>("Sprites/UI/Icons/trophy");
            if (trophySprite != null)
                achievementIconImage.sprite = trophySprite;
            else
                achievementIconImage.color = new Color(1f, 0.85f, 0.3f, 1f); // Gold color as fallback

            // Text container
            var textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(contentGO.transform, false);
            var textContainerLE = textContainer.AddComponent<LayoutElement>();
            textContainerLE.flexibleWidth = 1;
            textContainerLE.preferredHeight = 140;

            var textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 5;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            // "Achievement Unlocked!" header
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(textContainer.transform, false);
            var headerTMP = headerGO.AddComponent<TextMeshProUGUI>();
            headerTMP.text = "Achievement Unlocked! (Tap for details)";
            headerTMP.fontSize = 22;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.color = new Color(1f, 0.85f, 0.3f, 1f); // Gold
            headerTMP.alignment = TextAlignmentOptions.MidlineLeft;
            headerTMP.raycastTarget = false;
            var headerLE = headerGO.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 30;

            // Achievement name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(textContainer.transform, false);
            achievementNameText = nameGO.AddComponent<TextMeshProUGUI>();
            achievementNameText.text = "Achievement Name";
            achievementNameText.fontSize = 32;
            achievementNameText.fontStyle = FontStyles.Bold;
            achievementNameText.color = Color.white;
            achievementNameText.alignment = TextAlignmentOptions.MidlineLeft;
            achievementNameText.raycastTarget = false;
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 40;

            // Achievement description
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(textContainer.transform, false);
            achievementDescText = descGO.AddComponent<TextMeshProUGUI>();
            achievementDescText.text = "Achievement description goes here";
            achievementDescText.fontSize = 20;
            achievementDescText.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            achievementDescText.alignment = TextAlignmentOptions.MidlineLeft;
            achievementDescText.raycastTarget = false;
            var descLE = descGO.AddComponent<LayoutElement>();
            descLE.preferredHeight = 25;

            // Reward text
            var rewardGO = new GameObject("Reward");
            rewardGO.transform.SetParent(textContainer.transform, false);
            achievementRewardText = rewardGO.AddComponent<TextMeshProUGUI>();
            achievementRewardText.text = "+50 Coins";
            achievementRewardText.fontSize = 22;
            achievementRewardText.fontStyle = FontStyles.Bold;
            achievementRewardText.color = new Color(0.5f, 1f, 0.5f, 1f); // Green
            achievementRewardText.alignment = TextAlignmentOptions.MidlineLeft;
            achievementRewardText.raycastTarget = false;
            var rewardLE = rewardGO.AddComponent<LayoutElement>();
            rewardLE.preferredHeight = 28;

            // ==========================================
            // DETAIL PANEL (shown when notification clicked)
            // ==========================================
            CreateAchievementDetailPanel();

            Debug.Log("[UIManager] Achievement notification panel created");
        }

        private void CreateAchievementDetailPanel()
        {
            achievementDetailPanel = new GameObject("Achievement Detail Panel");
            achievementDetailPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = achievementDetailPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Dark overlay background
            var overlayImg = achievementDetailPanel.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.85f);

            // Center card
            var cardGO = new GameObject("Card");
            cardGO.transform.SetParent(achievementDetailPanel.transform, false);
            var cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(800, 500);

            achievementDetailTierBg = cardGO.AddComponent<Image>();
            achievementDetailTierBg.color = new Color(0.3f, 0.25f, 0.5f, 1f);

            // Gold border for card
            var cardBorder = cardGO.AddComponent<Outline>();
            cardBorder.effectColor = new Color(1f, 0.85f, 0.3f, 1f);
            cardBorder.effectDistance = new Vector2(5, -5);

            // Card content layout
            var cardLayout = cardGO.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(40, 40, 30, 30);
            cardLayout.spacing = 20;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Header "ACHIEVEMENT UNLOCKED"
            var detailHeaderGO = new GameObject("Header");
            detailHeaderGO.transform.SetParent(cardGO.transform, false);
            var detailHeaderTMP = detailHeaderGO.AddComponent<TextMeshProUGUI>();
            detailHeaderTMP.text = "ACHIEVEMENT UNLOCKED";
            detailHeaderTMP.fontSize = 36;
            detailHeaderTMP.fontStyle = FontStyles.Bold;
            detailHeaderTMP.color = new Color(1f, 0.85f, 0.3f, 1f);
            detailHeaderTMP.alignment = TextAlignmentOptions.Center;
            var detailHeaderLE = detailHeaderGO.AddComponent<LayoutElement>();
            detailHeaderLE.preferredHeight = 50;

            // Tier label
            var tierGO = new GameObject("Tier");
            tierGO.transform.SetParent(cardGO.transform, false);
            achievementDetailTierText = tierGO.AddComponent<TextMeshProUGUI>();
            achievementDetailTierText.text = "GOLD";
            achievementDetailTierText.fontSize = 24;
            achievementDetailTierText.fontStyle = FontStyles.Bold;
            achievementDetailTierText.color = new Color(1f, 0.85f, 0.3f, 1f);
            achievementDetailTierText.alignment = TextAlignmentOptions.Center;
            var tierLE = tierGO.AddComponent<LayoutElement>();
            tierLE.preferredHeight = 35;

            // Achievement name
            var detailNameGO = new GameObject("Name");
            detailNameGO.transform.SetParent(cardGO.transform, false);
            achievementDetailNameText = detailNameGO.AddComponent<TextMeshProUGUI>();
            achievementDetailNameText.text = "Achievement Name";
            achievementDetailNameText.fontSize = 48;
            achievementDetailNameText.fontStyle = FontStyles.Bold;
            achievementDetailNameText.color = Color.white;
            achievementDetailNameText.alignment = TextAlignmentOptions.Center;
            var detailNameLE = detailNameGO.AddComponent<LayoutElement>();
            detailNameLE.preferredHeight = 70;

            // Description
            var detailDescGO = new GameObject("Description");
            detailDescGO.transform.SetParent(cardGO.transform, false);
            achievementDetailDescText = detailDescGO.AddComponent<TextMeshProUGUI>();
            achievementDetailDescText.text = "Description of the achievement goes here.";
            achievementDetailDescText.fontSize = 28;
            achievementDetailDescText.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            achievementDetailDescText.alignment = TextAlignmentOptions.Center;
            var detailDescLE = detailDescGO.AddComponent<LayoutElement>();
            detailDescLE.preferredHeight = 50;

            // Rewards section header
            var rewardsHeaderGO = new GameObject("RewardsHeader");
            rewardsHeaderGO.transform.SetParent(cardGO.transform, false);
            var rewardsHeaderTMP = rewardsHeaderGO.AddComponent<TextMeshProUGUI>();
            rewardsHeaderTMP.text = "REWARDS";
            rewardsHeaderTMP.fontSize = 24;
            rewardsHeaderTMP.fontStyle = FontStyles.Bold;
            rewardsHeaderTMP.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            rewardsHeaderTMP.alignment = TextAlignmentOptions.Center;
            var rewardsHeaderLE = rewardsHeaderGO.AddComponent<LayoutElement>();
            rewardsHeaderLE.preferredHeight = 35;

            // Rewards text
            var detailRewardGO = new GameObject("Reward");
            detailRewardGO.transform.SetParent(cardGO.transform, false);
            achievementDetailRewardText = detailRewardGO.AddComponent<TextMeshProUGUI>();
            achievementDetailRewardText.text = "+50 Coins";
            achievementDetailRewardText.fontSize = 36;
            achievementDetailRewardText.fontStyle = FontStyles.Bold;
            achievementDetailRewardText.color = new Color(0.5f, 1f, 0.5f, 1f);
            achievementDetailRewardText.alignment = TextAlignmentOptions.Center;
            var detailRewardLE = detailRewardGO.AddComponent<LayoutElement>();
            detailRewardLE.preferredHeight = 50;

            // Dismiss button
            var dismissBtnGO = new GameObject("DismissButton");
            dismissBtnGO.transform.SetParent(cardGO.transform, false);
            var dismissBtnLE = dismissBtnGO.AddComponent<LayoutElement>();
            dismissBtnLE.preferredWidth = 250;
            dismissBtnLE.preferredHeight = 70;

            var dismissBtnImg = dismissBtnGO.AddComponent<Image>();
            dismissBtnImg.color = new Color(0.3f, 0.6f, 0.9f, 1f);

            var dismissBtn = dismissBtnGO.AddComponent<Button>();
            dismissBtn.targetGraphic = dismissBtnImg;
            dismissBtn.onClick.AddListener(OnAchievementDetailDismissClicked);

            var dismissTextGO = new GameObject("Text");
            dismissTextGO.transform.SetParent(dismissBtnGO.transform, false);
            var dismissTextRect = dismissTextGO.AddComponent<RectTransform>();
            dismissTextRect.anchorMin = Vector2.zero;
            dismissTextRect.anchorMax = Vector2.one;
            dismissTextRect.offsetMin = Vector2.zero;
            dismissTextRect.offsetMax = Vector2.zero;
            var dismissTextTMP = dismissTextGO.AddComponent<TextMeshProUGUI>();
            dismissTextTMP.text = "DISMISS";
            dismissTextTMP.fontSize = 32;
            dismissTextTMP.fontStyle = FontStyles.Bold;
            dismissTextTMP.color = Color.white;
            dismissTextTMP.alignment = TextAlignmentOptions.Center;

            achievementDetailPanel.SetActive(false);
        }

        private void OnAchievementNotificationClicked()
        {
            if (currentAchievement == null || isShowingAchievementDetail)
                return;

            AudioManager.Instance?.PlayButtonClick();

            // Stop the auto-dismiss coroutine
            if (achievementCoroutine != null)
            {
                StopCoroutine(achievementCoroutine);
                achievementCoroutine = null;
            }

            // Hide notification banner
            achievementNotificationPanel.SetActive(false);

            // Show detail panel
            ShowAchievementDetail(currentAchievement);
        }

        private void ShowAchievementDetail(Achievement achievement)
        {
            isShowingAchievementDetail = true;

            // Update detail panel content
            if (achievementDetailNameText != null)
                achievementDetailNameText.text = achievement.name;
            if (achievementDetailDescText != null)
                achievementDetailDescText.text = achievement.description;

            // Tier text and color
            if (achievementDetailTierText != null)
            {
                achievementDetailTierText.text = achievement.tier.ToString().ToUpper();
                achievementDetailTierText.color = achievement.tier switch
                {
                    AchievementTier.Bronze => new Color(0.8f, 0.5f, 0.3f, 1f),
                    AchievementTier.Silver => new Color(0.75f, 0.75f, 0.8f, 1f),
                    AchievementTier.Gold => new Color(1f, 0.85f, 0.3f, 1f),
                    AchievementTier.Platinum => new Color(0.6f, 0.9f, 0.95f, 1f),
                    _ => Color.white
                };
            }

            // Background color
            if (achievementDetailTierBg != null)
            {
                achievementDetailTierBg.color = achievement.tier switch
                {
                    AchievementTier.Bronze => new Color(0.4f, 0.28f, 0.18f, 1f),
                    AchievementTier.Silver => new Color(0.35f, 0.35f, 0.4f, 1f),
                    AchievementTier.Gold => new Color(0.4f, 0.32f, 0.12f, 1f),
                    AchievementTier.Platinum => new Color(0.2f, 0.4f, 0.45f, 1f),
                    _ => new Color(0.3f, 0.25f, 0.5f, 1f)
                };
            }

            // Format rewards
            if (achievementDetailRewardText != null && achievement.rewards != null && achievement.rewards.Length > 0)
            {
                var rewardStrings = new List<string>();
                foreach (var reward in achievement.rewards)
                {
                    string rewardStr = reward.type switch
                    {
                        RewardType.Coins => $"+{reward.amount} Coins",
                        RewardType.UndoToken => $"+{reward.amount} Undo Token(s)",
                        RewardType.SkipToken => $"+{reward.amount} Skip Token(s)",
                        RewardType.UnlockKey => $"+{reward.amount} Unlock Key(s)",
                        RewardType.FreezeToken => $"+{reward.amount} Freeze Token(s)",
                        RewardType.TimerFreeze => $"+{reward.amount} Timer Freeze(s)",
                        RewardType.Trophy => "Trophy Earned!",
                        _ => $"+{reward.amount}"
                    };
                    rewardStrings.Add(rewardStr);
                }
                achievementDetailRewardText.text = string.Join("\n", rewardStrings);
            }

            achievementDetailPanel.SetActive(true);
        }

        private void OnAchievementDetailDismissClicked()
        {
            AudioManager.Instance?.PlayButtonClick();

            // Hide both panels immediately
            achievementDetailPanel.SetActive(false);
            achievementNotificationPanel.SetActive(false);

            isShowingAchievementDetail = false;
            isShowingAchievement = false;
            currentAchievement = null;

            // Reset notification position for next time
            var rect = achievementNotificationPanel.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 200);
            var canvasGroup = achievementNotificationPanel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Show next achievement if queued
            StartCoroutine(ShowNextAchievementDelayed());
        }

        private IEnumerator ShowNextAchievementDelayed()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            ShowNextAchievement();
        }

        private void OnAchievementUnlocked(Achievement achievement)
        {
            achievementQueue.Enqueue(achievement);

            if (!isShowingAchievement)
            {
                ShowNextAchievement();
            }
        }

        private void ShowNextAchievement()
        {
            if (achievementQueue.Count == 0)
            {
                isShowingAchievement = false;
                currentAchievement = null;
                return;
            }

            isShowingAchievement = true;
            currentAchievement = achievementQueue.Dequeue();

            // Update UI
            if (achievementNameText != null)
                achievementNameText.text = currentAchievement.name;
            if (achievementDescText != null)
                achievementDescText.text = currentAchievement.description;

            // Format reward text
            if (achievementRewardText != null && currentAchievement.rewards != null && currentAchievement.rewards.Length > 0)
            {
                var rewardStrings = new List<string>();
                foreach (var reward in currentAchievement.rewards)
                {
                    string rewardStr = reward.type switch
                    {
                        RewardType.Coins => $"+{reward.amount} Coins",
                        RewardType.UndoToken => $"+{reward.amount} Undo",
                        RewardType.SkipToken => $"+{reward.amount} Skip",
                        RewardType.UnlockKey => $"+{reward.amount} Key",
                        RewardType.FreezeToken => $"+{reward.amount} Freeze",
                        RewardType.TimerFreeze => $"+{reward.amount} Timer Freeze",
                        RewardType.Trophy => "Trophy Earned!",
                        _ => $"+{reward.amount}"
                    };
                    rewardStrings.Add(rewardStr);
                }
                achievementRewardText.text = string.Join(" | ", rewardStrings);
            }

            // Set tier color
            if (achievementTierBg != null)
            {
                achievementTierBg.color = currentAchievement.tier switch
                {
                    AchievementTier.Bronze => new Color(0.5f, 0.35f, 0.2f, 0.95f),
                    AchievementTier.Silver => new Color(0.45f, 0.45f, 0.5f, 0.95f),
                    AchievementTier.Gold => new Color(0.5f, 0.4f, 0.15f, 0.95f),
                    AchievementTier.Platinum => new Color(0.3f, 0.5f, 0.55f, 0.95f),
                    _ => new Color(0.2f, 0.15f, 0.4f, 0.95f)
                };
            }

            // Show notification
            if (achievementCoroutine != null)
                StopCoroutine(achievementCoroutine);
            achievementCoroutine = StartCoroutine(ShowAchievementAnimation());
        }

        private IEnumerator ShowAchievementAnimation()
        {
            if (achievementNotificationPanel == null)
            {
                isShowingAchievement = false;
                yield break;
            }

            achievementNotificationPanel.SetActive(true);
            var rect = achievementNotificationPanel.GetComponent<RectTransform>();
            var canvasGroup = achievementNotificationPanel.GetComponent<CanvasGroup>();

            // Play achievement sound
            AudioManager.Instance?.PlayAchievementSound();

            // Slide in from top
            float duration = 0.4f;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0, 200);
            Vector2 endPos = new Vector2(0, -20);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                canvasGroup.alpha = t;
                yield return null;
            }

            rect.anchoredPosition = endPos;
            canvasGroup.alpha = 1f;

            // Stay visible for a few seconds (user can tap to see details)
            yield return new WaitForSecondsRealtime(4f);

            // Auto-dismiss: slide out
            elapsed = 0f;
            duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(endPos, startPos, t);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            achievementNotificationPanel.SetActive(false);
            rect.anchoredPosition = startPos;
            canvasGroup.alpha = 0f;

            isShowingAchievement = false;
            currentAchievement = null;

            // Show next achievement if queued
            yield return new WaitForSecondsRealtime(0.3f);
            ShowNextAchievement();
        }

        #endregion

        #region Achievements Screen

        private void CreateAchievementsPanel()
        {
            achievementsPanel = new GameObject("Achievements Panel");
            achievementsPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = achievementsPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Override sorting so achievements render above everything
            var achieveCanvas = achievementsPanel.AddComponent<Canvas>();
            achieveCanvas.overrideSorting = true;
            achieveCanvas.sortingOrder = 5200;
            achievementsPanel.AddComponent<GraphicRaycaster>();

            // CanvasGroup for fade
            achievementsPanel.AddComponent<CanvasGroup>();

            // Cache tab sprites
            achvTabSprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_tab");
            achvTabPressedSprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_tab_pressed");

            // ============================================
            // Layer 0: Dark background (95% opacity to reduce distraction)
            // ============================================
            var dimBg = new GameObject("DimBg");
            dimBg.transform.SetParent(achievementsPanel.transform, false);
            var dimBgRect = dimBg.AddComponent<RectTransform>();
            dimBgRect.anchorMin = Vector2.zero;
            dimBgRect.anchorMax = Vector2.one;
            dimBgRect.offsetMin = Vector2.zero;
            dimBgRect.offsetMax = Vector2.zero;
            var dimBgImg = dimBg.AddComponent<Image>();
            dimBgImg.color = new Color(0, 0, 0, 0.95f);
            dimBgImg.raycastTarget = true;

            // ============================================
            // Layer 1: Board (wooden backdrop)
            // ============================================
            var boardGO = new GameObject("Board");
            boardGO.transform.SetParent(achievementsPanel.transform, false);
            var boardRect = boardGO.AddComponent<RectTransform>();
            boardRect.anchorMin = Vector2.zero;
            boardRect.anchorMax = Vector2.one;
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;
            var boardImg = boardGO.AddComponent<Image>();
            boardImg.sprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_board");
            boardImg.preserveAspect = true;
            boardImg.raycastTarget = false;

            // ============================================
            // Layer 2: Yellow background panel
            // ============================================
            var bgYellowGO = new GameObject("BgYellow");
            bgYellowGO.transform.SetParent(achievementsPanel.transform, false);
            var bgYellowRect = bgYellowGO.AddComponent<RectTransform>();
            bgYellowRect.anchorMin = Vector2.zero;
            bgYellowRect.anchorMax = Vector2.one;
            bgYellowRect.offsetMin = Vector2.zero;
            bgYellowRect.offsetMax = Vector2.zero;
            var bgYellowImg = bgYellowGO.AddComponent<Image>();
            bgYellowImg.sprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_bg_yellow");
            bgYellowImg.preserveAspect = true;
            bgYellowImg.raycastTarget = false;

            // ============================================
            // Layer 3: Scroll Area (achievement cards)
            // Positioned within the yellow bg bounds: x(132-938), y(520-1802) in 1080x1920
            // With extra left padding (~100px) so cards don't hide behind tabs
            // ============================================
            var scrollAreaGO = new GameObject("ScrollArea");
            scrollAreaGO.transform.SetParent(achievementsPanel.transform, false);
            var scrollAreaRect = scrollAreaGO.AddComponent<RectTransform>();
            // left=232/1080≈0.215, right=938/1080≈0.869
            // bottom=1-1802/1920≈0.061, top=1-520/1920≈0.729
            scrollAreaRect.anchorMin = new Vector2(0.215f, 0.061f);
            scrollAreaRect.anchorMax = new Vector2(0.869f, 0.729f);
            scrollAreaRect.offsetMin = Vector2.zero;
            scrollAreaRect.offsetMax = Vector2.zero;

            var scrollRect = scrollAreaGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 50f;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollAreaGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRect;

            // List content
            var listContentGO = new GameObject("ListContent");
            listContentGO.transform.SetParent(viewportGO.transform, false);
            var listContentRect = listContentGO.AddComponent<RectTransform>();
            listContentRect.anchorMin = new Vector2(0, 1);
            listContentRect.anchorMax = new Vector2(1, 1);
            listContentRect.pivot = new Vector2(0.5f, 1);
            listContentRect.anchoredPosition = Vector2.zero;
            listContentRect.sizeDelta = new Vector2(0, 0);

            achievementsListContent = listContentGO.transform;

            var contentSizeFitter = listContentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vertLayout = listContentGO.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 15;
            vertLayout.padding = new RectOffset(10, 10, 50, 10);
            vertLayout.childAlignment = TextAnchor.UpperCenter;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = false;

            scrollRect.content = listContentRect;

            // ============================================
            // Layer 4: Tab Container (left side, vertical)
            // Positioned at x≈10-228, y≈550-1290 in 1080x1920
            // ============================================
            var tabContainerGO = new GameObject("TabContainer");
            tabContainerGO.transform.SetParent(achievementsPanel.transform, false);
            var tabContainerRect = tabContainerGO.AddComponent<RectTransform>();
            // x: 0 (touch screen edge) to 251/1080≈0.232 (15% wider than original 218px)
            // y: shifted down 20px from original: bottom=1-1310/1920≈0.318, top=1-570/1920≈0.704
            tabContainerRect.anchorMin = new Vector2(0f, 0.318f);
            tabContainerRect.anchorMax = new Vector2(0.232f, 0.704f);
            tabContainerRect.offsetMin = Vector2.zero;
            tabContainerRect.offsetMax = Vector2.zero;

            var tabLayout = tabContainerGO.AddComponent<VerticalLayoutGroup>();
            tabLayout.spacing = 0;
            tabLayout.padding = new RectOffset(0, 0, 0, 0);
            tabLayout.childAlignment = TextAnchor.UpperCenter;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = false;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = false;

            achievementTabButtons.Clear();
            achievementTabSpriteImages.Clear();
            achievementTabsContainer = tabContainerGO.transform;

            // ============================================
            // Layer 5: Title Bar (metallic bar with world name)
            // ============================================
            var titleBarGO = new GameObject("TitleBar");
            titleBarGO.transform.SetParent(achievementsPanel.transform, false);
            var titleBarRect = titleBarGO.AddComponent<RectTransform>();
            titleBarRect.anchorMin = Vector2.zero;
            titleBarRect.anchorMax = Vector2.one;
            titleBarRect.offsetMin = Vector2.zero;
            titleBarRect.offsetMax = Vector2.zero;
            var titleBarImg = titleBarGO.AddComponent<Image>();
            titleBarImg.sprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_title_bar");
            titleBarImg.preserveAspect = true;
            titleBarImg.raycastTarget = false;

            // Title bar text - centered within the bar content area x(247-897), y(302-494)
            var titleBarTextGO = new GameObject("TitleBarText");
            titleBarTextGO.transform.SetParent(titleBarGO.transform, false);
            var titleBarTextRect = titleBarTextGO.AddComponent<RectTransform>();
            // x: 320/1080≈0.296, 897/1080≈0.831 (shifted right to leave room for icon)
            // y: 1-494/1920≈0.743, 1-302/1920≈0.843
            titleBarTextRect.anchorMin = new Vector2(0.296f, 0.743f);
            titleBarTextRect.anchorMax = new Vector2(0.831f, 0.843f);
            titleBarTextRect.offsetMin = Vector2.zero;
            titleBarTextRect.offsetMax = Vector2.zero;

            achievementTitleBarText = titleBarTextGO.AddComponent<TextMeshProUGUI>();
            achievementTitleBarText.text = "Achievement\nPoints";
            achievementTitleBarText.fontSize = 48;
            achievementTitleBarText.fontStyle = FontStyles.Bold;
            achievementTitleBarText.alignment = TextAlignmentOptions.Center;
            achievementTitleBarText.color = Color.white;
            achievementTitleBarText.enableAutoSizing = true;
            achievementTitleBarText.fontSizeMin = 28;
            achievementTitleBarText.fontSizeMax = 48;
            achievementTitleBarText.lineSpacing = -30f;
            if (FontManager.ExtraBold != null)
                achievementTitleBarText.font = FontManager.ExtraBold;

            // Title bar text shadow (stronger black, larger offset)
            var titleBarShadowGO = new GameObject("TitleBarShadow");
            titleBarShadowGO.transform.SetParent(titleBarGO.transform, false);
            var titleBarShadowRect = titleBarShadowGO.AddComponent<RectTransform>();
            titleBarShadowRect.anchorMin = new Vector2(0.296f, 0.743f);
            titleBarShadowRect.anchorMax = new Vector2(0.831f, 0.843f);
            titleBarShadowRect.offsetMin = new Vector2(3, -3);
            titleBarShadowRect.offsetMax = new Vector2(3, -3);
            var shadowText = titleBarShadowGO.AddComponent<TextMeshProUGUI>();
            shadowText.text = "Achievement\nPoints";
            shadowText.fontSize = 48;
            shadowText.fontStyle = FontStyles.Bold;
            shadowText.alignment = TextAlignmentOptions.Center;
            shadowText.color = new Color(0, 0, 0, 0.85f);
            shadowText.enableAutoSizing = true;
            shadowText.fontSizeMin = 28;
            shadowText.fontSizeMax = 48;
            shadowText.lineSpacing = -30f;
            if (FontManager.ExtraBold != null)
                shadowText.font = FontManager.ExtraBold;
            // Shadow renders behind main text
            titleBarShadowGO.transform.SetSiblingIndex(titleBarTextGO.transform.GetSiblingIndex());
            titleBarTextGO.transform.SetAsLastSibling();

            // ============================================
            // Layer 6: Title Banner ("ACHIEVEMENTS" decorative banner)
            // Placed before world icon so icon renders on top of banner stars
            // ============================================
            var titleBannerGO = new GameObject("TitleBanner");
            titleBannerGO.transform.SetParent(achievementsPanel.transform, false);
            var titleBannerRect = titleBannerGO.AddComponent<RectTransform>();
            titleBannerRect.anchorMin = Vector2.zero;
            titleBannerRect.anchorMax = Vector2.one;
            titleBannerRect.offsetMin = Vector2.zero;
            titleBannerRect.offsetMax = Vector2.zero;
            var titleBannerImg = titleBannerGO.AddComponent<Image>();
            titleBannerImg.sprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_title");
            titleBannerImg.preserveAspect = true;
            titleBannerImg.raycastTarget = false;

            // ============================================
            // Layer 7: World Icon (200x202px, positioned in title bar area)
            // Centered at roughly x=270, y=398 in 1080x1920 (middle of icon bbox x(171-370), y(303-504))
            // ============================================
            var worldIconGO = new GameObject("WorldIcon");
            worldIconGO.transform.SetParent(achievementsPanel.transform, false);
            var worldIconRect = worldIconGO.AddComponent<RectTransform>();
            // Center anchor at (270/1080, 1-398/1920) = (0.250, 0.793)
            worldIconRect.anchorMin = new Vector2(0.250f, 0.793f);
            worldIconRect.anchorMax = new Vector2(0.250f, 0.793f);
            worldIconRect.pivot = new Vector2(0.5f, 0.5f);
            worldIconRect.sizeDelta = new Vector2(200, 202);
            achievementWorldIconImage = worldIconGO.AddComponent<Image>();
            var generalIconSprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_general_icon");
            if (generalIconSprite != null)
                achievementWorldIconImage.sprite = generalIconSprite;
            achievementWorldIconImage.preserveAspect = true;
            achievementWorldIconImage.raycastTarget = false;

            // ============================================
            // Layer 8: Points text - positioned below/beside icon for General tab
            // Same area as icon: x(171-370), y(303-504)
            // ============================================
            var pointsTextGO = new GameObject("PointsText");
            pointsTextGO.transform.SetParent(achievementsPanel.transform, false);
            var pointsTextRect = pointsTextGO.AddComponent<RectTransform>();
            // x: 171/1080≈0.158, 370/1080≈0.343
            // y: 1-504/1920≈0.738, 1-303/1920≈0.842
            // Shifted 5px higher (5/1920 ≈ 0.0026) from original Y anchors
            pointsTextRect.anchorMin = new Vector2(0.158f, 0.7406f);
            pointsTextRect.anchorMax = new Vector2(0.343f, 0.8446f);
            pointsTextRect.offsetMin = Vector2.zero;
            pointsTextRect.offsetMax = Vector2.zero;

            achievementPointsText = pointsTextGO.AddComponent<TextMeshProUGUI>();
            achievementPointsText.text = "0";
            achievementPointsText.fontSize = 42;
            achievementPointsText.fontStyle = FontStyles.Bold;
            achievementPointsText.alignment = TextAlignmentOptions.Center;
            achievementPointsText.color = Color.black;
            achievementPointsText.enableAutoSizing = true;
            achievementPointsText.fontSizeMin = 20;
            achievementPointsText.fontSizeMax = 42;
            if (FontManager.ExtraBold != null)
                achievementPointsText.font = FontManager.ExtraBold;

            // ============================================
            // Layer 9: Close Button (X in upper-right)
            // 118×118 sprite centered at pixel (974, 215) in 1080×1920 → anchor (0.9019, 0.8880)
            // ============================================
            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(achievementsPanel.transform, false);
            var closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.9019f, 0.8880f);
            closeBtnRect.anchorMax = new Vector2(0.9019f, 0.8880f);
            closeBtnRect.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRect.sizeDelta = new Vector2(118, 118);

            var closeBtnNormalSprite = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_2");
            var closeBtnPressedSprite = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_pressed");
            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            closeBtnImg.sprite = closeBtnNormalSprite;
            closeBtnImg.raycastTarget = true;

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(HideAchievements);

            // Swap sprite on press (normal ↔ pressed)
            var closePressHandler = closeBtnGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var closePointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            closePointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            closePointerDown.callback.AddListener((data) => { closeBtnImg.sprite = closeBtnPressedSprite ?? closeBtnNormalSprite; });
            closePressHandler.triggers.Add(closePointerDown);
            var closePointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            closePointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            closePointerUp.callback.AddListener((data) => { closeBtnImg.sprite = closeBtnNormalSprite; });
            closePressHandler.triggers.Add(closePointerUp);

            Debug.Log("[UIManager] Achievements panel created (sprite-based UI)");
        }

        private void CreateAchievementTab(Transform parent, string tabId, string label)
        {
            var tabGO = new GameObject($"Tab_{tabId}");
            tabGO.transform.SetParent(parent, false);

            var tabLayoutElem = tabGO.AddComponent<LayoutElement>();
            tabLayoutElem.preferredHeight = 63; // 109px tab at ~58% screen scale (15% larger)
            tabLayoutElem.minHeight = 63;

            var tabImg = tabGO.AddComponent<Image>();
            tabImg.sprite = achvTabSprite;
            tabImg.preserveAspect = true;

            var tabBtn = tabGO.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;
            tabBtn.transition = UnityEngine.UI.Selectable.Transition.None;

            var capturedTab = tabId;
            tabBtn.onClick.AddListener(() => OnAchievementTabClicked(capturedTab));

            // Tab label text
            var tabTextGO = new GameObject("Text");
            tabTextGO.transform.SetParent(tabGO.transform, false);
            var tabTextRect = tabTextGO.AddComponent<RectTransform>();
            tabTextRect.anchorMin = Vector2.zero;
            tabTextRect.anchorMax = Vector2.one;
            tabTextRect.offsetMin = new Vector2(10, 0);
            tabTextRect.offsetMax = new Vector2(-5, 0);

            var tabText = tabTextGO.AddComponent<TextMeshProUGUI>();
            tabText.text = label;
            tabText.fontSize = 16;
            tabText.fontStyle = FontStyles.Bold;
            tabText.alignment = TextAlignmentOptions.Center;
            tabText.color = Color.white;
            tabText.enableAutoSizing = true;
            tabText.fontSizeMin = 8;
            tabText.fontSizeMax = 16;
            tabText.enableWordWrapping = true;
            tabText.overflowMode = TextOverflowModes.Truncate;
            if (FontManager.ExtraBold != null)
                tabText.font = FontManager.ExtraBold;

            achievementTabButtons[tabId] = tabBtn;
            achievementTabSpriteImages[tabId] = tabImg;
        }

        private void OnAchievementTabClicked(string tab)
        {
            AudioManager.Instance?.PlayButtonClick();
            currentAchievementTab = tab;
            UpdateAchievementTabVisuals();
            UpdateAchievementTitleBar();
            RefreshAchievementsPanel();
        }

        private void UpdateAchievementTabVisuals()
        {
            foreach (var kvp in achievementTabSpriteImages)
            {
                bool isSelected = kvp.Key == currentAchievementTab;
                kvp.Value.sprite = isSelected ? achvTabPressedSprite : achvTabSprite;
            }
        }

        private void PopulateAchievementTabs()
        {
            if (achievementTabsContainer == null || AchievementManager.Instance == null) return;

            foreach (Transform child in achievementTabsContainer)
            {
                Destroy(child.gameObject);
            }
            achievementTabButtons.Clear();
            achievementTabSpriteImages.Clear();

            foreach (var tabId in AchievementManager.Instance.AvailableTabs)
            {
                string shortName = Achievement.GetTabShortName(tabId);
                CreateAchievementTab(achievementTabsContainer, tabId, shortName);
            }
        }

        private void ShowAchievements()
        {
            if (achievementsPanel == null) return;

            currentAchievementTab = Achievement.TAB_GENERAL;
            PopulateAchievementTabs();
            UpdateAchievementTabVisuals();
            UpdateAchievementTitleBar();
            RefreshAchievementsPanel();
            achievementsPanel.SetActive(true);
            Debug.Log("[UIManager] Showing achievements panel");
        }

        private void HideAchievements()
        {
            if (achievementsPanel == null) return;

            AudioManager.Instance?.PlayButtonClick();
            achievementsPanel.SetActive(false);
            Debug.Log("[UIManager] Hiding achievements panel");
        }

        private void RefreshAchievementsPanel()
        {
            if (achievementsListContent == null || AchievementManager.Instance == null) return;

            // Clear existing items
            foreach (Transform child in achievementsListContent)
            {
                Destroy(child.gameObject);
            }

            // Update points text
            if (achievementPointsText != null && AchievementManager.Instance != null)
            {
                achievementPointsText.text = $"{AchievementManager.Instance.GetEarnedPoints()}";
            }

            // All tabs show category-grouped cards (no more Recent tab)
            RefreshCategoryTab(currentAchievementTab);
        }

        private void UpdateAchievementTitleBar()
        {
            if (achievementTitleBarText == null) return;

            bool isGeneral = currentAchievementTab == Achievement.TAB_GENERAL;
            var shadowText = achievementTitleBarText.transform.parent.Find("TitleBarShadow")?.GetComponent<TextMeshProUGUI>();

            if (isGeneral)
            {
                achievementTitleBarText.text = "Achievement\nPoints";
                if (shadowText != null) shadowText.text = "Achievement\nPoints";

                // Show points text, show general icon
                if (achievementPointsText != null)
                {
                    achievementPointsText.gameObject.SetActive(true);
                    achievementPointsText.text = AchievementManager.Instance != null
                        ? $"{AchievementManager.Instance.GetEarnedPoints()}"
                        : "0";
                }
                if (achievementWorldIconImage != null)
                {
                    var generalIcon = LoadFullRectSprite("Sprites/UI/Achievements/achv_general_icon");
                    if (generalIcon != null)
                    {
                        achievementWorldIconImage.sprite = generalIcon;
                        achievementWorldIconImage.enabled = true;
                    }
                    else
                    {
                        achievementWorldIconImage.enabled = false;
                    }
                }
            }
            else
            {
                // World tab: show world name on title bar
                string displayName = GetWorldTitleBarName(currentAchievementTab);
                achievementTitleBarText.text = displayName;
                if (shadowText != null) shadowText.text = displayName;

                // Hide points text
                if (achievementPointsText != null)
                    achievementPointsText.gameObject.SetActive(false);

                // Show world icon
                if (achievementWorldIconImage != null)
                {
                    var iconSprite = LoadFullRectSprite($"Sprites/UI/Achievements/achv_{currentAchievementTab}_icon");
                    if (iconSprite != null)
                    {
                        achievementWorldIconImage.sprite = iconSprite;
                        achievementWorldIconImage.enabled = true;
                    }
                    else
                    {
                        achievementWorldIconImage.enabled = false;
                    }
                }
            }
        }

        private static string GetWorldTitleBarName(string worldId)
        {
            switch (worldId)
            {
                case "island": return "St. Games\nIsland";
                case "supermarket": return "Superstore";
                case "farm": return "Wilty Acres";
                case "tavern": return "The Oink\n& Anchor";
                case "space": return "Space\nStation";
                default: return Achievement.GetTabDisplayName(worldId);
            }
        }

        private void RefreshCategoryTab(string tab)
        {
            var groupIds = AchievementManager.Instance.GetGroupIdsForTab(tab);

            foreach (var groupId in groupIds)
            {
                CreateAchievementCard(groupId);
            }
        }

        /// <summary>
        /// Inserts a line break at the best word boundary if text exceeds maxSingleLineChars.
        /// Title reference: "Globe Trotter" (13 chars). Description reference: "Make 1000 Matches" (18 chars).
        /// </summary>
        private string FormatTextForWrapping(string text, int maxSingleLineChars)
        {
            if (text.Length <= maxSingleLineChars)
                return text;

            // Find the best word break point near the middle for balanced wrapping
            int mid = text.Length / 2;
            int bestBreak = -1;
            for (int offset = 0; offset <= mid; offset++)
            {
                if (mid + offset < text.Length && text[mid + offset] == ' ')
                { bestBreak = mid + offset; break; }
                if (mid - offset > 0 && text[mid - offset] == ' ')
                { bestBreak = mid - offset; break; }
            }

            if (bestBreak > 0)
                return text.Substring(0, bestBreak) + "\n" + text.Substring(bestBreak + 1);

            return text;
        }

        /// <summary>
        /// Creates a single achievement card for a group.
        /// Large rectangle art with title/description centered in the open area.
        /// Progress bar with text below the rectangle.
        /// </summary>
        private void CreateAchievementCard(string groupId, Achievement specificAchievement = null, AchievementProgress specificProgress = null)
        {
            var mgr = AchievementManager.Instance;
            string artKey = mgr.GetGroupArtKey(groupId);
            AchievementTier? currentTier = mgr.GetGroupCurrentTier(groupId);
            Achievement nextMilestone = mgr.GetNextMilestone(groupId);
            int groupProgress = mgr.GetGroupProgress(groupId);

            Achievement displayAchievement = specificAchievement ?? nextMilestone ?? mgr.GetAchievementsByGroup(groupId)[0];
            bool isSpecificUnlocked = specificProgress != null && specificProgress.isUnlocked;

            // Determine which tier sprite to load
            string tierSuffix = "grey";
            if (isSpecificUnlocked && specificAchievement != null)
            {
                tierSuffix = specificAchievement.tier.ToString().ToLower();
            }
            else if (currentTier.HasValue)
            {
                tierSuffix = currentTier.Value.ToString().ToLower();
            }

            string spritePath = $"Sprites/UI/Achievements/{artKey}_{tierSuffix}";

            // Card container (no background — sits directly on yellow panel)
            // Compute card height dynamically so progress bar matches rect width exactly.
            // Rect art is 687×301 (aspect 2.283). Progress bar is 457×98 (aspect 4.663).
            // Both are centered at the same width, derived from rect's rendered height.
            const float RECT_ASPECT = 687f / 301f;  // 2.283
            const float RECT_DISPLAY_HEIGHT = 216f;  // fixed height for the rectangle image
            float renderedRectWidth = RECT_DISPLAY_HEIGHT * RECT_ASPECT; // ~494px

            // Load progress bar sprite early to compute its aspect-correct height
            var progressBarSprite = LoadFullRectSprite("Sprites/UI/Achievements/progress_bar");
            const float BAR_ASPECT = 457f / 98f; // 4.663 fallback
            float barAspect = BAR_ASPECT;
            if (progressBarSprite != null)
                barAspect = (float)progressBarSprite.texture.width / progressBarSprite.texture.height;
            float barDisplayHeight = renderedRectWidth / barAspect; // ~106px

            float cardHeight = RECT_DISPLAY_HEIGHT + barDisplayHeight;
            float barFraction = barDisplayHeight / cardHeight; // bottom portion for bar

            var cardGO = new GameObject($"Card_{groupId}_{displayAchievement.id}");
            var cardRect = cardGO.AddComponent<RectTransform>();
            cardGO.transform.SetParent(achievementsListContent, false);
            cardRect.sizeDelta = new Vector2(0, cardHeight);

            var cardLayout = cardGO.AddComponent<LayoutElement>();
            cardLayout.minHeight = cardHeight;
            cardLayout.preferredHeight = cardHeight;

            // ============================================
            // Achievement rectangle image (upper portion, centered)
            // Uses preserveAspect so rendered width = RECT_DISPLAY_HEIGHT × RECT_ASPECT
            // ============================================
            var rectImgGO = new GameObject("RectImage");
            rectImgGO.transform.SetParent(cardGO.transform, false);
            var rectImgRect = rectImgGO.AddComponent<RectTransform>();
            rectImgRect.anchorMin = new Vector2(0, barFraction);
            rectImgRect.anchorMax = new Vector2(1, 1);
            // Shift 30px left to center on the board
            rectImgRect.offsetMin = new Vector2(-30, 0);
            rectImgRect.offsetMax = new Vector2(-30, 0);

            var rectImg = rectImgGO.AddComponent<Image>();
            var rectSprite = LoadFullRectSprite(spritePath);
            if (rectSprite != null)
            {
                rectImg.sprite = rectSprite;
                rectImg.preserveAspect = true;
            }
            else
            {
                rectImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            // Title text ON the rectangle — centered in the open area right of the badge
            // Badge circle occupies ~38% of rectangle width, text area is 38%-97%
            // Shifted 10px left (10/687 ≈ 0.0146) from original anchors
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(rectImgGO.transform, false);
            var titleTextRect = titleGO.AddComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0.397f, 0.49f);
            titleTextRect.anchorMax = new Vector2(0.867f, 0.756f);
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            // Strip world name prefix from titles (e.g., "Tavern: Making Progress" → "Making Progress")
            string titleStr = displayAchievement.name;
            if (titleStr.Contains(": "))
                titleStr = titleStr.Substring(titleStr.IndexOf(": ") + 2);
            // Word-wrap titles longer than "Globe Trotter" (13 chars max single-line width)
            titleStr = FormatTextForWrapping(titleStr, 13);
            titleText.text = titleStr;
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.black;
            titleText.enableWordWrapping = true;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 14;
            titleText.fontSizeMax = 24;
            FontManager.ApplyBold(titleText);

            // Description text ON the rectangle (below title, centered in open area)
            // Shifted 10px left (10/687 ≈ 0.0146) from original anchors
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(rectImgGO.transform, false);
            var descTextRect = descGO.AddComponent<RectTransform>();
            descTextRect.anchorMin = new Vector2(0.397f, 0.227f);
            descTextRect.anchorMax = new Vector2(0.867f, 0.49f);
            descTextRect.offsetMin = Vector2.zero;
            descTextRect.offsetMax = Vector2.zero;

            var descText = descGO.AddComponent<TextMeshProUGUI>();
            // Word-wrap descriptions longer than "Make 1000 Matches" (18 chars max single-line width)
            string descStr = FormatTextForWrapping(displayAchievement.description, 18);
            descText.text = descStr;
            descText.fontSize = 18;
            descText.alignment = TextAlignmentOptions.Center;
            descText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            descText.enableWordWrapping = true;
            descText.enableAutoSizing = true;
            descText.fontSizeMin = 12;
            descText.fontSizeMax = 18;

            // ============================================
            // Progress bar area (below rectangle)
            // ============================================
            int progressCurrent;
            int progressTarget;
            if (nextMilestone != null)
            {
                progressCurrent = groupProgress;
                progressTarget = nextMilestone.targetValue;
            }
            else
            {
                var allInGroup = mgr.GetAchievementsByGroup(groupId);
                var lastMilestone = allInGroup[allInGroup.Count - 1];
                progressCurrent = groupProgress;
                progressTarget = lastMilestone.targetValue;
            }
            float progressFraction = progressTarget > 0 ? Mathf.Clamp01((float)progressCurrent / progressTarget) : 0f;

            // Progress bar container — centered below rect, exact same pixel width
            // Uses fixed sizeDelta so it matches the rendered rect width exactly
            var barContainerGO = new GameObject("ProgressBar");
            barContainerGO.transform.SetParent(cardGO.transform, false);
            var barContainerRect = barContainerGO.AddComponent<RectTransform>();
            barContainerRect.anchorMin = new Vector2(0.5f, 0);
            barContainerRect.anchorMax = new Vector2(0.5f, 0);
            barContainerRect.pivot = new Vector2(0.5f, 0);
            barContainerRect.sizeDelta = new Vector2(renderedRectWidth, barDisplayHeight);
            // Shift 30px left to match rect image centering
            barContainerRect.anchoredPosition = new Vector2(-30, 0);

            // Green fill behind the bar frame (visible through transparent center)
            // Inset to stay inside rounded corners of frame (457×98 sprite, ~14px corner radius)
            const float FILL_INSET_X = 14f / 457f; // ~3% horizontal padding for rounded corners
            const float FILL_INSET_Y = 14f / 98f;  // ~14% vertical padding for rounded corners
            float fillLeft = FILL_INSET_X;
            float fillRight = 1f - FILL_INSET_X;
            float fillWidth = fillRight - fillLeft;
            if (progressFraction > 0f)
            {
                var barFillGO = new GameObject("ProgressBarFill");
                barFillGO.transform.SetParent(barContainerGO.transform, false);
                var barFillRect = barFillGO.AddComponent<RectTransform>();
                barFillRect.anchorMin = new Vector2(fillLeft, FILL_INSET_Y);
                barFillRect.anchorMax = new Vector2(fillLeft + progressFraction * fillWidth, 1f - FILL_INSET_Y);
                barFillRect.offsetMin = Vector2.zero;
                barFillRect.offsetMax = Vector2.zero;
                var barFillImg = barFillGO.AddComponent<Image>();
                barFillImg.color = new Color(0.25f, 0.72f, 0.20f, 1f); // Green
            }

            // Progress bar frame sprite (fills container exactly — container is already aspect-correct)
            var barFrameGO = new GameObject("ProgressBarFrame");
            barFrameGO.transform.SetParent(barContainerGO.transform, false);
            var barFrameRect = barFrameGO.AddComponent<RectTransform>();
            barFrameRect.anchorMin = Vector2.zero;
            barFrameRect.anchorMax = Vector2.one;
            barFrameRect.offsetMin = Vector2.zero;
            barFrameRect.offsetMax = Vector2.zero;
            var barFrameImg = barFrameGO.AddComponent<Image>();
            if (progressBarSprite != null)
            {
                barFrameImg.sprite = progressBarSprite;
            }

            // Progress text centered on the bar
            var progressTextGO = new GameObject("ProgressText");
            progressTextGO.transform.SetParent(barContainerGO.transform, false);
            var progressTextRect = progressTextGO.AddComponent<RectTransform>();
            progressTextRect.anchorMin = Vector2.zero;
            progressTextRect.anchorMax = Vector2.one;
            progressTextRect.offsetMin = Vector2.zero;
            progressTextRect.offsetMax = Vector2.zero;

            var progressText = progressTextGO.AddComponent<TextMeshProUGUI>();
            progressText.text = $"{progressCurrent}/{progressTarget}";
            progressText.fontSize = 20;
            progressText.fontStyle = FontStyles.Bold;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.white;
            progressText.enableAutoSizing = true;
            progressText.fontSizeMin = 12;
            progressText.fontSizeMax = 20;
            FontManager.ApplyBold(progressText);
        }

        private Color GetTierColor(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Bronze:
                    return new Color(0.8f, 0.5f, 0.2f, 1f);
                case AchievementTier.Silver:
                    return new Color(0.7f, 0.72f, 0.78f, 1f);
                case AchievementTier.Gold:
                    return new Color(1f, 0.84f, 0f, 1f);
                case AchievementTier.Platinum:
                    return new Color(0.5f, 0.82f, 0.88f, 1f);
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Dialogue Panel

        private void CreateDialoguePanel()
        {
            dialoguePanel = new GameObject("Dialogue Panel");
            dialoguePanel.transform.SetParent(mainCanvas.transform, false);

            var rect = dialoguePanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Add canvas group for fade animations
            var canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();

            // Override sorting so dialogue renders above portal ResultOverlays (sortingOrder 5001)
            var dialogueCanvas = dialoguePanel.AddComponent<Canvas>();
            dialogueCanvas.overrideSorting = true;
            dialogueCanvas.sortingOrder = 5100;
            dialoguePanel.AddComponent<GraphicRaycaster>();

            // Semi-transparent overlay - blocks all clicks behind dialogue
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(dialoguePanel.transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.3f);
            overlayImage.raycastTarget = true; // Block clicks from reaching game elements

            // Mascot image - created BEFORE dialogue box so it renders behind
            // Positioned so just feet/legs are behind box, most of body visible above
            var mascotContainer = new GameObject("Mascot Container");
            mascotContainer.transform.SetParent(dialoguePanel.transform, false);
            var mascotRect = mascotContainer.AddComponent<RectTransform>();
            mascotRect.anchorMin = new Vector2(0, 0);
            mascotRect.anchorMax = new Vector2(0, 0);
            mascotRect.pivot = new Vector2(0, 0);
            mascotRect.anchoredPosition = new Vector2(10, 160); // Left side, positioned so feet overlap box
            mascotRect.sizeDelta = new Vector2(480, 750); // Large mascot visible above box

            dialogueMascotImage = mascotContainer.AddComponent<Image>();
            dialogueMascotImage.preserveAspect = true;
            dialogueMascotImage.raycastTarget = false; // Don't block clicks
            dialogueMascotImage.enabled = false; // Hidden until mascot set

            // Dialogue box container (bottom of screen) - renders ON TOP of mascot
            var dialogueBox = new GameObject("Dialogue Box");
            dialogueBox.transform.SetParent(dialoguePanel.transform, false);
            var boxRect = dialogueBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0, 0);
            boxRect.anchorMax = new Vector2(1, 0);
            boxRect.pivot = new Vector2(0.5f, 0);
            boxRect.anchoredPosition = new Vector2(0, 50);
            boxRect.sizeDelta = new Vector2(-60, 300); // Full width minus margins, 300px tall

            // Dialogue box background
            dialogueBoxImage = dialogueBox.AddComponent<Image>();
            var defaultBoxSprite = Resources.Load<Sprite>("Sprites/UI/Dialogue/dialoguebox_island");
            if (defaultBoxSprite != null)
            {
                dialogueBoxImage.sprite = defaultBoxSprite;
                dialogueBoxImage.type = Image.Type.Sliced;
            }
            else
            {
                dialogueBoxImage.color = new Color(0.9f, 0.85f, 0.7f, 0.98f); // Light beige fallback
            }

            // Text content area
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(dialogueBox.transform, false);
            var textRect = textArea.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(40, 30);   // Close to left edge
            textRect.offsetMax = new Vector2(-30, -70); // Right and top padding (push text below border)

            // Dialogue text
            var dialogueTextGO = new GameObject("Dialogue Text");
            dialogueTextGO.transform.SetParent(textArea.transform, false);
            var dialogueTextRect = dialogueTextGO.AddComponent<RectTransform>();
            dialogueTextRect.anchorMin = Vector2.zero;
            dialogueTextRect.anchorMax = Vector2.one;
            dialogueTextRect.offsetMin = new Vector2(10, 10);
            dialogueTextRect.offsetMax = new Vector2(-10, -10);

            dialogueText = dialogueTextGO.AddComponent<TextMeshProUGUI>();
            dialogueText.text = "";
            dialogueText.fontSize = 42;
            dialogueText.fontStyle = FontStyles.Bold;
            dialogueText.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark/black text
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.enableWordWrapping = true;

            // Continue indicator (blinking arrow or "tap to continue")
            var continueGO = new GameObject("Continue Indicator");
            continueGO.transform.SetParent(dialogueBox.transform, false);
            var continueRect = continueGO.AddComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1, 0);
            continueRect.anchorMax = new Vector2(1, 0);
            continueRect.pivot = new Vector2(1, 0);
            continueRect.anchoredPosition = new Vector2(-30, 20);
            continueRect.sizeDelta = new Vector2(150, 40);

            var continueText = continueGO.AddComponent<TextMeshProUGUI>();
            continueText.text = "Tap to continue ▶";
            continueText.fontSize = 20;
            continueText.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Dark gray
            continueText.alignment = TextAlignmentOptions.Right;
            continueText.fontStyle = FontStyles.Italic;

            dialogueContinueIndicator = continueGO;
            dialogueContinueIndicator.SetActive(false);

            // Add DialogueUI component and wire up references
            dialogueUI = dialoguePanel.AddComponent<DialogueUI>();

            // Use reflection to set serialized fields (since they're private)
            var uiType = typeof(DialogueUI);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            uiType.GetField("dialoguePanel", flags)?.SetValue(dialogueUI, dialoguePanel);
            uiType.GetField("dialogueBoxImage", flags)?.SetValue(dialogueUI, dialogueBoxImage);
            uiType.GetField("mascotImage", flags)?.SetValue(dialogueUI, dialogueMascotImage);
            uiType.GetField("dialogueText", flags)?.SetValue(dialogueUI, dialogueText);
            uiType.GetField("continueIndicator", flags)?.SetValue(dialogueUI, dialogueContinueIndicator);

            // Initialize after fields are set (hides the panel)
            dialogueUI.Initialize();

            Debug.Log($"[UIManager] Dialogue panel created, DialogueUI component: {dialogueUI != null}");
        }

        #endregion

        private void OnDestroy()
        {
            if (achievementCoroutine != null)
                StopCoroutine(achievementCoroutine);

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
            }

            if (Instance == this)
                Instance = null;
        }
    }
}
