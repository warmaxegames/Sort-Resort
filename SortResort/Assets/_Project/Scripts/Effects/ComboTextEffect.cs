using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Spawns a combo text sprite (GOOD!/AMAZING!/PERFECT!) with frame-by-frame animation
    /// for the shining intro, then a short hold and LeanTween fade-out. Self-destructs.
    /// Total duration: 1 second (18 frames at 30fps = 0.6s anim + 0.15s hold + 0.25s fade).
    /// </summary>
    public class ComboTextEffect : MonoBehaviour
    {
        private const float FrameRate = 30f;
        private const int FrameCount = 18; // frames 0-17
        private const float HoldDuration = 0.15f;
        private const float FadeDuration = 0.25f;

        // Static caches (loaded once, reused)
        private static Sprite[][] cachedFrames; // [0]=good, [1]=amazing, [2]=perfect
        private static AudioClip[] cachedClips; // [0]=good, [1]=amazing, [2]=perfect
        private static bool assetsLoaded;

        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private int currentFrame;
        private float frameTimer;
        private float frameDuration;
        private bool isAnimating;

        private static void LoadAssets()
        {
            if (assetsLoaded) return;

            cachedFrames = new Sprite[3][];
            string[] folders = { "Sprites/Effects/ComboGood", "Sprites/Effects/ComboAmazing", "Sprites/Effects/ComboPerfect" };
            string[] prefixes = { "good_", "amazing_", "perfect_" };

            for (int w = 0; w < 3; w++)
            {
                cachedFrames[w] = new Sprite[FrameCount];
                for (int i = 0; i < FrameCount; i++)
                {
                    string path = $"{folders[w]}/{prefixes[w]}{i:D5}";
                    cachedFrames[w][i] = Resources.Load<Sprite>(path);
                    if (cachedFrames[w][i] == null)
                        Debug.LogWarning($"[ComboTextEffect] Failed to load frame: {path}");
                }
            }

            cachedClips = new AudioClip[3];
            cachedClips[0] = Resources.Load<AudioClip>("Audio/SFX/combo_good");
            cachedClips[1] = Resources.Load<AudioClip>("Audio/SFX/combo_amazing");
            cachedClips[2] = Resources.Load<AudioClip>("Audio/SFX/combo_perfect");

            for (int i = 0; i < 3; i++)
            {
                if (cachedClips[i] == null)
                    Debug.LogWarning($"[ComboTextEffect] Failed to load combo sound clip index {i}");
            }

            assetsLoaded = true;
        }

        private static int GetAssetIndex(int comboStreak)
        {
            if (comboStreak == 2) return 0; // good
            if (comboStreak == 3) return 1; // amazing
            return 2; // perfect (4+)
        }

        /// <summary>
        /// Spawn a combo text effect above the match position.
        /// </summary>
        public static void Spawn(Vector3 worldPosition, int comboStreak)
        {
            LoadAssets();

            int idx = GetAssetIndex(comboStreak);
            if (cachedFrames[idx][0] == null) return;

            var go = new GameObject("ComboTextEffect");
            go.transform.position = worldPosition;

            var effect = go.AddComponent<ComboTextEffect>();
            effect.Play(cachedFrames[idx], cachedClips[idx]);
        }

        private void Play(Sprite[] animFrames, AudioClip clip)
        {
            frames = animFrames;
            frameDuration = 1f / FrameRate;
            currentFrame = 0;
            frameTimer = 0f;
            isAnimating = true;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 200; // Above MatchEffect (100)
            spriteRenderer.sprite = frames[0];

            // Play combo sound effect
            if (clip != null)
                AudioManager.Instance?.PlaySFX(clip, 1.3f);
        }

        private void Update()
        {
            if (!isAnimating) return;

            frameTimer += Time.deltaTime;
            if (frameTimer < frameDuration) return;

            frameTimer -= frameDuration;
            currentFrame++;

            if (currentFrame >= FrameCount)
            {
                // Animation done - hold on last frame, then fade out
                isAnimating = false;
                spriteRenderer.sprite = frames[FrameCount - 1];
                StartFadeOut();
                return;
            }

            spriteRenderer.sprite = frames[currentFrame];
        }

        private void StartFadeOut()
        {
            LeanTween.delayedCall(gameObject, HoldDuration, () =>
            {
                LeanTween.value(gameObject, 1f, 0f, FadeDuration)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnUpdate((float alpha) =>
                    {
                        if (spriteRenderer != null)
                        {
                            var c = spriteRenderer.color;
                            spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
                        }
                    })
                    .setOnComplete(() => Destroy(gameObject));
            });
        }

        /// <summary>
        /// Immediately destroy all active combo text effects.
        /// Call on level complete/fail to prevent frozen text on screen.
        /// </summary>
        public static void DestroyAll()
        {
            foreach (var effect in FindObjectsByType<ComboTextEffect>(FindObjectsSortMode.None))
            {
                Destroy(effect.gameObject);
            }
        }

        private void OnDestroy()
        {
            LeanTween.cancel(gameObject);
        }
    }
}
