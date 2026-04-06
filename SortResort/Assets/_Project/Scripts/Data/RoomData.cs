using UnityEngine;
using System;
using System.Collections.Generic;

namespace SortResort
{
    public enum RoomSlot
    {
        Background,
        Wall,
        Floor,
        Ceiling,
        Window,
        Curtains,
        CeilingLight,
        Rug,
        LeftDecorationTable,
        Bed,
        LeftDecorationItem,
        RightDecoration,
        Mascot,
        EmoteBubble,
        Emote
    }

    [Serializable]
    public class RoomCropRect
    {
        public int x, y, width, height;
    }

    public class RoomItemData
    {
        public string id;
        public RoomSlot slot;
        public string resourcePath;
        public string displayName;
        public RoomCropRect cropRect;
        public bool isFullscreen;

        public void ComputeAnchors(out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (isFullscreen)
            {
                // Slight shrink to show more of the image through the window
                anchorMin = new Vector2(0.1f, 0.15f);
                anchorMax = new Vector2(0.9f, 0.85f);
                return;
            }

            const float origW = 1080f;
            const float origH = 1920f;
            float minX = cropRect.x / origW;
            float minY = 1f - (cropRect.y + cropRect.height) / origH;
            float maxX = (cropRect.x + cropRect.width) / origW;
            float maxY = 1f - cropRect.y / origH;

            anchorMin = new Vector2(minX, minY);
            anchorMax = new Vector2(maxX, maxY);
        }
    }

    public static class RoomData
    {
        private static Dictionary<RoomSlot, List<RoomItemData>> _allItems;
        private static bool _loaded;

        public static readonly RoomSlot[] SlotLayerOrder = new RoomSlot[]
        {
            RoomSlot.Background,
            RoomSlot.Wall,
            RoomSlot.Floor,
            RoomSlot.Ceiling,
            RoomSlot.Window,
            RoomSlot.Curtains,
            RoomSlot.CeilingLight,
            RoomSlot.Rug,
            RoomSlot.LeftDecorationTable,
            RoomSlot.RightDecoration,
            RoomSlot.Bed,
            RoomSlot.LeftDecorationItem,
            RoomSlot.Mascot,
            RoomSlot.EmoteBubble,
            RoomSlot.Emote
        };

        private static readonly Dictionary<string, RoomSlot> SlotNameMap = new Dictionary<string, RoomSlot>
        {
            { "Background", RoomSlot.Background },
            { "Wall", RoomSlot.Wall },
            { "Floor", RoomSlot.Floor },
            { "Ceiling", RoomSlot.Ceiling },
            { "Window", RoomSlot.Window },
            { "Curtains", RoomSlot.Curtains },
            { "CeilingLight", RoomSlot.CeilingLight },
            { "Rug", RoomSlot.Rug },
            { "Bed", RoomSlot.Bed },
            { "LeftDecorationTable", RoomSlot.LeftDecorationTable },
            { "LeftDecorationItem", RoomSlot.LeftDecorationItem },
            { "RightDecoration", RoomSlot.RightDecoration },
            { "Mascot", RoomSlot.Mascot },
            { "EmoteBubble", RoomSlot.EmoteBubble },
            { "Emote", RoomSlot.Emote },
        };

        public static Dictionary<RoomSlot, List<RoomItemData>> GetAllItems()
        {
            EnsureLoaded();
            return _allItems;
        }

        public static List<RoomItemData> GetItemsForSlot(RoomSlot slot)
        {
            EnsureLoaded();
            if (_allItems.TryGetValue(slot, out var list))
                return list;
            return new List<RoomItemData>();
        }

        public static RoomItemData GetItemById(string itemId)
        {
            EnsureLoaded();
            foreach (var kvp in _allItems)
            {
                foreach (var item in kvp.Value)
                {
                    if (item.id == itemId) return item;
                }
            }
            return null;
        }

        public static string GetDefaultItemId(RoomSlot slot)
        {
            var items = GetItemsForSlot(slot);
            return items.Count > 0 ? items[0].id : null;
        }

        public static string GetSlotDisplayName(RoomSlot slot)
        {
            switch (slot)
            {
                case RoomSlot.Background: return "Background";
                case RoomSlot.Wall: return "Walls";
                case RoomSlot.Floor: return "Floor";
                case RoomSlot.Ceiling: return "Ceiling";
                case RoomSlot.Window: return "Window";
                case RoomSlot.Curtains: return "Curtains";
                case RoomSlot.CeilingLight: return "Lighting";
                case RoomSlot.Rug: return "Carpet";
                case RoomSlot.Bed: return "Bed";
                case RoomSlot.LeftDecorationTable: return "Table";
                case RoomSlot.LeftDecorationItem: return "Table Item";
                case RoomSlot.RightDecoration: return "Right Decor";
                case RoomSlot.Mascot: return "Mascot";
                case RoomSlot.EmoteBubble: return "Bubble";
                case RoomSlot.Emote: return "Emote";
                default: return slot.ToString();
            }
        }

        public static bool IsRoomItemUnlocked(string itemId)
        {
            if (IsNoneItem(itemId)) return true; // "None" always available
            if (SaveManager.Instance == null) return true; // Fallback if no save
            return SaveManager.Instance.OwnsRoomItem(itemId);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _allItems = new Dictionary<RoomSlot, List<RoomItemData>>();
            foreach (RoomSlot slot in SlotLayerOrder)
                _allItems[slot] = new List<RoomItemData>();

            // Add background items from existing world backgrounds
            AddBackgroundItems();

            // Parse room_cropped_info.json
            var textAsset = Resources.Load<TextAsset>("Data/room_cropped_info");
            if (textAsset == null)
            {
                Debug.LogWarning("[RoomData] room_cropped_info.json not found in Resources/Data/");
                return;
            }

            try
            {
                ParseCroppedInfo(textAsset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RoomData] Failed to parse room_cropped_info.json: {e}");
            }

            // Add "None" option to all slots except Wall, Floor, Ceiling
            AddNoneOptions();
        }

        public const string NONE_ITEM_ID = "none";

        private static void AddNoneOptions()
        {
            RoomSlot[] excludeSlots = { RoomSlot.Wall, RoomSlot.Floor, RoomSlot.Ceiling };
            foreach (var slot in SlotLayerOrder)
            {
                if (System.Array.IndexOf(excludeSlots, slot) >= 0)
                    continue;

                _allItems[slot].Add(new RoomItemData
                {
                    id = $"{NONE_ITEM_ID}_{slot}",
                    slot = slot,
                    resourcePath = null,
                    displayName = "None",
                    isFullscreen = false,
                    cropRect = new RoomCropRect { x = 0, y = 0, width = 1, height = 1 }
                });
            }
        }

        public static bool IsNoneItem(string itemId)
        {
            return itemId != null && itemId.StartsWith(NONE_ITEM_ID);
        }

        private static string FormatDisplayName(string id)
        {
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        private static void AddBackgroundItems()
        {
            // Default sky background (from lucky spin screen)
            _allItems[RoomSlot.Background].Insert(0, new RoomItemData
            {
                id = "sky_clouds_background",
                slot = RoomSlot.Background,
                resourcePath = "Sprites/UI/LuckySpin/spin_screen_background",
                displayName = "Blue Sky",
                isFullscreen = true,
                cropRect = new RoomCropRect { x = 0, y = 0, width = 1080, height = 1920 }
            });

            // World backgrounds that use the level select color images (scaled to fit window)
            string[] worlds = { "supermarket", "tavern" };
            string[] names = { "Supermarket", "Tavern" };

            for (int i = 0; i < worlds.Length; i++)
            {
                _allItems[RoomSlot.Background].Add(new RoomItemData
                {
                    id = $"{worlds[i]}_background",
                    slot = RoomSlot.Background,
                    resourcePath = $"Sprites/Backgrounds/{worlds[i]}_background_color",
                    displayName = names[i],
                    isFullscreen = true,
                    cropRect = new RoomCropRect { x = 0, y = 0, width = 1080, height = 1920 }
                });
            }

            // Dedicated window backgrounds (positioned via crop data, loaded from JSON)
            // These are added via the JSON parser (slot = "Background")
        }

        private static void ParseCroppedInfo(string json)
        {
            // Parse the top-level JSON object: find the root {}, then iterate top-level keys
            int rootStart = json.IndexOf('{');
            if (rootStart < 0) return;
            int rootEnd = FindMatchingBrace(json, rootStart);
            if (rootEnd < 0) return;

            // Work inside the root object (between { and })
            int pos = rootStart + 1;

            while (pos < rootEnd)
            {
                // Find next top-level key
                int keyStart = json.IndexOf('"', pos);
                if (keyStart < 0 || keyStart >= rootEnd) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                if (keyEnd < 0) break;

                string itemId = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                // Find the colon, then the opening brace of the value object
                int colonIdx = json.IndexOf(':', keyEnd + 1);
                if (colonIdx < 0) break;
                int objStart = json.IndexOf('{', colonIdx + 1);
                if (objStart < 0) break;

                int objEnd = FindMatchingBrace(json, objStart);
                if (objEnd < 0) break;

                string objStr = json.Substring(objStart, objEnd - objStart + 1);

                // Move past this entire value object for the next iteration
                pos = objEnd + 1;

                // Skip button entries
                string slotName = ParseStringField(objStr, "slot");
                if (slotName == "BUTTON" || string.IsNullOrEmpty(slotName))
                    continue;

                if (!SlotNameMap.TryGetValue(slotName, out var slot))
                    continue;

                // Parse crop_rect
                int cropStart = objStr.IndexOf("\"crop_rect\"");
                if (cropStart < 0) continue;
                int cropObjStart = objStr.IndexOf('{', cropStart);
                int cropObjEnd = objStr.IndexOf('}', cropObjStart);
                string cropStr = objStr.Substring(cropObjStart, cropObjEnd - cropObjStart + 1);

                var cropRect = new RoomCropRect
                {
                    x = ParseIntField(cropStr, "x"),
                    y = ParseIntField(cropStr, "y"),
                    width = ParseIntField(cropStr, "width"),
                    height = ParseIntField(cropStr, "height")
                };

                string displayName = FormatDisplayName(itemId);

                var item = new RoomItemData
                {
                    id = itemId,
                    slot = slot,
                    resourcePath = $"Sprites/UI/MyRoom/{slotName}/{itemId}",
                    displayName = displayName,
                    cropRect = cropRect,
                    isFullscreen = false
                };

                _allItems[slot].Add(item);
                Debug.Log($"[RoomData] Loaded item: {itemId} -> slot {slotName}");
            }

            // Log summary
            int totalItems = 0;
            foreach (var kvp in _allItems)
                totalItems += kvp.Value.Count;
            Debug.Log($"[RoomData] Loaded {totalItems} room items across {_allItems.Count} slots");
        }

        private static int FindMatchingBrace(string json, int openPos)
        {
            int depth = 0;
            for (int i = openPos; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        private static string ParseStringField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return null;

            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return null;

            int quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) return null;

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        private static int ParseIntField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return 0;

            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return 0;

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
    }
}
