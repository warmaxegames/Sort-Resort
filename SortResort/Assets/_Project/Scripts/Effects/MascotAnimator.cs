using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort
{
    /// <summary>
    /// Plays a frame-by-frame animation on a UI Image component.
    /// Used for mascot animations on level complete screen.
    /// Loads frames from Resources/Sprites/Mascots/Animations/{World}/
    /// Frames are cached statically so they load once and play instantly on subsequent uses.
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

        // Static frame cache: key = "WorldName/animationName", value = cached sprites
        private static Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();

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
        /// Preload animation frames for a world into the static cache.
        /// Call this during level loading so frames are ready instantly at level complete.
        /// </summary>
        public static void PreloadFrames(string worldId)
        {
            string animationName = GetVictoryAnimationName(worldId);
            string worldFolder = GetWorldFolderName(worldId);
            if (string.IsNullOrEmpty(animationName) || string.IsNullOrEmpty(worldFolder))
                return;

            string cacheKey = $"{worldFolder}/{animationName}";
            if (frameCache.ContainsKey(cacheKey))
                return; // Already cached

            var loaded = LoadFramesFromResources(worldFolder, animationName);
            if (loaded != null && loaded.Length > 0)
            {
                frameCache[cacheKey] = loaded;
                Debug.Log($"[MascotAnimator] Preloaded {loaded.Length} frames for {cacheKey}");
            }
        }

        /// <summary>
        /// Load animation frames for a mascot animation.
        /// Uses static cache if available, otherwise loads from Resources.
        /// </summary>
        public bool LoadFrames(string worldName, string animationName)
        {
            if (string.IsNullOrEmpty(worldName) || string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning("[MascotAnimator] World name or animation name not specified");
                return false;
            }

            string cacheKey = $"{worldName}/{animationName}";

            // Check static cache first
            if (frameCache.TryGetValue(cacheKey, out var cached))
            {
                frames = cached;
                Debug.Log($"[MascotAnimator] Using cached {frames.Length} frames for {cacheKey}");
                return true;
            }

            // Cache miss - load from Resources
            var loaded = LoadFramesFromResources(worldName, animationName);
            if (loaded != null && loaded.Length > 0)
            {
                frameCache[cacheKey] = loaded;
                frames = loaded;
                Debug.Log($"[MascotAnimator] Loaded and cached {frames.Length} frames for {cacheKey}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Internal: load frames from Resources as full-rect sprites.
        /// </summary>
        private static Sprite[] LoadFramesFromResources(string worldName, string animationName)
        {
            string basePath = $"Sprites/Mascots/Animations/{worldName}/{animationName}";

            var frameList = new List<Sprite>();
            int maxFrames = 100; // Safety limit

            for (int i = 0; i < maxFrames; i++)
            {
                string framePath = $"{basePath}_{i:D5}";
                // Load as Texture2D and create full-rect sprite to prevent Unity's
                // auto-trimming from giving each frame different dimensions.
                // Inconsistent sprite rects cause size stuttering with preserveAspect.
                var tex = Resources.Load<Texture2D>(framePath);

                if (tex == null)
                {
                    break;
                }

                var frame = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                frameList.Add(frame);
            }

            if (frameList.Count == 0)
            {
                // Try loading with _0 suffix (for sprites imported as Multiple/sliced)
                for (int i = 0; i < maxFrames; i++)
                {
                    string framePath = $"{basePath}_{i:D5}_0";
                    var tex = Resources.Load<Texture2D>(framePath);
                    if (tex == null) break;
                    var frame = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    frameList.Add(frame);
                }

                if (frameList.Count == 0)
                {
                    Debug.LogWarning($"[MascotAnimator] No frames found for {worldName}/{animationName}");
                    return null;
                }
            }

            return frameList.ToArray();
        }

        /// <summary>
        /// Load animation frames from an arbitrary Resources path.
        /// Frames must be named {prefix}_{NNNNN}.png (5-digit zero-padded).
        /// </summary>
        public bool LoadFramesFromPath(string resourceBasePath, string prefix)
        {
            string cacheKey = $"{resourceBasePath}/{prefix}";

            if (frameCache.TryGetValue(cacheKey, out var cached))
            {
                frames = cached;
                return true;
            }

            var frameList = new List<Sprite>();
            for (int i = 0; i < 200; i++)
            {
                string framePath = $"{resourceBasePath}/{prefix}_{i:D5}";
                var tex = Resources.Load<Texture2D>(framePath);
                if (tex == null) break;
                var frame = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                frameList.Add(frame);
            }

            if (frameList.Count == 0)
            {
                Debug.LogWarning($"[MascotAnimator] No frames found at {resourceBasePath}/{prefix}_NNNNN");
                return false;
            }

            frames = frameList.ToArray();
            frameCache[cacheKey] = frames;
            Debug.Log($"[MascotAnimator] Loaded {frames.Length} frames from {cacheKey}");
            return true;
        }

        /// <summary>
        /// Preload animation frames from an arbitrary Resources path into the static cache.
        /// </summary>
        public static void PreloadFramesFromPath(string resourceBasePath, string prefix)
        {
            string cacheKey = $"{resourceBasePath}/{prefix}";
            if (frameCache.ContainsKey(cacheKey)) return;

            var frameList = new List<Sprite>();
            for (int i = 0; i < 200; i++)
            {
                string framePath = $"{resourceBasePath}/{prefix}_{i:D5}";
                var tex = Resources.Load<Texture2D>(framePath);
                if (tex == null) break;
                var frame = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                frameList.Add(frame);
            }

            if (frameList.Count > 0)
            {
                frameCache[cacheKey] = frameList.ToArray();
                Debug.Log($"[MascotAnimator] Preloaded {frameList.Count} frames from {cacheKey}");
            }
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
        /// Set whether the animation should loop
        /// </summary>
        public bool Loop { get => loop; set => loop = value; }

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
                "supermarket" => "tommythumbsup",
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
                "island" => 30f,       // 56 frames / 30fps = 1.87s
                "supermarket" => 42f,  // 80 frames / 42fps = 1.90s (deduplicated from 99 frames)
                "farm" => 16f,         // 31 frames / 16fps = 1.94s (deduplicated from 83 frames)
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
