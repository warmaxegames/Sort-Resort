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
    single_slot_lock_overlay_image: str = ""

    def __post_init__(self):
        if not self.container_image:
            self.container_image = f"{self.world_id}_container"
        if not self.single_slot_image:
            self.single_slot_image = f"{self.world_id}_single_slot_container"
        if not self.lock_overlay_image:
            self.lock_overlay_image = f"{self.world_id}_lockoverlay"
        if not self.single_slot_lock_overlay_image:
            self.single_slot_lock_overlay_image = f"{self.world_id}_single_slot_lockoverlay"

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


# ── Container Dimensions (Godot Pixels) ─────────────────────────────────────
# Base slot: 83px × uniform_scale(1.14) × border_scale(1.2)
CONTAINER_WIDTH_3SLOT = 83 * 1.14 * 3 * 1.2   # ~341px
CONTAINER_WIDTH_1SLOT = 83 * 1.14 * 1 * 1.2   # ~114px
SLOT_HEIGHT = 166 * 1.14                        # ~189px
ROW_DEPTH_OFFSET = 4 * 1.14                     # ~4.56px per extra row
MIN_CONTAINER_GAP = 30  # minimum gap between container edges (pixels)

# Screen safe bounds (Godot pixels) — container centers must keep edges inside
SCREEN_MIN_X, SCREEN_MAX_X = 200, 880
SCREEN_MIN_Y, SCREEN_MAX_Y = 250, 1600

# Carousel horizontal spacing (edge-to-edge + small gap)
CAROUSEL_H_SPACING = int(CONTAINER_WIDTH_3SLOT) + 15  # ~356px
CAROUSEL_V_SPACING = int(SLOT_HEIGHT) + MIN_CONTAINER_GAP             # ~219px


def _container_half_width(slot_count):
    """Half-width of a container in Godot pixels."""
    if slot_count == 1:
        return CONTAINER_WIDTH_1SLOT / 2
    return CONTAINER_WIDTH_3SLOT / 2


def _container_half_height(max_rows):
    """Half visual height of a container in Godot pixels."""
    base = SLOT_HEIGHT
    extra = ROW_DEPTH_OFFSET * max(0, max_rows - 1)
    return (base + extra) / 2


def _get_bounding_box(cx, cy, slot_count, max_rows):
    """Return (x_min, x_max, y_min, y_max) bounding box for a container."""
    hw = _container_half_width(slot_count)
    hh = _container_half_height(max_rows)
    return (cx - hw, cx + hw, cy - hh, cy + hh)


def _boxes_overlap(box_a, box_b, gap=MIN_CONTAINER_GAP):
    """Check if two AABB bounding boxes overlap (with gap)."""
    ax_min, ax_max, ay_min, ay_max = box_a
    bx_min, bx_max, by_min, by_max = box_b
    return (ax_min - gap < bx_max and ax_max + gap > bx_min and
            ay_min - gap < by_max and ay_max + gap > by_min)


def _get_travel_box(cx, cy, slot_count, max_rows, move_dir, distance):
    """Swept bounding box for a moving container over its full travel path."""
    hw = _container_half_width(slot_count)
    hh = _container_half_height(max_rows)
    if move_dir == "right":
        return (cx - hw, cx + distance + hw, cy - hh, cy + hh)
    elif move_dir == "left":
        return (cx - distance - hw, cx + hw, cy - hh, cy + hh)
    elif move_dir == "down":
        return (cx - hw, cx + hw, cy - hh, cy + distance + hh)
    elif move_dir == "up":
        return (cx - hw, cx + hw, cy - distance - hh, cy + hh)
    return (cx - hw, cx + hw, cy - hh, cy + hh)


def _get_safe_backforth_distance(px, py, slot_count, max_rows, move_dir, placed_ranges):
    """Compute the max safe back-and-forth distance that won't overlap neighbors.

    Uses full 2D bounding box overlap checks against ALL containers (not just
    same-row). This prevents movers from passing through statics on adjacent rows.

    Args:
        px, py: container center position (Godot pixels)
        slot_count: number of slots (1 or 3)
        max_rows: row depth for height calculation
        move_dir: "left" or "right"
        placed_ranges: list of tuples (x_min, x_max, y_min, y_max, idx)

    Returns:
        max safe move_distance in pixels (may be 0 if no room)
    """
    hw = _container_half_width(slot_count)
    hh = _container_half_height(max_rows)

    # Screen bounds (container edge must stay on-screen)
    if move_dir == "right":
        screen_max_dist = SCREEN_MAX_X - px
    else:
        screen_max_dist = px - SCREEN_MIN_X

    max_dist = max(0, screen_max_dist)

    # Binary-search style: check decreasing distances against all neighbors
    # For efficiency, compute analytically per neighbor
    my_y_min = py - hh
    my_y_max = py + hh

    for r in placed_ranges:
        rx_min, rx_max, ry_min, ry_max, _idx = r

        # Skip if no vertical overlap (with gap)
        if my_y_max + MIN_CONTAINER_GAP <= ry_min or my_y_min - MIN_CONTAINER_GAP >= ry_max:
            continue

        if move_dir == "right":
            # Moving right: right edge at distance d = px + d + hw
            # Must not reach neighbor's left edge minus gap
            if rx_min > px + hw:
                safe = rx_min - hw - MIN_CONTAINER_GAP - px
                max_dist = min(max_dist, max(0, safe))
        else:
            # Moving left: left edge at distance d = px - d - hw
            # Must not reach neighbor's right edge plus gap
            if rx_max < px - hw:
                safe = px - rx_max - hw - MIN_CONTAINER_GAP
                max_dist = min(max_dist, max(0, safe))

    return max_dist


# ── Symmetric Back-and-Forth Parameters ─────────────────────────────────────

def _compute_bf_params(positions, single_indices, locked_indices, spec, placed_ranges, rng):
    """Pre-compute symmetric back-and-forth parameters per row group.

    Groups b&f candidates by row (Y position), then assigns equal move_distance
    to containers on the same row so movement looks visually balanced.

    Args:
        positions: list of (x, y) tuples for all static containers
        single_indices: set of position indices that are single-slot
        locked_indices: set of position indices that are locked
        spec: level spec dict
        placed_ranges: list of tuples (x_min, x_max, y_min, y_max, idx)
        rng: random.Random instance

    Returns:
        dict mapping position index -> (direction, distance, speed)
    """
    level = spec["level"]
    max_rows = spec["max_rows"]
    bf_speed = 40 + level * 0.4
    Y_ROW_TOLERANCE = 150  # for grouping b&f candidates into visual rows

    # Identify which position indices are back-and-forth movers
    bf_indices = []
    if spec["use_backforth"]:
        for i in range(min(spec["backforth_count"], len(positions))):
            if i not in locked_indices:
                bf_indices.append(i)

    if not bf_indices:
        return {}

    # Group b&f indices by row (Y position within tolerance)
    row_groups = []  # list of lists of indices
    assigned = set()
    for i in bf_indices:
        if i in assigned:
            continue
        group = [i]
        assigned.add(i)
        _, iy = positions[i]
        for j in bf_indices:
            if j not in assigned and abs(positions[j][1] - iy) <= Y_ROW_TOLERANCE:
                group.append(j)
                assigned.add(j)
        row_groups.append(group)

    bf_params = {}  # index -> (direction, distance, speed)

    def _update_range(idx, direction, dist, slots):
        """Update placed_ranges entry for a mover's travel bounds."""
        hw = _container_half_width(slots)
        for ri, r in enumerate(placed_ranges):
            if r[4] == idx:
                x_min, x_max, y_min, y_max, _ = r
                if direction == "right":
                    x_max = positions[idx][0] + dist + hw
                else:
                    x_min = positions[idx][0] - dist - hw
                placed_ranges[ri] = (x_min, x_max, y_min, y_max, idx)
                break

    for group in row_groups:
        if len(group) == 1:
            i = group[0]
            px, py = positions[i]
            slot_count = 1 if i in single_indices else 3
            preferred_dir = rng.choice(["left", "right"])
            desired_dist = 150 + rng.randint(0, 100)
            other_ranges = [r for r in placed_ranges if r[4] != i]

            for try_dir in [preferred_dir, "left" if preferred_dir == "right" else "right"]:
                safe = _get_safe_backforth_distance(
                    px, py, slot_count, max_rows, try_dir, other_ranges)
                capped = min(desired_dist, int(safe))
                if capped >= 50:
                    bf_params[i] = (try_dir, capped, bf_speed)
                    _update_range(i, try_dir, capped, slot_count)
                    break

        elif len(group) >= 2:
            group.sort(key=lambda idx: positions[idx][0])
            pair_idx = 0
            while pair_idx + 1 < len(group):
                left_i = group[pair_idx]
                right_i = group[pair_idx + 1]
                lx, ly = positions[left_i]
                rx, ry = positions[right_i]
                left_slots = 1 if left_i in single_indices else 3
                right_slots = 1 if right_i in single_indices else 3
                left_hw = _container_half_width(left_slots)
                right_hw = _container_half_width(right_slots)

                other_ranges = [r for r in placed_ranges
                                if r[4] != left_i and r[4] != right_i]

                desired_dist = 150 + rng.randint(0, 100)

                # Option A: Inward movement
                inner_gap = rx - right_hw - (lx + left_hw) - MIN_CONTAINER_GAP * 2
                inward_max = max(0, int(inner_gap / 2))
                left_inward_safe = _get_safe_backforth_distance(
                    lx, ly, left_slots, max_rows, "right", other_ranges)
                right_inward_safe = _get_safe_backforth_distance(
                    rx, ry, right_slots, max_rows, "left", other_ranges)
                inward_dist = min(desired_dist, inward_max,
                                  int(left_inward_safe), int(right_inward_safe))

                # Option B: Outward movement
                left_outward_safe = _get_safe_backforth_distance(
                    lx, ly, left_slots, max_rows, "left", other_ranges)
                right_outward_safe = _get_safe_backforth_distance(
                    rx, ry, right_slots, max_rows, "right", other_ranges)
                outward_dist = min(desired_dist,
                                   int(left_outward_safe), int(right_outward_safe))

                if inward_dist >= 50 and inward_dist >= outward_dist:
                    sym_dist = inward_dist
                    left_dir, right_dir = "right", "left"
                elif outward_dist >= 50:
                    sym_dist = outward_dist
                    left_dir, right_dir = "left", "right"
                elif inward_dist >= 50:
                    sym_dist = inward_dist
                    left_dir, right_dir = "right", "left"
                else:
                    pair_idx += 2
                    continue

                bf_params[left_i] = (left_dir, sym_dist, bf_speed)
                bf_params[right_i] = (right_dir, sym_dist, bf_speed)
                _update_range(left_i, left_dir, sym_dist, left_slots)
                _update_range(right_i, right_dir, sym_dist, right_slots)

                pair_idx += 2

            # Handle odd leftover
            if pair_idx < len(group):
                i = group[pair_idx]
                px, py = positions[i]
                slot_count = 1 if i in single_indices else 3
                preferred_dir = rng.choice(["left", "right"])
                desired_dist = 150 + rng.randint(0, 100)
                other_ranges_single = [r for r in placed_ranges if r[4] != i]

                for try_dir in [preferred_dir, "left" if preferred_dir == "right" else "right"]:
                    safe = _get_safe_backforth_distance(
                        px, py, slot_count, max_rows, try_dir, other_ranges_single)
                    capped = min(desired_dist, int(safe))
                    if capped >= 50:
                        bf_params[i] = (try_dir, capped, bf_speed)
                        _update_range(i, try_dir, capped, slot_count)
                        break

    return bf_params


# ── Container Positions ──────────────────────────────────────────────────────

def get_y_gap(max_rows, n_containers=0, level=1):
    """Dynamic vertical spacing between container rows.

    Base gaps are tighter than the old fixed values. Reduces further at
    higher levels and higher container counts to fit more on screen.
    Minimum gap ensures containers never overlap.
    """
    base = {1: 220, 2: 260, 3: 300}.get(max_rows, 260)

    # Tighter at higher levels
    if level >= 40:
        base = int(base * 0.85)

    # Tighter with many containers
    if n_containers >= 10:
        base = int(base * 0.85)

    # Minimum: visual height + gap
    min_gap = int(SLOT_HEIGHT + ROW_DEPTH_OFFSET * max(0, max_rows - 1)) + MIN_CONTAINER_GAP
    return max(min_gap, base)


def get_static_positions(count, max_rows, y_offset=0, level=1):
    """Grid positions for static containers. All within safe screen bounds."""
    y_gap = get_y_gap(max_rows, count, level)
    cols_3 = [200, 540, 880]
    cols_2 = [370, 710]

    n_rows_needed = math.ceil(count / 3)
    total_height = (n_rows_needed - 1) * y_gap
    y_min = SCREEN_MIN_Y + y_offset
    y_max = SCREEN_MAX_Y
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

    return [(max(SCREEN_MIN_X, min(SCREEN_MAX_X, x)),
             max(SCREEN_MIN_Y, min(SCREEN_MAX_Y, y))) for x, y in positions]


# ── Container Builder ────────────────────────────────────────────────────────

def make_container(cid, x, y, config, slot_count=3, max_rows=1, is_locked=False,
                   unlock_matches=0, is_moving=False, move_type="",
                   move_direction="", move_speed=50.0, move_distance=200.0,
                   despawn=False, is_single_slot=False):
    """Create a container dict for level JSON."""
    cont_img = config.single_slot_image if is_single_slot else config.container_image
    if is_locked:
        lock_img = (config.single_slot_lock_overlay_image if is_single_slot
                    else config.lock_overlay_image)
    else:
        lock_img = ""
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
    """Determine level parameters based on level number.

    Uses a cumulative mechanic availability system: each mechanic has an
    unlock level and persists thereafter (probability-based). A complexity
    cap prevents overwhelming early levels.
    """
    rng = random.Random(level * 42 + 7)

    # ── Faster container count ramp ────────────────────────────────────
    if level <= 1:
        n_containers = 3
    elif level <= 3:
        n_containers = level + 2                          # 4, 5
    elif level <= 7:
        n_containers = min(5 + (level - 4) // 2, 7)      # 5-7
    elif level <= 15:
        n_containers = min(7 + (level - 8) // 4, 9)      # 7-9
    elif level <= 30:
        n_containers = min(8 + (level - 16) // 7, 10)    # 8-10
    elif level <= 60:
        n_containers = min(9 + (level - 31) // 15, 11)   # 9-11
    else:
        n_containers = min(10 + (level - 61) // 20, 12)  # 10-12

    max_rows = get_max_rows(level)

    # ── Mechanic unlock levels ─────────────────────────────────────────
    UNLOCK_LOCKED    = 11
    UNLOCK_SINGLE    = 16
    UNLOCK_BACKFORTH = 26
    UNLOCK_CAROUSEL  = 31
    UNLOCK_DESPAWN   = 36
    INTRO_RANGE      = 5  # levels after unlock where mechanic is guaranteed

    # ── Cumulative mechanic availability ───────────────────────────────
    # Each mechanic: (unlock_level, name)
    mechanics_available = []
    for unlock, name in [
        (UNLOCK_LOCKED,    "locked"),
        (UNLOCK_SINGLE,    "singleslot"),
        (UNLOCK_BACKFORTH, "backforth"),
        (UNLOCK_CAROUSEL,  "carousel"),
        (UNLOCK_DESPAWN,   "despawn"),
    ]:
        if level >= unlock:
            mechanics_available.append((unlock, name))

    # Determine which mechanics are active this level
    active_mechanics = set()
    for unlock, name in mechanics_available:
        levels_since = level - unlock
        if levels_since < INTRO_RANGE:
            # Introduction range: guaranteed
            active_mechanics.add(name)
        else:
            # Post-introduction: probability scales 30% -> 70%
            prob = min(0.70, 0.30 + (levels_since - INTRO_RANGE) * 0.005)
            if rng.random() < prob:
                active_mechanics.add(name)

    # ── Complexity cap (early levels only) ─────────────────────────────
    # max 1 mechanic at L11-15, max 2 at L16-25, max 3 at L26-40, no cap L41+
    if level < 16:
        max_mechanics = 1
    elif level < 26:
        max_mechanics = 2
    elif level < 41:
        max_mechanics = 3
    else:
        max_mechanics = 99

    # If over cap, remove newest non-intro mechanics first
    while len(active_mechanics) > max_mechanics:
        # Find the mechanic with the highest unlock level that isn't in intro
        removable = []
        for unlock, name in reversed(mechanics_available):
            if name in active_mechanics and (level - unlock) >= INTRO_RANGE:
                removable.append(name)
        if removable:
            active_mechanics.discard(removable[0])
        else:
            break  # All remaining are in intro range, can't remove

    # ── Configure each mechanic's parameters ───────────────────────────
    use_locked = "locked" in active_mechanics
    use_singleslot = "singleslot" in active_mechanics
    use_backforth = "backforth" in active_mechanics
    use_carousel = "carousel" in active_mechanics
    use_despawn = "despawn" in active_mechanics

    # Locked: count and matches scale with level
    locked_count = 0
    locked_matches_range = (0, 0)
    if use_locked:
        locked_count = 1 + (level - UNLOCK_LOCKED) // 15
        locked_count = min(locked_count, max(1, n_containers // 4))
        lo = min(3, 2 + (level - UNLOCK_LOCKED) // 15)
        hi = min(3, lo + 1)
        locked_matches_range = (lo, hi)

    # Single-slot: count scales
    singleslot_count = 0
    if use_singleslot:
        singleslot_count = 1 + (level - UNLOCK_SINGLE) // 25
        singleslot_count = min(singleslot_count, max(1, n_containers // 3))

    # Back-and-forth: count scales
    backforth_count = 0
    if use_backforth:
        backforth_count = 1 + (level - UNLOCK_BACKFORTH) // 20
        backforth_count = min(backforth_count, 3)

    # Carousel: count scales
    carousel_count = 0
    if use_carousel:
        carousel_count = 3 + (level - UNLOCK_CAROUSEL) // 20
        carousel_count = min(carousel_count, 5)

    # Despawn: count scales
    despawn_count = 0
    if use_despawn:
        despawn_count = 2 + (level - UNLOCK_DESPAWN) // 15
        despawn_count = min(despawn_count, 4)

    return {
        "level": level, "n_containers": n_containers, "max_rows": max_rows,
        "use_locked": use_locked, "locked_count": locked_count,
        "locked_matches_range": locked_matches_range,
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
    center_col_occupied = False  # True when despawn or vertical carousel uses X=540

    # Carousel containers (allowed off-screen, they scroll in)
    if spec["use_carousel"]:
        car_speed = 60 + level * 0.3
        car_mr = min(spec["max_rows"], 2)
        hw = CONTAINER_WIDTH_3SLOT / 2  # ~170

        # For levels 50+, randomly choose vertical or horizontal carousel
        use_vertical_carousel = level >= 50 and rng.random() < 0.4

        if use_vertical_carousel:
            # Vertical carousel at X=540: disable despawn (shares center column)
            spec["use_despawn"] = False
            center_col_occupied = True

            # Subtract only the budgeted carousel_count from statics (rest are free)
            carousel_subtract = min(spec["carousel_count"], 3)
            static_count -= carousel_subtract

            n_car = 10  # Fixed count for seamless vertical wrapping
            car_dir = rng.choice(["up", "down"])
            spacing = CAROUSEL_V_SPACING
            car_x = 540
            hh = _container_half_height(car_mr)

            # Start positions fully off-screen
            if car_dir == "down":
                start_y = -(hh + 30)
            else:
                start_y = 1920 + hh + 30

            for i in range(n_car):
                if car_dir == "down":
                    cy = start_y + i * spacing
                else:
                    cy = start_y - i * spacing
                c = make_container(
                    f"carousel_{idx}", car_x, cy, config,
                    slot_count=3, max_rows=car_mr,
                    is_moving=True, move_type="carousel",
                    move_direction=car_dir, move_speed=car_speed,
                    move_distance=n_car * spacing
                )
                containers.append(c)
                idx += 1
            y_offset = 0  # vertical carousel doesn't push static layout down
        else:
            # Horizontal carousel: enforce minimum 5 for seamless wrapping
            n_car = max(5, min(spec["carousel_count"], 5))
            static_count -= n_car

            car_dir = rng.choice(["right", "left"])
            spacing = CAROUSEL_H_SPACING
            car_y = 200

            # Start positions: first container fully off-screen
            if car_dir == "right":
                start_x = -(hw + 30)
            else:
                start_x = 1080 + hw + 30

            for i in range(n_car):
                if car_dir == "right":
                    cx = start_x + i * spacing
                else:
                    cx = start_x - i * spacing
                c = make_container(
                    f"carousel_{idx}", cx, car_y, config,
                    slot_count=3, max_rows=car_mr,
                    is_moving=True, move_type="carousel",
                    move_direction=car_dir, move_speed=car_speed,
                    move_distance=n_car * spacing
                )
                containers.append(c)
                idx += 1

            # Push statics below carousel with proper dynamic gap
            y_offset = get_y_gap(spec["max_rows"], static_count, level)

    # Despawn containers (single-column vertical stack at center X=540)
    # Bottom container visible, upper containers stacked above (some off-screen).
    # When bottom is cleared, containers above fall down into view.
    if spec["use_despawn"]:
        n_desp = min(spec["despawn_count"], 4)
        static_count -= n_desp
        desp_mr = min(spec["max_rows"], 2)
        vis_h = int(SLOT_HEIGHT + ROW_DEPTH_OFFSET * max(0, desp_mr - 1))
        v_spacing = vis_h + 5   # edge-to-edge with minimal gap

        center_col_occupied = True

        # Single-slot despawn for levels 50+ (30% chance for last container)
        desp_single_last = level >= 50 and rng.random() < 0.30

        # Single-column stack at X=540, bottom container at play area top
        desp_x = 540
        desp_y_bottom = SCREEN_MIN_Y + y_offset  # bottom of stack (visible)

        for i in range(n_desp):
            desp_y = desp_y_bottom - i * v_spacing  # stack upward

            is_last = (i == n_desp - 1)
            slot_count = 1 if (desp_single_last and is_last) else 3
            c = make_container(
                f"despawn_{idx}", desp_x, desp_y, config,
                slot_count=slot_count, max_rows=desp_mr,
                despawn=True, is_single_slot=(slot_count == 1)
            )
            containers.append(c)
            idx += 1

    # Static containers (must be fully on-screen)
    static_count = max(2, static_count)

    # Determine which static indices are locked or single-slot
    # (must be computed before position generation to inform b&f layout)
    locked_indices = set()
    single_indices = set()

    if spec["use_locked"]:
        lc = min(spec["locked_count"], static_count)
        # Randomize locked indices from positions 1+ (never lock position 0)
        lock_candidates = list(range(1, static_count))
        rng.shuffle(lock_candidates)
        for i in lock_candidates[:lc]:
            locked_indices.add(i)

    if spec["use_singleslot"]:
        # Allow locked single-slots (remove the locked exclusion filter)
        candidates = list(range(static_count))
        rng.shuffle(candidates)
        n_single = min(spec["singleslot_count"], len(candidates))
        for i in candidates[:n_single]:
            single_indices.add(i)

    # Count back-and-forth containers (first N non-locked positions)
    n_bf = 0
    if spec["use_backforth"]:
        for i in range(min(spec["backforth_count"], static_count)):
            if i not in locked_indices:
                n_bf += 1

    y_gap = get_y_gap(spec["max_rows"], static_count, level)

    if n_bf > 0:
        # Back-and-forth containers need dedicated wide-spacing rows
        # (max 2 per row at screen edges, or 1 centered) so they have
        # room to move without overlapping neighbors.
        bf_positions = []
        bf_y = SCREEN_MIN_Y + y_offset
        bf_remaining = n_bf
        while bf_remaining > 0:
            if bf_remaining >= 2:
                bf_positions.extend([(200, min(SCREEN_MAX_Y, bf_y)),
                                     (880, min(SCREEN_MAX_Y, bf_y))])
                bf_remaining -= 2
            else:
                # Odd b&f: use left column if center is occupied
                odd_x = 200 if center_col_occupied else 540
                bf_positions.append((odd_x, min(SCREEN_MAX_Y, bf_y)))
                bf_remaining -= 1
            bf_y += y_gap

        bf_rows_used = math.ceil(n_bf / 2)
        n_static_only = static_count - n_bf

        if center_col_occupied:
            # Remaining static in 2-column layout (center occupied by despawn/carousel)
            cols = [200, 880]
            y0 = SCREEN_MIN_Y + y_offset + bf_rows_used * y_gap
            static_positions = []
            remaining = n_static_only
            row = 0
            while remaining > 0:
                y = y0 + row * y_gap
                if remaining >= 2:
                    static_positions.extend([(x, min(SCREEN_MAX_Y, y)) for x in cols])
                    remaining -= 2
                else:
                    static_positions.append((200, min(SCREEN_MAX_Y, y)))
                    remaining -= 1
                row += 1
        else:
            # Remaining static in standard 3-column layout
            if n_static_only > 0:
                static_y_offset = y_offset + bf_rows_used * y_gap
                static_positions = get_static_positions(
                    n_static_only, spec["max_rows"], static_y_offset, level)
            else:
                static_positions = []

        positions = bf_positions + static_positions

    elif center_col_occupied:
        # 2-column layout (center occupied by despawn or vertical carousel)
        positions = []
        cols = [200, 880]
        y0 = SCREEN_MIN_Y + y_offset
        remaining = static_count
        row = 0
        while remaining > 0:
            y = y0 + row * y_gap
            if remaining >= 2:
                positions.extend([(x, min(SCREEN_MAX_Y, y)) for x in cols])
                remaining -= 2
            else:
                positions.append((200, min(SCREEN_MAX_Y, y)))
                remaining -= 1
            row += 1
    else:
        # Standard 3-column layout
        positions = get_static_positions(static_count, spec["max_rows"], y_offset, level)

    # ── Bounds validation: clamp container centers to safe screen area ──
    # Centers stay within SCREEN_MIN/MAX bounds; edges naturally extend beyond
    clamped = []
    for i, (px, py) in enumerate(positions):
        cx = max(SCREEN_MIN_X, min(SCREEN_MAX_X, px))
        cy = max(SCREEN_MIN_Y, min(SCREEN_MAX_Y, py))
        clamped.append((cx, cy))
    positions = clamped

    # Track bounding ranges of all containers for overlap prevention.
    # Each entry: (x_min, x_max, y_min, y_max, idx) where idx is the
    # position index (or -1 for carousel/despawn).
    placed_ranges = []

    # Pre-populate with carousel/despawn containers already placed
    for c in containers:
        cx = c["position"]["x"]
        cy = c["position"]["y"]
        mr = c.get("max_rows_per_slot", spec["max_rows"])
        if c["is_moving"] and c["move_type"] == "carousel":
            # Carousel containers sweep the full screen; skip overlap checks
            pass
        else:
            box = _get_bounding_box(cx, cy, c["slot_count"], mr)
            placed_ranges.append((*box, -1))

    # Pre-populate with ALL static positions (as static bounding boxes)
    for i, (px, py) in enumerate(positions):
        slot_count = 1 if i in single_indices else 3
        box = _get_bounding_box(px, py, slot_count, spec["max_rows"])
        placed_ranges.append((*box, i))

    # Pre-compute symmetric back-and-forth parameters per row group
    bf_params = _compute_bf_params(
        positions, single_indices, locked_indices, spec, placed_ranges, rng)

    for i, (px, py) in enumerate(positions):
        slot_count = 1 if i in single_indices else 3
        is_locked = i in locked_indices
        if is_locked:
            lo, hi = spec["locked_matches_range"]
            unlock_matches = rng.randint(lo, hi) if lo <= hi else lo
        else:
            unlock_matches = 0

        # Back-and-forth movement (pre-computed symmetrically)
        is_moving = False; move_type = ""; move_dir = ""
        move_speed = 50.0; move_dist = 200.0

        if i in bf_params:
            is_moving = True
            move_type = "back_and_forth"
            move_dir, move_dist, move_speed = bf_params[i]

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
