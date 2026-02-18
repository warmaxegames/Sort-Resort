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

**Last Updated:** 2026-02-17

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
- **Achievement system** (COMPLETE) - 45 achievements across 7 categories with card-based UI, tier art rectangles, notifications with achievement sound, rewards. Sprite-based progress bar (`progress_bar.png` frame with green fill, aspect-matched to achievement rectangles). Text wrapping for long titles/descriptions (`FormatTextForWrapping` helper). Close button with pressed state (`closebutton_2`/`closebutton_pressed`, 118x118, positioned at exact pixel coords). Cards and bars centered with 30px left offset. "Achievement Points" title bar text (48pt Benzin ExtraBold). Per-world descriptions use "in this World" instead of specific world names.
- Level solver tool (Editor window + in-game auto-solve button)
- **Dialogue system** - Typewriter text with Animal Crossing-style voices, mascot portraits, voice toggle in settings, timer pauses during dialogue. Full story content: 5 worlds × 11 checkpoints (welcome + every 10 levels), 4 mode tutorial dialogues, 5 hard mode unlock dialogues. Story is mode-agnostic (fires in any mode, plays once). Dr. Miller overarching mystery across all worlds.
- **Level complete screen** - Mode-specific animations: Star Mode has rays, curtains, star ribbon, grey/gold star animations, dancing stars (3-star); Timer Mode has stopwatch count-up animation with "New Record!" bounce; Free Play skips stars; Hard Mode combines stars + timer. All modes: mascot thumbsup (Island), bottom board animation, sprite-based buttons
- **Level failed screen** - Fullscreen fail_screen.png background, reason text ("Out of Moves"/"Out of Time"), animated bottom board, sprite-based buttons (retry, level select)
- **Fail-before-last-move** - Level fails after second-to-last move if not complete (prevents awkward final-move-still-fails scenario)
- **Star threshold separation** - All thresholds guaranteed at least 1 move apart
- **Mode-specific HUD overlay** - All 4 modes have custom ui_top bar overlays (free_ui_top, stars_ui_top, timer_ui_top, hard_mode_UI_top) with mode-specific counter text positions. World icon overlay (island only). Sprite-based settings gear button (116x116, with pressed state) and undo button (142x62, with pressed state) positioned on the overlay. Settings gear opens sprite-based pause menu
- **Portal completion overlays** - Mode-specific portal overlays on level select: free_portal (checkmark) for FreePlay, 1/2/3star_portal for StarMode, timer_portal (with best time text) for TimerMode, both star+timer overlays layered for HardMode
- **Level generator** - Python reverse-play generator (`reverse_generator.py`) builds levels backwards, guaranteeing solvability. Solver-verified with retry mechanism (up to 20 seeds). Star thresholds derived from solver's actual move count. 100 Island levels generated and verified. Locked containers participate in reverse-play with unlock-timing cutoffs.
- **Benzin font system** - Custom Benzin-Bold font across all UI text. FontManager static utility with lazy-loaded font properties (Bold, SemiBold, Medium, ExtraBold). Editor script generates TMP SDF font assets. TMP default font set to Benzin-Bold so dynamic text auto-inherits. ApplyBold() calls on 8 scripts with serialized TMP fields.
- **Sprite-based mode tabs** - Level select mode tabs use custom sprite images (`free_tab`, `stars_tab`, `timer_tab`, `hard_tab`) with baked-in text. Selected=white tint, unselected=60% grey, locked=30% grey.
- **Sprite-based pause menu** - Fullscreen 1080x1920 overlay sprites for board background and 4 buttons (Resume, Restart, Settings, Quit) with pressed states. `LoadFullRectSprite()` bypasses Unity's alpha-trimming import. EventTrigger-based press feedback swaps normal/pressed sprites. Invisible hit areas positioned at exact pixel coordinates via anchor-based sizing.
- **Achievement sound** - Dedicated `achievement_sound.mp3` plays at 1.3x volume when achievement notification slides in. Plays per-notification so multiple queued achievements each get the sound.
- **World-specific dialogue boxes** - Island, Tavern, Space, Farm worlds have custom dialogue box sprites (`dialoguebox_{worldId}`). Auto-loaded by `DialogueUI.LoadDialogueBoxForWorld()`.

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
   - Supermarket
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
| Layout | Spacing | Containers | move_distance | Notes |
|--------|---------|------------|---------------|-------|
| Horizontal | ~356px (X) | 5 min | 5 × spacing | First container 100px off-screen at spawn |
| Vertical | ~257px (Y) | 10 fixed | 10 × spacing | L50+, 40% chance. Statics use 2-col |
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
- **HUD Overlay**: All 4 modes have per-mode fullscreen bar overlay (`free_ui_top`, `stars_ui_top`, `timer_ui_top`, `hard_mode_UI_top`). Counter texts positioned per mode (FreePlay: level only; StarMode: level+moves; TimerMode: level+timer; HardMode: level+moves+timer). World icon overlay (`{worldId}_icon_UI_top`, currently island only). White counter text color.
- **HUD Buttons**: Record (debug), Solve (debug) in button row. Settings gear (116x116 sprite with pressed state) and Undo (142x62 sprite with pressed state) on settings overlay. Settings gear opens pause menu.
- **Pause Menu**: Sprite-based fullscreen overlays (1080x1920). Board background + 4 buttons (Resume, Restart, Settings, Quit) each with normal/pressed sprites. Loaded via `LoadFullRectSprite()` (Texture2D → full-rect Sprite.Create) to avoid Unity alpha-trimming. Button hit areas use anchor-based positioning at exact pixel coords: Resume Y=877, Restart Y=1055, Settings Y=1229, Quit Y=1404 (all centered at X=534). EventTrigger swaps sprites on PointerDown/PointerUp. Canvas sortingOrder=5200.
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

#### Card-Based UI (COMPLETE - 2026-02-17)
- Fullscreen overlay (Canvas sortingOrder=5200, dark background 0.7 alpha)
- Centered 1000×1700 content panel with header, horizontal tab bar, scrollable card list
- Each card: tier-appropriate rectangle art (687×301, preserveAspect), title (Benzin Bold 24), description, sprite-based progress bar
- Rectangle upgrades: grey → bronze → silver → gold as milestones are unlocked
- **Progress bar**: `progress_bar.png` (457×98) metallic frame with green fill behind it. Bar width matches rendered rect width exactly (~494px). Green fill inset by corner radius (~14px) to stay inside rounded frame edges. Progress text "X/Y" centered on bar.
- **Text wrapping**: `FormatTextForWrapping(text, maxChars)` inserts line breaks at best word boundary. Title threshold: 13 chars ("Globe Trotter"). Description threshold: 18 chars ("Make 1000 Matches"). Title/description shifted 10px left from original anchors.
- **Close button**: 118×118 `closebutton_2` sprite at anchor (0.9019, 0.8880) with `closebutton_pressed` swap on PointerDown/PointerUp via EventTrigger.
- **Card centering**: Rect image and progress bar both offset 30px left to center on board.
- **Title bar**: "Achievement Points" at 48pt Benzin ExtraBold (with shadow). Points text shifted 5px higher.
- **Per-world descriptions**: Use "in this World" instead of specific world names (context from tab).
- `AchievementManager` helper methods: `GetGroupArtKey()`, `GetGroupCurrentTier()`, `GetNextMilestone()`, `GetGroupProgress()`, `GetGroupLastUnlockDate()`, `GetRecentlyUnlocked()`, `GetGroupIdsForTab()`

#### Notification System (unchanged)
- `CreateAchievementNotificationPanel` slides in from top on unlock
- Achievement detail panel on tap
- `achievement_sound.mp3` at 1.3× volume per notification
- Queue system for multiple simultaneous unlocks

### Font System
- **Font family**: Benzin (Bold, SemiBold, Medium, ExtraBold) - TTFs in `Assets/_Project/Fonts/`
- **SDF assets**: Generated via `Tools > Sort Resort > Fonts > Generate Benzin SDF Fonts` into `Resources/Fonts/`
- **TMP default**: Set via `Tools > Sort Resort > Fonts > Set Benzin-Bold as TMP Default` - all dynamic TMP text auto-inherits
- **FontManager.cs**: Static utility in `Assets/_Project/Scripts/Managers/` - lazy-loads from `Resources/Fonts/BENZIN-{WEIGHT} SDF`
- **Properties**: `FontManager.Bold`, `.SemiBold`, `.Medium`, `.ExtraBold`
- **Helper**: `FontManager.ApplyBold(TMP_Text text)` - overrides serialized Inspector fields with Benzin-Bold at runtime
- **Applied in**: GameHUDScreen, LevelCompleteScreen, LevelFailedScreen, SettingsScreen, DialogueUI, WorldSelectionScreen, LevelSelectionScreen, LevelNode (8 scripts)
- **Not needed in**: UIManager, LevelSelectScreen, ItemContainer (dynamic TMP creation inherits TMP default)
- **Editor script**: `Assets/_Project/Scripts/Editor/FontAssetGenerator.cs` - sampling size 90, atlas 1024x1024

### Level Generator (Python)
- **Infrastructure**: `level_generator.py` - WorldConfig, progression curves, container builder, specs
- **Reverse generator**: `reverse_generator.py` - reverse-play item placement + solver-verified generation
- **Island config**: `generate_island_levels.py` - imports from reverse_generator, defines 50 Island items
- **Python solver**: `level_solver.py` - greedy solver port for verification during generation
- **Usage**: `python generate_island_levels.py` (creates 100 levels in `Resources/Data/Levels/Island/`)
- **Algorithm**: Reverse-play construction V2 (no work container). For each triple: pick random container, push existing items deeper, place triple at front, scatter 1-3 items to other containers. Each scatter = 1 forward move.
- **Locked containers**: Participate in reverse-play loop with cutoff timing. Last `unlock_matches_required` triples exclude locked container (those forward triples unlock it).
- **Validation**: No starting triples at ANY row depth. Solver verifies each level; retries with different seed on failure (up to 20 attempts). Star thresholds use solver's actual move count. Full AABB bounding box checks for overlap and off-screen detection.
- **To add a new world**: Create `generate_{world}_levels.py` with a `WorldConfig` defining item groups, then run it.

#### Cumulative Mechanic System
Mechanics unlock at fixed levels and persist via probability (30-70%). A complexity cap prevents overwhelming early levels (max 1 at L11-15, max 2 at L16-25, max 3 at L26-40, no cap at L41+). Introduction range (5 levels after unlock) guarantees the mechanic appears. Carousel and despawn are mutually exclusive (spatial conflict).

#### Container Dimension Constants
- `CONTAINER_WIDTH_3SLOT`: ~341px, `CONTAINER_WIDTH_1SLOT`: ~114px
- `SLOT_HEIGHT`: ~227px (includes border_scale 1.2), `ROW_DEPTH_OFFSET`: ~4.56px per extra row
- `CAROUSEL_H_SPACING`: ~356px (15px gap), `CAROUSEL_V_SPACING`: ~257px
- `MIN_CONTAINER_GAP`: 30px between container edges
- Screen safe bounds: X: 200-880, Y: 250-1600 (container centers)

#### Spatial Layout Design
- **Standard layout**: 3-column grid (X=200, 540, 880) with dynamic vertical spacing via `get_y_gap()`
- **Despawn layout**: 5-12 containers stacked vertically at X=540 (single-column). Bottom container visible at Y=250, upper containers extend off-screen above (~236px spacing). When bottom is cleared, containers above fall down (cascade mechanic). Bottom 3 have full depth; upper ones are single-row to limit item count. Statics use 2-column layout (X=200, 880) to avoid center. Only 4 despawn containers come from static budget; rest are additional.
- **Vertical carousel layout**: 10 containers at X=540 scrolling up/down. Statics use 2-column layout. Only subtracts budgeted carousel_count from static pool.
- **Horizontal carousel layout**: 5+ containers scrolling left/right at Y=200. First container starts 100px off-screen. Static y_offset computed dynamically via `get_y_gap()`.
- **B&F layout**: Back-and-forth movers get dedicated wide-spacing rows at screen edges (X=200, 880). If center column is occupied, odd B&F containers use left column instead.
- **2D AABB collision**: `_get_bounding_box()`, `_boxes_overlap()`, `_get_travel_box()` for overlap prevention between statics and B&F sweep paths.

#### Lock System
- `WorldConfig` supports both `lock_overlay_image` and `single_slot_lock_overlay_image` (defaults to `{world_id}_single_slot_lockoverlay`)
- Lock match range 1-9: `lo = max(1, 1 + (level-11)//20)`, `hi = min(9, lo + 2 + (level-11)//12)`
- L11: 1-3 matches, L26: 2-5, L36: 2-6, L51: 3-8, L75: 4-9. Per-container randomization via `rng.randint(lo, hi)` ensures different values on the same level.

### Level Structure (All Worlds)
- level_001: 3 containers, 2 item types, intro (hardcoded 2-move tutorial)
- Levels 2-10: 4-7 containers, increasing item types, multi-row
- Levels 11+: locked containers introduced (1-9 matches to unlock, widening with level)
- Levels 16+: single-slot containers (can also be locked, uses own lock overlay)
- Levels 26+: back-and-forth movement (symmetric pairs, 2D collision-checked)
- Levels 31+: carousel movement (horizontal 5+ containers, 100px off-screen spawn; mutually exclusive with despawn)
- Levels 36+: despawn-on-match (5-12 containers stacked at X=540, cascade falling; mutually exclusive with carousel)
- Levels 41+: all mechanics combined (probability-based, no complexity cap)
- Levels 50+: vertical carousel possible (10 containers, 40% chance)
- Fill ratios: 50-83% across levels, 50 item types per world

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
