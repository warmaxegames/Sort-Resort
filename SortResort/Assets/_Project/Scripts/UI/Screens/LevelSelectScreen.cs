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

        // Cached sprites for level portals
        private Sprite portalSprite;
        private Sprite[] starSprites = new Sprite[4]; // 0 = none, 1-3 = star count

        // Mode tab colors
        private static readonly Color[] ModeColors = {
            new Color(0.3f, 0.8f, 0.3f, 1f),   // FreePlay - green
            new Color(0.9f, 0.4f, 0.6f, 1f),   // StarMode - pink
            new Color(0.3f, 0.6f, 0.9f, 1f),   // TimerMode - blue
            new Color(0.85f, 0.2f, 0.2f, 1f)   // HardMode - red
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

        private void LoadPortalSprites()
        {
            portalSprite = Resources.Load<Sprite>("Sprites/UI/Icons/level_portal");
            if (portalSprite == null)
            {
                Debug.LogError("[LevelSelectScreen] level_portal.png NOT FOUND at Sprites/UI/Icons/level_portal");
                portalSprite = Resources.Load<Sprite>("UI/Icons/level_portal");
                if (portalSprite != null)
                    Debug.Log("[LevelSelectScreen] Found portal at alternate path UI/Icons/level_portal");
            }

            starSprites[0] = null;
            starSprites[1] = Resources.Load<Sprite>("Sprites/UI/Icons/portal_1_stars");
            starSprites[2] = Resources.Load<Sprite>("Sprites/UI/Icons/portal_2_stars");
            starSprites[3] = Resources.Load<Sprite>("Sprites/UI/Icons/portal_3_stars");

            Debug.Log($"[LevelSelectScreen] Loaded sprites - portal:{portalSprite != null}, star1:{starSprites[1] != null}, star2:{starSprites[2] != null}, star3:{starSprites[3] != null}");
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
        // PORTAL TINT COLOR
        // ============================================

        private Color GetPortalTintForMode()
        {
            return ModeColors[(int)selectedMode];
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

            // Background - use portal sprite
            var bgImage = btnGO.AddComponent<Image>();
            if (portalSprite != null)
            {
                bgImage.sprite = portalSprite;
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

            // Stars overlay image (used in Star/Hard modes)
            var starsGO = new GameObject("StarsOverlay");
            starsGO.transform.SetParent(btnGO.transform, false);
            var starsRect = starsGO.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.11f, 0.22f);
            starsRect.anchorMax = new Vector2(0.89f, 0.36f);
            starsRect.offsetMin = Vector2.zero;
            starsRect.offsetMax = Vector2.zero;

            var starsImage = starsGO.AddComponent<Image>();
            starsImage.preserveAspect = true;
            starsImage.enabled = false;

            // Info text (for timer best time or checkmark)
            var infoGO = new GameObject("InfoText");
            infoGO.transform.SetParent(btnGO.transform, false);
            var infoRect = infoGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.1f, 0.05f);
            infoRect.anchorMax = new Vector2(0.9f, 0.22f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            var infoText = infoGO.AddComponent<TextMeshProUGUI>();
            infoText.text = "";
            infoText.fontSize = 22;
            infoText.fontStyle = FontStyles.Bold;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.color = Color.white;
            infoText.enabled = false;

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
            levelBtn.Initialize(levelNumber, button, bgImage, levelText, starsImage, infoText, lockGO, starSprites);

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
            PortalAnimation.Instance.Play(buttonRect, () =>
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
            Color portalTint = GetPortalTintForMode();

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

                levelButtons[i].UpdateState(isUnlocked, stars, bestTime, isCompleted, selectedMode, portalTint);
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
    /// Supports mode-specific display: stars, best time, checkmark, portal tinting.
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        private int levelNumber;
        private Button button;
        private Image backgroundImage;
        private TextMeshProUGUI levelText;
        private Image starsOverlay;
        private TextMeshProUGUI infoText;
        private GameObject lockOverlay;
        private Sprite[] starSprites;

        public void Initialize(int level, Button btn, Image bg, TextMeshProUGUI text, Image starsImg, TextMeshProUGUI info, GameObject lockObj, Sprite[] stars)
        {
            levelNumber = level;
            button = btn;
            backgroundImage = bg;
            levelText = text;
            starsOverlay = starsImg;
            infoText = info;
            lockOverlay = lockObj;
            starSprites = stars;
        }

        public void UpdateState(bool isUnlocked, int starsEarned, float bestTime, bool isCompleted, GameMode mode, Color portalTint)
        {
            // Lock overlay
            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            // Button interactable
            if (button != null)
                button.interactable = isUnlocked;

            // Portal tint: mode color when unlocked, grey when locked
            if (backgroundImage != null)
            {
                backgroundImage.color = isUnlocked ? portalTint : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            // Text color
            if (levelText != null)
            {
                levelText.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            // Stars overlay - only in StarMode and HardMode
            if (starsOverlay != null && starSprites != null)
            {
                bool showStars = (mode == GameMode.StarMode || mode == GameMode.HardMode);

                if (!isUnlocked || !showStars)
                {
                    starsOverlay.enabled = false;
                }
                else if (starsEarned > 0 && starsEarned <= 3 && starSprites[starsEarned] != null)
                {
                    starsOverlay.sprite = starSprites[starsEarned];
                    starsOverlay.enabled = true;
                    starsOverlay.color = Color.white;
                }
                else
                {
                    starsOverlay.enabled = false;
                }
            }

            // Info text - mode-specific
            if (infoText != null)
            {
                if (!isUnlocked || !isCompleted)
                {
                    infoText.enabled = false;
                }
                else
                {
                    switch (mode)
                    {
                        case GameMode.FreePlay:
                            // Show "Done" for completed levels
                            infoText.text = "Done";
                            infoText.fontSize = 22;
                            infoText.color = new Color(0.3f, 1f, 0.3f, 1f);
                            infoText.enabled = true;
                            break;

                        case GameMode.TimerMode:
                            // Show best time in M:SS.CC format
                            if (bestTime > 0)
                            {
                                int minutes = (int)(bestTime / 60);
                                int seconds = (int)(bestTime % 60);
                                int cs = (int)((bestTime % 1f) * 100f);
                                infoText.text = $"{minutes}:{seconds:D2}.{cs:D2}";
                                infoText.fontSize = 18;
                                infoText.color = new Color(0.7f, 0.9f, 1f, 1f);
                                infoText.enabled = true;
                            }
                            else
                            {
                                infoText.enabled = false;
                            }
                            break;

                        case GameMode.HardMode:
                            // Show best time below stars in M:SS.CC format
                            if (bestTime > 0)
                            {
                                int min = (int)(bestTime / 60);
                                int sec = (int)(bestTime % 60);
                                int centis = (int)((bestTime % 1f) * 100f);
                                infoText.text = $"{min}:{sec:D2}.{centis:D2}";
                                infoText.fontSize = 18;
                                infoText.color = new Color(1f, 0.8f, 0.8f, 1f);
                                infoText.enabled = true;
                            }
                            else
                            {
                                infoText.enabled = false;
                            }
                            break;

                        default: // StarMode - stars handle it
                            infoText.enabled = false;
                            break;
                    }
                }
            }
        }
    }
}
