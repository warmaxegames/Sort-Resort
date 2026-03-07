using System.Collections;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Plays a frame-by-frame hammer hit animation at a world position, then self-destructs.
    /// Frames are loaded from Resources/Sprites/Effects/HammerHitEffect/.
    /// </summary>
    public class HammerHitEffect : MonoBehaviour
    {
        private static Sprite[] cachedFrames;

        private SpriteRenderer spriteRenderer;
        private const float FRAME_RATE = 24f;
        private const float WORLD_SCALE = 1.15f;
        // Pivot positioned at the blue sparks impact point (where the hammer strikes)
        // so the click position aligns with the spark center
        private static readonly Vector2 SPARKS_PIVOT = new Vector2(0.3645f, 0.4918f);

        public static void Play(Vector3 worldPosition)
        {
            var frames = GetFrames();
            if (frames == null || frames.Length == 0) return;

            var go = new GameObject("HammerHitEffect");
            go.transform.position = worldPosition;
            go.transform.localScale = Vector3.one * WORLD_SCALE;
            var effect = go.AddComponent<HammerHitEffect>();
            effect.StartAnimation(frames);
        }

        private static Sprite[] GetFrames()
        {
            if (cachedFrames != null) return cachedFrames;

            var textures = Resources.LoadAll<Texture2D>("Sprites/Effects/HammerHitEffect");
            if (textures.Length == 0)
            {
                Debug.LogWarning("[HammerHitEffect] No frames found");
                return null;
            }

            System.Array.Sort(textures, (a, b) => a.name.CompareTo(b.name));
            cachedFrames = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                cachedFrames[i] = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    SPARKS_PIVOT, 100f);
            }

            Debug.Log($"[HammerHitEffect] Loaded {cachedFrames.Length} frames");
            return cachedFrames;
        }

        private void StartAnimation(Sprite[] frames)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 500; // Above containers and items
            spriteRenderer.sprite = frames[0];
            StartCoroutine(AnimateFrames(frames));
        }

        private IEnumerator AnimateFrames(Sprite[] frames)
        {
            float frameTime = 1f / FRAME_RATE;
            float timer = 0f;
            int currentFrame = 0;

            while (currentFrame < frames.Length - 1)
            {
                timer += Time.deltaTime;
                if (timer >= frameTime)
                {
                    timer -= frameTime;
                    currentFrame++;
                    spriteRenderer.sprite = frames[currentFrame];
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
