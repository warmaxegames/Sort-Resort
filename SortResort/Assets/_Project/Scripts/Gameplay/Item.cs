using System;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Item represents a draggable game object that can be placed in containers.
    /// Handles visual states, drag-drop interaction, and pooling support.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Item : MonoBehaviour
    {
        public enum ItemState
        {
            Idle,
            Selected,
            Dragging,
            Matched,
            Returning
        }

        [Header("Item Data")]
        [SerializeField] private string itemId;
        [SerializeField] private Sprite itemSprite;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private GameObject selectionIndicator;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        [SerializeField] private Color dragColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        [SerializeField] private float selectedScale = 1.05f;
        [SerializeField] private float dragScale = 1.1f;
        [SerializeField] private int dragSortingOrder = 100;

        // State
        private ItemState currentState = ItemState.Idle;
        private bool isInteractive = true;
        private int originalSortingOrder;
        private Vector3 originalScale;

        // Drag tracking
        private Vector3 dragOffset;
        private Vector3 originalPosition;
        private Transform originalParent;
        private Slot originalSlot;
        private ItemContainer originalContainer;

        // Pool tracking
        private bool isPooled = false;
        private Action<Item> onReturnToPool;

        // Properties
        public string ItemId => itemId;
        public ItemState CurrentState => currentState;
        public bool IsInteractive => isInteractive && currentState != ItemState.Matched;
        public bool IsDragging => currentState == ItemState.Dragging;
        public bool IsMatched => currentState == ItemState.Matched;
        public Slot CurrentSlot { get; private set; }
        public ItemContainer CurrentContainer { get; private set; }

        // Events
        public event Action<Item> OnSelected;
        public event Action<Item> OnDeselected;
        public event Action<Item> OnDragStarted;
        public event Action<Item> OnDragEnded;
        public event Action<Item> OnMatched;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider2D>();

            originalScale = transform.localScale;
            originalSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

            Debug.Log($"[Item] Awake - {gameObject.name} originalScale captured: {originalScale}");
        }

        private void OnEnable()
        {
            ResetState();
        }

        /// <summary>
        /// Configure this item with data
        /// </summary>
        public void Configure(string id, Sprite sprite, string worldItemPath = null)
        {
            itemId = id;
            itemSprite = sprite;

            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            gameObject.name = $"Item_{id}";
        }

        /// <summary>
        /// Recapture the original scale (call after external scale changes)
        /// </summary>
        public void RecaptureOriginalScale()
        {
            originalScale = transform.localScale;
            Debug.Log($"[Item] RecaptureOriginalScale - {itemId} new originalScale: {originalScale}");
        }

        /// <summary>
        /// Set the slot this item occupies
        /// </summary>
        public void SetSlot(Slot slot, ItemContainer container)
        {
            CurrentSlot = slot;
            CurrentContainer = container;
        }

        /// <summary>
        /// Clear the slot reference
        /// </summary>
        public void ClearSlot()
        {
            CurrentSlot = null;
            CurrentContainer = null;
        }

        #region State Management

        public void SetState(ItemState newState)
        {
            if (currentState == newState) return;

            var previousState = currentState;
            currentState = newState;

            OnStateChanged(previousState, newState);
        }

        private void OnStateChanged(ItemState previousState, ItemState newState)
        {
            switch (newState)
            {
                case ItemState.Idle:
                    ApplyIdleVisuals();
                    break;
                case ItemState.Selected:
                    ApplySelectedVisuals();
                    OnSelected?.Invoke(this);
                    break;
                case ItemState.Dragging:
                    ApplyDragVisuals();
                    OnDragStarted?.Invoke(this);
                    break;
                case ItemState.Matched:
                    OnMatched?.Invoke(this);
                    break;
                case ItemState.Returning:
                    ApplyIdleVisuals();
                    break;
            }
        }

        private void ApplyIdleVisuals()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;

            Debug.Log($"[Item] ApplyIdleVisuals - {itemId} resetting scale from {transform.localScale} to {originalScale}");
            transform.localScale = originalScale;
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = originalSortingOrder;

            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
        }

        private void ApplySelectedVisuals()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = selectedColor;

            transform.localScale = originalScale * selectedScale;

            if (selectionIndicator != null)
                selectionIndicator.SetActive(true);
        }

        private void ApplyDragVisuals()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = dragColor;

            transform.localScale = originalScale * dragScale;
            spriteRenderer.sortingOrder = dragSortingOrder;

            if (selectionIndicator != null)
                selectionIndicator.SetActive(true);
        }

        #endregion

        #region Drag and Drop

        /// <summary>
        /// Start dragging this item
        /// </summary>
        public bool StartDrag(Vector3 pointerWorldPosition)
        {
            if (!IsInteractive) return false;
            if (currentState == ItemState.Dragging) return false;

            Debug.Log($"[Item] StartDrag - {itemId} from container: {CurrentContainer?.ContainerId ?? "null"}, slot: {CurrentSlot?.SlotIndex.ToString() ?? "null"}");

            // Store original state for potential cancel
            originalPosition = transform.position;
            originalParent = transform.parent;
            originalSlot = CurrentSlot;
            originalContainer = CurrentContainer;

            // Calculate drag offset
            dragOffset = transform.position - pointerWorldPosition;

            // Clear from current slot (but keep references for cancel)
            if (CurrentContainer != null)
            {
                Debug.Log($"[Item] StartDrag - Removing {itemId} from container {CurrentContainer.ContainerId}");
                CurrentContainer.RemoveItemFromSlot(this);
            }
            else
            {
                Debug.LogWarning($"[Item] StartDrag - {itemId} has no CurrentContainer!");
            }

            SetState(ItemState.Dragging);

            GameEvents.InvokeItemPickedUp(gameObject);

            return true;
        }

        /// <summary>
        /// Update drag position
        /// </summary>
        public void UpdateDrag(Vector3 pointerWorldPosition)
        {
            if (currentState != ItemState.Dragging) return;

            transform.position = pointerWorldPosition + dragOffset;
        }

        /// <summary>
        /// End drag - attempt to drop on target slot
        /// </summary>
        public void EndDrag(Slot targetSlot)
        {
            if (currentState != ItemState.Dragging) return;

            if (targetSlot != null && targetSlot.CanAcceptDrop())
            {
                // Valid drop
                DropOnSlot(targetSlot);
            }
            else
            {
                // Invalid drop - return to original position
                CancelDrag();
            }
        }

        /// <summary>
        /// Drop this item on a slot
        /// </summary>
        private void DropOnSlot(Slot slot)
        {
            var container = slot.ParentContainer;
            if (container == null)
            {
                Debug.Log($"[Item] DropOnSlot FAILED - slot {slot.name} has no ParentContainer!");
                CancelDrag();
                return;
            }

            Debug.Log($"[Item] DropOnSlot - {itemId} dropping on container {container.ContainerId}, slot {slot.SlotIndex}");

            // Place in new slot
            bool success = container.PlaceItemInSlot(this, slot.SlotIndex);

            if (success)
            {
                Debug.Log($"[Item] DropOnSlot - {itemId} placed successfully. Resetting scale from {transform.localScale} to {originalScale}");

                // Force reset scale before setting state (in case parenting changed it)
                transform.localScale = originalScale;

                SetState(ItemState.Idle);
                GameEvents.InvokeItemDropped(gameObject);
                OnDragEnded?.Invoke(this);

                // Increment move count
                GameManager.Instance?.IncrementMoveCount();
            }
            else
            {
                Debug.Log($"[Item] DropOnSlot - {itemId} placement FAILED, canceling drag");
                CancelDrag();
            }
        }

        /// <summary>
        /// Cancel drag and return to original position
        /// </summary>
        public void CancelDrag()
        {
            if (currentState != ItemState.Dragging && currentState != ItemState.Returning)
            {
                SetState(ItemState.Idle);
                return;
            }

            SetState(ItemState.Returning);

            // Return to original container/slot
            if (originalContainer != null && originalSlot != null)
            {
                originalContainer.PlaceItemInSlot(this, originalSlot.SlotIndex, forcePlace: true);
            }

            // Animate back to original position using LeanTween
            LeanTween.move(gameObject, originalPosition, 0.2f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    SetState(ItemState.Idle);
                    GameEvents.InvokeItemReturnedToOrigin(gameObject);
                    OnDragEnded?.Invoke(this);
                });
        }

        #endregion

        #region Matching

        /// <summary>
        /// Mark this item as matched and play animation
        /// </summary>
        public void MarkAsMatched(Action onComplete = null)
        {
            if (currentState == ItemState.Matched) return;

            SetState(ItemState.Matched);
            isInteractive = false;

            // Play match animation
            PlayMatchAnimation(() =>
            {
                onComplete?.Invoke();
                ReturnToPool();
            });
        }

        private void PlayMatchAnimation(Action onComplete)
        {
            // Fade out and scale down
            LeanTween.alpha(gameObject, 0f, 0.5f);
            LeanTween.scale(gameObject, Vector3.zero, 0.5f)
                .setEase(LeanTweenType.easeInBack)
                .setOnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region Pooling

        /// <summary>
        /// Set up pooling callback
        /// </summary>
        public void SetPoolCallback(Action<Item> returnCallback)
        {
            onReturnToPool = returnCallback;
            isPooled = true;
        }

        /// <summary>
        /// Return this item to the pool
        /// </summary>
        public void ReturnToPool()
        {
            ResetState();

            if (isPooled && onReturnToPool != null)
            {
                onReturnToPool.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Reset item to default state
        /// </summary>
        public void ResetState()
        {
            currentState = ItemState.Idle;
            isInteractive = true;

            CurrentSlot = null;
            CurrentContainer = null;
            originalSlot = null;
            originalContainer = null;

            // Reset visuals
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
                spriteRenderer.sortingOrder = originalSortingOrder;
            }

            transform.localScale = originalScale;

            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);

            // Cancel any active tweens
            LeanTween.cancel(gameObject);
        }

        #endregion

        #region Interactivity

        public void SetInteractive(bool interactive)
        {
            isInteractive = interactive;

            if (boxCollider != null)
            {
                boxCollider.enabled = interactive;
            }
        }

        /// <summary>
        /// Set the visual depth (for back row items)
        /// </summary>
        public void SetRowDepth(int row, int maxRows)
        {
            if (row == 0)
            {
                // Front row - fully visible and interactive
                if (spriteRenderer != null)
                    spriteRenderer.color = normalColor;
                SetInteractive(true);
            }
            else
            {
                // Back row - grayed out and non-interactive
                float grayAmount = row == 1 ? 0.5f : 0.3f;
                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(grayAmount, grayAmount, grayAmount, 1f);
                SetInteractive(false);
            }

            // Adjust sorting order based on row
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = originalSortingOrder - row;
            }
        }

        #endregion

        #region Collision Detection

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsDragging) return;

            var slot = other.GetComponent<Slot>();
            if (slot != null)
            {
                slot.OnItemEntered(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsDragging) return;

            var slot = other.GetComponent<Slot>();
            if (slot != null)
            {
                slot.OnItemExited(this);
            }
        }

        #endregion
    }
}
