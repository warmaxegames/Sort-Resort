#if UNITY_EDITOR
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public static void SolveAllIslandLevels() => SolveWorldLevels("island", 1, 100);

        [MenuItem("Tools/Sort Resort/Solver/Solve All Supermarket Levels")]
        public static void SolveAllSupermarketLevels() => SolveWorldLevels("supermarket", 1, 100);

        [MenuItem("Tools/Sort Resort/Solver/Solve All Farm Levels")]
        public static void SolveAllFarmLevels() => SolveWorldLevels("farm", 1, 100);

        [MenuItem("Tools/Sort Resort/Solver/Solve All Tavern Levels")]
        public static void SolveAllTavernLevels() => SolveWorldLevels("tavern", 1, 100);

        [MenuItem("Tools/Sort Resort/Solver/Solve All Space Levels")]
        public static void SolveAllSpaceLevels() => SolveWorldLevels("space", 1, 100);

        [MenuItem("Tools/Sort Resort/Solver/Update All Level Thresholds (Parallel)")]
        public static void UpdateAllLevelThresholds()
        {
            ParallelThresholdRunner.Start(changedOnly: false);
        }

        [MenuItem("Tools/Sort Resort/Solver/Update Changed Level Thresholds Only (Parallel)")]
        public static void UpdateChangedLevelThresholds()
        {
            ParallelThresholdRunner.Start(changedOnly: true);
        }

        private static void SolveWorldLevels(string worldId, int start, int end)
        {
            var report = new StringBuilder();
            report.AppendLine($"=== SOLVER REPORT: {worldId} ===\n");

            int solved = 0;
            int failed = 0;
            int skipped = 0;
            int thresholdMismatches = 0;
            bool cancelled = false;

            for (int level = start; level <= end; level++)
            {
                var levelData = LevelDataLoader.LoadLevel(worldId, level);
                if (levelData == null)
                {
                    skipped++;
                    report.AppendLine($"Level {level:D3}: SKIPPED - file not found or failed to load");
                    continue;
                }

                var solver = new LevelSolver();
                solver.VerboseLogging = false;

                solver.OnProgressUpdate = (moves, itemsRemaining, elapsed) =>
                {
                    return EditorUtility.DisplayCancelableProgressBar(
                        $"Solving {worldId} Levels",
                        $"Level {level}: {moves} moves, {itemsRemaining} items, {elapsed:F1}s",
                        (float)(level - start) / (end - start + 1)
                    );
                };

                var result = solver.SolveLevelBest(levelData);

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
                    Debug.LogWarning($"[Solver] {worldId} Level {level}: FAILED - {result.FailureReason}");
                }
            }

            EditorUtility.ClearProgressBar();

            report.AppendLine($"\n=== SUMMARY ===");
            report.AppendLine($"Solved: {solved}");
            report.AppendLine($"Failed: {failed}");
            report.AppendLine($"Skipped (not found): {skipped}");
            report.AppendLine($"Threshold mismatches: {thresholdMismatches}");
            if (cancelled) report.AppendLine($"(Cancelled by user)");

            if (failed > 0)
                Debug.LogError(report.ToString());
            else if (skipped > 0)
                Debug.LogWarning(report.ToString());
            else
                Debug.Log(report.ToString());

            EditorUtility.DisplayDialog($"Solver: {worldId}",
                $"Solved: {solved}\nFailed: {failed}\nSkipped (not found): {skipped}\nThreshold mismatches: {thresholdMismatches}" +
                (cancelled ? "\n(Cancelled by user)" : "") +
                "\n\nSee console for full report.",
                "OK");
        }
    }

    /// <summary>
    /// Runs parallel threshold updates using background threads with a responsive progress bar.
    /// Levels are solved on worker threads; the main thread polls for completion via EditorApplication.update.
    /// </summary>
    public static class ParallelThresholdRunner
    {
        private struct LevelEntry
        {
            public string WorldId;
            public int Level;
            public LevelData Data;
            public string FilePath;
        }

        private struct ThresholdResult
        {
            public string WorldId;
            public int Level;
            public bool Success;
            public int SolverMoves;
            public int[] NewThresholds;
            public string FailureReason;
        }

        private static List<LevelEntry> _entries;
        private static ConcurrentBag<ThresholdResult> _results;
        private static int _completedCount;
        private static int _totalCount;
        private static int _skippedCount;
        private static bool _running;
        private static Task _solveTask;
        private static System.Diagnostics.Stopwatch _stopwatch;

        public static void Start(bool changedOnly)
        {
            if (_running)
            {
                EditorUtility.DisplayDialog("Already Running", "A parallel threshold update is already in progress.", "OK");
                return;
            }

            string[] worlds = { "island", "supermarket", "farm", "tavern", "space" };

            // Load all level data on main thread
            EditorUtility.DisplayProgressBar("Loading Levels", "Loading all level data...", 0f);

            _entries = new List<LevelEntry>();
            _skippedCount = 0;

            foreach (var worldId in worlds)
            {
                string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1);
                for (int level = 1; level <= 100; level++)
                {
                    var levelData = LevelDataLoader.LoadLevel(worldId, level);
                    if (levelData == null) continue;

                    string filePath = $"Assets/_Project/Resources/Data/Levels/{worldFolder}/level_{level:D3}.json";

                    if (changedOnly && levelData.construction_moves > 0 &&
                        levelData.star_move_thresholds != null &&
                        levelData.star_move_thresholds.Length >= 1 &&
                        levelData.star_move_thresholds[0] == levelData.construction_moves)
                    {
                        _skippedCount++;
                        continue;
                    }

                    _entries.Add(new LevelEntry
                    {
                        WorldId = worldId,
                        Level = level,
                        Data = levelData,
                        FilePath = filePath
                    });
                }
            }

            _totalCount = _entries.Count;
            _completedCount = 0;
            _results = new ConcurrentBag<ThresholdResult>();

            if (_totalCount == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("No Levels to Update",
                    changedOnly ? $"All levels already have matching thresholds.\n({_skippedCount} skipped)" : "No levels found.",
                    "OK");
                return;
            }

            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _running = true;

            // Launch background solving
            int maxThreads = Mathf.Max(2, System.Environment.ProcessorCount - 1);
            Debug.Log($"[Solver] Starting parallel threshold update: {_totalCount} levels on {maxThreads} threads" +
                      (_skippedCount > 0 ? $" ({_skippedCount} skipped)" : ""));

            _solveTask = Task.Run(() =>
            {
                Parallel.ForEach(_entries,
                    new ParallelOptions { MaxDegreeOfParallelism = maxThreads },
                    entry =>
                    {
                        var solver = new LevelSolver();
                        solver.VerboseLogging = false;
                        var result = solver.SolveLevelBest(entry.Data);

                        if (result.Success)
                        {
                            int optimal = result.TotalMoves;
                            int[] thresholds = new int[]
                            {
                                optimal,
                                Mathf.RoundToInt(optimal * 1.15f),
                                Mathf.RoundToInt(optimal * 1.30f),
                                Mathf.RoundToInt(optimal * 1.40f)
                            };
                            for (int i = 1; i < thresholds.Length; i++)
                            {
                                if (thresholds[i] <= thresholds[i - 1])
                                    thresholds[i] = thresholds[i - 1] + 1;
                            }
                            _results.Add(new ThresholdResult
                            {
                                WorldId = entry.WorldId, Level = entry.Level,
                                Success = true, SolverMoves = optimal, NewThresholds = thresholds
                            });
                        }
                        else
                        {
                            _results.Add(new ThresholdResult
                            {
                                WorldId = entry.WorldId, Level = entry.Level,
                                Success = false, FailureReason = result.FailureReason
                            });
                        }

                        Interlocked.Increment(ref _completedCount);
                    }
                );
            });

            // Register update callback to poll progress
            EditorApplication.update += PollProgress;
        }

        private static void PollProgress()
        {
            if (!_running) return;

            int completed = _completedCount;
            float progress = (float)completed / _totalCount;
            float elapsed = (float)_stopwatch.Elapsed.TotalSeconds;
            float estimatedTotal = completed > 0 ? elapsed / progress : 0f;
            float remaining = estimatedTotal - elapsed;

            string timeStr = remaining > 0
                ? $" ~{remaining:F0}s remaining"
                : "";

            bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                "Solving Levels (Parallel)",
                $"{completed}/{_totalCount} levels solved ({elapsed:F0}s elapsed{timeStr})",
                progress
            );

            if (cancelled)
            {
                // Can't easily cancel Parallel.ForEach, but clear UI
                EditorUtility.ClearProgressBar();
                EditorApplication.update -= PollProgress;
                _running = false;
                Debug.LogWarning("[Solver] Cancelled by user. Background threads may still be running briefly.");
                return;
            }

            if (_solveTask.IsCompleted)
            {
                EditorApplication.update -= PollProgress;
                _running = false;
                _stopwatch.Stop();
                OnComplete();
            }
        }

        private static void OnComplete()
        {
            EditorUtility.DisplayProgressBar("Writing Results", "Updating JSON files...", 0.95f);

            var report = new StringBuilder();
            report.AppendLine("=== THRESHOLD UPDATE REPORT ===\n");

            int totalUpdated = 0;
            int totalFailed = 0;

            var sortedResults = _results.ToList();
            // Sort: worlds alphabetically, then by level number
            sortedResults.Sort((a, b) =>
            {
                int wCmp = string.Compare(a.WorldId, b.WorldId, System.StringComparison.Ordinal);
                return wCmp != 0 ? wCmp : a.Level.CompareTo(b.Level);
            });

            string currentWorld = "";
            foreach (var r in sortedResults)
            {
                if (r.WorldId != currentWorld)
                {
                    currentWorld = r.WorldId;
                    report.AppendLine($"\n--- {currentWorld.ToUpper()} ---");
                }

                if (r.Success)
                {
                    // Find the file path for this level
                    var entry = _entries.FirstOrDefault(e => e.WorldId == r.WorldId && e.Level == r.Level);
                    if (File.Exists(entry.FilePath))
                    {
                        string json = File.ReadAllText(entry.FilePath);
                        var data = JsonUtility.FromJson<LevelData>(json);
                        data.star_move_thresholds = r.NewThresholds;
                        string updatedJson = JsonUtility.ToJson(data, true);
                        File.WriteAllText(entry.FilePath, updatedJson);
                    }

                    report.AppendLine($"Level {r.Level:D3}: {r.SolverMoves} moves -> [{r.NewThresholds[0]}, {r.NewThresholds[1]}, {r.NewThresholds[2]}, {r.NewThresholds[3]}]");
                    totalUpdated++;
                }
                else
                {
                    report.AppendLine($"Level {r.Level:D3}: FAILED - {r.FailureReason}");
                    totalFailed++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            report.AppendLine($"\n=== SUMMARY ===");
            report.AppendLine($"Updated: {totalUpdated} levels");
            report.AppendLine($"Failed: {totalFailed} levels");
            if (_skippedCount > 0)
                report.AppendLine($"Skipped (unchanged): {_skippedCount} levels");
            report.AppendLine($"Time: {_stopwatch.Elapsed.TotalSeconds:F1}s");

            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog("Thresholds Updated",
                $"Updated {totalUpdated} levels\nFailed: {totalFailed} levels" +
                (_skippedCount > 0 ? $"\nSkipped (unchanged): {_skippedCount}" : "") +
                $"\nTime: {_stopwatch.Elapsed.TotalSeconds:F1}s" +
                "\n\nSee console for details.",
                "OK");
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

            EditorGUILayout.LabelField("Select Level", EditorStyles.boldLabel);
            selectedWorldIndex = EditorGUILayout.Popup("World", selectedWorldIndex, worldOptions);
            levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);

            EditorGUILayout.Space();

            if (GUILayout.Button("SOLVE LEVEL", GUILayout.Height(40)))
            {
                SolveLevel();
            }

            EditorGUILayout.Space(20);

            if (lastResult != null)
            {
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

                if (lastResult.Success)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField($"Status: SOLVED", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Solver Moves: {lastResult.TotalMoves}");
                    EditorGUILayout.LabelField($"Matches: {lastResult.TotalMatches}");
                    EditorGUILayout.LabelField($"Solve Time: {lastResult.SolveTimeMs:F1}ms");

                    EditorGUILayout.Space();

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

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Copy Move Sequence to Clipboard"))
                    {
                        CopyMoveSequenceToClipboard();
                    }
                }
                else
                {
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

            solver.OnProgressUpdate = (moves, itemsRemaining, elapsed) =>
            {
                return EditorUtility.DisplayCancelableProgressBar(
                    "Solving Level",
                    $"Moves: {moves} | Items remaining: {itemsRemaining} | Time: {elapsed:F1}s",
                    itemsRemaining > 0 ? 1f - (itemsRemaining / 30f) : 1f
                );
            };

            try
            {
                lastResult = solver.SolveLevelBest(lastLevelData);
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

            int optimal = lastResult.TotalMoves;
            int[] newThresholds = new int[]
            {
                optimal,
                Mathf.RoundToInt(optimal * 1.15f),
                Mathf.RoundToInt(optimal * 1.30f),
                Mathf.RoundToInt(optimal * 1.40f)
            };

            for (int i = 1; i < newThresholds.Length; i++)
            {
                if (newThresholds[i] <= newThresholds[i - 1])
                    newThresholds[i] = newThresholds[i - 1] + 1;
            }

            string json = File.ReadAllText(filePath);
            var levelData = JsonUtility.FromJson<LevelData>(json);
            levelData.star_move_thresholds = newThresholds;

            string updatedJson = JsonUtility.ToJson(levelData, true);
            File.WriteAllText(filePath, updatedJson);

            AssetDatabase.Refresh();

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
