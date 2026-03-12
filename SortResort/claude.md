# Sort Resort - Unity Rebuild Progress Tracker

## Project Overview

**Sort Resort** is a casual puzzle/sorting game for iOS, Android, and Tablets. Players drag and drop items across containers to achieve triple matches. Rebuilt from Godot 4 to Unity.

### Core Mechanics
- Drag-and-drop items between container slots
- Triple match system (3 identical items = cleared)
- Multi-row containers with depth visualization
- Locked containers with countdown unlock
- Moving containers on paths (carousel)
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
- Splash screen, level select, full game flow with fade transitions
- 5 worlds: Island, Supermarket, Farm, Space, Tavern (100 levels each, 500 total)
- Drag-drop with visual feedback, sounds, triple-match detection
- Row advancement, locked containers, carousel movement, despawn-on-match
- World-specific backgrounds, music, and ambient audio
- Save/load progress (PlayerPrefs), undo system, timer (M:SS.CC)
- **4 Game Modes** - FreePlay (green), StarMode (pink), TimerMode (blue), HardMode (gold)
- **Achievement system** - 45 achievements, card-based UI, tier art, notifications
- **Dialogue system** - 65 sequences, typewriter text, Animal Crossing voices
- **Level complete/failed screens** - Mode-specific animations, mascot thumbsup
- **HUD overlay** - Per-mode bars, counter text, pause menu
- **Combo text effects** - "GOOD!"/"AMAZING!"/"PERFECT!" on match streaks
- **Level generator** - Python reverse-play generator, solver-verified, 500 levels
- **Level solver** - Ensemble solver (5 strategies x 4 runs = 20 attempts)
- **Benzin font system** - FontManager utility, TMP default

---

## TODO List

### Priority Items
1. ~~**Improve Solver**~~ - DONE. Ensemble solver, 5,969 moves across 100 levels.
2. **Level Complete Screen** - Animated mascot art for non-Island worlds, audio per star scenario
3. **Level Failed Screen** - Sound effect audio
4. **World-Specific Lock Overlays** - Farm, Supermarket, Space
5. **World-Specific Dialogue Boxes** - Supermarket
6. ~~**Generate Levels**~~ - DONE. All 5 worlds, 500 levels total.
7. **World Icon Assets** - HUD icons for Supermarket, Farm, Tavern, Space
8. **Mobile/Device Testing**

### Future Items
- Trophy Room, Player Profile, Leaderboards, Premium Currency, Shop & Monetization
- Daily Login Rewards, Share Button, Accessibility Options
- Ad Monetization, Lives/Energy, Season Pass, Hints/Boosters, Star Gates
- Daily/Weekly Challenges, Seasonal Events, Cloud Save, Localization, Analytics

---

## Technical Reference

### Key Patterns
- **Object Pooling**: 120-250 item pool for mobile performance
- **Event System**: C# events/Actions for decoupled communication
- **Singleton Managers**: GameManager, AudioManager, SaveManager, UIManager, ScreenManager
- **Data-Driven**: JSON files for levels, items, worlds
- **State Machine**: GameState enum (Playing, LevelSelection, Paused, etc.)

### Coordinate System (Portrait Mode)
Level positions use **screen pixels** (origin top-left, 1080x1920):
```csharp
float unityX = (screenPos.x - 540f) / 100f;   // Screen center X = 540
float unityY = (960f - screenPos.y) / 100f;    // Screen center Y = 960, flip Y
```

### Screen & Camera
- **Resolution**: 1080x1920 (portrait), **Camera orthoSize**: 9.6
- **Visible area**: 10.8 x 19.2 Unity units, **Coordinate divisor**: 100
- **Top HUD bar**: Y=0 to ~230, **Bottom items bar**: Y=1752 to 1920
- **3-column layout**: Left=200/-3.4, Center=540/0, Right=880/+3.4

### Layer Configuration
- Layer 6: "Items" - for item raycasting
- Layer 7: "Slots" - for slot raycasting

### Carousel Configuration
| Layout | Spacing | Containers | Notes |
|--------|---------|------------|-------|
| Horizontal | ~351px | 5 min | First 100px off-screen |
| Vertical | ~227px | 10 fixed | L50+, 40% chance |
Carousel and despawn are mutually exclusive (spatial conflict).

---

## Detailed Reference Docs

For deeper details on specific systems, see:
- **[docs/LEVEL_GENERATOR.md](docs/LEVEL_GENERATOR.md)** - Python generator, spatial layout, mechanics, container dimensions, lock system
- **[docs/SOLVER.md](docs/SOLVER.md)** - Ensemble solver strategies, heuristics, star thresholds
- **[docs/SYSTEMS.md](docs/SYSTEMS.md)** - Game modes, timer, dialogue, achievements, level complete animation, combo text, fonts
- **[docs/ASSETS.md](docs/ASSETS.md)** - Asset locations, crop metadata workflow, adding new images/animations
- **[docs/WEBGL_DEPLOY.md](docs/WEBGL_DEPLOY.md)** - Build & deploy procedure, GitHub Pages, LFS, patching

### WebGL Deploy Quick Reference
See [docs/WEBGL_DEPLOY.md](docs/WEBGL_DEPLOY.md) for full procedure. Summary:
1. Commit source to `master` and push
2. Run `python patch_webgl_build.py` (CRITICAL)
3. In `WebGL_Build/`: `git add -A && git commit && git push origin gh-pages`
4. **NEVER checkout gh-pages in the parent repo** - destroys Unity project files
