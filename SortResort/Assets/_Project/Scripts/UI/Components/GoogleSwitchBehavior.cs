using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SortResort.UI
{
    /// <summary>
    /// Handles the Google-style switch animation and color changes.
    /// Attach to a Toggle component to get sliding handle behavior.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class GoogleSwitchBehavior : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color onTrackColor = new Color(0.2f, 0.7f, 0.4f, 1f);  // Green
        [SerializeField] private Color offTrackColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
        [SerializeField] private Color onHandleColor = Color.white;
        [SerializeField] private Color offHandleColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.15f;

        private Toggle toggle;
        private Image trackImage;
        private RectTransform handleRect;
        private Image handleImage;

        private float handleOnX;
        private float handleOffX = 4f;
        private Coroutine animationCoroutine;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        /// <summary>
        /// Initialize the switch with references to track and handle
        /// </summary>
        public void Initialize(Image track, RectTransform handle)
        {
            trackImage = track;
            handleRect = handle;
            handleImage = handle.GetComponent<Image>();

            // Calculate handle positions based on track width
            // Handle starts at 4px from left edge when off
            // Handle ends at (trackWidth - handleWidth - 4px) when on
            float trackWidth = 100f; // Default width
            float handleWidth = 42f; // Default handle size
            handleOffX = 4f;
            handleOnX = trackWidth - handleWidth - 4f;

            // Set initial state without animation
            UpdateVisuals(toggle.isOn, instant: true);
        }

        private void OnToggleChanged(bool isOn)
        {
            UpdateVisuals(isOn, instant: false);
        }

        private void UpdateVisuals(bool isOn, bool instant)
        {
            if (trackImage == null || handleRect == null) return;

            if (instant)
            {
                // Instant update
                trackImage.color = isOn ? onTrackColor : offTrackColor;
                if (handleImage != null)
                    handleImage.color = isOn ? onHandleColor : offHandleColor;

                var pos = handleRect.anchoredPosition;
                pos.x = isOn ? handleOnX : handleOffX;
                handleRect.anchoredPosition = pos;
            }
            else
            {
                // Animated update
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }
                animationCoroutine = StartCoroutine(AnimateSwitch(isOn));
            }
        }

        private IEnumerator AnimateSwitch(bool isOn)
        {
            float startX = handleRect.anchoredPosition.x;
            float targetX = isOn ? handleOnX : handleOffX;
            Color startTrackColor = trackImage.color;
            Color targetTrackColor = isOn ? onTrackColor : offTrackColor;
            Color startHandleColor = handleImage != null ? handleImage.color : Color.white;
            Color targetHandleColor = isOn ? onHandleColor : offHandleColor;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);

                // Animate handle position
                var pos = handleRect.anchoredPosition;
                pos.x = Mathf.Lerp(startX, targetX, t);
                handleRect.anchoredPosition = pos;

                // Animate track color
                trackImage.color = Color.Lerp(startTrackColor, targetTrackColor, t);

                // Animate handle color
                if (handleImage != null)
                    handleImage.color = Color.Lerp(startHandleColor, targetHandleColor, t);

                yield return null;
            }

            // Ensure final state
            var finalPos = handleRect.anchoredPosition;
            finalPos.x = targetX;
            handleRect.anchoredPosition = finalPos;
            trackImage.color = targetTrackColor;
            if (handleImage != null)
                handleImage.color = targetHandleColor;

            animationCoroutine = null;
        }

        private void OnDestroy()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
        }
    }
}
