using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SortResort.UI
{
    /// <summary>
    /// Animates two star sprites orbiting a single shared center point in a "knockout stars" pattern.
    /// Both stars follow the same elliptical path, offset by 180 degrees, crossing in front of
    /// each other as they orbit. Stars appear larger when in front and smaller when behind.
    /// Loop duration matches original reference: 28 frames at 24fps (~1.167s).
    /// </summary>
    public class KOStarsTween : MonoBehaviour
    {
        private Image star1Image;
        private Image star2Image;
        private RectTransform star1Rect;
        private RectTransform star2Rect;
        private RectTransform containerRect;
        private Coroutine animCoroutine;

        private const float LOOP_DURATION = 28f / 24f; // ~1.167s
        private const float MIN_SCALE = 0.72f;
        private const float MAX_SCALE = 1.0f;
        private const float STAR_BASE_SIZE = 82.5f; // 55 * 1.5 = 50% larger

        // Orbit radii as fraction of container size
        private const float RADIUS_X_FRAC = 0.35f;
        private const float RADIUS_Y_FRAC = 0.30f;

        /// <summary>
        /// Creates and initializes the KO stars effect as a child of the given parent.
        /// </summary>
        public static KOStarsTween Create(Transform parent, Sprite starSprite, Vector2 containerSize)
        {
            var go = new GameObject("KO Stars");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = containerSize;

            var tween = go.AddComponent<KOStarsTween>();
            tween.containerRect = rect;
            tween.CreateStars(starSprite);
            return tween;
        }

        private void CreateStars(Sprite starSprite)
        {
            // Star 1
            var s1GO = new GameObject("Star1");
            s1GO.transform.SetParent(transform, false);
            star1Rect = s1GO.AddComponent<RectTransform>();
            star1Rect.anchorMin = new Vector2(0.5f, 0.5f);
            star1Rect.anchorMax = new Vector2(0.5f, 0.5f);
            star1Rect.pivot = new Vector2(0.5f, 0.5f);
            star1Rect.sizeDelta = new Vector2(STAR_BASE_SIZE, STAR_BASE_SIZE);
            star1Image = s1GO.AddComponent<Image>();
            star1Image.sprite = starSprite;
            star1Image.preserveAspect = true;
            star1Image.raycastTarget = false;

            // Star 2
            var s2GO = new GameObject("Star2");
            s2GO.transform.SetParent(transform, false);
            star2Rect = s2GO.AddComponent<RectTransform>();
            star2Rect.anchorMin = new Vector2(0.5f, 0.5f);
            star2Rect.anchorMax = new Vector2(0.5f, 0.5f);
            star2Rect.pivot = new Vector2(0.5f, 0.5f);
            star2Rect.sizeDelta = new Vector2(STAR_BASE_SIZE, STAR_BASE_SIZE);
            star2Image = s2GO.AddComponent<Image>();
            star2Image.sprite = starSprite;
            star2Image.preserveAspect = true;
            star2Image.raycastTarget = false;
        }

        public void Play()
        {
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);
            gameObject.SetActive(true);
            animCoroutine = StartCoroutine(AnimateLoop());
        }

        public void Stop()
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateLoop()
        {
            float radiusX = containerRect.sizeDelta.x * RADIUS_X_FRAC;
            float radiusY = containerRect.sizeDelta.y * RADIUS_Y_FRAC;
            float startTime = Time.unscaledTime;

            while (true)
            {
                float elapsed = Time.unscaledTime - startTime;
                float t = (elapsed % LOOP_DURATION) / LOOP_DURATION;
                float angle = -t * Mathf.PI * 2f; // clockwise

                // Both stars orbit the same center, 180 degrees apart
                ApplyOrbit(star1Rect, angle, radiusX, radiusY);
                ApplyOrbit(star2Rect, angle + Mathf.PI, radiusX, radiusY);

                // Depth ordering: star closer to "front" (bottom of ellipse) renders on top
                // sin(angle) < 0 means bottom = front; sin(angle) > 0 means top = back
                float depth1 = -Mathf.Sin(angle);
                float depth2 = -Mathf.Sin(angle + Mathf.PI);
                if (depth1 >= depth2)
                    star1Rect.SetAsLastSibling();
                else
                    star2Rect.SetAsLastSibling();

                yield return null;
            }
        }

        private void ApplyOrbit(RectTransform rt, float angle, float radiusX, float radiusY)
        {
            float x = Mathf.Cos(angle) * radiusX;
            float y = Mathf.Sin(angle) * radiusY;

            // Scale based on depth: bottom of ellipse (sin < 0) = front = larger,
            // top of ellipse (sin > 0) = back = smaller
            float depthT = (Mathf.Sin(angle) + 1f) * 0.5f; // 0 (bottom/front) to 1 (top/back)
            float scale = Mathf.Lerp(MAX_SCALE, MIN_SCALE, depthT);

            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDisable()
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
        }
    }
}
