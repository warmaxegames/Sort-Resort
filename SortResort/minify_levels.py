#!/usr/bin/env python3
"""
Sort Resort - Level JSON Minifier

Reads all level JSON files, strips unnecessary data, and writes back compact.
Optimizations:
  1. Remove whitespace (no pretty-printing)
  2. Remove unlock_animation field (never read by game code)
  3. Omit default-value fields from containers
  4. Round float positions to 1 decimal place
  5. Omit empty moving_tracks arrays

Run from project root:
  python minify_levels.py

This is safe to re-run; it's idempotent.
"""

import json
import os
import sys

# Default values for container fields — if a field matches its default, omit it.
CONTAINER_DEFAULTS = {
    "is_locked": False,
    "unlock_matches_required": 0,
    "lock_overlay_image": "",
    "unlock_animation": "",  # Always remove this field
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

# Fields that should ALWAYS be removed (never read by game code)
ALWAYS_REMOVE = {"unlock_animation"}


def round_position(pos):
    """Round position floats to 1 decimal place."""
    return {
        "x": round(pos["x"], 1),
        "y": round(pos["y"], 1),
    }


def minify_container(container):
    """Strip default-value fields and round positions."""
    result = {}

    for key, value in container.items():
        # Always remove these fields
        if key in ALWAYS_REMOVE:
            continue

        # Skip default-value fields
        if key in CONTAINER_DEFAULTS and value == CONTAINER_DEFAULTS[key]:
            continue

        # Round position floats
        if key == "position" and isinstance(value, dict):
            result[key] = round_position(value)
            continue

        result[key] = value

    return result


def minify_level(level_data):
    """Minify a complete level data structure."""
    result = {}

    for key, value in level_data.items():
        # Omit empty moving_tracks
        if key == "moving_tracks" and (not value or value == []):
            continue

        # Minify each container
        if key == "containers" and isinstance(value, list):
            result[key] = [minify_container(c) for c in value]
            continue

        result[key] = value

    return result


def process_file(filepath):
    """Minify a single level JSON file. Returns (original_size, new_size)."""
    with open(filepath, "r") as f:
        original = f.read()

    original_size = len(original.encode("utf-8"))

    try:
        data = json.loads(original)
    except json.JSONDecodeError as e:
        print(f"  ERROR: Failed to parse {filepath}: {e}")
        return original_size, original_size

    minified = minify_level(data)
    compact = json.dumps(minified, separators=(",", ":"))

    with open(filepath, "w", newline="\n") as f:
        f.write(compact)

    new_size = len(compact.encode("utf-8"))
    return original_size, new_size


def main():
    # Find all level directories
    base_dir = os.path.join(
        os.path.dirname(os.path.abspath(__file__)),
        "Assets", "_Project", "Resources", "Data", "Levels"
    )

    if not os.path.isdir(base_dir):
        print(f"ERROR: Level directory not found: {base_dir}")
        sys.exit(1)

    total_original = 0
    total_new = 0
    file_count = 0

    worlds = sorted(os.listdir(base_dir))
    for world in worlds:
        world_dir = os.path.join(base_dir, world)
        if not os.path.isdir(world_dir):
            continue

        files = sorted(f for f in os.listdir(world_dir)
                       if f.startswith("level_") and f.endswith(".json"))

        if not files:
            continue

        world_original = 0
        world_new = 0

        print(f"\n{world}: {len(files)} levels")

        for filename in files:
            filepath = os.path.join(world_dir, filename)
            orig, new = process_file(filepath)
            world_original += orig
            world_new += new
            file_count += 1

        savings = world_original - world_new
        pct = (savings / world_original * 100) if world_original > 0 else 0
        print(f"  {world_original:,} -> {world_new:,} bytes "
              f"(saved {savings:,} bytes, {pct:.1f}%)")

        total_original += world_original
        total_new += world_new

    if file_count == 0:
        print("No level files found!")
        return

    total_savings = total_original - total_new
    total_pct = (total_savings / total_original * 100) if total_original > 0 else 0

    print(f"\n{'=' * 50}")
    print(f"Total: {file_count} files")
    print(f"  {total_original:,} -> {total_new:,} bytes")
    print(f"  Saved {total_savings:,} bytes ({total_pct:.1f}%)")
    print(f"  {total_original / 1024 / 1024:.2f} MB -> {total_new / 1024 / 1024:.2f} MB")


if __name__ == "__main__":
    main()
