# Sort Resort - Unity Rebuild Progress Tracker

## Project Overview

**Sort Resort** is a casual puzzle/sorting game for iOS, Android, and Tablets. Players drag and drop items across containers to achieve triple matches. Rebuilt from Godot 4 to Unity.

### Core Mechanics
- Drag-and-drop items between container slots
- Triple match system (3 identical items = cleared)
- Multi-row containers with depth visualization
- Locked containers with countdown unlock
- Moving containers on paths (carousel, back-and-forth)
- Star rating based on move efficiency (1-3 stars)
- 100 levels per world, 5 worlds planned

### Folder Structure
- `godot structure/` - Design documentation and context
- `sortresort/` - Original Godot 4 project (fully functional)
- `Unity redo/` - Unity rebuild target

---

## Current Status

**Last Updated:** 2026-02-04

### Working Features (Unity)
- Splash screen → Level select with fade transition
- 5 worlds: Island, Supermarket, Farm, Tavern, Space (5-11 levels each, 31 total)
- Full game flow: Level Select → Play → Complete/Fail → Back/Next
- Drag-drop with visual feedback, sounds, triple-match detection
- Row advancement, locked containers with unlock animation
- Carousel movement, despawn-on-match stacking containers
- World-specific backgrounds, music, and ambient audio
- Save/load progress (PlayerPrefs), undo system, optional timer
- Achievement system (88 achievements with UI, notifications, rewards)
- Level solver tool (Editor window + in-game auto-solve button)
- **Dialogue system** - Typewriter text with Animal Crossing-style voices, mascot portraits, per-world welcome dialogues, voice toggle in settings, timer pauses during dialogue
- **Level complete screen** - Animated rays, curtains, star ribbon, level/moves labels with text overlays, grey stars + gold star animations (1/2/3 star), dancing stars animation (3-star), mascot thumbsup animation (Island), bottom board animation, sprite-based buttons (return to map, replay, next level)
- **Level failed screen** - Fullscreen fail_screen.jpg background, reason text ("Out of Moves"/"Out of Time"), animated bottom board, sprite-based buttons (retry, level select)
- **Fail-before-last-move** - Level fails after second-to-last move if not complete (prevents awkward final-move-still-fails scenario)
- **Star threshold separation** - All thresholds guaranteed at least 1 move apart

---

## TODO List

### Priority Items
1. **Level Complete Screen** - Remaining:
   - Animated mascot art assets for each world (currently only Island has thumbsup animation)
   - Audio: music and sound effects for each scenario (1-star, 2-star, 3-star)
2. **Level Failed Screen** - Remaining:
   - Sound effect audio for failed screen
3. **World-Specific Lock Overlays** - Need custom designs for:
   - Farm
   - Supermarket
   - Space
4. **World-Specific Dialogue Boxes** - Need custom designs for:
   - Farm
   - Supermarket
   - Space
5. **Dialogue System** - Build out content:
   - Tutorial dialogue
   - World-by-world stories
   - Dialogue checkpoints throughout level progression
6. **Add More Levels** - All worlds need levels beyond current set:
   - Island
   - Farm
   - Supermarket
   - Tavern
   - Space
7. **Mobile/Device Testing** - Test performance on mobile devices, tablets, different screen sizes

### Future Lower Priority Items
8. **Trophy Room** - World-specific and achievement trophies:
   - Silhouettes for missing trophies, hover tooltip showing how to earn them
   - Vertical scroll through many shelves (extendable)
9. **Player Profile Creation/Customization**:
   - Player name
   - Player avatar
   - Player title
   - Player background
   - Player profile frame
   - Player cosmetics (clothes, hats, sunglasses, accessories)
10. **Leaderboards**:
    - Categories: achievement points, matches made, trophies, daily/weekly activity, etc.
    - Backend infrastructure for tracking/displaying leaderboards

---

## Technical Reference

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

### Container & Item Scaling System

**Reference this when making changes to avoid containers going off-screen or overlapping.**

#### Screen & Camera
- **Resolution**: 1080×1920 (portrait)
- **Screen center**: (540, 600) in Godot pixels
- **Camera orthoSize**: 9.6
- **Coordinate divisor**: 100 (pixels to Unity units)
- **Visible area**: 10.8 × 19.2 Unity units

#### Container Scaling
```
Base slot: 83 × 166 pixels
Uniform scale: 1.14
Container border scale: 1.2 (17% border around slots)
```

#### Standard 3-Column Layout (Godot X → Unity X)
| Position | Godot X | Unity X |
|----------|---------|---------|
| Left     | 200     | -3.4    |
| Center   | 540     | 0       |
| Right    | 880     | +3.4    |

#### Safe Position Bounds (Godot Pixels)
- **3-slot containers**: X: 186-894, Y: 150-1696
- **1-slot containers**: X: 72-1008

#### Key Files
| File | Purpose |
|------|---------|
| `ScreenManager.cs` | Camera orthoSize (9.6), aspect ratio |
| `ItemContainer.cs` | Slot sizing, container sprite scaling |
| `LevelValidator.cs` | Position validation |

### Carousel Train Configuration
| Layout | Spacing | Containers | move_distance |
|--------|---------|------------|---------------|
| Vertical | 198px (Y) | 10 each | 1980 |
| Horizontal | 297px (X) | 5 each | 1485 |

### Layer Configuration
- Layer 6: "Items" - for item raycasting
- Layer 7: "Slots" - for slot raycasting

### Timer System
- `time_limit_seconds` in level JSON (0 or omitted = no timer)
- Disable globally via `SaveManager.IsTimerEnabled()`
- Timer pauses automatically while dialogue is active
- Power-ups: `LevelManager.FreezeTimer(duration)`, `AddTime(seconds)`
- UI: M:SS format, flashes red under 10s, cyan when frozen

### Dialogue System
- **Data**: `Resources/Data/Dialogue/dialogues.json` (mascots, dialogues, triggers)
- **Mascots**: Whiskers (island), Tommy (supermarket), Mara (farm), Hog (tavern), Leika (space)
- **Sprites**: `Resources/Sprites/Mascots/{worldId}_{mascotName}_{expression}.png`
- **Voice clips**: `Resources/Audio/Dialogue/Letters/A-Z.wav` (per-letter Animal Crossing style)
- **Trigger types**: WorldFirstLevel (type:1), LevelComplete (type:0), plus unused types in enum
- **UI**: DialogueUI uses CanvasGroup for visibility (not SetActive), retries subscription in Update()
- **Settings**: Voice toggle via `SaveManager.IsVoiceEnabled()` / `SetVoiceEnabled()`
- **Persistence**: Played dialogues stored in PlayerPrefs key "PlayedDialogues" (pipe-delimited)
- **Reset**: `SaveManager.ResetAllProgress()` also clears played dialogues

### Level Complete Animation
- **AnimatedLevelComplete.cs**: 3 layers - rays, curtains, star ribbon (level board removed)
- **MascotAnimator.cs**: Frame-by-frame animation for mascot thumbsup (Island world, 56 frames)
- Frames loaded via `Resources.LoadAll<Texture2D>()` from folders in `Resources/Sprites/UI/LevelComplete/`
- Mascot animation frames: `Resources/Sprites/Mascots/Animations/{World}/`
- **Animation sequence**: rays+curtains+mascot → labels fade in → star ribbon → grey stars → gold stars (1-by-1) → dancing stars (3-star only) → bottom board → buttons
- **Star animations**: Star1 (9 frames), Star2 (9 frames), Star3 (9 frames) at 15fps; DancingStars (74 frames) at 24fps
- **Level Failed screen**: fail_screen.jpg background, reason text overlay, bottom board animation (shared frames), retry + level select sprite buttons

### Asset Locations (Unity)
- Item Sprites: `Resources/Sprites/Items/{World}/`
- Container Sprites: `Resources/Sprites/Containers/`
- Mascot Sprites: `Resources/Sprites/Mascots/`
- Audio: `Resources/Audio/{Music|SFX|UI}/`
- Prefabs: `Resources/Prefabs/`
- Level Data: `Resources/Data/Levels/{World}/`
- Dialogue Data: `Resources/Data/Dialogue/`

### Level Structure (All Worlds)
- level_001: 3 containers, 2 item types, intro
- level_002: 5 containers, 5 item types, multi-row
- level_003: 10 containers (carousel), 6 item types, 2 locked
- level_004: 30 containers (vertical carousel), 6 item types
- level_005: 9 containers (despawn-on-match), 6 item types
- level_006 (Island only): moving_tracks format

### Level Solver
- Editor: Tools > Sort Resort > Solver > Solve Level...
- In-game: "Solve" button in HUD
- Greedy algorithm with heuristics for pairing and row advancement
- Alerts saved when player beats solver score (`{persistentDataPath}/SolverAlerts/`)
- Move sequence output includes scores, reasons, and top 3 runner-ups for debugging
- Key heuristics added:
  - **Self-blocking penalty**: -200 when pair's 3rd item is hidden behind the pair at same container (fires even in enables-match path)
  - **Room-will-open**: +30 for pairs at full containers when non-matching items are fully accessible types (will clear soon, opening room). Also exempts "fills container" penalty.
- Solver matches player optimal on Levels 8 (19), 9 (21), 10 (23)

### Star Thresholds & Move Limits
Thresholds are based on solver optimal move count:
```
star_move_thresholds: [3-star, 2-star, 1-star, fail]

3-star: solver moves (optimal)
2-star: solver × 1.15 (rounded to nearest)
1-star: solver × 1.30 (rounded to nearest)
Fail:   solver × 1.40 (rounded to nearest)
```

Example (solver finds 10-move solution):
- 3-star: ≤10 moves
- 2-star: ≤12 moves (10 × 1.15 = 11.5 → 12)
- 1-star: ≤13 moves (10 × 1.30 = 13)
- Fail:   >14 moves (10 × 1.40 = 14)

To update all level thresholds: `Tools > Sort Resort > Solver > Update All Level Thresholds`
