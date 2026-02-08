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

**Last Updated:** 2026-02-08

### Working Features (Unity)
- Splash screen → Level select with fade transition
- 5 worlds: Island (100 levels), Supermarket, Farm, Tavern, Space (5-11 levels each for non-Island)
- Full game flow: Level Select → Play → Complete/Fail → Back/Next
- Drag-drop with visual feedback, sounds, triple-match detection
- Row advancement, locked containers with unlock animation
- Carousel movement, despawn-on-match stacking containers
- World-specific backgrounds, music, and ambient audio
- Save/load progress (PlayerPrefs), undo system, timer format M:SS.CC
- **4 Game Modes** - Free Play (green, no limits), Star Mode (pink, move-based stars), Timer Mode (blue, beat the clock), Hard Mode (gold, stars + timer). Separate progress per mode, mode selector tabs on level select, mode-specific portal tinting, mode-specific vortex animations (green/pink/blue/gold per mode), mode-specific level complete animations (stopwatch count-up for timer, "New Record!" pulse), mode-specific HUD visibility, Hard Mode locked per-world until Star+Timer 100% complete, debug unlock/lock button on level select
- Achievement system (88 achievements with UI, notifications, rewards)
- Level solver tool (Editor window + in-game auto-solve button)
- **Dialogue system** - Typewriter text with Animal Crossing-style voices, mascot portraits, voice toggle in settings, timer pauses during dialogue. Full story content: 5 worlds × 11 checkpoints (welcome + every 10 levels), 4 mode tutorial dialogues, 5 hard mode unlock dialogues. Story is mode-agnostic (fires in any mode, plays once). Dr. Miller overarching mystery across all worlds.
- **Level complete screen** - Mode-specific animations: Star Mode has rays, curtains, star ribbon, grey/gold star animations, dancing stars (3-star); Timer Mode has stopwatch count-up animation with "New Record!" bounce; Free Play skips stars; Hard Mode combines stars + timer. All modes: mascot thumbsup (Island), bottom board animation, sprite-based buttons
- **Level failed screen** - Fullscreen fail_screen.png background, reason text ("Out of Moves"/"Out of Time"), animated bottom board, sprite-based buttons (retry, level select)
- **Fail-before-last-move** - Level fails after second-to-last move if not complete (prevents awkward final-move-still-fails scenario)
- **Star threshold separation** - All thresholds guaranteed at least 1 move apart
- **Mode-specific HUD overlay** - All 4 modes have custom ui_top bar overlays (free_ui_top, stars_ui_top, timer_ui_top, hard_mode_UI_top) with mode-specific counter text positions. World icon overlay (island only). Sprite-based settings gear button (116x116, with pressed state) and undo button (142x62, with pressed state) positioned on the overlay. Settings gear opens pause menu (Resume, Restart, Settings, Quit to Menu)
- **Portal completion overlays** - Mode-specific portal overlays on level select: free_portal (checkmark) for FreePlay, 1/2/3star_portal for StarMode, timer_portal (with best time text) for TimerMode, both star+timer overlays layered for HardMode
- **Level generator** - Python reverse-play generator (`reverse_generator.py`) builds levels backwards, guaranteeing solvability. Solver-verified with retry mechanism (up to 20 seeds). Star thresholds derived from solver's actual move count. 100 Island levels generated and verified. Locked containers participate in reverse-play with unlock-timing cutoffs.

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
5. **Generate Levels for Remaining Worlds** - Create world config files like `generate_island_levels.py`:
   - Farm
   - Supermarket
   - Tavern
   - Space
6. **World Icon Assets** - HUD world icons for non-Island worlds:
   - Supermarket, Farm, Tavern, Space
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
    - Friends list leaderboard (compare scores with friends)
    - Backend infrastructure for tracking/displaying leaderboards
11. **Premium Currency** - In-game currency earned through gameplay and purchaseable, tied to shop, cosmetics, and achievements
12. **Shop & Monetization**:
    - In-app purchases for worlds (each world individually purchaseable)
    - Cosmetics shop (profile items, accessories)
    - Power-ups (timer freeze, extra moves, hints, etc.)
13. **Build Out Achievement Rewards** - Replace placeholder rewards with real rewards (premium currency, cosmetics, titles, etc.)
14. **Daily Login Rewards** - Incentivize daily play with escalating rewards
15. **Share Button** - Post scores/achievements to social media from post-level screen
16. **Accessibility Options** - Additional settings for accessibility support

### Future Roadmap Ideas
- **Ad Monetization** - Rewarded ads (extra moves, continue after fail, free power-up) and interstitial ads between levels
- **Lives/Energy System** - Lose a life on failure, lives regenerate over time or can be purchased
- **Season Pass/Battle Pass** - Tiered reward track (free + premium) with daily/weekly task progression
- **Lucky Wheel/Daily Spin** - Random reward mechanic, can tie into daily login
- **Hints/Boosters** - Show best move hint, shuffle items, wildcard match-anything item; purchaseable with premium currency
- **Continue After Fail** - Spend currency or watch ad for extra moves instead of restarting
- **Star Gates** - Require total star count to unlock next world (replay incentive)
- **Daily/Weekly Challenges** - Special levels with unique constraints and exclusive rewards
- **Seasonal Events** - Limited-time themed events with exclusive cosmetics/rewards
- **Push Notifications** - Energy refilled, daily rewards, event reminders, friend activity
- **Friend System** - Add friends, send/receive lives, see friends' progress on level map
- **App Store Rating Prompt** - Prompt for review after positive moments (e.g. 3-star completion)
- **Cloud Save** - Cross-device progress sync
- **Localization** - Multi-language support for global audience
- **Analytics** - Player behavior tracking, retention funnels, monetization metrics
- **Haptic Feedback** - Vibration on matches, combos, failures (mobile feel)
- **Offline Support** - Ensure core gameplay works without internet
- **World Completion Rewards** - Special trophy, cosmetic, or currency for 3-starring all levels in a world
- **Collection System** - Collect themed item sets across levels for bonus rewards

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

### Game Modes System
- **Enum**: `GameMode.cs` - FreePlay(0), StarMode(1), TimerMode(2), HardMode(3)
- **Save Data**: `SaveData` v2 with `List<ModeProgress>` - separate progress per mode
- **Active Mode**: `SaveManager.GetActiveGameMode()` / `SetActiveGameMode()`
- **Mode Colors**: Green (Free), Pink (Stars), Blue (Timer), Gold (Hard)
- **Hard Mode Unlock**: Per-world, requires all 100 levels in both Star + Timer mode
- **Level Select**: Mode tabs row with colored pill buttons, portal tinting per mode
- **Level Complete**: Mode-branched animation sequence (PlayStarSequence, PlayTimerCountUpAnimation)
- **HUD**: Star display hidden in FreePlay/TimerMode; timer only for TimerMode/HardMode
- **HUD Overlay**: All 4 modes have per-mode fullscreen bar overlay (`free_ui_top`, `stars_ui_top`, `timer_ui_top`, `hard_mode_UI_top`). Counter texts positioned per mode (FreePlay: level only; StarMode: level+moves; TimerMode: level+timer; HardMode: level+moves+timer). World icon overlay (`{worldId}_icon_UI_top`, currently island only). White counter text color.
- **HUD Buttons**: Record (debug), Solve (debug) in button row. Settings gear (116x116 sprite with pressed state) and Undo (142x62 sprite with pressed state) on settings overlay. Settings gear opens pause menu.
- **Portal Overlays**: Level select portals show completion status: `free_portal` (checkmark) for FreePlay, `1/2/3star_portal` for StarMode, `timer_portal` (with best time text) for TimerMode, both star+timer layered for HardMode.
- **World Unlocks**: Shared across all modes via `GetWorldCompletedLevelCountAnyMode()`
- **LevelCompletionData**: Struct with levelNumber, starsEarned, timeTaken, mode, isNewBestTime

### Timer System
- `time_limit_seconds` in level JSON (0 or omitted = no timer)
- Timer active only in TimerMode and HardMode (mode-driven, not settings-driven)
- Runtime fallback for missing timer values: `FailThreshold * 6` seconds
- Timer pauses automatically while dialogue is active
- Power-ups: `LevelManager.FreezeTimer(duration)`, `AddTime(seconds)`
- UI: M:SS.CC format (centiseconds), flashes red under 10s, cyan when frozen
- Overlay timer: dark red flash and dark teal frozen (visible on light wood background)

### Dialogue System
- **Data**: `Resources/Data/Dialogue/dialogues.json` (mascots, dialogues, triggers)
- **Story Bible**: `Story.txt` (root folder) - full narrative, all dialogue text, design notes
- **Mascots**: Whiskers (island cat), Tommy (supermarket raccoon), Mara (farm alpaca), Mason (tavern hog), Leika (space dog)
- **Sprites**: `Resources/Sprites/Mascots/{worldId}_{mascotName}_{expression}.png`
- **Voice clips**: `Resources/Audio/Dialogue/Letters/A-Z.wav` (per-letter Animal Crossing style)
- **Trigger types**: LevelComplete (0), WorldFirstLevel (1), WorldComplete (2), MatchMilestone (3), StarMilestone (4), FirstPlay (5), ReturnAfterAbsence (6), Achievement (7), ModeFirstPlay (8), HardModeUnlock (9)
- **Mode-agnostic story**: Story dialogues fire on level completion in ANY mode. `playOnce: true` prevents repeats across modes. Player completes world story by reaching level 100 in any single mode.
- **Mode tutorials**: ModeFirstPlay triggers (type 8) fire first time player switches to each mode. Uses `gameMode` field on trigger (0=FreePlay, 1=StarMode, 2=TimerMode, 3=HardMode). Whiskers delivers all mode tutorials.
- **Hard Mode unlock**: HardModeUnlock triggers (type 9) fire when Hard Mode unlocks for a world. Each world's own mascot congratulates. Checked in `GameManager.CompleteLevel()` after saving progress.
- **Dialogue content**: 65 dialogue sequences total - 5 worlds × 11 checkpoints (welcome + levels 10-100) + 4 mode tutorials + 5 hard mode unlocks
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
- **Level Failed screen**: fail_screen.png background, reason text overlay, bottom board animation (shared frames), retry + level select sprite buttons

### Asset Locations (Unity)
- Item Sprites: `Resources/Sprites/Items/{World}/`
- Container Sprites: `Resources/Sprites/Containers/`
- Mascot Sprites: `Resources/Sprites/Mascots/`
- HUD Overlays: `Resources/Sprites/UI/HUD/` (`free_ui_top`, `stars_ui_top`, `timer_ui_top`, `hard_mode_UI_top`, `island_icon_UI_top`, `settings_button`, `settings_button_pressed`, `undo_button`, `undo_button_pressed`)
- Portal Overlays: `Resources/Sprites/UI/Icons/` (`1star_portal`, `2star_portal`, `3star_portal`, `timer_portal`, `free_portal`)
- Audio: `Resources/Audio/{Music|SFX|UI}/`
- Prefabs: `Resources/Prefabs/`
- Level Data: `Resources/Data/Levels/{World}/`
- Dialogue Data: `Resources/Data/Dialogue/`

### Level Generator (Python)
- **Infrastructure**: `level_generator.py` - WorldConfig, progression curves, container builder, specs
- **Reverse generator**: `reverse_generator.py` - reverse-play item placement + solver-verified generation
- **Island config**: `generate_island_levels.py` - imports from reverse_generator, defines 50 Island items
- **Python solver**: `level_solver.py` - greedy solver port for verification during generation
- **Usage**: `python generate_island_levels.py` (creates 100 levels in `Resources/Data/Levels/Island/`)
- **Algorithm**: Reverse-play construction V2 (no work container). For each triple: pick random container, push existing items deeper, place triple at front, scatter 1-3 items to other containers. Each scatter = 1 forward move.
- **Locked containers**: Participate in reverse-play loop with cutoff timing. Last `unlock_matches_required` triples exclude locked container (those forward triples unlock it).
- **Validation**: No starting triples at ANY row depth. Solver verifies each level; retries with different seed on failure (up to 20 attempts). Star thresholds use solver's actual move count.
- **To add a new world**: Create `generate_{world}_levels.py` with a `WorldConfig` defining item groups, then run it.

### Level Structure (All Worlds)
- level_001: 3 containers, 2 item types, intro (hardcoded 2-move tutorial)
- Levels 2-10: 4-7 containers, increasing item types, multi-row
- Levels 11+: locked containers introduced
- Levels 16+: single-slot containers
- Levels 26+: back-and-forth movement
- Levels 31+: carousel movement
- Levels 36+: despawn-on-match
- Levels 41+: all mechanics combined
- Fill ratios: 57-87% across levels, 50 item types per world

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
