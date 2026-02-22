using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        private static readonly string[] ModeFramePaths = {
            "Sprites/UI/Portal/FreePlay",   // FreePlay = 0
            "Sprites/UI/Portal/StarMode",   // StarMode = 1
            "Sprites/UI/Portal/TimerMode",  // TimerMode = 2
            "Sprites/UI/Portal/HardMode"    // HardMode = 3
        };
        private static Sprite[][] modeFrames; // [modeIndex][frameIndex]

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
        private Sprite[] activeFrames; // frames for the currently playing mode
        private GameObject overlayCloneContainer; // cloned overlay visuals rendered on top of vortex

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
            if (modeFrames != null) return;

            modeFrames = new Sprite[ModeFramePaths.Length][];

            for (int m = 0; m < ModeFramePaths.Length; m++)
            {
                var textures = Resources.LoadAll<Texture2D>(ModeFramePaths[m]);
                if (textures.Length == 0)
                {
                    Debug.LogWarning($"[PortalAnimation] No frames found at {ModeFramePaths[m]}");
                    modeFrames[m] = new Sprite[0];
                    continue;
                }

                Array.Sort(textures, (a, b) => a.name.CompareTo(b.name));

                modeFrames[m] = new Sprite[textures.Length];
                for (int i = 0; i < textures.Length; i++)
                {
                    var tex = textures[i];
                    modeFrames[m][i] = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }

                Debug.Log($"[PortalAnimation] Loaded {modeFrames[m].Length} frames for {(GameMode)m}");
            }
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
        /// Plays the mode-specific portal animation at the position/size of the given RectTransform,
        /// then invokes the callback partway through so the fade overlaps the vortex.
        /// </summary>
        public void Play(RectTransform sourceButton, GameMode mode, Action callback)
        {
            if (isPlaying) return;

            int modeIndex = (int)mode;
            if (modeFrames == null || modeIndex < 0 || modeIndex >= modeFrames.Length
                || modeFrames[modeIndex] == null || modeFrames[modeIndex].Length == 0)
            {
                callback?.Invoke();
                return;
            }

            activeFrames = modeFrames[modeIndex];
            onComplete = callback;
            currentFrame = 0;
            timer = 0f;
            hideTimer = -1f;
            reachedEnd = false;
            callbackFired = false;
            isPlaying = true;

            // Enable canvas before ForceUpdateCanvases so the CanvasScaler processes
            // (disabled canvases are skipped, causing wrong size/position on first click)
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();

            // Position the portal image over the source button
            PositionOverButton(sourceButton);

            // Clone active overlay visuals (stars, timer, checkmark) on top of the vortex
            CloneButtonOverlays(sourceButton);

            // Play the warp sound
            AudioManager.Instance?.PlayWarpSound();

            portalImage.sprite = activeFrames[0];
        }

        private void Hide()
        {
            isPlaying = false;
            DestroyOverlayClones();
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

        private void CloneButtonOverlays(RectTransform sourceButton)
        {
            DestroyOverlayClones();

            // Find ResultOverlays child of the portal button
            Transform resultOverlays = sourceButton.Find("ResultOverlays");
            if (resultOverlays == null) return;

            // Check if there are any active overlays worth cloning
            bool hasActiveOverlay = false;
            for (int i = 0; i < resultOverlays.childCount; i++)
            {
                var child = resultOverlays.GetChild(i);
                var img = child.GetComponent<Image>();
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if ((img != null && img.enabled) || (tmp != null && tmp.enabled))
                {
                    hasActiveOverlay = true;
                    break;
                }
            }
            if (!hasActiveOverlay) return;

            // Create container matching portalRect's position and size on our overlay canvas
            var containerGO = new GameObject("OverlayClones");
            containerGO.transform.SetParent(transform, false);
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = portalRect.anchorMin;
            containerRect.anchorMax = portalRect.anchorMax;
            containerRect.pivot = portalRect.pivot;
            containerRect.anchoredPosition = portalRect.anchoredPosition;
            containerRect.sizeDelta = portalRect.sizeDelta;
            overlayCloneContainer = containerGO;

            // Clone each active overlay child
            for (int i = 0; i < resultOverlays.childCount; i++)
            {
                var child = resultOverlays.GetChild(i);
                var childRect = child.GetComponent<RectTransform>();
                if (childRect == null) continue;

                var srcImg = child.GetComponent<Image>();
                var srcTmp = child.GetComponent<TextMeshProUGUI>();

                if (srcImg != null && srcImg.enabled)
                {
                    var cloneGO = new GameObject(child.name + "_Clone");
                    cloneGO.transform.SetParent(containerGO.transform, false);
                    var cloneRect = cloneGO.AddComponent<RectTransform>();
                    CopyRectTransform(childRect, cloneRect);
                    var cloneImg = cloneGO.AddComponent<Image>();
                    cloneImg.sprite = srcImg.sprite;
                    cloneImg.color = srcImg.color;
                    cloneImg.preserveAspect = srcImg.preserveAspect;
                    cloneImg.type = srcImg.type;
                    cloneImg.raycastTarget = false;
                }
                else if (srcTmp != null && srcTmp.enabled)
                {
                    var cloneGO = new GameObject(child.name + "_Clone");
                    cloneGO.transform.SetParent(containerGO.transform, false);
                    var cloneRect = cloneGO.AddComponent<RectTransform>();
                    CopyRectTransform(childRect, cloneRect);
                    var cloneTmp = cloneGO.AddComponent<TextMeshProUGUI>();
                    cloneTmp.text = srcTmp.text;
                    cloneTmp.fontSize = srcTmp.fontSize;
                    cloneTmp.fontStyle = srcTmp.fontStyle;
                    cloneTmp.alignment = srcTmp.alignment;
                    cloneTmp.color = srcTmp.color;
                    cloneTmp.font = srcTmp.font;
                    cloneTmp.raycastTarget = false;
                }
            }
        }

        private void DestroyOverlayClones()
        {
            if (overlayCloneContainer != null)
            {
                Destroy(overlayCloneContainer);
                overlayCloneContainer = null;
            }
        }

        private static void CopyRectTransform(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.offsetMin = src.offsetMin;
            dst.offsetMax = src.offsetMax;
        }

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
            if (activeFrames == null || activeFrames.Length == 0) return;

            timer += Time.unscaledDeltaTime;
            float frameTime = 1f / frameRate;

            if (timer >= frameTime)
            {
                timer -= frameTime;
                currentFrame++;

                if (currentFrame >= activeFrames.Length)
                {
                    // Hold on last frame until hide timer expires
                    currentFrame = activeFrames.Length - 1;
                    reachedEnd = true;
                    return;
                }

                portalImage.sprite = activeFrames[currentFrame];

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
