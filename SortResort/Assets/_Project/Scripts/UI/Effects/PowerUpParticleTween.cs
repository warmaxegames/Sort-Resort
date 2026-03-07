using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort.UI
{
    /// <summary>
    /// Spawns 3 particle images that burst from a source position and arc toward a target
    /// position in a fan pattern with bezier curves. Used for Moves Freeze (stars) and
    /// Time Freeze (snowflakes) power-up activation feedback.
    /// </summary>
    public class PowerUpParticleTween : MonoBehaviour
    {
        private const int PARTICLE_COUNT = 3;
        private const float DURATION = 0.75f;
        private const float STAGGER_DELAY = 0.075f;
        private const float PARTICLE_SIZE = 90f;
        private const float FAN_SPREAD = 60f; // pixels of lateral spread at midpoint
        private const float ARC_HEIGHT = 80f; // pixels of upward bulge at midpoint
        private const float SPIN_DEGREES = 180f; // rotation during flight (snowflakes)

        /// <summary>
        /// Spawns the particle effect on the given canvas.
        /// </summary>
        /// <param name="canvasTransform">Parent canvas transform</param>
        /// <param name="sprite">Star or snowflake sprite</param>
        /// <param name="startAnchor">Anchor position of the power-up button (normalized)</param>
        /// <param name="endAnchor">Anchor position of the target HUD counter (normalized)</param>
        /// <param name="spin">Whether particles should rotate during flight</param>
        public static void Play(Transform canvasTransform, Sprite sprite,
            Vector2 startAnchor, Vector2 endAnchor, bool spin = false)
        {
            if (sprite == null || canvasTransform == null) return;

            var go = new GameObject("PowerUpParticleTween");
            go.transform.SetParent(canvasTransform, false);
            go.transform.SetAsLastSibling(); // Render on top of all other UI
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tween = go.AddComponent<PowerUpParticleTween>();
            tween.StartCoroutine(tween.RunEffect(sprite, startAnchor, endAnchor, spin));
        }

        private IEnumerator RunEffect(Sprite sprite, Vector2 startAnchor, Vector2 endAnchor, bool spin)
        {
            var rt = GetComponent<RectTransform>();
            var canvasSize = rt.rect.size;

            // Convert anchors to local positions within the fullscreen rect
            Vector2 startPos = new Vector2(startAnchor.x * canvasSize.x, startAnchor.y * canvasSize.y);
            Vector2 endPos = new Vector2(endAnchor.x * canvasSize.x, endAnchor.y * canvasSize.y);

            // Fan offsets: left, center, right
            float[] fanOffsets = { -FAN_SPREAD, 0f, FAN_SPREAD };

            // Direction perpendicular to the travel path (for fan spread)
            Vector2 travel = endPos - startPos;
            Vector2 perpendicular = new Vector2(-travel.normalized.y, travel.normalized.x);

            // Create particles
            var particles = new RectTransform[PARTICLE_COUNT];
            var images = new Image[PARTICLE_COUNT];
            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                var pGO = new GameObject($"Particle_{i}");
                pGO.transform.SetParent(transform, false);
                var pRect = pGO.AddComponent<RectTransform>();
                pRect.anchorMin = Vector2.zero;
                pRect.anchorMax = Vector2.zero;
                pRect.pivot = new Vector2(0.5f, 0.5f);
                pRect.sizeDelta = new Vector2(PARTICLE_SIZE, PARTICLE_SIZE);
                pRect.anchoredPosition = startPos;
                pRect.localScale = Vector3.one * 0.4f;

                var img = pGO.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f); // start invisible

                particles[i] = pRect;
                images[i] = img;
            }

            // Animate all particles with stagger
            float totalDuration = DURATION + STAGGER_DELAY * (PARTICLE_COUNT - 1);
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < PARTICLE_COUNT; i++)
                {
                    float particleElapsed = elapsed - i * STAGGER_DELAY;
                    if (particleElapsed < 0f) continue;

                    float t = Mathf.Clamp01(particleElapsed / DURATION);

                    // Smooth ease-out curve
                    float eased = 1f - (1f - t) * (1f - t);

                    // Bezier arc: midpoint offset for fan spread + upward arc
                    Vector2 midOffset = perpendicular * fanOffsets[i] + Vector2.up * ARC_HEIGHT;
                    Vector2 midPoint = (startPos + endPos) * 0.5f + midOffset;

                    // Quadratic bezier: B(t) = (1-t)^2*P0 + 2(1-t)t*P1 + t^2*P2
                    float oneMinusT = 1f - eased;
                    Vector2 pos = oneMinusT * oneMinusT * startPos
                                + 2f * oneMinusT * eased * midPoint
                                + eased * eased * endPos;

                    particles[i].anchoredPosition = pos;

                    // Scale: ramp up 0.4->0.7 over first 30%, then down to 0.3 over last 30%
                    float scale;
                    if (t < 0.3f)
                        scale = Mathf.Lerp(0.4f, 0.7f, t / 0.3f);
                    else if (t < 0.7f)
                        scale = 0.7f;
                    else
                        scale = Mathf.Lerp(0.7f, 0.3f, (t - 0.7f) / 0.3f);
                    particles[i].localScale = Vector3.one * scale;

                    // Alpha: fade in over first 10%, hold, fade out over last 30%
                    float alpha;
                    if (t < 0.1f)
                        alpha = Mathf.Lerp(0f, 1f, t / 0.1f);
                    else if (t < 0.7f)
                        alpha = 1f;
                    else
                        alpha = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                    images[i].color = new Color(1f, 1f, 1f, alpha);

                    // Rotation (snowflakes spin)
                    if (spin)
                    {
                        float angle = Mathf.Lerp(0f, SPIN_DEGREES, eased);
                        particles[i].localRotation = Quaternion.Euler(0f, 0f, angle);
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
