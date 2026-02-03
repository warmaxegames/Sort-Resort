using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Analyzes level complexity to generate difficulty ratings.
    /// Uses both static analysis (level structure) and dynamic analysis (solver results).
    /// </summary>
    public class LevelComplexityAnalyzer
    {
        /// <summary>
        /// Result of complexity analysis
        /// </summary>
        public class ComplexityResult
        {
            // Raw metrics
            public int ItemTypeCount;
            public int TotalItems;
            public int ContainerCount;
            public int LockedContainerCount;
            public int MaxRowDepth;
            public int MovingContainerCount;
            public int StuckItemsAtStart;
            public int ActionableItemsAtStart;
            public float EmptySpaceRatio;

            // Solver metrics
            public int OptimalMoveCount;
            public int MatchCount;
            public float MovesPerMatch;
            public int TempLocationMoves;  // Moves that put items in non-ideal spots
            public bool SolverSucceeded;
            public float SolveTimeMs;

            // Computed scores
            public int StructureScore;     // From static analysis
            public int SolverScore;        // From dynamic analysis
            public int TotalScore;         // Combined
            public DifficultyRating Rating;

            public override string ToString()
            {
                return $"Complexity: {TotalScore} ({Rating})\n" +
                       $"  Structure: {StructureScore} | Solver: {SolverScore}\n" +
                       $"  Items: {TotalItems} ({ItemTypeCount} types) | Moves: {OptimalMoveCount} | Matches: {MatchCount}\n" +
                       $"  Locked: {LockedContainerCount} | Moving: {MovingContainerCount} | MaxDepth: {MaxRowDepth}\n" +
                       $"  Stuck: {StuckItemsAtStart} | Empty: {EmptySpaceRatio:P0}";
            }
        }

        public enum DifficultyRating
        {
            Tutorial,    // 0-30: Very easy, teaching mechanics
            Easy,        // 31-60: Simple puzzles
            Medium,      // 61-100: Moderate challenge
            Hard,        // 101-150: Requires planning
            Expert,      // 151-200: Complex multi-step solutions
            Master       // 201+: Extremely difficult
        }

        /// <summary>
        /// Analyze a level's complexity
        /// </summary>
        public ComplexityResult Analyze(LevelData levelData)
        {
            var result = new ComplexityResult();

            // === STATIC ANALYSIS ===
            AnalyzeStructure(levelData, result);

            // === DYNAMIC ANALYSIS (run solver) ===
            AnalyzeWithSolver(levelData, result);

            // === COMPUTE SCORES ===
            ComputeScores(result);

            return result;
        }

        private void AnalyzeStructure(LevelData levelData, ComplexityResult result)
        {
            var itemCounts = new Dictionary<string, int>();
            int totalSlots = 0;
            int occupiedSlots = 0;
            int maxDepth = 1;

            foreach (var container in levelData.containers)
            {
                result.ContainerCount++;

                if (container.is_locked)
                    result.LockedContainerCount++;

                if (container.is_moving)
                    result.MovingContainerCount++;

                int slotCount = container.slot_count > 0 ? container.slot_count : 3;
                int rowCount = container.max_rows_per_slot > 0 ? container.max_rows_per_slot : 1;

                totalSlots += slotCount;
                maxDepth = Math.Max(maxDepth, rowCount);

                if (container.initial_items != null)
                {
                    foreach (var item in container.initial_items)
                    {
                        result.TotalItems++;
                        occupiedSlots++;

                        if (!itemCounts.ContainsKey(item.id))
                            itemCounts[item.id] = 0;
                        itemCounts[item.id]++;

                        // Check if item is in back row (stuck)
                        if (item.row > 0 || container.is_locked)
                        {
                            result.StuckItemsAtStart++;
                        }
                        else
                        {
                            result.ActionableItemsAtStart++;
                        }
                    }
                }
            }

            result.ItemTypeCount = itemCounts.Count;
            result.MaxRowDepth = maxDepth;
            result.EmptySpaceRatio = totalSlots > 0 ? 1f - ((float)occupiedSlots / totalSlots) : 0f;
            result.MatchCount = result.TotalItems / 3; // Assuming triple matches
        }

        private void AnalyzeWithSolver(LevelData levelData, ComplexityResult result)
        {
            var solver = new LevelSolver();
            solver.VerboseLogging = false;

            var solveResult = solver.SolveLevel(levelData);

            result.SolverSucceeded = solveResult.Success;
            result.OptimalMoveCount = solveResult.TotalMoves;
            result.SolveTimeMs = solveResult.SolveTimeMs;

            if (solveResult.Success && result.MatchCount > 0)
            {
                result.MovesPerMatch = (float)solveResult.TotalMoves / result.MatchCount;
            }

            // Count "temp location" moves by analyzing move sequence
            // A move to a container with no matching items (when not creating a match) is likely temporary
            if (solveResult.Success && solveResult.MoveSequence != null)
            {
                result.TempLocationMoves = CountTempLocationMoves(levelData, solveResult.MoveSequence);
            }
        }

        private int CountTempLocationMoves(LevelData levelData, List<LevelSolver.Move> moves)
        {
            // Simplified counting: moves where item goes to container without matching items
            // and doesn't immediately result in a match
            int tempMoves = 0;

            // Build initial state to track
            var containerItems = new Dictionary<int, List<string>>();
            for (int i = 0; i < levelData.containers.Count; i++)
            {
                containerItems[i] = new List<string>();
                var container = levelData.containers[i];
                if (container.initial_items != null)
                {
                    foreach (var item in container.initial_items)
                    {
                        if (item.row == 0) // Only front row for simplicity
                            containerItems[i].Add(item.id);
                    }
                }
            }

            foreach (var move in moves)
            {
                // Check if destination has matching items
                var destItems = containerItems.ContainsKey(move.ToContainerIndex)
                    ? containerItems[move.ToContainerIndex]
                    : new List<string>();

                int matchingCount = destItems.Count(i => i == move.ItemId);

                // If no matching items at destination and won't complete match, it's temporary
                if (matchingCount == 0)
                {
                    tempMoves++;
                }

                // Update tracking (simplified - doesn't handle matches/row advancement)
                if (containerItems.ContainsKey(move.FromContainerIndex))
                    containerItems[move.FromContainerIndex].Remove(move.ItemId);
                if (!containerItems.ContainsKey(move.ToContainerIndex))
                    containerItems[move.ToContainerIndex] = new List<string>();
                containerItems[move.ToContainerIndex].Add(move.ItemId);
            }

            return tempMoves;
        }

        private void ComputeScores(ComplexityResult result)
        {
            // === STRUCTURE SCORE ===
            int structureScore = 0;

            // Item complexity
            structureScore += result.ItemTypeCount * 5;
            structureScore += result.TotalItems * 1;

            // Container complexity
            structureScore += result.LockedContainerCount * 15;
            structureScore += result.MovingContainerCount * 20;

            // Depth complexity
            structureScore += (result.MaxRowDepth - 1) * 10;

            // Accessibility
            structureScore += result.StuckItemsAtStart * 4;

            // Maneuvering room (negative = easier)
            structureScore -= (int)(result.EmptySpaceRatio * 30);

            result.StructureScore = Math.Max(0, structureScore);

            // === SOLVER SCORE ===
            int solverScore = 0;

            if (result.SolverSucceeded)
            {
                // Move efficiency
                solverScore += result.OptimalMoveCount * 2;

                // Temp location moves indicate tricky solutions
                solverScore += result.TempLocationMoves * 5;

                // High moves-per-match ratio indicates complex setups
                if (result.MovesPerMatch > 2.5f)
                    solverScore += 10;
                if (result.MovesPerMatch > 3.0f)
                    solverScore += 15;
            }
            else
            {
                // Unsolvable or very hard
                solverScore += 100;
            }

            result.SolverScore = solverScore;

            // === TOTAL ===
            result.TotalScore = result.StructureScore + result.SolverScore;

            // === RATING ===
            result.Rating = GetRating(result.TotalScore);
        }

        private DifficultyRating GetRating(int score)
        {
            if (score <= 30) return DifficultyRating.Tutorial;
            if (score <= 60) return DifficultyRating.Easy;
            if (score <= 100) return DifficultyRating.Medium;
            if (score <= 150) return DifficultyRating.Hard;
            if (score <= 200) return DifficultyRating.Expert;
            return DifficultyRating.Master;
        }

        /// <summary>
        /// Analyze all levels in a world and return sorted by complexity
        /// </summary>
        public List<(int levelNum, ComplexityResult result)> AnalyzeWorld(string worldId, int maxLevel = 100)
        {
            var results = new List<(int levelNum, ComplexityResult result)>();

            for (int i = 1; i <= maxLevel; i++)
            {
                var levelData = LevelDataLoader.LoadLevel(worldId, i);
                if (levelData == null) break;

                var result = Analyze(levelData);
                results.Add((i, result));
            }

            return results.OrderBy(r => r.result.TotalScore).ToList();
        }

        /// <summary>
        /// Print complexity analysis for a level
        /// </summary>
        public static void PrintAnalysis(string worldId, int levelNum)
        {
            var levelData = LevelDataLoader.LoadLevel(worldId, levelNum);
            if (levelData == null)
            {
                Debug.Log($"Level {worldId}/{levelNum} not found");
                return;
            }

            var analyzer = new LevelComplexityAnalyzer();
            var result = analyzer.Analyze(levelData);

            Debug.Log($"\n=== COMPLEXITY ANALYSIS: {worldId} Level {levelNum} ===\n{result}");
        }

        /// <summary>
        /// Print complexity analysis for all levels in a world
        /// </summary>
        public static void PrintWorldAnalysis(string worldId)
        {
            var analyzer = new LevelComplexityAnalyzer();
            var results = analyzer.AnalyzeWorld(worldId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"\n=== WORLD COMPLEXITY ANALYSIS: {worldId.ToUpper()} ===");
            sb.AppendLine($"{"Level",-8} {"Score",-8} {"Rating",-10} {"Moves",-8} {"Items",-8} {"Types",-8} {"Locked",-8}");
            sb.AppendLine(new string('-', 60));

            foreach (var (levelNum, result) in results)
            {
                sb.AppendLine($"{levelNum,-8} {result.TotalScore,-8} {result.Rating,-10} {result.OptimalMoveCount,-8} {result.TotalItems,-8} {result.ItemTypeCount,-8} {result.LockedContainerCount,-8}");
            }

            Debug.Log(sb.ToString());
        }
    }
}
