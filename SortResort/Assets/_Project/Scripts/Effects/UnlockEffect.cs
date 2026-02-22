using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Plays a frame-by-frame unlock animation for locked containers.
    /// Loads frames from Resources/Sprites/UI/Overlays/LockAnimations/{World}/
    /// </summary>
    public class UnlockEffect : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float frameRate = 30f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private float frameDuration;
        private bool isPlaying = false;

        private System.Action onComplete;

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 50; // Same as lock overlay
        }

        /// <summary>
        /// Load animation frames for the given lock overlay image name.
        /// E.g., "island_lockoverlay" loads from LockAnimations/Island/island_lockoverlay_00000.png etc.
        /// </summary>
        public bool LoadFrames(string lockOverlayImage)
        {
            if (string.IsNullOrEmpty(lockOverlayImage))
            {
                Debug.LogWarning("[UnlockEffect] No lock overlay image specified");
                return false;
            }

            // Extract world name from lock overlay image (e.g., "island_lockoverlay" -> "Island")
            string worldName = ExtractWorldName(lockOverlayImage);
            if (string.IsNullOrEmpty(worldName))
            {
                Debug.LogWarning($"[UnlockEffect] Could not extract world name from: {lockOverlayImage}");
                return false;
            }

            // Build the base path
            string basePath = $"Sprites/UI/Overlays/LockAnimations/{worldName}/{lockOverlayImage}";

            // Count how many frames exist (try loading until we fail)
            // Load as Texture2D and create full-rect sprites to prevent Unity alpha-trimming
            var frameList = new System.Collections.Generic.List<Sprite>();
            int maxFrames = 50; // Safety limit

            for (int i = 0; i < maxFrames; i++)
            {
                string framePath = $"{basePath}_{i:D5}";
                Texture2D tex = Resources.Load<Texture2D>(framePath);

                if (tex == null)
                {
                    // No more frames
                    break;
                }

                // Create full-rect sprite to preserve transparent padding (prevents alpha-trimming)
                Sprite frame = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                frameList.Add(frame);
            }

            if (frameList.Count == 0)
            {
                Debug.LogWarning($"[UnlockEffect] No frames found at path: {basePath}_00000");
                return false;
            }

            frames = frameList.ToArray();

            return true;
        }

        /// <summary>
        /// Extract world name from lock overlay image name.
        /// E.g., "island_lockoverlay" -> "Island", "island_single_slot_lockoverlay" -> "Island"
        /// </summary>
        private string ExtractWorldName(string lockOverlayImage)
        {
            // Known world prefixes
            string[] worlds = { "island", "supermarket", "farm", "tavern", "space" };

            string lowerImage = lockOverlayImage.ToLower();

            foreach (var world in worlds)
            {
                if (lowerImage.StartsWith(world))
                {
                    // Capitalize first letter
                    return char.ToUpper(world[0]) + world.Substring(1);
                }
            }

            // Fallback: try to extract from base_lockoverlay format
            if (lowerImage.StartsWith("base"))
            {
                return "Base";
            }

            return null;
        }

        /// <summary>
        /// Play the unlock animation
        /// </summary>
        public void Play(System.Action onCompleteCallback = null)
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError("[UnlockEffect] No frames loaded!");
                onCompleteCallback?.Invoke();
                Destroy(gameObject);
                return;
            }

            onComplete = onCompleteCallback;
            frameDuration = 1f / frameRate;
            currentFrame = 0;
            frameTimer = 0f;
            isPlaying = true;

            // Set first frame
            spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (!isPlaying) return;

            frameTimer += Time.deltaTime;

            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrame++;

                if (currentFrame >= frames.Length)
                {
                    // Animation complete
                    isPlaying = false;
                    onComplete?.Invoke();
                    Destroy(gameObject);
                    return;
                }

                spriteRenderer.sprite = frames[currentFrame];
            }
        }

        /// <summary>
        /// Create and play an unlock effect at the specified transform with given scale
        /// </summary>
        public static UnlockEffect PlayAt(Transform parent, string lockOverlayImage, Vector3 localScale, System.Action onComplete = null)
        {
            var go = new GameObject("UnlockEffect");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = localScale;

            var effect = go.AddComponent<UnlockEffect>();

            if (!effect.LoadFrames(lockOverlayImage))
            {
                // Failed to load frames, destroy and return null
                Destroy(go);
                onComplete?.Invoke();
                return null;
            }

            effect.Play(onComplete);
            return effect;
        }
    }
}
