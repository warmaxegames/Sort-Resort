using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class LevelFailedScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI moveCountText;
        [SerializeField] private TextMeshProUGUI tipText;

        [Header("Mascot")]
        [SerializeField] private Image mascotImage;

        [Header("Background")]
        [SerializeField] private Image dimBackground;
        [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.7f);

        [Header("Tips")]
        [SerializeField] private string[] failureTips = {
            "Try matching items in different order!",
            "Look for chain reaction opportunities!",
            "Plan your moves ahead!",
            "Don't forget about the back rows!",
            "You can do it - try again!"
        };

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SortResort.FontManager.ApplyBold(titleText);
            SortResort.FontManager.ApplyBold(moveCountText);
            SortResort.FontManager.ApplyBold(tipText);

            if (dimBackground != null)
            {
                dimBackground.color = dimColor;
            }
        }

        private void SetupButtons()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            GameEvents.OnLevelFailed += OnLevelFailed;
        }

        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            GameEvents.OnLevelFailed -= OnLevelFailed;
        }

        private void OnLevelFailed(int levelNumber, string reason)
        {
            Show();
            UpdateDisplay();
            AudioManager.Instance?.PlayFailureSound();
        }

        private void UpdateDisplay()
        {
            if (titleText != null)
            {
                titleText.text = "Out of Moves!";
            }

            if (moveCountText != null)
            {
                int moves = GameManager.Instance?.CurrentMoveCount ?? 0;
                moveCountText.text = $"Used {moves} moves";
            }

            if (tipText != null && failureTips.Length > 0)
            {
                tipText.text = failureTips[Random.Range(0, failureTips.Length)];
            }

            UpdateMascot();
        }

        private void UpdateMascot()
        {
            if (mascotImage == null) return;

            var world = WorldProgressionManager.Instance?.GetWorldData(GameManager.Instance?.CurrentWorldId);
            if (world != null)
            {
                mascotImage.sprite = world.mascotSad;
            }
        }

        private void OnRetryClicked()
        {
            PlayButtonSound();
            Hide(true);
            GameManager.Instance?.RestartLevel();
        }

        private void OnExitClicked()
        {
            PlayButtonSound();
            Hide(true);
            GameManager.Instance?.GoToLevelSelection(GameManager.Instance.CurrentWorldId);
        }

        private void PlayButtonSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
