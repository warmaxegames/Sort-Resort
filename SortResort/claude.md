# Sort Resort - Unity Rebuild Progress Tracker

## Project Overview

**Sort Resort** is a casual puzzle/sorting game designed for iOS, Android, and Tablets. Players drag and drop items across containers to achieve triple matches. The game is being rebuilt from Godot 4 to Unity.

### Core Mechanics
- Drag-and-drop items between container slots
- Triple match system (3 identical items = cleared)
- Multi-row containers with depth visualization
- Locked containers with countdown unlock
- Moving containers on paths
- Star rating based on move efficiency (1-3 stars)
- 100 levels per world, 3+ worlds planned

### Folder Structure
- `godot structure/` - Design documentation and context
- `sortresort/` - Original Godot 4 project (fully functional)
- `Unity redo/` - Unity rebuild target

---

## Current Status

**Last Updated:** 2026-01-28

### Godot Version (Complete)
- 6 levels complete (Resort 001-006)
- All core systems implemented
- 100+ item sprites across 4 worlds
- Full audio system (10+ tracks, 8+ SFX)
- UI screens complete
- Dialogue system functional

### Unity Version (In Progress)
- Basic folder structure created
- Manager singletons scaffolded (GameManager, AudioManager, SaveManager, TransitionManager)
- UI screen base classes created
- GameState enum defined
- Bootstrap system ready
- **274 sprite assets imported** (items, UI, containers, animations)
- **18 audio files imported** (8 music, 8 SFX, 2 UI)
- **15 JSON data files imported** (items, worlds, levels, dialogue)
- **Font imported** (BENZIN-BOLD.TTF)
- **LeanTween animation library imported**
- **Phase 1 Core Gameplay COMPLETE:**
  - Item.cs (drag-drop, pooling, visual states)
  - Slot.cs (drop zone detection, validation)
  - ItemContainer.cs (slots, matching, row advancement, lock system)
  - DragDropManager.cs (input handling, raycasting)
  - LevelManager.cs (level loading, item spawning, completion)
- **GameSceneSetup.cs Working:**
  - Loads real levels from JSON
  - Sprites loading from Resources/Sprites/
  - Containers and items spawn correctly
  - Coordinate conversion from Godot to Unity working
  - First triple-match works correctly
  - **BUG: Second triple-match not animating correctly** (see Known Issues)

---

## Task List

### IMMEDIATE TODO (Next Session)
1. **FIX: Second triple-match animation bug**
   - First match works perfectly
   - Second match: items get locked (isInteractive=false) but don't animate/disappear
   - Debug logging added to ProcessMatch - check Console output
   - Likely issue: some items already in Matched state, or same item reference appearing multiple times
   - Check if items from first match are somehow still in the slot data

2. **After bug fix:**
   - Test with more levels (002-006)
   - Verify row advancement works
   - Test locked containers

---

### Phase 1: Core Gameplay Systems (HIGH PRIORITY) - COMPLETE
- [x] **1.1** Create Item.cs - Drag-drop behavior, pooling support, visual states
- [x] **1.2** Create ItemContainer.cs - Multi-slot management, row advancement, lock system
- [x] **1.3** Create Slot.cs - Drop zone validation, collider-based detection
- [x] **1.4** Implement drag-drop input system (mouse + touch support) - DragDropManager.cs
- [x] **1.5** Implement match detection logic (triple match identification) - in ItemContainer.cs
- [x] **1.6** Create object pooling system for items - uses existing ObjectPool.cs
- [x] **1.7** Create selection visual feedback (outline shader or sprite swap) - in Item.cs

### Phase 2: Level & Data Systems (HIGH PRIORITY) - MOSTLY COMPLETE
- [x] **2.1** Create ItemDatabase.cs - Load items.json, manage item definitions (in LevelManager.cs)
- [ ] **2.2** Create WorldDatabase.cs - Load worlds.json, manage world configs
- [x] **2.3** Create LevelLoader.cs - Parse level JSON files, instantiate containers (uses LevelData.cs + LevelManager.cs)
- [x] **2.4** Create LevelManager.cs - Track moves, calculate stars, check completion
- [x] **2.5** Set up JSON data files in Resources folder
- [x] **2.6** Create container prefabs (standard, single-slot variants) - PrefabGenerator.cs
- [x] **2.7** Create item prefab with sprite renderer and colliders - PrefabGenerator.cs
- [x] **2.8** GameSceneSetup.cs for proper level loading

### Phase 3: UI Implementation (MEDIUM PRIORITY)
- [ ] **3.1** Implement MainMenuScreen fully
- [ ] **3.2** Implement WorldSelectionScreen with world gallery
- [ ] **3.3** Implement LevelSelectionScreen with 100-level grid
- [ ] **3.4** Implement GameHUDScreen (move counter, pause button, undo)
- [ ] **3.5** Implement PauseMenuScreen
- [ ] **3.6** Implement LevelCompleteScreen with star animation
- [ ] **3.7** Implement LevelFailedScreen
- [ ] **3.8** Implement SettingsScreen with volume sliders
- [ ] **3.9** Create LevelNode prefab for level selection

### Phase 4: Audio System (MEDIUM PRIORITY)
- [ ] **4.1** Implement AudioManager fully (music playback, crossfading)
- [ ] **4.2** Implement SFX system (match, drag, drop, unlock sounds)
- [ ] **4.3** Set up audio mixer with volume groups
- [x] **4.4** Import all audio assets
- [ ] **4.5** Add world-specific music switching

### Phase 5: Save & Progression (MEDIUM PRIORITY)
- [ ] **5.1** Implement SaveManager (PlayerPrefs or JSON file)
- [ ] **5.2** Track completed levels per world
- [ ] **5.3** Track star ratings per level
- [ ] **5.4** Track world unlock status
- [ ] **5.5** Implement settings persistence (audio volumes)

### Phase 6: Visual Polish (LOWER PRIORITY)
- [ ] **6.1** Create match animation (18-frame explosion or particle effect)
- [ ] **6.2** Create container unlock animation
- [ ] **6.3** Implement row depth visualization (grayed back rows)
- [ ] **6.4** Add tween animations (item scale, highlight effects)
- [ ] **6.5** Implement scene transitions
- [ ] **6.6** Add mascot reactions

### Phase 7: Advanced Features (LOWER PRIORITY)
- [ ] **7.1** Implement moving container system with path following
- [ ] **7.2** Implement dialogue system
- [ ] **7.3** Create undo system
- [ ] **7.4** Profile customization system
- [ ] **7.5** Leaderboard integration

### Phase 8: Content & Testing
- [x] **8.1** Import all sprite assets
- [ ] **8.2** Create remaining levels (7-100 for each world)
- [ ] **8.3** Test on mobile devices
- [ ] **8.4** Performance optimization
- [ ] **8.5** Bug fixing and polish

---

## Known Issues

### ACTIVE: Second Triple-Match Animation Bug
**Symptoms:**
- First triple-match works perfectly (items animate and disappear)
- Second triple-match: items become non-interactive but don't animate/disappear
- Level registers as complete (data is correct) but visuals are stuck

**Debug info added:**
- ProcessMatch now logs each item's state before processing
- Check Console for: `[ItemContainer] ProcessMatch item X: {itemId}, state: {state}`

**Possible causes to investigate:**
1. Items already in Matched state when ProcessMatch runs
2. Same item reference appearing multiple times in matchedItems list
3. LeanTween animation not starting or completing
4. Items from first match still referenced somewhere

**Files involved:**
- `ItemContainer.cs` - ProcessMatch(), CheckForMatches()
- `Item.cs` - MarkAsMatched(), PlayMatchAnimation(), ResetState()

---

## Completed Tasks

### Phase 1 - Core Gameplay (COMPLETE)
- [x] Item.cs with drag-drop, pooling, visual states
- [x] Slot.cs with drop zone detection
- [x] ItemContainer.cs with matching and lock system
- [x] DragDropManager.cs with raycasting
- [x] LevelManager.cs with level loading
- [x] Triple-match detection and match animation
- [x] Row advancement after match
- [x] Debug logging system for troubleshooting

### Testing & Sprites (COMPLETE)
- [x] TestSceneSetup.cs for runtime testing (manual containers)
- [x] GameSceneSetup.cs for JSON level loading
- [x] Real sprites loaded from Resources folder
- [x] Sprite sizing normalized (fit within slots)
- [x] Items positioned at bottom of slots
- [x] Container scaling isolated (shelf sprite only)
- [x] Layer masks configured (Items=6, Slots=7)

---

## Technical Notes

### Key Patterns to Follow
- **Object Pooling**: Essential for mobile performance (50-60 item pool)
- **Event System**: Use C# events/Actions for decoupled communication
- **Singleton Managers**: GameManager, AudioManager, SaveManager, UIManager
- **Data-Driven**: JSON files for levels, items, worlds
- **State Machine**: Game states (Loading, MainMenu, Playing, Paused, etc.)

### Godot → Unity Coordinate Conversion
Level JSON uses Godot pixel coordinates. Conversion in `ItemContainer.Initialize()`:
```csharp
float unityX = (godotPos.x - 512f) / 100f;  // Center X, 100 pixels per unit
float unityY = (400f - godotPos.y) / 100f;  // Flip Y axis
```

### Godot → Unity Mapping
| Godot | Unity |
|-------|-------|
| Area2D | Collider2D + trigger |
| Signals | C# events/UnityEvents |
| Tweens | DOTween or LeanTween |
| Autoload | Singleton MonoBehaviour |
| PackedScene | Prefab |
| res:// | Resources/ or Addressables |

### Asset Locations (Unity - Current)
- Item Sprites: `Assets/_Project/Resources/Sprites/Items/{World}/`
- Container Sprites: `Assets/_Project/Resources/Sprites/Containers/`
- Prefabs: `Assets/_Project/Resources/Prefabs/`
- Data: `Assets/_Project/Resources/Data/`
- Levels: `Assets/_Project/Resources/Data/Levels/{World}/`
- Scripts: `Assets/_Project/Scripts/`

### Layer Configuration
- Layer 6: "Items" - for item raycasting
- Layer 7: "Slots" - for slot raycasting

---

## Questions & Decisions

### Resolved
1. **Unity version:** 6.2
2. **Animation library:** LeanTween (free)
3. **Asset loading:** Resources (simpler, sufficient for project scope)
4. **Input system:** New Unity Input System (better cross-platform)

### Open Questions
*None currently*

---

## Session Log

### 2026-01-28 - Initial Review
- Explored all three project folders
- Documented Godot implementation
- Created initial task list
- Created this tracking document

### 2026-01-28 - Asset Import Complete
- Imported 274 sprite files to `Assets/_Project/Art/`
- Imported 18 audio files to `Assets/_Project/Audio/`
- Imported font (BENZIN-BOLD.TTF)
- Imported 15 JSON data files to `Assets/_Project/Resources/Data/`
- Resolved technical decisions (Unity 6.2, LeanTween, Resources, New Input System)

### 2026-01-28 - Phase 1: Core Gameplay Systems Complete
Created 5 new scripts in `Assets/_Project/Scripts/Gameplay/`:
- Item.cs, Slot.cs, ItemContainer.cs, DragDropManager.cs, LevelManager.cs

### 2026-01-28 - Testing & Sprite Implementation (Session 2)
- TestSceneSetup.cs created for manual testing
- Sprite sizing and positioning implemented
- Various bug fixes for drag-drop and slot management

### 2026-01-28 - Bug Fixes & Prefab System (Session 3)
- Vertical offset bug fixed
- Match counter bug fixed
- PrefabGenerator updated with proper serialized field wiring
- Item scaling system added

### 2026-01-28 - GameSceneSetup & Level Loading (Session 4)

**GameSceneSetup.cs Created:**
- New script for proper level loading from JSON
- Creates all managers at runtime (GameManager, DragDropManager, LevelManager)
- Loads prefabs from Resources/Prefabs/
- Loads level via LevelManager.LoadLevel()
- Includes restart button and HUD display

**Setup Steps Documented:**
1. Run `Tools > Sort Resort > Generate Prefabs`
2. Move prefabs to `Assets/_Project/Resources/Prefabs/`
3. Move sprites to `Assets/_Project/Resources/Sprites/Items/{World}/`
4. Create new scene, add empty GameObject with GameSceneSetup component
5. Press Play

**Coordinate Conversion Added:**
- Godot uses pixel coords (e.g., 500, 400)
- Unity uses world units centered at origin
- Conversion: `unityX = (godotX - 512) / 100`, `unityY = (400 - godotY) / 100`

**Container Sprite Loading Added:**
- ItemContainer.LoadContainerSprite() method
- Loads from Resources/Sprites/Containers/
- Falls back to base_shelf if specific sprite not found
- Scales sprite to fit container width

**Bug Fixes This Session:**
1. Fixed: GameSceneSetup compile error (removed ResetMoveCount call)
2. Fixed: Level path in LevelDataLoader (Data/Levels/{World}/ with capitalization)
3. Fixed: Slot layer not set (added layer 7 in CreateSlotComponent)
4. Fixed: Item layer not set from pool (added layer 6 in GetItemFromPool)
5. Fixed: Item collider too small (ScaleToFitSlot now updates collider size)
6. Fixed: LeanTween.alpha not working with SpriteRenderer (use LeanTween.value instead)
7. Fixed: ResetState not properly resetting (cancel tweens first, check for zero scale)
8. Fixed: DropOnSlot overwriting Matched state with Idle (added state check)

**Files Modified:**
- `GameSceneSetup.cs` - New file for level loading
- `LevelData.cs` - Fixed level path with capitalization
- `ItemContainer.cs` - Added coordinate conversion, sprite loading, slot layer, debug logging
- `LevelManager.cs` - Added item layer assignment
- `Item.cs` - Fixed collider update, alpha animation, reset state, drop state check

**Current State:**
- Level 001 loads and displays correctly
- Drag-drop works
- First triple-match works perfectly
- **BUG:** Second triple-match doesn't animate (items lock but don't disappear)
- Debug logging added to investigate

