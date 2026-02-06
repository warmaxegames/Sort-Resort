using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    public class WorldProgressionManager : MonoBehaviour
    {
        public static WorldProgressionManager Instance { get; private set; }

        [Header("World Configuration")]
        [SerializeField] private List<WorldUnlockRequirement> worldUnlockRequirements = new List<WorldUnlockRequirement>();

        [Header("World Data References")]
        [SerializeField] private List<WorldData> allWorlds = new List<WorldData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDefaultRequirements();
        }

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
        }

        private void InitializeDefaultRequirements()
        {
            if (worldUnlockRequirements.Count == 0)
            {
                // Island is always unlocked (default world)
                worldUnlockRequirements.Add(new WorldUnlockRequirement
                {
                    worldId = "island",
                    isDefaultUnlocked = true
                });

                // Supermarket unlocks after 20 Island levels
                worldUnlockRequirements.Add(new WorldUnlockRequirement
                {
                    worldId = "supermarket",
                    requiredWorldId = "island",
                    requiredLevelsCompleted = 20
                });

                // Farm unlocks after 20 Supermarket levels
                worldUnlockRequirements.Add(new WorldUnlockRequirement
                {
                    worldId = "farm",
                    requiredWorldId = "supermarket",
                    requiredLevelsCompleted = 20
                });

                // Tavern unlocks after 20 Farm levels
                worldUnlockRequirements.Add(new WorldUnlockRequirement
                {
                    worldId = "tavern",
                    requiredWorldId = "farm",
                    requiredLevelsCompleted = 20
                });

                // Space unlocks after 20 Tavern levels
                worldUnlockRequirements.Add(new WorldUnlockRequirement
                {
                    worldId = "space",
                    requiredWorldId = "tavern",
                    requiredLevelsCompleted = 20
                });
            }
        }

        private void OnLevelCompleted(int levelNumber, int stars)
        {
            CheckForWorldUnlocks();
        }

        public void CheckForWorldUnlocks()
        {
            foreach (var requirement in worldUnlockRequirements)
            {
                if (requirement.isDefaultUnlocked) continue;
                if (SaveManager.Instance.IsWorldUnlocked(requirement.worldId)) continue;

                if (CanUnlockWorld(requirement))
                {
                    SaveManager.Instance.UnlockWorld(requirement.worldId);
                    Debug.Log($"[WorldProgressionManager] World unlocked: {requirement.worldId}");
                }
            }
        }

        public bool CanUnlockWorld(WorldUnlockRequirement requirement)
        {
            if (requirement.isDefaultUnlocked) return true;
            if (requirement.requiresPurchase) return false; // Requires IAP

            int completedLevels = SaveManager.Instance.GetWorldCompletedLevelCountAnyMode(requirement.requiredWorldId);
            return completedLevels >= requirement.requiredLevelsCompleted;
        }

        public bool IsWorldUnlocked(string worldId)
        {
            var requirement = worldUnlockRequirements.Find(r => r.worldId == worldId);
            if (requirement == null) return false;
            if (requirement.isDefaultUnlocked) return true;

            return SaveManager.Instance.IsWorldUnlocked(worldId);
        }

        public WorldUnlockStatus GetWorldUnlockStatus(string worldId)
        {
            var requirement = worldUnlockRequirements.Find(r => r.worldId == worldId);
            if (requirement == null)
            {
                return new WorldUnlockStatus { isUnlocked = false, requiresPurchase = true };
            }

            if (requirement.isDefaultUnlocked || SaveManager.Instance.IsWorldUnlocked(worldId))
            {
                return new WorldUnlockStatus { isUnlocked = true };
            }

            int currentProgress = SaveManager.Instance.GetWorldCompletedLevelCountAnyMode(requirement.requiredWorldId);

            return new WorldUnlockStatus
            {
                isUnlocked = false,
                requiresPurchase = requirement.requiresPurchase,
                requiredWorldId = requirement.requiredWorldId,
                requiredLevels = requirement.requiredLevelsCompleted,
                currentProgress = currentProgress
            };
        }

        public List<WorldData> GetAllWorlds()
        {
            return allWorlds;
        }

        public List<WorldData> GetUnlockedWorlds()
        {
            var unlocked = new List<WorldData>();
            foreach (var world in allWorlds)
            {
                if (IsWorldUnlocked(world.worldID))
                {
                    unlocked.Add(world);
                }
            }
            return unlocked;
        }

        public WorldData GetWorldData(string worldId)
        {
            return allWorlds.Find(w => w.worldID == worldId);
        }

        public int GetWorldDisplayOrder(string worldId)
        {
            for (int i = 0; i < worldUnlockRequirements.Count; i++)
            {
                if (worldUnlockRequirements[i].worldId == worldId)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    [System.Serializable]
    public class WorldUnlockRequirement
    {
        public string worldId;
        public bool isDefaultUnlocked;
        public bool requiresPurchase;
        public string requiredWorldId;
        public int requiredLevelsCompleted;
    }

    public struct WorldUnlockStatus
    {
        public bool isUnlocked;
        public bool requiresPurchase;
        public string requiredWorldId;
        public int requiredLevels;
        public int currentProgress;

        public float ProgressPercentage => requiredLevels > 0 ? (float)currentProgress / requiredLevels : 0f;
        public int LevelsRemaining => Mathf.Max(0, requiredLevels - currentProgress);
    }
}
