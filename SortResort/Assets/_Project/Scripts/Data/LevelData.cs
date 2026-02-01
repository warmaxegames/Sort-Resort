using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    [Serializable]
    public class LevelData
    {
        public int id;
        public string world_id;
        public string name;
        public int[] star_move_thresholds; // [3-star, 2-star, 1-star max moves]
        public int time_limit_seconds; // 0 = no time limit (optional timer feature)
        public List<ContainerDefinition> containers;
        public List<MovingTrackDefinition> moving_tracks;

        public int ThreeStarThreshold => star_move_thresholds != null && star_move_thresholds.Length > 0 ? star_move_thresholds[0] : 5;
        public int TwoStarThreshold => star_move_thresholds != null && star_move_thresholds.Length > 1 ? star_move_thresholds[1] : 10;
        public int OneStarThreshold => star_move_thresholds != null && star_move_thresholds.Length > 2 ? star_move_thresholds[2] : 15;
        public bool HasTimeLimit => time_limit_seconds > 0;
    }

    [Serializable]
    public class ContainerDefinition
    {
        public string id;
        public PositionData position;
        public string container_type; // "standard" or "single_slot"
        public string container_image;
        public int slot_count = 3;
        public int max_rows_per_slot = 4;
        public bool is_locked;
        public int unlock_matches_required;
        public string lock_overlay_image;
        public string unlock_animation;

        // Movement settings
        public bool is_moving;
        public string move_type; // "carousel", "back_and_forth"
        public string move_direction; // "left", "right", "up", "down"
        public float move_speed = 50f;
        public float move_distance = 200f;
        public string track_id;

        // Falling settings
        public bool is_falling;
        public float fall_speed = 100f;
        public float fall_target_y;
        public bool despawn_on_match;

        public List<ItemPlacement> initial_items;
    }

    [Serializable]
    public class PositionData
    {
        public float x;
        public float y;

        public Vector2 ToVector2() => new Vector2(x, y);
        public Vector3 ToVector3() => new Vector3(x, y, 0);
    }

    [Serializable]
    public class ItemPlacement
    {
        public string id; // Item ID from ItemData
        public int row;   // 0 = front row, 1+ = back rows
        public int slot;  // Which slot in the container (0, 1, 2)
    }

    [Serializable]
    public class MovingTrackDefinition
    {
        public string id;
        public string direction; // "left", "right", "up", "down"
        public float speed = 80f;
        public PositionData spawn_position;
        public float track_length;
        public float container_gap = 120f;
        public List<string> container_ids; // References to container definitions
    }

    // Wrapper class for JSON array parsing
    [Serializable]
    public class LevelDataWrapper
    {
        public LevelData level;
    }

    public static class LevelDataLoader
    {
        public static LevelData LoadFromJSON(string jsonContent)
        {
            try
            {
                // Try direct parse first
                var levelData = JsonUtility.FromJson<LevelData>(jsonContent);
                if (levelData != null && levelData.id > 0)
                {
                    return levelData;
                }

                // Try wrapped parse
                var wrapper = JsonUtility.FromJson<LevelDataWrapper>(jsonContent);
                return wrapper?.level;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelDataLoader] Failed to parse level JSON: {e.Message}");
                return null;
            }
        }

        public static LevelData LoadFromResources(string path)
        {
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                Debug.LogError($"[LevelDataLoader] Level file not found: {path}");
                return null;
            }

            return LoadFromJSON(textAsset.text);
        }

        public static LevelData LoadLevel(string worldId, int levelNumber)
        {
            string levelNum = levelNumber.ToString("D3");
            // Capitalize world name for folder (island -> Island)
            string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1).ToLower();
            string path = $"Data/Levels/{worldFolder}/level_{levelNum}";
            return LoadFromResources(path);
        }
    }
}
