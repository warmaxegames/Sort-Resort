using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace SortResort.UI
{
    /// <summary>
    /// Level selection screen with world navigation and level grid.
    /// Shows 100 levels per world in a scrollable grid.
    /// Styled to match the Godot version with portal sprites and world images.
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

        [Header("Settings")]
        [SerializeField] private int levelsPerWorld = 100;

        // Cached sprites for level portals
        private Sprite portalSprite;
        private Sprite[] starSprites = new Sprite[4]; // 0 = none, 1-3 = star count

        // Runtime data
        private List<string> worldIds = new List<string> { "island", "supermarket", "farm", "tavern", "space" };
        private int currentWorldIndex = 0;
        private List<LevelButton> levelButtons = new List<LevelButton>();

        public System.Action<string, int> OnLevelSelected;

        private void Start()
        {
            Debug.Log("[LevelSelectScreen] Start called");
            LoadPortalSprites();
            SetupWorldNavigation();
            CreateLevelGrid();
            RefreshDisplay();
            Debug.Log($"[LevelSelectScreen] Initialized with {levelButtons.Count} level buttons");
        }

        private void LoadPortalSprites()
        {
            // Load portal background sprite
            portalSprite = Resources.Load<Sprite>("Sprites/UI/Icons/level_portal");
            if (portalSprite == null)
            {
                Debug.LogError("[LevelSelectScreen] level_portal.png NOT FOUND at Sprites/UI/Icons/level_portal");
                // Try alternate paths
                portalSprite = Resources.Load<Sprite>("UI/Icons/level_portal");
                if (portalSprite != null)
                    Debug.Log("[LevelSelectScreen] Found portal at alternate path UI/Icons/level_portal");
            }

            // Load star overlay sprites
            starSprites[0] = null; // No stars
            starSprites[1] = Resources.Load<Sprite>("Sprites/UI/Icons/portal_1_stars");
            starSprites[2] = Resources.Load<Sprite>("Sprites/UI/Icons/portal_2_stars");
            starSprites[3] = Resources.Load<Sprite>("Sprites/UI/Icons/portal_3_stars");

            Debug.Log($"[LevelSelectScreen] Loaded sprites - portal:{portalSprite != null}, star1:{starSprites[1] != null}, star2:{starSprites[2] != null}, star3:{starSprites[3] != null}");
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
            Debug.Log($"[LevelSelectScreen] CreateLevelGrid - gridParent: {levelGridParent != null}, portalSprite: {portalSprite != null}");

            if (levelGridParent == null)
            {
                Debug.LogError("[LevelSelectScreen] levelGridParent is null! Cannot create level buttons.");
                return;
            }

            Debug.Log($"[LevelSelectScreen] Grid parent name: {levelGridParent.name}, childCount before: {levelGridParent.childCount}");

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

            Debug.Log($"[LevelSelectScreen] Created {levelButtons.Count} level buttons, gridParent childCount after: {levelGridParent.childCount}");
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
            // Use color tint for button states
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.colors = colors;

            // Level number text - centered in the main orb area (above the star circles)
            var textGO = new GameObject("LevelNumber");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            // Position text centered in the glowing orb area, moved down
            textRect.anchorMin = new Vector2(0.1f, 0.22f);
            textRect.anchorMax = new Vector2(0.9f, 0.78f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var levelText = textGO.AddComponent<TextMeshProUGUI>();
            levelText.text = levelNumber.ToString();
            levelText.fontSize = 72; // Large but fits within orb
            levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.color = Color.white;
            // Outline for visibility against glowing background
            levelText.outlineWidth = 0.3f;
            levelText.outlineColor = new Color32(80, 0, 80, 200);

            // Stars overlay image - positioned to fit INSIDE the dark circles at bottom of portal
            var starsGO = new GameObject("StarsOverlay");
            starsGO.transform.SetParent(btnGO.transform, false);
            var starsRect = starsGO.AddComponent<RectTransform>();
            // Stars positioned to align with dark circles
            starsRect.anchorMin = new Vector2(0.11f, 0.22f);
            starsRect.anchorMax = new Vector2(0.89f, 0.36f);
            starsRect.offsetMin = Vector2.zero;
            starsRect.offsetMax = Vector2.zero;

            var starsImage = starsGO.AddComponent<Image>();
            starsImage.preserveAspect = true;
            starsImage.enabled = false; // Hidden until stars are earned

            // Lock overlay (grays out locked levels)
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
            levelBtn.Initialize(levelNumber, button, bgImage, levelText, starsImage, lockGO, starSprites);

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

            // Update world sprite image
            LoadWorldImage(worldId);

            // Update navigation buttons visibility/interactability
            if (prevWorldButton != null)
            {
                prevWorldButton.interactable = currentWorldIndex > 0;
                // Fade out disabled buttons
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
        }

        private void LoadWorldImage(string worldId)
        {
            if (worldImage == null) return;

            // Try loading world sprite (island_world.png, supermarket_world.png, etc.)
            var sprite = Resources.Load<Sprite>($"Sprites/UI/Worlds/{worldId}_world");
            if (sprite == null)
            {
                // Try alternate naming (resort_world for backwards compatibility)
                sprite = Resources.Load<Sprite>($"Sprites/UI/Worlds/resort_world");
            }

            if (sprite != null)
            {
                worldImage.sprite = sprite;
                worldImage.enabled = true;
                Debug.Log($"[LevelSelectScreen] Loaded world sprite for {worldId}");
            }
            else
            {
                worldImage.enabled = false;
                Debug.LogWarning($"[LevelSelectScreen] World sprite not found for {worldId}");
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
    /// Individual level button in the grid.
    /// Uses portal sprite background and star overlay sprites like Godot version.
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        private int levelNumber;
        private Button button;
        private Image backgroundImage;
        private TextMeshProUGUI levelText;
        private Image starsOverlay;
        private GameObject lockOverlay;
        private Sprite[] starSprites;

        public void Initialize(int level, Button btn, Image bg, TextMeshProUGUI text, Image starsImg, GameObject lockObj, Sprite[] stars)
        {
            levelNumber = level;
            button = btn;
            backgroundImage = bg;
            levelText = text;
            starsOverlay = starsImg;
            lockOverlay = lockObj;
            starSprites = stars;
        }

        public void UpdateState(bool isUnlocked, int starsEarned)
        {
            // Update lock overlay visibility - hide it, we use tinting instead
            if (lockOverlay != null)
                lockOverlay.SetActive(false);

            // Update button interactable
            if (button != null)
                button.interactable = isUnlocked;

            // Update background tint for locked state (like Godot's gray portals)
            if (backgroundImage != null)
            {
                backgroundImage.color = isUnlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            // Update text color for locked state
            if (levelText != null)
            {
                levelText.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            // Stars - ONLY show for unlocked levels that have earned stars
            if (starsOverlay != null && starSprites != null)
            {
                // Locked levels: NO stars shown at all
                if (!isUnlocked)
                {
                    starsOverlay.enabled = false;
                }
                // Unlocked with stars earned: show the star sprite
                else if (starsEarned > 0 && starsEarned <= 3 && starSprites[starsEarned] != null)
                {
                    starsOverlay.sprite = starSprites[starsEarned];
                    starsOverlay.enabled = true;
                    starsOverlay.color = Color.white;
                }
                // Unlocked but no stars yet: hide stars
                else
                {
                    starsOverlay.enabled = false;
                }
            }
        }
    }
}
