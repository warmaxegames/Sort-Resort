using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Manages level loading, item spawning, match tracking, and completion checking.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject containerPrefab;
        [SerializeField] private GameObject singleSlotContainerPrefab;

        [Header("Parents")]
        [SerializeField] private Transform containersParent;
        [SerializeField] private Transform floatingItemsParent;

        [Header("Item Pool")]
        [SerializeField] private int initialPoolSize = 50;
        [SerializeField] private int maxPoolSize = 100;

        // Level data
        private LevelData currentLevel;
        private string currentWorldId;
        private int currentLevelNumber;

        // Containers
        private List<ItemContainer> containers = new List<ItemContainer>();

        // Item pool
        private ObjectPool<Item> itemPool;

        // Item database (loaded from JSON)
        private Dictionary<string, ItemDefinition> itemDatabase = new Dictionary<string, ItemDefinition>();
        private Dictionary<string, Sprite> itemSprites = new Dictionary<string, Sprite>();

        // Tracking
        private int totalItemsAtStart;
        private int itemsRemaining;
        private int matchesMade;

        // Star thresholds
        private int[] starThresholds;

        // Properties
        public LevelData CurrentLevel => currentLevel;
        public int ItemsRemaining => itemsRemaining;
        public int MatchesMade => matchesMade;

        // Events
        public event Action OnLevelLoaded;
        public event Action OnLevelCleared;
        public event Action<int> OnItemsRemainingChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create parents if not assigned
            if (containersParent == null)
            {
                var go = new GameObject("Containers");
                containersParent = go.transform;
            }

            if (floatingItemsParent == null)
            {
                var go = new GameObject("FloatingItems");
                floatingItemsParent = go.transform;
            }
        }

        private void Start()
        {
            LoadItemDatabase();
            InitializeItemPool();
        }

        #region Database Loading

        /// <summary>
        /// Load item definitions from JSON
        /// </summary>
        private void LoadItemDatabase()
        {
            var textAsset = Resources.Load<TextAsset>("Data/items");
            if (textAsset == null)
            {
                Debug.LogError("[LevelManager] Failed to load items.json");
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<ItemDatabaseWrapper>(textAsset.text);
                if (wrapper != null && wrapper.items != null)
                {
                    foreach (var item in wrapper.items)
                    {
                        itemDatabase[item.id] = item;
                    }
                }
                Debug.Log($"[LevelManager] Loaded {itemDatabase.Count} item definitions");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelManager] Failed to parse items.json: {e.Message}");
            }
        }

        /// <summary>
        /// Get or load a sprite for an item
        /// </summary>
        private Sprite GetItemSprite(string itemId, string worldId)
        {
            string cacheKey = $"{worldId}/{itemId}";

            if (itemSprites.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // Try to load from Resources
            string path = $"Art/Items/{char.ToUpper(worldId[0]) + worldId.Substring(1)}/{itemId}";
            var sprite = Resources.Load<Sprite>(path);

            if (sprite == null)
            {
                // Try alternate path
                path = $"Items/{worldId}/{itemId}";
                sprite = Resources.Load<Sprite>(path);
            }

            if (sprite != null)
            {
                itemSprites[cacheKey] = sprite;
            }
            else
            {
                Debug.LogWarning($"[LevelManager] Failed to load sprite: {path}");
            }

            return sprite;
        }

        #endregion

        #region Item Pool

        private void InitializeItemPool()
        {
            if (itemPrefab == null)
            {
                Debug.LogError("[LevelManager] Item prefab not assigned!");
                return;
            }

            itemPool = new ObjectPool<Item>(
                itemPrefab.GetComponent<Item>(),
                floatingItemsParent,
                initialPoolSize,
                maxPoolSize,
                onGet: item => item.gameObject.SetActive(true),
                onRelease: item =>
                {
                    item.ResetState();
                    item.gameObject.SetActive(false);
                }
            );

            Debug.Log($"[LevelManager] Item pool initialized with {initialPoolSize} items");
        }

        /// <summary>
        /// Get an item from the pool
        /// </summary>
        public Item GetItemFromPool(string itemId, string worldId)
        {
            var item = itemPool?.Get();
            if (item == null)
            {
                Debug.LogError("[LevelManager] Failed to get item from pool!");
                return null;
            }

            // Configure the item
            var sprite = GetItemSprite(itemId, worldId);
            item.Configure(itemId, sprite);
            item.SetPoolCallback(ReturnItemToPool);

            return item;
        }

        /// <summary>
        /// Return an item to the pool
        /// </summary>
        public void ReturnItemToPool(Item item)
        {
            if (item == null) return;

            itemPool?.Release(item);
            UpdateItemsRemaining();
        }

        #endregion

        #region Level Loading

        /// <summary>
        /// Load a level by world and number
        /// </summary>
        public void LoadLevel(string worldId, int levelNumber)
        {
            currentWorldId = worldId;
            currentLevelNumber = levelNumber;

            // Clear previous level
            ClearLevel();

            // Load level data
            currentLevel = LevelDataLoader.LoadLevel(worldId, levelNumber);
            if (currentLevel == null)
            {
                Debug.LogError($"[LevelManager] Failed to load level {worldId}/{levelNumber}");
                return;
            }

            // Store star thresholds
            starThresholds = currentLevel.star_move_thresholds;

            // Create containers
            CreateContainers();

            // Spawn initial items
            SpawnInitialItems();

            // Calculate total items
            totalItemsAtStart = CountTotalItems();
            itemsRemaining = totalItemsAtStart;
            matchesMade = 0;

            Debug.Log($"[LevelManager] Level loaded: {worldId} #{levelNumber}, {totalItemsAtStart} items");

            OnLevelLoaded?.Invoke();
            GameEvents.InvokeLevelStarted(levelNumber);
        }

        /// <summary>
        /// Clear current level
        /// </summary>
        public void ClearLevel()
        {
            // Return all items to pool
            foreach (var container in containers)
            {
                if (container != null)
                {
                    Destroy(container.gameObject);
                }
            }
            containers.Clear();

            // Release all pooled items
            itemPool?.ReleaseAll();

            currentLevel = null;
            itemsRemaining = 0;
            matchesMade = 0;
        }

        /// <summary>
        /// Create containers from level data
        /// </summary>
        private void CreateContainers()
        {
            if (currentLevel.containers == null) return;

            foreach (var containerDef in currentLevel.containers)
            {
                CreateContainer(containerDef);
            }
        }

        /// <summary>
        /// Create a single container
        /// </summary>
        private ItemContainer CreateContainer(ContainerDefinition definition)
        {
            // Choose prefab based on type
            var prefab = definition.container_type == "single_slot"
                ? singleSlotContainerPrefab
                : containerPrefab;

            if (prefab == null)
            {
                prefab = containerPrefab;
            }

            // Instantiate
            var go = Instantiate(prefab, containersParent);
            var container = go.GetComponent<ItemContainer>();

            if (container == null)
            {
                container = go.AddComponent<ItemContainer>();
            }

            // Initialize
            container.Initialize(definition);

            // Subscribe to events
            container.OnItemsMatched += OnContainerItemsMatched;
            container.OnContainerEmpty += OnContainerBecameEmpty;

            containers.Add(container);

            return container;
        }

        /// <summary>
        /// Spawn initial items in containers
        /// </summary>
        private void SpawnInitialItems()
        {
            if (currentLevel.containers == null) return;

            for (int c = 0; c < currentLevel.containers.Count && c < containers.Count; c++)
            {
                var containerDef = currentLevel.containers[c];
                var container = containers[c];

                if (containerDef.initial_items == null) continue;

                foreach (var placement in containerDef.initial_items)
                {
                    var item = GetItemFromPool(placement.id, currentWorldId);
                    if (item != null)
                    {
                        container.PlaceItemInRow(item, placement.slot, placement.row);
                    }
                }
            }
        }

        #endregion

        #region Match & Completion Tracking

        private void OnContainerItemsMatched(ItemContainer container, string itemId, int count)
        {
            matchesMade++;
            UpdateItemsRemaining();

            Debug.Log($"[LevelManager] Match! {count}x {itemId}, total matches: {matchesMade}");

            // Increment match count in GameManager
            GameManager.Instance?.IncrementMatchCount(itemId);
        }

        private void OnContainerBecameEmpty(ItemContainer container)
        {
            Debug.Log($"[LevelManager] Container {container.ContainerId} is now empty");
            CheckLevelComplete();
        }

        private void UpdateItemsRemaining()
        {
            int count = CountTotalItems();

            if (count != itemsRemaining)
            {
                itemsRemaining = count;
                OnItemsRemainingChanged?.Invoke(itemsRemaining);

                if (itemsRemaining == 0)
                {
                    CheckLevelComplete();
                }
            }
        }

        private int CountTotalItems()
        {
            int count = 0;
            foreach (var container in containers)
            {
                if (container != null)
                {
                    count += container.GetTotalItemCount();
                }
            }
            return count;
        }

        /// <summary>
        /// Check if level is complete (all items cleared)
        /// </summary>
        private void CheckLevelComplete()
        {
            if (itemsRemaining > 0) return;

            // All items cleared!
            Debug.Log("[LevelManager] Level complete!");

            OnLevelCleared?.Invoke();

            // Calculate stars
            int movesUsed = GameManager.Instance?.CurrentMoveCount ?? 0;
            int stars = CalculateStars(movesUsed);

            // Complete the level
            GameManager.Instance?.CompleteLevel(stars);
        }

        /// <summary>
        /// Calculate star rating based on moves
        /// </summary>
        public int CalculateStars(int movesUsed)
        {
            if (starThresholds == null || starThresholds.Length < 3)
                return 1;

            if (movesUsed <= starThresholds[0]) return 3;
            if (movesUsed <= starThresholds[1]) return 2;
            if (movesUsed <= starThresholds[2]) return 1;

            return 0; // Failed
        }

        /// <summary>
        /// Check if player has failed (no valid moves remaining)
        /// </summary>
        public void CheckForFailure(int maxMoves)
        {
            int movesUsed = GameManager.Instance?.CurrentMoveCount ?? 0;

            if (movesUsed >= maxMoves && itemsRemaining > 0)
            {
                Debug.Log("[LevelManager] Level failed - out of moves!");
                GameManager.Instance?.FailLevel();
            }
        }

        #endregion

        #region Public Utilities

        /// <summary>
        /// Get container by ID
        /// </summary>
        public ItemContainer GetContainer(string containerId)
        {
            return containers.Find(c => c.ContainerId == containerId);
        }

        /// <summary>
        /// Get all containers
        /// </summary>
        public List<ItemContainer> GetAllContainers()
        {
            return new List<ItemContainer>(containers);
        }

        /// <summary>
        /// Restart current level
        /// </summary>
        public void RestartLevel()
        {
            if (!string.IsNullOrEmpty(currentWorldId) && currentLevelNumber > 0)
            {
                LoadLevel(currentWorldId, currentLevelNumber);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from container events
            foreach (var container in containers)
            {
                if (container != null)
                {
                    container.OnItemsMatched -= OnContainerItemsMatched;
                    container.OnContainerEmpty -= OnContainerBecameEmpty;
                }
            }

            itemPool?.Clear();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    // Helper classes for JSON parsing
    [Serializable]
    public class ItemDatabaseWrapper
    {
        public List<ItemDefinition> items;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string sprite;
        public string world;
        public int unlock_level;
    }
}
