using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Solves Sort Resort levels using a greedy algorithm that prioritizes
    /// matches requiring the fewest moves.
    /// </summary>
    public class LevelSolver
    {
        // Enable verbose logging for debugging
        public bool VerboseLogging { get; set; } = true;

        private void Log(string message)
        {
            if (VerboseLogging)
            {
                Debug.Log($"[Solver] {message}");
            }
        }

        #region Data Structures

        /// <summary>
        /// Represents the state of a container during solving
        /// </summary>
        public class ContainerState
        {
            public string Id;
            public int SlotCount;
            public int MaxRows;
            public bool IsLocked;
            public int UnlockMatchesRequired;
            public int CurrentUnlockProgress;

            // slots[slotIndex][rowIndex] = itemId (null if empty)
            public List<List<string>> Slots;

            public ContainerState Clone()
            {
                var clone = new ContainerState
                {
                    Id = Id,
                    SlotCount = SlotCount,
                    MaxRows = MaxRows,
                    IsLocked = IsLocked,
                    UnlockMatchesRequired = UnlockMatchesRequired,
                    CurrentUnlockProgress = CurrentUnlockProgress,
                    Slots = new List<List<string>>()
                };

                foreach (var slot in Slots)
                {
                    clone.Slots.Add(new List<string>(slot));
                }

                return clone;
            }

            public string GetFrontItem(int slotIndex)
            {
                if (slotIndex < 0 || slotIndex >= Slots.Count) return null;
                return Slots[slotIndex][0];
            }

            public bool IsFrontSlotEmpty(int slotIndex)
            {
                return GetFrontItem(slotIndex) == null;
            }

            public int GetEmptyFrontSlotCount()
            {
                int count = 0;
                for (int i = 0; i < SlotCount; i++)
                {
                    if (IsFrontSlotEmpty(i)) count++;
                }
                return count;
            }

            public List<string> GetFrontRowItems()
            {
                var items = new List<string>();
                for (int i = 0; i < SlotCount; i++)
                {
                    var item = GetFrontItem(i);
                    if (item != null) items.Add(item);
                }
                return items;
            }

            public bool HasBackRowItems()
            {
                foreach (var slot in Slots)
                {
                    for (int r = 1; r < slot.Count; r++)
                    {
                        if (slot[r] != null) return true;
                    }
                }
                return false;
            }

            public int GetBackRowItemCount()
            {
                int count = 0;
                foreach (var slot in Slots)
                {
                    for (int r = 1; r < slot.Count; r++)
                    {
                        if (slot[r] != null) count++;
                    }
                }
                return count;
            }

            public List<string> GetBackRowItemTypes()
            {
                var items = new List<string>();
                foreach (var slot in Slots)
                {
                    for (int r = 1; r < slot.Count; r++)
                    {
                        if (slot[r] != null) items.Add(slot[r]);
                    }
                }
                return items;
            }

            public bool IsEmpty()
            {
                foreach (var slot in Slots)
                {
                    foreach (var item in slot)
                    {
                        if (item != null) return false;
                    }
                }
                return true;
            }

            public int GetTotalItemCount()
            {
                int count = 0;
                foreach (var slot in Slots)
                {
                    foreach (var item in slot)
                    {
                        if (item != null) count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Represents the full game state during solving
        /// </summary>
        public class GameState
        {
            public List<ContainerState> Containers;
            public int MoveCount;
            public int MatchCount;

            public GameState Clone()
            {
                return new GameState
                {
                    Containers = Containers.Select(c => c.Clone()).ToList(),
                    MoveCount = MoveCount,
                    MatchCount = MatchCount
                };
            }

            public int GetTotalItemCount()
            {
                return Containers.Sum(c => c.GetTotalItemCount());
            }

            public bool IsComplete()
            {
                return GetTotalItemCount() == 0;
            }
        }

        /// <summary>
        /// Represents a single move: item from one slot to another
        /// </summary>
        public struct Move
        {
            public int FromContainerIndex;
            public int FromSlot;
            public int ToContainerIndex;
            public int ToSlot;
            public string ItemId;

            public override string ToString()
            {
                return $"Move {ItemId} from Container[{FromContainerIndex}].Slot[{FromSlot}] to Container[{ToContainerIndex}].Slot[{ToSlot}]";
            }
        }

        /// <summary>
        /// Result of solving a level
        /// </summary>
        public class SolveResult
        {
            public bool Success;
            public int TotalMoves;
            public int TotalMatches;
            public List<Move> MoveSequence;
            public string FailureReason;
            public float SolveTimeMs;

            public override string ToString()
            {
                if (Success)
                    return $"SOLVED in {TotalMoves} moves, {TotalMatches} matches ({SolveTimeMs:F1}ms)";
                else
                    return $"FAILED: {FailureReason} ({SolveTimeMs:F1}ms)";
            }
        }

        #endregion

        #region Solving

        private const int MAX_MOVES = 500; // Prevent infinite loops

        // Allow external cancellation via callback
        // Returns true if solver should stop, false to continue
        // Parameters: (currentMoves, itemsRemaining, elapsedSeconds)
        public Func<int, int, float, bool> OnProgressUpdate { get; set; } = null;

        // Item accessibility classification
        private enum ItemAccessibility
        {
            Accessible,      // In front row of unlocked container
            NearlyAccessible, // Will become accessible after 1 row advance or 1 unlock
            Stuck            // Deeply buried or behind multiple locks
        }

        /// <summary>
        /// Solve a level from JSON data
        /// </summary>
        public SolveResult SolveLevel(LevelData levelData)
        {
            var startTime = DateTime.Now;
            var result = new SolveResult
            {
                MoveSequence = new List<Move>()
            };

            Log($"=== Starting solve for {levelData.name} ===");

            // Initialize game state from level data
            var state = InitializeState(levelData);

            if (state == null)
            {
                result.FailureReason = "Failed to initialize state";
                result.SolveTimeMs = (float)(DateTime.Now - startTime).TotalMilliseconds;
                return result;
            }

            // Log initial state
            LogState(state, "Initial state");

            // Process any immediate matches at start
            int initialMatches = ProcessAllMatches(state);
            if (initialMatches > 0)
            {
                Log($"Processed {initialMatches} immediate match(es) at start");
            }

            // Main solving loop
            int iteration = 0;
            Move? lastMove = null; // Track last move to prevent oscillation
            var recentMoves = new List<Move>(); // Track recent moves for pattern detection
            const int PATTERN_WINDOW = 10; // Look for patterns in last N moves
            while (!state.IsComplete() && state.MoveCount < MAX_MOVES)
            {
                // Check for cancellation via progress callback
                if (OnProgressUpdate != null)
                {
                    float elapsed = (float)(DateTime.Now - startTime).TotalSeconds;
                    bool shouldCancel = OnProgressUpdate(state.MoveCount, state.GetTotalItemCount(), elapsed);
                    if (shouldCancel)
                    {
                        Log($"CANCELLED by user after {elapsed:F1}s");
                        result.FailureReason = $"Cancelled ({state.MoveCount} moves, {state.GetTotalItemCount()} items remaining)";
                        result.TotalMoves = state.MoveCount;
                        result.TotalMatches = state.MatchCount;
                        result.SolveTimeMs = elapsed * 1000f;
                        return result;
                    }
                }

                iteration++;
                Log($"--- Iteration {iteration}: {state.GetTotalItemCount()} items remaining ---");

                var bestMove = FindBestMove(state, lastMove, recentMoves);

                if (bestMove == null)
                {
                    Log("STUCK: No valid moves found!");
                    LogState(state, "Final stuck state");
                    result.FailureReason = $"No valid moves found. {state.GetTotalItemCount()} items remaining.";
                    result.TotalMoves = state.MoveCount;
                    result.TotalMatches = state.MatchCount;
                    result.SolveTimeMs = (float)(DateTime.Now - startTime).TotalMilliseconds;
                    return result;
                }

                // Execute the move
                Log($"Executing: {bestMove.Value}");
                ExecuteMove(state, bestMove.Value);
                result.MoveSequence.Add(bestMove.Value);
                lastMove = bestMove.Value; // Track for oscillation prevention

                // Track recent moves for pattern detection
                recentMoves.Add(bestMove.Value);
                if (recentMoves.Count > PATTERN_WINDOW)
                {
                    recentMoves.RemoveAt(0);
                }

                // Process matches
                int newMatches = ProcessAllMatches(state);
                if (newMatches > 0)
                {
                    Log($"Match! {newMatches} match(es) made. Total matches: {state.MatchCount}");
                    lastMove = null; // Reset after match - new board state, oscillation less likely
                    recentMoves.Clear(); // Clear pattern history after match
                }
            }

            if (state.IsComplete())
            {
                Log($"=== SOLVED in {state.MoveCount} moves, {state.MatchCount} matches ===");
                result.Success = true;
                result.TotalMoves = state.MoveCount;
                result.TotalMatches = state.MatchCount;

                // Print detailed move sequence for analysis
                PrintMoveSequence(result.MoveSequence, levelData);
            }
            else
            {
                Log("FAILED: Max moves exceeded");
                result.FailureReason = "Max moves exceeded";
                result.TotalMoves = state.MoveCount;
                result.TotalMatches = state.MatchCount;

                // Print move sequence even on failure for debugging
                PrintMoveSequence(result.MoveSequence, levelData, failed: true);
            }

            result.SolveTimeMs = (float)(DateTime.Now - startTime).TotalMilliseconds;
            return result;
        }

        /// <summary>
        /// Print a detailed move sequence for comparing with manual solutions
        /// </summary>
        private void PrintMoveSequence(List<Move> moves, LevelData levelData, bool failed = false)
        {
            // Get container names for readability
            var containerNames = new Dictionary<int, string>();
            for (int i = 0; i < levelData.containers.Count; i++)
            {
                containerNames[i] = levelData.containers[i].id;
            }

            // Build entire output as single string for easy copying
            var sb = new System.Text.StringBuilder();
            if (failed)
            {
                sb.AppendLine("\n=== SOLVER MOVE SEQUENCE (FAILED - Max moves exceeded) ===");
                sb.AppendLine($"Moves before failure: {moves.Count}\n");
            }
            else
            {
                sb.AppendLine("\n=== SOLVER MOVE SEQUENCE ===");
                sb.AppendLine($"Total moves: {moves.Count}\n");
            }

            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                string fromName = containerNames.ContainsKey(move.FromContainerIndex)
                    ? containerNames[move.FromContainerIndex]
                    : $"Container[{move.FromContainerIndex}]";
                string toName = containerNames.ContainsKey(move.ToContainerIndex)
                    ? containerNames[move.ToContainerIndex]
                    : $"Container[{move.ToContainerIndex}]";

                sb.AppendLine($"  {i + 1,2}. {move.ItemId,-16} : {fromName}[slot {move.FromSlot}] -> {toName}[slot {move.ToSlot}]");
            }

            sb.AppendLine("\n=== END SOLVER SEQUENCE ===");

            // Single log call - easy to copy from Unity console
            Debug.Log(sb.ToString());
        }

        private void LogState(GameState state, string label)
        {
            if (!VerboseLogging) return;

            Log($"{label}:");
            foreach (var container in state.Containers)
            {
                string lockStr = container.IsLocked ? $" [LOCKED {container.CurrentUnlockProgress}/{container.UnlockMatchesRequired}]" : "";
                var frontItems = container.GetFrontRowItems();
                string itemsStr = frontItems.Count > 0 ? string.Join(", ", frontItems) : "(empty)";
                int backCount = 0;
                foreach (var slot in container.Slots)
                {
                    for (int r = 1; r < slot.Count; r++)
                    {
                        if (slot[r] != null) backCount++;
                    }
                }
                string backStr = backCount > 0 ? $" (+{backCount} back)" : "";
                Log($"  {container.Id}{lockStr}: {itemsStr}{backStr}");
            }
        }

        /// <summary>
        /// Initialize game state from level data
        /// </summary>
        private GameState InitializeState(LevelData levelData)
        {
            var state = new GameState
            {
                Containers = new List<ContainerState>(),
                MoveCount = 0,
                MatchCount = 0
            };

            foreach (var containerDef in levelData.containers)
            {
                var container = new ContainerState
                {
                    Id = containerDef.id,
                    SlotCount = containerDef.slot_count > 0 ? containerDef.slot_count : 3,
                    MaxRows = containerDef.max_rows_per_slot > 0 ? containerDef.max_rows_per_slot : 4,
                    IsLocked = containerDef.is_locked,
                    UnlockMatchesRequired = containerDef.unlock_matches_required,
                    CurrentUnlockProgress = 0,
                    Slots = new List<List<string>>()
                };

                // Initialize empty slots
                for (int s = 0; s < container.SlotCount; s++)
                {
                    var slot = new List<string>();
                    for (int r = 0; r < container.MaxRows; r++)
                    {
                        slot.Add(null);
                    }
                    container.Slots.Add(slot);
                }

                // Place initial items
                if (containerDef.initial_items != null)
                {
                    foreach (var item in containerDef.initial_items)
                    {
                        if (item.slot >= 0 && item.slot < container.SlotCount &&
                            item.row >= 0 && item.row < container.MaxRows)
                        {
                            container.Slots[item.slot][item.row] = item.id;
                        }
                    }
                }

                state.Containers.Add(container);
            }

            return state;
        }

        /// <summary>
        /// Find the best move using improved strategy:
        /// 1. Always take 1-move matches (always optimal)
        /// 2. Otherwise, score ALL candidate moves and pick the best
        /// </summary>
        private Move? FindBestMove(GameState state, Move? lastMove = null, List<Move> recentMoves = null)
        {
            Log("Searching for best move...");

            // RULE 1: Always take 1-move matches - they're always optimal
            var oneMoveMatch = FindOneMoveMatch(state);
            if (oneMoveMatch != null)
            {
                Log($"Taking 1-move match: {oneMoveMatch.Value}");
                return oneMoveMatch;
            }

            // RULE 2: Analyze which items are "actionable" vs "stuck"
            var itemStatus = AnalyzeItemAccessibility(state);
            LogItemStatus(itemStatus);

            // RULE 3: Get all valid moves, but prioritize actionable items
            var allMoves = GetAllValidMoves(state);
            if (allMoves.Count == 0)
            {
                Log("No valid moves available!");
                return null;
            }

            // Build set of recent move patterns to detect oscillation
            var recentMoveSet = new HashSet<string>();
            if (recentMoves != null)
            {
                foreach (var rm in recentMoves)
                {
                    // Track both the move and its reverse
                    recentMoveSet.Add($"{rm.ItemId}:{rm.FromContainerIndex}->{rm.ToContainerIndex}");
                }
            }

            // RULE 4: Score all moves on unified scale and pick the best
            var scoredMoves = new List<(Move move, int score, string reason)>();

            foreach (var move in allMoves)
            {
                var (score, reason) = ScoreMoveUnified(state, move, itemStatus);

                // RULE 5: Heavily penalize moves that reverse the last move (prevents oscillation)
                if (lastMove.HasValue && IsReversalMove(move, lastMove.Value))
                {
                    score -= 1000;
                    reason += ", REVERSAL PENALTY";
                }

                // RULE 6: Penalize moves that are part of a recent oscillation pattern
                string moveKey = $"{move.ItemId}:{move.FromContainerIndex}->{move.ToContainerIndex}";
                string reverseMoveKey = $"{move.ItemId}:{move.ToContainerIndex}->{move.FromContainerIndex}";
                if (recentMoveSet.Contains(reverseMoveKey))
                {
                    // This move reverses a recent move - likely part of oscillation
                    score -= 500;
                    reason += ", PATTERN PENALTY";
                }

                scoredMoves.Add((move, score, reason));
            }

            // Sort by score descending
            scoredMoves.Sort((a, b) => b.score.CompareTo(a.score));

            // Log top candidates
            Log($"Top move candidates:");
            for (int i = 0; i < Math.Min(5, scoredMoves.Count); i++)
            {
                var (move, score, reason) = scoredMoves[i];
                Log($"  {i + 1}. {move.ItemId}: {state.Containers[move.FromContainerIndex].Id}[{move.FromSlot}] -> {state.Containers[move.ToContainerIndex].Id}[{move.ToSlot}] (score: {score}, {reason})");
            }

            var best = scoredMoves[0];
            Log($"Selected: {best.move} (score: {best.score})");
            return best.move;
        }

        /// <summary>
        /// Analyze accessibility of each item type
        /// Returns dict of itemId -> (accessible count, nearly accessible count, total count)
        /// </summary>
        private Dictionary<string, (int accessible, int nearlyAccessible, int total)> AnalyzeItemAccessibility(GameState state)
        {
            var result = new Dictionary<string, (int accessible, int nearlyAccessible, int total)>();

            // Count containers that are close to unlocking (1 match away)
            var nearUnlockContainers = new HashSet<int>();
            for (int ci = 0; ci < state.Containers.Count; ci++)
            {
                var c = state.Containers[ci];
                if (c.IsLocked && c.UnlockMatchesRequired - c.CurrentUnlockProgress <= 1)
                {
                    nearUnlockContainers.Add(ci);
                }
            }

            // Check which containers have nearly-empty front rows (could advance soon)
            var nearAdvanceContainers = new HashSet<int>();
            for (int ci = 0; ci < state.Containers.Count; ci++)
            {
                var c = state.Containers[ci];
                if (c.IsLocked) continue;
                int occupiedFront = 0;
                for (int s = 0; s < c.SlotCount; s++)
                {
                    if (!c.IsFrontSlotEmpty(s)) occupiedFront++;
                }
                // If only 1 item in front row, it could be moved to trigger row advance
                if (occupiedFront <= 1 && c.HasBackRowItems())
                {
                    nearAdvanceContainers.Add(ci);
                }
            }

            // Now classify each item
            for (int ci = 0; ci < state.Containers.Count; ci++)
            {
                var container = state.Containers[ci];

                for (int s = 0; s < container.SlotCount; s++)
                {
                    for (int r = 0; r < container.Slots[s].Count; r++)
                    {
                        var itemId = container.Slots[s][r];
                        if (itemId == null) continue;

                        if (!result.ContainsKey(itemId))
                        {
                            result[itemId] = (0, 0, 0);
                        }

                        var current = result[itemId];
                        current.total++;

                        if (r == 0 && !container.IsLocked)
                        {
                            // Front row of unlocked container = accessible
                            current.accessible++;
                        }
                        else if (r == 0 && nearUnlockContainers.Contains(ci))
                        {
                            // Front row of container about to unlock = nearly accessible
                            current.nearlyAccessible++;
                        }
                        else if (r == 1 && nearAdvanceContainers.Contains(ci))
                        {
                            // Row 1 of container about to advance = nearly accessible
                            current.nearlyAccessible++;
                        }
                        // Else: stuck (deeply buried)

                        result[itemId] = current;
                    }
                }
            }

            return result;
        }

        private void LogItemStatus(Dictionary<string, (int accessible, int nearlyAccessible, int total)> itemStatus)
        {
            if (!VerboseLogging) return;

            Log("Item accessibility:");
            foreach (var kvp in itemStatus)
            {
                var (acc, near, total) = kvp.Value;
                string status = (acc + near >= 2) ? "ACTIONABLE" : "stuck";
                Log($"  {kvp.Key}: {acc} accessible, {near} nearly, {total} total [{status}]");
            }
        }

        /// <summary>
        /// Unified move scoring - considers all factors on one scale
        /// </summary>
        private (int score, string reason) ScoreMoveUnified(GameState state, Move move,
            Dictionary<string, (int accessible, int nearlyAccessible, int total)> itemStatus)
        {
            int score = 0;
            var reasons = new List<string>();

            var fromContainer = state.Containers[move.FromContainerIndex];
            var toContainer = state.Containers[move.ToContainerIndex];

            // Get item's actionability
            var (accessible, nearlyAccessible, total) = itemStatus.ContainsKey(move.ItemId)
                ? itemStatus[move.ItemId]
                : (0, 0, 0);
            bool isActionable = (accessible + nearlyAccessible) >= 2;

            // Get destination info (needed for multiple scoring factors)
            var destItems = toContainer.GetFrontRowItems();
            int matchingAtDest = destItems.Count(i => i == move.ItemId);

            // === MATCH-ENABLING BONUSES ===

            // Simulate the move and check what it enables
            var testState = state.Clone();
            ExecuteMove(testState, move);

            // Check for immediate match
            if (WouldMatch(testState))
            {
                score += 200;
                reasons.Add("creates match");
            }
            else
            {
                // Check if enables 1-move match
                bool oldVerbose = VerboseLogging;
                VerboseLogging = false;
                var followUp = FindOneMoveMatch(testState);
                VerboseLogging = oldVerbose;

                if (followUp != null)
                {
                    // Enables a match, but how good is THIS move for the item being moved?
                    // If we're just "getting out of the way" without benefit, give less bonus
                    if (matchingAtDest >= 1)
                    {
                        // Great - enables match AND creates pair
                        score += 120;
                        reasons.Add("enables match + creates pair");
                    }
                    else if (destItems.Count == 0)
                    {
                        // OK - enables match and goes to empty container (staging)
                        score += 80;
                        reasons.Add("enables match (to empty)");
                    }
                    else
                    {
                        // Poor - enables match but item goes to bad location (mixing)
                        // This item will likely need to move again later
                        score += 40;
                        reasons.Add("enables match (temp location)");
                    }
                }
            }

            // === PAIRING BONUS (if not already counted in enables-match section) ===
            bool alreadyCreditedPair = reasons.Contains("enables match + creates pair");

            if (matchingAtDest == 1 && !alreadyCreditedPair)
            {
                // Check if 3rd item of this type is accessible BEFORE giving pair bonus
                bool thirdIsAccessible = accessible >= 3; // All 3 copies are accessible

                // Check if destination will have room for the 3rd item after this move
                int emptyAtDest = toContainer.GetEmptyFrontSlotCount();
                bool hasRoomForThird = emptyAtDest >= 2; // After this move fills 1 slot, still has 1+ empty

                if (thirdIsAccessible && hasRoomForThird)
                {
                    // EXCELLENT PAIR: 3rd item accessible AND room to complete triple
                    score += 180;
                    reasons.Add("creates completable pair");
                }
                else if (thirdIsAccessible && !hasRoomForThird)
                {
                    // BLOCKED PAIR: 3rd is accessible but no room - need extra move to clear space
                    score -= 50;
                    reasons.Add("creates BLOCKED pair (no room for 3rd)");
                }
                else if (!thirdIsAccessible && hasRoomForThird)
                {
                    // WAITING PAIR: 3rd hidden but room exists - OK but low priority
                    score += 20;
                    reasons.Add("creates waiting pair (3rd hidden)");

                    // PENALTY if this pair blocks back row items at destination
                    if (toContainer.HasBackRowItems())
                    {
                        score -= 80;
                        reasons.Add("pair blocks reveals");
                    }
                }
                else
                {
                    // WORST PAIR: 3rd hidden AND no room - very bad
                    score -= 100;
                    reasons.Add("creates useless pair (hidden + blocked)");
                }
            }
            else if (matchingAtDest == 0 && destItems.Count > 0 && !reasons.Contains("enables match (temp location)"))
            {
                // Only penalize mixing if we didn't already note it as "temp location"
                score -= 10;
                reasons.Add("mixes items");
            }

            // === ACTIONABILITY BONUS/PENALTY ===
            if (isActionable)
            {
                score += 30;
                reasons.Add("actionable item");
            }
            else
            {
                // Moving a stuck item - only valuable if it enables something useful
                bool hasUsefulEffect = reasons.Contains("creates match") ||
                                       reasons.Any(r => r.StartsWith("enables match")) ||
                                       reasons.Contains("creates pair");
                if (!hasUsefulEffect)
                {
                    score -= 40;
                    reasons.Add("stuck item shuffle");
                }
            }

            // === ROW ADVANCEMENT BONUS ===
            // If this move will clear front row and advance back items
            int fromOccupiedCount = 0;
            for (int s = 0; s < fromContainer.SlotCount; s++)
            {
                if (!fromContainer.IsFrontSlotEmpty(s)) fromOccupiedCount++;
            }

            if (fromOccupiedCount == 1 && fromContainer.HasBackRowItems())
            {
                // IMMEDIATE REVEAL: This move clears the front row
                var revealedItems = GetItemsThatWouldAdvance(fromContainer);
                int revealCount = revealedItems.Count;
                score += 100 + (revealCount * 25);
                reasons.Add($"triggers row advance ({revealCount} items)");

                // Extra bonus if revealed item completes a waiting pair (2 matching + empty slot)
                foreach (var revealedItem in revealedItems)
                {
                    if (HasWaitingPairForItem(state, revealedItem))
                    {
                        score += 80;
                        reasons.Add($"reveals {revealedItem} for waiting pair");
                        break; // Only count bonus once
                    }
                }

                // Extra bonus if this move ALSO creates a good pair (combo move)
                if (matchingAtDest == 1 && accessible >= 3)
                {
                    score += 50;
                    reasons.Add("combo: pair + reveal");
                }
            }
            else if (fromContainer.HasBackRowItems())
            {
                // PROGRESS TOWARD REVEAL: Source container has back-row items
                // Moving items out brings us closer to revealing them
                int backRowCount = fromContainer.GetBackRowItemCount();
                int progressBonus = 30 + (backRowCount * 10);
                score += progressBonus;
                reasons.Add($"progress toward reveal ({backRowCount} hidden)");
            }

            // === DESTINATION QUALITY ===
            int destEmptySlots = toContainer.GetEmptyFrontSlotCount();
            if (destEmptySlots <= 1)
            {
                score -= 15;
                reasons.Add("fills container");
            }

            // === STAGING MOVE BONUS ===
            // When moving to empty container (staging), it's neutral unless it also reveals items
            if (destItems.Count == 0 && matchingAtDest == 0)
            {
                // This is a staging move - check if it at least triggers row advance
                if (fromOccupiedCount == 1 && fromContainer.HasBackRowItems())
                {
                    // Good staging - reveals items while staging
                    score += 20;
                    reasons.Add("productive staging");
                }
                else
                {
                    // Pure staging with no reveal - not great but sometimes necessary
                    score -= 5;
                    reasons.Add("staging move");
                }
            }

            // === MATCH-IN-PLACE CONSIDERATION ===
            // If source container has room and this item is actionable, prefer keeping it there
            // BUT not if we're making a good pair or triggering reveals
            int fromEmptySlots = fromContainer.GetEmptyFrontSlotCount();
            bool makingGoodPair = matchingAtDest == 1 && accessible >= 3;
            bool triggeringReveal = fromOccupiedCount == 1 && fromContainer.HasBackRowItems();
            if (fromEmptySlots >= 2 && isActionable && matchingAtDest == 0 && !triggeringReveal)
            {
                // We're evacuating an actionable item to a non-pairing destination
                // This might disrupt a potential match-in-place
                score -= 35;
                reasons.Add("disrupts match-in-place potential");
            }

            // === MATCH-AT-REVEALING-CONTAINER BONUS ===
            // Prefer matching at containers with hidden back-row items because:
            // 1. It reveals those items (making them accessible)
            // 2. It empties the container for potential reuse as a collection point
            // NOTE: Only give this bonus if the match will actually complete (not pairs with hidden 3rd)
            bool willCompleteTriple = matchingAtDest >= 2 || (matchingAtDest == 1 && accessible >= 3);
            if (toContainer.HasBackRowItems() && matchingAtDest >= 1 && willCompleteTriple)
            {
                // Count how many items are hidden in the destination
                int hiddenCount = toContainer.GetBackRowItemCount();

                // Bonus scales with how many items we'll reveal
                int revealBonus = 50 + (hiddenCount * 20);
                score += revealBonus;
                reasons.Add($"triple reveals {hiddenCount} hidden item(s)");

                // Check if hidden items are different types that could use this container
                var hiddenItems = toContainer.GetBackRowItemTypes();
                var uniqueHiddenTypes = hiddenItems.Where(h => h != move.ItemId).Distinct().ToList();
                if (uniqueHiddenTypes.Count > 0)
                {
                    // After matching, container is empty and reveals items of other types
                    // This container becomes a collection point for those types
                    score += 30;
                    reasons.Add("clears container for revealed items");
                }
            }

            string reason = reasons.Count > 0 ? string.Join(", ", reasons) : "neutral";
            return (score, reason);
        }

        /// <summary>
        /// Find a move that immediately results in a match (1-move match)
        /// Condition: Container has 2 matching items + 1 empty slot, and a 3rd matching item exists elsewhere
        /// IMPROVED: When multiple 1-move matches exist, prefer ones that reveal hidden items
        /// </summary>
        private Move? FindOneMoveMatch(GameState state)
        {
            Log("  Searching for 1-move matches...");

            // Collect ALL possible 1-move matches, then pick the best one
            var candidates = new List<(Move move, int revealScore, string reason)>();

            // For each unlocked container with at least 1 empty front slot
            for (int ci = 0; ci < state.Containers.Count; ci++)
            {
                var container = state.Containers[ci];
                if (container.IsLocked) continue;
                if (container.SlotCount < 3) continue;

                int emptySlot = -1;
                for (int s = 0; s < container.SlotCount; s++)
                {
                    if (container.IsFrontSlotEmpty(s))
                    {
                        emptySlot = s;
                        break;
                    }
                }

                if (emptySlot == -1) continue;

                // Get front row items
                var frontItems = container.GetFrontRowItems();
                if (frontItems.Count < 2) continue;

                // Check if we have 2 of the same item
                var itemCounts = new Dictionary<string, int>();
                foreach (var item in frontItems)
                {
                    if (!itemCounts.ContainsKey(item)) itemCounts[item] = 0;
                    itemCounts[item]++;
                }

                foreach (var kvp in itemCounts)
                {
                    if (kvp.Value >= 2)
                    {
                        // Found 2 matching items, look for a 3rd elsewhere
                        string targetItem = kvp.Key;

                        for (int oci = 0; oci < state.Containers.Count; oci++)
                        {
                            if (oci == ci) continue;
                            var otherContainer = state.Containers[oci];
                            if (otherContainer.IsLocked) continue;

                            for (int os = 0; os < otherContainer.SlotCount; os++)
                            {
                                var item = otherContainer.GetFrontItem(os);
                                if (item == targetItem)
                                {
                                    // Found a valid 1-move match!
                                    var move = new Move
                                    {
                                        FromContainerIndex = oci,
                                        FromSlot = os,
                                        ToContainerIndex = ci,
                                        ToSlot = emptySlot,
                                        ItemId = targetItem
                                    };

                                    // Score this move based on what it reveals
                                    int revealScore = 0;
                                    var reasons = new List<string>();

                                    // Check if moving FROM this container triggers row advance
                                    int fromOccupied = 0;
                                    for (int s = 0; s < otherContainer.SlotCount; s++)
                                    {
                                        if (!otherContainer.IsFrontSlotEmpty(s)) fromOccupied++;
                                    }
                                    if (fromOccupied == 1 && otherContainer.HasBackRowItems())
                                    {
                                        // This move will reveal items!
                                        var revealedItems = GetItemsThatWouldAdvance(otherContainer);
                                        revealScore += 100 + (revealedItems.Count * 20);
                                        reasons.Add($"reveals {revealedItems.Count} from source");

                                        // Extra bonus if revealed item has a waiting pair
                                        foreach (var revealed in revealedItems)
                                        {
                                            if (HasWaitingPairForItem(state, revealed))
                                            {
                                                revealScore += 50;
                                                reasons.Add($"reveals {revealed} for waiting pair");
                                            }
                                        }
                                    }

                                    // Check if completing triple at destination reveals items
                                    if (container.HasBackRowItems())
                                    {
                                        var destRevealed = GetItemsThatWouldAdvance(container);
                                        revealScore += 50 + (destRevealed.Count * 15);
                                        reasons.Add($"triple reveals {destRevealed.Count} at dest");
                                    }

                                    string reason = reasons.Count > 0 ? string.Join(", ", reasons) : "basic triple";
                                    candidates.Add((move, revealScore, reason));
                                }
                            }
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                Log("  No 1-move matches available");
                return null;
            }

            // Sort by reveal score descending and pick the best
            candidates.Sort((a, b) => b.revealScore.CompareTo(a.revealScore));

            var best = candidates[0];
            Log($"  Found {candidates.Count} 1-move match(es). Best: {best.move.ItemId} (score: {best.revealScore}, {best.reason})");

            return best.move;
        }

        /// <summary>
        /// Get all valid moves in the current state
        /// </summary>
        private List<Move> GetAllValidMoves(GameState state)
        {
            var moves = new List<Move>();

            // Find all accessible items (front row of unlocked containers)
            for (int fromCi = 0; fromCi < state.Containers.Count; fromCi++)
            {
                var fromContainer = state.Containers[fromCi];
                if (fromContainer.IsLocked) continue;

                for (int fromSlot = 0; fromSlot < fromContainer.SlotCount; fromSlot++)
                {
                    var item = fromContainer.GetFrontItem(fromSlot);
                    if (item == null) continue;

                    // Find all valid destinations
                    for (int toCi = 0; toCi < state.Containers.Count; toCi++)
                    {
                        if (toCi == fromCi) continue;

                        var toContainer = state.Containers[toCi];
                        if (toContainer.IsLocked) continue;

                        for (int toSlot = 0; toSlot < toContainer.SlotCount; toSlot++)
                        {
                            if (toContainer.IsFrontSlotEmpty(toSlot))
                            {
                                moves.Add(new Move
                                {
                                    FromContainerIndex = fromCi,
                                    FromSlot = fromSlot,
                                    ToContainerIndex = toCi,
                                    ToSlot = toSlot,
                                    ItemId = item
                                });
                            }
                        }
                    }
                }
            }

            return moves;
        }

        /// <summary>
        /// Execute a move on the game state
        /// </summary>
        private void ExecuteMove(GameState state, Move move)
        {
            var fromContainer = state.Containers[move.FromContainerIndex];
            var toContainer = state.Containers[move.ToContainerIndex];

            // Remove from source
            fromContainer.Slots[move.FromSlot][0] = null;

            // Add to destination
            toContainer.Slots[move.ToSlot][0] = move.ItemId;

            state.MoveCount++;

            // Check for row advancement in source container
            CheckAndAdvanceRows(fromContainer);
        }

        /// <summary>
        /// Check if all front slots are empty and advance back rows if so
        /// </summary>
        private void CheckAndAdvanceRows(ContainerState container)
        {
            // Check if ALL front slots are empty
            bool allEmpty = true;
            for (int s = 0; s < container.SlotCount; s++)
            {
                if (!container.IsFrontSlotEmpty(s))
                {
                    allEmpty = false;
                    break;
                }
            }

            if (!allEmpty) return;

            // Check if there are back items to advance
            if (!container.HasBackRowItems()) return;

            // Advance all rows forward
            for (int s = 0; s < container.SlotCount; s++)
            {
                // Find first non-null row
                int firstNonNull = -1;
                for (int r = 1; r < container.Slots[s].Count; r++)
                {
                    if (container.Slots[s][r] != null)
                    {
                        firstNonNull = r;
                        break;
                    }
                }

                if (firstNonNull > 0)
                {
                    // Shift all items forward
                    for (int r = firstNonNull; r < container.Slots[s].Count; r++)
                    {
                        container.Slots[s][r - firstNonNull] = container.Slots[s][r];
                        if (r >= firstNonNull)
                            container.Slots[s][r] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Process all matches in all containers
        /// Returns the number of matches processed
        /// </summary>
        private int ProcessAllMatches(GameState state)
        {
            int totalMatches = 0;
            bool matchFound;
            do
            {
                matchFound = false;

                for (int ci = 0; ci < state.Containers.Count; ci++)
                {
                    var container = state.Containers[ci];
                    if (container.SlotCount < 3) continue;

                    if (ProcessContainerMatch(state, container))
                    {
                        matchFound = true;
                        totalMatches++;
                    }
                }
            } while (matchFound);

            return totalMatches;
        }

        /// <summary>
        /// Check and process a match in a container
        /// </summary>
        private bool ProcessContainerMatch(GameState state, ContainerState container)
        {
            // Get all front row items
            var frontItems = new List<string>();
            for (int s = 0; s < container.SlotCount; s++)
            {
                frontItems.Add(container.GetFrontItem(s));
            }

            // Check if all slots are filled and all match
            bool allFilled = frontItems.All(i => i != null);
            if (!allFilled) return false;

            bool allMatch = frontItems.Distinct().Count() == 1;
            if (!allMatch) return false;

            // Match found! Clear the items
            for (int s = 0; s < container.SlotCount; s++)
            {
                container.Slots[s][0] = null;
            }

            state.MatchCount++;

            // Unlock progress for locked containers
            foreach (var c in state.Containers)
            {
                if (c.IsLocked)
                {
                    c.CurrentUnlockProgress++;
                    if (c.CurrentUnlockProgress >= c.UnlockMatchesRequired)
                    {
                        c.IsLocked = false;
                    }
                }
            }

            // Advance rows
            CheckAndAdvanceRows(container);

            return true;
        }

        /// <summary>
        /// Check if any container would match in current state
        /// </summary>
        private bool WouldMatch(GameState state)
        {
            foreach (var container in state.Containers)
            {
                if (container.SlotCount < 3) continue;

                var frontItems = new List<string>();
                for (int s = 0; s < container.SlotCount; s++)
                {
                    frontItems.Add(container.GetFrontItem(s));
                }

                bool allFilled = frontItems.All(i => i != null);
                if (!allFilled) continue;

                bool allMatch = frontItems.Distinct().Count() == 1;
                if (allMatch) return true;
            }

            return false;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if a move reverses the previous move (same item, swapped containers)
        /// </summary>
        private bool IsReversalMove(Move current, Move previous)
        {
            // A reversal is when we move the same item back to where it came from
            return current.ItemId == previous.ItemId &&
                   current.FromContainerIndex == previous.ToContainerIndex &&
                   current.ToContainerIndex == previous.FromContainerIndex;
        }

        /// <summary>
        /// Get the items that would advance to front row if all front slots were empty
        /// </summary>
        private List<string> GetItemsThatWouldAdvance(ContainerState container)
        {
            var items = new List<string>();
            for (int s = 0; s < container.SlotCount; s++)
            {
                // Find the first non-null item starting from row 1
                for (int r = 1; r < container.Slots[s].Count; r++)
                {
                    if (container.Slots[s][r] != null)
                    {
                        items.Add(container.Slots[s][r]);
                        break; // Only first non-null per slot advances
                    }
                }
            }
            return items;
        }

        /// <summary>
        /// Check if there's a "waiting pair" for an item type:
        /// A container with exactly 2 of this item and at least 1 empty slot
        /// </summary>
        private bool HasWaitingPairForItem(GameState state, string itemId)
        {
            for (int ci = 0; ci < state.Containers.Count; ci++)
            {
                var container = state.Containers[ci];
                if (container.IsLocked) continue;
                if (container.SlotCount < 3) continue;

                int matchCount = 0;
                int emptyCount = 0;
                for (int s = 0; s < container.SlotCount; s++)
                {
                    var frontItem = container.GetFrontItem(s);
                    if (frontItem == itemId) matchCount++;
                    else if (frontItem == null) emptyCount++;
                }

                // Waiting pair: 2 matching items + at least 1 empty slot for the 3rd
                if (matchCount == 2 && emptyCount >= 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Print the current state for debugging
        /// </summary>
        public static void DebugPrintState(GameState state)
        {
            Debug.Log($"=== Game State: {state.MoveCount} moves, {state.MatchCount} matches, {state.GetTotalItemCount()} items remaining ===");

            foreach (var container in state.Containers)
            {
                string lockStr = container.IsLocked ? $" [LOCKED {container.CurrentUnlockProgress}/{container.UnlockMatchesRequired}]" : "";
                Debug.Log($"Container {container.Id}{lockStr}:");

                for (int s = 0; s < container.SlotCount; s++)
                {
                    string slotStr = $"  Slot {s}: ";
                    for (int r = 0; r < container.Slots[s].Count; r++)
                    {
                        var item = container.Slots[s][r];
                        slotStr += item != null ? $"[{item}]" : "[---]";
                        slotStr += " ";
                    }
                    Debug.Log(slotStr);
                }
            }
        }

        #endregion
    }
}
