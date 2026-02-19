using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Static utility class to run the level solver on levels from the editor or at runtime.
    /// Usage: Call LevelSolverTests.TestAllLevels() from the console or a custom editor menu.
    /// </summary>
    public static class LevelSolverTests
    {
        private static readonly string[] WorldIds = { "island", "supermarket", "farm", "tavern", "space" };

        /// <summary>
        /// Test all levels across all worlds
        /// </summary>
        [UnityEngine.ContextMenu("Test All Levels")]
        public static void TestAllLevels()
        {
            Debug.Log("=== LEVEL SOLVER: Testing All Levels ===");

            var solver = new LevelSolver();
            solver.VerboseLogging = false; // Disable verbose for batch testing

            int totalLevels = 0;
            int solvedLevels = 0;
            int failedLevels = 0;
            float totalTime = 0f;
            var failedList = new List<string>();

            foreach (var worldId in WorldIds)
            {
                Debug.Log($"\n--- Testing {worldId} ---");

                for (int levelNum = 1; levelNum <= 100; levelNum++)
                {
                    var levelData = LevelDataLoader.LoadLevel(worldId, levelNum);
                    if (levelData == null)
                    {
                        // No more levels in this world
                        break;
                    }

                    totalLevels++;
                    var result = solver.SolveLevelBest(levelData);
                    totalTime += result.SolveTimeMs;

                    if (result.Success)
                    {
                        solvedLevels++;
                        Debug.Log($"  {worldId} level {levelNum}: SOLVED in {result.TotalMoves} moves, {result.TotalMatches} matches ({result.SolveTimeMs:F1}ms)");
                    }
                    else
                    {
                        failedLevels++;
                        failedList.Add($"{worldId}/{levelNum}");
                        Debug.LogWarning($"  {worldId} level {levelNum}: FAILED - {result.FailureReason}");
                    }
                }
            }

            Debug.Log($"\n=== SUMMARY ===");
            Debug.Log($"Total levels tested: {totalLevels}");
            Debug.Log($"Solved: {solvedLevels} ({(float)solvedLevels / totalLevels * 100:F1}%)");
            Debug.Log($"Failed: {failedLevels}");
            Debug.Log($"Total solve time: {totalTime:F1}ms");
            Debug.Log($"Avg time per level: {totalTime / totalLevels:F2}ms");

            if (failedList.Count > 0)
            {
                Debug.LogWarning($"Failed levels: {string.Join(", ", failedList)}");
            }
        }

        /// <summary>
        /// Test a specific level with verbose output
        /// </summary>
        public static void TestLevel(string worldId, int levelNumber)
        {
            Debug.Log($"=== Testing {worldId} level {levelNumber} ===");

            var levelData = LevelDataLoader.LoadLevel(worldId, levelNumber);
            if (levelData == null)
            {
                Debug.LogError($"Level not found: {worldId}/{levelNumber}");
                return;
            }

            var solver = new LevelSolver();
            solver.VerboseLogging = true; // Enable verbose for single level

            var result = solver.SolveLevelBest(levelData);

            Debug.Log($"\n=== RESULT: {result} ===");

            if (result.Success)
            {
                Debug.Log("Move sequence:");
                for (int i = 0; i < result.MoveSequence.Count; i++)
                {
                    Debug.Log($"  {i + 1}. {result.MoveSequence[i]}");
                }
            }
        }

        /// <summary>
        /// Test all levels in a specific world
        /// </summary>
        public static void TestWorld(string worldId)
        {
            Debug.Log($"=== Testing world: {worldId} ===");

            var solver = new LevelSolver();
            solver.VerboseLogging = false;

            int solvedCount = 0;
            int totalCount = 0;

            for (int levelNum = 1; levelNum <= 100; levelNum++)
            {
                var levelData = LevelDataLoader.LoadLevel(worldId, levelNum);
                if (levelData == null) break;

                totalCount++;
                var result = solver.SolveLevelBest(levelData);

                if (result.Success)
                {
                    solvedCount++;
                    Debug.Log($"Level {levelNum}: {result.TotalMoves} moves ({result.SolveTimeMs:F1}ms)");
                }
                else
                {
                    Debug.LogWarning($"Level {levelNum}: FAILED - {result.FailureReason}");
                }
            }

            Debug.Log($"\n{worldId}: {solvedCount}/{totalCount} levels solved");
        }
    }
}
