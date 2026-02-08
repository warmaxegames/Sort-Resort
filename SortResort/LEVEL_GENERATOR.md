# Sort Resort - Level Generator Reference

## Overview

The level generator produces 100 JSON level files for each world. It is split into two parts:

- **`level_generator.py`** - Core engine (world-agnostic). All progression curves, mechanics scheduling, placement logic, and validation.
- **`generate_{world}_levels.py`** - Per-world config file. Just a `WorldConfig` with item definitions and an entry point.

### Usage

```bash
# Generate 100 Island levels
python generate_island_levels.py

# Output: Assets/_Project/Resources/Data/Levels/Island/level_001.json through level_100.json
# Also: Assets/_Project/Resources/Data/Levels/Island_levels_summary.txt
```

After generating, **run the Unity solver** to finalize star thresholds:
`Tools > Sort Resort > Solver > Update All Level Thresholds`

---

## WorldConfig

```python
WorldConfig(
    world_id="island",           # Used for asset path derivation
    item_groups=[                # (unlock_level, [item_ids]) tuples
        (1, ["item_a", "item_b", "item_c"]),
        (5, ["item_d", "item_e"]),
        ...
    ],
    # Optional overrides (auto-derived from world_id if omitted):
    container_image="island_container",
    single_slot_image="island_single_slot_container",
    lock_overlay_image="island_lockoverlay",
)
```

| Field | Default | Description |
|-------|---------|-------------|
| `world_id` | required | World identifier. Used in JSON output and asset name derivation. |
| `item_groups` | required | List of `(unlock_level, [item_ids])`. Items become available when level >= unlock_level. |
| `container_image` | `{world_id}_container` | Sprite name for standard 3-slot containers. |
| `single_slot_image` | `{world_id}_single_slot_container` | Sprite name for single-slot containers. |
| `lock_overlay_image` | `{world_id}_lockoverlay` | Sprite name for locked container overlays. |

### Item Design Guidelines

- Target **50 items per world**, grouped by visual family (3-4 items per group)
- Groups unlock progressively from L1 through ~L36 so all items are available for the final 64 levels
- Each item always appears in **triples** (3 copies per level) for the match-3 mechanic
- Item IDs must match sprite filenames in `Resources/Sprites/Items/{World}/`

---

## Progression Curves

### Fill Ratio (`get_target_fill_ratio`)

Controls how full containers are. This is the **primary driver** of difficulty.

| Level Range | Fill Ratio | Description |
|-------------|-----------|-------------|
| L1 | 55% | Tutorial - easy intro |
| L2-5 | 80% | Immediately challenging after tutorial |
| L6-15 | 80% → 90% | Gradual ramp (+1% per level) |
| L16-25 | 90% → 95% | Near-full (+0.5% per level) |
| L25-100 | 95% | Maximum density, minimal maneuvering room |

### Row Depth (`get_max_rows`)

Controls how many items are stacked behind each other in a slot. Deeper rows = items hidden behind others.

| Level Range | Max Rows | Description |
|-------------|----------|-------------|
| L1 | 1 | Tutorial - single row, all items visible |
| L2-9 | 2 | Two rows - items hidden behind front row |
| L10-14 | 2 or 3 | Alternating (even levels = 3, odd = 2) |
| L15-100 | 3 | Three rows - maximum depth |

### Item Type Count (`get_target_types`)

Minimum item variety. Secondary to fill ratio (fill ratio determines total items, this ensures variety).

Formula: `max(2, round(2 + 23 * ((level-1)/99)^0.7))`

| Level | Types | Level | Types |
|-------|-------|-------|-------|
| L1 | 2 | L25 | 12 |
| L5 | 5 | L50 | 18 |
| L10 | 7 | L75 | 22 |
| L15 | 9 | L100 | 25 |

### Container Count (`get_level_spec`)

| Level Range | Containers | Notes |
|-------------|-----------|-------|
| L1-3 | 3, 4, 5 | Tutorial ramp |
| L4-10 | 4-7 | Gradual increase |
| L11-20 | 5-8 | Adding mechanics |
| L21-40 | 6-10 | Mid-game |
| L41-70 | 8-11 | Late-game |
| L71-100 | 9-12 | Endgame |

---

## Mechanics Schedule

New mechanics are introduced every ~5 levels, then combined from L41 onward.

| Level Range | Mechanic | Description |
|-------------|----------|-------------|
| **L1-5** | Static only | 3-slot containers, 1 row, no special mechanics |
| **L6-10** | Multi-row | 2 rows per slot (items hidden behind front row) |
| **L11-15** | Locked | Containers start locked, unlock after N matches nearby |
| **L16-20** | Single-slot | 1-slot containers (tight space). Locked also appears L18+ |
| **L21-25** | Deep rows | Mix of 2/3 rows. Locked reappears L23+ |
| **L26-30** | Back-and-forth | Containers move left/right. Locked L28+ |
| **L31-35** | Carousel | Horizontal train of containers. Locked L33+ |
| **L36-40** | Despawn | Containers disappear when a match is made on them. Locked L38+ |
| **L41-100** | All combined | Rotating combo of all mechanics (see below) |

### L41+ Mechanic Rotation

A 7-cycle combo system `(level - 41) % 7`:
- **0, 6**: Locked
- **1, 5**: Carousel (+ single-slot on 5)
- **2, 4**: Back-and-forth
- **3, 6**: Despawn

Additional escalation:
- **L60+**: Locked always present (count scales with level)
- **L70+**: Single-slot always present
- **L80+**: Random extra mechanic (carousel, back-and-forth, or despawn)

### Mechanic Parameters

| Mechanic | Parameters |
|----------|-----------|
| **Locked** | `locked_count`: 1-3 containers. `locked_matches`: 1-3 matches to unlock. |
| **Single-slot** | `singleslot_count`: 1-3 containers. Never overlaps with locked. |
| **Carousel** | `carousel_count`: 3-5 containers. Horizontal train, spacing=297px. Speed: 60+level*0.3. |
| **Back-and-forth** | `backforth_count`: 1-3 containers. Speed: 40+level*0.4. Random left/right direction. |
| **Despawn** | `despawn_count`: 2-4 containers. Stacked vertically at center (x=540). |

---

## Container Layout

### Screen Coordinates

- **Screen**: 1080x1920 pixels (portrait)
- **Safe X bounds**: 200-880 (3-slot), 72-1008 (1-slot)
- **Safe Y bounds**: 250-1600
- **Standard 3-column X**: 200 (left), 540 (center), 880 (right)
- **2-column X**: 370, 710

### Y Spacing by Row Depth

| Max Rows | Y Gap |
|----------|-------|
| 1 | 250px |
| 2 | 300px |
| 3 | 350px |

Y gap auto-compresses if containers would overflow the safe area.

### Layout Logic

1. **Carousel** containers are placed first (off-screen, they scroll in). Y offset added for remaining containers.
2. **Despawn** containers use center column (x=540), stacked vertically. Static containers shift to 2-column layout.
3. **Static** containers fill a 3-column grid (or 2-column when despawn is active).
4. **Locked** containers are always the last static containers (bottom of grid).
5. **Single-slot** containers are the last non-locked static containers.
6. **Back-and-forth** containers are the first non-locked static containers.

---

## Item Placement Algorithm

### Overview

Each item type appears exactly 3 times. Items are distributed across containers using a **randomized least-filled** algorithm.

### Steps

1. **Calculate total items** from fill ratio: `ceil(total_capacity * target_fill)`, rounded up to nearest multiple of 3.
2. **Select item types**: `max(fill_types, variety_types)`, capped by available items and container capacity.
3. **Create 3 copies** of each selected item type, shuffle the list.
4. **Split front/back**: ~55% front-row (1 row) or ~40% front-row (2-3 rows). A 10% buffer of front-row slots is kept empty for player maneuvering.
5. **Ensure visibility**: Every item type has at least 1 copy in the front row (so the player can see and plan).
6. **Place front items** using randomized least-filled selection.
7. **Place back items** into any available back row (1, 2), including buffer slots where row 0 is empty.
8. **Fix starting triples**: Swap front-row items between containers to prevent any container from starting with 3+ identical items.

### Randomized Least-Filled (`pick_random_top`)

Instead of always placing in the absolute least-filled container (which creates systematic patterns), the algorithm:
1. Sorts candidates by fill level
2. Takes the top 3 least-filled
3. Picks one randomly from those 3

This provides **even distribution with natural variance**, avoiding systematic pair creation.

### Buffer Slots

Buffer slots have row 0 empty (for player to move items into) but can have items in back rows (1, 2). This means:
- Container still has items (not visually empty)
- Player has an open front-row position to work with
- Items in back rows are revealed as the player clears front-row items

### Triple Prevention (`fix_starting_triples`)

After placement, a cleanup pass ensures no container has 3+ identical items in its front row (which would instantly match on level start). The algorithm:
1. Find any container with 3+ identical front-row items
2. Swap one of those items with a different-type front-row item from another container
3. Repeat up to 200 iterations
4. If targeted swaps fail, shuffle all front-row item IDs randomly and retry

---

## Level 1 (Hardcoded Tutorial)

Level 1 is always a 2-move tutorial regardless of world:
- 3 containers, 2 item types (A, B)
- Container 1: [A, A, _]
- Container 2: [B, B, _]
- Container 3: [A, B, _]
- Solution: Move A from C3→C1 (match), Move B from C3→C2 (match)
- Thresholds: [2, 3, 4, 5]

---

## Timer System

Timer value scales from generous to tight:

```
seconds_per_item = 2.0 - (level - 1) * (1.5 / 99)
timer = max(10, round(total_items * seconds_per_item))
```

| Level | Seconds/Item | Example (30 items) |
|-------|-------------|-------------------|
| L1 | 2.0s | 60s |
| L25 | 1.64s | 49s |
| L50 | 1.26s | 38s |
| L75 | 0.88s | 26s |
| L100 | 0.5s | 15s |

Timer is only active in TimerMode and HardMode (mode-driven in Unity).

---

## Star Thresholds (Estimated)

The generator produces **estimated** thresholds. The Unity solver must finalize them.

### Estimation Formula

```
base_factor = 0.7
+0.1 if max_rows >= 2
+0.1 if max_rows >= 3
+0.05 if locked
+0.05 if carousel or back-and-forth
+0.05 if despawn
+0.02 if single-slot

estimated_optimal = max(3, round(total_items * factor))

3-star: estimated_optimal
2-star: max(3star+1, ceil(estimated * 1.15))
1-star: max(2star+1, ceil(estimated * 1.30))
fail:   max(1star+1, ceil(estimated * 1.40))
```

All thresholds are guaranteed at least 1 move apart.

### Finalization

After generating levels, run in Unity:
`Tools > Sort Resort > Solver > Update All Level Thresholds`

This runs the actual solver on each level to find the true optimal move count, then applies the multipliers (1.0x, 1.15x, 1.30x, 1.40x).

---

## Output Format

### Level JSON Structure

```json
{
    "id": 25,
    "world_id": "island",
    "name": "level_025",
    "star_move_thresholds": [15, 18, 20, 22],
    "time_limit_seconds": 45,
    "containers": [
        {
            "id": "container_1",
            "position": {"x": 200.0, "y": 300.0},
            "container_type": "standard",
            "container_image": "island_container",
            "slot_count": 3,
            "max_rows_per_slot": 3,
            "is_locked": false,
            "unlock_matches_required": 0,
            "lock_overlay_image": "",
            "unlock_animation": "",
            "is_moving": false,
            "move_type": "",
            "move_direction": "",
            "move_speed": 50.0,
            "move_distance": 200.0,
            "track_id": "",
            "is_falling": false,
            "fall_speed": 100.0,
            "fall_target_y": 0.0,
            "despawn_on_match": false,
            "initial_items": [
                {"id": "sandpale_red", "row": 0, "slot": 0},
                {"id": "coconut", "row": 0, "slot": 1},
                {"id": "beachball_mixed", "row": 1, "slot": 0},
                {"id": "margarita", "row": 2, "slot": 1}
            ]
        }
    ],
    "moving_tracks": []
}
```

### Summary File

Generated alongside levels at `Assets/_Project/Resources/Data/Levels/{world}_levels_summary.txt`. Contains per-level stats:
```
L 25:  8c, 15t, 45i, 3r,  92% fill, 0e/3f, thresh=[15, 18, 20, 22], timer=45s, [locked]
```
Fields: containers, types, items, max rows, fill %, empty/full containers, thresholds, timer, mechanics.

---

## Validation

The generator checks for and reports:

| Check | Error if... |
|-------|------------|
| Item count | Not a multiple of 3 |
| Starting triples | Any container has 3+ identical front-row items |
| Container positions | Static container outside safe bounds (150-930 x, 150-1700 y) |
| Empty containers | Any container has 0 items |
| Item usage | Any item type is never used across all 100 levels |
| Placement failure | Not all items could be placed |

Errors are printed to console and included in the summary file.

---

## Deterministic Seeds

Each level uses seed `level * 42 + 7` for its RNG. This means:
- Same level number always generates the same level (for reproducibility)
- Regenerating doesn't change existing levels unless the algorithm changes
- Different worlds with the same level number get the same container layout but different items

---

## Creating a New World

1. Create `generate_{world}_levels.py`:

```python
from level_generator import WorldConfig, generate_levels
import os

CONFIG = WorldConfig(
    world_id="farmland",
    item_groups=[
        (1,  ["egg_white", "egg_brown", "egg_blue"]),
        (1,  ["apple_red", "apple_green", "apple_gold"]),
        (3,  ["carrot", "corn", "potato"]),
        # ... ~17 groups totaling 50 items, unlocking L1-L36
    ],
)

assert len(CONFIG.all_items) == 50

if __name__ == "__main__":
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "Assets", "_Project", "Resources", "Data", "Levels", "Farmland")
    generate_levels(CONFIG, output_dir)
```

2. Ensure sprite assets exist at `Resources/Sprites/Items/{World}/` matching each item ID
3. Ensure container sprites exist: `{world_id}_container`, `{world_id}_single_slot_container`, `{world_id}_lockoverlay`
4. Run: `python generate_farmland_levels.py`
5. In Unity: `Tools > Sort Resort > Solver > Update All Level Thresholds`

---

## Design Decisions & Rationale

| Decision | Rationale |
|----------|-----------|
| Fill ratio as primary driver | More impactful than item variety for difficulty. A 92%-full board with 10 types is harder than 60%-full with 20 types. |
| 10% front-row buffer | Players need empty front-row slots to maneuver items. Too few = unsolvable. Too many = trivial. |
| Randomized top-3 selection | Pure least-filled creates detectable patterns (systematic pairs). Top-3 random adds variance while keeping distribution even. |
| Buffer slots with back-row items | Leaving entire slots empty wastes capacity. Back-row items behind empty front slots use space efficiently while maintaining maneuvering room. |
| Mechanics introduced individually | Players learn one mechanic at a time (5-level windows) before combinations start at L41. |
| L41+ combo rotation | 7-cycle ensures all mechanics appear regularly. Higher levels layer more mechanics simultaneously. |
| Locked containers always last in grid | Being at the bottom means they unlock as the player clears items from containers above - natural top-to-bottom flow. |
| Deterministic seeds | Reproducible levels for debugging and testing. Same seed = same level even after code restarts. |
| Triple prevention via swaps | Swapping between containers preserves the overall item distribution. Random shuffling is only a fallback. |
| All 50 items used | Usage tracking with least-used selection ensures every item appears across the 100 levels. No wasted art assets. |
