using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Runs the level solver in-game, executing moves visually on screen.
    /// Attach to a GameObject and call StartAutoSolve() from a button.
    /// </summary>
    public class LevelSolverRunner : MonoBehaviour
    {
        public static LevelSolverRunner Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float moveDelay = 0.5f; // Time between moves
        [SerializeField] private float animationDuration = 0.25f; // Time for item to move

        private bool isSolving = false;
        private Coroutine solveCoroutine;

        public bool IsSolving => isSolving;

        // Events
        public event System.Action OnSolveStarted;
        public event System.Action<bool, int, int, float> OnSolveCompleted; // success, moves, matches, timeMs

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Start auto-solving the current level
        /// </summary>
        public void StartAutoSolve()
        {
            if (isSolving)
            {
                Debug.LogWarning("[LevelSolverRunner] Already solving!");
                return;
            }

            if (LevelManager.Instance?.CurrentLevel == null)
            {
                Debug.LogError("[LevelSolverRunner] No level loaded!");
                return;
            }

            // First, solve the level to get the move sequence
            var solver = new LevelSolver();
            var result = solver.SolveLevel(LevelManager.Instance.CurrentLevel);

            if (!result.Success)
            {
                Debug.LogError($"[LevelSolverRunner] Solver failed: {result.FailureReason}");
                OnSolveCompleted?.Invoke(false, 0, 0, result.SolveTimeMs);
                return;
            }

            Debug.Log($"[LevelSolverRunner] Solution found: {result.TotalMoves} moves, {result.TotalMatches} matches");

            // Now execute the moves visually
            solveCoroutine = StartCoroutine(ExecuteSolution(result));
        }

        /// <summary>
        /// Stop auto-solving
        /// </summary>
        public void StopAutoSolve()
        {
            if (solveCoroutine != null)
            {
                StopCoroutine(solveCoroutine);
                solveCoroutine = null;
            }
            isSolving = false;
        }

        /// <summary>
        /// Execute the solution moves visually
        /// </summary>
        private IEnumerator ExecuteSolution(LevelSolver.SolveResult solution)
        {
            isSolving = true;
            OnSolveStarted?.Invoke();

            var containers = LevelManager.Instance.GetAllContainers();
            int movesMade = 0;
            float startTime = Time.realtimeSinceStartup;

            foreach (var move in solution.MoveSequence)
            {
                // Small delay before next move for readability
                yield return new WaitForSeconds(moveDelay);

                // Find the actual containers and items
                if (move.FromContainerIndex >= containers.Count || move.ToContainerIndex >= containers.Count)
                {
                    Debug.LogError($"[LevelSolverRunner] Invalid container index in move");
                    continue;
                }

                var fromContainer = containers[move.FromContainerIndex];
                var toContainer = containers[move.ToContainerIndex];

                if (fromContainer == null || toContainer == null)
                {
                    Debug.LogWarning($"[LevelSolverRunner] Container destroyed during solve");
                    continue;
                }

                // Get the item to move
                var item = fromContainer.GetItemInSlot(move.FromSlot, 0);
                if (item == null)
                {
                    // Item might have been matched or moved - game state diverged from solver's prediction
                    Debug.LogWarning($"[LevelSolverRunner] Item not found at expected slot. Game state may have diverged.");
                    continue;
                }

                // Verify item matches expected
                if (item.ItemId != move.ItemId)
                {
                    Debug.LogWarning($"[LevelSolverRunner] Item mismatch: expected {move.ItemId}, found {item.ItemId}. Game state may have diverged.");
                }

                // Execute the move visually (includes wait for match processing)
                yield return ExecuteMoveVisual(item, fromContainer, move.FromSlot, toContainer, move.ToSlot);

                movesMade++;
            }

            float totalTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            isSolving = false;

            Debug.Log($"[LevelSolverRunner] Auto-solve complete: {movesMade} moves executed");

            // Show result
            OnSolveCompleted?.Invoke(true, movesMade, solution.TotalMatches, totalTime);
        }

        /// <summary>
        /// Execute a single move with visual animation
        /// </summary>
        private IEnumerator ExecuteMoveVisual(Item item, ItemContainer fromContainer, int fromSlot,
                                               ItemContainer toContainer, int toSlot)
        {
            // Store original scale before any modifications
            Vector3 originalScale = item.transform.localScale;

            // Scale up briefly to highlight the item being moved
            LeanTween.scale(item.gameObject, originalScale * 1.2f, 0.1f);
            yield return new WaitForSeconds(0.15f);

            // Remove from source container (don't trigger row advance yet)
            fromContainer.RemoveItemFromSlot(item, triggerRowAdvance: false);

            // Get current world position before unparenting
            Vector3 startWorldPos = item.transform.position;

            // Unparent for animation
            item.transform.SetParent(null);
            item.transform.position = startWorldPos; // Maintain world position after unparent

            // Get target position in world space
            float itemHeight = GetItemHeight(item);
            Vector3 targetWorldPos = toContainer.GetItemWorldPositionBottomAligned(toSlot, 0, itemHeight);

            // Animate to destination in world space
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / animationDuration);
                item.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
                yield return null;
            }

            // Reset scale BEFORE placing (important for height calculation in PlaceItemInSlot)
            item.transform.localScale = originalScale;

            // Place in destination container - this handles parenting and correct local positioning
            bool placed = toContainer.PlaceItemInSlot(item, toSlot, forcePlace: false);

            if (!placed)
            {
                Debug.LogError($"[LevelSolverRunner] Failed to place item in slot {toSlot}");
            }

            // Play drop sound
            AudioManager.Instance?.PlayDropSound();

            // Increment game move count
            GameManager.Instance?.IncrementMoveCount();

            // Wait for match processing to complete
            // Match detection is immediate, but ProcessMatch uses LeanTween.delayedCall(0.3f)
            // to advance rows, plus 0.2s for row animation
            yield return new WaitForSeconds(0.6f);

            // Now check if source container needs row advancement
            // (this handles the case where removing an item empties the front row)
            fromContainer.CheckAndAdvanceAllRows();
        }

        private float GetItemHeight(Item item)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                return sr.bounds.size.y;
            }
            return 1f;
        }

        private void OnDestroy()
        {
            StopAutoSolve();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
