using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Test setup script with REAL sprites - creates everything needed to test core gameplay.
    /// Attach this to an empty GameObject in a new scene and press Play.
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float slotWidth = 1.28f;
        [SerializeField] private float slotHeight = 2.56f;

        [Header("Layer Configuration")]
        [SerializeField] private int itemLayer = 6;  // "Items" layer
        [SerializeField] private int slotLayer = 7;  // "Slots" layer

        // Sprite cache
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private Sprite containerSprite;

        // Runtime references
        private Camera mainCamera;
        private GameObject managersRoot;

        private void Start()
        {
            Debug.Log("=== Test Scene Setup Starting (Real Sprites) ===");

            LoadSprites();
            SetupCamera();
            SetupManagers();
            LoadTestLevel();

            Debug.Log("=== Test Scene Setup Complete ===");
            Debug.Log("Drag items between containers! Match 3 of the same to clear them!");
        }

        private void LoadSprites()
        {
            // Load container sprite
            containerSprite = Resources.Load<Sprite>("Sprites/Containers/base_shelf");
            if (containerSprite == null)
                Debug.LogWarning("[Setup] Could not load container sprite");

            // Load item sprites
            string[] itemIds = { "coconut", "pineapple", "beachball_mixed", "sunhat" };
            foreach (var id in itemIds)
            {
                var sprite = Resources.Load<Sprite>($"Sprites/Items/Resort/{id}");
                if (sprite != null)
                {
                    spriteCache[id] = sprite;
                    Debug.Log($"[Setup] Loaded sprite: {id}");
                }
                else
                {
                    Debug.LogWarning($"[Setup] Could not load sprite: {id}");
                }
            }
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
            mainCamera.backgroundColor = new Color(0.4f, 0.7f, 0.9f); // Nice sky blue

            // Add URP camera data if needed
            var urpCameraData = mainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpCameraData == null)
            {
                urpCameraData = mainCamera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            }

            Debug.Log("[Setup] Camera configured");
        }

        private void SetupManagers()
        {
            managersRoot = new GameObject("--- MANAGERS ---");

            // GameManager
            var gmGO = new GameObject("GameManager");
            gmGO.transform.SetParent(managersRoot.transform);
            gmGO.AddComponent<GameManager>();

            // DragDropManager
            var ddGO = new GameObject("DragDropManager");
            ddGO.transform.SetParent(managersRoot.transform);
            var ddm = ddGO.AddComponent<DragDropManager>();

            // Configure DragDropManager
            var itemLayerField = typeof(DragDropManager).GetField("itemLayerMask",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var slotLayerField = typeof(DragDropManager).GetField("slotLayerMask",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraField = typeof(DragDropManager).GetField("gameCamera",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var debugField = typeof(DragDropManager).GetField("debugMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            itemLayerField?.SetValue(ddm, (LayerMask)(1 << itemLayer));
            slotLayerField?.SetValue(ddm, (LayerMask)(1 << slotLayer));
            cameraField?.SetValue(ddm, mainCamera);
            debugField?.SetValue(ddm, false); // Disable debug spam

            Debug.Log("[Setup] Managers created");
        }

        private void LoadTestLevel()
        {
            Debug.Log("[Setup] Creating test level with real sprites...");

            // Create two containers
            // Container 0: coconut, pineapple, coconut (middle slot has pineapple)
            // Container 1: coconut, null, null (one coconut to move)
            // Move coconut from container 1 to middle of container 0 to get triple match!
            CreateRealContainer(new Vector3(-3f, 0, 0), 0, new string[] { "coconut", "pineapple", "coconut" });
            CreateRealContainer(new Vector3(3f, 0, 0), 1, new string[] { "coconut", null, null });

            // Set game state to playing
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            Debug.Log("[Setup] Test level created!");
            Debug.Log("[Setup] TIP: Move the pineapple to an empty slot, then move a coconut to match 3!");
        }

        private void CreateRealContainer(Vector3 position, int containerId, string[] itemIds)
        {
            var containerGO = new GameObject($"Container_{containerId}");
            containerGO.transform.position = position;
            containerGO.transform.localScale = Vector3.one; // Keep container at scale 1

            // Add ItemContainer component
            var itemContainer = containerGO.AddComponent<ItemContainer>();

            // Add container sprite as a CHILD object (so its scale doesn't affect slots/items)
            if (containerSprite != null)
            {
                var shelfGO = new GameObject("ShelfSprite");
                shelfGO.transform.SetParent(containerGO.transform);
                shelfGO.transform.localPosition = Vector3.zero;

                var sr = shelfGO.AddComponent<SpriteRenderer>();
                sr.sprite = containerSprite;
                sr.sortingOrder = -1;

                // Scale only the sprite object, not the container
                float targetWidth = slotWidth * 3.5f;
                float spriteWidth = containerSprite.bounds.size.x;
                float scale = targetWidth / spriteWidth;
                shelfGO.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                // Fallback to colored rectangle
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(containerGO.transform);
                bgGO.transform.localPosition = Vector3.zero;
                var bgSR = bgGO.AddComponent<SpriteRenderer>();
                bgSR.sprite = CreateRectSprite(slotWidth * 3.2f, slotHeight * 1.1f);
                bgSR.color = new Color(0.55f, 0.35f, 0.2f, 1f);
                bgSR.sortingOrder = -1;
            }

            // Create slots parent
            var slotsParentGO = new GameObject("Slots");
            slotsParentGO.transform.SetParent(containerGO.transform);
            slotsParentGO.transform.localPosition = Vector3.zero;
            slotsParentGO.transform.localScale = Vector3.one; // Ensure no inherited scale issues

            // Create 3 slots
            var slots = new List<Slot>();
            for (int i = 0; i < 3; i++)
            {
                var slot = CreateSlot(slotsParentGO.transform, i, itemContainer);
                slots.Add(slot);
            }

            // Initialize the ItemContainer (pass slotsParent transform)
            InitializeContainer(itemContainer, slots, containerId, slotsParentGO.transform);

            // Create items
            for (int i = 0; i < itemIds.Length && i < 3; i++)
            {
                if (!string.IsNullOrEmpty(itemIds[i]))
                {
                    CreateRealItem(itemContainer, i, itemIds[i]);
                }
            }
        }

        private void InitializeContainer(ItemContainer container, List<Slot> slots, int containerId, Transform slotsParentTransform)
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            var slotsField = typeof(ItemContainer).GetField("slots", bindingFlags);
            var slotComponentsField = typeof(ItemContainer).GetField("slotComponents", bindingFlags);
            var slotCountField = typeof(ItemContainer).GetField("slotCount", bindingFlags);
            var containerIdField = typeof(ItemContainer).GetField("containerId", bindingFlags);
            var slotsParentField = typeof(ItemContainer).GetField("slotsParent", bindingFlags);
            var slotSpacingField = typeof(ItemContainer).GetField("slotSpacing", bindingFlags);
            var slotSizeField = typeof(ItemContainer).GetField("slotSize", bindingFlags);

            var slotData = new List<List<Item>>();
            for (int i = 0; i < 3; i++)
            {
                var row = new List<Item> { null, null, null, null };
                slotData.Add(row);
            }

            slotsField?.SetValue(container, slotData);
            slotComponentsField?.SetValue(container, slots);
            slotCountField?.SetValue(container, 3);
            containerIdField?.SetValue(container, $"container_{containerId}");
            slotsParentField?.SetValue(container, slotsParentTransform);

            // Set slot dimensions to match our test setup (in pixels, will be /100 for units)
            slotSpacingField?.SetValue(container, slotWidth * 100f);  // 128 pixels
            slotSizeField?.SetValue(container, new Vector2(slotWidth * 100f, slotHeight * 100f));  // 128x256 pixels

            Debug.Log($"[Setup] Container {containerId} initialized with {slots.Count} slots, slotsParent set");
        }

        private Slot CreateSlot(Transform parent, int slotIndex, ItemContainer container)
        {
            var slotGO = new GameObject($"Slot_{slotIndex}");
            slotGO.transform.SetParent(parent);

            // Position slots using same formula as ItemContainer.GetSlotLocalPosition
            // x = (slotIndex - (slotCount - 1) / 2f) * slotSpacing / 100f
            // For 3 slots: slot0 = -1.28, slot1 = 0, slot2 = 1.28
            float x = (slotIndex - 1f) * slotWidth;  // slotWidth is already in world units (1.28)
            slotGO.transform.localPosition = new Vector3(x, 0, 0);
            slotGO.layer = slotLayer;

            // Subtle slot indicator
            var sr = slotGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(slotWidth * 0.9f, slotHeight * 0.9f);
            sr.color = new Color(1f, 1f, 1f, 0.1f); // Very subtle
            sr.sortingOrder = 0;

            // Collider
            var col = slotGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(slotWidth, slotHeight);
            col.isTrigger = true;

            // Slot component
            var slot = slotGO.AddComponent<Slot>();

            // Initialize via reflection
            var indexField = typeof(Slot).GetField("slotIndex",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rowField = typeof(Slot).GetField("row",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var frontRowField = typeof(Slot).GetField("isFrontRow",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var interactiveField = typeof(Slot).GetField("isInteractive",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parentContainerField = typeof(Slot).GetField("parentContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            indexField?.SetValue(slot, slotIndex);
            rowField?.SetValue(slot, 0);
            frontRowField?.SetValue(slot, true);
            interactiveField?.SetValue(slot, true);
            parentContainerField?.SetValue(slot, container);

            return slot;
        }

        private void CreateRealItem(ItemContainer container, int slotIndex, string itemId)
        {
            var itemGO = new GameObject($"Item_{itemId}_{slotIndex}");
            itemGO.layer = itemLayer;

            // Get sprite
            Sprite sprite = null;
            spriteCache.TryGetValue(itemId, out sprite);

            // Add sprite renderer FIRST (before Item component)
            var sr = itemGO.AddComponent<SpriteRenderer>();

            // Add collider BEFORE Item component
            var col = itemGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Track the final item height for positioning
            float finalItemHeight = 0f;

            if (sprite != null)
            {
                sr.sprite = sprite;

                // Scale items to fit within the slot with padding
                // Slot dimensions: slotWidth=1.28, slotHeight=2.56 (in world units)
                // Target: item should fill ~80% of slot height OR ~90% of slot width (whichever is limiting)
                float targetHeight = slotHeight * 0.8f;  // 2.56 * 0.8 = 2.048 world units
                float targetWidth = slotWidth * 0.9f;   // 1.28 * 0.9 = 1.152 world units

                // Calculate scale needed to fit within both constraints
                float scaleByHeight = targetHeight / sprite.bounds.size.y;
                float scaleByWidth = targetWidth / sprite.bounds.size.x;

                // Use the smaller scale to ensure item fits within slot
                float scale = Mathf.Min(scaleByHeight, scaleByWidth);

                itemGO.transform.localScale = new Vector3(scale, scale, 1f);
                col.size = sprite.bounds.size * 0.9f;

                // Calculate final item height in world units
                finalItemHeight = sprite.bounds.size.y * scale;

                Debug.Log($"[Setup] Item {itemId}: sprite bounds={sprite.bounds.size}, scale={scale:F2}, finalHeight={finalItemHeight:F2}");
            }
            else
            {
                // Fallback colored rectangle
                float fallbackHeight = slotHeight * 0.4f;
                sr.sprite = CreateRectSprite(slotWidth * 0.7f, fallbackHeight);
                sr.color = GetColorForItem(itemId);
                col.size = new Vector2(slotWidth * 0.7f, fallbackHeight);
                finalItemHeight = fallbackHeight;
            }
            sr.sortingOrder = 10;

            // NOW add Item component - Awake() will capture the correct scale
            var item = itemGO.AddComponent<Item>();
            item.Configure(itemId, sprite);

            // Explicitly recapture scale to ensure it's correct
            item.RecaptureOriginalScale();

            // Set references
            var slotComponentsField = typeof(ItemContainer).GetField("slotComponents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var slotComponents = slotComponentsField?.GetValue(container) as List<Slot>;
            if (slotComponents != null && slotIndex < slotComponents.Count)
            {
                item.SetSlot(slotComponents[slotIndex], container);
            }

            // Place in container data
            var slotsField = typeof(ItemContainer).GetField("slots",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var slots = slotsField?.GetValue(container) as List<List<Item>>;
            if (slots != null && slotIndex < slots.Count)
            {
                slots[slotIndex][0] = item;
            }

            // Position using ItemContainer's method for consistency with drag-drop
            Vector3 worldPos = container.GetItemWorldPositionBottomAligned(slotIndex, 0, finalItemHeight);
            itemGO.transform.position = worldPos;

            // Debug: also check what GetItemHeight would return
            var srCheck = itemGO.GetComponent<SpriteRenderer>();
            float srBoundsHeight = srCheck != null ? srCheck.bounds.size.y : 0f;
            Debug.Log($"[Setup] Placed {itemId} at {worldPos}, finalItemHeight={finalItemHeight:F4}, sr.bounds.height={srBoundsHeight:F4}, diff={srBoundsHeight - finalItemHeight:F4}");
        }

        private Color GetColorForItem(string itemId)
        {
            switch (itemId)
            {
                case "coconut": return new Color(0.6f, 0.4f, 0.2f);
                case "pineapple": return new Color(1f, 0.85f, 0.2f);
                case "beachball_mixed": return new Color(1f, 0.3f, 0.3f);
                case "sunhat": return new Color(0.9f, 0.85f, 0.6f);
                default: return Color.white;
            }
        }

        private Sprite CreateRectSprite(float width, float height)
        {
            int w = Mathf.Max(4, (int)(width * 10));
            int h = Mathf.Max(4, (int)(height * 10));

            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));

            GUI.color = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

            GUILayout.BeginVertical(style);
            GUILayout.Label("<size=16><b>SORT RESORT - Test Scene</b></size>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);
            GUILayout.Label("1. Move pineapple to an empty slot in right container");
            GUILayout.Label("2. Move coconut to fill the gap");
            GUILayout.Label("3. When 3 coconuts match, they disappear!");
            GUILayout.Space(5);

            if (GameManager.Instance != null)
            {
                GUILayout.Label($"Moves: {GameManager.Instance.CurrentMoveCount}");
                GUILayout.Label($"Matches: {GameManager.Instance.CurrentMatchCount}");
            }

            GUILayout.Space(5);
            GUILayout.Label("<size=10>Check Console (Window > General > Console) for debug logs</size>", new GUIStyle(GUI.skin.label) { richText = true });

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
