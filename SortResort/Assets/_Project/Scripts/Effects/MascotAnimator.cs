using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort
{
    /// <summary>
    /// Plays a frame-by-frame animation on a UI Image component.
    /// Used for mascot animations on level complete screen.
    /// Loads frames from Resources/Sprites/Mascots/Animations/{World}/
    /// </summary>
    public class MascotAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float frameRate = 30f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool playOnEnable = false;

        private Image targetImage;
        private Sprite[] frames;
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private float frameDuration;
        private bool isPlaying = false;

        private System.Action onComplete;

        private void Awake()
        {
            targetImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (playOnEnable && frames != null && frames.Length > 0)
            {
                Play();
            }
        }

        /// <summary>
        /// Load animation frames for a mascot animation.
        /// E.g., "whiskerthumbsup" in world "Island" loads from Mascots/Animations/Island/whiskerthumbsup_00000.png etc.
        /// </summary>
        public bool LoadFrames(string worldName, string animationName)
        {
            if (string.IsNullOrEmpty(worldName) || string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning("[MascotAnimator] World name or animation name not specified");
                return false;
            }

            string basePath = $"Sprites/Mascots/Animations/{worldName}/{animationName}";

            var frameList = new List<Sprite>();
            int maxFrames = 100; // Safety limit

            for (int i = 0; i < maxFrames; i++)
            {
                string framePath = $"{basePath}_{i:D5}";
                Sprite frame = Resources.Load<Sprite>(framePath);

                if (frame == null)
                {
                    break;
                }

                frameList.Add(frame);
            }

            if (frameList.Count == 0)
            {
                Debug.LogWarning($"[MascotAnimator] No frames found at path: {basePath}_00000. Trying with _0 suffix...");

                // Try loading with _0 suffix (for sprites imported as Multiple/sliced)
                for (int i = 0; i < maxFrames; i++)
                {
                    string framePath = $"{basePath}_{i:D5}_0";
                    Sprite frame = Resources.Load<Sprite>(framePath);
                    if (frame == null) break;
                    frameList.Add(frame);
                }

                if (frameList.Count == 0)
                {
                    Debug.LogError($"[MascotAnimator] No frames found at either path format");
                    return false;
                }
            }

            frames = frameList.ToArray();
            Debug.Log($"[MascotAnimator] Loaded {frames.Length} frames for {worldName}/{animationName}");
            return true;
        }

        /// <summary>
        /// Play the animation from the beginning
        /// </summary>
        public void Play(System.Action onCompleteCallback = null)
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning("[MascotAnimator] No frames loaded!");
                onCompleteCallback?.Invoke();
                return;
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
                if (targetImage == null)
                {
                    Debug.LogError("[MascotAnimator] No Image component found!");
                    onCompleteCallback?.Invoke();
                    return;
                }
            }

            onComplete = onCompleteCallback;
            frameDuration = 1f / frameRate;
            currentFrame = 0;
            frameTimer = 0f;
            isPlaying = true;

            targetImage.sprite = frames[0];
            targetImage.enabled = true;
        }

        /// <summary>
        /// Stop the animation
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
        }

        /// <summary>
        /// Check if animation is currently playing
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Set the frame rate for this animator instance
        /// </summary>
        public float FrameRate { get => frameRate; set => frameRate = value; }

        /// <summary>
        /// Get the total duration of the animation in seconds
        /// </summary>
        public float Duration => frames != null ? frames.Length / frameRate : 0f;

        private void Update()
        {
            if (!isPlaying || frames == null) return;

            frameTimer += Time.unscaledDeltaTime;

            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrame++;

                if (currentFrame >= frames.Length)
                {
                    if (loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        isPlaying = false;
                        onComplete?.Invoke();
                        return;
                    }
                }

                targetImage.sprite = frames[currentFrame];
            }
        }

        /// <summary>
        /// Static helper to get the world-specific victory animation name
        /// Returns null if no animation exists for that world
        /// </summary>
        public static string GetVictoryAnimationName(string worldId)
        {
            return worldId?.ToLower() switch
            {
                "island" => "whiskerthumbsup",
                // Add more worlds as animations are created
                // "supermarket" => "tommythumbsup",
                "farm" => "marathumbsup",
                // "tavern" => "hogthumbsup",
                // "space" => "leikathumbsup",
                _ => null
            };
        }

        /// <summary>
        /// Get the frame rate for a world's victory animation.
        /// Tuned so all animations complete in approximately the same duration (~1.87s).
        /// </summary>
        public static float GetVictoryAnimationFPS(string worldId)
        {
            return worldId?.ToLower() switch
            {
                "island" => 30f,  // 56 frames / 30fps = 1.87s
                "farm" => 44f,    // 83 frames / 44fps = 1.89s
                _ => 30f
            };
        }

        /// <summary>
        /// Static helper to get the world name in proper case for resource loading
        /// </summary>
        public static string GetWorldFolderName(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return null;
            return char.ToUpper(worldId[0]) + worldId.Substring(1).ToLower();
        }
    }
}
