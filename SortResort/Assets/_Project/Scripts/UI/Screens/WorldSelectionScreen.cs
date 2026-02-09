using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class WorldSelectionScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [SerializeField] private Button selectWorldButton;
        [SerializeField] private Button buyWorldButton;

        [Header("World Display")]
        [SerializeField] private Image worldPreviewImage;
        [SerializeField] private TextMeshProUGUI worldNameText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI unlockRequirementText;
        [SerializeField] private GameObject lockOverlay;

        [Header("World Data")]
        [SerializeField] private List<WorldData> allWorlds = new List<WorldData>();

        private int currentWorldIndex = 0;
        private WorldData currentWorld;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SortResort.FontManager.ApplyBold(worldNameText);
            SortResort.FontManager.ApplyBold(progressText);
            SortResort.FontManager.ApplyBold(unlockRequirementText);
        }

        private void Start()
        {
            LoadWorldsFromManager();
            RefreshDisplay();
        }

        private void SetupButtons()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (leftArrowButton != null)
                leftArrowButton.onClick.AddListener(OnLeftArrowClicked);

            if (rightArrowButton != null)
                rightArrowButton.onClick.AddListener(OnRightArrowClicked);

            if (selectWorldButton != null)
                selectWorldButton.onClick.AddListener(OnSelectWorldClicked);

            if (buyWorldButton != null)
                buyWorldButton.onClick.AddListener(OnBuyWorldClicked);
        }

        private void LoadWorldsFromManager()
        {
            if (WorldProgressionManager.Instance != null)
            {
                allWorlds = WorldProgressionManager.Instance.GetAllWorlds();
            }

            if (allWorlds.Count == 0)
            {
                Debug.LogWarning("[WorldSelectionScreen] No worlds available");
            }
        }

        private void RefreshDisplay()
        {
            if (allWorlds.Count == 0) return;

            currentWorld = allWorlds[currentWorldIndex];
            var unlockStatus = WorldProgressionManager.Instance?.GetWorldUnlockStatus(currentWorld.worldID)
                ?? new WorldUnlockStatus { isUnlocked = currentWorld.isDefaultWorld };

            // Update world preview
            if (worldPreviewImage != null)
            {
                worldPreviewImage.sprite = unlockStatus.isUnlocked ? currentWorld.worldIcon : currentWorld.worldIconLocked;
                worldPreviewImage.color = unlockStatus.isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            // Update world name
            if (worldNameText != null)
            {
                worldNameText.text = currentWorld.worldName;
            }

            // Update progress text
            if (progressText != null && SaveManager.Instance != null)
            {
                int completedLevels = SaveManager.Instance.GetWorldCompletedLevelCount(currentWorld.worldID);
                int totalStars = SaveManager.Instance.GetWorldTotalStars(currentWorld.worldID);
                progressText.text = $"{completedLevels}/{currentWorld.totalLevels} Levels | {totalStars} Stars";
            }

            // Update lock state
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!unlockStatus.isUnlocked);
            }

            // Update unlock requirement text
            if (unlockRequirementText != null)
            {
                if (!unlockStatus.isUnlocked)
                {
                    if (unlockStatus.requiresPurchase)
                    {
                        unlockRequirementText.text = $"Purchase to unlock - ${currentWorld.purchasePrice:F2}";
                    }
                    else
                    {
                        unlockRequirementText.text = $"Complete {unlockStatus.LevelsRemaining} more levels in {unlockStatus.requiredWorldId} to unlock";
                    }
                    unlockRequirementText.gameObject.SetActive(true);
                }
                else
                {
                    unlockRequirementText.gameObject.SetActive(false);
                }
            }

            // Update buttons
            if (selectWorldButton != null)
            {
                selectWorldButton.gameObject.SetActive(unlockStatus.isUnlocked);
            }

            if (buyWorldButton != null)
            {
                buyWorldButton.gameObject.SetActive(!unlockStatus.isUnlocked && unlockStatus.requiresPurchase);
            }

            // Update navigation arrows
            UpdateNavigationArrows();
        }

        private void UpdateNavigationArrows()
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.interactable = currentWorldIndex > 0;
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.interactable = currentWorldIndex < allWorlds.Count - 1;
            }
        }

        private void OnBackClicked()
        {
            PlayButtonSound();
            GameManager.Instance?.GoToMainMenu();
        }

        private void OnLeftArrowClicked()
        {
            PlayButtonSound();
            if (currentWorldIndex > 0)
            {
                currentWorldIndex--;
                RefreshDisplay();
            }
        }

        private void OnRightArrowClicked()
        {
            PlayButtonSound();
            if (currentWorldIndex < allWorlds.Count - 1)
            {
                currentWorldIndex++;
                RefreshDisplay();
            }
        }

        private void OnSelectWorldClicked()
        {
            PlayButtonSound();
            if (currentWorld != null)
            {
                GameManager.Instance?.GoToLevelSelection(currentWorld.worldID);
            }
        }

        private void OnBuyWorldClicked()
        {
            PlayButtonSound();
            // TODO: Implement IAP purchase flow
            Debug.Log($"[WorldSelectionScreen] Buy world: {currentWorld?.worldID}");
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        public void SelectWorld(string worldId)
        {
            for (int i = 0; i < allWorlds.Count; i++)
            {
                if (allWorlds[i].worldID == worldId)
                {
                    currentWorldIndex = i;
                    RefreshDisplay();
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (leftArrowButton != null) leftArrowButton.onClick.RemoveAllListeners();
            if (rightArrowButton != null) rightArrowButton.onClick.RemoveAllListeners();
            if (selectWorldButton != null) selectWorldButton.onClick.RemoveAllListeners();
            if (buyWorldButton != null) buyWorldButton.onClick.RemoveAllListeners();
        }
    }
}
