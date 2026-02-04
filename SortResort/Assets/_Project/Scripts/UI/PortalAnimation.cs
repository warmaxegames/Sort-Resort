using System;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort.UI
{
    /// <summary>
    /// Plays a portal vortex animation (25 frames at 30fps) as an overlay
    /// at the position/size of the clicked portal button. Holds on last frame,
    /// then hides after the fade duration so it's gone before the fade-in reveals gameplay.
    /// </summary>
    public class PortalAnimation : MonoBehaviour
    {
        public static PortalAnimation Instance { get; private set; }

        private static Sprite[] cachedFrames;

        private Image portalImage;
        private RectTransform portalRect;
        private Canvas canvas;
        private RectTransform canvasRect;

        private bool isPlaying;
        private int currentFrame;
        private float timer;
        private float frameRate = 30f;
        private bool reachedEnd;
        private Action onComplete;
        private bool callbackFired;
        private float hideTimer;

        // Fire the callback at frame 15 so the fade starts while the vortex is still playing
        private const int callbackFrame = 15;
        // How long to wait after callback before hiding (matches fade-out duration)
        private const float hideDuration = 0.55f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            // Create overlay canvas
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = GetComponent<RectTransform>();

            // Create portal image (not fullscreen - will be positioned per-play)
            var imageGO = new GameObject("PortalImage");
            imageGO.transform.SetParent(transform, false);

            portalRect = imageGO.AddComponent<RectTransform>();
            portalRect.anchorMin = new Vector2(0.5f, 0.5f);
            portalRect.anchorMax = new Vector2(0.5f, 0.5f);
            portalRect.pivot = new Vector2(0.5f, 0.5f);

            portalImage = imageGO.AddComponent<Image>();
            portalImage.raycastTarget = false;
            portalImage.preserveAspect = true;

            // Load frames
            LoadFrames();

            // Start hidden
            canvas.enabled = false;
        }

        private void LoadFrames()
        {
            if (cachedFrames != null) return;

            var textures = Resources.LoadAll<Texture2D>("Sprites/UI/Portal");
            if (textures.Length == 0)
            {
                Debug.LogWarning("[PortalAnimation] No frames found at Sprites/UI/Portal");
                cachedFrames = new Sprite[0];
                return;
            }

            Array.Sort(textures, (a, b) => a.name.CompareTo(b.name));

            cachedFrames = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                cachedFrames[i] = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }

            Debug.Log($"[PortalAnimation] Loaded {cachedFrames.Length} portal frames");
        }

        /// <summary>
        /// Ensures the singleton instance exists.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance != null) return;

            var go = new GameObject("PortalAnimation");
            go.AddComponent<PortalAnimation>();
        }

        /// <summary>
        /// Plays the portal animation at the position/size of the given RectTransform,
        /// then invokes the callback partway through so the fade overlaps the vortex.
        /// </summary>
        public void Play(RectTransform sourceButton, Action callback)
        {
            if (isPlaying) return;
            if (cachedFrames == null || cachedFrames.Length == 0)
            {
                callback?.Invoke();
                return;
            }

            onComplete = callback;
            currentFrame = 0;
            timer = 0f;
            hideTimer = -1f;
            reachedEnd = false;
            callbackFired = false;
            isPlaying = true;

            // Position the portal image over the source button
            PositionOverButton(sourceButton);

            // Play the warp sound
            AudioManager.Instance?.PlayWarpSound();

            portalImage.sprite = cachedFrames[0];
            canvas.enabled = true;
        }

        private void Hide()
        {
            isPlaying = false;
            canvas.enabled = false;
        }

        private void PositionOverButton(RectTransform sourceButton)
        {
            // Get the button's world-space corners
            Vector3[] corners = new Vector3[4];
            sourceButton.GetWorldCorners(corners);
            // corners: 0=bottomLeft, 1=topLeft, 2=topRight, 3=bottomRight

            // Convert corners to screen space
            Canvas sourceCanvas = sourceButton.GetComponentInParent<Canvas>();
            Camera sourceCamera = null;
            if (sourceCanvas != null && sourceCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                sourceCamera = sourceCanvas.worldCamera;

            Vector2 screenMin, screenMax;
            if (sourceCamera != null)
            {
                screenMin = sourceCamera.WorldToScreenPoint(corners[0]);
                screenMax = sourceCamera.WorldToScreenPoint(corners[2]);
            }
            else
            {
                // ScreenSpaceOverlay: world coords are already screen pixels
                screenMin = corners[0];
                screenMax = corners[2];
            }

            // Convert screen positions to our overlay canvas local space
            Vector2 localMin, localMax;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenMin, null, out localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenMax, null, out localMax);

            // Set position (center) and size
            Vector2 center = (localMin + localMax) / 2f;
            Vector2 size = new Vector2(
                Mathf.Abs(localMax.x - localMin.x),
                Mathf.Abs(localMax.y - localMin.y));

            portalRect.anchoredPosition = center;
            portalRect.sizeDelta = size;
        }

        public bool IsPlaying => isPlaying;

        private void Update()
        {
            if (!isPlaying) return;

            // Count down hide timer once the fade has been triggered
            if (hideTimer >= 0f)
            {
                hideTimer -= Time.unscaledDeltaTime;
                if (hideTimer <= 0f)
                {
                    Hide();
                    return;
                }
            }

            if (reachedEnd) return;
            if (cachedFrames == null || cachedFrames.Length == 0) return;

            timer += Time.unscaledDeltaTime;
            float frameTime = 1f / frameRate;

            if (timer >= frameTime)
            {
                timer -= frameTime;
                currentFrame++;

                if (currentFrame >= cachedFrames.Length)
                {
                    // Hold on last frame until hide timer expires
                    currentFrame = cachedFrames.Length - 1;
                    reachedEnd = true;
                    return;
                }

                portalImage.sprite = cachedFrames[currentFrame];

                // Fire callback partway through to start the fade while vortex still plays
                if (!callbackFired && currentFrame >= callbackFrame)
                {
                    callbackFired = true;
                    hideTimer = hideDuration;
                    var cb = onComplete;
                    onComplete = null;
                    cb?.Invoke();
                }
            }
        }
    }
}
