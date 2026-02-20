#!/usr/bin/env python3
"""
Sort Resort - Reverse-Play Level Generator

Generates levels by playing the game in reverse, guaranteeing solvability
and providing exact star thresholds without needing a separate solver.

Algorithm (V2 - push-based, no work container):
  For each triple:
    1. Pick a random unlocked 3-slot container
    2. If front row has items, push all items one row deeper
    3. Place 3 matching items at front row
    4. Randomly scatter 1-3 items to other containers' empty front slots
    5. Each scattered item = 1 construction move = 1 forward move

  Locked containers are pre-filled with complete triples (instant match on unlock).
  The reverse of the construction sequence IS the forward solution.
  Star thresholds are derived from construction move count.
"""

import json
import math
import os
import random
from typing import List, Tuple

from level_generator import (
    WorldConfig, get_level_spec, build_containers,
    get_available_items, select_items, calc_timer,
    get_target_fill_ratio, get_target_types,
    _get_bounding_box, _boxes_overlap, _get_travel_box,
    _container_half_height,
    SCREEN_MIN_X, SCREEN_MAX_X, SCREEN_MIN_Y, SCREEN_MAX_Y,
    MIN_CONTAINER_GAP, HUD_BAR_BOTTOM_Y,
)
from level_solver import solve_level, solve_level_best


# ── Reverse-Play Item Placement ─────────────────────────────────────────────

def reverse_place_items(containers, item_ids, max_rows, rng, level=1):
    """Place items using reverse-play construction.

    ALL containers (including locked) participate in the reverse-play loop.
    For each triple: pick a random eligible container, push existing items
    deeper if needed, place triple at front, scatter 1-3 items to other
    containers' empty front slots.

    Locked containers have a cutoff: during the last `unlock_matches_required`
    triples of reverse construction, no items can go into or out of a locked
    container. Those last triples correspond to the first forward-play triples
    that unlock the container.

    Returns (total_triples_placed, construction_move_count).
    construction_move_count == forward optimal move count.
    """
    unlocked = [c for c in containers if not c["is_locked"]]
    locked = [c for c in containers if c["is_locked"]]

    if not unlocked and not locked:
        return 0, 0

    # Per-container row limits and grid
    c_rows = {}
    grid = {}
    for c in containers:
        mr = c.get("max_rows_per_slot", max_rows)
        c_rows[c["id"]] = mr
        grid[c["id"]] = [[None] * mr for _ in range(c["slot_count"])]

    all_triples = list(item_ids)
    # NOTE: Do NOT shuffle here — caller controls placement order
    # (duplicates first = buried deep, originals later = near surface)
    total_planned = len(all_triples)

    # Per locked container: after this many triples placed (in reverse order),
    # the container becomes off-limits for the remaining triples.
    # Those remaining triples are the first ones solved in forward play,
    # which are needed to unlock the container.
    lock_cutoffs = {}
    for lc in locked:
        unlock_req = lc.get("unlock_matches_required", 1)
        lock_cutoffs[lc["id"]] = total_planned - unlock_req

    # Identify off-screen containers (top edge above HUD bar).
    # Items can be placed IN these (they become accessible after cascade)
    # but scattered items should NOT go TO them (players can't reach them).
    off_screen_ids = set()
    for c in containers:
        cy = c["position"]["y"]
        mr = c.get("max_rows_per_slot", max_rows)
        half_h = _container_half_height(mr)
        # In Godot Y coords: smaller Y = higher on screen
        # Container's top edge (smallest Y) is above HUD bar bottom
        top_edge = cy - half_h
        if top_edge < HUD_BAR_BOTTOM_Y:
            off_screen_ids.add(c["id"])

    # ── Helpers ────────────────────────────────────────────────────────

    def is_active(c, triples_placed):
        """Check if a container can participate at this point in construction."""
        if not c["is_locked"]:
            return True
        return triples_placed < lock_cutoffs.get(c["id"], 0)

    def get_active_containers(triples_placed):
        """Get all containers that can participate right now."""
        return [c for c in containers if is_active(c, triples_placed)]

    def front_empty(cid, slots):
        """Check if all front-row slots are empty."""
        return all(grid[cid][s][0] is None for s in range(slots))

    def can_push(cid, slots, mr):
        """Check if items can be pushed one row deeper."""
        if mr <= 1:
            return False
        return all(grid[cid][s][mr - 1] is None for s in range(slots))

    def push_deeper(cid, slots, mr):
        """Shift all items one row deeper in this container."""
        for s in range(slots):
            for r in range(mr - 1, 0, -1):
                grid[cid][s][r] = grid[cid][s][r - 1]
            grid[cid][s][0] = None

    def get_scatter_dests(exclude_cid, triples_placed):
        """Get empty front slots in other active on-screen containers.
        Excludes off-screen containers (players can't reach them)."""
        dests = []
        for c in get_active_containers(triples_placed):
            if c["id"] == exclude_cid:
                continue
            if c["id"] in off_screen_ids:
                continue  # Don't scatter to off-screen containers
            for s in range(c["slot_count"]):
                if grid[c["id"]][s][0] is None:
                    dests.append((c["id"], s))
        return dests

    # ── Place each triple ──────────────────────────────────────────────
    total_triples = 0
    total_moves = 0

    # Track empty front slots across active unlocked containers
    empty_front_count = sum(
        1 for c in unlocked
        for s in range(c["slot_count"])
        if grid[c["id"]][s][0] is None
    )
    MIN_EMPTY_FRONT = 3  # Always keep at least 3 empty front slots

    for item_id in all_triples:
        active = get_active_containers(total_triples)

        # Find eligible containers: 3-slot, active, front empty or can push
        eligible = []
        for c in active:
            if c["slot_count"] < 3:
                continue
            cid = c["id"]
            mr = c_rows[cid]
            if front_empty(cid, c["slot_count"]):
                eligible.append(c)
            elif can_push(cid, c["slot_count"], mr):
                eligible.append(c)

        if not eligible:
            break  # No room to host any more triples

        # Prefer containers that need pushing (preserves empty front slots).
        need_push = [c for c in eligible
                     if not front_empty(c["id"], c["slot_count"])]
        if need_push and (empty_front_count <= MIN_EMPTY_FRONT + 3
                          or rng.random() < 0.7):
            target = rng.choice(need_push)
        else:
            target = rng.choice(eligible)

        cid = target["id"]
        mr = c_rows[cid]

        # Push existing items deeper if front isn't empty
        host_front_items = sum(
            1 for s in range(target["slot_count"])
            if grid[cid][s][0] is not None
        )
        if host_front_items > 0:
            push_deeper(cid, target["slot_count"], mr)
            if not target["is_locked"]:
                empty_front_count += host_front_items

        # Place triple at front row
        for s in range(min(3, target["slot_count"])):
            grid[cid][s][0] = item_id
        if not target["is_locked"]:
            empty_front_count -= 3

        # Find scatter destinations (empty front slots in other active containers)
        dests = get_scatter_dests(cid, total_triples)

        if not dests:
            # No scatter targets - triple stays complete (instant match)
            total_triples += 1
            continue

        # Scatter 1-3 items with weighted distribution to reduce leftover pairs:
        # 10% scatter 1 (leaves pair), 50% scatter 2, 40% scatter 3
        max_scatter = min(3, len(dests))
        roll = rng.random()
        if max_scatter == 1:
            n_scatter = 1
        elif max_scatter == 2:
            n_scatter = 1 if roll < 0.10 else 2
        else:
            n_scatter = 1 if roll < 0.10 else (2 if roll < 0.60 else 3)

        rng.shuffle(dests)

        # Pick which slots to scatter from (random subset)
        scatter_slots = list(range(min(3, target["slot_count"])))
        rng.shuffle(scatter_slots)
        scatter_slots = scatter_slots[:n_scatter]

        for i, slot in enumerate(scatter_slots):
            dest_cid, dest_slot = dests[i]
            grid[dest_cid][dest_slot][0] = item_id
            grid[cid][slot][0] = None

        total_triples += 1
        total_moves += n_scatter

        # ── Variance move: every 3 triples, move an existing front item ──
        if total_triples % 3 == 0:
            active_now = get_active_containers(total_triples)
            sources = []
            for c in active_now:
                if c["id"] in off_screen_ids:
                    continue  # Don't move from off-screen
                for s in range(c["slot_count"]):
                    if grid[c["id"]][s][0] is not None:
                        sources.append((c["id"], s))
            if sources:
                rng.shuffle(sources)
                for src_cid, src_s in sources:
                    var_dests = [
                        (c["id"], s) for c in active_now
                        if c["id"] != src_cid and c["id"] not in off_screen_ids
                        for s in range(c["slot_count"])
                        if grid[c["id"]][s][0] is None
                    ]
                    if var_dests:
                        dst_cid, dst_s = rng.choice(var_dests)
                        grid[dst_cid][dst_s][0] = grid[src_cid][src_s][0]
                        grid[src_cid][src_s][0] = None
                        total_moves += 1
                        break

    # ── Ensure playable board (enough empty front slots) ───────────────
    # Only count unlocked containers for empty front slots (locked ones
    # aren't accessible at game start)
    empty_front_count = sum(
        1 for c in unlocked
        for s in range(c["slot_count"])
        if grid[c["id"]][s][0] is None
    )
    if empty_front_count < MIN_EMPTY_FRONT:
        for c in unlocked:
            if empty_front_count >= MIN_EMPTY_FRONT:
                break
            cid = c["id"]
            mr = c_rows[cid]
            if mr <= 1:
                continue
            for s in range(c["slot_count"]):
                if empty_front_count >= MIN_EMPTY_FRONT:
                    break
                if grid[cid][s][0] is None:
                    continue  # Already empty
                for r in range(1, mr):
                    if grid[cid][s][r] is None:
                        grid[cid][s][r] = grid[cid][s][0]
                        grid[cid][s][0] = None
                        empty_front_count += 1
                        break

    # ── Post-processing ────────────────────────────────────────────────
    # Fix starting triples and empty containers across ALL containers
    all_playable = unlocked + locked
    _fix_starting_triples(grid, all_playable, rng, c_rows)
    _ensure_no_empty_containers(grid, all_playable, rng, c_rows)
    _ensure_singleslot_depth(grid, all_playable, rng, c_rows)

    # ── Convert grid → initial_items ───────────────────────────────────
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

    return total_triples, total_moves


def _fix_starting_triples(grid, pool, rng, c_rows):
    """If any 3-slot container has 3 matching items in ANY row, swap one out.
    Checks all rows (not just front) to prevent hidden triples that auto-match
    when row advancement occurs. Loops until none remain (max 50 iterations)."""
    for _ in range(50):
        found = False
        for c in pool:
            cid = c["id"]
            if c["slot_count"] < 3:
                continue
            mr = c_rows[cid]
            for r in range(mr):
                row_items = [grid[cid][s][r] for s in range(c["slot_count"])]
                if any(item is None for item in row_items):
                    continue
                if len(set(row_items)) != 1:
                    continue

                # Triple found at row r — swap slot 0 at this row
                found = True
                target_item = row_items[0]
                swapped = False
                candidates = list(pool)
                rng.shuffle(candidates)
                for other in candidates:
                    if other["id"] == cid:
                        continue
                    o_mr = c_rows[other["id"]]
                    # Swap with same row in other container if possible
                    swap_row = r if r < o_mr else 0
                    for os_idx in range(other["slot_count"]):
                        oi = grid[other["id"]][os_idx][swap_row]
                        if oi is not None and oi != target_item:
                            grid[cid][0][r], grid[other["id"]][os_idx][swap_row] = oi, target_item
                            swapped = True
                            break
                    if swapped:
                        break
                if found:
                    break  # restart scan after a fix
            if found:
                break
        if not found:
            break


def _ensure_no_empty_containers(grid, pool, rng, c_rows):
    """Move an item to any unlocked container that ended up empty."""
    for c in pool:
        cid = c["id"]
        mr = c_rows[cid]
        has_any = any(grid[cid][s][r] is not None
                      for s in range(c["slot_count"]) for r in range(mr))
        if has_any:
            continue

        # Steal one front item from the fullest container
        fullest = max(pool, key=lambda x: sum(
            1 for s in range(x["slot_count"])
            for r in range(c_rows[x["id"]])
            if grid[x["id"]][s][r] is not None
        ))
        if fullest["id"] == cid:
            continue
        for s in range(fullest["slot_count"]):
            if grid[fullest["id"]][s][0] is not None:
                item = grid[fullest["id"]][s][0]
                grid[fullest["id"]][s][0] = None
                grid[cid][0][0] = item
                break


def _ensure_singleslot_depth(grid, containers, rng, c_rows):
    """Ensure single-slot containers with max_rows >= 2 have depth.

    For each single-slot container with room for back rows, ensure at least
    1 item is in a back row (so the player must clear front to reveal it).
    Steals items from the fullest other unlocked container if needed.
    """
    unlocked = [c for c in containers if not c["is_locked"]]

    for c in containers:
        if c["slot_count"] != 1:
            continue
        cid = c["id"]
        mr = c_rows[cid]
        if mr < 2:
            continue

        # Count items in this container
        items_here = sum(1 for r in range(mr) if grid[cid][0][r] is not None)
        back_items = sum(1 for r in range(1, mr) if grid[cid][0][r] is not None)

        if items_here >= 2 and back_items >= 1:
            continue  # Already has depth

        # Need at least 2 items total with at least 1 in back row
        if items_here < 2:
            # Steal items from fullest unlocked container
            donors = sorted(unlocked, key=lambda x: sum(
                1 for s in range(x["slot_count"])
                for r in range(c_rows[x["id"]])
                if grid[x["id"]][s][r] is not None
            ), reverse=True)

            for donor in donors:
                if donor["id"] == cid:
                    continue
                did = donor["id"]
                for s in range(donor["slot_count"]):
                    if grid[did][s][0] is not None and items_here < 2:
                        item = grid[did][s][0]
                        grid[did][s][0] = None
                        # Place in first empty row
                        for r in range(mr):
                            if grid[cid][0][r] is None:
                                grid[cid][0][r] = item
                                items_here += 1
                                break
                if items_here >= 2:
                    break

        # Ensure at least 1 item is in a back row
        back_items = sum(1 for r in range(1, mr) if grid[cid][0][r] is not None)
        if back_items == 0 and items_here >= 2:
            # Move front item to row 1
            if grid[cid][0][0] is not None and grid[cid][0][1] is None:
                grid[cid][0][1] = grid[cid][0][0]
                grid[cid][0][0] = None


# ── Capacity Calculation ─────────────────────────────────────────────────────

def get_max_triples(containers, max_rows):
    """Max triples placeable. Total slot capacity / 3."""
    total_slots = sum(c["slot_count"] * c.get("max_rows_per_slot", max_rows)
                      for c in containers)
    return total_slots // 3


# ── Level Generation ─────────────────────────────────────────────────────────

def generate_level(level, config, item_usage, seed_offset=0):
    """Generate a single level using reverse-play construction."""
    spec = get_level_spec(level)
    if seed_offset > 0:
        spec["rng"] = random.Random(level * 42 + 7 + seed_offset)
    rng = spec["rng"]
    containers = build_containers(spec, config)

    # Level 1: hardcoded 2-move tutorial
    if level == 1:
        available = get_available_items(config, level)
        selected = select_items(rng, available, 2, item_usage)
        a, b = selected[0], selected[1]
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
            "containers": containers, "moving_tracks": [],
        }

    # Determine item count
    total_capacity = sum(c["slot_count"] * c["max_rows_per_slot"] for c in containers)
    max_placeable = get_max_triples(containers, spec["max_rows"])

    target_fill = get_target_fill_ratio(level)
    target_triples = max(2, math.ceil(total_capacity * target_fill / 3))
    variety_types = get_target_types(level)

    available = get_available_items(config, level)
    # Number of unique item types (capped by available pool, usually 50)
    n_unique = max(target_triples, variety_types)
    n_unique = min(n_unique, max_placeable, len(available))
    n_unique = max(2, n_unique)

    selected = select_items(rng, available, n_unique, item_usage)

    # If target requires more triples than unique types, add duplicate triples.
    # All 50 unique items are used first; duplicates are only added beyond that.
    # Duplicates are placed FIRST in reverse construction so they end up buried
    # deep beneath their originals — preventing easy surface-level double matches.
    n_total_triples = min(target_triples, max_placeable)
    duplicates = []
    if n_total_triples > len(selected):
        extra = n_total_triples - len(selected)
        duplicates = [rng.choice(selected) for _ in range(extra)]

    # Build placement order: duplicates first (buried deep), then unique items
    # (near surface). Within each group, shuffle for variety.
    rng.shuffle(duplicates)
    rng.shuffle(selected)
    placement_order = duplicates + selected

    # Place items via reverse construction
    total_triples, construction_moves = reverse_place_items(
        containers, placement_order, spec["max_rows"], rng, level)

    n_items = sum(len(c["initial_items"]) for c in containers)
    timer = calc_timer(level, n_items)

    # Star thresholds from exact construction move count
    optimal = max(2, construction_moves)
    t3 = optimal
    t2 = max(t3 + 1, round(optimal * 1.15))
    t1 = max(t2 + 1, round(optimal * 1.30))
    fail = max(t1 + 1, round(optimal * 1.40))

    return {
        "id": level, "world_id": config.world_id, "name": f"level_{level:03d}",
        "star_move_thresholds": [t3, t2, t1, fail],
        "time_limit_seconds": timer,
        "containers": containers, "moving_tracks": [],
        "construction_moves": construction_moves,
    }


# ── Compact JSON Serialization ────────────────────────────────────────────────

# Default values for container fields — omit from JSON if field matches default.
_CONTAINER_DEFAULTS = {
    "is_locked": False,
    "unlock_matches_required": 0,
    "lock_overlay_image": "",
    "unlock_animation": "",
    "is_moving": False,
    "move_type": "",
    "move_direction": "",
    "move_speed": 50.0,
    "move_distance": 200.0,
    "track_id": "",
    "is_falling": False,
    "fall_speed": 100.0,
    "fall_target_y": 0.0,
    "despawn_on_match": False,
}

# Fields that should always be removed (never read by game code)
_ALWAYS_REMOVE = {"unlock_animation"}


def _compact_container(container):
    """Strip default-value fields and round positions for compact JSON."""
    result = {}
    for key, value in container.items():
        if key in _ALWAYS_REMOVE:
            continue
        if key in _CONTAINER_DEFAULTS and value == _CONTAINER_DEFAULTS[key]:
            continue
        if key == "position" and isinstance(value, dict):
            result[key] = {
                "x": round(value["x"], 1),
                "y": round(value["y"], 1),
            }
            continue
        result[key] = value
    return result


def _compact_level(level_data):
    """Produce a compact version of level data for JSON serialization."""
    result = {}
    for key, value in level_data.items():
        if key == "moving_tracks" and (not value or value == []):
            continue
        if key == "containers" and isinstance(value, list):
            result[key] = [_compact_container(c) for c in value]
            continue
        result[key] = value
    return result


# ── Batch Generation ─────────────────────────────────────────────────────────

def generate_levels(config, output_dir, count=100):
    """Generate all levels for a world using reverse-play construction."""
    os.makedirs(output_dir, exist_ok=True)

    # Delete existing levels
    for f in os.listdir(output_dir):
        if f.startswith("level_") and f.endswith(".json"):
            os.remove(os.path.join(output_dir, f))

    all_items = config.all_items
    item_usage = {item: 0 for item in all_items}

    print(f"\nGenerating {count} {config.world_id.title()} levels "
          f"(reverse-play V2) to: {output_dir}\n")

    stats = []
    errors = []
    mechanic_histogram = {}
    move_comparisons = []  # (level, construction_moves, solver_moves)

    MAX_ATTEMPTS = 20

    for level in range(1, count + 1):
        # Try generating with solver verification; retry with different seeds
        best_data = None
        best_moves = None
        for attempt in range(MAX_ATTEMPTS):
            saved_usage = dict(item_usage)
            level_data = generate_level(level, config, item_usage,
                                        seed_offset=attempt * 1000)
            # Use ensemble solver for best possible move count
            result = solve_level_best(level_data)
            if result.success:
                best_data = level_data
                best_moves = result.total_moves
                break
            # Restore item usage for retry
            item_usage.update(saved_usage)

        if best_data is None:
            # All attempts failed — use last generated level
            best_data = level_data
            errors.append(f"L{level}: Solver failed after {MAX_ATTEMPTS} attempts")
        else:
            # Update thresholds using ensemble solver's best move count
            optimal = max(2, best_moves)
            t3 = optimal
            t2 = max(t3 + 1, round(optimal * 1.15))
            t1 = max(t2 + 1, round(optimal * 1.30))
            fail = max(t1 + 1, round(optimal * 1.40))
            best_data["star_move_thresholds"] = [t3, t2, t1, fail]

        level_data = best_data
        filepath = os.path.join(output_dir, f"level_{level:03d}.json")
        with open(filepath, "w", newline="\n") as f_out:
            compact = _compact_level(level_data)
            json.dump(compact, f_out, separators=(",", ":"))

        n_items = sum(len(c["initial_items"]) for c in level_data["containers"])
        n_containers = len(level_data["containers"])
        thresh = level_data["star_move_thresholds"]
        timer = level_data["time_limit_seconds"]
        n_types = n_items // 3 if n_items > 0 else 0

        # Validation
        if n_items % 3 != 0:
            errors.append(f"L{level}: {n_items} items (NOT multiple of 3!)")

        # Collect bounding boxes for overlap/collision checks
        static_boxes = []  # (box, container_id)
        bf_boxes = []      # (travel_box, container_id)

        for c in level_data["containers"]:
            if c["slot_count"] >= 3:
                max_row = c["max_rows_per_slot"]
                for row in range(max_row):
                    row_items = [None] * c["slot_count"]
                    for item in c["initial_items"]:
                        if item["row"] == row:
                            row_items[item["slot"]] = item["id"]
                    if (all(ri is not None for ri in row_items)
                            and len(set(row_items)) == 1):
                        errors.append(
                            f"L{level}: {c['id']} has triple at row {row}!")

            cx, cy = c["position"]["x"], c["position"]["y"]
            mr = c["max_rows_per_slot"]

            if not c["is_moving"] and not c["despawn_on_match"]:
                # Full AABB screen bounds check against actual screen pixels
                # Screen is 1080x1920; allow 20px margin outside actual edges
                box = _get_bounding_box(cx, cy, c["slot_count"], mr)
                x_min, x_max, y_min, y_max = box
                if (x_min < -20 or x_max > 1100 or
                        y_min < -20 or y_max > 1700):
                    errors.append(
                        f"L{level}: {c['id']} bbox ({x_min:.0f}-{x_max:.0f}, "
                        f"{y_min:.0f}-{y_max:.0f}) off-screen")
                static_boxes.append((box, c["id"]))

            if c["is_moving"] and c["move_type"] == "back_and_forth":
                tbox = _get_travel_box(cx, cy, c["slot_count"], mr,
                                       c["move_direction"], c["move_distance"])
                bf_boxes.append((tbox, c["id"]))

        # Static overlap detection (allow up to 10px edge overlap since grid
        # positions naturally have containers touching at edges)
        for i in range(len(static_boxes)):
            for j in range(i + 1, len(static_boxes)):
                box_a, id_a = static_boxes[i]
                box_b, id_b = static_boxes[j]
                if _boxes_overlap(box_a, box_b, gap=-10):
                    errors.append(
                        f"L{level}: {id_a} and {id_b} overlap!")

        # B&F path collision with static containers
        for tbox, bf_id in bf_boxes:
            for sbox, s_id in static_boxes:
                if s_id == bf_id:
                    continue
                if _boxes_overlap(tbox, sbox, gap=-10):
                    errors.append(
                        f"L{level}: B&F {bf_id} sweep collides with {s_id}")

        total_cap = sum(c["slot_count"] * c["max_rows_per_slot"]
                        for c in level_data["containers"])
        fill_pct = round(100 * n_items / total_cap) if total_cap > 0 else 0
        n_empty = sum(1 for c in level_data["containers"]
                      if len(c["initial_items"]) == 0)
        if n_empty > 0:
            errors.append(f"L{level}: {n_empty} empty container(s)")

        mechanics = set()
        max_r = 1
        for c in level_data["containers"]:
            if c["is_moving"] and c["move_type"] == "carousel":
                mechanics.add("carousel")
            if c["is_moving"] and c["move_type"] == "back_and_forth":
                mechanics.add("b&f")
            if c["is_locked"]:
                mechanics.add("locked")
            if c["despawn_on_match"]:
                mechanics.add("despawn")
            if c["slot_count"] == 1:
                mechanics.add("single")
            max_r = max(max_r, c["max_rows_per_slot"])

        # Track mechanic usage for histogram
        for m in mechanics:
            mechanic_histogram[m] = mechanic_histogram.get(m, 0) + 1

        attempt_str = f" (attempt {attempt + 1})" if attempt > 0 else ""
        solver_str = f", solver={best_moves}m" if best_moves else ", UNSOLVED"
        construction_moves = level_data.get("construction_moves", 0)
        cmoves_str = f", construct={construction_moves}m" if construction_moves else ""
        stat = (f"L{level:3d}: {n_containers:2d}c, {n_types:2d}t, "
                f"{n_items:3d}i, {max_r}r, {fill_pct:3d}% fill, "
                f"thresh={thresh}, timer={timer}s{solver_str}{cmoves_str}{attempt_str}")
        if mechanics:
            stat += f", [{', '.join(sorted(mechanics))}]"
        stats.append(stat)
        print(stat)

        # Track construction vs solver comparison
        if construction_moves and best_moves:
            move_comparisons.append((level, construction_moves, best_moves))

    # Summary
    print(f"\n{'=' * 60}")
    min_used = min(item_usage.values())
    max_used = max(item_usage.values())
    unused = [item for item in all_items if item_usage[item] == 0]
    print(f"Item usage: min={min_used}, max={max_used}")
    if unused:
        print(f"UNUSED: {unused}")
        errors.append(f"Unused items: {unused}")
    else:
        print(f"All {len(all_items)} items used!")

    # Mechanic histogram
    print(f"\nMechanic histogram (levels using each):")
    for mech in sorted(mechanic_histogram.keys()):
        print(f"  {mech}: {mechanic_histogram[mech]}/{count}")

    # Construction vs Solver comparison
    if move_comparisons:
        print(f"\nConstruction vs Solver moves:")
        print(f"  {'Level':>5}  {'Construct':>9}  {'Solver':>6}  {'Diff':>5}  {'%':>6}")
        print(f"  {'-'*5}  {'-'*9}  {'-'*6}  {'-'*5}  {'-'*6}")
        total_construct = 0
        total_solver = 0
        solver_better = 0
        solver_worse = 0
        solver_equal = 0
        for lvl, cm, sm in move_comparisons:
            diff = sm - cm
            pct = (diff / cm * 100) if cm > 0 else 0
            print(f"  L{lvl:>3}  {cm:>9}  {sm:>6}  {diff:>+5}  {pct:>+5.1f}%")
            total_construct += cm
            total_solver += sm
            if sm < cm:
                solver_better += 1
            elif sm > cm:
                solver_worse += 1
            else:
                solver_equal += 1
        print(f"\n  Summary: solver better={solver_better}, equal={solver_equal}, worse={solver_worse}")
        avg_diff = (total_solver - total_construct) / len(move_comparisons)
        print(f"  Totals: construct={total_construct}, solver={total_solver}, "
              f"avg diff={avg_diff:+.1f} moves/level")

    if errors:
        print(f"\nERRORS ({len(errors)}):")
        for e in errors:
            print(f"  {e}")
    else:
        print("\nNo errors!")

    print(f"\nDone! Generated {count} levels.")

    # Summary file
    summary_path = os.path.join(output_dir, "..",
                                f"{config.world_id}_levels_summary.txt")
    with open(summary_path, "w") as f_out:
        f_out.write(f"{config.world_id.title()} Levels Summary\n")
        f_out.write("=" * 80 + "\n\n")
        for s in stats:
            f_out.write(s + "\n")
        f_out.write(f"\nItem usage: min={min_used}, max={max_used}\n")
        if unused:
            f_out.write(f"UNUSED: {unused}\n")
        f_out.write(f"\nMechanic histogram:\n")
        for mech in sorted(mechanic_histogram.keys()):
            f_out.write(f"  {mech}: {mechanic_histogram[mech]}/{count}\n")
        if errors:
            f_out.write(f"\nErrors:\n")
            for e in errors:
                f_out.write(f"  {e}\n")

    return errors
