#!/usr/bin/env python3
"""
Sort Resort Level Generator - Core Module
Generates 100 levels for any world given a WorldConfig.

Mechanic schedule (new every ~5 levels):
  L1:     Tutorial (3 containers, 1 row, 55% fill)
  L2-5:   Static 3-slot, 2 rows, 80% fill
  L6-10:  Multi-row (mix 2/3 rows), 80-85% fill
  L11-15: Locked containers, 3 rows by L15, 85-90% fill
  L16-20: Single-slot containers, 90-93% fill
  L21-25: Deep rows, 93-95% fill
  L26-30: Back-and-forth movement
  L31-35: Carousel movement
  L36-40: Despawn-on-match
  L41+:   All mechanics, escalating complexity

Fill ratio ramps: 55% (L1) -> 80% (L2-5) -> 90% (L15) -> 95% (L25+)
Timer: 2.0s/item (L1) down to 0.5s/item (L100).
Thresholds: Estimated, run Unity solver to finalize.
"""

from dataclasses import dataclass
import json
import math
import os
import random
from typing import List, Tuple


# ── World Configuration ──────────────────────────────────────────────────────

@dataclass
class WorldConfig:
    """Configuration for a world's level generation.

    Attributes:
        world_id:        World identifier (e.g. "island", "supermarket")
        item_groups:     List of (unlock_level, [item_ids]) tuples.
                         Items become available at the specified level.
        container_image: Sprite name for standard containers.
                         Defaults to "{world_id}_container".
        single_slot_image: Sprite name for single-slot containers.
                         Defaults to "{world_id}_single_slot_container".
        lock_overlay_image: Sprite name for lock overlays.
                         Defaults to "{world_id}_lockoverlay".
    """
    world_id: str
    item_groups: List[Tuple[int, List[str]]]
    container_image: str = ""
    single_slot_image: str = ""
    lock_overlay_image: str = ""

    def __post_init__(self):
        if not self.container_image:
            self.container_image = f"{self.world_id}_container"
        if not self.single_slot_image:
            self.single_slot_image = f"{self.world_id}_single_slot_container"
        if not self.lock_overlay_image:
            self.lock_overlay_image = f"{self.world_id}_lockoverlay"

    @property
    def all_items(self):
        items = []
        for _, group in self.item_groups:
            items.extend(group)
        return items


# ── Progression Curves ───────────────────────────────────────────────────────

def get_target_fill_ratio(level):
    """Target overall fill ratio - 80%+ immediately after tutorial, 95% by level 25.
    L1: 55% (tutorial), L2-5: 80%, L6-15: 80% -> 90%, L16-25: 90% -> 95%, L25+: 95%."""
    if level <= 1:
        return 0.55
    elif level <= 5:
        return 0.80
    elif level <= 15:
        return 0.80 + (level - 5) * 0.01        # 80% -> 90%
    elif level <= 25:
        return 0.90 + (level - 15) * 0.005       # 90% -> 95%
    else:
        return 0.95


def get_target_types(level):
    """Minimum item types for variety (secondary to fill ratio)."""
    t = (level - 1) / 99.0
    return max(2, round(2 + 23 * (t ** 0.7)))


def get_max_rows(level):
    """Row depth per slot - multi-row starts at L2, 3 rows by L15.
    L1: 1 (tutorial), L2-9: 2, L10-14: mix 2/3, L15+: 3."""
    if level <= 1:
        return 1
    elif level < 10:
        return 2
    elif level < 15:
        return 3 if (level % 2 == 0) else 2
    else:
        return 3


# ── Item Management ──────────────────────────────────────────────────────────

def get_available_items(config, level):
    """Return items unlocked at or before this level."""
    available = []
    for unlock_lvl, items in config.item_groups:
        if level >= unlock_lvl:
            available.extend(items)
    return available


def select_items(rng, available, n_types, item_usage):
    """Select n_types items from available pool, preferring least-used."""
    shuffled = available[:]
    rng.shuffle(shuffled)
    sorted_by_usage = sorted(shuffled, key=lambda x: item_usage[x])
    selected = sorted_by_usage[:n_types]
    for item in selected:
        item_usage[item] += 1
    return selected


# ── Container Positions ──────────────────────────────────────────────────────

def get_static_positions(count, max_rows, y_offset=0):
    """Grid positions for static containers. All within safe screen bounds.
    Screen: 1080x1920, safe X: 200-880, safe Y: 250-1600."""
    y_gap = {1: 250, 2: 300, 3: 350}.get(max_rows, 300)
    cols_3 = [200, 540, 880]
    cols_2 = [370, 710]

    n_rows_needed = math.ceil(count / 3)
    total_height = (n_rows_needed - 1) * y_gap
    y_min = 250 + y_offset
    y_max = 1600
    available = y_max - y_min
    if total_height > available and n_rows_needed > 1:
        y_gap = available // (n_rows_needed - 1)

    y0 = y_min
    positions = []
    remaining = count
    row = 0
    while remaining > 0:
        y = y0 + row * y_gap
        if remaining >= 3:
            positions.extend([(x, y) for x in cols_3])
            remaining -= 3
        elif remaining == 2:
            positions.extend([(x, y) for x in cols_2])
            remaining -= 2
        else:
            positions.append((540, y))
            remaining -= 1
        row += 1

    return [(max(200, min(880, x)), max(250, min(1600, y))) for x, y in positions]


# ── Container Builder ────────────────────────────────────────────────────────

def make_container(cid, x, y, config, slot_count=3, max_rows=1, is_locked=False,
                   unlock_matches=0, is_moving=False, move_type="",
                   move_direction="", move_speed=50.0, move_distance=200.0,
                   despawn=False, is_single_slot=False):
    """Create a container dict for level JSON."""
    cont_img = config.single_slot_image if is_single_slot else config.container_image
    lock_img = config.lock_overlay_image if is_locked else ""
    return {
        "id": cid,
        "position": {"x": float(x), "y": float(y)},
        "container_type": "standard",
        "container_image": cont_img,
        "slot_count": slot_count,
        "max_rows_per_slot": max_rows,
        "is_locked": is_locked,
        "unlock_matches_required": unlock_matches,
        "lock_overlay_image": lock_img,
        "unlock_animation": "",
        "is_moving": is_moving,
        "move_type": move_type,
        "move_direction": move_direction,
        "move_speed": move_speed,
        "move_distance": move_distance,
        "track_id": "",
        "is_falling": False,
        "fall_speed": 300.0 if despawn else 100.0,
        "fall_target_y": 0.0,
        "despawn_on_match": despawn,
        "initial_items": []
    }


# ── Level Spec ───────────────────────────────────────────────────────────────

def get_level_spec(level):
    """Determine level parameters based on level number."""
    rng = random.Random(level * 42 + 7)

    # Container count ramp
    if level <= 3:
        n_containers = level + 2                                  # 3, 4, 5
    elif level <= 10:
        n_containers = min(4 + (level - 1) // 2, 7)
    elif level <= 20:
        n_containers = min(5 + (level - 5) // 3, 8)
    elif level <= 40:
        n_containers = min(6 + (level - 10) // 5, 10)
    elif level <= 70:
        n_containers = min(8 + (level - 40) // 10, 11)
    else:
        n_containers = min(9 + (level - 60) // 15, 12)

    max_rows = get_max_rows(level)

    # Mechanics configuration
    use_locked = False; locked_count = 0; locked_matches = 0
    use_singleslot = False; singleslot_count = 0
    use_carousel = False; carousel_count = 0
    use_backforth = False; backforth_count = 0
    use_despawn = False; despawn_count = 0

    if 11 <= level <= 15:
        use_locked = True
        locked_count = 1 + (level - 11) // 2
        locked_matches = 1 + (level - 11) // 3
    elif 16 <= level <= 20:
        use_singleslot = True
        singleslot_count = 1 + (level - 16) // 2
        if level >= 18:
            use_locked = True; locked_count = 1; locked_matches = 1
    elif 21 <= level <= 25:
        if level >= 23:
            use_locked = True
            locked_count = 1 + (level - 23)
            locked_matches = 1
    elif 26 <= level <= 30:
        use_backforth = True
        backforth_count = 1 + (level - 26) // 2
        if level >= 28:
            use_locked = True; locked_count = 1; locked_matches = 1 + (level - 28)
    elif 31 <= level <= 35:
        use_carousel = True
        carousel_count = 3 + (level - 31) // 2
        if level >= 33:
            use_locked = True; locked_count = 1; locked_matches = 1
    elif 36 <= level <= 40:
        use_despawn = True
        despawn_count = 2 + (level - 36) // 2
        if level >= 38:
            use_locked = True; locked_count = 1; locked_matches = 2
    elif level >= 41:
        combo = (level - 41) % 7
        if combo in (0, 6):
            use_locked = True
            locked_count = 1 + level // 30
            locked_matches = 1 + level // 40
        if combo in (1, 5):
            use_carousel = True
            carousel_count = 3 + level // 30
        if combo in (2, 4):
            use_backforth = True
            backforth_count = 1 + level // 30
        if combo in (3, 6):
            use_despawn = True
            despawn_count = 2 + level // 30
        if combo == 5 or level >= 70:
            use_singleslot = True
            singleslot_count = 1 + level // 50
        if level >= 60:
            use_locked = True
            locked_count = max(locked_count, 1 + (level - 60) // 15)
            locked_matches = max(locked_matches, 1 + (level - 60) // 20)
        if level >= 80:
            extra = rng.choice(["carousel", "backforth", "despawn"])
            if extra == "carousel" and not use_carousel:
                use_carousel = True; carousel_count = 3
            elif extra == "backforth" and not use_backforth:
                use_backforth = True; backforth_count = 2
            elif extra == "despawn" and not use_despawn:
                use_despawn = True; despawn_count = 2

    return {
        "level": level, "n_containers": n_containers, "max_rows": max_rows,
        "use_locked": use_locked, "locked_count": locked_count,
        "locked_matches": locked_matches,
        "use_singleslot": use_singleslot, "singleslot_count": singleslot_count,
        "use_carousel": use_carousel, "carousel_count": carousel_count,
        "use_backforth": use_backforth, "backforth_count": backforth_count,
        "use_despawn": use_despawn, "despawn_count": despawn_count,
        "rng": rng,
    }


# ── Build Containers ─────────────────────────────────────────────────────────

def build_containers(spec, config):
    """Create all containers with positions and mechanics."""
    rng = spec["rng"]
    level = spec["level"]
    containers = []
    idx = 1
    static_count = spec["n_containers"]
    y_offset = 0

    # Carousel containers (allowed off-screen, they scroll in)
    if spec["use_carousel"]:
        n_car = min(spec["carousel_count"], 5)
        static_count -= n_car
        spacing = 297
        car_dir = rng.choice(["right", "left"])
        car_speed = 60 + level * 0.3
        car_y = 200
        for i in range(n_car):
            cx = (-150 + i * spacing) if car_dir == "right" else (1230 - i * spacing)
            c = make_container(
                f"carousel_{idx}", cx, car_y, config,
                slot_count=3, max_rows=min(spec["max_rows"], 2),
                is_moving=True, move_type="carousel",
                move_direction=car_dir, move_speed=car_speed,
                move_distance=n_car * spacing
            )
            containers.append(c)
            idx += 1
        y_offset = 250 if spec["max_rows"] <= 2 else 300

    # Despawn containers (stacked vertically, allowed off-screen)
    if spec["use_despawn"]:
        n_desp = min(spec["despawn_count"], 4)
        static_count -= n_desp
        desp_x = 540
        desp_y_base = 500 + y_offset
        for i in range(n_desp):
            c = make_container(
                f"despawn_{idx}", desp_x, desp_y_base + i * 198, config,
                slot_count=3, max_rows=min(spec["max_rows"], 2),
                despawn=True
            )
            containers.append(c)
            idx += 1

    # Static containers (must be fully on-screen)
    static_count = max(2, static_count)

    if spec["use_despawn"]:
        # Despawn uses center column, so static uses 2-column layout
        positions = []
        cols = [200, 880]
        y_gap = {1: 250, 2: 300, 3: 350}.get(spec["max_rows"], 300)
        y0 = 300 + y_offset
        remaining = static_count
        row = 0
        while remaining > 0:
            y = y0 + row * y_gap
            if remaining >= 2:
                positions.extend([(x, min(1600, y)) for x in cols])
                remaining -= 2
            else:
                positions.append((200, min(1600, y)))
                remaining -= 1
            row += 1
    else:
        positions = get_static_positions(static_count, spec["max_rows"], y_offset)

    # Determine which static indices are locked or single-slot
    locked_indices = set()
    single_indices = set()

    if spec["use_locked"]:
        lc = min(spec["locked_count"], static_count)
        for i in range(max(0, static_count - lc), static_count):
            locked_indices.add(i)

    if spec["use_singleslot"]:
        candidates = [i for i in range(static_count) if i not in locked_indices]
        n_single = min(spec["singleslot_count"], len(candidates))
        for i in candidates[-n_single:]:
            single_indices.add(i)

    for i, (px, py) in enumerate(positions):
        slot_count = 1 if i in single_indices else 3
        is_locked = i in locked_indices
        unlock_matches = spec["locked_matches"] if is_locked else 0

        # Back-and-forth movement
        is_moving = False; move_type = ""; move_dir = ""
        move_speed = 50.0; move_dist = 200.0

        if spec["use_backforth"] and i < spec["backforth_count"] and not is_locked:
            is_moving = True
            move_type = "back_and_forth"
            move_dir = rng.choice(["left", "right"])
            move_speed = 40 + level * 0.4
            max_d = (880 - px) if move_dir == "right" else (px - 200)
            move_dist = min(150 + rng.randint(0, 100), max(50, int(max_d)))

        c = make_container(
            f"container_{idx}", px, py, config,
            slot_count=slot_count, max_rows=spec["max_rows"],
            is_locked=is_locked, unlock_matches=unlock_matches,
            is_moving=is_moving, move_type=move_type,
            move_direction=move_dir, move_speed=move_speed,
            move_distance=move_dist, is_single_slot=(i in single_indices)
        )
        containers.append(c)
        idx += 1

    return containers


# ── Placement Capacity ────────────────────────────────────────────────────────

def get_max_triple_count(containers, max_rows):
    """Calculate the maximum number of complete triples that can be placed.

    Per-row triple capacity = floor(slots_at_row / 3), since all 3 copies of
    a triple must be at the same row depth.  Locked containers are filled
    completely with their own triples.
    """
    unlocked = [c for c in containers if not c["is_locked"]]
    locked = [c for c in containers if c["is_locked"]]

    count = 0
    # Locked containers: capacity = floor(total_slots / 3)
    for lc in locked:
        mr = lc.get("max_rows_per_slot", max_rows)
        count += (lc["slot_count"] * mr) // 3

    # Unlocked containers: per-row capacity
    for r in range(max_rows):
        slots = sum(c["slot_count"] for c in unlocked
                    if c.get("max_rows_per_slot", max_rows) > r)
        count += slots // 3

    return count


# ── Item Placement (Reverse Construction) ────────────────────────────────────
#
# Levels are built by "playing the game in reverse":
#   1. Start with an empty board
#   2. Place 3 matching items in a container (un-match)
#   3. Shuffle items around with random valid moves
#   4. For multi-row: push all items one row deeper (un-advance)
#   5. Repeat from step 2 for next layer
#
# The level is guaranteed solvable because reversing the construction
# sequence gives a valid forward solution.

def place_items(containers, item_ids, max_rows, rng, level=1):
    """Place items using reverse construction for guaranteed solvability.

    Algorithm:
    1. Reserve full triples for locked containers (placed directly)
    2. Distribute remaining triples across row depths
    3. For each row depth, do reverse construction: place triple, shuffle at that row
    4. Result: items distributed across containers and rows, level is solvable

    Returns the estimated forward solution move count, or 0 on failure.
    """
    unlocked = [c for c in containers if not c["is_locked"]]
    locked = [c for c in containers if c["is_locked"]]

    if not unlocked:
        return 0

    # ── Internal state: grid[container_id][slot][row] = item_id or None ──
    c_rows = {}  # container_id -> actual max_rows for this container
    grid = {}
    for c in containers:
        mr = c.get("max_rows_per_slot", max_rows)
        c_rows[c["id"]] = mr
        grid[c["id"]] = [[None] * mr for _ in range(c["slot_count"])]

    def get_at(cid, slot, row):
        return grid[cid][slot][row]

    def set_at(cid, slot, row, val):
        grid[cid][slot][row] = val

    def find_container_with_n_empties_at_row(pool, n, row):
        """Find a container in pool with >= n empty slots at given row."""
        candidates = []
        for c in pool:
            if c["slot_count"] < n or c_rows[c["id"]] <= row:
                continue
            empties = sum(1 for s in range(c["slot_count"])
                         if get_at(c["id"], s, row) is None)
            if empties >= n:
                candidates.append(c)
        if candidates:
            return rng.choice(candidates)
        return None

    def shuffle_at_row(pool, row, n_moves):
        """Make n random swaps of items at the given row between containers."""
        actual = 0
        for _ in range(n_moves * 3):
            if actual >= n_moves:
                break
            sources = [(c, s) for c in pool for s in range(c["slot_count"])
                       if c_rows[c["id"]] > row and get_at(c["id"], s, row) is not None]
            dests = [(c, s) for c in pool for s in range(c["slot_count"])
                     if c_rows[c["id"]] > row and get_at(c["id"], s, row) is None]
            if not sources or not dests:
                break
            rng.shuffle(sources)
            rng.shuffle(dests)
            moved = False
            for sc, ss in sources:
                for dc, ds in dests:
                    if sc["id"] != dc["id"]:
                        item = get_at(sc["id"], ss, row)
                        set_at(sc["id"], ss, row, None)
                        set_at(dc["id"], ds, row, item)
                        actual += 1
                        moved = True
                        break
                if moved:
                    break
        return actual

    def place_triple_at_row(item_id, copies, pool, row):
        """Place copies of item_id into empty slots at given row in pool."""
        eligible = [c for c in pool if c_rows[c["id"]] > row]
        if copies == 3:
            target = find_container_with_n_empties_at_row(eligible, 3, row)
            if target:
                empties = [s for s in range(target["slot_count"])
                           if get_at(target["id"], s, row) is None]
                for i in range(3):
                    set_at(target["id"], empties[i], row, item_id)
                return 3
        placed = 0
        rng.shuffle(eligible)
        for c in eligible:
            for s in range(c["slot_count"]):
                if get_at(c["id"], s, row) is None and placed < copies:
                    set_at(c["id"], s, row, item_id)
                    placed += 1
        return placed

    # ── Reserve triples for locked containers ────────────────────────────
    triples = list(item_ids)
    rng.shuffle(triples)

    unlocked_triples = list(triples)

    for lc in locked:
        lc_mr = c_rows[lc["id"]]
        lc_capacity = lc["slot_count"] * lc_mr
        lc_triple_count = lc_capacity // 3
        assigned = []
        while len(assigned) < lc_triple_count and unlocked_triples:
            assigned.append(unlocked_triples.pop())
        idx = 0
        for item_id in assigned:
            for copy in range(3):
                slot = idx % lc["slot_count"]
                row = idx // lc["slot_count"]
                if row < lc_mr:
                    grid[lc["id"]][slot][row] = item_id
                    idx += 1

    # ── Distribute triples across row depths ─────────────────────────────
    # Calculate available slots per row depth in unlocked containers
    slots_per_row = []
    for r in range(max_rows):
        slots_per_row.append(
            sum(c["slot_count"] for c in unlocked if c_rows[c["id"]] > r))

    triples_by_row = [[] for _ in range(max_rows)]
    for item_id in unlocked_triples:
        # Find the row with most remaining capacity (greedy assignment)
        best_row = 0
        best_remaining = -1
        for r in range(max_rows):
            capacity = slots_per_row[r] // 3
            used = len(triples_by_row[r])
            remaining = capacity - used
            if remaining > best_remaining:
                best_remaining = remaining
                best_row = r
        triples_by_row[best_row].append(item_id)

    # ── Difficulty scaling: shuffles per triple ──────────────────────────
    base_shuffles = max(2, 1 + level // 8)
    total_construction_moves = 0

    # ── For each row depth, do reverse construction ──────────────────────
    # Row 0 = front (matched first in forward play)
    # Row 1+ = deeper (revealed after front cleared and rows advance)
    for row in range(max_rows):
        row_triples = triples_by_row[row]
        pool = [c for c in unlocked if c_rows[c["id"]] > row]

        for item_id in row_triples:
            placed = place_triple_at_row(item_id, 3, pool, row)
            if placed < 3:
                # Try any remaining unlocked container at this row
                for c in unlocked:
                    if c_rows[c["id"]] > row:
                        for s in range(c["slot_count"]):
                            if get_at(c["id"], s, row) is None and placed < 3:
                                set_at(c["id"], s, row, item_id)
                                placed += 1

            # Shuffle at this row to distribute items
            n_shuffle = base_shuffles + rng.randint(0, 2)
            moved = shuffle_at_row(pool, row, n_shuffle)
            total_construction_moves += moved

    # ── Ensure no empty unlocked containers (redistribute if needed) ────
    for c in unlocked:
        mr = c_rows[c["id"]]
        has_any = any(grid[c["id"]][s][r] is not None
                      for s in range(c["slot_count"]) for r in range(mr))
        if not has_any:
            fullest = max(unlocked, key=lambda x: sum(
                1 for s in range(x["slot_count"])
                if grid[x["id"]][s][0] is not None))
            for s in range(fullest["slot_count"]):
                if grid[fullest["id"]][s][0] is not None:
                    item = grid[fullest["id"]][s][0]
                    grid[fullest["id"]][s][0] = None
                    grid[c["id"]][0][0] = item
                    total_construction_moves += 1
                    break

    # ── Convert grid state to initial_items format ───────────────────────
    for c in containers:
        c["initial_items"] = []
        mr = c_rows[c["id"]]
        for s in range(c["slot_count"]):
            for r in range(mr):
                if grid[c["id"]][s][r] is not None:
                    c["initial_items"].append({
                        "id": grid[c["id"]][s][r],
                        "row": r,
                        "slot": s,
                    })

    return max(1, total_construction_moves)


# ── Timer & Thresholds ───────────────────────────────────────────────────────

def calc_timer(level, n_items):
    """Timer scales from 2.0s/item (L1) to 0.5s/item (L100)."""
    secs_per_item = 2.0 - (level - 1) * (1.5 / 99.0)
    return max(10, round(n_items * secs_per_item))


def estimate_thresholds(level, n_items, spec):
    """Estimate star thresholds. Will be updated by Unity solver."""
    factor = 0.7
    if spec["max_rows"] >= 2: factor += 0.1
    if spec["max_rows"] >= 3: factor += 0.1
    if spec["use_locked"]: factor += 0.05
    if spec["use_carousel"] or spec["use_backforth"]: factor += 0.05
    if spec["use_despawn"]: factor += 0.05
    if spec["use_singleslot"]: factor += 0.02

    est = max(3, round(n_items * factor))
    t3 = est
    t2 = max(t3 + 1, math.ceil(est * 1.15))
    t1 = max(t2 + 1, math.ceil(est * 1.30))
    fail = max(t1 + 1, math.ceil(est * 1.40))
    return [t3, t2, t1, fail]


# ── Level Generator ──────────────────────────────────────────────────────────

def generate_level(level, config, item_usage):
    """Generate a single level for the given world."""
    spec = get_level_spec(level)
    rng = spec["rng"]
    containers = build_containers(spec, config)

    # Level 1: hardcoded 2-move tutorial
    if level == 1:
        available = get_available_items(config, level)
        selected = select_items(rng, available, 2, item_usage)
        a, b = selected[0], selected[1]
        # C1: [A, A, _]  C2: [B, B, _]  C3: [A, B, _]
        # Move A from C3->C1 = match, Move B from C3->C2 = match -> 2 moves
        containers[0]["initial_items"] = [
            {"id": a, "row": 0, "slot": 0},
            {"id": a, "row": 0, "slot": 1},
        ]
        containers[1]["initial_items"] = [
            {"id": b, "row": 0, "slot": 0},
            {"id": b, "row": 0, "slot": 1},
        ]
        containers[2]["initial_items"] = [
            {"id": a, "row": 0, "slot": 0},
            {"id": b, "row": 0, "slot": 1},
        ]
        timer = calc_timer(level, 6)
        return {
            "id": level, "world_id": config.world_id, "name": f"level_{level:03d}",
            "star_move_thresholds": [2, 3, 4, 5], "time_limit_seconds": timer,
            "containers": containers, "moving_tracks": []
        }

    # Calculate items from fill ratio (primary driver) and variety (secondary)
    total_capacity = sum(c["slot_count"] * c["max_rows_per_slot"] for c in containers)
    target_fill = get_target_fill_ratio(level)
    target_items = math.ceil(total_capacity * target_fill)
    target_items = ((target_items + 2) // 3) * 3     # Round up to nearest multiple of 3

    available = get_available_items(config, level)
    max_types = max(2, (total_capacity - 3) // 3)     # Leave at least 1 slot buffer
    fill_types = max(2, target_items // 3)
    variety_types = get_target_types(level)

    n_types = max(fill_types, variety_types)
    n_types = min(n_types, max_types, len(available))
    # Cap to actual placeable triple count (per-row rounding loses some slots)
    max_triples = get_max_triple_count(containers, spec["max_rows"])
    n_types = min(n_types, max_triples)
    n_types = max(2, n_types)
    n_items = n_types * 3

    selected = select_items(rng, available, n_types, item_usage)
    construction_moves = place_items(containers, selected, spec["max_rows"], rng, level)

    actual = sum(len(c["initial_items"]) for c in containers)
    if construction_moves == 0 or actual != n_items:
        print(f"  !! Level {level}: PLACEMENT FAILED - placed {actual}/{n_items}")
        for c in containers:
            cap = c["slot_count"] * c["max_rows_per_slot"]
            used = len(c["initial_items"])
            print(f"     {c['id']}: {used}/{cap}")

    timer = calc_timer(level, n_items)
    thresholds = estimate_thresholds(level, n_items, spec)

    return {
        "id": level, "world_id": config.world_id, "name": f"level_{level:03d}",
        "star_move_thresholds": thresholds, "time_limit_seconds": timer,
        "containers": containers, "moving_tracks": []
    }


# ── Main Entry Point ─────────────────────────────────────────────────────────

def generate_levels(config, output_dir, count=100):
    """Generate levels for the given world configuration.
    Returns list of error strings (empty = success)."""
    os.makedirs(output_dir, exist_ok=True)

    # Delete existing levels
    for f in os.listdir(output_dir):
        if f.startswith("level_") and f.endswith(".json"):
            os.remove(os.path.join(output_dir, f))
            print(f"  Deleted old {f}")

    all_items = config.all_items
    item_count = len(all_items)
    item_usage = {item: 0 for item in all_items}

    print(f"\nGenerating {count} {config.world_id.title()} levels to: {output_dir}\n")

    stats = []
    errors = []

    for level in range(1, count + 1):
        level_data = generate_level(level, config, item_usage)
        filepath = os.path.join(output_dir, f"level_{level:03d}.json")
        with open(filepath, "w") as f:
            json.dump(level_data, f, indent=4)

        n_items = sum(len(c["initial_items"]) for c in level_data["containers"])
        n_containers = len(level_data["containers"])
        thresh = level_data["star_move_thresholds"]
        timer = level_data["time_limit_seconds"]
        n_types = n_items // 3 if n_items > 0 else 0

        if n_items % 3 != 0:
            errors.append(f"L{level}: {n_items} items (NOT multiple of 3!)")

        mechanics = set()
        max_r = 1
        for c in level_data["containers"]:
            if c["is_moving"] and c["move_type"] == "carousel": mechanics.add("carousel")
            if c["is_moving"] and c["move_type"] == "back_and_forth": mechanics.add("b&f")
            if c["is_locked"]: mechanics.add("locked")
            if c["despawn_on_match"]: mechanics.add("despawn")
            if c["slot_count"] == 1: mechanics.add("single")
            max_r = max(max_r, c["max_rows_per_slot"])

        # Note: starting triples are expected with reverse construction
        # (they provide the first match opportunity)

        # Check static container positions
        for c in level_data["containers"]:
            if not c["is_moving"] and not c["despawn_on_match"]:
                x, y = c["position"]["x"], c["position"]["y"]
                if x < 150 or x > 930 or y < 150 or y > 1700:
                    errors.append(f"L{level}: {c['id']} at ({x},{y}) off-screen")

        # Fill stats
        total_cap = sum(c["slot_count"] * c["max_rows_per_slot"]
                       for c in level_data["containers"])
        fill_pct = round(100 * n_items / total_cap) if total_cap > 0 else 0
        n_empty = sum(1 for c in level_data["containers"]
                     if len(c["initial_items"]) == 0)
        n_full = sum(1 for c in level_data["containers"]
                     if len(c["initial_items"]) == c["slot_count"] * c["max_rows_per_slot"])
        if n_empty > 0:
            errors.append(f"L{level}: {n_empty} empty container(s)")

        stat = (f"L{level:3d}: {n_containers:2d}c, {n_types:2d}t, {n_items:3d}i, "
                f"{max_r}r, {fill_pct:3d}% fill, {n_empty}e/{n_full}f, "
                f"thresh={thresh}, timer={timer}s")
        if mechanics:
            stat += f", [{', '.join(sorted(mechanics))}]"
        stats.append(stat)
        print(stat)

    # Item usage report
    print(f"\n{'='*60}")
    print(f"Item usage across all {count} levels:")
    unused = [item for item in all_items if item_usage[item] == 0]
    min_used = min(item_usage.values())
    max_used = max(item_usage.values())
    print(f"  Min usage: {min_used}, Max usage: {max_used}")
    if unused:
        print(f"  UNUSED ITEMS: {unused}")
        errors.append(f"Unused items: {unused}")
    else:
        print(f"  All {item_count} items used!")

    if errors:
        print(f"\nERRORS ({len(errors)}):")
        for e in errors:
            print(f"  {e}")
    else:
        print(f"\nNo errors!")

    print(f"\nDone! Generated {count} levels.")
    print("IMPORTANT: Run 'Tools > Sort Resort > Solver > Update All Level Thresholds' in Unity")

    # Summary file
    summary_path = os.path.join(output_dir, "..", f"{config.world_id}_levels_summary.txt")
    with open(summary_path, "w") as f:
        f.write(f"{config.world_id.title()} Levels Summary\n")
        f.write("=" * 80 + "\n\n")
        f.write("NOTE: Thresholds are ESTIMATED. Run Unity solver to finalize.\n\n")
        for s in stats:
            f.write(s + "\n")
        f.write(f"\nItem usage: min={min_used}, max={max_used}\n")
        if unused:
            f.write(f"UNUSED: {unused}\n")
        if errors:
            f.write(f"\nErrors:\n")
            for e in errors:
                f.write(f"  {e}\n")

    return errors
