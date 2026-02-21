using UnityEngine;
using System.Collections.Generic;

namespace SortResort
{
    /// <summary>
    /// Loads crop metadata from crop_metadata.json and applies crop-aware anchors
    /// to RectTransforms so trimmed sprites display at their original fullscreen position.
    /// </summary>
    public static class CropMetadata
    {
        [System.Serializable]
        private class CropEntry
        {
            public int x, y, w, h;
            public int orig_w, orig_h;
            public bool is_group;
        }

        [System.Serializable]
        private class CropData
        {
            public int version;
            public Dictionary<string, CropEntry> crops;
        }

        private static Dictionary<string, CropEntry> _crops;
        private static bool _loaded;

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var textAsset = Resources.Load<TextAsset>("Data/crop_metadata");
            if (textAsset == null)
            {
                _crops = new Dictionary<string, CropEntry>();
                return;
            }

            // Unity's JsonUtility doesn't support Dictionary, so parse manually
            _crops = new Dictionary<string, CropEntry>();
            var json = textAsset.text;

            // Simple JSON parsing for our known structure
            try
            {
                // Find "crops" object
                int cropsIdx = json.IndexOf("\"crops\"");
                if (cropsIdx < 0) return;

                int braceStart = json.IndexOf('{', cropsIdx + 7);
                if (braceStart < 0) return;

                // Parse each entry
                int pos = braceStart + 1;
                while (pos < json.Length)
                {
                    // Find next key
                    int keyStart = json.IndexOf('"', pos);
                    if (keyStart < 0) break;

                    int keyEnd = json.IndexOf('"', keyStart + 1);
                    if (keyEnd < 0) break;

                    string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                    // Find the value object
                    int objStart = json.IndexOf('{', keyEnd);
                    if (objStart < 0) break;

                    int objEnd = json.IndexOf('}', objStart);
                    if (objEnd < 0) break;

                    string objStr = json.Substring(objStart, objEnd - objStart + 1);

                    // Parse fields from the object
                    var entry = new CropEntry();
                    entry.x = ParseIntField(objStr, "x");
                    entry.y = ParseIntField(objStr, "y");
                    entry.w = ParseIntField(objStr, "w");
                    entry.h = ParseIntField(objStr, "h");
                    entry.orig_w = ParseIntField(objStr, "orig_w");
                    entry.orig_h = ParseIntField(objStr, "orig_h");
                    entry.is_group = objStr.Contains("\"is_group\"") && objStr.Contains("true");

                    if (entry.orig_w > 0 && entry.orig_h > 0)
                        _crops[key] = entry;

                    pos = objEnd + 1;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CropMetadata] Failed to parse crop_metadata.json: {e.Message}");
                _crops = new Dictionary<string, CropEntry>();
            }
        }

        private static int ParseIntField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return 0;

            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return 0;

            // Find the number after the colon
            int start = colonIdx + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t'))
                start++;

            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
                end++;

            if (end > start && int.TryParse(json.Substring(start, end - start), out int val))
                return val;

            return 0;
        }

        /// <summary>
        /// Apply crop-aware anchors to a RectTransform.
        /// Converts the PIL crop rect to Unity anchors so the trimmed sprite
        /// appears at its original fullscreen position.
        /// </summary>
        /// <param name="rt">The RectTransform to modify</param>
        /// <param name="resourcePath">Resource path without extension (e.g., "Sprites/UI/HUD/free_ui_top")</param>
        /// <returns>True if crop data was found and applied, false otherwise</returns>
        public static bool ApplyCropAnchors(RectTransform rt, string resourcePath)
        {
            if (rt == null) return false;

            EnsureLoaded();

            if (_crops == null || !_crops.TryGetValue(resourcePath, out var entry))
                return false;

            float ow = entry.orig_w;
            float oh = entry.orig_h;

            // PIL coordinates: top-left origin (x, y, w, h)
            // Unity anchors: bottom-left origin
            // anchorMin.x = x / orig_w
            // anchorMin.y = 1 - (y + h) / orig_h  (bottom edge in Unity)
            // anchorMax.x = (x + w) / orig_w
            // anchorMax.y = 1 - y / orig_h  (top edge in Unity)
            rt.anchorMin = new Vector2(entry.x / ow, 1f - (entry.y + entry.h) / oh);
            rt.anchorMax = new Vector2((entry.x + entry.w) / ow, 1f - entry.y / oh);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return true;
        }

        /// <summary>
        /// Check if a resource path has crop data without applying it.
        /// </summary>
        public static bool HasCropData(string resourcePath)
        {
            EnsureLoaded();
            return _crops != null && _crops.ContainsKey(resourcePath);
        }

        /// <summary>
        /// Apply crop anchors for an animation folder path.
        /// Animation groups are keyed by directory path (e.g., "Sprites/UI/LevelComplete/DancingStars").
        /// </summary>
        public static bool ApplyCropAnchorsForFolder(RectTransform rt, string folderResourcePath)
        {
            return ApplyCropAnchors(rt, folderResourcePath);
        }

        /// <summary>
        /// Convert a fullscreen-space anchor position to crop-relative anchor position.
        /// Use this for child elements (text, icons) that are children of a cropped image
        /// and need their anchors recalculated to match the new parent rect.
        /// </summary>
        /// <param name="fullscreenAnchor">The original anchor in fullscreen (0-1) space</param>
        /// <param name="parentResourcePath">The resource path of the cropped parent image</param>
        /// <returns>The anchor converted to crop-relative space, or the original if no crop data</returns>
        public static Vector2 ConvertAnchorToCropSpace(Vector2 fullscreenAnchor, string parentResourcePath)
        {
            EnsureLoaded();

            if (_crops == null || !_crops.TryGetValue(parentResourcePath, out var entry))
                return fullscreenAnchor;

            float ow = entry.orig_w;
            float oh = entry.orig_h;

            // Parent's Unity anchors (what ApplyCropAnchors sets)
            float parentMinX = entry.x / ow;
            float parentMinY = 1f - (entry.y + entry.h) / oh;
            float parentMaxX = (entry.x + entry.w) / ow;
            float parentMaxY = 1f - entry.y / oh;

            float parentW = parentMaxX - parentMinX;
            float parentH = parentMaxY - parentMinY;

            if (parentW <= 0 || parentH <= 0)
                return fullscreenAnchor;

            // Convert from fullscreen space to parent-relative space
            float relX = (fullscreenAnchor.x - parentMinX) / parentW;
            float relY = (fullscreenAnchor.y - parentMinY) / parentH;

            return new Vector2(relX, relY);
        }
    }
}
