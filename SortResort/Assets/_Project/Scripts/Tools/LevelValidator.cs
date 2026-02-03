using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Validates level data for common issues like containers off-screen
    /// </summary>
    public static class LevelValidator
    {
        // Screen bounds in Godot coordinates (portrait 1080x1920)
        private const float SCREEN_WIDTH = 1080f;
        private const float SCREEN_HEIGHT = 1920f;
        private const float SCREEN_CENTER_X = 540f;
        private const float SCREEN_CENTER_Y = 600f;  // Y center for gameplay area

        // Unity camera settings (must match ScreenManager)
        // OrthoSize 9.6 shows full 1080px width: 1080/100 / (2 * 0.5625) = 9.6
        private const float CAMERA_ORTHO_SIZE = 9.6f;
        private const float REFERENCE_ASPECT = 0.5625f;  // 1080/1920
        private const float COORD_DIVISOR = 100f;  // Pixels to Unity units

        // Minimum spacing between container centers (in Godot pixels)
        private const float MIN_CONTAINER_SPACING = 20f;

        // Calculated visible area in Unity units
        private static float VisibleWidthUnits => 2f * CAMERA_ORTHO_SIZE * REFERENCE_ASPECT;  // = 9 units
        private static float VisibleHeightUnits => 2f * CAMERA_ORTHO_SIZE;  // = 16 units
        private static float VisibleHalfWidth => VisibleWidthUnits / 2f;  // = 4.5 units

        // Container dimensions (Godot pixels)
        // Base slot 83x166 scaled uniformly by 1.14, then container sprite adds 1.2x border
        // Total container width = 83 * 1.14 * 3 * 1.2 = 341px at positions 200, 540, 880
        // This maintains original 83% slot-to-container ratio
        private const float SLOT_WIDTH = 83f * 1.14f;   // ~95px (uniformly scaled)
        private const float SLOT_HEIGHT = 166f * 1.14f; // ~189px (uniformly scaled)
        private const float CONTAINER_BORDER_SCALE = 1.2f;  // Container sprite is 1.2x wider than slots

        // Edge margin - extra padding from screen edge for visual comfort (in Godot pixels)
        private const float EDGE_MARGIN_PIXELS = 15f;

        // UI reserved areas (in Godot pixels from edges)
        private const float TOP_UI_HEIGHT = 150f;   // HUD area at top
        private const float BOTTOM_UI_HEIGHT = 130f; // Bottom safe area

        // Calculate container width in UNITY units based on slot count
        private static float GetContainerWidthUnits(int slotCount)
        {
            // Container width = (slots * slot_width) * scale / divisor
            return (slotCount * SLOT_WIDTH * CONTAINER_BORDER_SCALE) / COORD_DIVISOR;
        }

        // Calculate safe X bounds for a container with given slot count
        // Based on actual camera visible area, not just screen pixel math
        private static float GetMinX(int slotCount)
        {
            float containerHalfWidthUnits = GetContainerWidthUnits(slotCount) / 2f;
            float edgeMarginUnits = EDGE_MARGIN_PIXELS / COORD_DIVISOR;

            // Container center must be far enough from edge that container fits
            // Min Unity X = -VisibleHalfWidth + containerHalfWidth + margin
            float minUnityX = -VisibleHalfWidth + containerHalfWidthUnits + edgeMarginUnits;

            // Convert back to Godot coords: GodotX = UnityX * 100 + 540
            return minUnityX * COORD_DIVISOR + SCREEN_CENTER_X;
        }

        private static float GetMaxX(int slotCount)
        {
            float containerHalfWidthUnits = GetContainerWidthUnits(slotCount) / 2f;
            float edgeMarginUnits = EDGE_MARGIN_PIXELS / COORD_DIVISOR;

            // Container center must be far enough from edge that container fits
            // Max Unity X = +VisibleHalfWidth - containerHalfWidth - margin
            float maxUnityX = VisibleHalfWidth - containerHalfWidthUnits - edgeMarginUnits;

            // Convert back to Godot coords: GodotX = UnityX * 100 + 540
            return maxUnityX * COORD_DIVISOR + SCREEN_CENTER_X;
        }

        // Y bounds (based on camera visible area)
        private static float GetMinY()
        {
            return TOP_UI_HEIGHT;
        }

        private static float GetMaxY()
        {
            // Based on camera visible height and gameplay area
            return SCREEN_HEIGHT - BOTTOM_UI_HEIGHT - (SLOT_HEIGHT / 2f);
        }

        public class ValidationResult
        {
            public bool IsValid = true;
            public List<string> Warnings = new List<string>();
            public List<string> Errors = new List<string>();

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();
                if (IsValid)
                {
                    sb.AppendLine("Level is VALID");
                }
                else
                {
                    sb.AppendLine("Level has ERRORS");
                }

                foreach (var error in Errors)
                {
                    sb.AppendLine($"  ERROR: {error}");
                }
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  WARNING: {warning}");
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Validate a level for common issues
        /// </summary>
        public static ValidationResult ValidateLevel(LevelData level)
        {
            var result = new ValidationResult();

            if (level == null)
            {
                result.IsValid = false;
                result.Errors.Add("Level data is null");
                return result;
            }

            if (level.containers == null || level.containers.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("Level has no containers");
                return result;
            }

            // Track item counts for triple-match validation
            var itemCounts = new Dictionary<string, int>();

            foreach (var container in level.containers)
            {
                // Skip moving containers - they can start off-screen
                if (container.is_moving)
                    continue;

                // Get slot count (default to 3 if not specified)
                int slotCount = container.slot_count > 0 ? container.slot_count : 3;

                // Calculate bounds for this container's size
                float minX = GetMinX(slotCount);
                float maxX = GetMaxX(slotCount);
                float minY = GetMinY();
                float maxY = GetMaxY();

                // Container width in Godot pixels for error messages
                float containerWidthPixels = GetContainerWidthUnits(slotCount) * COORD_DIVISOR;

                // Check X bounds
                if (container.position.x < minX)
                {
                    float leftEdge = container.position.x - (containerWidthPixels / 2f);
                    result.IsValid = false;
                    result.Errors.Add($"{container.id}: X={container.position.x} too far LEFT (min: {minX:F0}, left edge would be at {leftEdge:F0})");
                }
                else if (container.position.x > maxX)
                {
                    float rightEdge = container.position.x + (containerWidthPixels / 2f);
                    result.IsValid = false;
                    result.Errors.Add($"{container.id}: X={container.position.x} too far RIGHT (max: {maxX:F0}, right edge would be at {rightEdge:F0})");
                }

                // Check Y bounds
                if (container.position.y < minY)
                {
                    result.Warnings.Add($"{container.id}: Y={container.position.y} may be too HIGH (min: {minY:F0})");
                }
                else if (container.position.y > maxY)
                {
                    result.Warnings.Add($"{container.id}: Y={container.position.y} may be too LOW (max: {maxY:F0})");
                }

                // Count items
                if (container.initial_items != null)
                {
                    foreach (var item in container.initial_items)
                    {
                        if (!itemCounts.ContainsKey(item.id))
                            itemCounts[item.id] = 0;
                        itemCounts[item.id]++;
                    }
                }
            }

            // Validate item counts (should be multiples of 3 for triple-match)
            foreach (var kvp in itemCounts)
            {
                if (kvp.Value % 3 != 0)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Item '{kvp.Key}' has {kvp.Value} copies (not divisible by 3)");
                }
            }

            // Check for container overlaps (static containers only)
            var staticContainers = new List<ContainerDefinition>();
            foreach (var container in level.containers)
            {
                if (!container.is_moving)
                    staticContainers.Add(container);
            }

            for (int i = 0; i < staticContainers.Count; i++)
            {
                for (int j = i + 1; j < staticContainers.Count; j++)
                {
                    var c1 = staticContainers[i];
                    var c2 = staticContainers[j];

                    // Get container dimensions
                    int slots1 = c1.slot_count > 0 ? c1.slot_count : 3;
                    int slots2 = c2.slot_count > 0 ? c2.slot_count : 3;
                    float width1 = GetContainerWidthUnits(slots1) * COORD_DIVISOR;
                    float width2 = GetContainerWidthUnits(slots2) * COORD_DIVISOR;
                    float height = SLOT_HEIGHT;  // Approximate height

                    // Calculate bounding boxes (in Godot pixels)
                    float left1 = c1.position.x - width1 / 2f;
                    float right1 = c1.position.x + width1 / 2f;
                    float top1 = c1.position.y - height / 2f;
                    float bottom1 = c1.position.y + height / 2f;

                    float left2 = c2.position.x - width2 / 2f;
                    float right2 = c2.position.x + width2 / 2f;
                    float top2 = c2.position.y - height / 2f;
                    float bottom2 = c2.position.y + height / 2f;

                    // Check for overlap (with minimum spacing)
                    bool xOverlap = left1 < right2 + MIN_CONTAINER_SPACING && right1 > left2 - MIN_CONTAINER_SPACING;
                    bool yOverlap = top1 < bottom2 + MIN_CONTAINER_SPACING && bottom1 > top2 - MIN_CONTAINER_SPACING;

                    if (xOverlap && yOverlap)
                    {
                        result.Warnings.Add($"Containers '{c1.id}' and '{c2.id}' may overlap at ({c1.position.x},{c1.position.y}) and ({c2.position.x},{c2.position.y})");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Validate all levels in a world
        /// </summary>
        public static void ValidateWorld(string worldId, int maxLevel = 100)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"\n=== VALIDATING WORLD: {worldId.ToUpper()} ===\n");

            int validCount = 0;
            int invalidCount = 0;

            for (int i = 1; i <= maxLevel; i++)
            {
                var levelData = LevelDataLoader.LoadLevel(worldId, i);
                if (levelData == null)
                    break;

                var result = ValidateLevel(levelData);

                if (result.IsValid && result.Warnings.Count == 0)
                {
                    validCount++;
                }
                else
                {
                    if (!result.IsValid)
                        invalidCount++;

                    sb.AppendLine($"Level {i}:");
                    sb.AppendLine(result.ToString());
                }
            }

            sb.AppendLine($"\nSummary: {validCount} valid, {invalidCount} invalid");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Get recommended safe position bounds for level design
        /// </summary>
        public static string GetSafePositionGuide()
        {
            float width3Units = GetContainerWidthUnits(3);
            float width1Units = GetContainerWidthUnits(1);

            return $@"
=== SAFE CONTAINER POSITIONS (Godot Coordinates) ===

Screen: {SCREEN_WIDTH} x {SCREEN_HEIGHT} (portrait)
Camera visible area: {VisibleWidthUnits:F1} x {VisibleHeightUnits:F1} Unity units
Coordinate conversion: {COORD_DIVISOR} pixels = 1 Unity unit

Container Dimensions:
  - 1-slot: {width1Units:F2} Unity units wide ({width1Units * COORD_DIVISOR:F0} px equivalent)
  - 3-slot: {width3Units:F2} Unity units wide ({width3Units * COORD_DIVISOR:F0} px equivalent)

X Position (horizontal):
  For 3-SLOT containers:
    - Minimum: {GetMinX(3):F0} (left edge safe)
    - Center: {SCREEN_CENTER_X}
    - Maximum: {GetMaxX(3):F0} (right edge safe)
    - Standard 3-column: 200, 540, 880 (~30px edge margin, ~2px overlap)

  For 1-SLOT containers:
    - Minimum: {GetMinX(1):F0} (left edge safe)
    - Maximum: {GetMaxX(1):F0} (right edge safe)

Y Position (vertical):
  - Minimum: {GetMinY():F0} (top safe, below HUD)
  - Maximum: {GetMaxY():F0} (bottom safe)
  - Row spacing: ~250 pixels recommended

NOTE: Moving containers (carousel, etc.) CAN start off-screen.

OVERLAP PREVENTION:
  - Container width (3-slot): ~{GetContainerWidthUnits(3) * COORD_DIVISOR:F0}px
  - For ~2px overlap, spacing = container width - 2 = ~{GetContainerWidthUnits(3) * COORD_DIVISOR - 2:F0}px
  - Standard 3-column layout (200, 540, 880) provides {540 - 200}px spacing
";
        }
    }
}
