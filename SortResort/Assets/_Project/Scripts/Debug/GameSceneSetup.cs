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
            mainCamera.orthographicSize = 6f;
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = new Color(0.4f, 0.7f, 0.9f);

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

            if (LevelManager.Instance != null)
            {
                GameManager.Instance?.SetState(GameState.Playing);
                LevelManager.Instance.LoadLevel(worldId, levelNumber);
                Debug.Log($"[GameSceneSetup] Loaded level: {worldId} #{levelNumber}");
            }
            else
            {
                Debug.LogError("[GameSceneSetup] LevelManager.Instance is null!");
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 250));

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label($"<size=16><b>SORT RESORT - {worldId} Level {levelNumber}</b></size>",
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);

            if (GameManager.Instance != null)
            {
                GUILayout.Label($"Moves: {GameManager.Instance.CurrentMoveCount}");
                GUILayout.Label($"Matches: {GameManager.Instance.CurrentMatchCount}");
            }

            if (LevelManager.Instance != null)
            {
                GUILayout.Label($"Items Remaining: {LevelManager.Instance.ItemsRemaining}");
            }

            GUILayout.Space(10);
            GUILayout.Label("Drag items to match 3 of the same type!");

            GUILayout.Space(10);
            if (GUILayout.Button("Restart Level"))
            {
                LevelManager.Instance?.RestartLevel();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
