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

**Last Updated:** 2026-01-29

### Godot Version (Complete)
- 6 levels complete (Resort 001-006)
- All core systems implemented
- 100+ item sprites across 4 worlds
- Full audio system (10+ tracks, 8+ SFX)
- UI screens complete
- Dialogue system functional

### Unity Version (In Progress)
- **Phase 1 Core Gameplay: COMPLETE**
- **Phase 2 Level & Data Systems: COMPLETE**
- **Phase 3 UI Implementation: PARTIAL** (Level Select, HUD, Level Complete done)
- **Lock System: COMPLETE** (visual overlay, countdown, unlock animation)
- **Portrait Mode: COMPLETE** (ScreenManager for responsive scaling)

#### Working Features:
- Level selection screen with 100-level grid and world navigation
- Full game flow: Level Select -> Play -> Complete -> Back to Levels
- Drag-drop with visual feedback
- Triple-match detection and animation
- Row advancement (only when ALL front slots empty)
- Locked containers with visual overlay and countdown
- Unlock animation when matches reach threshold
- Portrait phone aspect ratio support
- Move counter, match counter, star preview

---

## Task List

### IMMEDIATE TODO (Next Session)
1. **Copy lock overlay sprites to Resources**
   - From: `Assets/_Project/Art/UI/Overlays/base_lockoverlay.png`
   - To: `Assets/_Project/Resources/Sprites/UI/Overlays/`
   - Same for `base_single_slot_lockoverlay.png`

2. **Test remaining levels (003-006)**
   - Level 003 has moving containers (carousel, back_and_forth)
   - Level 005-006 have moving tracks (more complex)

3. **Implement SaveManager persistence**
   - Save completed levels and star ratings
   - Unlock next level on completion

4. **Audio integration**
   - Copy audio files to Resources/Audio/
   - Wire up match sound, unlock sound, button clicks

---

### Phase 1: Core Gameplay Systems - COMPLETE
- [x] Item.cs - Drag-drop behavior, pooling support, visual states
- [x] ItemContainer.cs - Multi-slot management, row advancement, lock system
- [x] Slot.cs - Drop zone validation, collider-based detection
- [x] DragDropManager.cs - Input handling with new Input System
- [x] LevelManager.cs - Level loading, item spawning, completion
- [x] Object pooling system for items
- [x] Selection visual feedback

### Phase 2: Level & Data Systems - COMPLETE
- [x] ItemDatabase loading from items.json
- [x] LevelLoader parsing JSON files
- [x] LevelManager tracking moves, stars, completion
- [x] JSON data files in Resources folder
- [x] Container prefabs (standard, single-slot)
- [x] Item prefab with sprite renderer and colliders
- [x] GameSceneSetup.cs for proper level loading
- [ ] WorldDatabase.cs - Load worlds.json (optional)

### Phase 3: UI Implementation - PARTIAL
- [ ] **3.1** MainMenuScreen
- [ ] **3.2** WorldSelectionScreen
- [x] **3.3** LevelSelectionScreen with 100-level grid
- [x] **3.4** GameHUDScreen (move counter, stars, pause/restart)
- [ ] **3.5** PauseMenuScreen
- [x] **3.6** LevelCompleteScreen with buttons
- [ ] **3.7** LevelFailedScreen
- [ ] **3.8** SettingsScreen with volume sliders

### Phase 4: Audio System - NOT STARTED
- [ ] **4.1** AudioManager music playback, crossfading
- [ ] **4.2** SFX system (match, drag, drop, unlock)
- [ ] **4.3** Audio mixer with volume groups
- [x] **4.4** Audio assets imported (need to copy to Resources)
- [ ] **4.5** World-specific music switching

### Phase 5: Save & Progression - NOT STARTED
- [ ] **5.1** SaveManager (PlayerPrefs or JSON file)
- [ ] **5.2** Track completed levels per world
- [ ] **5.3** Track star ratings per level
- [ ] **5.4** Track world unlock status
- [ ] **5.5** Settings persistence (audio volumes)

### Phase 6: Visual Polish - PARTIAL
- [ ] **6.1** Match animation (use 18-frame sprites)
- [x] **6.2** Container unlock animation (scale + fade)
- [x] **6.3** Row depth visualization (grayed back rows, vertical offset)
- [x] **6.4** Tween animations (item scale, highlight)
- [ ] **6.5** Scene transitions
- [ ] **6.6** Mascot reactions

### Phase 7: Advanced Features - PARTIAL
- [x] **7.1** ContainerMovement.cs (back_and_forth, carousel, falling)
- [ ] **7.2** Moving tracks system (level 005-006)
- [ ] **7.3** Dialogue system
- [ ] **7.4** Undo system
- [ ] **7.5** Profile customization

### Phase 8: Content & Testing
- [x] **8.1** Import all sprite assets
- [ ] **8.2** Create remaining levels (7-100 for each world)
- [ ] **8.3** Test on mobile devices
- [ ] **8.4** Performance optimization
- [ ] **8.5** Bug fixing and polish

---

## Known Issues

### Slot Detection Debug Logging
- Added verbose logging to `FindBestDropSlot()` and `CanAcceptDrop()`
- If drops fail, check Console for exact reason (locked, not empty, not interactive)
- Can be removed once stable

---

## Completed Features (This Session)

### UI System
- **UIManager.cs** - Runtime UI creation, screen management
- **LevelSelectScreen.cs** - 100-level grid, world navigation (< >), lock states
- **Level Complete Screen** - Replay, Next, Levels buttons
- **Game HUD** - Move counter, match counter, star preview, pause/restart

### Lock System (Matching Godot)
- Visual lock overlay covering container
- Countdown text showing matches remaining (black text, centered)
- Matches anywhere in level decrement ALL locked containers
- Unlock animation (scale up + fade out)
- Slots become interactive after unlock

### Row Advancement Fix
- Items only advance when ALL front row slots are empty
- Not per-slot advancement
- Triggers after successful drop, not during drag
- Prevents items advancing while player is still dragging

### Portrait Mode Support
- **ScreenManager.cs** - Responsive scaling based on aspect ratio
- Camera orthographic size adjusted for portrait (9:16)
- Game world scales to fit narrow screens
- Coordinate conversion updated for portrait center point

### Bug Fixes
- JSON trailing commas in level_002.json and level_004.json
- InputSystem compatibility (InputSystemUIInputModule)
- Level complete timing (waits for match animation)
- Button sizing in layout groups (LayoutElement)

---

## Technical Notes

### Key Patterns
- **Object Pooling**: 50-60 item pool for mobile performance
- **Event System**: C# events/Actions for decoupled communication
- **Singleton Managers**: GameManager, AudioManager, SaveManager, UIManager, ScreenManager
- **Data-Driven**: JSON files for levels, items, worlds
- **State Machine**: GameState enum (Playing, LevelSelection, Paused, etc.)

### Coordinate Conversion (Portrait)
```csharp
float unityX = (godotPos.x - 540f) / 100f;  // Center for portrait
float unityY = (600f - godotPos.y) / 100f;  // Flip Y, portrait center
```

### Layer Configuration
- Layer 6: "Items" - for item raycasting
- Layer 7: "Slots" - for slot raycasting

### Asset Locations
- Item Sprites: `Resources/Sprites/Items/{World}/`
- Container Sprites: `Resources/Sprites/Containers/`
- Lock Overlays: `Resources/Sprites/UI/Overlays/` (need to copy)
- Audio: `Resources/Audio/` (need to copy)
- Prefabs: `Resources/Prefabs/`
- Level Data: `Resources/Data/Levels/{World}/`

---

## Session Log

### 2026-01-29 - UI System, Locks, Portrait Mode

**New Files Created:**
- `UIManager.cs` - Runtime UI creation and screen management
- `LevelSelectScreen.cs` - Level selection with world navigation
- `ScreenManager.cs` - Responsive scaling for different aspect ratios
- `ContainerMovement.cs` - Moving/falling container support

**Major Features:**
1. Complete UI flow (Level Select -> Play -> Complete -> Back)
2. Lock system with visual overlay and countdown
3. Portrait mode support with responsive scaling
4. Row advancement fix (all slots must be empty)

**Bug Fixes:**
- JSON parse errors (trailing commas)
- Input System compatibility
- Level complete timing
- Button layout in HorizontalLayoutGroup

**Files Modified:**
- `ItemContainer.cs` - Lock overlay creation, unlock animation, row advancement
- `LevelManager.cs` - Global match notification to locked containers
- `Item.cs` - Row advance after successful drop only
- `DragDropManager.cs` - Debug logging for slot detection
- `Slot.cs` - Enhanced CanAcceptDrop logging
- `GameSceneSetup.cs` - Portrait camera, ScreenManager integration

### Previous Sessions
- 2026-01-28: Initial setup, asset import, Phase 1 core gameplay
- See git history for detailed changes

---

## Next Steps Priority

1. **Copy assets to Resources** (lock overlays, audio)
2. **Test levels 003-006** (moving containers)
3. **SaveManager** - Persist progress
4. **Audio** - Wire up sounds
5. **Main Menu** - Splash screen, start button
6. **Mobile testing** - Touch input, performance
