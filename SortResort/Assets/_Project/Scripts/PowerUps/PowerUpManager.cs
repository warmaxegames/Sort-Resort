using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SortResort
{
    public enum PowerUpInteractionMode
    {
        None,
        SelectLockedContainer,
        SelectFirstItem,
        SelectSecondItem
    }

    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        private PowerUpInteractionMode interactionMode = PowerUpInteractionMode.None;
        private PowerUpType activeType;
        private Item firstSelectedItem;
        private SpriteRenderer firstSelectedOriginalRenderer;
        private Color firstSelectedOriginalColor;
        private bool isMovesFrozen;
        private Coroutine movesFreezeCoroutine;

        // Input System
        private InputAction pointerPositionAction;
        private InputAction pointerClickAction;

        // UI reference
        private PowerUpBarUI barUI;

        // Cached raycast results
        private readonly RaycastHit2D[] raycastHits = new RaycastHit2D[10];

        public bool IsMovesFrozen => isMovesFrozen;
        public PowerUpInteractionMode CurrentMode => interactionMode;

        private PowerUpBarUI BarUI
        {
            get
            {
                if (barUI == null)
                    barUI = FindAnyObjectByType<PowerUpBarUI>();
                return barUI;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupInput();
        }

        private void SetupInput()
        {
            pointerPositionAction = new InputAction("PointerPosition", InputActionType.Value, "<Pointer>/position");
            pointerClickAction = new InputAction("PointerClick", InputActionType.Button, "<Pointer>/press");

            pointerPositionAction.Enable();
            pointerClickAction.Enable();

            pointerClickAction.started += OnPointerDown;
        }

        private void OnDestroy()
        {
            if (pointerClickAction != null)
            {
                pointerClickAction.started -= OnPointerDown;
                pointerClickAction.Dispose();
            }
            pointerPositionAction?.Dispose();

            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += OnLevelEnd;
            GameEvents.OnLevelFailed += OnLevelFailed;
            GameEvents.OnLevelRestarted += OnLevelRestarted;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= OnLevelEnd;
            GameEvents.OnLevelFailed -= OnLevelFailed;
            GameEvents.OnLevelRestarted -= OnLevelRestarted;
        }

        private void OnPointerDown(InputAction.CallbackContext context)
        {
            if (interactionMode == PowerUpInteractionMode.None) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            HandleSelectionClick();
        }

        #region Public API

        public int GetCount(PowerUpType type)
        {
            return SaveManager.Instance?.GetPowerUpCount(type) ?? 0;
        }

        public void UseCharge(PowerUpType type)
        {
            if (SaveManager.Instance == null) return;
            int current = SaveManager.Instance.GetPowerUpCount(type);
            if (current > 0)
            {
                current--;
                SaveManager.Instance.SetPowerUpCount(type, current);
                GameEvents.InvokePowerUpCountChanged(type, current);
                GameEvents.InvokePowerUpUsed(type);
            }
        }

        public void AddCharge(PowerUpType type, int amount = 1)
        {
            if (SaveManager.Instance == null) return;
            int current = SaveManager.Instance.GetPowerUpCount(type);
            current += amount;
            SaveManager.Instance.SetPowerUpCount(type, current);
            GameEvents.InvokePowerUpCountChanged(type, current);
        }

        /// <summary>
        /// Called when a power-up button is clicked.
        /// </summary>
        public void ActivatePowerUp(PowerUpType type)
        {
            // If same button clicked again during selection, cancel
            if (interactionMode != PowerUpInteractionMode.None && activeType == type)
            {
                CancelInteraction();
                return;
            }

            // Cancel any existing interaction first
            if (interactionMode != PowerUpInteractionMode.None)
            {
                CancelInteraction();
            }

            int count = GetCount(type);

            // If count is 0, grant a free charge (ad placeholder)
            if (count <= 0)
            {
                AddCharge(type, 1);
                Debug.Log($"[PowerUpManager] Ad placeholder: granted 1 free {type}");
                return; // Don't activate yet, they got the charge
            }

            switch (type)
            {
                case PowerUpType.SwapItems:
                    StartSwapItemsMode();
                    break;
                case PowerUpType.DestroyLocker:
                    StartDestroyLockerMode();
                    break;
                case PowerUpType.MoveFreeze:
                    ActivateMoveFreeze();
                    break;
                case PowerUpType.TimeFreeze:
                    ActivateTimeFreeze();
                    break;
            }
        }

        public void CancelInteraction()
        {
            if (firstSelectedItem != null && firstSelectedOriginalRenderer != null)
            {
                firstSelectedOriginalRenderer.color = firstSelectedOriginalColor;
            }
            firstSelectedItem = null;
            firstSelectedOriginalRenderer = null;
            interactionMode = PowerUpInteractionMode.None;

            // Clear button highlight
            BarUI?.ClearAllHighlights();

            // Re-enable drag drop
            if (DragDropManager.Instance != null)
                DragDropManager.Instance.SetEnabled(true);

            Debug.Log("[PowerUpManager] Interaction cancelled");
        }

        #endregion

        #region Swap Items

        private void StartSwapItemsMode()
        {
            activeType = PowerUpType.SwapItems;
            interactionMode = PowerUpInteractionMode.SelectFirstItem;
            firstSelectedItem = null;

            // Disable normal drag-drop during selection
            if (DragDropManager.Instance != null)
                DragDropManager.Instance.SetEnabled(false);

            // Highlight the active button yellow
            BarUI?.SetButtonHighlight(PowerUpType.SwapItems, true);

            Debug.Log("[PowerUpManager] Swap Items: select first item");
        }

        private void HandleSwapFirstItemClick(Vector3 worldPos)
        {
            Item item = RaycastForFrontRowItem(worldPos);
            if (item == null) return;

            // Highlight selected item yellow
            firstSelectedItem = item;
            firstSelectedOriginalRenderer = item.GetComponent<SpriteRenderer>();
            if (firstSelectedOriginalRenderer != null)
            {
                firstSelectedOriginalColor = firstSelectedOriginalRenderer.color;
                firstSelectedOriginalRenderer.color = Color.yellow;
            }

            interactionMode = PowerUpInteractionMode.SelectSecondItem;
            Debug.Log($"[PowerUpManager] Swap Items: first item selected - {item.ItemId}");
        }

        private void HandleSwapSecondItemClick(Vector3 worldPos)
        {
            Item item = RaycastForFrontRowItem(worldPos);
            if (item == null) return;

            // If same item clicked, deselect
            if (item == firstSelectedItem)
            {
                if (firstSelectedOriginalRenderer != null)
                    firstSelectedOriginalRenderer.color = firstSelectedOriginalColor;
                firstSelectedItem = null;
                firstSelectedOriginalRenderer = null;
                interactionMode = PowerUpInteractionMode.SelectFirstItem;
                Debug.Log("[PowerUpManager] Swap Items: deselected first item");
                return;
            }

            // Execute swap
            Item secondItem = item;
            ExecuteSwap(firstSelectedItem, secondItem);
        }

        private void ExecuteSwap(Item itemA, Item itemB)
        {
            // Restore first item color
            if (firstSelectedOriginalRenderer != null)
                firstSelectedOriginalRenderer.color = firstSelectedOriginalColor;

            var containerA = itemA.CurrentContainer;
            var containerB = itemB.CurrentContainer;
            var slotA = itemA.CurrentSlot;
            var slotB = itemB.CurrentSlot;

            if (containerA == null || containerB == null || slotA == null || slotB == null)
            {
                Debug.LogWarning("[PowerUpManager] Swap failed: items don't have valid slots");
                CancelInteraction();
                return;
            }

            int slotIndexA = slotA.SlotIndex;
            int slotIndexB = slotB.SlotIndex;

            // Remove both items from their slots (no row advance during swap)
            containerA.RemoveItemFromSlot(itemA, false);
            containerB.RemoveItemFromSlot(itemB, false);

            // Get target positions before placing
            float heightA = containerB.GetItemHeight(itemA);
            float heightB = containerA.GetItemHeight(itemB);
            Vector3 targetPosA = containerB.GetItemLocalPositionBottomAligned(slotIndexB, 0, heightA);
            Vector3 targetPosB = containerA.GetItemLocalPositionBottomAligned(slotIndexA, 0, heightB);

            // Place items in swapped slots (data only)
            containerB.PlaceItemInSlotNoPosition(itemA, slotIndexB);
            containerA.PlaceItemInSlotNoPosition(itemB, slotIndexA);

            // Tween to new positions
            LeanTween.moveLocal(itemA.gameObject, targetPosA, 0.4f).setEaseOutQuad();
            LeanTween.moveLocal(itemB.gameObject, targetPosB, 0.4f).setEaseOutQuad().setOnComplete(() =>
            {
                // Check for matches after swap completes
                containerA.CheckForMatches();
                if (containerB != containerA)
                    containerB.CheckForMatches();
            });

            // Use charge (swap is free - no move counted)
            UseCharge(PowerUpType.SwapItems);

            // Play button click sound
            AudioManager.Instance?.PlayButtonClick();

            // End interaction
            firstSelectedItem = null;
            firstSelectedOriginalRenderer = null;
            interactionMode = PowerUpInteractionMode.None;

            // Clear button highlight
            BarUI?.ClearAllHighlights();

            // Re-enable drag drop
            if (DragDropManager.Instance != null)
                DragDropManager.Instance.SetEnabled(true);

            Debug.Log($"[PowerUpManager] Swap Items: swapped {itemA.ItemId} and {itemB.ItemId}");
        }

        #endregion

        #region Destroy Locker

        private void StartDestroyLockerMode()
        {
            activeType = PowerUpType.DestroyLocker;
            interactionMode = PowerUpInteractionMode.SelectLockedContainer;

            // Disable normal drag-drop during selection
            if (DragDropManager.Instance != null)
                DragDropManager.Instance.SetEnabled(false);

            // Highlight the active button yellow
            BarUI?.SetButtonHighlight(PowerUpType.DestroyLocker, true);

            Debug.Log("[PowerUpManager] Destroy Locker: select a locked container");
        }

        private void HandleDestroyLockerClick(Vector3 worldPos)
        {
            // Raycast for slots (layer 7), then get parent container
            int hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, raycastHits, 0f, 1 << 7);

            ItemContainer lockedContainer = null;
            for (int i = 0; i < hitCount; i++)
            {
                var slot = raycastHits[i].collider.GetComponent<Slot>();
                if (slot != null)
                {
                    var container = slot.GetComponentInParent<ItemContainer>();
                    if (container != null && container.IsLocked)
                    {
                        lockedContainer = container;
                        break;
                    }
                }
            }

            // Also try raycasting on the container's lock overlay collider area
            if (lockedContainer == null)
            {
                // Try item layer as fallback (containers may have colliders)
                hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, raycastHits, 0f, 1 << 6);
                for (int i = 0; i < hitCount; i++)
                {
                    var container = raycastHits[i].collider.GetComponentInParent<ItemContainer>();
                    if (container != null && container.IsLocked)
                    {
                        lockedContainer = container;
                        break;
                    }
                }
            }

            if (lockedContainer == null) return;

            // Unlock the container
            lockedContainer.Unlock();
            UseCharge(PowerUpType.DestroyLocker);

            AudioManager.Instance?.PlayButtonClick();

            // End interaction
            interactionMode = PowerUpInteractionMode.None;

            // Clear button highlight
            BarUI?.ClearAllHighlights();

            if (DragDropManager.Instance != null)
                DragDropManager.Instance.SetEnabled(true);

            Debug.Log($"[PowerUpManager] Destroy Locker: unlocked container {lockedContainer.ContainerId}");
        }

        #endregion

        #region Move Freeze

        private void ActivateMoveFreeze()
        {
            if (isMovesFrozen) return;

            UseCharge(PowerUpType.MoveFreeze);
            AudioManager.Instance?.PlayButtonClick();

            if (movesFreezeCoroutine != null)
                StopCoroutine(movesFreezeCoroutine);
            movesFreezeCoroutine = StartCoroutine(MovesFreezeCoroutine(5f));

            Debug.Log("[PowerUpManager] Moves Freeze activated for 5 seconds");
        }

        private IEnumerator MovesFreezeCoroutine(float duration)
        {
            isMovesFrozen = true;

            // Play tick-tock during freeze window
            AudioManager.Instance?.StartTickTock();

            yield return new WaitForSeconds(duration);

            isMovesFrozen = false;
            AudioManager.Instance?.StopTickTock();
            movesFreezeCoroutine = null;

            Debug.Log("[PowerUpManager] Moves Freeze expired");
        }

        #endregion

        #region Time Freeze

        private void ActivateTimeFreeze()
        {
            UseCharge(PowerUpType.TimeFreeze);
            AudioManager.Instance?.PlayButtonClick();

            // Use existing LevelManager freeze timer
            LevelManager.Instance?.FreezeTimer(10f);

            Debug.Log("[PowerUpManager] Time Freeze activated for 10 seconds");
        }

        #endregion

        #region Input Handling

        private void HandleSelectionClick()
        {
            Vector2 screenPos = pointerPositionAction.ReadValue<Vector2>();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;

            switch (interactionMode)
            {
                case PowerUpInteractionMode.SelectFirstItem:
                    HandleSwapFirstItemClick(worldPos);
                    break;
                case PowerUpInteractionMode.SelectSecondItem:
                    HandleSwapSecondItemClick(worldPos);
                    break;
                case PowerUpInteractionMode.SelectLockedContainer:
                    HandleDestroyLockerClick(worldPos);
                    break;
            }
        }

        private Item RaycastForFrontRowItem(Vector3 worldPos)
        {
            int hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, raycastHits, 0f, 1 << 6);

            Item closestItem = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var item = raycastHits[i].collider.GetComponent<Item>();
                if (item != null && item.IsInteractive && item.CurrentSlot != null)
                {
                    // Must be front row (row 0) - IsInteractive already ensures this for non-locked containers
                    float dist = Vector3.Distance(worldPos, item.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestItem = item;
                    }
                }
            }

            return closestItem;
        }

        #endregion

        #region Level Lifecycle

        private void OnLevelEnd(int levelNumber, int stars)
        {
            CleanupOnLevelEnd();
        }

        private void OnLevelFailed(int levelNumber, string reason)
        {
            CleanupOnLevelEnd();
        }

        private void OnLevelRestarted()
        {
            CleanupOnLevelEnd();
        }

        private void CleanupOnLevelEnd()
        {
            CancelInteraction();

            if (movesFreezeCoroutine != null)
            {
                StopCoroutine(movesFreezeCoroutine);
                movesFreezeCoroutine = null;
            }
            isMovesFrozen = false;
        }

        #endregion
    }
}
