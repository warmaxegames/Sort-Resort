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
        [SerializeField] private Vector2 slotSize = new Vector2(128f, 256f);

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
        [SerializeField] private float slotSpacing = 128f;
        [SerializeField] private float rowDepthOffset = 10f;

        // Slot data structure: slots[slotIndex][row] = Item
        private List<List<Item>> slots = new List<List<Item>>();
        private List<Slot> slotComponents = new List<Slot>();

        // Properties
        public string ContainerId => containerId;
        public ContainerType Type => containerType;
        public bool IsLocked => isLocked;
        public int SlotCount => slotCount;

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
            containerId = definition.id;
            containerType = definition.container_type == "single_slot"
                ? ContainerType.SingleSlot
                : ContainerType.Standard;
            slotCount = definition.slot_count > 0 ? definition.slot_count : 3;
            maxRowsPerSlot = definition.max_rows_per_slot > 0 ? definition.max_rows_per_slot : 4;
            isLocked = definition.is_locked;
            unlockMatchesRequired = definition.unlock_matches_required;
            currentUnlockProgress = 0;

            // Set position
            transform.position = definition.position.ToVector3();

            // Initialize slot structure
            InitializeSlots();

            // Setup lock overlay
            UpdateLockVisuals();

            gameObject.name = $"Container_{containerId}";
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

            // Position item in world space at bottom of slot
            float itemHeight = GetItemHeight(item);
            Vector3 newPos = GetItemWorldPositionBottomAligned(slotIndex, 0, itemHeight);
            Debug.Log($"[ItemContainer] PlaceItemInSlot {item.ItemId}: itemHeight={itemHeight:F4}, oldPos={item.transform.position}, newPos={newPos}");
            item.transform.position = newPos;
            item.SetRowDepth(0, maxRowsPerSlot);

            Debug.Log($"[ItemContainer] Placed item {item.ItemId} in slot {slotIndex}");

            // Check for matches
            CheckForMatches();

            return true;
        }

        /// <summary>
        /// Get the rendered height of an item in world units
        /// </summary>
        private float GetItemHeight(Item item)
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

            item.transform.SetParent(slotsParent);
            float itemHeight = GetItemHeight(item);
            item.transform.position = GetItemWorldPositionBottomAligned(slotIndex, row, itemHeight);
            item.SetRowDepth(row, maxRowsPerSlot);

            return true;
        }

        /// <summary>
        /// Remove an item from its slot
        /// </summary>
        public void RemoveItemFromSlot(Item item)
        {
            Debug.Log($"[ItemContainer] RemoveItemFromSlot called for {item?.ItemId ?? "null"} in container {containerId}");

            if (item == null)
            {
                Debug.LogWarning("[ItemContainer] RemoveItemFromSlot called with null item!");
                return;
            }

            bool found = false;
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
                        return;
                    }
                }
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

            // Play match sound
            AudioManager.Instance?.PlayMatchSound();

            // Mark items as matched and remove from slots
            int matchedCount = 0;
            foreach (var item in matchedItems)
            {
                if (item != null)
                {
                    // Clear from slot data
                    RemoveItemFromSlot(item);

                    // Play match animation
                    item.MarkAsMatched();
                    matchedCount++;
                }
            }

            // Fire event and increment match count
            OnItemsMatched?.Invoke(this, itemId, matchedCount);
            GameManager.Instance?.IncrementMatchCount(itemId);

            // Update lock progress
            if (isLocked)
            {
                IncrementUnlockProgress();
            }

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
        /// Advance items from back rows to front
        /// </summary>
        public void AdvanceRows()
        {
            bool anyAdvanced = false;

            for (int s = 0; s < slots.Count; s++)
            {
                // If front row is empty and there are back row items
                if (slots[s][0] == null)
                {
                    // Find first non-null item in back rows
                    for (int r = 1; r < slots[s].Count; r++)
                    {
                        if (slots[s][r] != null)
                        {
                            // Move all items forward
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
            }

            if (anyAdvanced)
            {
                // Update all item positions and visuals
                UpdateAllItemPositions();

                OnRowAdvanced?.Invoke(this);

                // Check for new matches
                LeanTween.delayedCall(0.2f, CheckForMatches);
            }
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
                        // Animate to new position (bottom-aligned)
                        float itemHeight = GetItemHeight(item);
                        Vector3 targetPos = GetItemWorldPositionBottomAligned(s, r, itemHeight);
                        LeanTween.move(item.gameObject, targetPos, 0.2f)
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

            // Animate lock overlay
            if (lockOverlay != null)
            {
                LeanTween.alpha(lockOverlay, 0f, 0.3f)
                    .setOnComplete(() => lockOverlay.SetActive(false));
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
