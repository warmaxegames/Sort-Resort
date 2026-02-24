using UnityEngine;
using UnityEngine.UI;

namespace SortResort
{
    /// <summary>
    /// Spawns an animated "+1"/"+3"/"+5" bonus timer sprite above the HUD timer
    /// when combo streaks trigger in TimerMode/HardMode. Adds bonus seconds to the timer.
    /// Animation timing matches ComboTextEffect (1s total: pop-in + hold + fade-out).
    /// </summary>
    public class ComboTimerBonus : MonoBehaviour
    {
        // Match ComboTextEffect timing exactly
        private const float PopInDuration = 0.2f;
        private const float HoldDuration = 0.55f; // 0.6s anim + 0.15s hold - popIn = 0.55s
        private const float FadeDuration = 0.25f;
        // Total: 0.2 + 0.55 + 0.25 = 1.0s (matches combo text)

        private static Sprite[] cachedSprites; // [0]=plus_1, [1]=plus_3, [2]=plus_5
        private static bool assetsLoaded;

        private Image image;
        private RectTransform rectTransform;

        private static void LoadAssets()
        {
            if (assetsLoaded) return;

            cachedSprites = new Sprite[3];
            string[] names = { "plus_1", "plus_3", "plus_5" };

            for (int i = 0; i < 3; i++)
            {
                var tex = Resources.Load<Texture2D>($"Sprites/Effects/{names[i]}");
                if (tex != null)
                {
                    cachedSprites[i] = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }
                else
                {
                    Debug.LogWarning($"[ComboTimerBonus] Failed to load: Sprites/Effects/{names[i]}");
                }
            }

            assetsLoaded = true;
        }

        /// <summary>
        /// Spawn a timer bonus effect above the HUD timer and add time.
        /// Only works in TimerMode/HardMode when timer is active.
        /// </summary>
        public static void Spawn(int comboStreak)
        {
            // Only in timer-active modes
            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            if (mode != GameMode.TimerMode && mode != GameMode.HardMode) return;
            if (LevelManager.Instance == null || !LevelManager.Instance.IsTimerActive) return;

            LoadAssets();

            int idx = comboStreak == 2 ? 0 : comboStreak == 3 ? 1 : 2;
            float bonusSeconds = comboStreak == 2 ? 1f : comboStreak == 3 ? 3f : 5f;

            if (cachedSprites[idx] == null) return;

            // Add time to the timer
            LevelManager.Instance.AddTime(bonusSeconds);

            // Find the overlay timer text to position relative to it
            var timerGO = GameObject.Find("Overlay Timer");
            if (timerGO == null) return;

            var timerRect = timerGO.GetComponent<RectTransform>();
            if (timerRect == null) return;

            // Create the bonus image as a sibling of the timer text (same parent = HUD overlay)
            var go = new GameObject("ComboTimerBonus");
            go.transform.SetParent(timerRect.parent, false);

            var bonus = go.AddComponent<ComboTimerBonus>();
            bonus.Setup(cachedSprites[idx], timerRect);
        }

        private void Setup(Sprite sprite, RectTransform timerRect)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();

            // Copy the timer's anchor position so we're at the same vertical level
            rectTransform.anchorMin = timerRect.anchorMin;
            rectTransform.anchorMax = timerRect.anchorMax;
            rectTransform.pivot = new Vector2(0f, 0.5f); // Left-anchored so it sits to the right

            // Position to the right of the timer on the wood bar
            // Timer sizeDelta.x is 174, so half-width is 87. Offset a bit past that.
            rectTransform.anchoredPosition = new Vector2(timerRect.sizeDelta.x * 0.5f + 5f, 0f);
            rectTransform.sizeDelta = new Vector2(100f, 70f);

            image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Start invisible and small for pop-in
            image.color = new Color(1f, 1f, 1f, 0f);
            rectTransform.localScale = Vector3.zero;

            PlayAnimation();
        }

        private void PlayAnimation()
        {
            // Phase 1: Pop-in (scale 0 -> 1.15 -> 1.0 with alpha 0 -> 1)
            LeanTween.scale(rectTransform, Vector3.one * 1.15f, PopInDuration * 0.6f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    LeanTween.scale(rectTransform, Vector3.one, PopInDuration * 0.4f)
                        .setEase(LeanTweenType.easeInOutQuad);
                });

            LeanTween.value(gameObject, 0f, 1f, PopInDuration * 0.5f)
                .setOnUpdate((float alpha) =>
                {
                    if (image != null)
                        image.color = new Color(1f, 1f, 1f, alpha);
                });

            // Phase 2: Float upward slightly during hold
            float startY = rectTransform.anchoredPosition.y;
            LeanTween.value(gameObject, startY, startY + 15f, PopInDuration + HoldDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnUpdate((float y) =>
                {
                    if (rectTransform != null)
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
                });

            // Phase 3: Fade-out (matching combo text fade timing)
            LeanTween.delayedCall(gameObject, PopInDuration + HoldDuration, () =>
            {
                LeanTween.value(gameObject, 1f, 0f, FadeDuration)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnUpdate((float alpha) =>
                    {
                        if (image != null)
                            image.color = new Color(1f, 1f, 1f, alpha);
                    })
                    .setOnComplete(() => Destroy(gameObject));
            });
        }

        /// <summary>
        /// Immediately destroy all active timer bonus effects.
        /// </summary>
        public static void DestroyAll()
        {
            foreach (var effect in FindObjectsByType<ComboTimerBonus>(FindObjectsSortMode.None))
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
