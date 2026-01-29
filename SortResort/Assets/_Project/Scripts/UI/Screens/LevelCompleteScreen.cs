using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class LevelCompleteScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI moveCountText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Star Display")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;

        [Header("Mascot")]
        [SerializeField] private Image mascotImage;

        [Header("Animation Settings")]
        [SerializeField] private float starAnimationDelay = 0.5f;
        [SerializeField] private float starAnimationInterval = 0.3f;

        [Header("Messages")]
        [SerializeField] private string[] oneStarMessages = { "Good job!", "You did it!" };
        [SerializeField] private string[] twoStarMessages = { "Great work!", "Excellent!" };
        [SerializeField] private string[] threeStarMessages = { "Perfect!", "Amazing!", "Superstar!" };

        private int earnedStars;
        private int moveCount;

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            GameEvents.OnLevelCompleted += OnLevelCompleted;
        }

        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
        }

        private void OnLevelCompleted(int levelNumber, int stars)
        {
            earnedStars = stars;
            moveCount = GameManager.Instance?.CurrentMoveCount ?? 0;

            Show();
            StartCoroutine(PlayVictorySequence());
        }

        private IEnumerator PlayVictorySequence()
        {
            // Set initial state
            SetInteractable(false);

            if (titleText != null)
            {
                titleText.text = "Level Complete!";
            }

            if (moveCountText != null)
            {
                moveCountText.text = $"Completed in {moveCount} moves";
            }

            // Reset stars
            foreach (var star in starImages)
            {
                if (star != null)
                {
                    star.sprite = starEmptySprite;
                    star.color = Color.gray;
                    star.transform.localScale = Vector3.zero;
                }
            }

            // Play victory sound
            AudioManager.Instance?.PlayVictorySound();

            // Wait before showing stars
            yield return new WaitForSecondsRealtime(starAnimationDelay);

            // Animate stars
            for (int i = 0; i < earnedStars && i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    yield return StartCoroutine(AnimateStar(starImages[i]));
                    AudioManager.Instance?.PlayStarEarned();
                    yield return new WaitForSecondsRealtime(starAnimationInterval);
                }
            }

            // Show message
            UpdateMessage();

            // Update mascot
            UpdateMascot();

            // Enable interaction
            SetInteractable(true);
        }

        private IEnumerator AnimateStar(Image star)
        {
            star.sprite = starFilledSprite;
            star.color = Color.yellow;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Bounce effect
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
                star.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            star.transform.localScale = Vector3.one;
        }

        private void UpdateMessage()
        {
            if (messageText == null) return;

            string[] messages = earnedStars switch
            {
                3 => threeStarMessages,
                2 => twoStarMessages,
                _ => oneStarMessages
            };

            if (messages.Length > 0)
            {
                messageText.text = messages[Random.Range(0, messages.Length)];
            }
        }

        private void UpdateMascot()
        {
            if (mascotImage == null) return;

            var world = WorldProgressionManager.Instance?.GetWorldData(GameManager.Instance?.CurrentWorldId);
            if (world == null) return;

            mascotImage.sprite = earnedStars >= 2 ? world.mascotHappy : world.mascotIdle;
        }

        private void OnNextLevelClicked()
        {
            PlayButtonSound();
            Hide(true);
            GameManager.Instance?.GoToNextLevel();
        }

        private void OnReplayClicked()
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
            if (nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
            if (replayButton != null) replayButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
