# Sort Resort - Unity Rebuild Progress Tracker

## Project Overview

**Sort Resort** is a casual puzzle/sorting game designed for iOS, Android, and Tablets. Players drag and drop items across containers to achieve triple matches. The game is being rebuilt from Godot 4 to Unity.

### Core Mechanics
- Drag-and-drop items between container slots
- Triple match system (3 identical items = cleared)
- Multi-row containers with depth visualization
- Locked containers with countdown unlock
- Moving containers on paths (carousel, back-and-forth)
- Star rating based on move efficiency (1-3 stars)
- 100 levels per world, 3 worlds planned

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
- **Phase 3 UI Implementation: MOSTLY COMPLETE** (Level Select, HUD, Level Complete done)
- **Phase 4 Audio System: COMPLETE** (all SFX wired up, world-specific music + ambient)
- **Phase 5 Save & Progression: COMPLETE** (PlayerPrefs, level unlock, stars)
- **Phase 7 Advanced Features: PARTIAL** (moving containers, despawn-on-match)

#### Working Features:
- Level selection screen with 100-level grid and world navigation
- **3 worlds playable:** Resort, Supermarket, Farm (5 levels each)
- Full game flow: Level Select → Play → Complete → Back to Levels / Next Level
- Drag-drop with visual feedback and sound effects
- Triple-match detection with animation and sound
- Row advancement (only when ALL front slots empty)
- Locked containers with visual overlay and countdown unlock
- Unlock animation with sound when matches reach threshold
- Portrait phone aspect ratio support
- Move counter, match counter, star preview
- Carousel movement (horizontal/vertical trains)
- Despawn-on-match stacking containers with chain-falling
- World-specific backgrounds and music + ambient audio
- Music continues across levels in same world
- Save progress persisted across sessions
- Back button in HUD to exit to level select

---

## Task List

### IMMEDIATE TODO (Next Session)
1. **Main Menu / Splash Screen** - Start button, settings access
2. **Mobile Testing** - Touch input validation on device
3. **Pause Menu Screen** - Resume, settings, quit options
4. **Settings Screen** - Volume sliders for music/SFX

### Future Priorities
5. **Match Animation** - Use 18-frame explosion sprites
6. **Scene Transitions** - Fade between screens
7. **Dialogue System** - Tutorial and story dialogues
8. **Undo System** - Allow undoing last move
9. **Create Levels 7-100** - Complete level content for all worlds

---

### Phase 1: Core Gameplay Systems - COMPLETE ✅
- [x] Item.cs - Drag-drop behavior, pooling support, visual states
- [x] ItemContainer.cs - Multi-slot management, row advancement, lock system
- [x] Slot.cs - Drop zone validation, collider-based detection
- [x] DragDropManager.cs - Input handling with new Input System
- [x] LevelManager.cs - Level loading, item spawning, completion
- [x] Object pooling system for items
- [x] Selection visual feedback

### Phase 2: Level & Data Systems - COMPLETE ✅
- [x] ItemDatabase loading from items.json
- [x] LevelLoader parsing JSON files
- [x] LevelManager tracking moves, stars, completion
- [x] JSON data files in Resources folder
- [x] Container prefabs (standard, single-slot)
- [x] Item prefab with sprite renderer and colliders
- [x] GameSceneSetup.cs for proper level loading
- [x] **16 level files total** (Resort 6, Supermarket 5, Farm 5)

### Phase 3: UI Implementation - MOSTLY COMPLETE
- [ ] **3.1** MainMenuScreen / SplashScreen
- [x] **3.2** WorldSelectionScreen - integrated in LevelSelectScreen
- [x] **3.3** LevelSelectionScreen with 100-level grid
- [x] **3.4** GameHUDScreen (move counter, stars, back button)
- [ ] **3.5** PauseMenuScreen
- [x] **3.6** LevelCompleteScreen with buttons (Levels, Replay, Next)
- [ ] **3.7** LevelFailedScreen
- [ ] **3.8** SettingsScreen with volume sliders

### Phase 4: Audio System - COMPLETE ✅
- [x] AudioManager with music playback and crossfading
- [x] SFX system (match, drag, drop, unlock, victory sounds)
- [x] World-specific music (gameplay + ambient per world)
- [x] Worldmap music on level select screen
- [x] Music continues across levels in same world
- [x] Fast crossfade (0.5s) between music tracks
- [x] Audio files in Resources/Audio/

### Phase 5: Save & Progression - COMPLETE ✅
- [x] SaveManager (PlayerPrefs)
- [x] Track completed levels per world
- [x] Track star ratings per level
- [x] Track world unlock status
- [x] Settings persistence (audio volumes)

### Phase 6: Visual Polish - PARTIAL
- [ ] **6.1** Match animation (18-frame explosion)
- [x] **6.2** Container unlock animation (scale + fade)
- [x] **6.3** Row depth visualization (grayed back rows, vertical offset)
- [x] **6.4** Tween animations (item scale, highlight)
- [ ] **6.5** Scene transitions
- [ ] **6.6** Mascot reactions

### Phase 7: Advanced Features - PARTIAL
- [x] **7.1** ContainerMovement.cs (back_and_forth, carousel, falling)
- [x] **7.2** Despawn-on-match stacking containers
- [ ] **7.3** Dialogue system
- [ ] **7.4** Undo system
- [ ] **7.5** Profile customization

### Phase 8: Content & Testing
- [x] **8.1** Import all sprite assets
- [x] **8.2** Levels 1-5 for all 3 worlds (15 levels total + Resort 006)
- [ ] **8.3** Create remaining levels (7-100 for each world)
- [ ] **8.4** Test on mobile devices
- [ ] **8.5** Performance optimization
- [ ] **8.6** Bug fixing and polish

---

## Technical Notes

### Key Patterns
- **Object Pooling**: 50-60 item pool for mobile performance
- **Event System**: C# events/Actions for decoupled communication
- **Singleton Managers**: GameManager, AudioManager, SaveManager, UIManager, ScreenManager
- **Data-Driven**: JSON files for levels, items, worlds
- **State Machine**: GameState enum (Playing, LevelSelection, Paused, etc.)

### Coordinate Conversion (Portrait Mode)
```csharp
float unityX = (godotPos.x - 540f) / 100f;  // Center for portrait
float unityY = (600f - godotPos.y) / 100f;  // Flip Y, portrait center
```

### Container Dimensions (Godot pixels)
- **Slot size**: 83×166 pixels
- **Slot spacing**: 83 pixels horizontal
- **Container visual**: ~297×198 pixels (includes border)

### Carousel Train Configuration
| Layout | Spacing | Containers | move_distance |
|--------|---------|------------|---------------|
| Vertical trains | 198px (Y) | 10 each | 1980 |
| Horizontal trains | 297px (X) | 5 each | 1485 |

### Layer Configuration
- Layer 6: "Items" - for item raycasting
- Layer 7: "Slots" - for slot raycasting

### Asset Locations (Unity)
- Item Sprites: `Resources/Sprites/Items/{World}/`
- Container Sprites: `Resources/Sprites/Containers/`
- Audio: `Resources/Audio/{Music|SFX|UI}/`
- Prefabs: `Resources/Prefabs/`
- Level Data: `Resources/Data/Levels/{World}/`

### Item Mappings Across Worlds
Levels 1-5 are identical in structure across all worlds, with item swaps:

| Resort | Supermarket | Farm |
|--------|-------------|------|
| bluesunglasses | chips | potato |
| coconut | honey | carrot |
| greencoconut | eggs | turnip |
| pinkpolkadotbikini | ketchup | redtomato |
| sunscreenspf50 | mustard | whiteonion |
| surfboardpalmtrees | milk | romaine |
| sandals | soda | greentomato |
| lemonade | orangejuice | potato |
| margarita | grapesoda | carrot |
| icecreamcone | chocolatebar | turnip |
| sandpale_blue | pickles | redtomato |
| flipflops | peanutbutter | whiteonion |
| sunhat | mayo | romaine |
| pineapple | popcornbucket | greentomato |
| sunscreen | cider | potato |
| beachball_mixed | coffeebeans | turnip |

---

## Session Log

### 2026-01-28 - Initial Setup & Phase 1
- Explored all project folders, documented Godot implementation
- Imported 274 sprites, 18 audio files, 15 JSON files
- Created core gameplay scripts (Item, Slot, ItemContainer, DragDropManager, LevelManager)
- Implemented drag-drop, triple-match detection, row advancement
- Created TestSceneSetup for runtime testing

### 2026-01-29 - UI, Audio, Multi-World Support

**Audio System Enhancements:**
- Rewrote `AudioManager.cs` with ambient sound support
- Added `PlayWorldmapMusic()` for level select screen
- Added `PlayWorldGameplayAudio(worldId)` - plays both music + ambient
- Added `StopGameplayAudio()` to stop before victory sound
- Reduced crossfade duration from 2.0s to 0.5s for snappier transitions
- Music continues when playing "Next Level" in same world (no restart)
- Wired up all sound effects:
  - `Item.cs`: Drag sound on StartDrag(), Drop sound on DropOnSlot()
  - `ItemContainer.cs`: Match sound after items marked

**UI Improvements:**
- Replaced Pause ("II") and Restart ("R") buttons with single "Back" button
- Back button exits current level and returns to level select
- Added worldmap music playback on level select startup
- Fixed manager creation order (AudioManager before UIManager)

**Bug Fixes:**
- Fixed invisible items on level 3 (was using supermarket items with resort world_id)
- Fixed music not playing on level select (AudioManager created after UIManager.CreateRuntimeUI())
- Fixed music crossfade overlap (reduced duration)

**Multi-World Level Support:**
- Updated `level_003.json` to use resort-appropriate items
- Created `Supermarket/level_001.json` through `level_005.json`
- Created `Farm/level_001.json` through `level_005.json`
- All worlds now have identical level structures with world-appropriate items
- Total: 16 playable levels across 3 worlds

**Files Modified:**
- `AudioManager.cs` - Complete rewrite with ambient, worldmap music, world tracking
- `UIManager.cs` - Back button, worldmap music on startup
- `GameSceneSetup.cs` - Manager creation order fix
- `Item.cs` - Added drag/drop sound calls
- `ItemContainer.cs` - Added match sound call
- `level_003.json` - Fixed items to use resort world

**Files Created:**
- `Data/Levels/Supermarket/level_001.json` through `level_005.json`
- `Data/Levels/Farm/level_001.json` through `level_005.json`

---

## Known Issues
None currently - all identified bugs have been fixed.

---

## Next Session Checklist
1. [ ] Create Main Menu / Splash Screen
2. [ ] Test on mobile device (touch input)
3. [ ] Create Pause Menu Screen
4. [ ] Create Settings Screen with volume controls
5. [ ] Add match explosion animation
