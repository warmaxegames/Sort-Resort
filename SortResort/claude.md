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

**Last Updated:** 2026-02-03

### Working Features (Unity)
- Splash screen → Level select with fade transition
- 5 worlds: Island, Supermarket, Farm, Tavern, Space (5-6 levels each, 26 total)
- Full game flow: Level Select → Play → Complete/Fail → Back/Next
- Drag-drop with visual feedback, sounds, triple-match detection
- Row advancement, locked containers with unlock animation
- Carousel movement, despawn-on-match stacking containers
- World-specific backgrounds, music, and ambient audio
- Save/load progress (PlayerPrefs), undo system, optional timer
- Achievement system (88 achievements with UI, notifications, rewards)
- Level solver tool (Editor window + in-game auto-solve button)
- **Dialogue system** - Typewriter text with Animal Crossing-style voices, mascot portraits, per-world welcome dialogues, voice toggle in settings, timer pauses during dialogue
- **Level complete screen** - Animated rays, curtains, level board, star ribbon, mascot thumbsup animation (Island)

---

## TODO List

### High Priority
1. **World-specific lock overlays** - Placeholders exist (copies of base), need custom designs
2. **Polish dialogue system** - Core working with welcome dialogues for all 5 worlds. Remaining:
   - Add more dialogue triggers (level milestones, achievements, world completion)
   - Create mascot sprites for Farm (Mara - only neutral), Tavern (Hog - missing neutral/happy)
   - Add more expression variants for existing mascots
3. **Level Complete screen (animated)** - Rays, curtains, level board, star ribbon, mascot animation done. Remaining:
   - Add level number text overlay on level board
   - Add star animations to ribbon based on star count
   - Move replay/next/level select buttons into animated screen (remove blue placeholder)
4. **Mobile Testing** - Touch input validation on device

### Medium Priority
5. **Create Levels 7-100** - Complete level content for all worlds
6. **Trophy Room** - 3D shelf display for earned trophies
7. **Profile Customization** - Player profiles with customizable mascot/avatar (character, eye colors, hats, outfits, name)

### Lower Priority
8. **Level Creator Tool** - GUI-based level editor:
    - Visual container placement, world/theme selection
    - Slot configuration, container properties (locked, movement, despawn)
    - Item assignment with validation, star thresholds, timer setting
    - Play-test, export/import JSON
9. **Leaderboard System** - Competitive rankings:
    - Total achievement points, daily challenge, weekly stars, world speed runs
    - Profile pages with mascot, titles, trophy showcase, stats
    - Requires backend service

---

## Incomplete Phases

### Phase 6: Visual Polish - PARTIAL
- [x] Match animation (15-frame pink glow effect)
- [x] Container unlock animation (scale + fade)
- [x] Row depth visualization (grayed back rows, vertical offset)
- [x] Tween animations (item scale, highlight)
- [x] Scene transitions (fade between screens)
- [ ] Mascot reactions

### Phase 7: Advanced Features - PARTIAL
- [x] ContainerMovement.cs (back_and_forth, carousel, falling)
- [x] Despawn-on-match stacking containers
- [x] Undo system
- [x] Dialogue system (working: typewriter, voices, mascot sprites, triggers, voice toggle, timer pause)
- [ ] Profile customization

### Phase 8: Content & Testing
- [x] Import all sprite assets
- [x] Levels 1-5 for all 5 worlds (26 levels total)
- [ ] Create remaining levels (7-100 for each world)
- [ ] Test on mobile devices
- [ ] Performance optimization
- [ ] Bug fixing and polish

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
- **AnimatedLevelComplete.cs**: 4 layers - rays, curtains, level board, star ribbon
- **MascotAnimator.cs**: Frame-by-frame animation for mascot thumbsup (Island world, 56 frames)
- Frames loaded via `Resources.LoadAll<Texture2D>()` from folders in `Resources/Sprites/UI/LevelComplete/`
- Mascot animation frames: `Resources/Sprites/Mascots/Animations/{World}/`

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
