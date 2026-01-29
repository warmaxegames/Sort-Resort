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
- **Test Scene Working:**
  - Real sprites loading from Resources/Sprites/
  - Drag-drop functional with visual feedback
  - Triple-match detection working with animations
  - Items positioned at bottom of slots
  - Container scaling fixed (shelf sprite scaled, container at scale=1)

---

## Task List

### IMMEDIATE TODO (Next Session)
1. **FIX: Vertical position offset after drag-drop**
   - Items are slightly HIGHER after being dropped vs their initial position
   - Compare `TestSceneSetup.CreateRealItem()` positioning vs `ItemContainer.GetItemWorldPositionBottomAligned()`
   - Check if `GetItemHeight()` returns different value than `finalItemHeight` in TestSceneSetup
   - May need to verify sprite bounds calculation is consistent

2. **Create proper prefabs** (to replace TestSceneSetup reflection hacks)
   - Item prefab with SpriteRenderer, BoxCollider2D, Item component
   - Container prefab with ItemContainer, child Slots, shelf sprite
   - Slot prefab with BoxCollider2D, Slot component

3. **Test full match flow**
   - Verify triple-match clears items correctly
   - Verify row advancement works after match
   - Verify move counter increments
   - Verify match counter increments

---

### Phase 1: Core Gameplay Systems (HIGH PRIORITY) - COMPLETE
- [x] **1.1** Create Item.cs - Drag-drop behavior, pooling support, visual states
- [x] **1.2** Create ItemContainer.cs - Multi-slot management, row advancement, lock system
- [x] **1.3** Create Slot.cs - Drop zone validation, collider-based detection
- [x] **1.4** Implement drag-drop input system (mouse + touch support) - DragDropManager.cs
- [x] **1.5** Implement match detection logic (triple match identification) - in ItemContainer.cs
- [x] **1.6** Create object pooling system for items - uses existing ObjectPool.cs
- [x] **1.7** Create selection visual feedback (outline shader or sprite swap) - in Item.cs

### Phase 2: Level & Data Systems (HIGH PRIORITY) - PARTIAL
- [x] **2.1** Create ItemDatabase.cs - Load items.json, manage item definitions (in LevelManager.cs)
- [ ] **2.2** Create WorldDatabase.cs - Load worlds.json, manage world configs
- [x] **2.3** Create LevelLoader.cs - Parse level JSON files, instantiate containers (uses LevelData.cs + LevelManager.cs)
- [x] **2.4** Create LevelManager.cs - Track moves, calculate stars, check completion
- [x] **2.5** Set up JSON data files in Resources folder
- [ ] **2.6** Create container prefabs (standard, single-slot variants)
- [ ] **2.7** Create item prefab with sprite renderer and colliders

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
- [x] TestSceneSetup.cs for runtime testing
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

### Godot → Unity Mapping
| Godot | Unity |
|-------|-------|
| Area2D | Collider2D + trigger |
| Signals | C# events/UnityEvents |
| Tweens | DOTween or LeanTween |
| Autoload | Singleton MonoBehaviour |
| PackedScene | Prefab |
| res:// | Resources/ or Addressables |

### Asset Locations (Godot)
- Items: `sortresort/assets/images/items/{world}/`
- Audio: `sortresort/assets/audio/`
- UI: `sortresort/assets/images/ui/`
- Levels: `sortresort/data/levels/{world}/`

### Asset Locations (Unity)
- Items: `Assets/_Project/Art/Items/{World}/`
- Audio: `Assets/_Project/Audio/{Music|SFX|UI}/`
- UI: `Assets/_Project/Art/UI/{Backgrounds|Buttons|Icons|Overlays|Worlds}/`
- Containers: `Assets/_Project/Art/Containers/`
- Animations: `Assets/_Project/Art/Animations/{ItemMatch|ContainerUnlock}/`
- Data: `Assets/_Project/Resources/Data/`
- Levels: `Assets/_Project/Resources/Data/Levels/{World}/`
- Scripts: `Assets/_Project/Scripts/`

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
  - Items: Resort (50), Supermarket (48), Farm (45)
  - UI: Backgrounds (10), Buttons (17), Icons (4), Overlays (6), Worlds (3)
  - Containers (7), Mascots (4)
  - Animations: ItemMatch (18 frames), ContainerUnlock (62 frames)
- Imported 18 audio files to `Assets/_Project/Audio/`
  - Music (8), SFX (8), UI (2)
- Imported font (BENZIN-BOLD.TTF)
- Imported 15 JSON data files to `Assets/_Project/Resources/Data/`
  - items.json, worlds.json
  - 6 level files (Resort 001-006)
  - 7 dialogue files
- Resolved technical decisions (Unity 6.2, LeanTween, Resources, New Input System)

### 2026-01-28 - Phase 1: Core Gameplay Systems Complete
Created 5 new scripts in `Assets/_Project/Scripts/Gameplay/`:

1. **Item.cs** (~350 lines)
   - Visual states (Idle, Selected, Dragging, Matched, Returning)
   - Drag-drop behavior with offset calculation
   - Object pooling support with callbacks
   - Row depth visualization for back-row items
   - Match animation using LeanTween
   - Trigger-based collision detection

2. **Slot.cs** (~200 lines)
   - Drop zone detection via BoxCollider2D trigger
   - Visual feedback (highlight for valid/invalid drops)
   - Tracks items currently in drop zone
   - Validates drop acceptance (front row, empty, unlocked)

3. **ItemContainer.cs** (~400 lines)
   - Multi-slot management (configurable slot count)
   - Row-based item storage (front row interactive, back rows visual)
   - Triple match detection in front row
   - Automatic row advancement on match
   - Lock system with countdown unlock
   - Events for match, unlock, empty

4. **DragDropManager.cs** (~250 lines)
   - Uses new Unity Input System
   - Mouse and touch input support
   - Raycasting for item/slot detection
   - Manages drag state and hover feedback

5. **LevelManager.cs** (~350 lines)
   - Loads level JSON files
   - Spawns containers and items
   - Object pool for items
   - Tracks matches and completion
   - Calculates star ratings

### 2026-01-28 - Testing & Sprite Implementation (Session 2)

**TestSceneSetup.cs created** (~400 lines)
- Runtime test scene creation without prefabs
- Loads real sprites from `Resources/Sprites/Items/Resort/`
- Creates containers with ItemContainer component
- Creates slots with proper positioning and colliders
- Initializes all references via reflection (temporary approach)

**Sprite Analysis & Sizing:**
- Analyzed source sprite dimensions via Unity meta files:
  - coconut: 128x146px (nearly square)
  - pineapple: 128x252px (very tall)
  - beachball: 124x125px (square)
  - sunhat: 128x173px (slightly tall)
- Implemented proportional scaling to fit slots (80% height or 90% width)
- Items now properly sized regardless of source dimensions

**Item Positioning Improvements:**
- Added `GetItemWorldPositionBottomAligned()` to ItemContainer
- Items now sit at bottom of slots, not floating in center
- Added `GetItemHeight()` helper using SpriteRenderer bounds
- Updated all positioning methods (PlaceItemInSlot, PlaceItemInRow, UpdateAllItemPositions)

**Container Scaling Fix:**
- Issue: Container scale affected slot/item positioning
- Solution: Container stays at scale=1, shelf sprite is separate child object
- ShelfSprite child is scaled, slots/items use unscaled container space

**Bug Fixes:**
- Fixed: Items staying large after drop (RecaptureOriginalScale method)
- Fixed: Slot not clearing properly (extensive debug logging added)
- Fixed: slotsParent not being set (now passed to ItemContainer)
- Fixed: Slot spacing mismatch (slotSpacing/slotSize now set properly)
- Removed: Corrupted "nul" file causing Unity import loop

**Known Issue (TO FIX NEXT):**
- Items shift slightly HIGHER vertically after drag-drop vs initial position
- Likely difference in height calculation between TestSceneSetup and ItemContainer
