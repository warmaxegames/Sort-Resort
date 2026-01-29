using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Sets up a complete game scene and loads a level from JSON.
    /// Attach this to an empty GameObject in a new scene and press Play.
    ///
    /// Prerequisites:
    /// 1. Run Tools > Sort Resort > Generate Prefabs (creates prefabs in Assets/_Project/Prefabs/)
    /// 2. Move prefabs to Resources: Assets/_Project/Resources/Prefabs/
    /// 3. Sprites should be in: Assets/_Project/Resources/Sprites/Items/{World}/
    /// 4. Audio should be in: Assets/_Project/Resources/Audio/{SFX|UI|Music}/
    ///    (Copy from Assets/_Project/Audio/ to Assets/_Project/Resources/Audio/)
    /// </summary>
    public class GameSceneSetup : MonoBehaviour
    {
        [Header("Level to Load")]
        [SerializeField] private string worldId = "resort";
        [SerializeField] private int levelNumber = 1;

        [Header("Layer Configuration")]
        [SerializeField] private int itemLayer = 6;  // "Items" layer
        [SerializeField] private int slotLayer = 7;  // "Slots" layer

        private Camera mainCamera;

        private void Start()
        {
            Debug.Log("=== Game Scene Setup Starting ===");

            SetupCamera();
            SetupManagers();
            LoadLevel();

            Debug.Log("=== Game Scene Setup Complete ===");
        }

        private void SetupCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var camGO = new GameObject("Main Camera");
                mainCamera = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }

            mainCamera.orthographic = true;

            // Portrait phone aspect ratio (9:16 or similar)
            // Increase orthographic size for taller viewport
            mainCamera.orthographicSize = 8f;
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = new Color(0.4f, 0.7f, 0.9f);

            // Force portrait aspect ratio in editor for testing
            // Target: 9:16 (1080x1920) phone resolution
            #if UNITY_EDITOR
            // Note: In editor, you can set Game view to a portrait resolution
            // like 1080x1920 or 750x1334 (iPhone) for proper testing
            #endif

            // Add URP camera data if needed
            var urpCameraData = mainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpCameraData == null)
            {
                mainCamera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            }

            Debug.Log("[GameSceneSetup] Camera configured");
        }

        private void SetupManagers()
        {
            var managersRoot = new GameObject("--- MANAGERS ---");

            // GameManager
            var gmGO = new GameObject("GameManager");
            gmGO.transform.SetParent(managersRoot.transform);
            gmGO.AddComponent<GameManager>();
            Debug.Log("[GameSceneSetup] GameManager created");

            // ScreenManager (for responsive scaling)
            var screenMgrGO = new GameObject("ScreenManager");
            screenMgrGO.transform.SetParent(managersRoot.transform);
            screenMgrGO.AddComponent<ScreenManager>();
            Debug.Log("[GameSceneSetup] ScreenManager created");

            // DragDropManager
            var ddGO = new GameObject("DragDropManager");
            ddGO.transform.SetParent(managersRoot.transform);
            var ddm = ddGO.AddComponent<DragDropManager>();
            ConfigureDragDropManager(ddm);
            Debug.Log("[GameSceneSetup] DragDropManager created");

            // LevelManager
            var lmGO = new GameObject("LevelManager");
            lmGO.transform.SetParent(managersRoot.transform);
            var lm = lmGO.AddComponent<LevelManager>();
            ConfigureLevelManager(lm);
            Debug.Log("[GameSceneSetup] LevelManager created");

            // UIManager
            var uiGO = new GameObject("UIManager");
            uiGO.transform.SetParent(managersRoot.transform);
            var uim = uiGO.AddComponent<UIManager>();
            uim.CreateRuntimeUI();
            Debug.Log("[GameSceneSetup] UIManager created with runtime UI");

            // AudioManager
            var amGO = new GameObject("AudioManager");
            amGO.transform.SetParent(managersRoot.transform);
            amGO.AddComponent<AudioManager>();
            Debug.Log("[GameSceneSetup] AudioManager created");

            // SaveManager
            var smGO = new GameObject("SaveManager");
            smGO.transform.SetParent(managersRoot.transform);
            smGO.AddComponent<SaveManager>();
            Debug.Log("[GameSceneSetup] SaveManager created");
        }

        private void ConfigureDragDropManager(DragDropManager ddm)
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            var itemLayerField = typeof(DragDropManager).GetField("itemLayerMask", bindingFlags);
            var slotLayerField = typeof(DragDropManager).GetField("slotLayerMask", bindingFlags);
            var cameraField = typeof(DragDropManager).GetField("gameCamera", bindingFlags);

            itemLayerField?.SetValue(ddm, (LayerMask)(1 << itemLayer));
            slotLayerField?.SetValue(ddm, (LayerMask)(1 << slotLayer));
            cameraField?.SetValue(ddm, mainCamera);
        }

        private void ConfigureLevelManager(LevelManager lm)
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Load prefabs from Resources
            var itemPrefab = Resources.Load<GameObject>("Prefabs/Item");
            var containerPrefab = Resources.Load<GameObject>("Prefabs/Container");
            var singleSlotContainerPrefab = Resources.Load<GameObject>("Prefabs/SingleSlotContainer");

            if (itemPrefab == null)
                Debug.LogError("[GameSceneSetup] Failed to load Item prefab from Resources/Prefabs/Item");
            if (containerPrefab == null)
                Debug.LogError("[GameSceneSetup] Failed to load Container prefab from Resources/Prefabs/Container");
            if (singleSlotContainerPrefab == null)
                Debug.LogError("[GameSceneSetup] Failed to load SingleSlotContainer prefab from Resources/Prefabs/SingleSlotContainer");

            // Assign prefabs via reflection
            var itemPrefabField = typeof(LevelManager).GetField("itemPrefab", bindingFlags);
            var containerPrefabField = typeof(LevelManager).GetField("containerPrefab", bindingFlags);
            var singleSlotPrefabField = typeof(LevelManager).GetField("singleSlotContainerPrefab", bindingFlags);

            itemPrefabField?.SetValue(lm, itemPrefab);
            containerPrefabField?.SetValue(lm, containerPrefab);
            singleSlotPrefabField?.SetValue(lm, singleSlotContainerPrefab);

            Debug.Log($"[GameSceneSetup] Prefabs assigned - Item: {itemPrefab != null}, Container: {containerPrefab != null}, SingleSlot: {singleSlotContainerPrefab != null}");
        }

        private void LoadLevel()
        {
            // Wait a frame for managers to initialize
            StartCoroutine(LoadLevelDelayed());
        }

        private System.Collections.IEnumerator LoadLevelDelayed()
        {
            yield return null; // Wait one frame

            // Don't auto-load a level - let the Level Select screen handle it
            // The UIManager shows the level select screen by default
            GameManager.Instance?.SetState(GameState.LevelSelection);
            Debug.Log("[GameSceneSetup] Ready - showing level select screen");
        }

        // UI is now handled by UIManager - OnGUI removed
    }
}
