using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SortResort
{
    /// <summary>
    /// Generates Sort Resort levels with configurable complexity parameters.
    /// Integrates with LevelSolver to verify solvability and set star thresholds.
    /// </summary>
    public class LevelGenerator
    {
        #region Configuration

        /// <summary>
        /// Parameters for level generation
        /// </summary>
        [Serializable]
        public class LevelParams
        {
            public int levelNumber = 1;
            public string worldId = "island";

            // Container configuration
            public int standardContainerCount = 6;
            public int singleSlotContainerCount = 0;
            public int singleSlotDepth = 3;           // Rows for single-slot containers
            public int standardMaxRows = 3;           // Max rows for standard containers

            // Locked containers
            public int lockedContainerCount = 0;
            public int minUnlockMatches = 2;
            public int maxUnlockMatches = 5;

            // Items
            public int itemTypeCount = 6;             // Number of different item types to use
            public int emptySlots = 3;                // Target number of empty front slots

            // Trains (carousel containers)
            public int trainCount = 0;
            public int containersPerTrain = 6;
            public bool horizontalTrains = true;      // false = vertical

            // Timer (seconds per item, scales with level)
            public float secondsPerItem = 3.0f;

            // Generation settings
            public int maxGenerationAttempts = 50;
            public int seed = -1;                     // -1 = random seed
        }

        /// <summary>
        /// Result of level generation
        /// </summary>
        public class GenerationResult
        {
            public bool Success;
            public LevelData LevelData;
            public LevelSolver.SolveResult SolveResult;
            public int[] StarThresholds;              // [3-star, 2-star, 1-star]
            public string FailureReason;
            public int AttemptsUsed;
            public int TotalItems;
            public int TimeLimitSeconds;
        }

        #endregion

        #region Static Item Lists

        // All available Island items (50 total)
        private static readonly string[] IslandItems = new string[]
        {
            "beachbag", "beachball_mixed", "beachball_redblue", "beachball_yellowblue",
            "bluesunglasses", "camera", "coconut", "flipflops", "flipflopsgreen",
            "greencoconut", "hibiscusflower", "icecreamcone", "icecreamconechocolate",
            "icecreamparfait", "lemonade", "lemonadepitcher", "lifepreserver",
            "maitai", "margarita", "messageinabottle", "orangedrink", "oysterblue",
            "pineapple", "pinkpolkadotbikini", "piratepopsicle", "popsiclemintchocolate",
            "sandals", "sandpale_blue", "sandpale_green", "sandpale_red", "scubatanks",
            "sparkleicecreamcone", "strawberrydaiquiri", "suitcaseyellow",
            "sunglassesorange", "sunglassespurple", "sunhat", "sunscreen",
            "sunscreenspf50", "surfboardbluepink", "surfboardbluewhitepeace",
            "surfboardbluewhitestripes", "surfboardblueyellow", "surfboardpalmtrees",
            "surfboardredorange", "swimmask", "vanilladrink", "vanillaicecreamcone",
            "watermelondrink", "watermelonslice"
        };

        // Timer scaling: seconds per item based on level number
        // Level 1-10: 4.0s, Level 11-20: 3.8s, ... Level 91-100: 2.0s
        public static float GetSecondsPerItem(int levelNumber)
        {
            int tier = Mathf.Clamp((levelNumber - 1) / 10, 0, 9);  // 0-9
            return 4.0f - (tier * 0.2f);  // 4.0, 3.8, 3.6, ... 2.0
        }

        #endregion

        #region Generation

        private LevelSolver solver = new LevelSolver();

        /// <summary>
        /// Generate a level with the given parameters
        /// </summary>
        public GenerationResult GenerateLevel(LevelParams parameters)
        {
            var result = new GenerationResult();

            // Initialize random seed
            if (parameters.seed >= 0)
            {
                Random.InitState(parameters.seed);
            }
            else
            {
                Random.InitState((int)DateTime.Now.Ticks);
            }

            // Disable verbose solver logging during generation attempts
            solver.VerboseLogging = false;

            for (int attempt = 1; attempt <= parameters.maxGenerationAttempts; attempt++)
            {
                result.AttemptsUsed = attempt;

                // Generate level data
                var levelData = CreateLevelData(parameters);
                if (levelData == null)
                {
                    result.FailureReason = "Failed to create level data";
                    continue;
                }

                // Quick validation: first match must be achievable in ≤2 moves
                int movesToFirstMatch = GetMovesToFirstMatch(levelData);
                if (movesToFirstMatch > 2)
                {
                    result.FailureReason = $"First match requires {movesToFirstMatch} moves (max 2)";
                    continue;
                }

                // Verify with solver
                var solveResult = solver.SolveLevel(levelData);

                if (solveResult.Success)
                {
                    result.Success = true;
                    result.LevelData = levelData;
                    result.SolveResult = solveResult;
                    result.TotalItems = CountTotalItems(levelData);
                    result.TimeLimitSeconds = Mathf.CeilToInt(result.TotalItems * parameters.secondsPerItem);

                    // Set star thresholds based on solver's move count
                    // 3-star: exact optimal, 2-star: +20%, 1-star: +50%
                    int optimal = solveResult.TotalMoves;
                    result.StarThresholds = new int[]
                    {
                        optimal,                           // 3-star = solver's score
                        Mathf.CeilToInt(optimal * 1.2f),   // 2-star
                        Mathf.CeilToInt(optimal * 1.5f)    // 1-star
                    };

                    // Ensure each threshold is at least 1 move apart
                    for (int i = 1; i < result.StarThresholds.Length; i++)
                    {
                        if (result.StarThresholds[i] <= result.StarThresholds[i - 1])
                            result.StarThresholds[i] = result.StarThresholds[i - 1] + 1;
                    }

                    // Apply thresholds and timer to level data
                    levelData.star_move_thresholds = result.StarThresholds;
                    levelData.time_limit_seconds = result.TimeLimitSeconds;

                    Debug.Log($"[LevelGenerator] Level {parameters.levelNumber} generated successfully in {attempt} attempt(s)");
                    Debug.Log($"  Solver: {solveResult.TotalMoves} moves, {solveResult.TotalMatches} matches");
                    Debug.Log($"  Stars: 3★≤{result.StarThresholds[0]}, 2★≤{result.StarThresholds[1]}, 1★≤{result.StarThresholds[2]}");
                    Debug.Log($"  Timer: {result.TimeLimitSeconds}s ({parameters.secondsPerItem:F1}s × {result.TotalItems} items)");

                    return result;
                }
                else
                {
                    result.FailureReason = solveResult.FailureReason;
                }
            }

            Debug.LogWarning($"[LevelGenerator] Failed to generate solvable level after {parameters.maxGenerationAttempts} attempts");
            result.Success = false;
            return result;
        }

        /// <summary>
        /// Create level data from parameters
        /// </summary>
        private LevelData CreateLevelData(LevelParams p)
        {
            var level = new LevelData
            {
                id = p.levelNumber,
                world_id = p.worldId,
                name = $"level_{p.levelNumber:D3}",
                containers = new List<ContainerDefinition>()
            };

            // Select item types for this level
            var itemPool = SelectItemTypes(p.worldId, p.itemTypeCount);
            if (itemPool.Count < p.itemTypeCount)
            {
                Debug.LogWarning($"[LevelGenerator] Only {itemPool.Count} items available, requested {p.itemTypeCount}");
            }

            // Calculate total containers
            int totalContainers = p.standardContainerCount + p.singleSlotContainerCount;
            if (p.trainCount > 0)
            {
                totalContainers += p.trainCount * p.containersPerTrain;
            }

            // Calculate positions for containers
            var positions = CalculateContainerPositions(p);

            // Track which containers will be locked
            var lockedIndices = SelectLockedContainers(totalContainers, p.lockedContainerCount, p.trainCount > 0);

            // Create container definitions
            int containerIndex = 0;

            // Standard containers (static)
            for (int i = 0; i < p.standardContainerCount; i++)
            {
                var container = CreateStandardContainer(containerIndex, positions[containerIndex], p, lockedIndices.Contains(containerIndex));
                level.containers.Add(container);
                containerIndex++;
            }

            // Single-slot containers (static)
            for (int i = 0; i < p.singleSlotContainerCount; i++)
            {
                var container = CreateSingleSlotContainer(containerIndex, positions[containerIndex], p, lockedIndices.Contains(containerIndex));
                level.containers.Add(container);
                containerIndex++;
            }

            // Train containers
            for (int t = 0; t < p.trainCount; t++)
            {
                var trainContainers = CreateTrainContainers(containerIndex, t, p, lockedIndices);
                level.containers.AddRange(trainContainers);
                containerIndex += trainContainers.Count;
            }

            // Distribute items across containers
            DistributeItems(level.containers, itemPool, p.emptySlots);

            return level;
        }

        /// <summary>
        /// Select random item types for this level
        /// </summary>
        private List<string> SelectItemTypes(string worldId, int count)
        {
            string[] allItems;
            switch (worldId.ToLower())
            {
                case "island":
                    allItems = IslandItems;
                    break;
                // Add other worlds here
                default:
                    allItems = IslandItems;
                    break;
            }

            // Shuffle and take requested count
            var shuffled = allItems.OrderBy(x => Random.value).ToList();
            return shuffled.Take(Mathf.Min(count, shuffled.Count)).ToList();
        }

        /// <summary>
        /// Calculate positions for all containers
        /// </summary>
        private List<Vector2> CalculateContainerPositions(LevelParams p)
        {
            var positions = new List<Vector2>();

            // Standard 3-column layout positions
            float[] xPositions = { 200, 540, 880 };
            float startY = 230;
            float rowSpacing = 250;

            int totalStatic = p.standardContainerCount + p.singleSlotContainerCount;
            int rows = Mathf.CeilToInt(totalStatic / 3f);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int index = row * 3 + col;
                    if (index >= totalStatic) break;

                    float y = startY + row * rowSpacing;
                    positions.Add(new Vector2(xPositions[col], y));
                }
            }

            // Train container positions (off-screen start positions handled by train config)
            float trainStartY = startY + rows * rowSpacing + 100;
            for (int t = 0; t < p.trainCount; t++)
            {
                for (int i = 0; i < p.containersPerTrain; i++)
                {
                    if (p.horizontalTrains)
                    {
                        // Horizontal train - containers spaced along X
                        float x = 540 + (i - p.containersPerTrain / 2) * 340;
                        positions.Add(new Vector2(x, trainStartY + t * 250));
                    }
                    else
                    {
                        // Vertical train - containers stacked along Y
                        float y = trainStartY + i * 200;
                        positions.Add(new Vector2(200 + t * 340, y));
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Select which container indices should be locked
        /// </summary>
        private HashSet<int> SelectLockedContainers(int totalContainers, int lockedCount, bool hasTrains)
        {
            var locked = new HashSet<int>();
            if (lockedCount <= 0) return locked;

            // Prefer locking containers in later positions (more strategic)
            var candidates = Enumerable.Range(1, totalContainers - 1).ToList();  // Don't lock first container
            candidates = candidates.OrderBy(x => Random.value).ToList();

            for (int i = 0; i < Mathf.Min(lockedCount, candidates.Count); i++)
            {
                locked.Add(candidates[i]);
            }

            return locked;
        }

        /// <summary>
        /// Create a standard 3-slot container
        /// </summary>
        private ContainerDefinition CreateStandardContainer(int index, Vector2 position, LevelParams p, bool isLocked)
        {
            var container = new ContainerDefinition
            {
                id = $"container_{index + 1}",
                position = new PositionData { x = position.x, y = position.y },
                container_type = "standard",
                container_image = $"{p.worldId}_container",
                slot_count = 3,
                max_rows_per_slot = p.standardMaxRows,
                is_locked = isLocked,
                initial_items = new List<ItemPlacement>()
            };

            if (isLocked)
            {
                container.unlock_matches_required = Random.Range(p.minUnlockMatches, p.maxUnlockMatches + 1);
                container.lock_overlay_image = $"{p.worldId}_lockoverlay";
            }

            return container;
        }

        /// <summary>
        /// Create a single-slot container (deep stack)
        /// </summary>
        private ContainerDefinition CreateSingleSlotContainer(int index, Vector2 position, LevelParams p, bool isLocked)
        {
            var container = new ContainerDefinition
            {
                id = $"container_{index + 1}",
                position = new PositionData { x = position.x, y = position.y },
                container_type = "single_slot",
                container_image = $"{p.worldId}_single_slot_container",
                slot_count = 1,
                max_rows_per_slot = p.singleSlotDepth,
                is_locked = isLocked,
                initial_items = new List<ItemPlacement>()
            };

            if (isLocked)
            {
                container.unlock_matches_required = Random.Range(p.minUnlockMatches, p.maxUnlockMatches + 1);
                container.lock_overlay_image = $"{p.worldId}_lockoverlay";
            }

            return container;
        }

        /// <summary>
        /// Create containers for a train (carousel)
        /// </summary>
        private List<ContainerDefinition> CreateTrainContainers(int startIndex, int trainIndex, LevelParams p, HashSet<int> lockedIndices)
        {
            var containers = new List<ContainerDefinition>();

            float trainSpacing = p.horizontalTrains ? 340 : 200;
            float moveDistance = trainSpacing * p.containersPerTrain;

            for (int i = 0; i < p.containersPerTrain; i++)
            {
                int index = startIndex + i;
                bool isLocked = lockedIndices.Contains(index);

                float x, y;
                if (p.horizontalTrains)
                {
                    x = 540 + (i - p.containersPerTrain / 2) * trainSpacing;
                    y = 800 + trainIndex * 250;  // Trains at bottom
                }
                else
                {
                    x = 200 + trainIndex * 340;
                    y = 300 + i * trainSpacing;
                }

                var container = new ContainerDefinition
                {
                    id = $"train{trainIndex + 1}_container_{i + 1}",
                    position = new PositionData { x = x, y = y },
                    container_type = "standard",
                    container_image = $"{p.worldId}_container",
                    slot_count = 3,
                    max_rows_per_slot = p.standardMaxRows,
                    is_locked = isLocked,
                    is_moving = true,
                    move_type = "carousel",
                    move_direction = p.horizontalTrains ? "right" : "down",
                    move_speed = 150,
                    move_distance = moveDistance,
                    initial_items = new List<ItemPlacement>()
                };

                if (isLocked)
                {
                    container.unlock_matches_required = Random.Range(p.minUnlockMatches, p.maxUnlockMatches + 1);
                    container.lock_overlay_image = $"{p.worldId}_lockoverlay";
                }

                containers.Add(container);
            }

            return containers;
        }

        /// <summary>
        /// Distribute items across containers to achieve target fill ratio
        /// </summary>
        private void DistributeItems(List<ContainerDefinition> containers, List<string> itemTypes, int targetEmptySlots)
        {
            // Calculate total front slots (these need some empty for maneuvering)
            int totalFrontSlots = 0;
            int totalBackSlots = 0;
            foreach (var container in containers)
            {
                totalFrontSlots += container.slot_count;
                totalBackSlots += container.slot_count * (container.max_rows_per_slot - 1);
            }

            // Ensure we have enough empty front slots for solvability
            // Rule: at least 1 empty front slot per 4 containers, minimum targetEmptySlots
            int minEmptyFrontSlots = Mathf.Max(targetEmptySlots, containers.Count / 4 + 2);
            int availableFrontSlots = totalFrontSlots - minEmptyFrontSlots;

            // Create item pool - exactly 3 of each item type
            var itemPool = new List<string>();
            foreach (var itemType in itemTypes)
            {
                itemPool.Add(itemType);
                itemPool.Add(itemType);
                itemPool.Add(itemType);
            }

            // Total items = itemTypes.Count * 3
            int totalItems = itemPool.Count;

            // Calculate how items should be distributed between front and back
            // Front row gets fewer items to ensure maneuverability
            int frontRowItems = Mathf.Min(availableFrontSlots, totalItems * 2 / 3);  // 2/3 in front max
            int backRowItems = totalItems - frontRowItems;

            // Adjust if we don't have enough back slots
            if (backRowItems > totalBackSlots)
            {
                backRowItems = totalBackSlots;
                frontRowItems = totalItems - backRowItems;
            }

            // Shuffle items
            itemPool = itemPool.OrderBy(x => Random.value).ToList();

            // Create placement lists
            var frontPlacements = new List<(int containerIdx, int slot)>();
            var backPlacements = new List<(int containerIdx, int slot, int row)>();

            // Collect all available slots
            for (int ci = 0; ci < containers.Count; ci++)
            {
                var container = containers[ci];
                for (int s = 0; s < container.slot_count; s++)
                {
                    frontPlacements.Add((ci, s));
                    for (int r = 1; r < container.max_rows_per_slot; r++)
                    {
                        backPlacements.Add((ci, s, r));
                    }
                }
            }

            // Shuffle placements
            frontPlacements = frontPlacements.OrderBy(x => Random.value).ToList();
            backPlacements = backPlacements.OrderBy(x => Random.value).ToList();

            int itemIndex = 0;

            // Place items in back rows first (they're harder to access)
            for (int i = 0; i < backRowItems && i < backPlacements.Count && itemIndex < itemPool.Count; i++)
            {
                var (ci, slot, row) = backPlacements[i];
                containers[ci].initial_items.Add(new ItemPlacement
                {
                    id = itemPool[itemIndex],
                    slot = slot,
                    row = row
                });
                itemIndex++;
            }

            // Place remaining items in front rows
            for (int i = 0; i < frontRowItems && i < frontPlacements.Count && itemIndex < itemPool.Count; i++)
            {
                var (ci, slot) = frontPlacements[i];
                containers[ci].initial_items.Add(new ItemPlacement
                {
                    id = itemPool[itemIndex],
                    slot = slot,
                    row = 0
                });
                itemIndex++;
            }

            // Verify total items is divisible by 3 (should always be true now)
            int totalPlaced = 0;
            foreach (var container in containers)
            {
                totalPlaced += container.initial_items.Count;
            }

            if (totalPlaced % 3 != 0)
            {
                Debug.LogWarning($"[LevelGenerator] Item count {totalPlaced} not divisible by 3, removing excess");
                int excess = totalPlaced % 3;
                for (int i = 0; i < excess; i++)
                {
                    for (int ci = containers.Count - 1; ci >= 0; ci--)
                    {
                        if (containers[ci].initial_items.Count > 0)
                        {
                            containers[ci].initial_items.RemoveAt(containers[ci].initial_items.Count - 1);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Count total items in a level
        /// </summary>
        private int CountTotalItems(LevelData level)
        {
            int count = 0;
            foreach (var container in level.containers)
            {
                if (container.initial_items != null)
                {
                    count += container.initial_items.Count;
                }
            }
            return count;
        }

        /// <summary>
        /// Calculate minimum moves needed for first match.
        /// Returns 0 if immediate match, 1 if one-move match, 2 if two-move match, or 999 if no quick match possible.
        /// </summary>
        private int GetMovesToFirstMatch(LevelData level)
        {
            // Build a quick state representation
            // For each unlocked container: track front row items and empty slots
            var containerFrontItems = new List<Dictionary<string, int>>();  // item type -> count
            var containerEmptySlots = new List<int>();
            var allAccessibleItems = new Dictionary<string, List<int>>();  // item type -> list of container indices

            for (int ci = 0; ci < level.containers.Count; ci++)
            {
                var container = level.containers[ci];
                var frontItems = new Dictionary<string, int>();
                int emptySlots = container.slot_count;

                if (container.initial_items != null)
                {
                    foreach (var item in container.initial_items)
                    {
                        if (item.row == 0)  // Front row only
                        {
                            if (!frontItems.ContainsKey(item.id))
                                frontItems[item.id] = 0;
                            frontItems[item.id]++;
                            emptySlots--;

                            // Track accessible items (only from unlocked containers)
                            if (!container.is_locked)
                            {
                                if (!allAccessibleItems.ContainsKey(item.id))
                                    allAccessibleItems[item.id] = new List<int>();
                                allAccessibleItems[item.id].Add(ci);
                            }
                        }
                    }
                }

                containerFrontItems.Add(frontItems);
                containerEmptySlots.Add(emptySlots);
            }

            // Check for immediate match (0 moves): any container with 3 matching items
            for (int ci = 0; ci < level.containers.Count; ci++)
            {
                foreach (var kvp in containerFrontItems[ci])
                {
                    if (kvp.Value >= 3)
                        return 0;
                }
            }

            // Check for 1-move match: container with 2 matching + empty slot, and 3rd accessible elsewhere
            for (int ci = 0; ci < level.containers.Count; ci++)
            {
                var container = level.containers[ci];
                if (container.is_locked) continue;
                if (containerEmptySlots[ci] < 1) continue;

                foreach (var kvp in containerFrontItems[ci])
                {
                    if (kvp.Value >= 2)
                    {
                        string itemType = kvp.Key;
                        // Check if there's a 3rd one accessible elsewhere
                        if (allAccessibleItems.ContainsKey(itemType))
                        {
                            foreach (int sourceCI in allAccessibleItems[itemType])
                            {
                                if (sourceCI != ci)
                                    return 1;  // Can complete match in 1 move
                            }
                        }
                    }
                }
            }

            // Check for 2-move match:
            // Case A: Container with 1 item, can receive 2 more of same type from other containers
            // Case B: Move an item to create a 2+empty situation, then complete with 1 more move
            for (int ci = 0; ci < level.containers.Count; ci++)
            {
                var container = level.containers[ci];
                if (container.is_locked) continue;
                if (containerEmptySlots[ci] < 2) continue;

                foreach (var kvp in containerFrontItems[ci])
                {
                    if (kvp.Value >= 1)
                    {
                        string itemType = kvp.Key;
                        // Count how many of this type are accessible elsewhere
                        int accessibleElsewhere = 0;
                        if (allAccessibleItems.ContainsKey(itemType))
                        {
                            foreach (int sourceCI in allAccessibleItems[itemType])
                            {
                                if (sourceCI != ci)
                                    accessibleElsewhere++;
                            }
                        }
                        if (accessibleElsewhere >= 2)
                            return 2;  // Can complete match in 2 moves
                    }
                }
            }

            // Case B: Any unlocked container with 2 empty slots can potentially collect 3 matching items
            for (int ci = 0; ci < level.containers.Count; ci++)
            {
                var container = level.containers[ci];
                if (container.is_locked) continue;
                if (containerEmptySlots[ci] < 3) continue;  // Need 3 empty to collect all 3

                // Check if any item type has 3 accessible copies
                foreach (var kvp in allAccessibleItems)
                {
                    if (kvp.Value.Count >= 3)
                    {
                        // Check that at least 3 are from different containers (can move them)
                        int movableCount = 0;
                        foreach (int sourceCI in kvp.Value)
                        {
                            if (sourceCI != ci)
                                movableCount++;
                        }
                        if (movableCount >= 3)
                            return 3;  // Could do it in 3 moves (still acceptable as fallback)
                    }
                }
            }

            return 999;  // No quick match possible
        }

        #endregion

        #region Batch Generation

        /// <summary>
        /// Generate multiple levels with progressive complexity
        /// </summary>
        public List<GenerationResult> GenerateLevelRange(string worldId, int startLevel, int endLevel)
        {
            var results = new List<GenerationResult>();

            for (int level = startLevel; level <= endLevel; level++)
            {
                var parameters = GetParametersForLevel(worldId, level);
                var result = GenerateLevel(parameters);
                results.Add(result);

                if (!result.Success)
                {
                    Debug.LogError($"[LevelGenerator] Failed to generate level {level}: {result.FailureReason}");
                }
            }

            return results;
        }

        /// <summary>
        /// Get parameters for a specific level number based on the complexity plan
        /// </summary>
        public static LevelParams GetParametersForLevel(string worldId, int level)
        {
            var p = new LevelParams
            {
                levelNumber = level,
                worldId = worldId,
                secondsPerItem = GetSecondsPerItem(level)
            };

            // Phase 1: Tutorial & Basics (1-10)
            if (level <= 10)
            {
                p.standardContainerCount = 3 + level;                    // 4-13 containers
                p.itemTypeCount = 2 + level;                             // 3-12 item types
                p.standardMaxRows = level <= 5 ? 1 : (level <= 8 ? 2 : 3);
                p.emptySlots = 3 + (level / 4);                          // 3-5 empty (more room for harder levels)
                p.maxGenerationAttempts = 50 + (level * 5);              // 55-100 attempts
            }
            // Phase 2: Locked Containers (11-25)
            else if (level <= 25)
            {
                int phase = level - 10;                                   // 1-15
                p.standardContainerCount = 12 + (phase / 3);             // 12-17 containers
                p.itemTypeCount = 8 + (phase / 2);                       // 8-15 item types
                p.standardMaxRows = 3;
                p.lockedContainerCount = 1 + (phase / 3);                // 1-6 locked
                p.minUnlockMatches = 2;
                p.maxUnlockMatches = 2 + (phase / 5);                    // 2-5 matches
                p.emptySlots = 4 + (phase / 5);                          // 4-7 empty
                p.maxGenerationAttempts = 100;
            }
            // Phase 3: Single-Slot Containers (26-45)
            else if (level <= 45)
            {
                int phase = level - 25;                                   // 1-20
                p.standardContainerCount = 12 - (phase / 4);             // 12-7 standard
                p.singleSlotContainerCount = 2 + (phase / 2);            // 2-12 single-slot
                p.singleSlotDepth = 3 + (phase / 4);                     // 3-8 deep
                p.itemTypeCount = 12 + (phase / 3);                      // 12-18 item types
                p.standardMaxRows = 3;
                p.lockedContainerCount = phase / 4;                      // 0-5 locked
                p.minUnlockMatches = 2;
                p.maxUnlockMatches = 4;
                p.emptySlots = 5;
                p.maxGenerationAttempts = 100;
            }
            // Phase 4: Carousel Trains (46-65)
            else if (level <= 65)
            {
                int phase = level - 45;                                   // 1-20
                p.standardContainerCount = 9 - (phase / 4);              // 9-4 standard
                p.trainCount = 1 + (phase / 5);                          // 1-4 trains
                p.containersPerTrain = 6 + (phase / 10);                 // 6-8 per train
                p.singleSlotContainerCount = phase / 4;                  // 0-5 single-slot
                p.singleSlotDepth = 4 + (phase / 5);                     // 4-8 deep
                p.itemTypeCount = 12 + (phase / 2);                      // 12-22 item types
                p.standardMaxRows = 3;
                p.lockedContainerCount = 2 + (phase / 3);                // 2-8 locked
                p.minUnlockMatches = 3;
                p.maxUnlockMatches = 5;
                p.emptySlots = 5;
                p.maxGenerationAttempts = 100;
            }
            // Phase 5: Expert Challenges (66-85)
            else if (level <= 85)
            {
                int phase = level - 65;                                   // 1-20
                p.standardContainerCount = 6 + (phase / 4);              // 6-11 standard
                p.trainCount = 2 + (phase / 5);                          // 2-6 trains
                p.containersPerTrain = 8;
                p.singleSlotContainerCount = 4 + (phase / 3);            // 4-10 single-slot
                p.singleSlotDepth = 6 + (phase / 5);                     // 6-10 deep
                p.itemTypeCount = 18 + (phase / 4);                      // 18-23 item types
                p.standardMaxRows = 3;
                p.lockedContainerCount = 4 + (phase / 3);                // 4-10 locked
                p.minUnlockMatches = 3;
                p.maxUnlockMatches = 5;
                p.emptySlots = 5;
                p.maxGenerationAttempts = 150;
            }
            // Phase 6: Master Levels (86-100)
            else
            {
                int phase = level - 85;                                   // 1-15
                p.standardContainerCount = 9 + (phase / 3);              // 9-14 standard
                p.trainCount = 4;                                         // 4 trains
                p.containersPerTrain = 9;
                p.singleSlotContainerCount = 9 + (phase / 3);            // 9-14 single-slot
                p.singleSlotDepth = 8 + (phase / 5);                     // 8-11 deep
                p.itemTypeCount = 20 + (phase / 3);                      // 20-25 item types
                p.standardMaxRows = 3;
                p.lockedContainerCount = 8 + (phase / 2);                // 8-15 locked
                p.minUnlockMatches = 4;
                p.maxUnlockMatches = 5;
                p.emptySlots = 5 + (phase / 3);                          // 5-10 empty (need more room)
                p.maxGenerationAttempts = 200;
            }

            return p;
        }

        #endregion
    }
}
