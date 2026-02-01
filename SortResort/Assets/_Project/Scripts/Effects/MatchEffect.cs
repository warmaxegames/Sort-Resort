using System.Collections;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Plays a frame-by-frame match effect animation and self-destructs when complete.
    /// </summary>
    public class MatchEffect : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float frameRate = 30f;
        [SerializeField] private float scale = 1.5f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private float frameDuration;
        private bool isPlaying = false;

        // Static sprite cache
        private static Sprite[] cachedFrames;
        private static bool framesLoaded = false;

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 100; // Above everything

            transform.localScale = Vector3.one * scale;

            LoadFrames();
        }

        private void LoadFrames()
        {
            // Use cached frames if available
            if (framesLoaded && cachedFrames != null)
            {
                frames = cachedFrames;
                return;
            }

            // Load all frames from Resources
            frames = new Sprite[15];
            for (int i = 0; i < 15; i++)
            {
                string path = $"Sprites/Effects/match effect_{i:D5}";
                frames[i] = Resources.Load<Sprite>(path);

                if (frames[i] == null)
                {
                    Debug.LogWarning($"[MatchEffect] Failed to load frame: {path}");
                }
            }

            // Cache for future use
            cachedFrames = frames;
            framesLoaded = true;

            Debug.Log($"[MatchEffect] Loaded {frames.Length} animation frames");
        }

        /// <summary>
        /// Play the match effect animation at the current position
        /// </summary>
        public void Play()
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError("[MatchEffect] No frames loaded!");
                Destroy(gameObject);
                return;
            }

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
                    Destroy(gameObject);
                    return;
                }

                spriteRenderer.sprite = frames[currentFrame];
            }
        }

        /// <summary>
        /// Spawn a match effect at the specified world position
        /// </summary>
        public static MatchEffect SpawnAt(Vector3 worldPosition)
        {
            var go = new GameObject("MatchEffect");
            go.transform.position = worldPosition;

            var effect = go.AddComponent<MatchEffect>();
            effect.Play();

            return effect;
        }

        /// <summary>
        /// Spawn a match effect at the center of multiple items
        /// </summary>
        public static MatchEffect SpawnAtCenter(params Transform[] transforms)
        {
            if (transforms == null || transforms.Length == 0)
                return null;

            // Calculate center position
            Vector3 center = Vector3.zero;
            int validCount = 0;

            foreach (var t in transforms)
            {
                if (t != null)
                {
                    center += t.position;
                    validCount++;
                }
            }

            if (validCount == 0)
                return null;

            center /= validCount;

            return SpawnAt(center);
        }
    }
}
