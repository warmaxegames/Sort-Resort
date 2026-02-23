using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SortResort
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Loading Screen")]
        [SerializeField] private bool useLoadingScreen = false;
        [SerializeField] private float minimumLoadingTime = 0.5f;

        private Canvas transitionCanvas;
        private Image fadeImage;
        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        public event Action OnTransitionStarted;
        public event Action OnTransitionCompleted;
        public event Action<float> OnLoadProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            CreateTransitionCanvas();
        }

        private void CreateTransitionCanvas()
        {
            var canvasObj = new GameObject("TransitionCanvas");
            canvasObj.transform.SetParent(transform);

            transitionCanvas = canvasObj.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 9999;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            var imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = true;

            var rectTransform = fadeImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            fadeImage.gameObject.SetActive(false);
        }

        public void LoadScene(string sceneName, Action onComplete = null)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[TransitionManager] Already transitioning, ignoring request");
                return;
            }

            StartCoroutine(TransitionToScene(sceneName, onComplete));
        }

        public void LoadScene(int sceneIndex, Action onComplete = null)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[TransitionManager] Already transitioning, ignoring request");
                return;
            }

            string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
            StartCoroutine(TransitionToScene(sceneName, onComplete));
        }

        private IEnumerator TransitionToScene(string sceneName, Action onComplete)
        {
            isTransitioning = true;
            OnTransitionStarted?.Invoke();

            // Fade out
            yield return StartCoroutine(Fade(0f, 1f));

            // Load scene
            if (useLoadingScreen)
            {
                yield return StartCoroutine(LoadSceneAsync(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
                yield return null; // Wait one frame for scene to initialize
            }

            // Fade in
            yield return StartCoroutine(Fade(1f, 0f));

            isTransitioning = false;
            OnTransitionCompleted?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            float startTime = Time.unscaledTime;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                OnLoadProgress?.Invoke(progress);

                if (operation.progress >= 0.9f)
                {
                    float elapsed = Time.unscaledTime - startTime;
                    if (elapsed >= minimumLoadingTime)
                    {
                        operation.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            OnLoadProgress?.Invoke(1f);
        }

        private IEnumerator Fade(float startAlpha, float endAlpha)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / fadeDuration);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, endAlpha);

            if (endAlpha <= 0f)
            {
                fadeImage.gameObject.SetActive(false);
            }
        }

        // Quick fade methods
        public void FadeIn(Action onComplete = null)
        {
            StartCoroutine(FadeInCoroutine(onComplete));
        }

        public void FadeOut(Action onComplete = null)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }

        private IEnumerator FadeInCoroutine(Action onComplete)
        {
            yield return StartCoroutine(Fade(1f, 0f));
            onComplete?.Invoke();
        }

        private IEnumerator FadeOutCoroutine(Action onComplete)
        {
            yield return StartCoroutine(Fade(0f, 1f));
            onComplete?.Invoke();
        }

        // Flash effect for feedback
        public void Flash(Color color, float duration = 0.1f)
        {
            StartCoroutine(FlashCoroutine(color, duration));
        }

        private IEnumerator FlashCoroutine(Color color, float duration)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = color;

            yield return new WaitForSecondsRealtime(duration);

            float elapsed = 0f;
            float fadeDur = duration;

            while (elapsed < fadeDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / fadeDur);
                fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            fadeImage.gameObject.SetActive(false);
        }

        public void SetFadeColor(Color color)
        {
            fadeColor = color;
        }

        public void SetFadeDuration(float duration)
        {
            fadeDuration = Mathf.Max(0.1f, duration);
        }
    }
}
