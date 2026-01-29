using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SortResort
{
    /// <summary>
    /// Manages drag and drop input for items using the new Unity Input System.
    /// Handles mouse and touch input, raycasting, and drop zone detection.
    /// </summary>
    public class DragDropManager : MonoBehaviour
    {
        public static DragDropManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private LayerMask itemLayerMask;
        [SerializeField] private LayerMask slotLayerMask;
        [SerializeField] private float dragThreshold = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool debugMode;

        // State
        private Item selectedItem;
        private Item draggedItem;
        private Slot currentHoveredSlot;
        private Vector3 pointerStartPosition;
        private bool isDragging;
        private bool pointerDown;

        // Input
        private InputAction pointerPositionAction;
        private InputAction pointerClickAction;

        // Cached raycast results
        private readonly RaycastHit2D[] raycastHits = new RaycastHit2D[10];

        // Properties
        public Item SelectedItem => selectedItem;
        public Item DraggedItem => draggedItem;
        public bool IsDragging => isDragging;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }

            SetupInput();
        }

        private void SetupInput()
        {
            // Create input actions for pointer
            pointerPositionAction = new InputAction("PointerPosition", InputActionType.Value, "<Pointer>/position");
            pointerClickAction = new InputAction("PointerClick", InputActionType.Button, "<Pointer>/press");

            pointerPositionAction.Enable();
            pointerClickAction.Enable();

            pointerClickAction.started += OnPointerDown;
            pointerClickAction.canceled += OnPointerUp;
        }

        private void OnDestroy()
        {
            pointerClickAction.started -= OnPointerDown;
            pointerClickAction.canceled -= OnPointerUp;

            pointerPositionAction?.Dispose();
            pointerClickAction?.Dispose();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!enabled) return;

            // Don't process input if game is not in playing state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (isDragging && draggedItem != null)
            {
                UpdateDrag();
                UpdateHoveredSlot();
            }
        }

        #region Input Handlers

        private void OnPointerDown(InputAction.CallbackContext context)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            pointerDown = true;
            Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
            Vector3 worldPos = gameCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;

            pointerStartPosition = worldPos;

            // Try to select an item
            Item hitItem = RaycastForItem(worldPos);
            if (hitItem != null && hitItem.IsInteractive)
            {
                SelectItem(hitItem);
                StartDrag(hitItem, worldPos);
            }
        }

        private void OnPointerUp(InputAction.CallbackContext context)
        {
            pointerDown = false;

            if (isDragging && draggedItem != null)
            {
                EndDrag();
            }

            DeselectItem();
        }

        #endregion

        #region Item Selection

        private void SelectItem(Item item)
        {
            if (selectedItem == item) return;

            DeselectItem();

            selectedItem = item;
            selectedItem.SetState(Item.ItemState.Selected);

            if (debugMode)
                Debug.Log($"[DragDropManager] Selected item: {item.ItemId}");
        }

        private void DeselectItem()
        {
            if (selectedItem != null)
            {
                if (selectedItem.CurrentState == Item.ItemState.Selected)
                {
                    selectedItem.SetState(Item.ItemState.Idle);
                }
                selectedItem = null;
            }
        }

        #endregion

        #region Drag Operations

        private void StartDrag(Item item, Vector3 worldPos)
        {
            if (item == null) return;

            bool success = item.StartDrag(worldPos);
            if (success)
            {
                isDragging = true;
                draggedItem = item;

                if (debugMode)
                    Debug.Log($"[DragDropManager] Started dragging: {item.ItemId}");
            }
        }

        private void UpdateDrag()
        {
            Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
            Vector3 worldPos = gameCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;

            draggedItem.UpdateDrag(worldPos);
        }

        private void EndDrag()
        {
            if (draggedItem == null) return;

            // Find the best slot to drop on
            Slot targetSlot = FindBestDropSlot();

            if (debugMode)
                Debug.Log($"[DragDropManager] Ending drag, target slot: {targetSlot?.SlotIndex.ToString() ?? "none"}");

            // End the drag on the item
            draggedItem.EndDrag(targetSlot);

            // Clear state
            isDragging = false;
            draggedItem = null;

            // Clear slot highlight
            if (currentHoveredSlot != null)
            {
                currentHoveredSlot.SetHighlight(false);
                currentHoveredSlot = null;
            }
        }

        /// <summary>
        /// Cancel the current drag operation
        /// </summary>
        public void CancelDrag()
        {
            if (draggedItem != null)
            {
                draggedItem.CancelDrag();
            }

            isDragging = false;
            draggedItem = null;

            if (currentHoveredSlot != null)
            {
                currentHoveredSlot.SetHighlight(false);
                currentHoveredSlot = null;
            }
        }

        #endregion

        #region Slot Detection

        private void UpdateHoveredSlot()
        {
            Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
            Vector3 worldPos = gameCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;

            Slot newSlot = RaycastForSlot(worldPos);

            if (newSlot != currentHoveredSlot)
            {
                // Unhighlight previous slot
                if (currentHoveredSlot != null)
                {
                    currentHoveredSlot.SetHighlight(false);
                }

                // Highlight new slot
                currentHoveredSlot = newSlot;
                if (currentHoveredSlot != null)
                {
                    bool canDrop = currentHoveredSlot.CanAcceptDrop();
                    currentHoveredSlot.SetHighlight(true, canDrop);
                }
            }
        }

        private Slot FindBestDropSlot()
        {
            if (draggedItem == null) return null;

            Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
            Vector3 worldPos = gameCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;

            // First check current hovered slot
            if (currentHoveredSlot != null && currentHoveredSlot.CanAcceptDrop())
            {
                return currentHoveredSlot;
            }

            // Raycast for slots
            Slot slot = RaycastForSlot(worldPos);
            if (slot != null && slot.CanAcceptDrop())
            {
                return slot;
            }

            return null;
        }

        #endregion

        #region Raycasting

        private Item RaycastForItem(Vector3 worldPos)
        {
            int hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, raycastHits, 0f, itemLayerMask);

            Item closestItem = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var item = raycastHits[i].collider.GetComponent<Item>();
                if (item != null && item.IsInteractive)
                {
                    float distance = Vector3.Distance(worldPos, item.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }

            return closestItem;
        }

        private Slot RaycastForSlot(Vector3 worldPos)
        {
            int hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, raycastHits, 0f, slotLayerMask);

            if (debugMode)
                Debug.Log($"[DragDrop] RaycastForSlot at {worldPos}, hits: {hitCount}, layerMask: {slotLayerMask.value}");

            Slot closestSlot = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var slot = raycastHits[i].collider.GetComponent<Slot>();
                if (debugMode)
                    Debug.Log($"[DragDrop] Hit {i}: {raycastHits[i].collider.name}, has Slot: {slot != null}");

                if (slot != null)
                {
                    float distance = Vector3.Distance(worldPos, slot.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSlot = slot;
                    }
                }
            }

            return closestSlot;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable or disable drag-drop functionality
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
            {
                CancelDrag();
            }
        }

        /// <summary>
        /// Check if there's an active drag operation
        /// </summary>
        public bool HasActiveDrag()
        {
            return isDragging && draggedItem != null;
        }

        #endregion
    }
}
