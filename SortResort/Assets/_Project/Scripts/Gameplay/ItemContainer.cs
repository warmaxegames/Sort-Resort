using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// ItemContainer manages a container that holds items in slots.
    /// Handles item matching, row advancement, and lock mechanics.
    /// </summary>
    public class ItemContainer : MonoBehaviour
    {
        public enum ContainerType
        {
            Standard,
            SingleSlot
        }

        [Header("Container Configuration")]
        [SerializeField] private string containerId;
        [SerializeField] private ContainerType containerType = ContainerType.Standard;
        [SerializeField] private int slotCount = 3;
        [SerializeField] private int maxRowsPerSlot = 4;
        [SerializeField] private Vector2 slotSize = new Vector2(90f, 180f);

        [Header("Lock Settings")]
        [SerializeField] private bool isLocked;
        [SerializeField] private int unlockMatchesRequired;
        private int currentUnlockProgress;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer containerSprite;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TMPro.TextMeshPro lockCountText;

        [Header("Slot Configuration")]
        [SerializeField] private float slotSpacing = 90f;
        [SerializeField] private float rowDepthOffset = 28f; // Vertical offset for back row items (in pixels)

        // Slot data structure: slots[slotIndex][row] = Item
        private List<List<Item>> slots = new List<List<Item>>();
        private List<Slot> slotComponents = new List<Slot>();

        // Properties
        public string ContainerId => containerId;
        public ContainerType Type => containerType;
        public bool IsLocked => isLocked;
        public int SlotCount => slotCount;
        public Vector2 SlotSizeUnits => slotSize / 100f; // Slot size in world units

        // Events
        public event Action<ItemContainer, string, int> OnItemsMatched; // container, itemId, count
        public event Action<ItemContainer> OnRowAdvanced;
        public event Action<ItemContainer> OnContainerUnlocked;
        public event Action<ItemContainer> OnContainerEmpty;

        private void Awake()
        {
            if (slotsParent == null)
            {
                slotsParent = transform;
            }
        }

        /// <summary>
        /// Initialize the container with configuration
        /// </summary>
        public void Initialize(ContainerDefinition definition)
        {
            // Force container sizing (override any prefab serialized values)
            // These values allow 3 containers across screen width and ~6 vertically
            // Container width = 3 slots × 83 = 249px, height = 166px (~7% smaller than original 90x180)
            slotSize = new Vector2(83f, 166f);
            slotSpacing = 83f;
            rowDepthOffset = 4f;

            containerId = definition.id;
            containerType = definition.container_type == "single_slot"
                ? ContainerType.SingleSlot
                : ContainerType.Standard;
            slotCount = definition.slot_count > 0 ? definition.slot_count : 3;
            maxRowsPerSlot = definition.max_rows_per_slot > 0 ? definition.max_rows_per_slot : 4;
            isLocked = definition.is_locked;
            unlockMatchesRequired = definition.unlock_matches_required;
            currentUnlockProgress = 0;

            // Set position - convert from Godot pixel coords to Unity world units
            // Godot uses top-left origin with Y increasing downward
            // Portrait phone layout: center around (540, 600) for typical level layouts
            Vector3 godotPos = definition.position.ToVector3();
            float unityX = (godotPos.x - 540f) / 100f;  // Center X (portrait phone center ~540)
            float unityY = (600f - godotPos.y) / 100f;  // Flip Y axis, center around 600 for portrait
            transform.position = new Vector3(unityX, unityY, 0f);
            Debug.Log($"[ItemContainer] {definition.id} position: Godot({godotPos.x}, {godotPos.y}) -> Unity({unityX:F2}, {unityY:F2})");

            // Initialize slot structure
            InitializeSlots();

            // Load container sprite
            LoadContainerSprite(definition.container_image);

            // Create and setup lock overlay if container is locked
            if (isLocked)
            {
                CreateLockOverlay(definition.lock_overlay_image);
            }
            UpdateLockVisuals();

            // Setup movement if needed
            if (definition.is_moving || definition.is_falling)
            {
                var movement = gameObject.AddComponent<ContainerMovement>();
                movement.Initialize(definition);
            }

            gameObject.name = $"Container_{containerId}";
        }

        /// <summary>
        /// Load and set the container sprite
        /// </summary>
        private void LoadContainerSprite(string imageName)
        {
            if (containerSprite == null)
            {
                // Try to find the SpriteRenderer in children (from prefab)
                containerSprite = GetComponentInChildren<SpriteRenderer>();
            }

            if (containerSprite == null)
            {
                Debug.LogWarning($"[ItemContainer] No SpriteRenderer found for container {containerId}");
                return;
            }

            // Try to load the specified sprite, with fallbacks
            string[] paths = {
                $"Sprites/Containers/{imageName}",
                $"Sprites/Containers/island_container",
                $"Sprites/Containers/supermarket_container"
            };

            Sprite sprite = null;
            foreach (var path in paths)
            {
                sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    if (path != $"Sprites/Containers/{imageName}")
                    {
                        Debug.LogWarning($"[ItemContainer] Using fallback sprite '{path}' instead of '{imageName}'");
                    }
                    else
                    {
                        Debug.Log($"[ItemContainer] Loaded container sprite: {imageName}");
                    }
                    break;
                }
            }

            if (sprite != null)
            {
                containerSprite.sprite = sprite;

                // Scale sprite to fit container width (3 slots * slotSpacing)
                float targetWidth = slotCount * slotSpacing / 100f * 1.2f;  // Slightly wider than slots
                float spriteWidth = sprite.bounds.size.x;
                float scale = targetWidth / spriteWidth;
                containerSprite.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                Debug.LogWarning($"[ItemContainer] Failed to load any container sprite for {containerId}");
            }
        }

        /// <summary>
        /// Initialize the slot data structure
        /// </summary>
        private void InitializeSlots()
        {
            slots.Clear();
            slotComponents.Clear();

            for (int i = 0; i < slotCount; i++)
            {
                // Initialize slot data
                var slotData = new List<Item>();
                for (int r = 0; r < maxRowsPerSlot; r++)
                {
                    slotData.Add(null);
                }
                slots.Add(slotData);

                // Create slot component for front row
                CreateSlotComponent(i);
            }
        }

        /// <summary>
        /// Create a slot component for collision detection
        /// </summary>
        private void CreateSlotComponent(int slotIndex)
        {
            var slotGO = new GameObject($"Slot_{slotIndex}");
            slotGO.transform.SetParent(slotsParent);
            slotGO.transform.localPosition = GetSlotLocalPosition(slotIndex, 0);
            slotGO.layer = 7; // Slots layer for DragDropManager detection

            var slot = slotGO.AddComponent<Slot>();
            slot.Initialize(slotIndex, 0, this);

            var collider = slotGO.AddComponent<BoxCollider2D>();
            collider.size = slotSize / 100f; // Convert pixels to units
            collider.isTrigger = true;

            slotComponents.Add(slot);
        }

        /// <summary>
        /// Get the local position for a slot
        /// </summary>
        private Vector3 GetSlotLocalPosition(int slotIndex, int row)
        {
            float x = (slotIndex - (slotCount - 1) / 2f) * slotSpacing;
            float y = row * rowDepthOffset;
            return new Vector3(x, y, 0) / 100f; // Convert pixels to units
        }

        /// <summary>
        /// Get the world position for an item in a slot (centered)
        /// </summary>
        public Vector3 GetItemWorldPosition(int slotIndex, int row)
        {
            Vector3 localPos = GetSlotLocalPosition(slotIndex, row);
            return slotsParent.TransformPoint(localPos);
        }

        /// <summary>
        /// Get the world position for an item sitting at the bottom of a slot
        /// </summary>
        public Vector3 GetItemWorldPositionBottomAligned(int slotIndex, int row, float itemHeight)
        {
            Vector3 localPos = GetSlotLocalPosition(slotIndex, row);

            // Offset to place item at bottom of slot
            // slotSize.y is the slot height in pixels, convert to units
            float slotHeightUnits = slotSize.y / 100f;
            float slotBottom = -slotHeightUnits / 2f;
            float yOffset = slotBottom + (itemHeight / 2f);

            Debug.Log($"[ItemContainer] GetItemWorldPositionBottomAligned: slotSize.y={slotSize.y}, slotHeightUnits={slotHeightUnits:F4}, slotBottom={slotBottom:F4}, itemHeight={itemHeight:F4}, yOffset={yOffset:F4}");

            localPos.y += yOffset;

            return slotsParent.TransformPoint(localPos);
        }

        #region Item Management

        /// <summary>
        /// Place an item in a specific slot (front row)
        /// </summary>
        public bool PlaceItemInSlot(Item item, int slotIndex, bool forcePlace = false)
        {
            if (slotIndex < 0 || slotIndex >= slotCount)
                return false;

            // Check if slot is available
            if (!forcePlace && !IsSlotEmpty(slotIndex, 0))
            {
                Debug.Log($"[ItemContainer] Cannot place {item.ItemId} in slot {slotIndex} - slot not empty");
                return false;
            }

            // Check if container is locked
            if (!forcePlace && isLocked)
                return false;

            // Place the item in data structure
            slots[slotIndex][0] = item;

            // Update item references
            item.SetSlot(slotComponents[slotIndex], this);

            // Parent to this container (important for moving containers)
            item.transform.SetParent(slotsParent);

            // Position item using LOCAL position so it moves with container
            float itemHeight = GetItemHeight(item);
            Vector3 localPos = GetItemLocalPositionBottomAligned(slotIndex, 0, itemHeight);
            Debug.Log($"[ItemContainer] PlaceItemInSlot {item.ItemId}: itemHeight={itemHeight:F4}, localPos={localPos}");
            item.transform.localPosition = localPos;
            item.SetRowDepth(0, maxRowsPerSlot);

            Debug.Log($"[ItemContainer] Placed item {item.ItemId} in slot {slotIndex}");

            // Check for matches
            CheckForMatches();

            return true;
        }

        /// <summary>
        /// Get the rendered height of an item in world units
        /// </summary>
        public float GetItemHeight(Item item)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // Get bounds in world space (accounts for scale)
                float height = sr.bounds.size.y;
                Debug.Log($"[ItemContainer] GetItemHeight for {item.ItemId}: sr.bounds.size.y={height:F4}, sprite.bounds.size.y={sr.sprite.bounds.size.y:F4}, scale={item.transform.localScale.y:F4}");
                return height;
            }
            return 1f; // Default fallback
        }

        /// <summary>
        /// Place item in slot (data + parenting only, no positioning) - used for cancel drag animation
        /// </summary>
        public void PlaceItemInSlotNoPosition(Item item, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCount) return;

            // Place the item in data structure
            slots[slotIndex][0] = item;

            // Update item references
            item.SetSlot(slotComponents[slotIndex], this);

            // Parent to slots container (so item moves with container)
            item.transform.SetParent(slotsParent);
            item.SetRowDepth(0, maxRowsPerSlot);
        }

        /// <summary>
        /// Get the LOCAL position for an item in a slot (relative to slotsParent)
        /// </summary>
        public Vector3 GetItemLocalPositionBottomAligned(int slotIndex, int row, float itemHeight)
        {
            Vector3 localPos = GetSlotLocalPosition(slotIndex, row);

            // Offset to place item at bottom of slot
            float slotHeightUnits = slotSize.y / 100f;
            float slotBottom = -slotHeightUnits / 2f;
            float yOffset = slotBottom + (itemHeight / 2f);

            localPos.y += yOffset;

            return localPos;
        }

        /// <summary>
        /// Place an item in a specific row (used for level loading)
        /// </summary>
        public bool PlaceItemInRow(Item item, int slotIndex, int row)
        {
            if (slotIndex < 0 || slotIndex >= slotCount)
                return false;
            if (row < 0 || row >= maxRowsPerSlot)
                return false;

            slots[slotIndex][row] = item;

            // Update item references
            if (row == 0 && slotIndex < slotComponents.Count)
            {
                item.SetSlot(slotComponents[slotIndex], this);
            }
            else
            {
                item.SetSlot(null, this);
            }

            // Parent first, then set LOCAL position so item moves with container
            item.transform.SetParent(slotsParent);
            float itemHeight = GetItemHeight(item);
            Vector3 localPos = GetItemLocalPositionBottomAligned(slotIndex, row, itemHeight);
            item.transform.localPosition = localPos;
            item.SetRowDepth(row, maxRowsPerSlot);

            Debug.Log($"[ItemContainer] PlaceItemInRow {item.ItemId}: slot={slotIndex}, row={row}, localPos={localPos}");

            return true;
        }

        /// <summary>
        /// Remove an item from its slot
        /// </summary>
        public void RemoveItemFromSlot(Item item, bool triggerRowAdvance = true)
        {
            Debug.Log($"[ItemContainer] RemoveItemFromSlot called for {item?.ItemId ?? "null"} in container {containerId}");

            if (item == null)
            {
                Debug.LogWarning("[ItemContainer] RemoveItemFromSlot called with null item!");
                return;
            }

            bool found = false;
            int removedFromSlot = -1;
            for (int s = 0; s < slots.Count; s++)
            {
                for (int r = 0; r < slots[s].Count; r++)
                {
                    if (slots[s][r] == item)
                    {
                        slots[s][r] = null;
                        item.ClearSlot();
                        Debug.Log($"[ItemContainer] Successfully removed item {item.ItemId} from slot {s}, row {r}");
                        found = true;
                        removedFromSlot = s;
                        break;
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                Debug.LogWarning($"[ItemContainer] Item {item.ItemId} NOT FOUND in any slot of container {containerId}!");
                // Debug: print current slot contents
                for (int s = 0; s < slots.Count; s++)
                {
                    var slotItem = slots[s][0];
                    Debug.Log($"[ItemContainer] Slot {s}: {(slotItem != null ? slotItem.ItemId : "empty")} (ref: {(slotItem != null ? slotItem.GetInstanceID().ToString() : "null")})");
                }
                Debug.Log($"[ItemContainer] Looking for item ref: {item.GetInstanceID()}");
            }

            // Check if we should trigger row advancement (only when ALL front slots are empty)
            if (found && triggerRowAdvance)
            {
                // Delay slightly to allow drag to complete
                LeanTween.delayedCall(0.1f, () =>
                {
                    CheckAndAdvanceAllRows();

                    // Check if container is now empty (for despawn-on-match containers)
                    if (IsEmpty())
                    {
                        Debug.Log($"[ItemContainer] Container {containerId} is now empty, invoking OnContainerEmpty");
                        OnContainerEmpty?.Invoke(this);
                    }
                });
            }
        }

        /// <summary>
        /// Check if a slot is empty
        /// </summary>
        public bool IsSlotEmpty(int slotIndex, int row)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return true;
            if (row < 0 || row >= slots[slotIndex].Count)
                return true;

            return slots[slotIndex][row] == null;
        }

        /// <summary>
        /// Get the item in a specific slot
        /// </summary>
        public Item GetItemInSlot(int slotIndex, int row)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return null;
            if (row < 0 || row >= slots[slotIndex].Count)
                return null;

            return slots[slotIndex][row];
        }

        /// <summary>
        /// Check if container is empty
        /// </summary>
        public bool IsEmpty()
        {
            foreach (var slot in slots)
            {
                foreach (var item in slot)
                {
                    if (item != null)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get total item count
        /// </summary>
        public int GetTotalItemCount()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                foreach (var item in slot)
                {
                    if (item != null)
                        count++;
                }
            }
            return count;
        }

        #endregion

        #region Match Detection

        /// <summary>
        /// Check for matches in the front row
        /// </summary>
        public void CheckForMatches()
        {
            if (containerType == ContainerType.SingleSlot)
                return;

            if (slotCount < 3)
                return;

            // Get front row items
            var frontItems = new List<Item>();
            for (int i = 0; i < slotCount; i++)
            {
                var item = GetItemInSlot(i, 0);
                frontItems.Add(item);
            }

            // Check if all front slots have items
            bool allFilled = true;
            foreach (var item in frontItems)
            {
                if (item == null)
                {
                    allFilled = false;
                    break;
                }
            }

            if (!allFilled)
                return;

            // Check if all items match
            string firstId = frontItems[0].ItemId;
            bool allMatch = true;
            foreach (var item in frontItems)
            {
                if (item.ItemId != firstId)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                ProcessMatch(frontItems, firstId);
            }
        }

        /// <summary>
        /// Process a successful match
        /// </summary>
        private void ProcessMatch(List<Item> matchedItems, string itemId)
        {
            Debug.Log($"[ItemContainer] Match found! {matchedItems.Count}x {itemId}");

            // Debug: log each item in the list
            for (int i = 0; i < matchedItems.Count; i++)
            {
                var item = matchedItems[i];
                Debug.Log($"[ItemContainer] ProcessMatch item {i}: {(item != null ? item.ItemId : "NULL")}, state: {(item != null ? item.CurrentState.ToString() : "N/A")}");
            }

            // Play match sound
            AudioManager.Instance?.PlayMatchSound();

            // Mark items as matched and remove from slots
            int matchedCount = 0;
            foreach (var item in matchedItems)
            {
                if (item != null)
                {
                    Debug.Log($"[ItemContainer] Processing item {item.ItemId}, current state: {item.CurrentState}");

                    // Clear from slot data (don't trigger row advance - we'll do it after animation)
                    RemoveItemFromSlot(item, triggerRowAdvance: false);

                    // Play match animation
                    item.MarkAsMatched();
                    matchedCount++;
                }
            }

            // Play match sound
            AudioManager.Instance?.PlayMatchSound();

            // Fire event and increment match count
            // Note: LevelManager listens to OnItemsMatched and notifies ALL locked containers
            OnItemsMatched?.Invoke(this, itemId, matchedCount);

            // Advance rows after a short delay
            LeanTween.delayedCall(0.3f, () =>
            {
                AdvanceRows();

                // Check if container is now empty
                if (IsEmpty())
                {
                    OnContainerEmpty?.Invoke(this);
                }
            });
        }

        #endregion

        #region Row Advancement

        /// <summary>
        /// Check if ALL front row slots are empty, and if so, advance all items forward
        /// </summary>
        public void CheckAndAdvanceAllRows()
        {
            // Check if ALL front row slots are empty
            bool allFrontEmpty = true;
            bool anyBackItems = false;

            for (int s = 0; s < slots.Count; s++)
            {
                if (slots[s][0] != null)
                {
                    allFrontEmpty = false;
                    break;
                }

                // Check if there are any back row items
                for (int r = 1; r < slots[s].Count; r++)
                {
                    if (slots[s][r] != null)
                    {
                        anyBackItems = true;
                        break;
                    }
                }
            }

            // Only advance if ALL front slots are empty AND there are back items to advance
            if (allFrontEmpty && anyBackItems)
            {
                Debug.Log($"[ItemContainer] All front slots empty - advancing all rows");
                AdvanceAllRowsForward();
            }
        }

        /// <summary>
        /// Advance all items from row 1 to row 0 across all slots
        /// </summary>
        private void AdvanceAllRowsForward()
        {
            bool anyAdvanced = false;

            for (int s = 0; s < slots.Count; s++)
            {
                // Find first non-null row in this slot
                for (int r = 1; r < slots[s].Count; r++)
                {
                    if (slots[s][r] != null)
                    {
                        // Move all items forward by r positions
                        for (int moveRow = r; moveRow < slots[s].Count; moveRow++)
                        {
                            int targetRow = moveRow - r;
                            if (targetRow < slots[s].Count && moveRow < slots[s].Count)
                            {
                                slots[s][targetRow] = slots[s][moveRow];
                                if (moveRow != targetRow)
                                    slots[s][moveRow] = null;
                            }
                        }
                        anyAdvanced = true;
                        break;
                    }
                }
            }

            if (anyAdvanced)
            {
                // Update all item positions with animation
                UpdateAllItemPositions();

                OnRowAdvanced?.Invoke(this);

                // Check for new matches after animation
                LeanTween.delayedCall(0.3f, CheckForMatches);
            }
        }

        /// <summary>
        /// Update positions and visuals for items in a specific slot
        /// </summary>
        private void UpdateSlotItemPositions(int slotIndex)
        {
            for (int r = 0; r < slots[slotIndex].Count; r++)
            {
                var item = slots[slotIndex][r];
                if (item != null)
                {
                    // Animate to new LOCAL position (so it moves correctly with moving containers)
                    float itemHeight = GetItemHeight(item);
                    Vector3 targetLocalPos = GetItemLocalPositionBottomAligned(slotIndex, r, itemHeight);

                    LeanTween.moveLocal(item.gameObject, targetLocalPos, 0.25f)
                        .setEase(LeanTweenType.easeOutQuad);

                    // Update row depth visuals
                    item.SetRowDepth(r, maxRowsPerSlot);

                    // Update slot reference for front row
                    if (r == 0 && slotIndex < slotComponents.Count)
                    {
                        item.SetSlot(slotComponents[slotIndex], this);
                    }
                    else
                    {
                        item.SetSlot(null, this);
                    }
                }
            }
        }

        /// <summary>
        /// Advance items from back rows to front (all slots) - only if ALL front slots are empty
        /// </summary>
        public void AdvanceRows()
        {
            // Use the same logic as CheckAndAdvanceAllRows
            CheckAndAdvanceAllRows();
        }

        /// <summary>
        /// Update positions and visuals for all items
        /// </summary>
        private void UpdateAllItemPositions()
        {
            for (int s = 0; s < slots.Count; s++)
            {
                for (int r = 0; r < slots[s].Count; r++)
                {
                    var item = slots[s][r];
                    if (item != null)
                    {
                        // Animate to new LOCAL position (so it moves correctly with moving containers)
                        float itemHeight = GetItemHeight(item);
                        Vector3 targetLocalPos = GetItemLocalPositionBottomAligned(s, r, itemHeight);
                        LeanTween.moveLocal(item.gameObject, targetLocalPos, 0.2f)
                            .setEase(LeanTweenType.easeOutQuad);

                        // Update row depth visuals
                        item.SetRowDepth(r, maxRowsPerSlot);

                        // Update slot reference for front row
                        if (r == 0 && s < slotComponents.Count)
                        {
                            item.SetSlot(slotComponents[s], this);
                        }
                        else
                        {
                            item.SetSlot(null, this);
                        }
                    }
                }
            }
        }

        #endregion

        #region Lock System

        /// <summary>
        /// Create the lock overlay GameObject with sprite and countdown
        /// </summary>
        private void CreateLockOverlay(string lockOverlayImage)
        {
            // Create lock overlay parent
            lockOverlay = new GameObject("LockOverlay");
            lockOverlay.transform.SetParent(transform);
            lockOverlay.transform.localPosition = Vector3.zero;

            // Create lock sprite
            var lockSpriteGO = new GameObject("LockSprite");
            lockSpriteGO.transform.SetParent(lockOverlay.transform);
            lockSpriteGO.transform.localPosition = Vector3.zero;

            var lockSpriteRenderer = lockSpriteGO.AddComponent<SpriteRenderer>();
            lockSpriteRenderer.sortingOrder = 50; // Above items

            // Try to load lock overlay sprite
            string[] spritePaths = {
                $"Sprites/Containers/{lockOverlayImage}",
                $"Sprites/UI/Overlays/{lockOverlayImage}",
                "Sprites/Containers/base_lockoverlay",
                "Sprites/UI/Overlays/base_lockoverlay"
            };

            Sprite lockSprite = null;
            foreach (var path in spritePaths)
            {
                lockSprite = Resources.Load<Sprite>(path);
                if (lockSprite != null)
                {
                    Debug.Log($"[ItemContainer] Loaded lock overlay from: {path}");
                    break;
                }
            }

            if (lockSprite != null)
            {
                lockSpriteRenderer.sprite = lockSprite;

                // Scale to fit container
                float containerWidth = slotCount * slotSpacing / 100f * 1.2f;
                float spriteWidth = lockSprite.bounds.size.x;
                float scale = containerWidth / spriteWidth;
                lockSpriteGO.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                // Fallback: create a semi-transparent dark overlay
                Debug.LogWarning($"[ItemContainer] Lock overlay sprite not found, using fallback");
                lockSpriteRenderer.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

                // Create a simple square sprite
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                lockSpriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

                // Scale to cover container
                float width = slotCount * slotSpacing / 100f * 1.3f;
                float height = slotSize.y / 100f * 1.2f;
                lockSpriteGO.transform.localScale = new Vector3(width, height, 1f);
            }

            // Create countdown text - positioned to center in lock overlay image
            var countdownGO = new GameObject("CountdownText");
            countdownGO.transform.SetParent(lockOverlay.transform);
            countdownGO.transform.localPosition = new Vector3(0, 0.55f, -0.1f);

            // Use TextMeshPro for the countdown
            lockCountText = countdownGO.AddComponent<TMPro.TextMeshPro>();
            lockCountText.text = unlockMatchesRequired.ToString();
            lockCountText.fontSize = 3;
            lockCountText.fontStyle = TMPro.FontStyles.Bold;
            lockCountText.alignment = TMPro.TextAlignmentOptions.Center;
            lockCountText.color = Color.black;
            lockCountText.sortingOrder = 51; // Above lock sprite

            // Add outline for readability
            lockCountText.outlineWidth = 0.15f;
            lockCountText.outlineColor = new Color(1f, 1f, 1f, 0.5f);

            Debug.Log($"[ItemContainer] Created lock overlay for {containerId}, matches required: {unlockMatchesRequired}");
        }

        /// <summary>
        /// Setup lock overlay visuals
        /// </summary>
        private void UpdateLockVisuals()
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(isLocked);
            }

            if (lockCountText != null && isLocked)
            {
                int remaining = unlockMatchesRequired - currentUnlockProgress;
                lockCountText.text = remaining.ToString();
            }

            // Update slot interactivity
            foreach (var slot in slotComponents)
            {
                slot.SetInteractive(!isLocked);
            }
        }

        /// <summary>
        /// Increment unlock progress
        /// </summary>
        public void IncrementUnlockProgress()
        {
            if (!isLocked) return;

            currentUnlockProgress++;

            int remaining = unlockMatchesRequired - currentUnlockProgress;
            if (lockCountText != null)
            {
                lockCountText.text = remaining.ToString();
            }

            if (currentUnlockProgress >= unlockMatchesRequired)
            {
                Unlock();
            }
        }

        /// <summary>
        /// Unlock the container
        /// </summary>
        public void Unlock()
        {
            if (!isLocked) return;

            isLocked = false;

            Debug.Log($"[ItemContainer] Container {containerId} unlocked!");

            // Play unlock sound
            AudioManager.Instance?.PlayUnlockSound();

            // Animate lock overlay with scale and fade
            if (lockOverlay != null)
            {
                // Scale up and fade out animation
                LeanTween.scale(lockOverlay, Vector3.one * 1.3f, 0.4f)
                    .setEase(LeanTweenType.easeOutBack);

                // Fade out all sprite renderers in lock overlay
                var spriteRenderers = lockOverlay.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in spriteRenderers)
                {
                    LeanTween.value(sr.gameObject, sr.color.a, 0f, 0.4f)
                        .setOnUpdate((float val) => {
                            if (sr != null)
                            {
                                var c = sr.color;
                                sr.color = new Color(c.r, c.g, c.b, val);
                            }
                        });
                }

                // Fade out text components
                var textComponents = lockOverlay.GetComponentsInChildren<TMPro.TextMeshPro>();
                foreach (var tmp in textComponents)
                {
                    LeanTween.value(tmp.gameObject, tmp.color.a, 0f, 0.4f)
                        .setOnUpdate((float val) => {
                            if (tmp != null)
                            {
                                var c = tmp.color;
                                tmp.color = new Color(c.r, c.g, c.b, val);
                            }
                        });
                }

                // Disable after animation
                LeanTween.delayedCall(0.5f, () => {
                    if (lockOverlay != null)
                        lockOverlay.SetActive(false);
                });
            }

            // Enable slot interactivity
            foreach (var slot in slotComponents)
            {
                slot.SetInteractive(true);
            }

            OnContainerUnlocked?.Invoke(this);
            GameEvents.InvokeContainerUnlocked(containerId);
        }

        #endregion

        #region Debug

        public void DebugLogState()
        {
            Debug.Log($"=== Container {containerId} State ===");
            Debug.Log($"Type: {containerType}, Locked: {isLocked}, Slots: {slotCount}");

            for (int s = 0; s < slots.Count; s++)
            {
                string slotStr = $"Slot {s}: ";
                for (int r = 0; r < slots[s].Count; r++)
                {
                    var item = slots[s][r];
                    slotStr += item != null ? $"[{item.ItemId}]" : "[empty]";
                    slotStr += " ";
                }
                Debug.Log(slotStr);
            }
        }

        #endregion

        private void OnDestroy()
        {
            LeanTween.cancel(gameObject);
        }
    }
}
