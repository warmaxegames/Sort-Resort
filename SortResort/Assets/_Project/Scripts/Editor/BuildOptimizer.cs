using UnityEngine;
using UnityEditor;
using System.IO;

namespace SortResort.Editor
{
    public static class BuildOptimizer
    {
        // ── Texture Optimization ──────────────────────────────────────────

        [MenuItem("Tools/Sort Resort/Optimize/Optimize Textures for WebGL")]
        public static void OptimizeTexturesForWebGL()
        {
            string spritesRoot = "Assets/_Project/Resources/Sprites";
            string[] allTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesRoot });

            int modified = 0;
            int total = allTextures.Length;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(allTextures[i]);
                    EditorUtility.DisplayProgressBar(
                        "Optimizing Textures",
                        $"Processing {Path.GetFileName(path)} ({i + 1}/{total})",
                        (float)i / total);

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    int maxSize = GetMaxTextureSize(path);

                    var platformSettings = importer.GetPlatformTextureSettings("WebGL");
                    platformSettings.overridden = true;
                    platformSettings.maxTextureSize = maxSize;
                    platformSettings.format = TextureImporterFormat.ASTC_6x6;
                    platformSettings.crunchedCompression = true;
                    platformSettings.compressionQuality = 50;

                    importer.SetPlatformTextureSettings(platformSettings);
                    importer.SaveAndReimport();
                    modified++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[BuildOptimizer] Texture optimization complete: {modified}/{total} textures updated with WebGL overrides.");
            EditorUtility.DisplayDialog("Texture Optimization",
                $"Updated {modified} of {total} textures with WebGL platform overrides.\n\n" +
                "Settings applied:\n" +
                "- ASTC 6x6 format\n" +
                "- Crunch compression (quality 50)\n" +
                "- Category-specific max texture sizes",
                "OK");
        }

        static int GetMaxTextureSize(string assetPath)
        {
            string normalized = assetPath.Replace("\\", "/");

            // Full-screen animation frames → 1024
            if (normalized.Contains("LevelComplete/Rays") ||
                normalized.Contains("LevelComplete/Curtains") ||
                normalized.Contains("Mascots/Animations"))
                return 1024;

            // Smaller animation elements → 512
            if (normalized.Contains("LevelComplete/DancingStars") ||
                normalized.Contains("LevelComplete/StarRibbon") ||
                normalized.Contains("LevelComplete/Star1") ||
                normalized.Contains("LevelComplete/Star2") ||
                normalized.Contains("LevelComplete/Star3") ||
                normalized.Contains("LevelComplete/LevelBoard") ||
                normalized.Contains("LevelComplete/BottomBoard") ||
                normalized.Contains("LevelComplete/LevelCompleteText"))
                return 512;

            // Item sprites (~127px actual, rendered ~95px) → 256
            if (normalized.Contains("Sprites/Items/"))
                return 256;

            // Achievement art (687×301) → 512
            if (normalized.Contains("Sprites/UI/Achievements/"))
                return 512;

            // Portal icons (~134×68) → 256
            if (normalized.Contains("Sprites/UI/Icons/"))
                return 256;

            // HUD overlays (1080×222) → 1024
            if (normalized.Contains("Sprites/UI/HUD/"))
                return 1024;

            // Backgrounds (1080×1920) → 1024
            if (normalized.Contains("Sprites/Backgrounds/"))
                return 1024;

            // Pause menu (1080×1920) → 1024
            if (normalized.Contains("Sprites/UI/PauseMenu/"))
                return 1024;

            // Container sprites → 512
            if (normalized.Contains("Sprites/Containers/"))
                return 512;

            // Dialogue boxes → 512
            if (normalized.Contains("Sprites/UI/Dialogue/"))
                return 512;

            // Everything else → 1024
            return 1024;
        }

        // ── Audio Optimization ────────────────────────────────────────────

        [MenuItem("Tools/Sort Resort/Optimize/Optimize Audio for WebGL")]
        public static void OptimizeAudioForWebGL()
        {
            string audioRoot = "Assets/_Project/Resources/Audio";
            string[] allAudio = AssetDatabase.FindAssets("t:AudioClip", new[] { audioRoot });

            int modified = 0;
            int total = allAudio.Length;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(allAudio[i]);
                    EditorUtility.DisplayProgressBar(
                        "Optimizing Audio",
                        $"Processing {Path.GetFileName(path)} ({i + 1}/{total})",
                        (float)i / total);

                    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null) continue;

                    string normalized = path.Replace("\\", "/");
                    bool isMusic = normalized.Contains("Audio/Music/");
                    bool isDialogue = normalized.Contains("Audio/Dialogue/");

                    // Reset forceToMono (undo any previous global setting)
                    importer.forceToMono = false;

                    // Dialogue letter clips are tiny WAVs — skip compression entirely,
                    // just clear any existing override to restore original import settings
                    if (isDialogue)
                    {
                        importer.ClearSampleSettingOverride("WebGL");
                        importer.SaveAndReimport();
                        modified++;
                        continue;
                    }

                    var platformSettings = importer.GetOverrideSampleSettings("WebGL");
                    if (isMusic)
                    {
                        platformSettings.loadType = AudioClipLoadType.CompressedInMemory;
                        platformSettings.quality = 0.4f;
                    }
                    else
                    {
                        platformSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                        platformSettings.quality = 0.6f;
                    }
                    platformSettings.compressionFormat = AudioCompressionFormat.Vorbis;

                    importer.SetOverrideSampleSettings("WebGL", platformSettings);
                    importer.SaveAndReimport();
                    modified++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[BuildOptimizer] Audio optimization complete: {modified}/{total} clips updated with WebGL overrides.");
            EditorUtility.DisplayDialog("Audio Optimization",
                $"Updated {modified} of {total} audio clips with WebGL overrides.\n\n" +
                "Settings applied:\n" +
                "- Music: CompressedInMemory, Vorbis q0.4, stereo\n" +
                "- SFX/UI: DecompressOnLoad, Vorbis q0.6\n" +
                "- Dialogue: DecompressOnLoad, Vorbis q0.5",
                "OK");
        }

        // ── Combined Optimization ─────────────────────────────────────────

        [MenuItem("Tools/Sort Resort/Optimize/Optimize All for WebGL")]
        public static void OptimizeAllForWebGL()
        {
            OptimizeTexturesForWebGL();
            OptimizeAudioForWebGL();

            EditorUtility.DisplayDialog("WebGL Optimization Complete",
                "All texture and audio optimizations applied.\n\n" +
                "Next steps:\n" +
                "1. Run 'python minify_levels.py' from project root\n" +
                "2. Play-test a few levels to verify quality\n" +
                "3. Build WebGL and compare size",
                "OK");
        }
    }
}
