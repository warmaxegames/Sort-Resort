#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SortResort
{
    /// <summary>
    /// Editor menu for level generation tools
    /// </summary>
    public static class LevelGeneratorMenu
    {
        private const string LEVELS_PATH = "Assets/_Project/Resources/Data/Levels";

        #region Single Level Generation

        [MenuItem("Tools/Sort Resort/Generator/Generate Single Level...")]
        public static void GenerateSingleLevelDialog()
        {
            LevelGeneratorWindow.ShowWindow();
        }

        #endregion

        #region Batch Generation

        [MenuItem("Tools/Sort Resort/Generator/Generate Island Levels 1-10")]
        public static void GenerateIslandLevels1to10()
        {
            GenerateLevelRange("island", 1, 10);
        }

        [MenuItem("Tools/Sort Resort/Generator/Generate Island Levels 11-25")]
        public static void GenerateIslandLevels11to25()
        {
            GenerateLevelRange("island", 11, 25);
        }

        [MenuItem("Tools/Sort Resort/Generator/Generate Island Levels 26-45")]
        public static void GenerateIslandLevels26to45()
        {
            GenerateLevelRange("island", 26, 45);
        }

        [MenuItem("Tools/Sort Resort/Generator/Generate Island Levels 46-65")]
        public static void GenerateIslandLevels46to65()
        {
            GenerateLevelRange("island", 46, 65);
        }

        [MenuItem("Tools/Sort Resort/Generator/Generate Island Levels 66-85")]
        public static void GenerateIslandLevels66to85()
        {
            GenerateLevelRange("island", 66, 85);
        }

        [MenuItem("Tools/Sort Resort/Generator/Generate Island Levels 86-100")]
        public static void GenerateIslandLevels86to100()
        {
            GenerateLevelRange("island", 86, 100);
        }

        [MenuItem("Tools/Sort Resort/Generator/Generate ALL Island Levels (1-100)")]
        public static void GenerateAllIslandLevels()
        {
            if (!EditorUtility.DisplayDialog("Generate All Levels",
                "This will generate 100 levels for the Island world.\nExisting levels will be overwritten.\n\nThis may take several minutes. Continue?",
                "Generate", "Cancel"))
            {
                return;
            }

            GenerateLevelRange("island", 1, 100);
        }

        #endregion

        #region Utilities

        [MenuItem("Tools/Sort Resort/Generator/Preview Level Parameters")]
        public static void PreviewLevelParameters()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== LEVEL PARAMETERS PREVIEW ===\n");

            int[] sampleLevels = { 1, 5, 10, 15, 20, 25, 30, 40, 50, 60, 70, 80, 90, 100 };

            sb.AppendLine("Lvl | Std | Sng | Dpth | Lock | Train | Items | Timer");
            sb.AppendLine("----|-----|-----|------|------|-------|-------|------");

            foreach (int level in sampleLevels)
            {
                var p = LevelGenerator.GetParametersForLevel("island", level);
                sb.AppendLine($"{level,3} | {p.standardContainerCount,3} | {p.singleSlotContainerCount,3} | {p.singleSlotDepth,4} | {p.lockedContainerCount,4} | {p.trainCount,5} | {p.itemTypeCount,5} | {p.secondsPerItem:F1}s");
            }

            sb.AppendLine("\n=== TIMER SCALING ===");
            sb.AppendLine("Levels 1-10:   4.0s per item");
            sb.AppendLine("Levels 11-20:  3.8s per item");
            sb.AppendLine("Levels 21-30:  3.6s per item");
            sb.AppendLine("Levels 31-40:  3.4s per item");
            sb.AppendLine("Levels 41-50:  3.2s per item");
            sb.AppendLine("Levels 51-60:  3.0s per item");
            sb.AppendLine("Levels 61-70:  2.8s per item");
            sb.AppendLine("Levels 71-80:  2.6s per item");
            sb.AppendLine("Levels 81-90:  2.4s per item");
            sb.AppendLine("Levels 91-100: 2.0s per item");

            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Sort Resort/Generator/Validate Existing Levels")]
        public static void ValidateExistingLevels()
        {
            ValidateLevelRange("island", 1, 100);
        }

        #endregion

        #region Implementation

        private static void GenerateLevelRange(string worldId, int start, int end)
        {
            string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1);
            string outputPath = $"{LEVELS_PATH}/{worldFolder}";

            // Ensure directory exists
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var generator = new LevelGenerator();
            int successCount = 0;
            int failCount = 0;

            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== LEVEL GENERATION REPORT: {worldId} {start}-{end} ===\n");

            for (int level = start; level <= end; level++)
            {
                EditorUtility.DisplayProgressBar(
                    $"Generating {worldId} Levels",
                    $"Level {level}/{end}",
                    (float)(level - start) / (end - start + 1)
                );

                var parameters = LevelGenerator.GetParametersForLevel(worldId, level);
                var result = generator.GenerateLevel(parameters);

                if (result.Success)
                {
                    // Save level to file
                    string json = JsonUtility.ToJson(result.LevelData, true);
                    string filePath = $"{outputPath}/level_{level:D3}.json";
                    File.WriteAllText(filePath, json);

                    report.AppendLine($"Level {level:D3}: SUCCESS");
                    report.AppendLine($"  Solver: {result.SolveResult.TotalMoves} moves in {result.SolveResult.SolveTimeMs:F0}ms");
                    report.AppendLine($"  Stars: [{result.StarThresholds[0]}, {result.StarThresholds[1]}, {result.StarThresholds[2]}]");
                    report.AppendLine($"  Items: {result.TotalItems}, Timer: {result.TimeLimitSeconds}s");
                    report.AppendLine($"  Attempts: {result.AttemptsUsed}");
                    report.AppendLine();

                    successCount++;
                }
                else
                {
                    report.AppendLine($"Level {level:D3}: FAILED - {result.FailureReason}");
                    report.AppendLine($"  Attempts: {result.AttemptsUsed}");
                    report.AppendLine();

                    failCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            report.AppendLine($"=== SUMMARY ===");
            report.AppendLine($"Success: {successCount}/{end - start + 1}");
            report.AppendLine($"Failed: {failCount}/{end - start + 1}");

            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog(
                "Generation Complete",
                $"Generated {successCount} levels successfully.\n{failCount} levels failed.\n\nSee console for details.",
                "OK"
            );
        }

        private static void ValidateLevelRange(string worldId, int start, int end)
        {
            string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1);
            string inputPath = $"{LEVELS_PATH}/{worldFolder}";

            var solver = new LevelSolver();
            solver.VerboseLogging = false;

            int validCount = 0;
            int invalidCount = 0;
            int missingCount = 0;

            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== LEVEL VALIDATION REPORT: {worldId} {start}-{end} ===\n");

            for (int level = start; level <= end; level++)
            {
                string filePath = $"{inputPath}/level_{level:D3}.json";

                if (!File.Exists(filePath))
                {
                    missingCount++;
                    continue;
                }

                EditorUtility.DisplayProgressBar(
                    $"Validating {worldId} Levels",
                    $"Level {level}/{end}",
                    (float)(level - start) / (end - start + 1)
                );

                var levelData = LevelDataLoader.LoadLevel(worldId, level);
                if (levelData == null)
                {
                    report.AppendLine($"Level {level:D3}: FAILED TO LOAD");
                    invalidCount++;
                    continue;
                }

                var result = solver.SolveLevel(levelData);

                if (result.Success)
                {
                    validCount++;

                    // Check if star thresholds are reasonable
                    bool thresholdsOk = levelData.star_move_thresholds != null &&
                                        levelData.star_move_thresholds.Length >= 3 &&
                                        result.TotalMoves <= levelData.star_move_thresholds[2];

                    if (!thresholdsOk)
                    {
                        report.AppendLine($"Level {level:D3}: SOLVABLE but thresholds may need adjustment");
                        report.AppendLine($"  Solver: {result.TotalMoves} moves, Thresholds: [{string.Join(", ", levelData.star_move_thresholds ?? new int[0])}]");
                    }
                }
                else
                {
                    report.AppendLine($"Level {level:D3}: UNSOLVABLE - {result.FailureReason}");
                    invalidCount++;
                }
            }

            EditorUtility.ClearProgressBar();

            report.AppendLine($"\n=== SUMMARY ===");
            report.AppendLine($"Valid: {validCount}");
            report.AppendLine($"Invalid: {invalidCount}");
            report.AppendLine($"Missing: {missingCount}");

            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog(
                "Validation Complete",
                $"Valid: {validCount}\nInvalid: {invalidCount}\nMissing: {missingCount}\n\nSee console for details.",
                "OK"
            );
        }

        #endregion
    }

    /// <summary>
    /// Editor window for generating individual levels with custom parameters
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        private LevelGenerator.LevelParams parameters = new LevelGenerator.LevelParams();
        private LevelGenerator.GenerationResult lastResult;
        private Vector2 scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<LevelGeneratorWindow>("Level Generator");
            window.minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Basic settings
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            parameters.levelNumber = EditorGUILayout.IntField("Level Number", parameters.levelNumber);
            parameters.worldId = EditorGUILayout.TextField("World ID", parameters.worldId);
            parameters.seed = EditorGUILayout.IntField("Random Seed (-1 = random)", parameters.seed);

            EditorGUILayout.Space();

            // Container settings
            EditorGUILayout.LabelField("Containers", EditorStyles.boldLabel);
            parameters.standardContainerCount = EditorGUILayout.IntSlider("Standard Containers", parameters.standardContainerCount, 3, 30);
            parameters.standardMaxRows = EditorGUILayout.IntSlider("Max Rows (Standard)", parameters.standardMaxRows, 1, 5);
            parameters.singleSlotContainerCount = EditorGUILayout.IntSlider("Single-Slot Containers", parameters.singleSlotContainerCount, 0, 20);
            parameters.singleSlotDepth = EditorGUILayout.IntSlider("Single-Slot Depth", parameters.singleSlotDepth, 2, 15);

            EditorGUILayout.Space();

            // Lock settings
            EditorGUILayout.LabelField("Locked Containers", EditorStyles.boldLabel);
            parameters.lockedContainerCount = EditorGUILayout.IntSlider("Locked Count", parameters.lockedContainerCount, 0, 15);
            parameters.minUnlockMatches = EditorGUILayout.IntSlider("Min Unlock Matches", parameters.minUnlockMatches, 1, 5);
            parameters.maxUnlockMatches = EditorGUILayout.IntSlider("Max Unlock Matches", parameters.maxUnlockMatches, parameters.minUnlockMatches, 7);

            EditorGUILayout.Space();

            // Train settings
            EditorGUILayout.LabelField("Trains (Carousels)", EditorStyles.boldLabel);
            parameters.trainCount = EditorGUILayout.IntSlider("Train Count", parameters.trainCount, 0, 6);
            parameters.containersPerTrain = EditorGUILayout.IntSlider("Containers Per Train", parameters.containersPerTrain, 3, 12);
            parameters.horizontalTrains = EditorGUILayout.Toggle("Horizontal Trains", parameters.horizontalTrains);

            EditorGUILayout.Space();

            // Item settings
            EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);
            parameters.itemTypeCount = EditorGUILayout.IntSlider("Item Types", parameters.itemTypeCount, 2, 30);
            parameters.emptySlots = EditorGUILayout.IntSlider("Empty Slots", parameters.emptySlots, 1, 10);

            EditorGUILayout.Space();

            // Timer settings
            EditorGUILayout.LabelField("Timer", EditorStyles.boldLabel);
            parameters.secondsPerItem = EditorGUILayout.Slider("Seconds Per Item", parameters.secondsPerItem, 1.5f, 5f);

            EditorGUILayout.Space();

            // Generation settings
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            parameters.maxGenerationAttempts = EditorGUILayout.IntSlider("Max Attempts", parameters.maxGenerationAttempts, 10, 200);

            EditorGUILayout.Space(20);

            // Buttons
            if (GUILayout.Button("Load Parameters from Level Plan", GUILayout.Height(30)))
            {
                parameters = LevelGenerator.GetParametersForLevel(parameters.worldId, parameters.levelNumber);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("GENERATE LEVEL", GUILayout.Height(40)))
            {
                GenerateLevel();
            }

            EditorGUILayout.Space();

            // Results
            if (lastResult != null)
            {
                EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);

                if (lastResult.Success)
                {
                    EditorGUILayout.HelpBox(
                        $"SUCCESS!\n" +
                        $"Solver: {lastResult.SolveResult.TotalMoves} moves, {lastResult.SolveResult.TotalMatches} matches\n" +
                        $"Stars: [{lastResult.StarThresholds[0]}, {lastResult.StarThresholds[1]}, {lastResult.StarThresholds[2]}]\n" +
                        $"Items: {lastResult.TotalItems}, Timer: {lastResult.TimeLimitSeconds}s\n" +
                        $"Attempts: {lastResult.AttemptsUsed}",
                        MessageType.Info
                    );

                    if (GUILayout.Button("Save Level to File"))
                    {
                        SaveLevelToFile();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"FAILED: {lastResult.FailureReason}\nAttempts: {lastResult.AttemptsUsed}",
                        MessageType.Error
                    );
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void GenerateLevel()
        {
            var generator = new LevelGenerator();
            lastResult = generator.GenerateLevel(parameters);

            if (lastResult.Success)
            {
                Debug.Log($"[LevelGeneratorWindow] Level generated successfully!");
            }
            else
            {
                Debug.LogWarning($"[LevelGeneratorWindow] Level generation failed: {lastResult.FailureReason}");
            }
        }

        private void SaveLevelToFile()
        {
            if (lastResult == null || !lastResult.Success) return;

            string worldFolder = char.ToUpper(parameters.worldId[0]) + parameters.worldId.Substring(1);
            string directory = $"Assets/_Project/Resources/Data/Levels/{worldFolder}";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = $"{directory}/level_{parameters.levelNumber:D3}.json";
            string json = JsonUtility.ToJson(lastResult.LevelData, true);
            File.WriteAllText(filePath, json);

            AssetDatabase.Refresh();
            Debug.Log($"[LevelGeneratorWindow] Saved level to {filePath}");

            EditorUtility.DisplayDialog("Level Saved", $"Level saved to:\n{filePath}", "OK");
        }
    }
}
#endif
