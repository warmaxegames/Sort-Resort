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

**Last Updated:** 2026-02-23

### Working Features (Unity)
- Splash screen → Level select with fade transition
- 5 worlds: Island (100), Supermarket (100), Farm (100), Space (100), Tavern (100) levels generated
- Full game flow: Level Select → Play → Complete/Fail → Back/Next
- Drag-drop with visual feedback, sounds, triple-match detection
- Row advancement, locked containers with unlock animation
- Carousel movement, despawn-on-match stacking containers
- World-specific backgrounds, music, and ambient audio
- Save/load progress (PlayerPrefs), undo system, timer format M:SS.CC
- **4 Game Modes** - FreePlay (green), StarMode (pink), TimerMode (blue), HardMode (gold). Separate progress, mode-specific HUD/portal/animations. Hard Mode locked until Star+Timer 100%.
- **Achievement system** - 45 achievements, card-based UI, tier art, sprite progress bars, notifications with sound
- **Dialogue system** - 65 sequences (5 worlds × 11 checkpoints + mode tutorials + hard mode unlocks), typewriter text, Animal Crossing voices, mode-agnostic story
- **Level complete/failed screens** - Mode-specific animations (stars, timer count-up), mascot thumbsup, sprite-based buttons
- **HUD overlay** - Per-mode ui_top bars, counter text, sprite gear/undo buttons, pause menu
- **Portal overlays** - Mode-specific completion indicators on level select portals
- **Level generator** - Python reverse-play generator, solver-verified, 500 levels across 5 worlds
- **Benzin font system** - FontManager utility, TMP default, ApplyBold() on 8 scripts
- **Combo text effects** - "GOOD!"/"AMAZING!"/"PERFECT!" on consecutive match streaks (2/3/4+), 18-frame animation + tween fade-out, per-word sound effects
- Level solver tool (Editor window + in-game auto-solve button)

---

## TODO List

### Priority Items
1. ~~**Improve Solver for Heavily-Locked Levels**~~ - DONE. Ensemble solver (5 strategies × 4 runs = 20 attempts) + 6 heuristic improvements. Total: 5,969 moves across 100 levels (was 6,182). Zero regressions.
2. **Level Complete Screen** - Remaining:
   - Animated mascot art assets for each world (currently only Island has thumbsup animation)
   - Audio: music and sound effects for each scenario (1-star, 2-star, 3-star)
3. **Level Failed Screen** - Remaining:
   - Sound effect audio for failed screen
4. **World-Specific Lock Overlays** - Need custom designs for:
   - Farm
   - Supermarket
   - Space
5. **World-Specific Dialogue Boxes** - Need custom designs for:
   - Supermarket
6. ~~**Generate Levels for Remaining Worlds**~~ - DONE. All 5 worlds have 100 solver-verified levels (500 total):
   - ~~Supermarket~~ - DONE (`generate_supermarket_levels.py`, 51 items, offset=10, 100/100 solver-verified)
   - ~~Farm~~ - DONE (`generate_farm_levels.py`, 52 items, offset=10, 100/100 solver-verified)
   - ~~Tavern~~ - DONE (`generate_tavern_levels.py`, 50 items, offset=10, 100/100 solver-verified)
   - ~~Space~~ - DONE (`generate_space_levels.py`, 50 items, offset=10, 100/100 solver-verified)
7. **World Icon Assets** - HUD world icons for non-Island worlds:
   - Supermarket, Farm, Tavern, Space
8. **Mobile/Device Testing** - Test performance on mobile devices, tablets, different screen sizes

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
- **Object Pooling**: 120-250 item pool for mobile performance
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

#### Key Files
| File | Purpose |
|------|---------|
| `ScreenManager.cs` | Camera orthoSize (9.6), aspect ratio |
| `ItemContainer.cs` | Slot sizing, container sprite scaling |
| `LevelValidator.cs` | Position validation |

### Carousel Train Configuration
| Layout | Spacing | Containers | move_distance | Notes |
|--------|---------|------------|---------------|-------|
| Horizontal | ~351px (X) | 5 min | 5 × spacing | First container 100px off-screen at spawn |
| Vertical | ~227px (Y) | 10 fixed | 10 × spacing | L50+, 40% chance. Edge-to-edge positioning. Statics use 2-col |
- **Mutual exclusion**: Carousel and despawn never appear on the same level (spatial conflict: carousel path at Y=200 overlaps despawn stack). When both selected by probability, prefer whichever is in its introduction range; otherwise coin flip.

### Layer Configuration
- Layer 6: "Items" - for item raycasting
- Layer 7: "Slots" - for slot raycasting

### Game Modes System
- **Enum**: `GameMode.cs` - FreePlay(0), StarMode(1), TimerMode(2), HardMode(3)
- **Save Data**: `SaveData` v2 with `List<ModeProgress>` - separate progress per mode
- **Active Mode**: `SaveManager.GetActiveGameMode()` / `SetActiveGameMode()`
- **Mode Colors**: Green (Free), Pink (Stars), Blue (Timer), Gold (Hard)
- **Hard Mode Unlock**: Per-world, requires all 100 levels in both Star + Timer mode
- **Level Select**: Mode tabs with sprite-based tab images (`free_tab`, `stars_tab`, `timer_tab`, `hard_tab`), portal tinting per mode
- **Level Complete**: Mode-branched animation sequence (PlayStarSequence, PlayTimerCountUpAnimation)
- **HUD**: Star display hidden in FreePlay/TimerMode; timer only for TimerMode/HardMode
- **HUD Overlay**: Per-mode fullscreen bar overlay with counter texts (FreePlay: level; StarMode: level+moves; TimerMode: level+timer; HardMode: all three). World icon overlay (island only). Sprite gear/undo buttons.
- **HUD Buttons**: Settings gear opens pause menu. Debug: Record, Solve buttons.
- **Pause Menu**: Sprite-based fullscreen overlays (1080x1920). Board + 4 buttons with pressed states. `LoadFullRectSprite()` bypasses alpha-trimming. Canvas sortingOrder=5200.
- **Portal Overlays**: Level select portals show completion status: `free_portal` (checkmark) for FreePlay, `1/2/3star_portal` for StarMode, `timer_portal` (with best time text) for TimerMode, both star+timer layered for HardMode.
- **World Unlocks**: Shared across all modes via `GetWorldCompletedLevelCountAnyMode()`
- **LevelCompletionData**: Struct with levelNumber, starsEarned, timeTaken, mode, isNewBestTime

### Timer System
- `time_limit_seconds` in level JSON (0 or omitted = no timer)
- Timer active only in TimerMode and HardMode (mode-driven, not settings-driven)
- Runtime fallback for missing timer values: `FailThreshold * 6` seconds
- Timer pauses automatically while dialogue is active
- Timer and tick-tock sound stop immediately on level complete (before victory sound plays)
- Power-ups: `LevelManager.FreezeTimer(duration)`, `AddTime(seconds)`
- UI: M:SS.CC format (centiseconds), flashes red under 10s, cyan when frozen
- Overlay timer: dark red flash and dark teal frozen (visible on light wood background)
- Level complete count-up: 1.5s animation from 0:00.00 to final time, sound stops when count finishes

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
- **MascotAnimator.cs**: Frame-by-frame animation for mascot thumbsup, static frame caching, preloaded at level start
- Mascot animations: Island (56 frames/30fps), Supermarket (80 frames/42fps), Farm (31 frames/16fps) — all ~1.9s duration
- Mascot animation frames: `Resources/Sprites/Mascots/Animations/{World}/`
- Victory board: `Resources/Sprites/UI/LevelComplete/VictoryBoard/{worldId}_board_victory_screen.png`
- **Animation sequence**: rays+curtains+mascot → labels fade in → star ribbon → grey stars → gold stars (1-by-1) → dancing stars (3-star only) → bottom board → buttons
- **Star animations**: Star1 (9 frames), Star2 (9 frames), Star3 (9 frames) at 15fps; DancingStars (74 frames) at 24fps
- **Level Failed screen**: fail_screen.png background, reason text overlay, bottom board animation (shared frames), retry + level select sprite buttons

### Adding New Image/Animation Assets (IMPORTANT)
All fullscreen UI images and animation frames **MUST** be cropped and registered in crop metadata before use. Failing to do this causes oversized textures, inconsistent sizing across worlds, and animation stutter.

#### For single images (victory boards, overlays, etc.):
1. **Crop** the PNG to its visible content bounding box (remove transparent padding):
   ```python
   from PIL import Image
   img = Image.open("source.png")
   bbox = img.getbbox()  # (x, y, x2, y2) of non-transparent content
   cropped = img.crop(bbox)
   cropped.save("Resources/.../final.png")
   ```
2. **Add crop metadata** to `Resources/Data/crop_metadata.json` with the original position:
   ```json
   "Sprites/UI/.../final": {
     "x": <bbox[0]>, "y": <bbox[1]>,
     "w": <bbox[2]-bbox[0]>, "h": <bbox[3]-bbox[1]>,
     "orig_w": 1080, "orig_h": 1920
   }
   ```
3. Code uses `LoadFullRectSprite()` + `CropMetadata.ApplyCropAnchors()` to position correctly.
4. **Always reset RectTransform anchors to fullscreen** before calling `ApplyCropAnchors` to prevent stale anchors from a previous world persisting.

#### For animation frames (mascot thumbsup, etc.):
1. **Find the union bounding box** across ALL frames (the largest area any frame uses).
2. **Crop all frames** to that union box so every frame has identical dimensions.
3. **Deduplicate** consecutive identical frames (hash-compare) to reduce frame count.
4. **Save** with naming convention `{animname}_{NNNNN}.png` (5-digit zero-padded).
5. **Add crop metadata** with `"is_group": true` for the folder path.
6. **Register** in `MascotAnimator.GetVictoryAnimationName()` and `GetVictoryAnimationFPS()`.
7. Frames are loaded as `Texture2D` + `Sprite.Create(fullRect)` to prevent Unity auto-trim stutter.
8. Frames are **statically cached** in MascotAnimator and **preloaded at level start** via `MascotAnimator.PreloadFrames()` to avoid freeze at level complete time.

### Asset Locations (Unity)
- Item Sprites: `Resources/Sprites/Items/{World}/`
- Container Sprites: `Resources/Sprites/Containers/`
- Mascot Sprites: `Resources/Sprites/Mascots/`
- HUD Overlays: `Resources/Sprites/UI/HUD/` (`free_ui_top`, `stars_ui_top`, `timer_ui_top`, `hard_mode_UI_top`, `island_icon_UI_top`, `settings_button`, `settings_button_pressed`, `undo_button`, `undo_button_pressed`, `free_tab`, `stars_tab`, `timer_tab`, `hard_tab`)
- Fonts: `Resources/Fonts/` (Benzin-Bold/SemiBold/Medium/ExtraBold SDF assets), source TTFs in `_Project/Fonts/`
- Pause Menu: `Resources/Sprites/UI/PauseMenu/` (`pause_board`, `pause_resume`, `pause_resume_pressed`, `pause_restart`, `pause_restart_pressed`, `pause_settings`, `pause_settings_pressed`, `pause_quit`, `pause_quit_pressed`)
- Portal Overlays: `Resources/Sprites/UI/Icons/` (`1star_portal`, `2star_portal`, `3star_portal`, `timer_portal`, `free_portal`)
- Dialogue Boxes: `Resources/Sprites/UI/Dialogue/` (`dialoguebox_island`, `dialoguebox_tavern`, `dialoguebox_space`, `dialoguebox_farm`)
- Achievement Art: `Resources/Sprites/UI/Achievements/` (`{artKey}_{tier}.png` - 7 categories × 4 tiers = 28 images, plus `progress_bar.png`, `closebutton_2.png`, `closebutton_pressed.png`)
- Audio: `Resources/Audio/{Music|SFX|UI}/` (includes `achievement_sound` in SFX)
- Prefabs: `Resources/Prefabs/`
- Level Data: `Resources/Data/Levels/{World}/`
- Dialogue Data: `Resources/Data/Dialogue/`

### Achievement System
- **Total**: 45 achievements (15 general + 30 per-world), down from 88
- **Structure**: Each achievement group has exactly 3 milestones (Bronze/Silver/Gold)
- **Files**: `Achievement.cs` (data types, enums, constants), `AchievementManager.cs` (singleton, tracking, events)
- **Tabs**: Recent, General, Island, Supermarket, Farm, Tavern, Space (horizontal scrollable)
- **Recent tab**: Shows individually unlocked milestones sorted by date descending
- **Category tabs**: Shows achievement cards grouped by category

#### General Tab - 5 Groups (15 achievements)
| Group ID | Description | Bronze | Silver | Gold | Tracking |
|----------|-------------|--------|--------|------|----------|
| `3star_levels_total` | 3 Star X Levels | 100 | 250 | 500 | Unique |
| `levels_total` | Complete X Levels | 10 | 100 | 500 | Unique |
| `matches_total` | Make X Matches | 100 | 1000 | 5000 | Total |
| `stars_total` | Earn X Stars | 50 | 500 | 1000 | Total |
| `world_explorer` | Visit X Worlds | 1 | 3 | 5 | Unique |

#### Per-World Tabs - 2 Groups Each (6 achievements × 5 worlds = 30)
| Group ID | Description | Bronze | Silver | Gold | Tracking |
|----------|-------------|--------|--------|------|----------|
| `{worldId}_levels` | Complete X Levels in World | 25 | 50 | 100 | Unique |
| `{worldId}_stars` | Earn X Stars in World | 100 | 200 | 300 | Total |

#### Art Assets
- 28 rectangle images: 7 art keys × 4 tiers (grey/bronze/silver/gold)
- Art keys: `3star_levels`, `levels`, `matches`, `stars`, `visit_worlds`, `world_levels`, `world_stars`
- Path: `Resources/Sprites/UI/Achievements/{artKey}_{tier}.png`
- Loaded via `LoadFullRectSprite()` to bypass Unity alpha-trimming

#### Card-Based UI (COMPLETE)
- Fullscreen overlay (Canvas sortingOrder=5200), header with tabs, scrollable card list
- Cards: tier art rectangle (grey→bronze→silver→gold), title, description, sprite progress bar
- `FormatTextForWrapping(text, maxChars)` for long titles/descriptions
- Per-world descriptions use "in this World" (context from active tab)
- Helper methods: `GetGroupArtKey()`, `GetGroupCurrentTier()`, `GetNextMilestone()`, `GetGroupProgress()`, `GetRecentlyUnlocked()`, `GetGroupIdsForTab()`
- Notification: slides in from top, `achievement_sound.mp3` at 1.3× volume, queue for multiples

### Font System
- **Font family**: Benzin (Bold, SemiBold, Medium, ExtraBold) - TTFs in `Assets/_Project/Fonts/`, SDF assets in `Resources/Fonts/`
- **FontManager.cs**: Static utility with lazy-loaded properties (`.Bold`, `.SemiBold`, `.Medium`, `.ExtraBold`). `ApplyBold()` called in 8 scripts with serialized TMP fields.
- **TMP default**: Benzin-Bold set as TMP default so dynamically-created text auto-inherits
- **Editor tools**: `Tools > Sort Resort > Fonts` for SDF generation and setting TMP default

### Combo Text System
- **Files**: `ComboTracker.cs` (static streak tracking), `ComboTextEffect.cs` (hybrid animation + tween)
- **Streak**: 2 consecutive = "GOOD!", 3 = "AMAZING!", 4+ = "PERFECT!"
- **Animation**: 18 frames at 30fps (0.6s) for scale-up + shine, then 0.15s hold, 0.25s fade-out (1s total)
- **Assets**: `Resources/Sprites/Effects/Combo{Good,Amazing,Perfect}/` (18 frames each), `Resources/Audio/SFX/combo_{good,amazing,perfect}.mp3`
- **Hooks**: `ComboTracker.NotifyDropStarted()` in `Item.DropOnSlot()`, `ComboTracker.NotifyMatch()` in `ItemContainer.ProcessMatch()`
- **Reset**: `ComboTracker.Reset()` in `LevelManager.LoadLevel()`, `ComboTextEffect.DestroyAll()` in `GameManager.CompleteLevel()`/`FailLevel()`
- **Chain match handling**: Multiple matches on same drop (row advance reveals) count as single combo increment

### Level Generator (Python)
- **Infrastructure**: `level_generator.py` - WorldConfig, progression curves, container builder, specs
- **Reverse generator**: `reverse_generator.py` - reverse-play item placement + solver-verified generation
- **World configs**: `generate_island_levels.py` (50 items), `generate_supermarket_levels.py` (51 items), `generate_farm_levels.py` (53 items), `generate_space_levels.py` (50 items), `generate_tavern_levels.py` (50 items)
- **Python solver**: `level_solver.py` - greedy solver port for verification during generation
- **Usage**: `python generate_{world}_levels.py` (creates 100 levels in `Resources/Data/Levels/{World}/`)
- **Algorithm**: Reverse-play construction V2 (no work container). For each triple: pick random container, push existing items deeper, place triple at front, scatter 1-3 items to other containers. Each scatter = 1 forward move.
- **Locked containers**: Participate in reverse-play loop with cutoff timing. Last `unlock_matches_required` triples exclude locked container (those forward triples unlock it).
- **Validation**: No starting triples at ANY row depth. Solver verifies each level; retries with different seed on failure (up to 20 attempts). Star thresholds use solver's actual move count. Full AABB bounding box checks for overlap and off-screen detection.
- **Complexity offset**: Non-default worlds use `complexity_offset` in WorldConfig so L1 starts at higher complexity. E.g. `complexity_offset=10` means L1 plays like Island L11 (locked containers just unlocked, ~9 containers). Mechanics unlock gradually across L1-L26, all unlocked by L31+. Item availability still uses actual level number.
- **Range-based generation**: `generate_levels(config, dir, start_level=X, end_level=Y)` generates only a subset without deleting others, enabling parallel generation (10 agents × 10 levels each).
- **To add a new world**: Create `generate_{world}_levels.py` with a `WorldConfig` defining item groups and `complexity_offset`, then run it.

#### Cumulative Mechanic System
Mechanics unlock at fixed levels and persist via probability (30-70%). A complexity cap prevents overwhelming early levels (max 1 at L11-15, max 2 at L16-25, max 3 at L26-40, no cap at L41+). Introduction range (5 levels after unlock) guarantees the mechanic appears. Carousel and despawn are mutually exclusive (spatial conflict).

#### Container Dimension Constants
- `CONTAINER_WIDTH_3SLOT`: ~341px, `CONTAINER_WIDTH_1SLOT`: ~114px
- `SLOT_HEIGHT`: ~227px (includes border_scale 1.2), `ROW_DEPTH_OFFSET`: ~4.56px per extra row
- `CAROUSEL_H_SPACING`: ~351px (10px gap), `CAROUSEL_V_SPACING`: ~227px (touching, no gap)
- `MIN_CONTAINER_GAP`: 30px between container edges
- `HUD_BAR_BOTTOM_Y`: -230 (Godot coords, above visible screen top)
- `SCREEN_BOTTOM_Y`: 1560px (Godot, bottom of visible area)
- Screen safe bounds: X: 200-880, Y: 100-1430 (container centers)

#### Spatial Layout Design
- **Standard layout**: 3-column grid (X=200, 540, 880) with dynamic vertical spacing via `get_y_gap()`
- **Despawn layout**: Full-screen stacked columns, 1-3 columns based on level. Bottom container at SCREEN_BOTTOM_Y, stacks upward to HUD bar, then extends off-screen above. Per-column count: `n_visible_per_col + 3`. Bottom 3 have full depth; upper ones single-row. Only `min(4, n_columns * 2)` from static budget; rest additional. L36-50: 1 column (X=540). L51-70: 1-2 columns (67%/33%). L71+: 1-3 columns (25%/50%/25%). Column positions: `{1: [540], 2: [200, 880], 3: [200, 540, 880]}`. Each column cascades independently (ContainerMovement uses X-tolerance matching).
- **Off-screen scatter exclusion**: Containers whose top edge is above HUD_BAR_BOTTOM_Y (-230) are excluded from scatter destinations in reverse-play. Triples CAN be placed in off-screen containers, but scattered items cannot go TO them.
- **Vertical carousel layout**: 10 containers at X=540 scrolling up/down. Edge-to-edge positioning (first container at visible screen edge + half-height). Statics use 2-column layout. Only subtracts budgeted carousel_count from static pool. X=540 tracked in `occupied_col_xs`.
- **Horizontal carousel layout**: 5+ containers scrolling left/right at Y=200. First container starts 100px off-screen. Static y_offset computed dynamically via `get_y_gap()`.
- **B&F layout**: Back-and-forth movers (L26-50 only) get dedicated wide-spacing rows at screen edges (X=200, 880). If center column is occupied, odd B&F containers use left column instead. Disabled after L50 to free screen space for more containers at higher levels.
- **2D AABB collision**: `_get_bounding_box()`, `_boxes_overlap()`, `_get_travel_box()` for overlap prevention between statics, B&F sweep paths, AND carousel swept bounding boxes. Carousel containers now included in `placed_ranges` (horizontal: full-width sweep at carousel Y; vertical: full-height sweep at carousel X).
- **Screen-fit cap**: Static container count is capped by `max_rows_fit * n_cols` to prevent overflow when container count ramp produces more containers than screen can fit.

#### Lock System
- `WorldConfig` supports both `lock_overlay_image` and `single_slot_lock_overlay_image` (defaults to `{world_id}_single_slot_lockoverlay`)
- Lock match range 1-9: `lo = max(1, 1 + (level-11)//20)`, `hi = min(9, lo + 2 + (level-11)//12)`
- L11: 1-3 matches, L26: 2-5, L36: 2-6, L51: 3-8, L75: 4-9. Per-container randomization via `rng.randint(lo, hi)` ensures different values on the same level.

### Level Structure (All Worlds)
- **Note**: Level numbers below refer to *effective* level (actual + complexity_offset). Island uses offset=0 so they match. Non-default worlds (offset=10) start L1 at effective L11.
- level_001: 3 containers, 2 item types, intro (hardcoded 2-move tutorial, only for offset=0 worlds)
- Levels 2-7: 4-9 containers, increasing item types, multi-row
- Levels 8-15: 8-12 containers
- Levels 11+: locked containers introduced (1-9 matches to unlock, widening with level)
- Levels 16+: single-slot containers (can also be locked, uses own lock overlay)
- Levels 26-50: back-and-forth movement (symmetric pairs, 2D collision-checked; disabled after L50 to make room for more containers)
- Levels 31+: carousel movement (horizontal 5+ containers, 100px off-screen spawn; mutually exclusive with despawn)
- Levels 36+: despawn-on-match (full-screen stacked columns, 1-3 columns, cascade falling; mutually exclusive with carousel)
- Levels 41+: all mechanics combined (probability-based, no complexity cap)
- Levels 50+: vertical carousel possible (10 containers, 40% chance). Container count 17-26
- Levels 61+: container count 17-26 (screen-fit capped)
- Fill ratios: 50-83% across levels, minimum 50 item types per world (some worlds may have more)

### Level Solver
- Editor: Tools > Sort Resort > Solver > Solve Level...
- In-game: "Solve" button in HUD
- **Ensemble solver**: `SolveLevelBest()` runs 5 strategies × 4 runs (1 clean + 3 noise) = 20 attempts per level, takes best result
  - Strategies: Balanced (default), PairFocused (pair=1.4, reveal=0.85), RevealFocused (pair=0.85, reveal=1.4), Cautious (caution=1.6), Aggressive (pair=1.1, reveal=1.3, caution=0.5)
  - Category tracking: `pairContrib`, `revealContrib`, `penaltyContrib` scaled by strategy weights
  - Noise restarts: ±8 random score perturbation per move for diversity (seeded for reproducibility)
  - Early termination: `construction_moves` from level JSON as initial upper bound, `move_limit` tightens as better results found
  - C#: `LevelSolver.SolveLevelBest(levelData)`, Python: `solve_level_best(level_dict)`
- Greedy algorithm with heuristics for pairing, row advancement, deadlock prevention
- Alerts saved when player beats solver score (`{persistentDataPath}/SolverAlerts/`)
- Move sequence output includes scores, reasons, and top 3 runner-ups for debugging
- Key heuristics:
  - **Self-blocking penalty**: -200 when pair's 3rd item is hidden behind the pair at same container
  - **Room-will-open**: +30 for pairs at full containers when non-matching items are fully accessible
  - **Source pair bonus**: +40 for revealing pairs at source on row advance, +25 for exposing source pairs
  - **Deadlock prevention**: -500/-150/-100/-30 graduated penalties for reducing empty front slots to 0/1/2/few
  - **Follow-up quality**: +20+count*10 for reveals from follow-up matches, +15 for chain matches
- Total: 5,969 moves across 100 Island levels (ensemble best)

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

### WebGL Build & Deploy

**Live URL**: https://warmaxegames.github.io/Sort-Resort/

#### "Commit and push and deploy to GitHub Pages" Procedure
**When the user says to commit/push/deploy, follow ALL steps in order. Do NOT skip any step.**

1. **Commit source changes** to `master` (Unity Redo/SortResort repo):
   - `git add` the changed files
   - `git commit` with descriptive message
   - `git push origin master`

2. **Run the framework JS patch** (CRITICAL — build WILL fail without this):
   ```bash
   cd "Unity Redo/SortResort"
   python patch_webgl_build.py
   ```
   - Verify output says "Patched:" — if it says "already patched" or "no match", investigate

3. **Deploy WebGL build** to GitHub Pages (WebGL_Build is a separate repo on `gh-pages`):
   ```bash
   cd "Unity Redo/SortResort/WebGL_Build"
   git add -A
   git commit -m "Deploy WebGL build - <brief description>"
   git push origin gh-pages
   ```
   - The push triggers a **GitHub Actions workflow** that checks out with LFS resolution and deploys

4. **Verify deployment**: Check https://github.com/warmaxegames/Sort-Resort/actions for the workflow run
   - Wait for the workflow to complete (green checkmark), then hard refresh (Ctrl+Shift+R)

#### Build Steps (user does manually in Unity before asking to deploy)
1. Unity: **File → Build Settings → WebGL → Build** (output: `WebGL_Build/`)
2. Steps 2-4 above are what Claude does when asked to "commit and push and deploy"

#### Notes
- `WebGL_Build/` is a separate git repo on the `gh-pages` branch
- **Git LFS is required** for `.data` and `.wasm` files (>100MB GitHub limit). LFS is configured in `WebGL_Build/.gitattributes`
- **GitHub Pages Source MUST be set to "GitHub Actions"** (not "Deploy from a branch") in repo settings. Without this, GitHub Pages serves raw LFS pointer files instead of actual binaries, causing `CompileError: wasm validation error: failed to match magic number`
  - Settings URL: https://github.com/warmaxegames/Sort-Resort/settings/pages → Source → GitHub Actions
- **NEVER remove Git LFS tracking** from the WebGL_Build repo — the .data file exceeds GitHub's 100MB limit and cannot be stored as a regular git object
- Build compression is disabled (`webGLCompressionFormat: 2`) to avoid hosting issues
- `wasm-opt.exe` is replaced with a shim on this machine (crashes otherwise); teammate builds are fully optimized
- `patch_webgl_build.py` fixes WASM import module mismatch — the shim skips `--minify-imports-and-exports`, so WASM uses `"env"` but framework JS expects `"a"`. Patch maps both.
- Do NOT use PowerShell `Compress-Archive` for zips — backslash paths cause 403 errors. Use `python make_zip.py` instead
