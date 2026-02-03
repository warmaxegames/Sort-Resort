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
        private Image[] levelCompleteStarImages;
        private TextMeshProUGUI levelCompleteMessageText;
        private TextMeshProUGUI levelCompleteMoveCountText;
        private AnimatedLevelComplete animatedLevelComplete;
        private GameObject levelFailedPanel;
        private TextMeshProUGUI levelFailedReasonText;
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
        private Button undoButton;
        private CanvasGroup undoButtonCanvasGroup;
        private Button autoSolveButton;
        private Button recordButton;
        private bool isRecordingMoves = false;

        // Achievements screen
        private GameObject achievementsPanel;
        private Transform achievementsListContent;
        private TextMeshProUGUI achievementsHeaderText;
        private TextMeshProUGUI achievementsCountText;
        private TextMeshProUGUI achievementsPointsText;
        private string currentAchievementTab = Achievement.TAB_ALL;
        private Dictionary<string, Button> achievementTabButtons = new Dictionary<string, Button>();
        private Dictionary<string, Image> achievementTabImages = new Dictionary<string, Image>();
        private Transform achievementTabsContainer;
        private HashSet<string> expandedGroups = new HashSet<string>();

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
        private TextMeshProUGUI dialogueNameText;
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
            GameEvents.OnLevelRestarted += OnLevelRestarted;
            GameEvents.OnGamePaused += OnGamePausedUI;
            GameEvents.OnGameResumed += OnGameResumedUI;
            GameEvents.OnSettingsClosed += OnSettingsClosedUI;
            GameEvents.OnTimerUpdated += OnTimerUpdated;
            GameEvents.OnTimerFrozen += OnTimerFrozen;
            GameEvents.OnLevelFailed += OnLevelFailed;
            GameEvents.OnTimerExpired += OnTimerExpiredUI;

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
            GameEvents.OnLevelRestarted -= OnLevelRestarted;
            GameEvents.OnGamePaused -= OnGamePausedUI;
            GameEvents.OnGameResumed -= OnGameResumedUI;
            GameEvents.OnSettingsClosed -= OnSettingsClosedUI;
            GameEvents.OnTimerUpdated -= OnTimerUpdated;
            GameEvents.OnTimerFrozen -= OnTimerFrozen;
            GameEvents.OnLevelFailed -= OnLevelFailed;
            GameEvents.OnTimerExpired -= OnTimerExpiredUI;

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
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

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
            trophyText.text = "\u2605"; // Star symbol as placeholder for trophy
            trophyText.fontSize = 48;
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
            profileRect.sizeDelta = new Vector2(580, 200); // Even larger

            var profileImg = profileGO.AddComponent<Image>();
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
            prevBtnRect.sizeDelta = new Vector2(130, 150);

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
            nextBtnRect.sizeDelta = new Vector2(130, 150);

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
            // LEVEL GRID SCROLL AREA - Rounded corners, larger scrollbar
            // ============================================
            var scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(levelSelectPanel.transform, false);
            var scrollAreaRect = scrollArea.AddComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0, 0);
            scrollAreaRect.anchorMax = new Vector2(1, 0.48f);
            scrollAreaRect.offsetMin = new Vector2(15, 15);
            scrollAreaRect.offsetMax = new Vector2(-15, 0);

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
            timerText.text = "0:00";
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
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(340, 50);

            var layout = buttonsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childForceExpandWidth = false;

            // Record button (debug feature) - records moves for comparison with solver
            recordButton = CreateButton(buttonsGO.transform, "Record", "Rec", OnRecordClicked, 50, 50);
            recordButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.5f, 0.5f); // Red tint

            // Auto-Solve button (debug feature) - solves level automatically
            autoSolveButton = CreateButton(buttonsGO.transform, "AutoSolve", "Solve", OnAutoSolveClicked, 60, 50);
            autoSolveButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5f, 1f, 0.5f); // Green tint

            // Undo button - undoes last move
            undoButton = CreateButton(buttonsGO.transform, "Undo", "Undo", OnUndoClicked, 60, 50);
            undoButtonCanvasGroup = undoButton.gameObject.AddComponent<CanvasGroup>();
            // Start disabled (no moves to undo)
            SetUndoButtonEnabled(false);

            // Pause button - opens pause menu
            CreateButton(buttonsGO.transform, "Pause", "II", OnPauseClicked, 50, 50);

            // Back button - exits level and returns to level select
            CreateButton(buttonsGO.transform, "Back", "Back", OnBackToLevelsClicked, 80, 50);
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

        private void CreateLevelCompletePanel()
        {
            levelCompletePanel = new GameObject("Level Complete Panel");
            levelCompletePanel.transform.SetParent(mainCanvas.transform, false);

            var rect = levelCompletePanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Animated background - Rays (behind everything, loops)
            var raysGO = new GameObject("Rays Background");
            raysGO.transform.SetParent(levelCompletePanel.transform, false);
            var raysRect = raysGO.AddComponent<RectTransform>();
            raysRect.anchorMin = Vector2.zero;
            raysRect.anchorMax = Vector2.one;
            raysRect.offsetMin = Vector2.zero;
            raysRect.offsetMax = Vector2.zero;
            var raysImage = raysGO.AddComponent<Image>();
            raysImage.preserveAspect = false;

            // Animated background - Curtains (on top of rays, plays once)
            var curtainsGO = new GameObject("Curtains");
            curtainsGO.transform.SetParent(levelCompletePanel.transform, false);
            var curtainsRect = curtainsGO.AddComponent<RectTransform>();
            curtainsRect.anchorMin = Vector2.zero;
            curtainsRect.anchorMax = Vector2.one;
            curtainsRect.offsetMin = Vector2.zero;
            curtainsRect.offsetMax = Vector2.zero;
            var curtainsImage = curtainsGO.AddComponent<Image>();
            curtainsImage.preserveAspect = false;

            // Add and initialize the animation controller
            animatedLevelComplete = levelCompletePanel.AddComponent<AnimatedLevelComplete>();
            animatedLevelComplete.Initialize(raysImage, curtainsImage);

            // Content container
            var content = new GameObject("Content");
            content.transform.SetParent(levelCompletePanel.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 500);

            // Background panel
            var panelBg = content.AddComponent<Image>();
            panelBg.color = new Color(0.2f, 0.6f, 0.9f, 1f);

            var vlayout = content.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(40, 40, 40, 40);
            vlayout.spacing = 30;
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;

            // Title
            CreateTextElement(content.transform, "Title", "Level Complete!", 48,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 80);

            // Stars container
            var starsContainer = new GameObject("Stars");
            starsContainer.transform.SetParent(content.transform, false);
            var starsRect = starsContainer.AddComponent<RectTransform>();
            starsRect.sizeDelta = new Vector2(300, 100);
            var starsLayout = starsContainer.AddComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 10;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childForceExpandWidth = false;
            starsLayout.childControlWidth = false;
            starsLayout.childControlHeight = false;

            // Load star sprite
            var starSprite = Resources.Load<Sprite>("Sprites/UI/Icons/star");

            // Create star image elements and store references
            levelCompleteStarImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var starGO = new GameObject($"CompleteStar_{i}");
                starGO.transform.SetParent(starsContainer.transform, false);
                var starRectTrans = starGO.AddComponent<RectTransform>();
                starRectTrans.sizeDelta = new Vector2(80, 80);

                var starImg = starGO.AddComponent<Image>();
                starImg.sprite = starSprite;
                starImg.preserveAspect = true;
                starImg.color = new Color(0.4f, 0.4f, 0.4f, 1f); // Gray by default

                levelCompleteStarImages[i] = starImg;
            }

            // Move count display
            var moveCountGO = CreateTextElement(content.transform, "MoveCount", "Completed in 0 moves", 24,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 40);
            levelCompleteMoveCountText = moveCountGO.GetComponent<TextMeshProUGUI>();

            // Message
            var messageGO = CreateTextElement(content.transform, "Message", "Great job!", 28,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 50);
            levelCompleteMessageText = messageGO.GetComponent<TextMeshProUGUI>();

            // Buttons container
            var buttonsContainer = new GameObject("Buttons");
            buttonsContainer.transform.SetParent(content.transform, false);
            var buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(500, 70);
            var buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 15;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            // Add layout element for proper sizing in vertical layout
            var layoutElement = buttonsContainer.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 70;
            layoutElement.preferredWidth = 500;

            CreateButton(buttonsContainer.transform, "Levels", "Levels", OnBackToLevelsClicked, 140, 60);
            CreateButton(buttonsContainer.transform, "Replay", "Replay", OnReplayClicked, 140, 60);
            CreateButton(buttonsContainer.transform, "Next", "Next", OnNextLevelFromCompleteClicked, 140, 60);

            Debug.Log("[UIManager] Level complete panel created with 3 buttons");
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

            // Dark overlay
            var overlay = levelFailedPanel.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.8f);

            // Content container
            var content = new GameObject("Content");
            content.transform.SetParent(levelFailedPanel.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 450);

            // Background panel (red/orange tint for failure)
            var panelBg = content.AddComponent<Image>();
            panelBg.color = new Color(0.8f, 0.3f, 0.2f, 1f);

            var vlayout = content.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(40, 40, 40, 40);
            vlayout.spacing = 25;
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;

            // Title
            CreateTextElement(content.transform, "Title", "Level Failed", 48,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 80);

            // Reason text (will be updated dynamically)
            var reasonGO = CreateTextElement(content.transform, "Reason", "Time's Up!", 32,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 60);
            levelFailedReasonText = reasonGO.GetComponent<TextMeshProUGUI>();

            // Encouragement message
            CreateTextElement(content.transform, "Message", "Try again!", 24,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 40);

            // Buttons container
            var buttonsContainer = new GameObject("Buttons");
            buttonsContainer.transform.SetParent(content.transform, false);
            var buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(400, 70);
            var buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            // Add layout element for proper sizing
            var layoutElement = buttonsContainer.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 70;
            layoutElement.preferredWidth = 400;

            CreateButton(buttonsContainer.transform, "Levels", "Levels", OnBackToLevelsFromFailedClicked, 160, 60);
            CreateButton(buttonsContainer.transform, "Retry", "Retry", OnRetryFromFailedClicked, 160, 60);

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
            // TIMER TOGGLE
            // ============================================
            var timerRowGO = new GameObject("TimerRow");
            timerRowGO.transform.SetParent(contentGO.transform, false);
            var timerRowRect = timerRowGO.AddComponent<RectTransform>();
            timerRowRect.anchorMin = new Vector2(0.5f, 1);
            timerRowRect.anchorMax = new Vector2(0.5f, 1);
            timerRowRect.pivot = new Vector2(0.5f, 1);
            timerRowRect.anchoredPosition = new Vector2(0, -700);
            timerRowRect.sizeDelta = new Vector2(700, 80);

            var timerLayout = timerRowGO.AddComponent<HorizontalLayoutGroup>();
            timerLayout.spacing = 20;
            timerLayout.childAlignment = TextAnchor.MiddleCenter;
            timerLayout.childForceExpandWidth = false;
            timerLayout.childForceExpandHeight = false;

            // Timer label
            var timerLabelGO = new GameObject("TimerLabel");
            timerLabelGO.transform.SetParent(timerRowGO.transform, false);
            var timerLabelText = timerLabelGO.AddComponent<TextMeshProUGUI>();
            timerLabelText.text = "Level Timer";
            timerLabelText.fontSize = 36;
            timerLabelText.fontStyle = FontStyles.Bold;
            timerLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            timerLabelText.color = Color.white;
            var timerLabelLE = timerLabelGO.AddComponent<LayoutElement>();
            timerLabelLE.preferredWidth = 400;
            timerLabelLE.preferredHeight = 60;

            // Timer toggle (Google-style switch)
            var (timerToggle, timerCheckmark) = CreateGoogleSwitch(timerRowGO.transform);

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
                timerToggle, timerCheckmark,
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

            // Dark overlay/dim background
            var dimBg = pauseMenuPanel.AddComponent<Image>();
            dimBg.color = new Color(0, 0, 0, 0.5f);

            // Add CanvasGroup for fading
            var canvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();

            // Load button sprites
            var boardSprite = Resources.Load<Sprite>("Sprites/UI/Settings/board");

            // Main content container (centered)
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(pauseMenuPanel.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 700);

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

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(contentGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -40);
            titleRect.sizeDelta = new Vector2(0, 80);

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "PAUSED";
            titleText.fontSize = 56;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Buttons container
            var buttonsContainer = new GameObject("Buttons");
            buttonsContainer.transform.SetParent(contentGO.transform, false);
            var buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsRect.sizeDelta = new Vector2(450, 450);

            var buttonsLayout = buttonsContainer.AddComponent<VerticalLayoutGroup>();
            buttonsLayout.spacing = 25;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;
            buttonsLayout.padding = new RectOffset(0, 0, 20, 20);

            // Create buttons
            var resumeBtn = CreatePauseMenuButton(buttonsContainer.transform, "Resume", new Color(0.2f, 0.7f, 0.3f, 1f));
            var restartBtn = CreatePauseMenuButton(buttonsContainer.transform, "Restart Level", new Color(0.3f, 0.5f, 0.8f, 1f));
            var settingsBtn = CreatePauseMenuButton(buttonsContainer.transform, "Settings", new Color(0.5f, 0.5f, 0.5f, 1f));
            var exitBtn = CreatePauseMenuButton(buttonsContainer.transform, "Quit to Menu", new Color(0.8f, 0.3f, 0.3f, 1f));

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

            Debug.Log("[UIManager] Pause menu panel created");
        }

        private Button CreatePauseMenuButton(Transform parent, string text, Color bgColor)
        {
            var btnGO = new GameObject(text + "Button");
            btnGO.transform.SetParent(parent, false);
            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 400;
            btnLE.preferredHeight = 80;

            var btnImg = btnGO.AddComponent<Image>();
            // Create rounded button
            var roundedSprite = CreateRoundedRectSprite(256, 64, 20, bgColor);
            if (roundedSprite != null)
            {
                btnImg.sprite = roundedSprite;
                btnImg.type = Image.Type.Sliced;
            }
            else
            {
                btnImg.color = bgColor;
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
            textTMP.fontSize = 36;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color = Color.white;

            return btn;
        }

        /// <summary>
        /// Show the pause menu
        /// </summary>
        public void ShowPauseMenu()
        {
            if (pauseMenuPanel != null)
            {
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

        // Event handlers
        private void OnLevelStarted(int levelNumber)
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
                animatedLevelComplete?.Hide();
            }
            if (hudPanel != null)
                hudPanel.SetActive(true);

            // Update title with level info
            string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
            string worldName = char.ToUpper(worldId[0]) + worldId.Substring(1);
            if (levelTitleText != null)
                levelTitleText.text = $"{worldName} - Level {levelNumber}";

            UpdateMoveDisplay(0);
            UpdateMatchDisplay(0);

            // Reset undo button (no moves to undo at level start)
            SetUndoButtonEnabled(false);

            // Show/hide timer based on level settings
            // Timer visibility is handled in OnTimerUpdated when first timer event comes in
            // Hide timer initially - it will be shown when OnTimerUpdated fires if level has timer
            if (timerContainer != null)
            {
                timerContainer.SetActive(false);
            }

            // Reset record button text (recording is stopped when level changes)
            if (recordButton != null)
            {
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rec";
                recordButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.5f, 0.5f); // Normal red
            }
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

        private void OnLevelCompleted(int levelNumber, int stars)
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);

                // Start animated background
                if (animatedLevelComplete != null)
                {
                    animatedLevelComplete.Play();
                }

                // Update stars display
                if (levelCompleteStarImages != null)
                {
                    for (int i = 0; i < levelCompleteStarImages.Length; i++)
                    {
                        if (levelCompleteStarImages[i] != null)
                        {
                            // Full color for earned, gray for not earned
                            levelCompleteStarImages[i].color = i < stars
                                ? Color.white  // Full color (sprite is already yellow)
                                : new Color(0.3f, 0.3f, 0.3f, 1f); // Gray tint
                        }
                    }
                }

                // Update move count
                if (levelCompleteMoveCountText != null)
                {
                    int moveCount = GameManager.Instance?.CurrentMoveCount ?? 0;
                    levelCompleteMoveCountText.text = $"Completed in {moveCount} moves";
                }

                // Update message based on stars
                if (levelCompleteMessageText != null)
                {
                    levelCompleteMessageText.text = stars switch
                    {
                        3 => "Perfect!",
                        2 => "Great work!",
                        _ => "Good job!"
                    };
                }
            }
        }

        private void OnLevelFailed(int levelNumber)
        {
            ShowLevelFailedScreen("Level Failed");
        }

        private void OnTimerExpiredUI()
        {
            ShowLevelFailedScreen("Time's Up!");
        }

        private void ShowLevelFailedScreen(string reason)
        {
            // Hide HUD
            if (hudPanel != null)
                hudPanel.SetActive(false);

            if (levelFailedPanel != null)
            {
                // Update reason text
                if (levelFailedReasonText != null)
                {
                    levelFailedReasonText.text = reason;
                }

                levelFailedPanel.SetActive(true);
                Debug.Log($"[UIManager] Showing level failed screen: {reason}");
            }
        }

        private void OnBackToLevelsFromFailedClicked()
        {
            Debug.Log("[UIManager] Back to Levels from failed clicked");
            AudioManager.Instance?.PlayButtonClick();

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
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
                animatedLevelComplete?.Hide();
            }
            if (levelFailedPanel != null)
                levelFailedPanel.SetActive(false);

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
            {
                timerText.color = isFrozen ? Color.cyan : Color.white;
            }
        }

        private void UpdateTimerDisplay(float timeRemaining)
        {
            if (timerText == null) return;

            // Format as M:SS
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = $"{minutes}:{seconds:D2}";

            // Change color when time is low (under 10 seconds)
            if (timeRemaining <= 10f && !LevelManager.Instance?.IsTimerFrozen == true)
            {
                // Flash red when low on time
                float flash = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
                timerText.color = Color.Lerp(Color.red, Color.white, flash);
            }
            else if (!LevelManager.Instance?.IsTimerFrozen == true)
            {
                timerText.color = Color.white;
            }
        }

        private void UpdateMoveDisplay(int moves)
        {
            if (moveCountText != null)
                moveCountText.text = moves.ToString();
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

            // Play sound
            AudioManager.Instance?.PlayMatchSound(); // Reuse match sound for now

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

            // Dark overlay background
            var dimBg = achievementsPanel.AddComponent<Image>();
            dimBg.color = new Color(0, 0, 0, 0.9f);

            // Main content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(achievementsPanel.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.02f, 0.02f);
            contentRect.anchorMax = new Vector2(0.98f, 0.98f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Content background
            var contentBg = contentGO.AddComponent<Image>();
            contentBg.sprite = CreateRoundedRectSprite(100, 100, 20, new Color(0.12f, 0.11f, 0.15f, 1f));
            contentBg.type = Image.Type.Sliced;

            // ============================================
            // HEADER
            // ============================================
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(contentGO.transform, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 100);

            var headerBg = headerGO.AddComponent<Image>();
            headerBg.color = new Color(0.08f, 0.07f, 0.1f, 1f);

            // Header title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.4f, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = Vector2.zero;

            achievementsHeaderText = titleGO.AddComponent<TextMeshProUGUI>();
            achievementsHeaderText.text = "ACHIEVEMENTS";
            achievementsHeaderText.fontSize = 40;
            achievementsHeaderText.fontStyle = FontStyles.Bold;
            achievementsHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
            achievementsHeaderText.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            // Points display (center)
            var pointsGO = new GameObject("Points");
            pointsGO.transform.SetParent(headerGO.transform, false);
            var pointsRect = pointsGO.AddComponent<RectTransform>();
            pointsRect.anchorMin = new Vector2(0.4f, 0);
            pointsRect.anchorMax = new Vector2(0.7f, 1);
            pointsRect.offsetMin = Vector2.zero;
            pointsRect.offsetMax = Vector2.zero;

            achievementsPointsText = pointsGO.AddComponent<TextMeshProUGUI>();
            achievementsPointsText.text = "<size=24>POINTS</size>\n<color=#FFD700>0</color> / 0";
            achievementsPointsText.fontSize = 32;
            achievementsPointsText.fontStyle = FontStyles.Bold;
            achievementsPointsText.alignment = TextAlignmentOptions.Center;
            achievementsPointsText.color = Color.white;

            // Count display
            var countGO = new GameObject("Count");
            countGO.transform.SetParent(headerGO.transform, false);
            var countRect = countGO.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.7f, 0);
            countRect.anchorMax = new Vector2(0.88f, 1);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;

            achievementsCountText = countGO.AddComponent<TextMeshProUGUI>();
            achievementsCountText.text = "<size=24>UNLOCKED</size>\n0 / 0";
            achievementsCountText.fontSize = 32;
            achievementsCountText.fontStyle = FontStyles.Bold;
            achievementsCountText.alignment = TextAlignmentOptions.Center;
            achievementsCountText.color = Color.white;

            // Close button
            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(headerGO.transform, false);
            var closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-15, 0);
            closeBtnRect.sizeDelta = new Vector2(70, 70);

            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            closeBtnImg.sprite = CreateRoundedRectSprite(70, 70, 35, new Color(0.5f, 0.15f, 0.15f, 1f));

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.onClick.AddListener(HideAchievements);

            var closeTextGO = new GameObject("X");
            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
            var closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 36;
            closeText.fontStyle = FontStyles.Bold;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;

            // ============================================
            // LEFT SIDEBAR - Category Tabs
            // ============================================
            var sidebarGO = new GameObject("Sidebar");
            sidebarGO.transform.SetParent(contentGO.transform, false);
            var sidebarRect = sidebarGO.AddComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0, 0);
            sidebarRect.anchorMax = new Vector2(0, 1);
            sidebarRect.pivot = new Vector2(0, 0.5f);
            sidebarRect.anchoredPosition = Vector2.zero;
            sidebarRect.sizeDelta = new Vector2(180, 0);
            sidebarRect.offsetMin = new Vector2(0, 10);
            sidebarRect.offsetMax = new Vector2(180, -110);

            var sidebarBg = sidebarGO.AddComponent<Image>();
            sidebarBg.color = new Color(0.08f, 0.07f, 0.1f, 1f);

            // Tab buttons container
            var tabsContainerGO = new GameObject("TabsContainer");
            tabsContainerGO.transform.SetParent(sidebarGO.transform, false);
            var tabsContainerRect = tabsContainerGO.AddComponent<RectTransform>();
            tabsContainerRect.anchorMin = Vector2.zero;
            tabsContainerRect.anchorMax = Vector2.one;
            tabsContainerRect.offsetMin = new Vector2(5, 5);
            tabsContainerRect.offsetMax = new Vector2(-5, -5);

            var tabsLayout = tabsContainerGO.AddComponent<VerticalLayoutGroup>();
            tabsLayout.spacing = 8;
            tabsLayout.padding = new RectOffset(5, 5, 10, 10);
            tabsLayout.childAlignment = TextAnchor.UpperCenter;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = false;

            // Create tab buttons dynamically from AchievementManager
            achievementTabButtons.Clear();
            achievementTabImages.Clear();
            achievementTabsContainer = tabsContainerGO.transform;
            // Tabs will be populated in ShowAchievements when AchievementManager is ready

            // ============================================
            // MAIN CONTENT - Scrollable Achievement List
            // ============================================
            var scrollAreaGO = new GameObject("ScrollArea");
            scrollAreaGO.transform.SetParent(contentGO.transform, false);
            var scrollAreaRect = scrollAreaGO.AddComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0, 0);
            scrollAreaRect.anchorMax = new Vector2(1, 1);
            scrollAreaRect.offsetMin = new Vector2(190, 10);
            scrollAreaRect.offsetMax = new Vector2(-10, -110);

            var scrollBg = scrollAreaGO.AddComponent<Image>();
            scrollBg.sprite = CreateRoundedRectSprite(100, 100, 15, new Color(0.15f, 0.14f, 0.18f, 1f));
            scrollBg.type = Image.Type.Sliced;

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
            viewportRect.offsetMin = new Vector2(10, 10);
            viewportRect.offsetMax = new Vector2(-30, -10);

            var viewportMask = viewportGO.AddComponent<RectMask2D>();
            viewportMask.softness = new Vector2Int(0, 15);

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
            vertLayout.spacing = 12;
            vertLayout.padding = new RectOffset(5, 5, 5, 5);
            vertLayout.childAlignment = TextAnchor.UpperCenter;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = false;

            scrollRect.content = listContentRect;

            // Scrollbar
            var scrollbarGO = new GameObject("Scrollbar");
            scrollbarGO.transform.SetParent(scrollAreaGO.transform, false);
            var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.anchoredPosition = new Vector2(-5, 0);
            scrollbarRect.sizeDelta = new Vector2(18, -20);
            scrollbarRect.offsetMin = new Vector2(-23, 10);
            scrollbarRect.offsetMax = new Vector2(-5, -10);

            var scrollbarImg = scrollbarGO.AddComponent<Image>();
            scrollbarImg.sprite = CreateRoundedRectSprite(18, 50, 9, new Color(0.1f, 0.1f, 0.12f, 1f));
            scrollbarImg.type = Image.Type.Sliced;

            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(scrollbarGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(2, 2);
            handleRect.offsetMax = new Vector2(-2, -2);

            var handleImg = handleGO.AddComponent<Image>();
            handleImg.sprite = CreateRoundedRectSprite(14, 40, 7, new Color(0.4f, 0.6f, 0.4f, 1f));
            handleImg.type = Image.Type.Sliced;

            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            Debug.Log("[UIManager] Achievements panel created with tabs");
        }

        private void CreateAchievementTab(Transform parent, string tabId, string label)
        {
            var tabGO = new GameObject($"Tab_{tabId}");
            tabGO.transform.SetParent(parent, false);

            var tabLayout = tabGO.AddComponent<LayoutElement>();
            tabLayout.minHeight = 60;
            tabLayout.preferredHeight = 60;

            var tabImg = tabGO.AddComponent<Image>();
            tabImg.sprite = CreateRoundedRectSprite(100, 60, 10, new Color(0.2f, 0.19f, 0.25f, 1f));
            tabImg.type = Image.Type.Sliced;

            var tabBtn = tabGO.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;

            // Capture tab value for closure
            var capturedTab = tabId;
            tabBtn.onClick.AddListener(() => OnAchievementTabClicked(capturedTab));

            var tabTextGO = new GameObject("Text");
            tabTextGO.transform.SetParent(tabGO.transform, false);
            var tabTextRect = tabTextGO.AddComponent<RectTransform>();
            tabTextRect.anchorMin = Vector2.zero;
            tabTextRect.anchorMax = Vector2.one;
            tabTextRect.offsetMin = new Vector2(10, 0);
            tabTextRect.offsetMax = new Vector2(-10, 0);

            var tabText = tabTextGO.AddComponent<TextMeshProUGUI>();
            tabText.text = label;
            tabText.fontSize = 22;
            tabText.fontStyle = FontStyles.Bold;
            tabText.alignment = TextAlignmentOptions.MidlineLeft;
            tabText.color = Color.white;

            achievementTabButtons[tabId] = tabBtn;
            achievementTabImages[tabId] = tabImg;
        }

        private void OnAchievementTabClicked(string tab)
        {
            AudioManager.Instance?.PlayButtonClick();
            currentAchievementTab = tab;
            UpdateAchievementTabVisuals();
            RefreshAchievementsPanel();
        }

        private void UpdateAchievementTabVisuals()
        {
            foreach (var kvp in achievementTabImages)
            {
                bool isSelected = kvp.Key == currentAchievementTab;
                kvp.Value.color = isSelected
                    ? new Color(0.3f, 0.5f, 0.3f, 1f)  // Green for selected
                    : new Color(0.2f, 0.19f, 0.25f, 1f); // Dark for unselected
            }
        }

        private void PopulateAchievementTabs()
        {
            if (achievementTabsContainer == null || AchievementManager.Instance == null) return;

            // Clear existing tabs
            foreach (Transform child in achievementTabsContainer)
            {
                Destroy(child.gameObject);
            }
            achievementTabButtons.Clear();
            achievementTabImages.Clear();

            // Create tabs from AchievementManager
            foreach (var tabId in AchievementManager.Instance.AvailableTabs)
            {
                string displayName = Achievement.GetTabDisplayName(tabId);
                CreateAchievementTab(achievementTabsContainer, tabId, displayName);
            }
        }

        private void ShowAchievements()
        {
            if (achievementsPanel == null) return;

            currentAchievementTab = Achievement.TAB_ALL;
            expandedGroups.Clear();
            PopulateAchievementTabs();
            UpdateAchievementTabVisuals();
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

            // Get filtered achievements
            var achievements = AchievementManager.Instance.GetAchievementsByTab(currentAchievementTab);
            int unlockedCount = AchievementManager.Instance.GetUnlockedCount();
            int totalCount = AchievementManager.Instance.GetTotalCount();
            int earnedPoints = AchievementManager.Instance.GetEarnedPoints();
            int totalPoints = AchievementManager.Instance.GetTotalPoints();

            // Update header texts
            if (achievementsCountText != null)
            {
                achievementsCountText.text = $"<size=24>UNLOCKED</size>\n{unlockedCount} / {totalCount}";
            }
            if (achievementsPointsText != null)
            {
                achievementsPointsText.text = $"<size=24>POINTS</size>\n<color=#FFD700>{earnedPoints}</color> / {totalPoints}";
            }

            // Group achievements and track which groups we've processed
            var processedGroups = new HashSet<string>();
            var ungroupedAchievements = new List<Achievement>();

            foreach (var achievement in achievements)
            {
                // Skip hidden achievements that aren't unlocked
                if (achievement.isHidden)
                {
                    var prog = AchievementManager.Instance.GetProgress(achievement.id);
                    if (prog == null || !prog.isUnlocked) continue;
                }

                if (!string.IsNullOrEmpty(achievement.groupId))
                {
                    if (!processedGroups.Contains(achievement.groupId))
                    {
                        processedGroups.Add(achievement.groupId);
                        CreateAchievementGroup(achievement.groupId);
                    }
                }
                else
                {
                    ungroupedAchievements.Add(achievement);
                }
            }

            // Add ungrouped achievements
            foreach (var achievement in ungroupedAchievements)
            {
                CreateAchievementEntry(achievement);
            }

            Debug.Log($"[UIManager] Refreshed achievements: {processedGroups.Count} groups, {ungroupedAchievements.Count} ungrouped");
        }

        private void CreateAchievementGroup(string groupId)
        {
            var groupAchievements = AchievementManager.Instance.GetAchievementsByGroup(groupId);
            if (groupAchievements.Count == 0) return;

            // Filter by current tab
            groupAchievements = groupAchievements.FindAll(a =>
                currentAchievementTab == Achievement.TAB_ALL || a.tab == currentAchievementTab);
            if (groupAchievements.Count == 0) return;

            string groupName = AchievementManager.Instance.GetGroupDisplayName(groupId);
            bool isExpanded = expandedGroups.Contains(groupId);

            // Calculate group progress
            int completedInGroup = 0;
            int totalPoints = 0;
            int earnedPoints = 0;
            int maxTarget = 0;
            int currentProgress = 0;

            foreach (var a in groupAchievements)
            {
                var prog = AchievementManager.Instance.GetProgress(a.id);
                if (prog != null && prog.isUnlocked)
                {
                    completedInGroup++;
                    earnedPoints += a.points;
                }
                totalPoints += a.points;
                if (a.targetValue > maxTarget) maxTarget = a.targetValue;
                if (prog != null && prog.currentValue > currentProgress)
                    currentProgress = prog.currentValue;
            }

            // Get highest tier for display
            var highestTier = groupAchievements[groupAchievements.Count - 1].tier;
            Color tierColor = GetTierColor(highestTier);

            // Group container
            var groupGO = new GameObject($"Group_{groupId}");
            groupGO.transform.SetParent(achievementsListContent, false);
            var groupRect = groupGO.AddComponent<RectTransform>();

            int baseHeight = 160; // Increased to fit date labels above milestones
            int expandedHeight = baseHeight + (isExpanded ? groupAchievements.Count * 90 : 0);

            // Set RectTransform size explicitly (required for VerticalLayoutGroup positioning)
            groupRect.sizeDelta = new Vector2(0, expandedHeight);

            var groupLayout = groupGO.AddComponent<LayoutElement>();
            groupLayout.minHeight = expandedHeight;
            groupLayout.preferredHeight = expandedHeight;

            // Group header (clickable to expand/collapse)
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(groupGO.transform, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 160);

            var headerBg = headerGO.AddComponent<Image>();
            headerBg.sprite = CreateRoundedRectSprite(100, 100, 12, new Color(0.18f, 0.17f, 0.22f, 1f));
            headerBg.type = Image.Type.Sliced;

            var headerBtn = headerGO.AddComponent<Button>();
            headerBtn.targetGraphic = headerBg;
            var capturedGroupId = groupId;
            headerBtn.onClick.AddListener(() => ToggleAchievementGroup(capturedGroupId));

            // Expand/collapse indicator
            var expandGO = new GameObject("ExpandIcon");
            expandGO.transform.SetParent(headerGO.transform, false);
            var expandRect = expandGO.AddComponent<RectTransform>();
            expandRect.anchorMin = new Vector2(0, 0.5f);
            expandRect.anchorMax = new Vector2(0, 0.5f);
            expandRect.pivot = new Vector2(0, 0.5f);
            expandRect.anchoredPosition = new Vector2(15, 0);
            expandRect.sizeDelta = new Vector2(30, 30);

            var expandText = expandGO.AddComponent<TextMeshProUGUI>();
            expandText.text = isExpanded ? "\u25BC" : "\u25B6"; // Down or right arrow
            expandText.fontSize = 24;
            expandText.alignment = TextAlignmentOptions.Center;
            expandText.color = Color.white;

            // Group name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(headerGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.55f);
            nameRect.anchorMax = new Vector2(0.6f, 1);
            nameRect.offsetMin = new Vector2(50, 0);
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = groupName;
            nameText.fontSize = 26;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = tierColor;

            // Points badge
            var pointsGO = new GameObject("Points");
            pointsGO.transform.SetParent(headerGO.transform, false);
            var pointsRect = pointsGO.AddComponent<RectTransform>();
            pointsRect.anchorMin = new Vector2(1, 0.55f);
            pointsRect.anchorMax = new Vector2(1, 1);
            pointsRect.pivot = new Vector2(1, 0.5f);
            pointsRect.anchoredPosition = new Vector2(-15, 0);
            pointsRect.sizeDelta = new Vector2(120, 40);

            var pointsText = pointsGO.AddComponent<TextMeshProUGUI>();
            pointsText.text = $"<color=#FFD700>{earnedPoints}</color>/{totalPoints} pts";
            pointsText.fontSize = 20;
            pointsText.alignment = TextAlignmentOptions.MidlineRight;
            pointsText.color = Color.white;

            // Multi-tier progress bar with markers
            var progressAreaGO = new GameObject("ProgressArea");
            progressAreaGO.transform.SetParent(headerGO.transform, false);
            var progressAreaRect = progressAreaGO.AddComponent<RectTransform>();
            progressAreaRect.anchorMin = new Vector2(0, 0);
            progressAreaRect.anchorMax = new Vector2(1, 0.55f);
            progressAreaRect.offsetMin = new Vector2(50, 10);
            progressAreaRect.offsetMax = new Vector2(-15, 0);

            // Progress bar background - positioned in lower portion to leave room for date labels
            var progressBgGO = new GameObject("ProgressBg");
            progressBgGO.transform.SetParent(progressAreaGO.transform, false);
            var progressBgRect = progressBgGO.AddComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0, 0);
            progressBgRect.anchorMax = new Vector2(1, 0.5f);
            progressBgRect.offsetMin = Vector2.zero;
            progressBgRect.offsetMax = Vector2.zero;

            var progressBgImg = progressBgGO.AddComponent<Image>();
            progressBgImg.sprite = CreateRoundedRectSprite(100, 20, 8, new Color(0.1f, 0.1f, 0.12f, 1f));
            progressBgImg.type = Image.Type.Sliced;

            // Progress fill (green)
            float fillPercent = maxTarget > 0 ? Mathf.Clamp01((float)currentProgress / maxTarget) : 0;
            var progressFillGO = new GameObject("ProgressFill");
            progressFillGO.transform.SetParent(progressBgGO.transform, false);
            var progressFillRect = progressFillGO.AddComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = new Vector2(fillPercent, 1);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;

            var progressFillImg = progressFillGO.AddComponent<Image>();
            progressFillImg.sprite = CreateRoundedRectSprite(100, 20, 8, new Color(0.3f, 0.7f, 0.3f, 1f));
            progressFillImg.type = Image.Type.Sliced;

            // Milestone markers
            for (int i = 0; i < groupAchievements.Count; i++)
            {
                var a = groupAchievements[i];
                float markerPos = maxTarget > 0 ? (float)a.targetValue / maxTarget : 0;

                var markerGO = new GameObject($"Marker_{i}");
                markerGO.transform.SetParent(progressBgGO.transform, false);
                var markerRect = markerGO.AddComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(markerPos, 0);
                markerRect.anchorMax = new Vector2(markerPos, 1);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.anchoredPosition = Vector2.zero;
                markerRect.sizeDelta = new Vector2(24, 24);

                var prog = AchievementManager.Instance.GetProgress(a.id);
                bool isComplete = prog != null && prog.isUnlocked;
                Color markerTierColor = GetTierColor(a.tier);

                var markerImg = markerGO.AddComponent<Image>();
                markerImg.sprite = CreateRoundedRectSprite(24, 24, 12, isComplete ? markerTierColor : new Color(0.3f, 0.3f, 0.35f, 1f));

                // Checkmark or number
                var markerTextGO = new GameObject("Text");
                markerTextGO.transform.SetParent(markerGO.transform, false);
                var markerTextRect = markerTextGO.AddComponent<RectTransform>();
                markerTextRect.anchorMin = Vector2.zero;
                markerTextRect.anchorMax = Vector2.one;
                markerTextRect.offsetMin = Vector2.zero;
                markerTextRect.offsetMax = Vector2.zero;

                var markerText = markerTextGO.AddComponent<TextMeshProUGUI>();
                markerText.text = isComplete ? "\u2713" : a.targetValue.ToString();
                markerText.fontSize = 14;
                markerText.fontStyle = FontStyles.Bold;
                markerText.alignment = TextAlignmentOptions.Center;
                markerText.color = isComplete ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);

                // Date label above marker (only if completed)
                if (isComplete && prog != null)
                {
                    var dateLabelGO = new GameObject("DateLabel");
                    dateLabelGO.transform.SetParent(markerGO.transform, false);
                    var dateLabelRect = dateLabelGO.AddComponent<RectTransform>();
                    dateLabelRect.anchorMin = new Vector2(0.5f, 1);
                    dateLabelRect.anchorMax = new Vector2(0.5f, 1);
                    dateLabelRect.pivot = new Vector2(0.5f, 0);
                    dateLabelRect.anchoredPosition = new Vector2(0, 6);
                    dateLabelRect.sizeDelta = new Vector2(100, 30);

                    var dateText = dateLabelGO.AddComponent<TextMeshProUGUI>();
                    dateText.text = prog.unlockedAt.ToString("yyyy-MM-dd");
                    dateText.fontSize = 16;
                    dateText.fontStyle = FontStyles.Bold;
                    dateText.alignment = TextAlignmentOptions.Center;
                    dateText.color = markerTierColor;
                    dateText.enableWordWrapping = false;
                    dateText.overflowMode = TextOverflowModes.Overflow;
                }
            }

            // Expanded content - individual achievements
            if (isExpanded)
            {
                for (int i = 0; i < groupAchievements.Count; i++)
                {
                    var a = groupAchievements[i];
                    CreateGroupedAchievementEntry(groupGO.transform, a, i, baseHeight);
                }
            }
        }

        private void CreateGroupedAchievementEntry(Transform parent, Achievement achievement, int index, int yOffset)
        {
            var progress = AchievementManager.Instance.GetProgress(achievement.id);
            bool isUnlocked = progress != null && progress.isUnlocked;
            Color tierColor = GetTierColor(achievement.tier);

            var entryGO = new GameObject($"Entry_{achievement.id}");
            entryGO.transform.SetParent(parent, false);
            var entryRect = entryGO.AddComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, 1);
            entryRect.anchorMax = new Vector2(1, 1);
            entryRect.pivot = new Vector2(0.5f, 1);
            entryRect.anchoredPosition = new Vector2(0, -(yOffset + index * 90));
            entryRect.sizeDelta = new Vector2(-40, 85);

            var entryBg = entryGO.AddComponent<Image>();
            Color bgColor = isUnlocked ? new Color(tierColor.r * 0.2f, tierColor.g * 0.2f, tierColor.b * 0.2f, 0.8f)
                                       : new Color(0.12f, 0.11f, 0.14f, 0.8f);
            entryBg.sprite = CreateRoundedRectSprite(100, 80, 8, bgColor);
            entryBg.type = Image.Type.Sliced;

            // Tier indicator
            var tierGO = new GameObject("Tier");
            tierGO.transform.SetParent(entryGO.transform, false);
            var tierRect = tierGO.AddComponent<RectTransform>();
            tierRect.anchorMin = new Vector2(0, 0);
            tierRect.anchorMax = new Vector2(0, 1);
            tierRect.pivot = new Vector2(0, 0.5f);
            tierRect.anchoredPosition = new Vector2(10, 0);
            tierRect.sizeDelta = new Vector2(50, 50);
            tierRect.offsetMin = new Vector2(10, 15);
            tierRect.offsetMax = new Vector2(60, -15);

            var tierImg = tierGO.AddComponent<Image>();
            tierImg.sprite = CreateRoundedRectSprite(50, 50, 8, tierColor);

            var tierTextGO = new GameObject("Text");
            tierTextGO.transform.SetParent(tierGO.transform, false);
            var tierTextRect = tierTextGO.AddComponent<RectTransform>();
            tierTextRect.anchorMin = Vector2.zero;
            tierTextRect.anchorMax = Vector2.one;
            tierTextRect.offsetMin = Vector2.zero;
            tierTextRect.offsetMax = Vector2.zero;

            var tierText = tierTextGO.AddComponent<TextMeshProUGUI>();
            tierText.text = isUnlocked ? "\u2713" : achievement.tier.ToString().Substring(0, 1);
            tierText.fontSize = 24;
            tierText.fontStyle = FontStyles.Bold;
            tierText.alignment = TextAlignmentOptions.Center;
            tierText.color = Color.white;

            // Name and description
            var textAreaGO = new GameObject("TextArea");
            textAreaGO.transform.SetParent(entryGO.transform, false);
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0, 0);
            textAreaRect.anchorMax = new Vector2(1, 1);
            textAreaRect.offsetMin = new Vector2(70, 5);
            textAreaRect.offsetMax = new Vector2(-120, -5);

            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(textAreaGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = achievement.name;
            nameText.fontSize = 22;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = isUnlocked ? tierColor : new Color(0.6f, 0.6f, 0.6f, 1f);

            var descGO = new GameObject("Desc");
            descGO.transform.SetParent(textAreaGO.transform, false);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            var descText = descGO.AddComponent<TextMeshProUGUI>();
            descText.text = achievement.description;
            descText.fontSize = 16;
            descText.alignment = TextAlignmentOptions.MidlineLeft;
            descText.color = new Color(0.5f, 0.5f, 0.5f, 1f);

            // Points and date
            var infoGO = new GameObject("Info");
            infoGO.transform.SetParent(entryGO.transform, false);
            var infoRect = infoGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 0);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(1, 0.5f);
            infoRect.anchoredPosition = new Vector2(-10, 0);
            infoRect.sizeDelta = new Vector2(100, 0);

            var infoText = infoGO.AddComponent<TextMeshProUGUI>();
            if (isUnlocked)
            {
                string dateStr = progress.unlockedAt.ToString("yyyy-MM-dd");
                infoText.text = $"<color=#FFD700>{achievement.points} pts</color>\n<size=14>{dateStr}</size>";
            }
            else
            {
                infoText.text = $"<color=#888888>{achievement.points} pts</color>";
            }
            infoText.fontSize = 18;
            infoText.alignment = TextAlignmentOptions.MidlineRight;
            infoText.color = Color.white;
        }

        private void ToggleAchievementGroup(string groupId)
        {
            AudioManager.Instance?.PlayButtonClick();
            if (expandedGroups.Contains(groupId))
                expandedGroups.Remove(groupId);
            else
                expandedGroups.Add(groupId);
            RefreshAchievementsPanel();
        }

        private void CreateAchievementEntry(Achievement achievement)
        {
            var progress = AchievementManager.Instance.GetProgress(achievement.id);
            bool isUnlocked = progress != null && progress.isUnlocked;
            int currentValue = progress?.currentValue ?? 0;

            Color tierColor = GetTierColor(achievement.tier);
            Color bgColor = isUnlocked ? new Color(tierColor.r * 0.25f, tierColor.g * 0.25f, tierColor.b * 0.25f, 1f)
                                       : new Color(0.14f, 0.13f, 0.17f, 1f);

            var entryGO = new GameObject($"Achievement_{achievement.id}");
            entryGO.transform.SetParent(achievementsListContent, false);
            var entryRect = entryGO.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(0, 110);

            var entryLayout = entryGO.AddComponent<LayoutElement>();
            entryLayout.minHeight = 110;
            entryLayout.preferredHeight = 110;

            var entryBg = entryGO.AddComponent<Image>();
            entryBg.sprite = CreateRoundedRectSprite(100, 100, 12, bgColor);
            entryBg.type = Image.Type.Sliced;

            // Tier accent
            var accentGO = new GameObject("TierAccent");
            accentGO.transform.SetParent(entryGO.transform, false);
            var accentRect = accentGO.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(6, 0);
            accentRect.offsetMin = new Vector2(0, 8);
            accentRect.offsetMax = new Vector2(6, -8);

            var accentImg = accentGO.AddComponent<Image>();
            accentImg.color = tierColor;

            // Icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(entryGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(18, 0);
            iconRect.sizeDelta = new Vector2(60, 60);

            var iconBg = iconGO.AddComponent<Image>();
            iconBg.sprite = CreateRoundedRectSprite(60, 60, 8, tierColor);

            var iconTextGO = new GameObject("IconText");
            iconTextGO.transform.SetParent(iconGO.transform, false);
            var iconTextRect = iconTextGO.AddComponent<RectTransform>();
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.offsetMin = Vector2.zero;
            iconTextRect.offsetMax = Vector2.zero;

            var iconText = iconTextGO.AddComponent<TextMeshProUGUI>();
            iconText.text = isUnlocked ? "\u2713" : "\u2605";
            iconText.fontSize = 32;
            iconText.fontStyle = FontStyles.Bold;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = isUnlocked ? Color.white : new Color(0.2f, 0.15f, 0.1f, 1f);

            // Text area
            var textAreaGO = new GameObject("TextArea");
            textAreaGO.transform.SetParent(entryGO.transform, false);
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0, 0);
            textAreaRect.anchorMax = new Vector2(1, 1);
            textAreaRect.offsetMin = new Vector2(90, 8);
            textAreaRect.offsetMax = new Vector2(-110, -8);

            // Name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(textAreaGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.6f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = achievement.name;
            nameText.fontSize = 24;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = isUnlocked ? tierColor : new Color(0.7f, 0.7f, 0.7f, 1f);

            // Description
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(textAreaGO.transform, false);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.3f);
            descRect.anchorMax = new Vector2(1, 0.6f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            var descText = descGO.AddComponent<TextMeshProUGUI>();
            descText.text = achievement.description;
            descText.fontSize = 18;
            descText.alignment = TextAlignmentOptions.MidlineLeft;
            descText.color = new Color(0.55f, 0.55f, 0.55f, 1f);

            // Progress or status
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(textAreaGO.transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.3f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            if (!isUnlocked)
            {
                // Progress bar (green)
                var progressBgGO = new GameObject("ProgressBg");
                progressBgGO.transform.SetParent(statusGO.transform, false);
                var progressBgRect = progressBgGO.AddComponent<RectTransform>();
                progressBgRect.anchorMin = new Vector2(0, 0.2f);
                progressBgRect.anchorMax = new Vector2(0.7f, 0.8f);
                progressBgRect.offsetMin = Vector2.zero;
                progressBgRect.offsetMax = Vector2.zero;

                var progressBgImg = progressBgGO.AddComponent<Image>();
                progressBgImg.sprite = CreateRoundedRectSprite(100, 16, 6, new Color(0.1f, 0.1f, 0.12f, 1f));
                progressBgImg.type = Image.Type.Sliced;

                float fillPct = achievement.targetValue > 0 ? Mathf.Clamp01((float)currentValue / achievement.targetValue) : 0;
                var progressFillGO = new GameObject("ProgressFill");
                progressFillGO.transform.SetParent(progressBgGO.transform, false);
                var progressFillRect = progressFillGO.AddComponent<RectTransform>();
                progressFillRect.anchorMin = Vector2.zero;
                progressFillRect.anchorMax = new Vector2(fillPct, 1);
                progressFillRect.offsetMin = Vector2.zero;
                progressFillRect.offsetMax = Vector2.zero;

                var progressFillImg = progressFillGO.AddComponent<Image>();
                progressFillImg.sprite = CreateRoundedRectSprite(100, 16, 6, new Color(0.3f, 0.7f, 0.3f, 1f));
                progressFillImg.type = Image.Type.Sliced;

                var progressTextGO = new GameObject("ProgressText");
                progressTextGO.transform.SetParent(statusGO.transform, false);
                var progressTextRect = progressTextGO.AddComponent<RectTransform>();
                progressTextRect.anchorMin = new Vector2(0.72f, 0);
                progressTextRect.anchorMax = new Vector2(1, 1);
                progressTextRect.offsetMin = Vector2.zero;
                progressTextRect.offsetMax = Vector2.zero;

                var progressText = progressTextGO.AddComponent<TextMeshProUGUI>();
                progressText.text = $"{currentValue}/{achievement.targetValue}";
                progressText.fontSize = 16;
                progressText.alignment = TextAlignmentOptions.MidlineLeft;
                progressText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            // Points and date (right side)
            var infoGO = new GameObject("Info");
            infoGO.transform.SetParent(entryGO.transform, false);
            var infoRect = infoGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 0);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(1, 0.5f);
            infoRect.anchoredPosition = new Vector2(-12, 0);
            infoRect.sizeDelta = new Vector2(90, 0);

            var infoText = infoGO.AddComponent<TextMeshProUGUI>();
            if (isUnlocked)
            {
                string dateStr = progress.unlockedAt.ToString("yyyy-MM-dd");
                infoText.text = $"<color=#FFD700>{achievement.points}</color>\n<size=14>{dateStr}</size>";
            }
            else
            {
                infoText.text = $"<color=#666666>{achievement.points}</color>\n<size=14>pts</size>";
            }
            infoText.fontSize = 22;
            infoText.fontStyle = FontStyles.Bold;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.color = Color.white;
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

        private string GetRewardString(AchievementReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0) return "";

            var parts = new List<string>();
            foreach (var reward in rewards)
            {
                switch (reward.type)
                {
                    case RewardType.Coins:
                        parts.Add($"<color=#FFD700>{reward.amount} Coins</color>");
                        break;
                    case RewardType.UndoToken:
                        parts.Add($"<color=#4FC3F7>+{reward.amount} Undo</color>");
                        break;
                    case RewardType.SkipToken:
                        parts.Add($"<color=#81C784>+{reward.amount} Skip</color>");
                        break;
                    case RewardType.Trophy:
                        parts.Add("<color=#E1BEE7>Trophy</color>");
                        break;
                    default:
                        parts.Add($"+{reward.amount} {reward.type}");
                        break;
                }
            }
            return string.Join("  ", parts);
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

            // Semi-transparent overlay (optional - can be disabled)
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(dialoguePanel.transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.3f);

            // Dialogue box container (bottom of screen)
            var dialogueBox = new GameObject("Dialogue Box");
            dialogueBox.transform.SetParent(dialoguePanel.transform, false);
            var boxRect = dialogueBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0, 0);
            boxRect.anchorMax = new Vector2(1, 0);
            boxRect.pivot = new Vector2(0.5f, 0);
            boxRect.anchoredPosition = new Vector2(0, 50);
            boxRect.sizeDelta = new Vector2(-60, 350); // Full width minus margins, 350px tall

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
                dialogueBoxImage.color = new Color(0.2f, 0.15f, 0.1f, 0.95f);
            }

            // Mascot image (left side, overlapping box)
            var mascotContainer = new GameObject("Mascot Container");
            mascotContainer.transform.SetParent(dialogueBox.transform, false);
            var mascotRect = mascotContainer.AddComponent<RectTransform>();
            mascotRect.anchorMin = new Vector2(0, 0.5f);
            mascotRect.anchorMax = new Vector2(0, 0.5f);
            mascotRect.pivot = new Vector2(0, 0.5f);
            mascotRect.anchoredPosition = new Vector2(20, 50);
            mascotRect.sizeDelta = new Vector2(200, 280);

            dialogueMascotImage = mascotContainer.AddComponent<Image>();
            dialogueMascotImage.preserveAspect = true;
            dialogueMascotImage.enabled = false; // Hidden until mascot set

            // Text content area (right side of mascot)
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(dialogueBox.transform, false);
            var textRect = textArea.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(240, 30);  // Left offset for mascot
            textRect.offsetMax = new Vector2(-30, -30); // Right and top padding

            // Name label (speaker name)
            var nameContainer = new GameObject("Name Container");
            nameContainer.transform.SetParent(dialogueBox.transform, false);
            var nameRect = nameContainer.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(0, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.anchoredPosition = new Vector2(240, -15);
            nameRect.sizeDelta = new Vector2(250, 50);

            // Name background
            var nameBg = nameContainer.AddComponent<Image>();
            nameBg.color = new Color(0.6f, 0.4f, 0.2f, 1f);

            // Name text
            var nameTextGO = new GameObject("Name Text");
            nameTextGO.transform.SetParent(nameContainer.transform, false);
            var nameTextRect = nameTextGO.AddComponent<RectTransform>();
            nameTextRect.anchorMin = Vector2.zero;
            nameTextRect.anchorMax = Vector2.one;
            nameTextRect.offsetMin = new Vector2(15, 5);
            nameTextRect.offsetMax = new Vector2(-15, -5);

            dialogueNameText = nameTextGO.AddComponent<TextMeshProUGUI>();
            dialogueNameText.text = "Speaker";
            dialogueNameText.fontSize = 28;
            dialogueNameText.fontStyle = FontStyles.Bold;
            dialogueNameText.color = Color.white;
            dialogueNameText.alignment = TextAlignmentOptions.Left;

            // Dialogue text
            var dialogueTextGO = new GameObject("Dialogue Text");
            dialogueTextGO.transform.SetParent(textArea.transform, false);
            var dialogueTextRect = dialogueTextGO.AddComponent<RectTransform>();
            dialogueTextRect.anchorMin = Vector2.zero;
            dialogueTextRect.anchorMax = Vector2.one;
            dialogueTextRect.offsetMin = new Vector2(10, 10);
            dialogueTextRect.offsetMax = new Vector2(-10, -50);

            dialogueText = dialogueTextGO.AddComponent<TextMeshProUGUI>();
            dialogueText.text = "";
            dialogueText.fontSize = 32;
            dialogueText.color = Color.white;
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
            continueText.text = "Tap to continue >";
            continueText.fontSize = 20;
            continueText.color = new Color(1, 1, 1, 0.7f);
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
            uiType.GetField("nameText", flags)?.SetValue(dialogueUI, dialogueNameText);
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
