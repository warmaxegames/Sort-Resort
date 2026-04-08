using UnityEngine;
using System.Collections.Generic;

namespace SortResort
{
    public static class LootTable
    {
        private static Dictionary<string, List<string>> _pools;
        private static bool _loaded;

        // World prefixes mapped to box IDs
        private static readonly Dictionary<string, string> PrefixToBoxId = new Dictionary<string, string>
        {
            { "space_", "space" },
            { "farm_", "farm" },
            { "tavern_", "tavern" },
            { "beach_", "island" },    // beach items go in island box
            { "island_", "island" },   // island items go in island box
            { "supermarket_", "supermarket" },
        };

        // Items that are generic (no world prefix) go in the island box
        private const string GENERIC_BOX_ID = "island";

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _pools = new Dictionary<string, List<string>>
            {
                { "normal", new List<string>() },
                { "island", new List<string>() },
                { "supermarket", new List<string>() },
                { "farm", new List<string>() },
                { "space", new List<string>() },
                { "tavern", new List<string>() },
            };

            // Default items are never in any loot pool
            var defaults = new HashSet<string>(GetDefaultOwnedItems());

            // Scan all room items and assign to pools
            var allItems = RoomData.GetAllItems();
            foreach (var kvp in allItems)
            {
                foreach (var item in kvp.Value)
                {
                    // Skip none/empty items and UI elements
                    if (RoomData.IsNoneItem(item.id)) continue;
                    if (item.resourcePath == null) continue;

                    // Default items are excluded from all loot pools
                    if (defaults.Contains(item.id)) continue;

                    string assignedBox = null;

                    // Check world prefixes
                    foreach (var prefix in PrefixToBoxId)
                    {
                        if (item.id.StartsWith(prefix.Key))
                        {
                            assignedBox = prefix.Value;
                            break;
                        }
                    }

                    // Generic non-default items go in the normal (general) box
                    if (assignedBox == null)
                        assignedBox = "normal";

                    if (_pools.ContainsKey(assignedBox))
                        _pools[assignedBox].Add(item.id);
                }
            }

            // Log pool sizes
            foreach (var kvp in _pools)
            {
                Debug.Log($"[LootTable] Pool '{kvp.Key}': {kvp.Value.Count} items");
            }
        }

        /// <summary>
        /// Get a random item from the box's loot pool. Always random from full pool.
        /// </summary>
        public static string GetRandomReward(string boxId)
        {
            EnsureLoaded();

            if (!_pools.TryGetValue(boxId, out var pool) || pool.Count == 0)
            {
                Debug.LogWarning($"[LootTable] No items in pool for box '{boxId}'");
                return null;
            }

            int index = Random.Range(0, pool.Count);
            return pool[index];
        }

        /// <summary>
        /// Get the full loot pool for a box type (for info panel display).
        /// </summary>
        public static List<string> GetLootPool(string boxId)
        {
            EnsureLoaded();

            if (_pools.TryGetValue(boxId, out var pool))
                return new List<string>(pool); // Return copy

            return new List<string>();
        }

        /// <summary>
        /// Get the list of default items that all players start with.
        /// </summary>
        public static List<string> GetDefaultOwnedItems()
        {
            return new List<string>
            {
                "ball_toy",
                "blue_bed",
                "blue_carpet",
                "ceiling",
                "emote_bubble",
                "floor",
                "lamp",
                "heart_emote",
                "left_decoration_table",
                "flowerpot",
                "wall",
                "window_frame",
                "whiskers",
                "sky_clouds_background",
                "blue_curtains",
            };
        }

        /// <summary>
        /// Get the number of items in a pool.
        /// </summary>
        public static int GetPoolSize(string boxId)
        {
            EnsureLoaded();
            if (_pools.TryGetValue(boxId, out var pool))
                return pool.Count;
            return 0;
        }
    }
}
