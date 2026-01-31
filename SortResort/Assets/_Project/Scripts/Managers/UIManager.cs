using System.Collections;
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
        private GameObject levelSelectPanel;
        private LevelSelectScreen levelSelectScreen;
        private GameObject settingsPanel;
        private SettingsScreen settingsScreen;
        private GameObject pauseMenuPanel;
        private TextMeshProUGUI levelTitleText;
        private TextMeshProUGUI moveCountText;
        private TextMeshProUGUI matchCountText;
        private TextMeshProUGUI itemsRemainingText;
        private Image[] starImages;

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

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged += OnItemsRemainingChanged;
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

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged -= OnItemsRemainingChanged;
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

        private void Start()
        {
            // Subscribe to LevelManager after it's initialized
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged += OnItemsRemainingChanged;
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
            CreateSettingsPanel();
            CreatePauseMenuPanel();

            // Hide all panels initially
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

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

        private void OnLevelSelectedFromMenu(string worldId, int levelNumber)
        {
            Debug.Log($"[UIManager] Level selected: {worldId} #{levelNumber}");

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
            rect.sizeDelta = new Vector2(180, 50);

            var layout = buttonsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childForceExpandWidth = false;

            // Pause button - opens pause menu
            CreateButton(buttonsGO.transform, "Pause", "II", OnPauseClicked, 50, 50);

            // Back button - exits level and returns to level select
            CreateButton(buttonsGO.transform, "Back", "Back", OnBackToLevelsClicked, 80, 50);
        }

        private void OnPauseClicked()
        {
            Debug.Log("[UIManager] Pause button clicked");
            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.PauseGame();
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

            // Dark overlay
            var overlay = levelCompletePanel.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.8f);

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
            starsRect.sizeDelta = new Vector2(300, 80);
            var starsLayout = starsContainer.AddComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 20;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childForceExpandWidth = false;

            for (int i = 0; i < 3; i++)
            {
                var star = new GameObject($"CompleteStar_{i}");
                star.transform.SetParent(starsContainer.transform, false);
                var starRect = star.AddComponent<RectTransform>();
                starRect.sizeDelta = new Vector2(60, 60);
                var starImg = star.AddComponent<Image>();
                starImg.color = Color.yellow;
            }

            // Message
            CreateTextElement(content.transform, "Message", "Great job!", 28,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 50);

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
            // BUTTONS (Reset Progress, Credits)
            // ============================================
            var buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(contentGO.transform, false);
            var buttonsContainerRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsContainerRect.anchorMin = new Vector2(0.5f, 1);
            buttonsContainerRect.anchorMax = new Vector2(0.5f, 1);
            buttonsContainerRect.pivot = new Vector2(0.5f, 1);
            buttonsContainerRect.anchoredPosition = new Vector2(0, -730);
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
                levelCompletePanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(true);

            // Update title with level info
            string worldId = GameManager.Instance?.CurrentWorldId ?? "island";
            string worldName = char.ToUpper(worldId[0]) + worldId.Substring(1);
            if (levelTitleText != null)
                levelTitleText.text = $"{worldName} - Level {levelNumber}";

            UpdateMoveDisplay(0);
            UpdateMatchDisplay(0);
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
                // Update stars display, etc.
            }
        }

        private void OnLevelRestarted()
        {
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            UpdateMoveDisplay(0);
            UpdateMatchDisplay(0);
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
            levelCompletePanel?.SetActive(false);

            // Set state back to level selection
            GameManager.Instance?.SetState(GameState.LevelSelection);

            ShowLevelSelect();

            // Clear the current level
            LevelManager.Instance?.ClearLevel();
        }

        private void OnReplayClicked()
        {
            Debug.Log("[UIManager] Replay clicked");
            levelCompletePanel?.SetActive(false);

            // Ensure state is Playing
            GameManager.Instance?.SetState(GameState.Playing);

            LevelManager.Instance?.RestartLevel();
        }

        private void OnNextLevelFromCompleteClicked()
        {
            Debug.Log("[UIManager] Next Level clicked");
            levelCompletePanel?.SetActive(false);

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
