using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Slot represents a drop zone within a container that can hold items.
    /// Handles drop detection and visual feedback.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Slot : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex;
        [SerializeField] private int row;
        [SerializeField] private bool isFrontRow = true;
        [SerializeField] private bool isInteractive = true;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private Color normalColor = Color.clear;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.7f, 0.3f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.5f, 0.5f, 0.3f);

        [Header("Components")]
        [SerializeField] private BoxCollider2D boxCollider;

        // References
        private ItemContainer parentContainer;
        private List<Item> itemsInDropZone = new List<Item>();
        private bool isHighlighted;

        // Properties
        public int SlotIndex => slotIndex;
        public int Row => row;
        public bool IsFrontRow => isFrontRow;
        public bool IsInteractive => isInteractive;
        public ItemContainer ParentContainer => parentContainer;

        // Events
        public event Action<Slot, Item> OnItemDropped;
        public event Action<Slot> OnSlotClicked;

        private void Awake()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider2D>();

            // Make it a trigger for overlap detection
            if (boxCollider != null)
                boxCollider.isTrigger = true;
        }

        private void Start()
        {
            // Find parent container
            parentContainer = GetComponentInParent<ItemContainer>();

            // Initialize visual state
            SetHighlight(false);
        }

        /// <summary>
        /// Initialize this slot with configuration
        /// </summary>
        public void Initialize(int index, int rowNumber, ItemContainer container)
        {
            slotIndex = index;
            row = rowNumber;
            isFrontRow = (rowNumber == 0);
            parentContainer = container;
            isInteractive = isFrontRow;

            gameObject.name = $"Slot_{index}_Row_{rowNumber}";
        }

        /// <summary>
        /// Check if this slot can accept a drop
        /// </summary>
        public bool CanAcceptDrop()
        {
            // Must be interactive
            if (!isInteractive)
            {
                Debug.Log($"[Slot] {gameObject.name} - CanAcceptDrop FALSE: not interactive");
                return false;
            }

            // Must be front row
            if (!isFrontRow)
            {
                Debug.Log($"[Slot] {gameObject.name} - CanAcceptDrop FALSE: not front row");
                return false;
            }

            // Must be empty
            if (!IsEmpty())
            {
                Debug.Log($"[Slot] {gameObject.name} - CanAcceptDrop FALSE: not empty");
                return false;
            }

            // Container must not be locked
            if (parentContainer != null && parentContainer.IsLocked)
            {
                Debug.Log($"[Slot] {gameObject.name} - CanAcceptDrop FALSE: container locked");
                return false;
            }

            Debug.Log($"[Slot] {gameObject.name} - CanAcceptDrop TRUE");
            return true;
        }

        /// <summary>
        /// Check if this slot is empty
        /// </summary>
        public bool IsEmpty()
        {
            if (parentContainer == null)
                return true;

            return parentContainer.IsSlotEmpty(slotIndex, row);
        }

        /// <summary>
        /// Called when an item enters this slot's drop zone
        /// </summary>
        public void OnItemEntered(Item item)
        {
            if (item == null || !item.IsDragging)
                return;

            if (!itemsInDropZone.Contains(item))
            {
                itemsInDropZone.Add(item);
            }

            // Show visual feedback
            if (CanAcceptDrop())
            {
                SetHighlight(true);
            }
            else
            {
                SetHighlight(true, isValid: false);
            }

            Debug.Log($"[Slot] Item {item.ItemId} entered slot {slotIndex}");
        }

        /// <summary>
        /// Called when an item exits this slot's drop zone
        /// </summary>
        public void OnItemExited(Item item)
        {
            itemsInDropZone.Remove(item);

            // Hide highlight if no items in zone
            if (itemsInDropZone.Count == 0)
            {
                SetHighlight(false);
            }

            Debug.Log($"[Slot] Item {item?.ItemId} exited slot {slotIndex}");
        }

        /// <summary>
        /// Get the best item currently in this slot's drop zone
        /// </summary>
        public Item GetItemInDropZone()
        {
            // Clean up invalid items
            itemsInDropZone.RemoveAll(i => i == null || !i.IsDragging);

            if (itemsInDropZone.Count == 0)
                return null;

            // Return the closest item if multiple
            if (itemsInDropZone.Count == 1)
                return itemsInDropZone[0];

            Item closest = null;
            float closestDistance = float.MaxValue;

            foreach (var item in itemsInDropZone)
            {
                float distance = Vector3.Distance(transform.position, item.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = item;
                }
            }

            return closest;
        }

        /// <summary>
        /// Set the highlight state
        /// </summary>
        public void SetHighlight(bool highlighted, bool isValid = true)
        {
            isHighlighted = highlighted;

            if (highlightRenderer != null)
            {
                if (highlighted)
                {
                    highlightRenderer.color = isValid ? highlightColor : invalidColor;

                    // Animate scale
                    LeanTween.cancel(gameObject);
                    LeanTween.scale(gameObject, Vector3.one * 1.05f, 0.15f)
                        .setEase(LeanTweenType.easeOutQuad);
                }
                else
                {
                    highlightRenderer.color = normalColor;

                    // Reset scale
                    LeanTween.cancel(gameObject);
                    LeanTween.scale(gameObject, Vector3.one, 0.15f)
                        .setEase(LeanTweenType.easeOutQuad);
                }
            }
        }

        /// <summary>
        /// Set whether this slot is interactive
        /// </summary>
        public void SetInteractive(bool interactive)
        {
            isInteractive = interactive;

            if (boxCollider != null)
            {
                boxCollider.enabled = interactive;
            }

            // Clear highlight if becoming non-interactive
            if (!interactive)
            {
                SetHighlight(false);
                itemsInDropZone.Clear();
            }
        }

        /// <summary>
        /// Update front row status
        /// </summary>
        public void SetFrontRow(bool frontRow)
        {
            isFrontRow = frontRow;
            SetInteractive(frontRow);
        }

        /// <summary>
        /// Get the world position for placing an item in this slot
        /// </summary>
        public Vector3 GetItemPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Force clear all items from drop zone tracking
        /// </summary>
        public void ClearDropZone()
        {
            itemsInDropZone.Clear();
            SetHighlight(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponent<Item>();
            if (item != null)
            {
                OnItemEntered(item);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var item = other.GetComponent<Item>();
            if (item != null)
            {
                OnItemExited(item);
            }
        }

        private void OnDestroy()
        {
            LeanTween.cancel(gameObject);
            itemsInDropZone.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw slot bounds in editor
            Gizmos.color = isFrontRow ? Color.green : Color.gray;
            Gizmos.DrawWireCube(transform.position, new Vector3(1.28f, 2.56f, 0.1f));
        }
#endif
    }
}
