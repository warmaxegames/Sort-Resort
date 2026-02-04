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
        private Vector2[] frameOffsets; // Offset to keep each frame centered like frame 0
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private float frameDuration;
        private bool isPlaying = false;

        private System.Action onComplete;
        private Transform spriteTransform; // Child transform for the sprite to apply offsets

        private void Awake()
        {
            // Create a child object for the sprite so we can offset it
            var spriteGO = new GameObject("Sprite");
            spriteGO.transform.SetParent(transform);
            spriteGO.transform.localPosition = Vector3.zero;
            spriteGO.transform.localScale = Vector3.one;
            spriteTransform = spriteGO.transform;

            spriteRenderer = spriteGO.AddComponent<SpriteRenderer>();
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
            var frameList = new System.Collections.Generic.List<Sprite>();
            int maxFrames = 50; // Safety limit

            for (int i = 0; i < maxFrames; i++)
            {
                string framePath = $"{basePath}_{i:D5}";
                Sprite frame = Resources.Load<Sprite>(framePath);

                if (frame == null)
                {
                    // No more frames
                    break;
                }

                frameList.Add(frame);
            }

            if (frameList.Count == 0)
            {
                Debug.LogWarning($"[UnlockEffect] No frames found at path: {basePath}_00000");
                return false;
            }

            frames = frameList.ToArray();

            // Calculate offsets to keep all frames aligned with frame 0
            // Each sprite's pivot might be different, causing visual shifting
            frameOffsets = new Vector2[frames.Length];

            if (frames.Length > 0)
            {
                // Use frame 0 as the reference - all other frames should align to it
                Sprite refSprite = frames[0];
                Vector2 refPivotNormalized = refSprite.pivot / refSprite.rect.size;
                Vector2 refCenter = new Vector2(0.5f, 0.5f);
                Vector2 refOffset = (refCenter - refPivotNormalized) * refSprite.bounds.size;

                for (int i = 0; i < frames.Length; i++)
                {
                    Sprite sprite = frames[i];
                    Vector2 pivotNormalized = sprite.pivot / sprite.rect.size;
                    Vector2 center = new Vector2(0.5f, 0.5f);
                    Vector2 offset = (center - pivotNormalized) * sprite.bounds.size;

                    // The offset needed to align this frame with frame 0
                    frameOffsets[i] = refOffset - offset;
                }
            }

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
            spriteTransform.localPosition = new Vector3(frameOffsets[0].x, frameOffsets[0].y, 0);
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
                spriteTransform.localPosition = new Vector3(frameOffsets[currentFrame].x, frameOffsets[currentFrame].y, 0);
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
