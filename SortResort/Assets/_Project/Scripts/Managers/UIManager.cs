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
        public static UIManager Instance { get; private set; }

        [Header("Screen References")]
        [SerializeField] private GameHUDScreen hudScreen;
        [SerializeField] private LevelCompleteScreen levelCompleteScreen;
        [SerializeField] private PauseMenuScreen pauseMenuScreen;

        [Header("Canvas Settings")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasScaler canvasScaler;

        // Runtime created UI elements
        private GameObject hudPanel;
        private GameObject levelCompletePanel;
        private GameObject levelSelectPanel;
        private LevelSelectScreen levelSelectScreen;
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

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnItemsRemainingChanged -= OnItemsRemainingChanged;
            }
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
            CreateLevelSelectPanel();
            CreateHUDPanel();
            CreateLevelCompletePanel();

            // Hide panels initially
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(false);

            // Show level select first
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(true);

            Debug.Log("[UIManager] Runtime UI created");
        }

        /// <summary>
        /// Show level select and hide gameplay UI
        /// </summary>
        public void ShowLevelSelect()
        {
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(true);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            // Refresh level select to show updated stars
            levelSelectScreen?.RefreshDisplay();
        }

        /// <summary>
        /// Show gameplay UI and hide level select
        /// </summary>
        public void ShowGameplay()
        {
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

        private void CreateLevelSelectPanel()
        {
            levelSelectPanel = new GameObject("Level Select Panel");
            levelSelectPanel.transform.SetParent(mainCanvas.transform, false);

            var rect = levelSelectPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background
            var bg = levelSelectPanel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.4f, 0.6f, 1f);

            // Header
            var header = new GameObject("Header");
            header.transform.SetParent(levelSelectPanel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.85f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            var headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.3f, 0.5f, 1f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(header.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.3f, 0);
            titleRect.anchorMax = new Vector2(0.7f, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "SELECT LEVEL";
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // World navigation container
            var worldNav = new GameObject("WorldNav");
            worldNav.transform.SetParent(header.transform, false);
            var worldNavRect = worldNav.AddComponent<RectTransform>();
            worldNavRect.anchorMin = new Vector2(0, 0);
            worldNavRect.anchorMax = new Vector2(1, 0.5f);
            worldNavRect.offsetMin = new Vector2(20, 10);
            worldNavRect.offsetMax = new Vector2(-20, -10);

            var worldNavLayout = worldNav.AddComponent<HorizontalLayoutGroup>();
            worldNavLayout.spacing = 20;
            worldNavLayout.childAlignment = TextAnchor.MiddleCenter;
            worldNavLayout.childForceExpandWidth = false;
            worldNavLayout.childForceExpandHeight = true;

            // Prev world button
            var prevBtn = CreateButton(worldNav.transform, "PrevWorld", "<", null, 60, 50);

            // World name
            var worldNameGO = new GameObject("WorldName");
            worldNameGO.transform.SetParent(worldNav.transform, false);
            var worldNameRect = worldNameGO.AddComponent<RectTransform>();
            worldNameRect.sizeDelta = new Vector2(200, 50);
            var worldNameText = worldNameGO.AddComponent<TextMeshProUGUI>();
            worldNameText.text = "Resort";
            worldNameText.fontSize = 32;
            worldNameText.fontStyle = FontStyles.Bold;
            worldNameText.alignment = TextAlignmentOptions.Center;
            worldNameText.color = Color.white;
            var worldNameLayout = worldNameGO.AddComponent<LayoutElement>();
            worldNameLayout.minWidth = 200;

            // Next world button
            var nextBtn = CreateButton(worldNav.transform, "NextWorld", ">", null, 60, 50);

            // Level grid scroll area
            var scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(levelSelectPanel.transform, false);
            var scrollRect = scrollArea.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 0.85f);
            scrollRect.offsetMin = new Vector2(20, 20);
            scrollRect.offsetMax = new Vector2(-20, -10);

            var scrollView = scrollArea.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Elastic;

            var scrollMask = scrollArea.AddComponent<Mask>();
            var scrollBg = scrollArea.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.25f, 0.4f, 1f);

            // Content container
            var content = new GameObject("Content");
            content.transform.SetParent(scrollArea.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;

            var gridLayout = content.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 120);
            gridLayout.spacing = new Vector2(15, 15);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = contentRect;

            // Add LevelSelectScreen component
            levelSelectScreen = levelSelectPanel.AddComponent<LevelSelectScreen>();

            // Wire up references using reflection (since we created UI at runtime)
            var screenType = typeof(LevelSelectScreen);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            var prevField = screenType.GetField("prevWorldButton", flags);
            var nextField = screenType.GetField("nextWorldButton", flags);
            var nameField = screenType.GetField("worldNameText", flags);
            var gridField = screenType.GetField("levelGridParent", flags);
            var scrollField = screenType.GetField("levelScrollRect", flags);

            Debug.Log($"[UIManager] Reflection fields found - prev:{prevField != null}, next:{nextField != null}, name:{nameField != null}, grid:{gridField != null}, scroll:{scrollField != null}");

            prevField?.SetValue(levelSelectScreen, prevBtn);
            nextField?.SetValue(levelSelectScreen, nextBtn);
            nameField?.SetValue(levelSelectScreen, worldNameText);
            gridField?.SetValue(levelSelectScreen, content.transform);
            scrollField?.SetValue(levelSelectScreen, scrollView);

            Debug.Log($"[UIManager] References set - prevBtn:{prevBtn != null}, nextBtn:{nextBtn != null}, content:{content != null}");

            // Connect level selected callback
            levelSelectScreen.OnLevelSelected += OnLevelSelectedFromMenu;

            Debug.Log("[UIManager] Level select panel created");
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
            rect.sizeDelta = new Vector2(150, 50);

            var layout = buttonsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childForceExpandWidth = false;

            // Pause button
            CreateButton(buttonsGO.transform, "Pause", "II", () => GameManager.Instance?.PauseGame());

            // Restart button
            CreateButton(buttonsGO.transform, "Restart", "R", () => LevelManager.Instance?.RestartLevel());
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
            string worldId = GameManager.Instance?.CurrentWorldId ?? "resort";
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
            string worldId = GameManager.Instance?.CurrentWorldId ?? "resort";
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
