using System;
using System.Collections;
using UnityEngine;

namespace SortResort.UI
{
    public abstract class BaseScreen : MonoBehaviour
    {
        [Header("Screen Settings")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float fadeInDuration = 0.3f;
        [SerializeField] protected float fadeOutDuration = 0.2f;

        public bool IsVisible { get; protected set; }
        public bool IsTransitioning { get; protected set; }

        public event Action OnScreenShown;
        public event Action OnScreenHidden;

        protected virtual void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        protected virtual void SubscribeToEvents() { }
        protected virtual void UnsubscribeFromEvents() { }

        public virtual void Show(bool instant = false)
        {
            if (IsVisible && !IsTransitioning) return;

            gameObject.SetActive(true);

            if (instant)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                IsVisible = true;
                OnShowComplete();
            }
            else
            {
                StartCoroutine(FadeIn());
            }
        }

        public virtual void Hide(bool instant = false)
        {
            if (!IsVisible && !IsTransitioning) return;

            if (instant)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                IsVisible = false;
                gameObject.SetActive(false);
                OnHideComplete();
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }

        protected virtual IEnumerator FadeIn()
        {
            IsTransitioning = true;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            IsVisible = true;
            IsTransitioning = false;

            OnShowComplete();
        }

        protected virtual IEnumerator FadeOut()
        {
            IsTransitioning = true;
            canvasGroup.interactable = false;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            IsVisible = false;
            IsTransitioning = false;

            gameObject.SetActive(false);
            OnHideComplete();
        }

        protected virtual void OnShowComplete()
        {
            OnScreenShown?.Invoke();
        }

        protected virtual void OnHideComplete()
        {
            OnScreenHidden?.Invoke();
        }

        public void SetInteractable(bool interactable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
            }
        }
    }
}
