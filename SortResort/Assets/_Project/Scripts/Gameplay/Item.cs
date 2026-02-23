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

        // Drag tilt
        private float currentTilt = 0f;
        private const float MaxTilt = 15f;
        private const float TiltSpeed = 12f;
        private const float TiltSensitivity = 2f;

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
        /// Scale the item to fit within slot dimensions while maintaining aspect ratio
        /// </summary>
        public void ScaleToFitSlot(float slotWidth, float slotHeight, float padding = 0.9f)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                Debug.LogWarning($"[Item] ScaleToFitSlot - {itemId} has no sprite!");
                return;
            }

            var sprite = spriteRenderer.sprite;
            float spriteWidth = sprite.bounds.size.x;
            float spriteHeight = sprite.bounds.size.y;

            // Calculate scale to fit within slot (with padding)
            float targetWidth = slotWidth * padding;
            float targetHeight = slotHeight * padding;

            float scaleX = targetWidth / spriteWidth;
            float scaleY = targetHeight / spriteHeight;

            // Use the smaller scale to maintain aspect ratio
            float scale = Mathf.Min(scaleX, scaleY);

            transform.localScale = new Vector3(scale, scale, 1f);

            // Update collider size to match sprite bounds
            if (boxCollider != null)
            {
                boxCollider.size = sprite.bounds.size;
                boxCollider.enabled = true;
            }

            // Recapture so drag/drop uses correct scale
            RecaptureOriginalScale();

            Debug.Log($"[Item] ScaleToFitSlot - {itemId} scaled to {scale:F3} (sprite: {spriteWidth:F2}x{spriteHeight:F2}, slot: {slotWidth:F2}x{slotHeight:F2})");
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
                    ApplyReturningVisuals();
                    break;
            }
        }

        private void ApplyIdleVisuals()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;

            Debug.Log($"[Item] ApplyIdleVisuals - {itemId} resetting scale from {transform.localScale} to {originalScale}");
            transform.localScale = originalScale;
            transform.rotation = Quaternion.identity;
            currentTilt = 0f;
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = originalSortingOrder;

            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
        }

        private void ApplyReturningVisuals()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;

            transform.localScale = originalScale;
            // Don't reset rotation here — CancelDrag animates it back to 0
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
            // Don't trigger row advance yet - wait until item is successfully placed elsewhere
            if (CurrentContainer != null)
            {
                Debug.Log($"[Item] StartDrag - Removing {itemId} from container {CurrentContainer.ContainerId}");
                CurrentContainer.RemoveItemFromSlot(this, triggerRowAdvance: false);
            }
            else
            {
                Debug.LogWarning($"[Item] StartDrag - {itemId} has no CurrentContainer!");
            }

            SetState(ItemState.Dragging);

            // Play drag sound
            AudioManager.Instance?.PlayDragSound();

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
        /// Update tilt rotation based on drag velocity
        /// </summary>
        public void UpdateDragTilt(Vector3 dragDelta)
        {
            if (currentState != ItemState.Dragging) return;

            // Convert per-frame delta to velocity (units/sec) for framerate independence
            float dragVelocityX = Time.deltaTime > 0f ? dragDelta.x / Time.deltaTime : 0f;
            float targetTilt = Mathf.Clamp(-dragVelocityX * TiltSensitivity, -MaxTilt, MaxTilt);
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, TiltSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, currentTilt);
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

            // Notify combo tracker that a drop is starting
            ComboTracker.NotifyDropStarted();

            // Check if item is being placed back in the same position
            bool isSamePosition = (originalContainer == container) &&
                                  (originalSlot != null && originalSlot.SlotIndex == slot.SlotIndex);

            // Reset scale and tilt BEFORE placing so GetItemHeight returns correct value
            transform.localScale = originalScale;
            transform.rotation = Quaternion.identity;
            currentTilt = 0f;

            // Place in new slot
            bool success = container.PlaceItemInSlot(this, slot.SlotIndex);

            if (success)
            {
                Debug.Log($"[Item] DropOnSlot - {itemId} placed successfully (samePosition: {isSamePosition})");

                // Play drop sound
                AudioManager.Instance?.PlayDropSound();

                // Skip move tracking if placed back in same position
                if (!isSamePosition)
                {
                    // Record move for comparison (records ALL moves including matches)
                    LevelManager.Instance?.RecordMoveForComparison(
                        this,
                        originalContainer,
                        originalSlot?.SlotIndex ?? 0,
                        container,
                        slot.SlotIndex
                    );

                    // Calculate row advancement info BEFORE it happens (for undo)
                    bool rowAdvancementWillOccur = false;
                    int[] rowAdvancementOffsets = null;
                    if (originalContainer != null)
                    {
                        rowAdvancementWillOccur = originalContainer.WouldRowAdvancement();
                        if (rowAdvancementWillOccur)
                        {
                            rowAdvancementOffsets = originalContainer.CalculateRowAdvancementOffsets();
                            Debug.Log($"[Item] Row advancement will occur in {originalContainer.ContainerId}");
                        }
                    }

                    // Always record move for solver comparison tracking
                    // Pass whether a match occurred so RecordMove can decide about undo history
                    bool matchOccurred = currentState == ItemState.Matched;
                    LevelManager.Instance?.RecordMove(
                        this,
                        originalContainer,
                        originalSlot?.SlotIndex ?? 0,
                        originalSlot?.Row ?? 0,
                        container,
                        slot.SlotIndex,
                        slot.Row,
                        rowAdvancementWillOccur,
                        rowAdvancementOffsets,
                        matchOccurred
                    );

                    // Increment move count only for actual moves
                    GameManager.Instance?.IncrementMoveCount();

                    // Now trigger row advancement (after recording the move)
                    if (originalContainer != null)
                    {
                        originalContainer.CheckAndAdvanceAllRows();
                    }
                }

                if (currentState != ItemState.Matched)
                {
                    SetState(ItemState.Idle);
                }

                GameEvents.InvokeItemDropped(gameObject);
                OnDragEnded?.Invoke(this);
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
                // Place in slot first (this parents the item to the container and sets up slot data)
                // Use skipPositioning=true so we can animate to the position
                originalContainer.PlaceItemInSlotNoPosition(this, originalSlot.SlotIndex);

                // Get local target position within the container
                float itemHeight = originalContainer.GetItemHeight(this);
                Vector3 targetLocalPos = originalContainer.GetItemLocalPositionBottomAligned(originalSlot.SlotIndex, 0, itemHeight);

                // Animate LOCAL position (so item moves WITH the container if it's moving)
                LeanTween.moveLocal(gameObject, targetLocalPos, 0.2f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnComplete(() =>
                    {
                        SetState(ItemState.Idle);
                        GameEvents.InvokeItemReturnedToOrigin(gameObject);
                        OnDragEnded?.Invoke(this);
                    });
                // Animate tilt back to 0
                LeanTween.rotateZ(gameObject, 0f, 0.2f).setEase(LeanTweenType.easeOutQuad);
                currentTilt = 0f;
            }
            else
            {
                // No original container - just animate back to original position
                LeanTween.move(gameObject, originalPosition, 0.2f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnComplete(() =>
                    {
                        SetState(ItemState.Idle);
                        GameEvents.InvokeItemReturnedToOrigin(gameObject);
                        OnDragEnded?.Invoke(this);
                    });
                // Animate tilt back to 0
                LeanTween.rotateZ(gameObject, 0f, 0.2f).setEase(LeanTweenType.easeOutQuad);
                currentTilt = 0f;
            }
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
            Debug.Log($"[Item] PlayMatchAnimation starting for {itemId}");

            // Fade out using SpriteRenderer color (LeanTween.alpha doesn't work well with sprites)
            if (spriteRenderer != null)
            {
                LeanTween.value(gameObject, spriteRenderer.color.a, 0f, 0.5f)
                    .setOnUpdate((float val) => {
                        if (spriteRenderer != null)
                        {
                            var c = spriteRenderer.color;
                            spriteRenderer.color = new Color(c.r, c.g, c.b, val);
                        }
                    });
            }

            // Scale down
            LeanTween.scale(gameObject, Vector3.zero, 0.5f)
                .setEase(LeanTweenType.easeInBack)
                .setOnComplete(() => {
                    Debug.Log($"[Item] PlayMatchAnimation complete for {itemId}");
                    onComplete?.Invoke();
                });
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
            // Cancel any active tweens FIRST
            LeanTween.cancel(gameObject);

            currentState = ItemState.Idle;
            isInteractive = true;

            CurrentSlot = null;
            CurrentContainer = null;
            originalSlot = null;
            originalContainer = null;

            // Reset visuals
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor; // This resets alpha to 1
                spriteRenderer.sortingOrder = originalSortingOrder;
            }

            // Reset rotation/tilt
            transform.rotation = Quaternion.identity;
            currentTilt = 0f;

            // Reset scale (use Vector3.one if originalScale is zero from match animation)
            if (originalScale == Vector3.zero || originalScale.x < 0.01f)
            {
                transform.localScale = Vector3.one;
            }
            else
            {
                transform.localScale = originalScale;
            }

            // Re-enable collider
            if (boxCollider != null)
            {
                boxCollider.enabled = true;
            }

            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);

            Debug.Log($"[Item] ResetState - {itemId} scale: {transform.localScale}, interactive: {isInteractive}");
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
