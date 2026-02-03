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

**Last Updated:** 2026-02-02

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

---

## TODO List

### High Priority
1. **Solver Optimization: COMPLETE** - Solver matches or nearly matches player:
   - Level 8: Solver 19 = Player 19 ✓
   - Level 9: Solver 22 vs Player 21 (1 behind - requires 4-move lookahead reveal combo)
   - Level 10: Solver 23 = Player 23 ✓
   - The Level 9 gap is a greedy algorithm limitation, not a heuristic issue
2. **World-specific lock overlays** - Placeholders exist (copies of base), need custom designs
3. **Dialogue & Mascots system** - Each world has unique mascot with pop-up dialogue:
   - Typewriter text reveal (letters appear one at a time)
   - Animal Crossing-style phonetic audio (sound per letter)
   - Milestone triggers (dialogue on progress events)
   - Data-driven dialogue management
   - Tap to advance
4. **Level Complete screen (animated)** - Rebuild with proper animations
5. **Scene Transitions** - Fade between screens
6. **Mobile Testing** - Touch input validation on device

### Medium Priority
7. **Create Levels 7-100** - Complete level content for all worlds
8. **Trophy Room** - 3D shelf display for earned trophies
9. **Profile Customization** - Player profiles with customizable mascot/avatar (character, eye colors, hats, outfits, name)

### Lower Priority
10. **Level Creator Tool** - GUI-based level editor:
    - Visual container placement, world/theme selection
    - Slot configuration, container properties (locked, movement, despawn)
    - Item assignment with validation, star thresholds, timer setting
    - Play-test, export/import JSON
11. **Leaderboard System** - Competitive rankings:
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
- [ ] Scene transitions
- [ ] Mascot reactions

### Phase 7: Advanced Features - PARTIAL
- [x] ContainerMovement.cs (back_and_forth, carousel, falling)
- [x] Despawn-on-match stacking containers
- [x] Undo system
- [ ] Dialogue system
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
- Power-ups: `LevelManager.FreezeTimer(duration)`, `AddTime(seconds)`
- UI: M:SS format, flashes red under 10s, cyan when frozen

### Asset Locations (Unity)
- Item Sprites: `Resources/Sprites/Items/{World}/`
- Container Sprites: `Resources/Sprites/Containers/`
- Audio: `Resources/Audio/{Music|SFX|UI}/`
- Prefabs: `Resources/Prefabs/`
- Level Data: `Resources/Data/Levels/{World}/`

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
- Known limitation: 4-move lookahead "reveal combos" not detected (greedy algorithm)

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
