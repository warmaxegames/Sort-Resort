using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace SortResort.UI
{
    /// <summary>
    /// Level selection screen with world navigation and level grid.
    /// Shows 100 levels per world in a scrollable grid.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        [Header("World Navigation")]
        [SerializeField] private Button prevWorldButton;
        [SerializeField] private Button nextWorldButton;
        [SerializeField] private TextMeshProUGUI worldNameText;
        [SerializeField] private Image worldImage;

        [Header("Level Grid")]
        [SerializeField] private Transform levelGridParent;
        [SerializeField] private ScrollRect levelScrollRect;
        [SerializeField] private GameObject levelButtonPrefab;

        [Header("Settings")]
        [SerializeField] private int levelsPerWorld = 100;
        [SerializeField] private int gridColumns = 5;

        // Runtime data
        private List<string> worldIds = new List<string> { "resort", "supermarket", "farm" };
        private int currentWorldIndex = 0;
        private List<LevelButton> levelButtons = new List<LevelButton>();

        public System.Action<string, int> OnLevelSelected;

        private void Start()
        {
            Debug.Log("[LevelSelectScreen] Start called");
            SetupWorldNavigation();
            CreateLevelGrid();
            RefreshDisplay();
            Debug.Log($"[LevelSelectScreen] Initialized with {levelButtons.Count} level buttons");
        }

        private void SetupWorldNavigation()
        {
            Debug.Log($"[LevelSelectScreen] SetupWorldNavigation - prevBtn: {prevWorldButton != null}, nextBtn: {nextWorldButton != null}");

            if (prevWorldButton != null)
            {
                prevWorldButton.onClick.AddListener(OnPrevWorldClicked);
                Debug.Log("[LevelSelectScreen] Prev button listener added");
            }
            if (nextWorldButton != null)
            {
                nextWorldButton.onClick.AddListener(OnNextWorldClicked);
                Debug.Log("[LevelSelectScreen] Next button listener added");
            }
        }

        private void OnPrevWorldClicked()
        {
            Debug.Log($"[LevelSelectScreen] Prev clicked, currentIndex: {currentWorldIndex}");
            if (currentWorldIndex > 0)
            {
                currentWorldIndex--;
                RefreshDisplay();
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        private void OnNextWorldClicked()
        {
            Debug.Log($"[LevelSelectScreen] Next clicked, currentIndex: {currentWorldIndex}, max: {worldIds.Count - 1}");
            if (currentWorldIndex < worldIds.Count - 1)
            {
                currentWorldIndex++;
                RefreshDisplay();
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        private void CreateLevelGrid()
        {
            Debug.Log($"[LevelSelectScreen] CreateLevelGrid - gridParent: {levelGridParent != null}");

            if (levelGridParent == null)
            {
                Debug.LogError("[LevelSelectScreen] levelGridParent is null!");
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
            rect.sizeDelta = new Vector2(120, 120);

            // Background
            var bgImage = btnGO.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.6f, 0.9f, 1f);

            // Button component
            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = bgImage;

            // Level number text
            var textGO = new GameObject("LevelNumber");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.4f);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var levelText = textGO.AddComponent<TextMeshProUGUI>();
            levelText.text = levelNumber.ToString();
            levelText.fontSize = 32;
            levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.color = Color.white;

            // Stars container
            var starsGO = new GameObject("Stars");
            starsGO.transform.SetParent(btnGO.transform, false);
            var starsRect = starsGO.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0, 0);
            starsRect.anchorMax = new Vector2(1, 0.4f);
            starsRect.offsetMin = new Vector2(5, 5);
            starsRect.offsetMax = new Vector2(-5, -5);

            var starsLayout = starsGO.AddComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 2;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childForceExpandWidth = false;
            starsLayout.childForceExpandHeight = false;

            // Create 3 star images
            var starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var starGO = new GameObject($"Star_{i}");
                starGO.transform.SetParent(starsGO.transform, false);
                var starRect = starGO.AddComponent<RectTransform>();
                starRect.sizeDelta = new Vector2(25, 25);
                var starImg = starGO.AddComponent<Image>();
                starImg.color = Color.gray;
                starImages[i] = starImg;
            }

            // Lock overlay
            var lockGO = new GameObject("Lock");
            lockGO.transform.SetParent(btnGO.transform, false);
            var lockRect = lockGO.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;

            var lockBg = lockGO.AddComponent<Image>();
            lockBg.color = new Color(0, 0, 0, 0.7f);

            var lockTextGO = new GameObject("LockText");
            lockTextGO.transform.SetParent(lockGO.transform, false);
            var lockTextRect = lockTextGO.AddComponent<RectTransform>();
            lockTextRect.anchorMin = Vector2.zero;
            lockTextRect.anchorMax = Vector2.one;
            lockTextRect.offsetMin = Vector2.zero;
            lockTextRect.offsetMax = Vector2.zero;

            var lockText = lockTextGO.AddComponent<TextMeshProUGUI>();
            lockText.text = "🔒";
            lockText.fontSize = 36;
            lockText.alignment = TextAlignmentOptions.Center;
            lockText.color = Color.white;

            // Create LevelButton component
            var levelBtn = btnGO.AddComponent<LevelButton>();
            levelBtn.Initialize(levelNumber, button, bgImage, levelText, starImages, lockGO);

            // Connect click handler
            int lvl = levelNumber; // Capture for closure
            button.onClick.AddListener(() => OnLevelButtonClicked(lvl));

            return levelBtn;
        }

        private void OnLevelButtonClicked(int levelNumber)
        {
            Debug.Log($"[LevelSelectScreen] Level button {levelNumber} clicked!");

            string worldId = worldIds[currentWorldIndex];

            // Check if level is unlocked
            if (!IsLevelUnlocked(worldId, levelNumber))
            {
                AudioManager.Instance?.PlayButtonClick();
                Debug.Log($"[LevelSelectScreen] Level {levelNumber} is locked");
                return;
            }

            AudioManager.Instance?.PlayButtonClick();
            Debug.Log($"[LevelSelectScreen] Selected {worldId} level {levelNumber}, invoking OnLevelSelected");

            OnLevelSelected?.Invoke(worldId, levelNumber);
        }

        public void RefreshDisplay()
        {
            string worldId = worldIds[currentWorldIndex];

            // Update world name
            if (worldNameText != null)
            {
                string displayName = char.ToUpper(worldId[0]) + worldId.Substring(1);
                worldNameText.text = displayName;
            }

            // Update navigation buttons
            if (prevWorldButton != null)
                prevWorldButton.interactable = currentWorldIndex > 0;
            if (nextWorldButton != null)
                nextWorldButton.interactable = currentWorldIndex < worldIds.Count - 1;

            // Update level buttons
            for (int i = 0; i < levelButtons.Count; i++)
            {
                int levelNumber = i + 1;
                bool isUnlocked = IsLevelUnlocked(worldId, levelNumber);
                int stars = GetLevelStars(worldId, levelNumber);

                levelButtons[i].UpdateState(isUnlocked, stars);
            }

            // Scroll to top
            if (levelScrollRect != null)
                levelScrollRect.normalizedPosition = new Vector2(0, 1);

            // Load world image if available
            LoadWorldImage(worldId);
        }

        private void LoadWorldImage(string worldId)
        {
            if (worldImage == null) return;

            var sprite = Resources.Load<Sprite>($"Sprites/UI/Worlds/{worldId}_world");
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
            // Level 1 is always unlocked
            if (levelNumber == 1) return true;

            // Check if previous level is completed
            if (SaveManager.Instance != null)
            {
                return SaveManager.Instance.IsLevelCompleted(worldId, levelNumber - 1);
            }

            // Fallback: only level 1 unlocked
            return false;
        }

        private int GetLevelStars(string worldId, int levelNumber)
        {
            if (SaveManager.Instance != null)
            {
                return SaveManager.Instance.GetLevelStars(worldId, levelNumber);
            }
            return 0;
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

        private void OnDestroy()
        {
            if (prevWorldButton != null) prevWorldButton.onClick.RemoveAllListeners();
            if (nextWorldButton != null) nextWorldButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// Individual level button in the grid
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        private int levelNumber;
        private Button button;
        private Image backgroundImage;
        private TextMeshProUGUI levelText;
        private Image[] starImages;
        private GameObject lockOverlay;

        private Color unlockedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        private Color completedColor = new Color(0.2f, 0.7f, 0.3f, 1f);

        public void Initialize(int level, Button btn, Image bg, TextMeshProUGUI text, Image[] stars, GameObject lockObj)
        {
            levelNumber = level;
            button = btn;
            backgroundImage = bg;
            levelText = text;
            starImages = stars;
            lockOverlay = lockObj;
        }

        public void UpdateState(bool isUnlocked, int starsEarned)
        {
            // Update lock overlay
            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            // Update button interactable
            if (button != null)
                button.interactable = isUnlocked;

            // Update background color
            if (backgroundImage != null)
            {
                if (!isUnlocked)
                    backgroundImage.color = lockedColor;
                else if (starsEarned > 0)
                    backgroundImage.color = completedColor;
                else
                    backgroundImage.color = unlockedColor;
            }

            // Update stars
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                    {
                        starImages[i].color = i < starsEarned ? Color.yellow : Color.gray;
                    }
                }
            }
        }
    }
}
