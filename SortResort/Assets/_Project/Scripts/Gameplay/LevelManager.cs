using System;
using System.Collections;
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
        [SerializeField] private int initialPoolSize = 120;
        [SerializeField] private int maxPoolSize = 250;

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

        // Background
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer colorBackgroundRenderer;
        private Coroutine colorFadeCoroutine;

        // Tracking
        private int totalItemsAtStart;
        private int itemsRemaining;
        private int matchesMade;

        // Star thresholds
        private int[] starThresholds;

        // Undo system
        private Stack<MoveRecord> moveHistory = new Stack<MoveRecord>();
        private bool canUndo = false;

        // Move recording for comparison with solver
        private bool isRecording = false;
        private List<MoveRecord> recordedMoves = new List<MoveRecord>();
        public bool IsRecording => isRecording;

        // Track ALL moves for solver alert (separate from undo history which clears on match)
        private List<SimpleMoveRecord> allMovesThisLevel = new List<SimpleMoveRecord>();

        // Timer system
        private float timeRemaining;
        private float totalTimeLimit;
        private bool timerActive = false;
        private bool timerFrozen = false;

        // Elapsed time tracking (all modes, for recording completion time)
        private float elapsedTime = 0f;

        // Properties
        public LevelData CurrentLevel => currentLevel;
        public string CurrentWorldId => currentWorldId;
        public int CurrentLevelNumber => currentLevelNumber;
        public float TimeRemaining => timeRemaining;
        public float TotalTimeLimit => totalTimeLimit;
        public float ElapsedTime => elapsedTime;
        public bool IsTimerActive => timerActive;
        public bool IsTimerFrozen => timerFrozen;
        public int ItemsRemaining => itemsRemaining;
        public int MatchesMade => matchesMade;
        public bool CanUndo => canUndo && moveHistory.Count > 0;

        // Events
        public event Action OnLevelLoaded;
        public event Action OnLevelCleared;
        public event Action<int> OnItemsRemainingChanged;
        public event Action<bool> OnUndoAvailableChanged;
        public event Action<float> OnTimerTick; // Fires every second with time remaining

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create game world parent for responsive scaling
            var gameWorldGO = new GameObject("--- GAME WORLD ---");

            // Create parents if not assigned
            if (containersParent == null)
            {
                var go = new GameObject("Containers");
                go.transform.SetParent(gameWorldGO.transform);
                containersParent = go.transform;
            }

            if (floatingItemsParent == null)
            {
                var go = new GameObject("FloatingItems");
                go.transform.SetParent(gameWorldGO.transform);
                floatingItemsParent = go.transform;
            }

            // Create background sprite renderer (behind everything)
            var backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(gameWorldGO.transform);
            backgroundGO.transform.localPosition = new Vector3(0, 0, 10f); // Behind everything
            backgroundRenderer = backgroundGO.AddComponent<SpriteRenderer>();
            backgroundRenderer.sortingOrder = -100; // Far back

            // Create color background overlay (slightly in front of mono, starts invisible)
            var colorBgGO = new GameObject("BackgroundColor");
            colorBgGO.transform.SetParent(gameWorldGO.transform);
            colorBgGO.transform.localPosition = new Vector3(0, 0, 9f);
            colorBackgroundRenderer = colorBgGO.AddComponent<SpriteRenderer>();
            colorBackgroundRenderer.sortingOrder = -99;
            colorBackgroundRenderer.color = new Color(1f, 1f, 1f, 0f);

            // Register with ScreenManager for responsive scaling
            StartCoroutine(RegisterWithScreenManager(gameWorldGO.transform));
        }

        private System.Collections.IEnumerator RegisterWithScreenManager(Transform gameWorldParent)
        {
            // Wait for ScreenManager to be available
            yield return null;

            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.SetGameWorldParent(gameWorldParent);
                Debug.Log("[LevelManager] Registered game world with ScreenManager");
            }
        }

        private void Start()
        {
            LoadItemDatabase();
            InitializeItemPool();

            // Subscribe to game events for timer pause/resume
            GameEvents.OnGamePaused += OnGamePaused;
            GameEvents.OnGameResumed += OnGameResumed;
        }

        private void Update()
        {
            // Track elapsed time in all modes (for recording completion time)
            if (GameManager.Instance?.CurrentState == GameState.Playing)
            {
                if (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive)
                {
                    elapsedTime += Time.deltaTime;
                }
            }

            UpdateTimer();
        }

        private void OnGamePaused()
        {
            // Timer automatically pauses when Time.timeScale = 0
            // but we track state for UI purposes
        }

        private void OnGameResumed()
        {
            // Timer automatically resumes when Time.timeScale = 1
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

            // Capitalize world name for folder (island -> Island)
            string worldFolder = char.ToUpper(worldId[0]) + worldId.Substring(1).ToLower();

            // Try paths in order of likelihood
            string[] paths = {
                $"Sprites/Items/{worldFolder}/{itemId}",      // Resources/Sprites/Items/Island/coconut
                $"Items/{worldFolder}/{itemId}",              // Resources/Items/Island/coconut
            };

            Sprite sprite = null;
            foreach (var path in paths)
            {
                sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    Debug.Log($"[LevelManager] Loaded sprite from: {path}");
                    break;
                }
            }

            if (sprite != null)
            {
                itemSprites[cacheKey] = sprite;
            }
            else
            {
                Debug.LogWarning($"[LevelManager] Failed to load sprite for {itemId} in world {worldId}. Tried paths: {string.Join(", ", paths)}");
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

            // Ensure item is on correct layer for raycasting
            item.gameObject.layer = 6; // Items layer

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

            // Reset combo streak tracking
            ComboTracker.Reset();

            // Load level data
            currentLevel = LevelDataLoader.LoadLevel(worldId, levelNumber);
            if (currentLevel == null)
            {
                Debug.LogError($"[LevelManager] Failed to load level {worldId}/{levelNumber}");
                return;
            }

            // Load world background and music
            string bgWorldId = !string.IsNullOrEmpty(currentLevel.world_id) ? currentLevel.world_id : worldId;
            LoadWorldBackground(bgWorldId);

            // Play world-specific gameplay audio (music + ambient)
            // This will only restart if the world changes
            AudioManager.Instance?.PlayWorldGameplayAudio(bgWorldId);

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
            elapsedTime = 0f;

            // Initialize timer if level has time limit
            InitializeTimer();

            Debug.Log($"[LevelManager] Level loaded: {worldId} #{levelNumber}, {totalItemsAtStart} items, timer: {(timerActive ? $"{totalTimeLimit}s" : "disabled")}");

            OnLevelLoaded?.Invoke();
            GameEvents.InvokeLevelStarted(levelNumber);
        }

        /// <summary>
        /// Initialize timer for current level
        /// </summary>
        private void InitializeTimer()
        {
            // Timer is active only in TimerMode and HardMode
            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            bool timerActiveForMode = (mode == GameMode.TimerMode || mode == GameMode.HardMode);

            if (timerActiveForMode && currentLevel != null)
            {
                // Use level's time limit, or estimate from fail threshold if not set
                if (currentLevel.HasTimeLimit)
                {
                    totalTimeLimit = currentLevel.time_limit_seconds;
                }
                else
                {
                    // Runtime fallback: ~6 seconds per optimal move
                    totalTimeLimit = currentLevel.FailThreshold * 6;
                }

                timeRemaining = totalTimeLimit;
                timerActive = true;
                timerFrozen = false;

                // Fire initial timer event
                GameEvents.InvokeTimerUpdated(timeRemaining);
            }
            else
            {
                totalTimeLimit = 0;
                timeRemaining = 0;
                timerActive = false;
                timerFrozen = false;
            }
        }

        /// <summary>
        /// Update timer countdown
        /// </summary>
        private void UpdateTimer()
        {
            if (!timerActive || timerFrozen) return;

            // Only update if game is playing
            if (GameManager.Instance?.CurrentState != GameState.Playing) return;

            // Pause timer while dialogue is active
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

            // Countdown
            timeRemaining -= Time.deltaTime;

            // Fire timer update event
            GameEvents.InvokeTimerUpdated(timeRemaining);
            OnTimerTick?.Invoke(timeRemaining);

            // Check for timer expiration
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                timerActive = false;
                OnTimerExpired();
            }
        }

        /// <summary>
        /// Called when timer reaches zero
        /// </summary>
        private void OnTimerExpired()
        {
            Debug.Log("[LevelManager] Timer expired - level failed!");
            GameEvents.InvokeTimerExpired();
            GameManager.Instance?.FailLevel("Out of Time");
        }

        /// <summary>
        /// Freeze the timer (for timer freeze power-up)
        /// </summary>
        public void FreezeTimer(float duration)
        {
            if (!timerActive) return;

            StartCoroutine(FreezeTimerCoroutine(duration));
        }

        private System.Collections.IEnumerator FreezeTimerCoroutine(float duration)
        {
            timerFrozen = true;
            GameEvents.InvokeTimerFrozen(true);
            Debug.Log($"[LevelManager] Timer frozen for {duration}s");

            yield return new WaitForSeconds(duration);

            timerFrozen = false;
            GameEvents.InvokeTimerFrozen(false);
            Debug.Log("[LevelManager] Timer unfrozen");
        }

        /// <summary>
        /// Add time to the timer (for time bonus power-up)
        /// </summary>
        public void AddTime(float seconds)
        {
            if (!timerActive) return;

            timeRemaining += seconds;
            if (timeRemaining > totalTimeLimit)
            {
                timeRemaining = totalTimeLimit; // Cap at original time
            }

            GameEvents.InvokeTimerUpdated(timeRemaining);
            Debug.Log($"[LevelManager] Added {seconds}s to timer, now {timeRemaining}s");
        }

        /// <summary>
        /// Load and display the background for a world
        /// </summary>
        private void LoadWorldBackground(string worldId)
        {
            if (backgroundRenderer == null) return;

            // Try to load world-specific background, fall back to base
            string[] backgroundPaths = new string[]
            {
                $"Sprites/Backgrounds/{worldId}_background",
                "Sprites/Backgrounds/basebackground"
            };

            Sprite bgSprite = null;
            foreach (var path in backgroundPaths)
            {
                bgSprite = Resources.Load<Sprite>(path);
                if (bgSprite != null)
                {
                    Debug.Log($"[LevelManager] Loaded background: {path}");
                    break;
                }
            }

            if (bgSprite != null)
            {
                backgroundRenderer.sprite = bgSprite;

                // Scale background to fill screen
                // Get camera bounds and scale sprite to fit
                if (Camera.main != null)
                {
                    float camHeight = Camera.main.orthographicSize * 2f;
                    float camWidth = camHeight * Camera.main.aspect;

                    float spriteHeight = bgSprite.bounds.size.y;
                    float spriteWidth = bgSprite.bounds.size.x;

                    // Scale to cover entire screen (use max scale to ensure no gaps)
                    float scaleX = camWidth / spriteWidth;
                    float scaleY = camHeight / spriteHeight;
                    float scale = Mathf.Max(scaleX, scaleY) * 1.1f; // 10% extra to ensure coverage

                    backgroundRenderer.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            else
            {
                Debug.LogWarning($"[LevelManager] No background found for world: {worldId}");
            }

            // Try to load color variant for progressive reveal effect
            if (colorBackgroundRenderer != null)
            {
                Sprite colorSprite = Resources.Load<Sprite>($"Sprites/Backgrounds/{worldId}_background_color");
                if (colorSprite != null)
                {
                    colorBackgroundRenderer.sprite = colorSprite;
                    colorBackgroundRenderer.color = new Color(1f, 1f, 1f, 0f);
                    colorBackgroundRenderer.enabled = true;

                    // Apply same scale as mono background
                    colorBackgroundRenderer.transform.localScale = backgroundRenderer.transform.localScale;

                    Debug.Log($"[LevelManager] Loaded color background for progressive reveal: {worldId}");
                }
                else
                {
                    colorBackgroundRenderer.enabled = false;
                }
            }
        }

        /// <summary>
        /// Clear current level
        /// </summary>
        public void ClearLevel()
        {
            // Clear static stack container tracking
            ContainerMovement.ClearAllStackContainers();

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

            // Clear undo history and move tracking
            moveHistory.Clear();
            allMovesThisLevel.Clear();
            UpdateUndoAvailable(false);

            // Reset recording state
            if (isRecording)
            {
                Debug.Log("[LevelManager] Recording stopped due to level change");
                isRecording = false;
            }
            recordedMoves.Clear();

            // Reset color background overlay
            if (colorFadeCoroutine != null)
            {
                StopCoroutine(colorFadeCoroutine);
                colorFadeCoroutine = null;
            }
            if (colorBackgroundRenderer != null)
            {
                colorBackgroundRenderer.color = new Color(1f, 1f, 1f, 0f);
            }

            // Reset timer
            timerActive = false;
            timerFrozen = false;
            timeRemaining = 0;
            totalTimeLimit = 0;

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

            // Add ContainerMovement if needed (for moving, falling, or despawn-on-match containers)
            if (definition.is_moving || definition.is_falling || definition.despawn_on_match)
            {
                var movement = go.GetComponent<ContainerMovement>();
                if (movement == null)
                {
                    movement = go.AddComponent<ContainerMovement>();
                }
                movement.Initialize(definition);
                Debug.Log($"[LevelManager] Added ContainerMovement to {definition.id}, despawn_on_match={definition.despawn_on_match}");
            }

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

                // Get slot size from container for scaling
                var slotSize = container.SlotSizeUnits;

                foreach (var placement in containerDef.initial_items)
                {
                    // Use the level's world_id for sprite loading (not the folder name)
                    string spriteWorldId = !string.IsNullOrEmpty(currentLevel.world_id)
                        ? currentLevel.world_id
                        : currentWorldId;

                    var item = GetItemFromPool(placement.id, spriteWorldId);
                    if (item != null)
                    {
                        // Scale item to fit slot before placing
                        item.ScaleToFitSlot(slotSize.x, slotSize.y);
                        container.PlaceItemInRow(item, placement.slot, placement.row);
                    }
                }
            }

            // Advance rows on all containers in case any have empty front rows
            // (generator post-processing may move front items to deeper rows)
            foreach (var container in containers)
            {
                container.CheckAndAdvanceAllRows();
            }
        }

        #endregion

        #region Match & Completion Tracking

        private void OnContainerItemsMatched(ItemContainer container, string itemId, int count)
        {
            matchesMade++;
            // NOTE: Don't call UpdateItemsRemaining() here - it's called from ReturnItemToPool()
            // after the match animation completes. Calling it here triggers level complete
            // while items are still animating on screen.

            Debug.Log($"[LevelManager] Match! {count}x {itemId}, total matches: {matchesMade}");

            // Clear undo history - can't undo after a match
            ClearMoveHistory();

            // Increment match count in GameManager
            GameManager.Instance?.IncrementMatchCount(itemId);

            // Notify ALL locked containers about the match (for unlock countdown)
            foreach (var c in containers)
            {
                if (c != null && c.IsLocked)
                {
                    c.IncrementUnlockProgress();
                }
            }

            // Progressive color reveal on match
            if (colorBackgroundRenderer != null && colorBackgroundRenderer.enabled)
            {
                int totalMatches = totalItemsAtStart / 3;
                if (totalMatches > 0)
                {
                    float targetAlpha = Mathf.Clamp01(matchesMade / (float)totalMatches);
                    if (colorFadeCoroutine != null) StopCoroutine(colorFadeCoroutine);
                    colorFadeCoroutine = StartCoroutine(FadeColorBackground(targetAlpha, 0.3f));
                }
            }
        }

        private System.Collections.IEnumerator FadeColorBackground(float targetAlpha, float duration)
        {
            Color color = colorBackgroundRenderer.color;
            float startAlpha = color.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                colorBackgroundRenderer.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            colorBackgroundRenderer.color = color;
            colorFadeCoroutine = null;
        }

        private void OnContainerBecameEmpty(ItemContainer container)
        {
            Debug.Log($"[LevelManager] Container {container.ContainerId} is now empty");
            // Update item count and check completion
            UpdateItemsRemaining();
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
            var mode = GameManager.Instance?.CurrentGameMode ?? GameMode.FreePlay;
            int movesUsed = GameManager.Instance?.CurrentMoveCount ?? 0;
            Debug.LogWarning($"[LevelManager] === LEVEL COMPLETE === Mode: {mode}, Moves: {movesUsed}, Recording: {isRecording}, Thresholds: {(starThresholds != null ? string.Join(",", starThresholds) : "NULL")}");

            // Stop recording and print moves if recording was active
            if (isRecording)
            {
                StopRecording();
            }

            // Play victory sound
            AudioManager.Instance?.PlayVictorySound();

            OnLevelCleared?.Invoke();

            // Calculate stars based on mode
            int stars = 0;

            if (mode == GameMode.StarMode || mode == GameMode.HardMode)
            {
                stars = CalculateStars(movesUsed);
            }

            // ALERT: Player beat the solver's score! Log in ALL modes for solver improvement.
            if (starThresholds != null && starThresholds.Length > 0 && movesUsed < starThresholds[0])
            {
                LogSolverAlert(movesUsed, starThresholds[0]);
            }
            else
            {
                Debug.Log($"[LevelManager] Solver alert NOT triggered: thresholds={(starThresholds != null ? "valid" : "NULL")}, length={(starThresholds?.Length ?? 0)}, movesUsed={movesUsed}, threshold[0]={(starThresholds != null && starThresholds.Length > 0 ? starThresholds[0].ToString() : "N/A")}");
            }

            // Complete the level with elapsed time
            GameManager.Instance?.CompleteLevel(stars, elapsedTime);
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
                GameManager.Instance?.FailLevel("Out of Moves");
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

        #region Undo System

        /// <summary>
        /// Record a move for potential undo and solver comparison
        /// </summary>
        public void RecordMove(Item item, ItemContainer fromContainer, int fromSlot, int fromRow,
                               ItemContainer toContainer, int toSlot, int toRow,
                               bool rowAdvancementOccurred = false, int[] rowAdvancementOffsets = null,
                               bool matchOccurred = false)
        {
            // Always track in allMovesThisLevel for solver comparison (uses string IDs, survives object destruction)
            allMovesThisLevel.Add(new SimpleMoveRecord
            {
                itemId = item?.ItemId ?? "unknown",
                fromContainerId = fromContainer?.ContainerId ?? "unknown",
                fromSlot = fromSlot,
                toContainerId = toContainer?.ContainerId ?? "unknown",
                toSlot = toSlot
            });

            // Only add to undo history if no match occurred (can't undo matches)
            if (!matchOccurred)
            {
                var record = new MoveRecord
                {
                    item = item,
                    fromContainer = fromContainer,
                    fromSlot = fromSlot,
                    fromRow = fromRow,
                    toContainer = toContainer,
                    toSlot = toSlot,
                    toRow = toRow,
                    rowAdvancementOccurred = rowAdvancementOccurred,
                    rowAdvancementOffsets = rowAdvancementOffsets
                };

                moveHistory.Push(record);
                UpdateUndoAvailable(true);
            }

            Debug.Log($"[LevelManager] Move recorded: {item.ItemId} from {fromContainer?.ContainerId}[{fromSlot},{fromRow}] to {toContainer?.ContainerId}[{toSlot},{toRow}]. Match: {matchOccurred}. Undo history: {moveHistory.Count}. Total moves: {allMovesThisLevel.Count}");
        }

        /// <summary>
        /// Clear move history (after a match, items can't be undone)
        /// </summary>
        public void ClearMoveHistory()
        {
            moveHistory.Clear();
            UpdateUndoAvailable(false);
            Debug.Log("[LevelManager] Move history cleared (match occurred)");
        }

        /// <summary>
        /// Undo the last move
        /// </summary>
        public bool UndoLastMove()
        {
            if (moveHistory.Count == 0)
            {
                Debug.Log("[LevelManager] No moves to undo");
                return false;
            }

            var record = moveHistory.Pop();

            // Check if item still exists and is valid
            if (record.item == null || record.item.IsMatched)
            {
                Debug.Log("[LevelManager] Cannot undo - item is null or matched");
                UpdateUndoAvailable(moveHistory.Count > 0);
                return false;
            }

            // Check if source container still exists
            if (record.fromContainer == null)
            {
                Debug.Log("[LevelManager] Cannot undo - source container is null");
                UpdateUndoAvailable(moveHistory.Count > 0);
                return false;
            }

            Debug.Log($"[LevelManager] Undoing move: {record.item.ItemId} back to {record.fromContainer.ContainerId}[{record.fromSlot},{record.fromRow}], rowAdvanced: {record.rowAdvancementOccurred}");

            // Remove item from current position
            if (record.toContainer != null)
            {
                record.toContainer.RemoveItemFromSlot(record.item, triggerRowAdvance: false);
            }

            // If row advancement occurred, reverse it BEFORE placing item back
            if (record.rowAdvancementOccurred && record.rowAdvancementOffsets != null)
            {
                Debug.Log($"[LevelManager] Reversing row advancement in container {record.fromContainer.ContainerId}");
                record.fromContainer.ReverseRowAdvancement(record.rowAdvancementOffsets);
            }

            // Place item back in original position
            bool success = record.fromContainer.PlaceItemInRow(record.item, record.fromSlot, record.fromRow);

            if (success)
            {
                // Decrement move count
                GameManager.Instance?.DecrementMoveCount();

                // Play a sound for feedback
                AudioManager.Instance?.PlayDropSound();

                Debug.Log($"[LevelManager] Undo successful. Remaining history: {moveHistory.Count}");
            }
            else
            {
                Debug.LogWarning("[LevelManager] Undo failed - could not place item back");
            }

            UpdateUndoAvailable(moveHistory.Count > 0);
            return success;
        }

        private void UpdateUndoAvailable(bool available)
        {
            canUndo = available;
            // Always fire event to ensure UI stays in sync
            OnUndoAvailableChanged?.Invoke(available);
        }

        #endregion

        #region Move Recording

        /// <summary>
        /// Start recording moves for comparison with solver
        /// </summary>
        public void StartRecording()
        {
            isRecording = true;
            recordedMoves.Clear();
            Debug.Log("[LevelManager] Recording started - make your moves!");
        }

        /// <summary>
        /// Stop recording and output the move sequence
        /// </summary>
        public void StopRecording()
        {
            isRecording = false;
            PrintRecordedMoves();
        }

        /// <summary>
        /// Record a move for comparison purposes (called for ALL moves, including matches)
        /// </summary>
        public void RecordMoveForComparison(Item item, ItemContainer fromContainer, int fromSlot,
                                            ItemContainer toContainer, int toSlot)
        {
            if (!isRecording) return;

            var record = new MoveRecord
            {
                item = item,
                fromContainer = fromContainer,
                fromSlot = fromSlot,
                fromRow = 0,
                toContainer = toContainer,
                toSlot = toSlot,
                toRow = 0
            };

            recordedMoves.Add(record);
        }

        /// <summary>
        /// Print recorded moves to console for comparison
        /// </summary>
        public void PrintRecordedMoves()
        {
            // Build entire output as single string for easy copying
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n=== YOUR RECORDED MOVE SEQUENCE ===");
            sb.AppendLine($"Total moves: {recordedMoves.Count}\n");

            for (int i = 0; i < recordedMoves.Count; i++)
            {
                var move = recordedMoves[i];
                string fromName = move.fromContainer != null ? move.fromContainer.ContainerId : "unknown";
                string toName = move.toContainer != null ? move.toContainer.ContainerId : "unknown";

                sb.AppendLine($"  {i + 1,2}. {move.item?.ItemId ?? "unknown",-16} : {fromName}[slot {move.fromSlot}] -> {toName}[slot {move.toSlot}]");
            }

            sb.AppendLine("\n=== END RECORDED SEQUENCE ===");

            // Single log call - easy to copy from Unity console
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Log detailed alert when player beats the solver, including move sequence.
        /// Writes to both console and a log file for later review.
        /// </summary>
        private void LogSolverAlert(int playerMoves, int solverMoves)
        {
            var sb = new System.Text.StringBuilder();
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string levelId = $"{currentLevel.world_id}_level_{currentLevel.id:D3}";

            sb.AppendLine("\n" + new string('=', 60));
            sb.AppendLine("!!! SOLVER ALERT - PLAYER BEAT SOLVER !!!");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine($"Timestamp: {timestamp}");
            sb.AppendLine($"Level: {currentLevel.world_id} - Level {currentLevel.id}");
            sb.AppendLine($"Player moves: {playerMoves}");
            sb.AppendLine($"Solver moves: {solverMoves}");
            sb.AppendLine($"Difference: {solverMoves - playerMoves} moves better!");
            sb.AppendLine();
            sb.AppendLine("=== PLAYER'S MOVE SEQUENCE ===");

            for (int i = 0; i < allMovesThisLevel.Count; i++)
            {
                var move = allMovesThisLevel[i];
                sb.AppendLine($"  {i + 1,2}. {move.itemId,-20} : {move.fromContainerId}[{move.fromSlot}] -> {move.toContainerId}[{move.toSlot}]");
            }

            sb.AppendLine();
            sb.AppendLine("ACTION REQUIRED: Run the solver on this level to get its move sequence,");
            sb.AppendLine("then compare to find where the solver missed an optimization.");
            sb.AppendLine(new string('=', 60) + "\n");

            string report = sb.ToString();

            // Log to console with warning level (yellow in Unity)
            Debug.LogWarning(report);

            // Also write to file for later review
            try
            {
                string logDir = System.IO.Path.Combine(Application.persistentDataPath, "SolverAlerts");
                Debug.LogWarning($"[SOLVER ALERT] Writing to: {logDir}");
                if (!System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                string filename = $"solver_alert_{levelId}_{timestamp.Replace(":", "-").Replace(" ", "_")}.txt";
                string filepath = System.IO.Path.Combine(logDir, filename);
                System.IO.File.WriteAllText(filepath, report);

                Debug.LogWarning($"[SOLVER ALERT] Report saved to: {filepath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SOLVER ALERT] FAILED to write log file: {e.Message}\n{e.StackTrace}");
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

            // Unsubscribe from game events
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnGameResumed -= OnGameResumed;

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

    /// <summary>
    /// Record of a single move for undo functionality
    /// </summary>
    public struct MoveRecord
    {
        public Item item;
        public ItemContainer fromContainer;
        public int fromSlot;
        public int fromRow;
        public ItemContainer toContainer;
        public int toSlot;
        public int toRow;

        // Row advancement tracking - stores how many rows each slot advanced
        public bool rowAdvancementOccurred;
        public int[] rowAdvancementOffsets; // Per-slot offset (how many rows items shifted forward)
    }

    /// <summary>
    /// Lightweight move record using string IDs (for solver comparison - survives object destruction)
    /// </summary>
    public struct SimpleMoveRecord
    {
        public string itemId;
        public string fromContainerId;
        public int fromSlot;
        public string toContainerId;
        public int toSlot;

        public override string ToString()
        {
            return $"{itemId}: {fromContainerId}[{fromSlot}] -> {toContainerId}[{toSlot}]";
        }
    }
}
