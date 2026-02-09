using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class GameHUDScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI moveCountText;

        [Header("Star Progress")]
        [SerializeField] private Image[] starProgressImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;

        [Header("Move Display Settings")]
        [SerializeField] private Color normalMoveColor = Color.white;
        [SerializeField] private Color warningMoveColor = Color.yellow;
        [SerializeField] private Color dangerMoveColor = Color.red;

        private int[] starThresholds;
        private int maxMoves;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SortResort.FontManager.ApplyBold(levelNameText);
            SortResort.FontManager.ApplyBold(moveCountText);
        }

        private void SetupButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(OnUndoClicked);
            }
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            GameEvents.OnMoveUsed += OnMoveUsed;
            GameEvents.OnLevelStarted += OnLevelStarted;
        }

        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            GameEvents.OnMoveUsed -= OnMoveUsed;
            GameEvents.OnLevelStarted -= OnLevelStarted;
        }

        public void Initialize(string levelName, int[] thresholds)
        {
            starThresholds = thresholds;

            if (thresholds != null && thresholds.Length > 2)
            {
                maxMoves = thresholds[2]; // 1-star threshold is max
            }

            if (levelNameText != null)
            {
                levelNameText.text = levelName;
            }

            UpdateMoveDisplay(0);
            UpdateStarProgress(0);
            UpdateUndoButton(false);
        }

        private void OnLevelStarted(int levelNumber)
        {
            if (levelNameText != null)
            {
                levelNameText.text = $"Level {levelNumber}";
            }
            UpdateMoveDisplay(0);
        }

        private void OnMoveUsed(int currentMoves)
        {
            UpdateMoveDisplay(currentMoves);
            UpdateStarProgress(currentMoves);
            UpdateUndoButton(currentMoves > 0);
        }

        private void UpdateMoveDisplay(int moves)
        {
            if (moveCountText == null) return;

            moveCountText.text = $"Moves: {moves}";

            // Update color based on progress
            if (starThresholds != null && starThresholds.Length > 2)
            {
                if (moves > starThresholds[1]) // Past 2-star threshold
                {
                    moveCountText.color = dangerMoveColor;
                }
                else if (moves > starThresholds[0]) // Past 3-star threshold
                {
                    moveCountText.color = warningMoveColor;
                }
                else
                {
                    moveCountText.color = normalMoveColor;
                }
            }
        }

        private void UpdateStarProgress(int currentMoves)
        {
            if (starProgressImages == null || starThresholds == null) return;

            int potentialStars = CalculatePotentialStars(currentMoves);

            for (int i = 0; i < starProgressImages.Length && i < 3; i++)
            {
                if (starProgressImages[i] != null)
                {
                    bool isFilled = i < potentialStars;
                    starProgressImages[i].sprite = isFilled ? starFilledSprite : starEmptySprite;
                    starProgressImages[i].color = isFilled ? Color.yellow : Color.gray;
                }
            }
        }

        private int CalculatePotentialStars(int moves)
        {
            if (starThresholds == null || starThresholds.Length < 3) return 3;

            if (moves <= starThresholds[0]) return 3;
            if (moves <= starThresholds[1]) return 2;
            if (moves <= starThresholds[2]) return 1;
            return 0;
        }

        private void UpdateUndoButton(bool canUndo)
        {
            if (undoButton != null)
            {
                undoButton.interactable = canUndo;
            }
        }

        private void OnPauseClicked()
        {
            PlayButtonSound();
            GameManager.Instance?.PauseGame();
        }

        private void OnUndoClicked()
        {
            PlayButtonSound();
            // Undo logic will be handled by UndoManager
            GameManager.Instance?.DecrementMoveCount();
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
            if (undoButton != null) undoButton.onClick.RemoveAllListeners();
        }
    }
}
