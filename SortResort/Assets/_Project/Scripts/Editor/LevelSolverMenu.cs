#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace SortResort
{
    /// <summary>
    /// Editor menu for level solver tools
    /// </summary>
    public static class LevelSolverMenu
    {
        [MenuItem("Tools/Sort Resort/Solver/Solve Level...")]
        public static void OpenSolverWindow()
        {
            LevelSolverWindow.ShowWindow();
        }

        [MenuItem("Tools/Sort Resort/Solver/Solve All Island Levels")]
        public static void SolveAllIslandLevels()
        {
            SolveWorldLevels("island", 1, 100);
        }

        [MenuItem("Tools/Sort Resort/Solver/Solve All Supermarket Levels")]
        public static void SolveAllSupermarketLevels()
        {
            SolveWorldLevels("supermarket", 1, 100);
        }

        [MenuItem("Tools/Sort Resort/Solver/Solve All Farm Levels")]
        public static void SolveAllFarmLevels()
        {
            SolveWorldLevels("farm", 1, 100);
        }

        [MenuItem("Tools/Sort Resort/Solver/Solve All Tavern Levels")]
        public static void SolveAllTavernLevels()
        {
            SolveWorldLevels("tavern", 1, 100);
        }

        [MenuItem("Tools/Sort Resort/Solver/Solve All Space Levels")]
        public static void SolveAllSpaceLevels()
        {
            SolveWorldLevels("space", 1, 100);
        }

        [MenuItem("Tools/Sort Resort/Solver/Update All Level Thresholds")]
        public static void UpdateAllLevelThresholds()
        {
            string[] worlds = { "island", "supermarket", "farm", "tavern", "space" };
            var report = new StringBuilder();
            report.AppendLine("=== THRESHOLD UPDATE REPORT ===\n");

            int totalUpdated = 0;
            int totalFailed = 0;

            foreach (var worldId in worlds)
            {
                report.AppendLine($"\n--- {worldId.ToUpper()} ---");

                for (int level = 1; level <= 100; level++)
                {
                    var levelData = LevelDataLoader.LoadLevel(worldId, level);
                    if (levelData == null) continue;

                    var solver = new LevelSolver();
                    solver.VerboseLogging = false;

                    // Progress bar
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Updating Thresholds",
                        $"{worldId} Level {level}",
                        (float)level / 100f))
                    {
                        EditorUtility.ClearProgressBar();
                        report.AppendLine("\n(Cancelled by user)");
                        Debug.Log(report.ToString());
                        return;
                    }

                    var result = solver.SolveLevel(levelData);

                    if (result.Success)
                    {
                        // Calculate new thresholds
                        int optimal = result.TotalMoves;
                        int[] newThresholds = new int[]
                        {
                            optimal,
                            Mathf.RoundToInt(optimal * 1.15f),
                            Mathf.RoundToInt(optimal * 1.30f),
                            Mathf.RoundToInt(optimal * 1.40f)
                        };

                        // Update file
                        string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1);
                        string filePath = $"Assets/_Project/Resources/Data/Levels/{worldFolder}/level_{level:D3}.json";

                        if (File.Exists(filePath))
                        {
                            string json = File.ReadAllText(filePath);
                            var data = JsonUtility.FromJson<LevelData>(json);
                            data.star_move_thresholds = newThresholds;
                            string updatedJson = JsonUtility.ToJson(data, true);
                            File.WriteAllText(filePath, updatedJson);

                            report.AppendLine($"Level {level:D3}: {optimal} moves -> [{newThresholds[0]}, {newThresholds[1]}, {newThresholds[2]}, {newThresholds[3]}]");
                            totalUpdated++;
                        }
                    }
                    else
                    {
                        report.AppendLine($"Level {level:D3}: FAILED - {result.FailureReason}");
                        totalFailed++;
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            report.AppendLine($"\n=== SUMMARY ===");
            report.AppendLine($"Updated: {totalUpdated} levels");
            report.AppendLine($"Failed: {totalFailed} levels");

            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog("Thresholds Updated",
                $"Updated {totalUpdated} levels\nFailed: {totalFailed} levels\n\nSee console for details.",
                "OK");
        }

        private static void SolveWorldLevels(string worldId, int start, int end)
        {
            var report = new StringBuilder();
            report.AppendLine($"=== SOLVER REPORT: {worldId} ===\n");

            int solved = 0;
            int failed = 0;
            int thresholdMismatches = 0;
            bool cancelled = false;

            for (int level = start; level <= end; level++)
            {
                var levelData = LevelDataLoader.LoadLevel(worldId, level);
                if (levelData == null) continue;

                var solver = new LevelSolver();
                solver.VerboseLogging = false;

                // Set up cancelable progress for this level
                solver.OnProgressUpdate = (moves, itemsRemaining, elapsed) =>
                {
                    return EditorUtility.DisplayCancelableProgressBar(
                        $"Solving {worldId} Levels",
                        $"Level {level}: {moves} moves, {itemsRemaining} items, {elapsed:F1}s",
                        (float)(level - start) / (end - start + 1)
                    );
                };

                var result = solver.SolveLevel(levelData);

                if (result.FailureReason != null && result.FailureReason.StartsWith("Cancelled"))
                {
                    cancelled = true;
                    report.AppendLine($"Level {level:D3}: CANCELLED");
                    break;
                }

                if (result.Success)
                {
                    solved++;
                    int current3Star = levelData.ThreeStarThreshold;
                    bool mismatch = result.TotalMoves != current3Star;

                    if (mismatch)
                    {
                        thresholdMismatches++;
                        report.AppendLine($"Level {level:D3}: {result.TotalMoves} moves (3-star threshold: {current3Star}) *** MISMATCH ***");
                    }
                    else
                    {
                        report.AppendLine($"Level {level:D3}: {result.TotalMoves} moves (OK)");
                    }
                }
                else
                {
                    failed++;
                    report.AppendLine($"Level {level:D3}: FAILED - {result.FailureReason}");
                }
            }

            EditorUtility.ClearProgressBar();

            report.AppendLine($"\n=== SUMMARY ===");
            report.AppendLine($"Solved: {solved}");
            report.AppendLine($"Failed: {failed}");
            report.AppendLine($"Threshold mismatches: {thresholdMismatches}");
            if (cancelled) report.AppendLine($"(Cancelled by user)");

            Debug.Log(report.ToString());
        }
    }

    /// <summary>
    /// Editor window for solving individual levels with detailed output
    /// </summary>
    public class LevelSolverWindow : EditorWindow
    {
        private string[] worldOptions = { "island", "supermarket", "farm", "tavern", "space" };
        private int selectedWorldIndex = 0;
        private int levelNumber = 1;

        private LevelSolver.SolveResult lastResult;
        private LevelData lastLevelData;
        private int current3StarThreshold;
        private bool thresholdNeedsUpdate;

        private Vector2 scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<LevelSolverWindow>("Level Solver");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Level Solver", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // World selection
            EditorGUILayout.LabelField("Select Level", EditorStyles.boldLabel);
            selectedWorldIndex = EditorGUILayout.Popup("World", selectedWorldIndex, worldOptions);
            levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);

            EditorGUILayout.Space();

            // Solve button
            if (GUILayout.Button("SOLVE LEVEL", GUILayout.Height(40)))
            {
                SolveLevel();
            }

            EditorGUILayout.Space(20);

            // Results
            if (lastResult != null)
            {
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

                if (lastResult.Success)
                {
                    // Success info
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField($"Status: SOLVED", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Solver Moves: {lastResult.TotalMoves}");
                    EditorGUILayout.LabelField($"Matches: {lastResult.TotalMatches}");
                    EditorGUILayout.LabelField($"Solve Time: {lastResult.SolveTimeMs:F1}ms");

                    EditorGUILayout.Space();

                    // Threshold comparison
                    EditorGUILayout.LabelField("Star Thresholds", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Current 3-star threshold: {current3StarThreshold}");
                    EditorGUILayout.LabelField($"Solver optimal: {lastResult.TotalMoves}");

                    if (thresholdNeedsUpdate)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox(
                            $"Threshold mismatch! Current: {current3StarThreshold}, Solver: {lastResult.TotalMoves}\n" +
                            $"Click 'Update Thresholds' to fix the JSON file.",
                            MessageType.Warning
                        );

                        if (GUILayout.Button("UPDATE THRESHOLDS", GUILayout.Height(30)))
                        {
                            UpdateThresholds();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Thresholds are correct!", MessageType.Info);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();

                    // Move sequence
                    EditorGUILayout.LabelField("Move Sequence", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    for (int i = 0; i < lastResult.MoveSequence.Count; i++)
                    {
                        var move = lastResult.MoveSequence[i];
                        string fromName = lastLevelData.containers[move.FromContainerIndex].id;
                        string toName = lastLevelData.containers[move.ToContainerIndex].id;
                        EditorGUILayout.LabelField($"{i + 1,2}. {move.ItemId,-20} : {fromName}[{move.FromSlot}] -> {toName}[{move.ToSlot}]");
                    }

                    EditorGUILayout.EndVertical();

                    // Copy button
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Copy Move Sequence to Clipboard"))
                    {
                        CopyMoveSequenceToClipboard();
                    }
                }
                else
                {
                    // Failed
                    EditorGUILayout.HelpBox(
                        $"FAILED: {lastResult.FailureReason}\nTime: {lastResult.SolveTimeMs:F1}ms",
                        MessageType.Error
                    );
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void SolveLevel()
        {
            string worldId = worldOptions[selectedWorldIndex];

            lastLevelData = LevelDataLoader.LoadLevel(worldId, levelNumber);
            if (lastLevelData == null)
            {
                EditorUtility.DisplayDialog("Error", $"Level {worldId} {levelNumber} not found!", "OK");
                lastResult = null;
                return;
            }

            current3StarThreshold = lastLevelData.ThreeStarThreshold;

            var solver = new LevelSolver();
            solver.VerboseLogging = false;

            // Set up cancelable progress bar
            solver.OnProgressUpdate = (moves, itemsRemaining, elapsed) =>
            {
                // Show progress bar with Cancel button
                // Returns true if user clicked Cancel
                return EditorUtility.DisplayCancelableProgressBar(
                    "Solving Level",
                    $"Moves: {moves} | Items remaining: {itemsRemaining} | Time: {elapsed:F1}s",
                    itemsRemaining > 0 ? 1f - (itemsRemaining / 30f) : 1f  // Estimate progress
                );
            };

            try
            {
                lastResult = solver.SolveLevel(lastLevelData);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (lastResult.Success)
            {
                thresholdNeedsUpdate = lastResult.TotalMoves != current3StarThreshold;

                Debug.Log($"[Solver] {worldId} Level {levelNumber}: {lastResult.TotalMoves} moves " +
                          $"(threshold: {current3StarThreshold}, match: {!thresholdNeedsUpdate})");
            }
            else
            {
                thresholdNeedsUpdate = false;
                Debug.LogWarning($"[Solver] {worldId} Level {levelNumber}: FAILED - {lastResult.FailureReason}");
            }
        }

        private void UpdateThresholds()
        {
            if (lastResult == null || !lastResult.Success) return;

            string worldId = worldOptions[selectedWorldIndex];
            string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1);
            string filePath = $"Assets/_Project/Resources/Data/Levels/{worldFolder}/level_{levelNumber:D3}.json";

            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("Error", $"File not found: {filePath}", "OK");
                return;
            }

            // Calculate new thresholds based on solver optimal:
            // 3-star: solver moves (optimal)
            // 2-star: solver × 1.15 (rounded)
            // 1-star: solver × 1.30 (rounded)
            // Fail:   solver × 1.40 (rounded)
            int optimal = lastResult.TotalMoves;
            int[] newThresholds = new int[]
            {
                optimal,                              // 3-star = solver's exact score
                Mathf.RoundToInt(optimal * 1.15f),    // 2-star = 15% more moves
                Mathf.RoundToInt(optimal * 1.30f),    // 1-star = 30% more moves
                Mathf.RoundToInt(optimal * 1.40f)     // Fail = 40% more moves
            };

            // Read and update JSON
            string json = File.ReadAllText(filePath);
            var levelData = JsonUtility.FromJson<LevelData>(json);
            levelData.star_move_thresholds = newThresholds;

            // Write back
            string updatedJson = JsonUtility.ToJson(levelData, true);
            File.WriteAllText(filePath, updatedJson);

            AssetDatabase.Refresh();

            // Update local state
            current3StarThreshold = optimal;
            thresholdNeedsUpdate = false;

            Debug.Log($"[Solver] Updated {worldId} Level {levelNumber} thresholds to [{newThresholds[0]}, {newThresholds[1]}, {newThresholds[2]}, {newThresholds[3]}]");

            EditorUtility.DisplayDialog("Success",
                $"Updated thresholds:\n" +
                $"3-star: {newThresholds[0]} moves\n" +
                $"2-star: {newThresholds[1]} moves\n" +
                $"1-star: {newThresholds[2]} moves\n" +
                $"Fail: >{newThresholds[3]} moves",
                "OK");
        }

        private void CopyMoveSequenceToClipboard()
        {
            if (lastResult == null || !lastResult.Success) return;

            var sb = new StringBuilder();
            sb.AppendLine($"=== {worldOptions[selectedWorldIndex]} Level {levelNumber} ===");
            sb.AppendLine($"Total moves: {lastResult.TotalMoves}");
            sb.AppendLine();

            for (int i = 0; i < lastResult.MoveSequence.Count; i++)
            {
                var move = lastResult.MoveSequence[i];
                string fromName = lastLevelData.containers[move.FromContainerIndex].id;
                string toName = lastLevelData.containers[move.ToContainerIndex].id;
                sb.AppendLine($"{i + 1,2}. {move.ItemId,-20} : {fromName}[{move.FromSlot}] -> {toName}[{move.ToSlot}]");
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[Solver] Move sequence copied to clipboard!");
        }
    }
}
#endif
