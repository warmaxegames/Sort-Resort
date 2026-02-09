#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace SortResort
{
    public static class FontAssetGenerator
    {
        private static readonly string[] FontFiles = {
            "BENZIN-BOLD",
            "BENZIN-SEMIBOLD",
            "BENZIN-MEDIUM",
            "BENZIN-EXTRABOLD"
        };

        [MenuItem("Tools/Sort Resort/Fonts/Generate Benzin SDF Fonts")]
        public static void GenerateAllBenzinFonts()
        {
            string inputDir = "Assets/_Project/Fonts";
            string outputDir = "Assets/_Project/Resources/Fonts";

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Fonts"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Fonts");
            }

            int created = 0;
            foreach (var fontName in FontFiles)
            {
                string ttfPath = $"{inputDir}/{fontName}.TTF";
                string outputPath = $"{outputDir}/{fontName} SDF.asset";

                var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (font == null)
                {
                    Debug.LogError($"[FontGenerator] TTF not found: {ttfPath}");
                    continue;
                }

                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
                if (existing != null)
                {
                    Debug.Log($"[FontGenerator] Regenerating: {outputPath}");
                    AssetDatabase.DeleteAsset(outputPath);
                }

                var fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024, 1024);

                if (fontAsset == null)
                {
                    Debug.LogError($"[FontGenerator] Failed to create SDF for {fontName}");
                    continue;
                }

                AssetDatabase.CreateAsset(fontAsset, outputPath);

                if (fontAsset.atlasTexture != null)
                {
                    fontAsset.atlasTexture.name = $"{fontName} Atlas";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                }

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = $"{fontName} Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"[FontGenerator] Created SDF font: {outputPath}");
                created++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Font Generation Complete",
                $"Generated {created}/{FontFiles.Length} Benzin SDF font assets in\n{outputDir}",
                "OK");
        }

        [MenuItem("Tools/Sort Resort/Fonts/Set Benzin-Bold as TMP Default")]
        public static void SetBenzinBoldAsDefault()
        {
            string boldPath = "Assets/_Project/Resources/Fonts/BENZIN-BOLD SDF.asset";
            var boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(boldPath);

            if (boldFont == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Benzin-Bold SDF not found. Run 'Generate Benzin SDF Fonts' first.",
                    "OK");
                return;
            }

            string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);

            if (settings == null)
            {
                Debug.LogError("[FontGenerator] TMP Settings not found at " + settingsPath);
                return;
            }

            var so = new SerializedObject(settings);
            var fontProp = so.FindProperty("m_defaultFontAsset");
            fontProp.objectReferenceValue = boldFont;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[FontGenerator] TMP default font set to Benzin-Bold SDF");
            EditorUtility.DisplayDialog("Success",
                "TMP default font changed to Benzin-Bold SDF.\n" +
                "All new TMP text components will use this font automatically.",
                "OK");
        }
    }
}
#endif
