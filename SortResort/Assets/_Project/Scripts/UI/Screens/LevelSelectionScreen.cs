using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class LevelSelectionScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI worldNameText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private ScrollRect levelScrollView;
        [SerializeField] private Transform levelGridContainer;

        [Header("Level Node")]
        [SerializeField] private GameObject levelNodePrefab;

        [Header("Display Settings")]
        [SerializeField] private int levelsPerRow = 5;

        private WorldData currentWorld;
        private LevelNode[] levelNodes;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SortResort.FontManager.ApplyBold(worldNameText);
            SortResort.FontManager.ApplyBold(progressText);
        }

        private void Start()
        {
            LoadCurrentWorld();
        }

        private void SetupButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void LoadCurrentWorld()
        {
            string worldId = GameManager.Instance?.CurrentWorldId;
            if (string.IsNullOrEmpty(worldId)) return;

            currentWorld = WorldProgressionManager.Instance?.GetWorldData(worldId);
            if (currentWorld == null) return;

            UpdateWorldDisplay();
            GenerateLevelNodes();
        }

        private void UpdateWorldDisplay()
        {
            if (currentWorld == null) return;

            if (worldNameText != null)
            {
                worldNameText.text = currentWorld.worldName;
            }

            if (progressText != null && SaveManager.Instance != null)
            {
                int completedLevels = SaveManager.Instance.GetWorldCompletedLevelCount(currentWorld.worldID);
                int totalStars = SaveManager.Instance.GetWorldTotalStars(currentWorld.worldID);
                int maxStars = currentWorld.totalLevels * 3;
                progressText.text = $"{completedLevels}/{currentWorld.totalLevels} Levels | {totalStars}/{maxStars} Stars";
            }
        }

        private void GenerateLevelNodes()
        {
            if (levelNodePrefab == null || levelGridContainer == null) return;
            if (currentWorld == null) return;

            // Clear existing nodes
            foreach (Transform child in levelGridContainer)
            {
                Destroy(child.gameObject);
            }

            levelNodes = new LevelNode[currentWorld.totalLevels];

            for (int i = 0; i < currentWorld.totalLevels; i++)
            {
                int levelNumber = i + 1;

                var nodeObj = Instantiate(levelNodePrefab, levelGridContainer);
                var node = nodeObj.GetComponent<LevelNode>();

                if (node != null)
                {
                    int stars = SaveManager.Instance?.GetLevelStars(currentWorld.worldID, levelNumber) ?? 0;
                    bool isUnlocked = SaveManager.Instance?.IsLevelUnlocked(currentWorld.worldID, levelNumber) ?? (levelNumber == 1);
                    bool isCompleted = SaveManager.Instance?.IsLevelCompleted(currentWorld.worldID, levelNumber) ?? false;

                    node.Initialize(levelNumber, stars, isUnlocked, isCompleted);
                    node.OnLevelSelected += OnLevelSelected;

                    levelNodes[i] = node;
                }
            }

            // Scroll to highest unlocked level
            ScrollToHighestUnlocked();
        }

        private void ScrollToHighestUnlocked()
        {
            if (levelScrollView == null || levelNodes == null) return;

            int highestLevel = SaveManager.Instance?.GetHighestLevelCompleted(currentWorld.worldID) ?? 0;
            int targetIndex = Mathf.Min(highestLevel, levelNodes.Length - 1);

            if (targetIndex > 0 && levelNodes[targetIndex] != null)
            {
                // Calculate scroll position based on level index
                float normalizedPosition = 1f - ((float)targetIndex / levelNodes.Length);
                levelScrollView.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            }
        }

        private void OnLevelSelected(int levelNumber)
        {
            PlayButtonSound();

            bool isUnlocked = SaveManager.Instance?.IsLevelUnlocked(currentWorld.worldID, levelNumber) ?? false;
            if (!isUnlocked)
            {
                Debug.Log($"[LevelSelectionScreen] Level {levelNumber} is locked");
                return;
            }

            GameManager.Instance?.StartLevel(currentWorld.worldID, levelNumber);
        }

        private void OnBackClicked()
        {
            PlayButtonSound();
            GameManager.Instance?.GoToWorldSelection();
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        public void RefreshLevelStates()
        {
            if (levelNodes == null || currentWorld == null) return;

            for (int i = 0; i < levelNodes.Length; i++)
            {
                if (levelNodes[i] == null) continue;

                int levelNumber = i + 1;
                int stars = SaveManager.Instance?.GetLevelStars(currentWorld.worldID, levelNumber) ?? 0;
                bool isUnlocked = SaveManager.Instance?.IsLevelUnlocked(currentWorld.worldID, levelNumber) ?? false;
                bool isCompleted = SaveManager.Instance?.IsLevelCompleted(currentWorld.worldID, levelNumber) ?? false;

                levelNodes[i].UpdateState(stars, isUnlocked, isCompleted);
            }

            UpdateWorldDisplay();
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveAllListeners();

            if (levelNodes != null)
            {
                foreach (var node in levelNodes)
                {
                    if (node != null)
                    {
                        node.OnLevelSelected -= OnLevelSelected;
                    }
                }
            }
        }
    }
}
