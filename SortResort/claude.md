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

**Last Updated:** 2026-01-30 (Session 6)

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
- Splash screen with play button and fade transition to level select
- Level selection screen with 100-level grid and world navigation
- **5 worlds configured:** Island, Supermarket, Farm, Tavern, Space (5 levels each)
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
1. ~~**Main Menu / Splash Screen** - Start button, settings access~~ DONE
2. ~~**Rename "resort" to "island"**~~ DONE - All references renamed
3. ~~**Add Space & Tavern worlds**~~ DONE - All 5 worlds now have items, containers, backgrounds, icons, music
4. **World-specific lock overlays** - Created placeholders for all worlds (copies of base), need custom designs
5. ~~**Island custom containers**~~ DONE - Copied island_container.png and island_single_slot_container.png from Godot
6. ~~**Update world map**~~ DONE - Level select screen now matches Godot version (light cyan background, top bar with mascot/settings/close, larger world image with side arrows, dark gray level grid with scrollbar)
7. **Dialogue & Mascots system** - Each world has unique mascot. Pop-up dialogue at set points for story progression. Review Godot dialogue system.
8. **Level Complete screen (animated)** - Rebuild with proper animations (Godot version was limited)
9. **Mobile Testing** - Touch input validation on device
10. **Pause Menu Screen** - Resume, settings, quit options
11. **Settings Screen** - Volume sliders for music/SFX

### Future Priorities
5. **Match Animation** - Use 18-frame explosion sprites
6. **Scene Transitions** - Fade between screens
7. **Dialogue System** - Tutorial and story dialogues
8. **Undo System** - Allow undoing last move
9. **Create Levels 7-100** - Complete level content for all worlds
10. **Profile Customization** - Player profiles with customizable mascot/avatar:
    - Choose mascot character
    - Customize eye colors
    - Select hats/headwear
    - Choose outfit/clothing
    - Player name editing

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
- [x] **26 level files total** (Island 6, Supermarket 5, Farm 5, Tavern 5, Space 5)

### Phase 3: UI Implementation - MOSTLY COMPLETE
- [x] **3.1** MainMenuScreen / SplashScreen
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
- [x] **8.2** Levels 1-5 for all 5 worlds (26 levels total including Island 006)
- [x] **8.2.1** Fix all level item counts (exactly 3 of each type)
- [x] **8.2.2** Fix all container image references (world-specific)
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

| Island | Supermarket | Farm | Tavern | Space |
|--------|-------------|------|--------|-------|
| bluesunglasses | chips | potato | crown | aliengreen |
| coconut | honey | carrot | shield_1 | astronauthelmet |
| greencoconut | eggs | turnip | cheese | crystalblue |
| pinkpolkadotbikini | ketchup | redtomato | drumstick | lasergun |
| sunscreenspf50 | mustard | whiteonion | goldcoin | meteor |
| surfboardpalmtrees | milk | romaine | mugbeer | rocket |
| sandals | soda | greentomato | sword_1 | satellite |
| lemonade | orangejuice | carrot | mugale | planet_blue |
| margarita | grapesoda | potato | crossbow | planet_red |
| icecreamcone | chocolatebar | turnip | torch | raygun |
| sandpale_blue | pickles | redtomato | helmet | ufo |
| flipflops | peanutbutter | whiteonion | axe | spacesuit |
| sunhat | mayo | romaine | bread | oxygentank |
| pineapple | popcornbucket | greentomato | apple | starmap |
| sunscreen | cider | turnip | grapes | alienblue |
| beachball_mixed | coffeebeans | carrot | chalice | crystalpurple |

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

### 2026-01-30 - Splash Screen Implementation

**Splash Screen Added:**
- Created `SplashScreen.cs` - new UI screen component extending BaseScreen
- Full-screen background using `splashscreen.png` (cat mascot + logo)
- Play button with sprite swap (blue normal → red pressed states)
- "Warp" sound effect on button press
- Fade transition from splash screen to level select

**Files Created:**
- `Scripts/UI/Screens/SplashScreen.cs` - Splash screen component

**Files Modified:**
- `AudioManager.cs` - Added warpClip and PlayWarpSound() method
- `UIManager.cs` - Added CreateSplashPanel(), ShowSplash(), OnSplashPlayClicked()
- `GameSceneSetup.cs` - Added TransitionManager creation, initial state is now MainMenu

**Assets Copied to Resources:**
- `Resources/Sprites/UI/Backgrounds/splashscreen.png`
- `Resources/Sprites/UI/Buttons/play_button.png`
- `Resources/Sprites/UI/Buttons/play_button_pressed.png`

**Rename "resort" to "island" (World Rename):**
- Renamed all code references in 8 C# files (SaveManager, WorldProgressionManager, LevelSelectScreen, UIManager, AudioManager, GameSceneSetup, TestSceneSetup, LevelData, LevelManager)
- Renamed folder `Resources/Sprites/Items/Resort/` → `Resources/Sprites/Items/Island/`
- Renamed folder `Resources/Data/Levels/Resort/` → `Resources/Data/Levels/Island/`
- Renamed file `Art/UI/Worlds/resort_world.png` → `island_world.png`
- Renamed file `Art/UI/Backgrounds/resort_background.png` → `island_background.png`
- Renamed 5 dialogue files `dialogue_resort_*` → `dialogue_island_*`
- Updated `worlds.json` - changed id, name, all file references
- Updated `items.json` - all "world": "resort" → "world": "island"
- Updated `triggers.json` - all resort references
- Updated all 6 Island level JSON files (world_id)

**Cleanup:**
- Removed empty `Resources/Sprites/Items/Tavern/` folder
- Removed empty `Resources/Database/` folder

---

### 2026-01-30 (Session 2) - Level Data Fixes & Container Assets

**Level JSON Fixes:**
- Fixed all level files to use world-specific container images (replaced "base_shelf" references)
- Fixed item counts to exactly 3 of each type for triple-match completion
- Verified all items in levels have matching sprites in their world folders
- Redesigned level_005 (despawn-on-match) across Island/Farm/Supermarket:
  - Reduced from 18 containers to 9 containers (3 stacks of 3)
  - Now has 6 item types × 3 copies = 18 total items
  - ~33% empty space for maneuvering
  - Uses multi-row slots for complexity

**Container Image Updates:**
- Copied island_container.png from Godot to Unity
- Copied island_single_slot_container.png from Godot to Unity

**Worlds & Levels Summary:**
| World | Levels | Container | Lock Overlay |
|-------|--------|-----------|--------------|
| Island | 6 | island_container | island_lockoverlay |
| Supermarket | 5 | supermarket_container | supermarket_lockoverlay |
| Farm | 5 | farm_container | farm_lockoverlay |
| Tavern | 5 | tavern_container | tavern_lockoverlay |
| Space | 5 | space_container | space_lockoverlay |

**Level Structure:**
- level_001: 3 containers, 2 item types, intro level
- level_002: 5 containers, 5 item types, multi-row
- level_003: 10 containers (carousel), 6 item types, 2 locked containers
- level_004: 30 containers (vertical carousel), 6 item types
- level_005: 9 containers (despawn-on-match), 6 item types, stacking mechanic
- level_006 (Island only): moving_tracks format

**Files Modified:**
- All level JSON files in Island/, Supermarket/, Farm/, Tavern/, Space/

---

### 2026-01-30 (Session 3) - Critical Bug Fixes

**HUD Reset Bug Fix:**
- GameManager now subscribes to `GameEvents.OnLevelStarted` event
- When any level starts, `currentMoveCount` and `currentMatchCount` are reset to 0
- This ensures the HUD displays correctly when clicking "Next Level" or selecting levels

**Items.json Format Fix:**
- Fixed items.json format from dictionary to array format
- Changed from: `{"potato": {"id": "potato", ...}}`
- To: `{"items": [{"id": "potato", ...}]}`
- This was causing the entire item database to fail to parse
- Farm and Tavern items now load correctly

**ObjectPool Null Reference Fix (Previous Session):**
- Added null checks in `ObjectPool.Get()` to skip destroyed objects
- Prevents MissingReferenceException when reusing pooled items

**Level_002 Container Overlap Fix (Previous Session):**
- Moved container_5 from y:455 to y:900 to prevent overlap with corner containers
- Applied to Island, Farm, and Supermarket level_002 files

**Files Modified:**
- `GameManager.cs` - Added OnLevelStarted handler to reset counters
- `items.json` - Converted from dictionary format to array format with "items" wrapper

---

### 2026-01-30 (Session 4) - Level Select Screen Godot-Style Redesign

**Level Select Screen Overhaul:**
- Redesigned to match Godot version's visual style
- Added world sprite display (island_world.png, supermarket_world.png, etc.)
- Navigation arrows now use button_left.png/button_right.png sprites
- Level grid changed from 5 columns to 3 columns (like Godot)
- Level buttons now use level_portal.png as background
- Star ratings now use portal_1/2/3_stars.png overlay sprites
- Scroll container has muted green background (RGB 0.403, 0.462, 0.388)
- Background uses base_world_background.png

**Assets Copied from Godot:**
- `Sprites/UI/Worlds/`: island_world.png, supermarket_world.png, farm_world.png, tavern_world.png, space_world.png
- `Sprites/UI/Buttons/`: button_left.png, button_right.png (with pressed states), back_button.png, replay_button.png, next_level_button.png, return_to_map_button.png, settings_button.png
- `Sprites/UI/Icons/`: level_portal.png, portal_1_stars.png, portal_2_stars.png, portal_3_stars.png
- `Sprites/UI/Backgrounds/`: base_world_background.png

**Container Overlap Fixes:**
- Fixed Tavern level_003, level_004, level_005 (container_5 y:505/450 → y:900)
- Fixed Space level_003, level_004, level_005 (container_5 y:505/450 → y:900)

**Sprite Import Fixes:**
- Fixed 32 item sprites with incorrect spriteMode (Multiple → Single)
- Items were sliced into multiple sub-sprites causing invisible/tiny sprites
- Affected worlds: Tavern, Space, Island, Farm, Supermarket

**Files Modified:**
- `UIManager.cs` - Rewrote CreateLevelSelectPanel() with Godot-style layout
- `LevelSelectScreen.cs` - Added portal sprite support, star overlay sprites, world image display

---

### 2026-01-30 (Session 5) - Level Select Screen Visual Matching

**Screenshot Comparison Update:**
- Compared Unity screenshot to Godot screenshot to identify visual differences
- Updated layout to match Godot version more closely

**Layout Changes:**
- Background: Changed from base_world_background.png to light cyan solid color (#A4DEDE)
- Added top bar with:
  - Cat mascot avatar (cat_mascot.png - copied from Godot cat_smug.png)
  - Player name panel (wood brown background)
  - Settings button (right side)
  - Close/Back button (far right)
- World area: Expanded to ~38% of screen height (was 25%)
- Navigation arrows: Larger (100x100), positioned at sides of world image
- World image: Now spans 70% width (anchors 0.15-0.85) for larger display
- Level grid: Darker gray background (#565D51), separate viewport and scrollbar
- Added visible white scrollbar with handle

**Level Button Improvements:**
- Locked levels now show gray-tinted portal instead of dark overlay
- Stars now always visible: gray when not earned, normal color when earned
- Text color also dims for locked levels

**Assets Copied:**
- `Sprites/UI/Icons/cat_mascot.png` - copied from Godot mascots/cat_smug.png

**Files Modified:**
- `UIManager.cs` - Complete rewrite of CreateLevelSelectPanel() with new layout
- `LevelSelectScreen.cs` - Updated LevelButton.UpdateState() for better locked level display

---

### 2026-01-30 (Session 6) - Level Select Screen Polish

**Rounded Corners Implementation:**
- Added `CreateRoundedRectTexture()` and `CreateRoundedRectSprite()` helper methods to UIManager
- These create rounded rectangle textures at runtime for UI elements
- Scroll area background now uses a 9-sliced rounded rect sprite (30px corner radius)
- Scrollbar track uses rounded rect sprite (15px corner radius)
- Scrollbar handle uses rounded rect sprite (12px corner radius)
- All rounded elements properly scale using Image.Type.Sliced

**Star Alignment Improvements:**
- Adjusted star overlay positioning to better fit portal's dark circles
- Stars now positioned at anchors (0.10, 0.02) to (0.90, 0.28)
- Level number text repositioned higher in orb area: (0.10, 0.30) to (0.90, 0.90)
- Text outline color changed to purple tint (80, 0, 80) for better visibility on pink portal

**Visual Details:**
- Scrollbar widened to 30px for better touch targets
- RectMask2D softness reduced to 25px (was 40px)
- Level number font size adjusted to 72 (was 80)

**Files Modified:**
- `UIManager.cs` - Added rounded rect generation methods, updated scroll area/scrollbar sprites
- `LevelSelectScreen.cs` - Adjusted star and text positioning in portal buttons

---

## Known Issues
None currently - all identified bugs have been fixed.

---

## Next Session Checklist
1. [x] Create Main Menu / Splash Screen
2. [x] Fix level data (item counts, container images)
3. [x] Copy island container images from Godot
4. [x] Fix HUD not resetting between levels
5. [x] Fix items.json format for proper parsing
6. [x] Update world map to match Godot version
7. [ ] Test on mobile device (touch input)
8. [ ] Create Pause Menu Screen
9. [ ] Create Settings Screen with volume controls
10. [ ] Add match explosion animation
11. [ ] Implement dialogue/mascot system
