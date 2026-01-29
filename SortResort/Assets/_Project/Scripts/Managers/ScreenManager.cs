using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Manages screen scaling and layout for different aspect ratios.
    /// Ensures the game looks correct on portrait phones, tablets, and landscape.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Reference Settings")]
        [SerializeField] private float referenceWidth = 1080f;
        [SerializeField] private float referenceHeight = 1920f;
        [SerializeField] private float baseOrthoSize = 8f;

        [Header("Scaling")]
        [SerializeField] private Transform gameWorldParent;
        [SerializeField] private float minWorldScale = 0.6f;
        [SerializeField] private float maxWorldScale = 1.2f;

        // Calculated values
        private float currentAspect;
        private float worldScale = 1f;
        private Camera mainCamera;

        // Properties
        public float WorldScale => worldScale;
        public float CurrentAspect => currentAspect;
        public bool IsPortrait => currentAspect < 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            UpdateScreenLayout();
        }

        private void Update()
        {
            // Check for resolution changes (editor, device rotation)
            float newAspect = (float)Screen.width / Screen.height;
            if (Mathf.Abs(newAspect - currentAspect) > 0.01f)
            {
                UpdateScreenLayout();
            }
        }

        /// <summary>
        /// Update the layout based on current screen dimensions
        /// </summary>
        public void UpdateScreenLayout()
        {
            currentAspect = (float)Screen.width / Screen.height;
            float referenceAspect = referenceWidth / referenceHeight;

            Debug.Log($"[ScreenManager] Screen: {Screen.width}x{Screen.height}, Aspect: {currentAspect:F2}, Reference: {referenceAspect:F2}");

            // Calculate camera size to fit content
            if (mainCamera != null && mainCamera.orthographic)
            {
                // For portrait (aspect < 1), we need a taller view
                // For landscape (aspect > 1), we need a wider view
                if (currentAspect < referenceAspect)
                {
                    // Screen is narrower than reference - increase ortho size to show more height
                    mainCamera.orthographicSize = baseOrthoSize * (referenceAspect / currentAspect);
                }
                else
                {
                    // Screen is wider than reference - use base size
                    mainCamera.orthographicSize = baseOrthoSize;
                }

                Debug.Log($"[ScreenManager] Camera orthoSize: {mainCamera.orthographicSize:F2}");
            }

            // Calculate world scale based on aspect ratio
            // Portrait phones need smaller containers to fit side-by-side
            if (IsPortrait)
            {
                // Scale down for narrow screens
                worldScale = Mathf.Clamp(currentAspect / referenceAspect * 1.2f, minWorldScale, 1f);
            }
            else
            {
                // Scale up slightly for wide screens
                worldScale = Mathf.Clamp(currentAspect / referenceAspect, 1f, maxWorldScale);
            }

            // Apply scale to game world parent
            if (gameWorldParent != null)
            {
                gameWorldParent.localScale = Vector3.one * worldScale;
                Debug.Log($"[ScreenManager] World scale: {worldScale:F2}");
            }
        }

        /// <summary>
        /// Set the game world parent transform for scaling
        /// </summary>
        public void SetGameWorldParent(Transform parent)
        {
            gameWorldParent = parent;
            UpdateScreenLayout();
        }

        /// <summary>
        /// Convert screen position to world position (accounting for scale)
        /// </summary>
        public Vector3 ScreenToWorldPosition(Vector3 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;
            return mainCamera.ScreenToWorldPoint(screenPos);
        }

        /// <summary>
        /// Get the visible world bounds at z=0
        /// </summary>
        public Rect GetVisibleWorldBounds()
        {
            if (mainCamera == null) return Rect.zero;

            float height = mainCamera.orthographicSize * 2f;
            float width = height * currentAspect;

            return new Rect(
                mainCamera.transform.position.x - width / 2f,
                mainCamera.transform.position.y - height / 2f,
                width,
                height
            );
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
