using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace SortResort.UI
{
    /// <summary>
    /// Level selection screen with world navigation, game mode tabs, and level grid.
    /// Shows 100 levels per world in a scrollable grid with mode-specific portal colors.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        [Header("World Navigation")]
        [SerializeField] private Button prevWorldButton;
        [SerializeField] private Button nextWorldButton;
        [SerializeField] private Image worldImage;

        [Header("Level Grid")]
        [SerializeField] private Transform levelGridParent;
        [SerializeField] private ScrollRect levelScrollRect;

        [Header("Mode Tabs")]
        [SerializeField] private Transform modeTabContainer;

        [Header("Settings")]
        [SerializeField] private int levelsPerWorld = 100;

        // Cached sprites for level portals (one per game mode)
        private Sprite[] portalSprites = new Sprite[4]; // [modeIndex] = first vortex frame
        private Sprite[] starSprites = new Sprite[4]; // 0 = none, 1-3 = star count
        private Sprite timerPortalSprite;  // timer overlay for Timer/Hard modes
        private Sprite freePortalSprite;   // checkmark overlay for Free mode

        // Mode tab colors
        private static readonly Color[] ModeColors = {
            new Color(0.3f, 0.8f, 0.3f, 1f),   // FreePlay - green
            new Color(0.9f, 0.4f, 0.6f, 1f),   // StarMode - pink
            new Color(0.3f, 0.6f, 0.9f, 1f),   // TimerMode - blue
            new Color(0.85f, 0.65f, 0.13f, 1f)  // HardMode - gold
        };

        private static readonly string[] ModeNames = { "Free", "Stars", "Timer", "Hard" };

        // Mode tab UI
        private Button[] modeTabButtons = new Button[4];
        private Image[] modeTabImages = new Image[4];
        private TextMeshProUGUI[] modeTabTexts = new TextMeshProUGUI[4];
        // Runtime data
        private List<string> worldIds = new List<string> { "island", "supermarket", "farm", "tavern", "space" };
        private int currentWorldIndex = 0;
        private GameMode selectedMode = GameMode.FreePlay;
        private List<LevelButton> levelButtons = new List<LevelButton>();
        private bool isPortalAnimating = false;

        // Tooltip
        private GameObject tooltipPanel;
        private TextMeshProUGUI tooltipText;
        private CanvasGroup tooltipCanvasGroup;
        private Coroutine tooltipCoroutine;

        // Debug: hard mode override
        private HashSet<string> debugUnlockedWorlds = new HashSet<string>();
        private TextMeshProUGUI debugButtonText;
        private Transform topBarTransform;

        public System.Action<string, int> OnLevelSelected;
        private bool initialized = false;

        /// <summary>
        /// Initialize the level select screen. Called by UIManager after all references are set.
        /// </summary>
        public void Initialize()
        {
            if (initialized) return;
            initialized = true;

            Debug.Log("[LevelSelectScreen] Initialize called");

            // Load the active mode from save
            selectedMode = SaveManager.Instance?.GetActiveGameMode() ?? GameMode.FreePlay;

            LoadPortalSprites();
            SetupWorldNavigation();
            CreateModeTabs();
            CreateDebugButton();
            CreateTooltip();
            CreateLevelGrid();

            // Force layout rebuild so GridLayoutGroup positions children properly
            if (levelGridParent != null)
            {
                Canvas.ForceUpdateCanvases();
                var contentRect = levelGridParent.GetComponent<RectTransform>();
                if (contentRect != null)
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            RefreshDisplay();
            Debug.Log($"[LevelSelectScreen] Initialized with {levelButtons.Count} level buttons, mode: {selectedMode}, levelsPerWorld: {levelsPerWorld}, gridParent: {(levelGridParent != null ? levelGridParent.name : "NULL")}");
        }

        private static readonly string[] PortalFramePaths = {
            "Sprites/UI/Portal/FreePlay/portal_00000",   // FreePlay = 0
            "Sprites/UI/Portal/StarMode/portal_00000",   // StarMode = 1
            "Sprites/UI/Portal/TimerMode/portal_00000",  // TimerMode = 2
            "Sprites/UI/Portal/HardMode/portal_00000"    // HardMode = 3
        };

        private void LoadPortalSprites()
        {
            for (int i = 0; i < 4; i++)
            {
                var tex = Resources.Load<Texture2D>(PortalFramePaths[i]);
                if (tex != null)
                {
                    portalSprites[i] = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }
                else
                {
                    Debug.LogWarning($"[LevelSelectScreen] Portal frame not found at {PortalFramePaths[i]}");
                }
            }

            starSprites[0] = null;
            starSprites[1] = LoadSpriteFromTexture("Sprites/UI/Icons/1star_portal");
            starSprites[2] = LoadSpriteFromTexture("Sprites/UI/Icons/2star_portal");
            starSprites[3] = LoadSpriteFromTexture("Sprites/UI/Icons/3star_portal");
            timerPortalSprite = LoadSpriteFromTexture("Sprites/UI/Icons/timer_portal");
            freePortalSprite = LoadSpriteFromTexture("Sprites/UI/Icons/free_portal");

            Debug.Log($"[LevelSelectScreen] Loaded sprites - portals:{portalSprites[0] != null}/{portalSprites[1] != null}/{portalSprites[2] != null}/{portalSprites[3] != null}, stars:{starSprites[1] != null}/{starSprites[2] != null}/{starSprites[3] != null}, timer:{timerPortalSprite != null}, free:{freePortalSprite != null}");
        }

        private Sprite LoadSpriteFromTexture(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void SetupWorldNavigation()
        {
            if (prevWorldButton != null)
                prevWorldButton.onClick.AddListener(OnPrevWorldClicked);
            if (nextWorldButton != null)
                nextWorldButton.onClick.AddListener(OnNextWorldClicked);
        }

        // ============================================
        // MODE TABS
        // ============================================

        private void CreateModeTabs()
        {
            if (modeTabContainer == null)
            {
                Debug.LogWarning("[LevelSelectScreen] modeTabContainer is null, mode tabs will not be created");
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                GameMode mode = (GameMode)i;
                CreateModeTab(mode, i);
            }

            UpdateModeTabVisuals();
        }

        private void CreateModeTab(GameMode mode, int index)
        {
            var tabGO = new GameObject($"ModeTab_{ModeNames[index]}");
            tabGO.transform.SetParent(modeTabContainer, false);

            var tabRect = tabGO.AddComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(220, 60);

            // Tab background
            var tabImage = tabGO.AddComponent<Image>();
            tabImage.color = ModeColors[index];
            // Round corners effect via sprite or just use color block
            tabImage.type = Image.Type.Sliced;

            // Tab button
            var tabButton = tabGO.AddComponent<Button>();
            tabButton.targetGraphic = tabImage;
            var colors = tabButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            tabButton.colors = colors;

            // Tab text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(tabGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);

            var tabText = textGO.AddComponent<TextMeshProUGUI>();
            tabText.text = ModeNames[index];
            tabText.fontSize = 28;
            tabText.fontStyle = FontStyles.Bold;
            tabText.alignment = TextAlignmentOptions.Center;
            tabText.color = Color.white;
            tabText.outlineWidth = 0.2f;
            tabText.outlineColor = new Color32(0, 0, 0, 128);

            // Store references
            modeTabButtons[index] = tabButton;
            modeTabImages[index] = tabImage;
            modeTabTexts[index] = tabText;

            // Click handler
            int idx = index;
            tabButton.onClick.AddListener(() => OnModeTabClicked((GameMode)idx));
        }

        private bool IsHardModeEffectivelyUnlocked(string worldId)
        {
            if (debugUnlockedWorlds.Contains(worldId)) return true;
            return SaveManager.Instance?.IsHardModeUnlocked(worldId) ?? false;
        }

        private void OnModeTabClicked(GameMode mode)
        {
            // Check if Hard Mode is locked for this world
            if (mode == GameMode.HardMode)
            {
                string worldId = worldIds[currentWorldIndex];
                if (!IsHardModeEffectivelyUnlocked(worldId))
                {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowTooltip("Locked until you complete the Stars and Timer game modes");
                    Debug.Log($"[LevelSelectScreen] Hard Mode locked for {worldId}");
                    return;
                }
            }

            if (selectedMode == mode) return;

            selectedMode = mode;
            SaveManager.Instance?.SetActiveGameMode(mode);
            AudioManager.Instance?.PlayButtonClick();

            UpdateModeTabVisuals();
            RefreshDisplay();

            Debug.Log($"[LevelSelectScreen] Mode changed to: {mode}");
        }

        private void UpdateModeTabVisuals()
        {
            string worldId = worldIds[currentWorldIndex];
            bool hardUnlocked = IsHardModeEffectivelyUnlocked(worldId);

            for (int i = 0; i < 4; i++)
            {
                GameMode mode = (GameMode)i;
                bool isSelected = (mode == selectedMode);
                bool isLocked = (mode == GameMode.HardMode && !hardUnlocked);

                if (modeTabImages[i] != null)
                {
                    // Selected tab: full color. Unselected: dimmed. Locked: very dim.
                    if (isLocked)
                        modeTabImages[i].color = new Color(ModeColors[i].r * 0.3f, ModeColors[i].g * 0.3f, ModeColors[i].b * 0.3f, 0.6f);
                    else if (isSelected)
                        modeTabImages[i].color = ModeColors[i];
                    else
                        modeTabImages[i].color = new Color(ModeColors[i].r * 0.6f, ModeColors[i].g * 0.6f, ModeColors[i].b * 0.6f, 0.8f);
                }

                if (modeTabTexts[i] != null)
                {
                    // Show lock text on Hard if locked
                    if (isLocked)
                        modeTabTexts[i].text = ModeNames[i] + " (Locked)";
                    else
                        modeTabTexts[i].text = ModeNames[i];

                    modeTabTexts[i].color = isSelected ? Color.white : new Color(1f, 1f, 1f, 0.7f);
                }

                // Keep all buttons interactable so locked Hard tab can show tooltip
                if (modeTabButtons[i] != null)
                {
                    modeTabButtons[i].interactable = true;
                }
            }

            // Update debug button text
            UpdateDebugButtonText();
        }

        // ============================================
        // PORTAL SPRITE
        // ============================================

        private Sprite GetPortalSpriteForMode()
        {
            return portalSprites[(int)selectedMode];
        }

        // ============================================
        // WORLD NAVIGATION
        // ============================================

        private void OnPrevWorldClicked()
        {
            if (currentWorldIndex > 0)
            {
                currentWorldIndex--;
                RefreshDisplay();
                UpdateModeTabVisuals(); // Hard mode unlock may differ per world
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        private void OnNextWorldClicked()
        {
            if (currentWorldIndex < worldIds.Count - 1)
            {
                currentWorldIndex++;
                RefreshDisplay();
                UpdateModeTabVisuals();
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        // ============================================
        // LEVEL GRID
        // ============================================

        private void CreateLevelGrid()
        {
            if (levelGridParent == null)
            {
                Debug.LogError("[LevelSelectScreen] levelGridParent is null! Cannot create level buttons.");
                return;
            }

            // Clear existing buttons
            foreach (var btn in levelButtons)
            {
                if (btn != null && btn.gameObject != null)
                    Destroy(btn.gameObject);
            }
            levelButtons.Clear();

            // Create level buttons
            for (int i = 1; i <= levelsPerWorld; i++)
            {
                var levelBtn = CreateLevelButton(i);
                if (levelBtn != null)
                    levelButtons.Add(levelBtn);
            }

            Debug.Log($"[LevelSelectScreen] Created {levelButtons.Count} level buttons");
        }

        private LevelButton CreateLevelButton(int levelNumber)
        {
            var btnGO = new GameObject($"Level_{levelNumber}");
            btnGO.transform.SetParent(levelGridParent, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200);

            // Background - use mode-specific portal sprite
            var bgImage = btnGO.AddComponent<Image>();
            var modeSprite = portalSprites[(int)selectedMode];
            if (modeSprite != null)
            {
                bgImage.sprite = modeSprite;
                bgImage.type = Image.Type.Simple;
                bgImage.preserveAspect = true;
            }
            else
            {
                bgImage.color = new Color(0.3f, 0.6f, 0.9f, 1f);
            }

            // Button component
            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = bgImage;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.colors = colors;

            // Level number text
            var textGO = new GameObject("LevelNumber");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.22f);
            textRect.anchorMax = new Vector2(0.9f, 0.78f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var levelText = textGO.AddComponent<TextMeshProUGUI>();
            levelText.text = levelNumber.ToString();
            levelText.fontSize = 72;
            levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.color = Color.white;
            levelText.outlineWidth = 0.3f;
            levelText.outlineColor = new Color32(80, 0, 80, 200);

            // Result overlays container - renders above portal swirl animation (sortingOrder 5001 > 5000)
            var resultOvGO = new GameObject("ResultOverlays");
            resultOvGO.transform.SetParent(btnGO.transform, false);
            var resultOvRect = resultOvGO.AddComponent<RectTransform>();
            resultOvRect.anchorMin = Vector2.zero;
            resultOvRect.anchorMax = Vector2.one;
            resultOvRect.offsetMin = Vector2.zero;
            resultOvRect.offsetMax = Vector2.zero;
            var resultCanvas = resultOvGO.AddComponent<Canvas>();
            resultCanvas.overrideSorting = true;
            resultCanvas.sortingOrder = 5001;
            resultOvGO.AddComponent<GraphicRaycaster>();

            // Timer overlay (full portal size, used in Timer/Hard modes)
            var timerOvGO = new GameObject("TimerOverlay");
            timerOvGO.transform.SetParent(resultOvGO.transform, false);
            var timerOvRect = timerOvGO.AddComponent<RectTransform>();
            timerOvRect.anchorMin = Vector2.zero;
            timerOvRect.anchorMax = Vector2.one;
            timerOvRect.offsetMin = Vector2.zero;
            timerOvRect.offsetMax = Vector2.zero;
            var timerOvImage = timerOvGO.AddComponent<Image>();
            timerOvImage.preserveAspect = true;
            timerOvImage.raycastTarget = false;
            timerOvImage.enabled = false;

            // Completion overlay (full portal size, stars/checkmark)
            var compOvGO = new GameObject("CompletionOverlay");
            compOvGO.transform.SetParent(resultOvGO.transform, false);
            var compOvRect = compOvGO.AddComponent<RectTransform>();
            compOvRect.anchorMin = Vector2.zero;
            compOvRect.anchorMax = Vector2.one;
            compOvRect.offsetMin = Vector2.zero;
            compOvRect.offsetMax = Vector2.zero;
            var compOvImage = compOvGO.AddComponent<Image>();
            compOvImage.preserveAspect = true;
            compOvImage.raycastTarget = false;
            compOvImage.enabled = false;

            // Best time text (vertically centered in timer_portal dark rounded rectangle)
            var timeTextGO = new GameObject("BestTimeText");
            timeTextGO.transform.SetParent(resultOvGO.transform, false);
            var timeTextRect = timeTextGO.AddComponent<RectTransform>();
            timeTextRect.anchorMin = new Vector2(0.275f, 0.12f);
            timeTextRect.anchorMax = new Vector2(0.975f, 0.30f);
            timeTextRect.offsetMin = Vector2.zero;
            timeTextRect.offsetMax = Vector2.zero;
            var bestTimeText = timeTextGO.AddComponent<TextMeshProUGUI>();
            bestTimeText.text = "";
            bestTimeText.fontSize = 26;
            bestTimeText.fontStyle = FontStyles.Bold;
            bestTimeText.alignment = TextAlignmentOptions.Center;
            bestTimeText.color = Color.white;
            bestTimeText.enabled = false;

            // Lock overlay
            var lockGO = new GameObject("Lock");
            lockGO.transform.SetParent(btnGO.transform, false);
            var lockRect = lockGO.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;

            var lockBg = lockGO.AddComponent<Image>();
            lockBg.color = new Color(0, 0, 0, 0.6f);

            // Create LevelButton component
            var levelBtn = btnGO.AddComponent<LevelButton>();
            levelBtn.Initialize(levelNumber, button, bgImage, levelText, compOvImage, timerOvImage, bestTimeText, lockGO, starSprites, timerPortalSprite, freePortalSprite);

            // Connect click handler
            int lvl = levelNumber;
            button.onClick.AddListener(() => OnLevelButtonClicked(lvl));

            return levelBtn;
        }

        private void OnLevelButtonClicked(int levelNumber)
        {
            if (isPortalAnimating) return;

            string worldId = worldIds[currentWorldIndex];

            if (!IsLevelUnlocked(worldId, levelNumber))
            {
                AudioManager.Instance?.PlayButtonClick();
                return;
            }

            RectTransform buttonRect = levelButtons[levelNumber - 1].GetComponent<RectTransform>();

            isPortalAnimating = true;
            PortalAnimation.EnsureInstance();
            PortalAnimation.Instance.Play(buttonRect, selectedMode, () =>
            {
                isPortalAnimating = false;
                OnLevelSelected?.Invoke(worldId, levelNumber);
            });
        }

        // ============================================
        // REFRESH DISPLAY
        // ============================================

        public void RefreshDisplay()
        {
            string worldId = worldIds[currentWorldIndex];
            Sprite modePortalSprite = GetPortalSpriteForMode();

            // Update world sprite image
            LoadWorldImage(worldId);

            // Update navigation buttons
            if (prevWorldButton != null)
            {
                prevWorldButton.interactable = currentWorldIndex > 0;
                var img = prevWorldButton.GetComponent<Image>();
                if (img != null)
                    img.color = currentWorldIndex > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            if (nextWorldButton != null)
            {
                nextWorldButton.interactable = currentWorldIndex < worldIds.Count - 1;
                var img = nextWorldButton.GetComponent<Image>();
                if (img != null)
                    img.color = currentWorldIndex < worldIds.Count - 1 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            // Update level buttons with mode-specific data
            for (int i = 0; i < levelButtons.Count; i++)
            {
                int levelNumber = i + 1;
                bool isUnlocked = IsLevelUnlocked(worldId, levelNumber);
                int stars = GetLevelStars(worldId, levelNumber);
                float bestTime = GetLevelBestTime(worldId, levelNumber);
                bool isCompleted = SaveManager.Instance?.IsLevelCompleted(worldId, levelNumber) ?? false;

                levelButtons[i].UpdateState(isUnlocked, stars, bestTime, isCompleted, selectedMode, modePortalSprite);
            }

            // Scroll to top
            if (levelScrollRect != null)
                levelScrollRect.normalizedPosition = new Vector2(0, 1);
        }

        private void LoadWorldImage(string worldId)
        {
            if (worldImage == null) return;

            var sprite = Resources.Load<Sprite>($"Sprites/UI/Worlds/{worldId}_world");
            if (sprite == null)
                sprite = Resources.Load<Sprite>($"Sprites/UI/Worlds/resort_world");

            if (sprite != null)
            {
                worldImage.sprite = sprite;
                worldImage.enabled = true;
            }
            else
            {
                worldImage.enabled = false;
            }
        }

        private bool IsLevelUnlocked(string worldId, int levelNumber)
        {
            if (levelNumber == 1) return true;
            if (SaveManager.Instance != null)
                return SaveManager.Instance.IsLevelCompleted(worldId, levelNumber - 1);
            return false;
        }

        private int GetLevelStars(string worldId, int levelNumber)
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.GetLevelStars(worldId, levelNumber);
            return 0;
        }

        private float GetLevelBestTime(string worldId, int levelNumber)
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.GetLevelBestTime(worldId, levelNumber);
            return 0f;
        }

        public void SetWorld(string worldId)
        {
            int index = worldIds.IndexOf(worldId);
            if (index >= 0)
            {
                currentWorldIndex = index;
                RefreshDisplay();
            }
        }

        /// <summary>
        /// Set the mode tab container reference (called from UIManager).
        /// </summary>
        public void SetModeTabContainer(Transform container)
        {
            modeTabContainer = container;
        }

        // ============================================
        // TOOLTIP
        // ============================================

        private void CreateTooltip()
        {
            if (modeTabContainer == null) return;

            tooltipPanel = new GameObject("ModeTooltip");
            tooltipPanel.transform.SetParent(modeTabContainer.parent, false);
            var tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0.05f, 0.42f);
            tooltipRect.anchorMax = new Vector2(0.95f, 0.465f);
            tooltipRect.offsetMin = Vector2.zero;
            tooltipRect.offsetMax = Vector2.zero;

            var tooltipBg = tooltipPanel.AddComponent<Image>();
            tooltipBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var textGO = new GameObject("TooltipText");
            textGO.transform.SetParent(tooltipPanel.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 2);
            textRect.offsetMax = new Vector2(-10, -2);

            tooltipText = textGO.AddComponent<TextMeshProUGUI>();
            tooltipText.fontSize = 20;
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.color = Color.white;

            tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            tooltipCanvasGroup.alpha = 0f;
            tooltipPanel.SetActive(false);
        }

        private void ShowTooltip(string message)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            if (tooltipCoroutine != null)
                StopCoroutine(tooltipCoroutine);

            tooltipText.text = message;
            tooltipPanel.SetActive(true);
            tooltipCoroutine = StartCoroutine(TooltipSequence());
        }

        private IEnumerator TooltipSequence()
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                tooltipCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.2f);
                yield return null;
            }
            tooltipCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(2.5f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                tooltipCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.3f);
                yield return null;
            }
            tooltipCanvasGroup.alpha = 0f;
            tooltipPanel.SetActive(false);
            tooltipCoroutine = null;
        }

        // ============================================
        // DEBUG: HARD MODE TOGGLE
        // ============================================

        public void SetTopBar(Transform topBar)
        {
            topBarTransform = topBar;
        }

        private void CreateDebugButton()
        {
            // Place in topBar if available, otherwise fall back to level select panel
            Transform parent = topBarTransform ?? modeTabContainer?.parent;
            if (parent == null) return;

            var debugBtnGO = new GameObject("DebugHardModeBtn");
            debugBtnGO.transform.SetParent(parent, false);
            var debugRect = debugBtnGO.AddComponent<RectTransform>();

            if (topBarTransform != null)
            {
                // Position to the left of the achievement button (which is at -225)
                debugRect.anchorMin = new Vector2(1, 0.5f);
                debugRect.anchorMax = new Vector2(1, 0.5f);
                debugRect.pivot = new Vector2(1, 0.5f);
                debugRect.anchoredPosition = new Vector2(-325, 0);
                debugRect.sizeDelta = new Vector2(90, 90);
            }
            else
            {
                debugRect.anchorMin = new Vector2(0.65f, 0.96f);
                debugRect.anchorMax = new Vector2(0.98f, 0.995f);
                debugRect.offsetMin = Vector2.zero;
                debugRect.offsetMax = Vector2.zero;
            }

            var debugBg = debugBtnGO.AddComponent<Image>();
            debugBg.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);

            var debugBtn = debugBtnGO.AddComponent<Button>();
            debugBtn.targetGraphic = debugBg;
            debugBtn.onClick.AddListener(OnDebugHardModeToggle);

            var debugTextGO = new GameObject("Text");
            debugTextGO.transform.SetParent(debugBtnGO.transform, false);
            var debugTextRect = debugTextGO.AddComponent<RectTransform>();
            debugTextRect.anchorMin = Vector2.zero;
            debugTextRect.anchorMax = Vector2.one;
            debugTextRect.offsetMin = new Vector2(4, 2);
            debugTextRect.offsetMax = new Vector2(-4, -2);

            debugButtonText = debugTextGO.AddComponent<TextMeshProUGUI>();
            debugButtonText.fontSize = 16;
            debugButtonText.fontStyle = FontStyles.Bold;
            debugButtonText.alignment = TextAlignmentOptions.Center;
            debugButtonText.color = Color.white;
            UpdateDebugButtonText();
        }

        private void UpdateDebugButtonText()
        {
            if (debugButtonText == null) return;
            string worldId = worldIds[currentWorldIndex];
            bool unlocked = IsHardModeEffectivelyUnlocked(worldId);
            debugButtonText.text = unlocked ? "LOCK\nHARD" : "UNLOCK\nHARD";
            debugButtonText.color = unlocked ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
        }

        private void OnDebugHardModeToggle()
        {
            string worldId = worldIds[currentWorldIndex];
            bool currentlyUnlocked = IsHardModeEffectivelyUnlocked(worldId);

            if (currentlyUnlocked)
            {
                // Lock it: remove debug override and clear hard mode progress
                debugUnlockedWorlds.Remove(worldId);
                SaveManager.Instance?.ClearModeWorldProgress(GameMode.HardMode, worldId);

                // If currently on Hard mode, switch to FreePlay
                if (selectedMode == GameMode.HardMode)
                {
                    selectedMode = GameMode.FreePlay;
                    SaveManager.Instance?.SetActiveGameMode(GameMode.FreePlay);
                }
            }
            else
            {
                // Unlock it via debug override
                debugUnlockedWorlds.Add(worldId);
            }

            AudioManager.Instance?.PlayButtonClick();
            UpdateModeTabVisuals();
            RefreshDisplay();
            Debug.Log($"[LevelSelectScreen] Debug Hard Mode toggle: {worldId} now {(IsHardModeEffectivelyUnlocked(worldId) ? "UNLOCKED" : "LOCKED")}");
        }

        private void OnDestroy()
        {
            if (prevWorldButton != null) prevWorldButton.onClick.RemoveAllListeners();
            if (nextWorldButton != null) nextWorldButton.onClick.RemoveAllListeners();
            foreach (var btn in modeTabButtons)
            {
                if (btn != null) btn.onClick.RemoveAllListeners();
            }
            if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        }
    }

    /// <summary>
    /// Individual level button in the grid.
    /// Supports mode-specific display: star overlays, timer overlays, checkmark, best time text.
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        private int levelNumber;
        private Button button;
        private Image backgroundImage;
        private TextMeshProUGUI levelText;
        private Image completionOverlay;
        private Image timerOverlay;
        private TextMeshProUGUI bestTimeText;
        private GameObject lockOverlay;
        private Sprite[] starSprites;       // [1]=1star_portal, [2]=2star_portal, [3]=3star_portal
        private Sprite timerPortalSprite;   // timer_portal overlay
        private Sprite freePortalSprite;    // free_portal checkmark overlay

        public void Initialize(int level, Button btn, Image bg, TextMeshProUGUI text,
            Image compOv, Image timerOv, TextMeshProUGUI timeText, GameObject lockObj,
            Sprite[] stars, Sprite timerSprite, Sprite freeSprite)
        {
            levelNumber = level;
            button = btn;
            backgroundImage = bg;
            levelText = text;
            completionOverlay = compOv;
            timerOverlay = timerOv;
            bestTimeText = timeText;
            lockOverlay = lockObj;
            starSprites = stars;
            timerPortalSprite = timerSprite;
            freePortalSprite = freeSprite;
        }

        public void UpdateState(bool isUnlocked, int starsEarned, float bestTime, bool isCompleted, GameMode mode, Sprite portalSprite)
        {
            // Lock overlay
            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            // Button interactable
            if (button != null)
                button.interactable = isUnlocked;

            // Portal sprite: colored when unlocked, greyed when locked
            if (backgroundImage != null)
            {
                if (portalSprite != null)
                    backgroundImage.sprite = portalSprite;
                backgroundImage.color = isUnlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            // Text color
            if (levelText != null)
                levelText.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);

            // Reset overlays
            if (completionOverlay != null) completionOverlay.enabled = false;
            if (timerOverlay != null) timerOverlay.enabled = false;
            if (bestTimeText != null) bestTimeText.enabled = false;

            if (!isUnlocked) return;

            switch (mode)
            {
                case GameMode.FreePlay:
                    // Show free_portal checkmark when completed
                    if (isCompleted && completionOverlay != null && freePortalSprite != null)
                    {
                        completionOverlay.sprite = freePortalSprite;
                        completionOverlay.enabled = true;
                    }
                    break;

                case GameMode.StarMode:
                    // Show Nstar_portal when stars earned
                    if (starsEarned > 0 && starsEarned <= 3 && completionOverlay != null
                        && starSprites != null && starSprites[starsEarned] != null)
                    {
                        completionOverlay.sprite = starSprites[starsEarned];
                        completionOverlay.enabled = true;
                    }
                    break;

                case GameMode.TimerMode:
                    // Show timer_portal + best time text when completed
                    if (isCompleted && timerOverlay != null && timerPortalSprite != null)
                    {
                        timerOverlay.sprite = timerPortalSprite;
                        timerOverlay.enabled = true;
                    }
                    if (isCompleted && bestTime > 0 && bestTimeText != null)
                    {
                        bestTimeText.text = FormatTime(bestTime);
                        bestTimeText.color = Color.white;
                        bestTimeText.enabled = true;
                    }
                    break;

                case GameMode.HardMode:
                    // Show BOTH star overlay AND timer overlay layered
                    if (starsEarned > 0 && starsEarned <= 3 && completionOverlay != null
                        && starSprites != null && starSprites[starsEarned] != null)
                    {
                        completionOverlay.sprite = starSprites[starsEarned];
                        completionOverlay.enabled = true;
                    }
                    if (isCompleted && timerOverlay != null && timerPortalSprite != null)
                    {
                        timerOverlay.sprite = timerPortalSprite;
                        timerOverlay.enabled = true;
                    }
                    if (isCompleted && bestTime > 0 && bestTimeText != null)
                    {
                        bestTimeText.text = FormatTime(bestTime);
                        bestTimeText.color = Color.white;
                        bestTimeText.enabled = true;
                    }
                    break;
            }
        }

        private string FormatTime(float time)
        {
            int minutes = (int)(time / 60);
            int seconds = (int)(time % 60);
            int cs = (int)((time % 1f) * 100f);
            return $"{minutes}:{seconds:D2}.{cs:D2}";
        }
    }
}
