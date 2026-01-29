using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class LevelNode : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button nodeButton;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image lockIcon;

        [Header("Star Display")]
        [SerializeField] private GameObject starContainer;
        [SerializeField] private Image[] starImages;

        [Header("Visual States")]
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color completedColor = new Color(0.8f, 1f, 0.8f, 1f);

        [Header("Star Sprites")]
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;

        public event Action<int> OnLevelSelected;

        private int levelNumber;
        private int currentStars;
        private bool isUnlocked;
        private bool isCompleted;

        private void Awake()
        {
            if (nodeButton != null)
            {
                nodeButton.onClick.AddListener(OnNodeClicked);
            }
        }

        public void Initialize(int level, int stars, bool unlocked, bool completed)
        {
            levelNumber = level;
            currentStars = stars;
            isUnlocked = unlocked;
            isCompleted = completed;

            UpdateVisuals();
        }

        public void UpdateState(int stars, bool unlocked, bool completed)
        {
            currentStars = stars;
            isUnlocked = unlocked;
            isCompleted = completed;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Update level number text
            if (levelNumberText != null)
            {
                levelNumberText.text = levelNumber.ToString();
                levelNumberText.color = isUnlocked ? Color.black : Color.gray;
            }

            // Update background color
            if (nodeBackground != null)
            {
                if (!isUnlocked)
                {
                    nodeBackground.color = lockedColor;
                }
                else if (isCompleted)
                {
                    nodeBackground.color = completedColor;
                }
                else
                {
                    nodeBackground.color = unlockedColor;
                }
            }

            // Update lock icon
            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(!isUnlocked);
            }

            // Update stars
            UpdateStarDisplay();

            // Update button interactability
            if (nodeButton != null)
            {
                nodeButton.interactable = isUnlocked;
            }
        }

        private void UpdateStarDisplay()
        {
            if (starContainer != null)
            {
                starContainer.SetActive(isCompleted && isUnlocked);
            }

            if (starImages == null) return;

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    bool isFilled = i < currentStars;
                    starImages[i].sprite = isFilled ? starFilled : starEmpty;
                    starImages[i].color = isFilled ? Color.yellow : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
        }

        private void OnNodeClicked()
        {
            if (isUnlocked)
            {
                OnLevelSelected?.Invoke(levelNumber);
            }
        }

        private void OnDestroy()
        {
            if (nodeButton != null)
            {
                nodeButton.onClick.RemoveListener(OnNodeClicked);
            }
        }
    }
}
