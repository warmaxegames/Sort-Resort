using System;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort.UI
{
    /// <summary>
    /// Splash screen shown on game startup.
    /// Displays game branding and a play button to enter the game.
    /// </summary>
    public class SplashScreen : BaseScreen
    {
        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Image backgroundImage;

        [Header("Button Sprites")]
        [SerializeField] private Sprite playButtonNormal;
        [SerializeField] private Sprite playButtonPressed;

        public event Action OnPlayClicked;

        private Image playButtonImage;

        protected override void Awake()
        {
            base.Awake();
        }

        private void SetupPlayButton()
        {
            if (playButton == null)
            {
                Debug.LogWarning("[SplashScreen] SetupPlayButton called but playButton is null");
                return;
            }

            Debug.Log("[SplashScreen] SetupPlayButton - wiring up button click listener");
            playButtonImage = playButton.GetComponent<Image>();

            // Setup button click
            playButton.onClick.AddListener(HandlePlayClicked);

            // Setup visual states if we have the sprites
            if (playButtonNormal != null && playButtonPressed != null && playButtonImage != null)
            {
                // Create sprite state for pressed appearance
                var spriteState = new SpriteState
                {
                    pressedSprite = playButtonPressed,
                    highlightedSprite = playButtonNormal,
                    selectedSprite = playButtonNormal,
                    disabledSprite = playButtonNormal
                };
                playButton.spriteState = spriteState;
                playButton.transition = Selectable.Transition.SpriteSwap;
                playButtonImage.sprite = playButtonNormal;
            }
        }

        private void HandlePlayClicked()
        {
            Debug.Log("[SplashScreen] Play button clicked!");

            // Play warp sound effect
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWarpSound();
                Debug.Log("[SplashScreen] Warp sound played");
            }
            else
            {
                Debug.LogWarning("[SplashScreen] AudioManager.Instance is null!");
            }

            // Disable button to prevent double-clicks
            if (playButton != null)
            {
                playButton.interactable = false;
            }

            // Notify listeners (UIManager will handle transition)
            Debug.Log($"[SplashScreen] OnPlayClicked has {OnPlayClicked?.GetInvocationList()?.Length ?? 0} subscribers");
            OnPlayClicked?.Invoke();
        }

        /// <summary>
        /// Initialize splash screen with runtime-created references
        /// </summary>
        public void Initialize(Button button, Image background, Sprite normalSprite, Sprite pressedSprite)
        {
            playButton = button;
            backgroundImage = background;
            playButtonNormal = normalSprite;
            playButtonPressed = pressedSprite;

            Debug.Log($"[SplashScreen] Initialize called - button:{button != null}, bg:{background != null}");
            SetupPlayButton();
        }

        protected override void OnShowComplete()
        {
            base.OnShowComplete();

            // Re-enable button when shown
            if (playButton != null)
            {
                playButton.interactable = true;
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandlePlayClicked);
            }
        }
    }
}
