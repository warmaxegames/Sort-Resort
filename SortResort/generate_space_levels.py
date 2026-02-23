#!/usr/bin/env python3
"""
Space Level Generator for Sort Resort
Uses the core level_generator module with Space-specific item configuration.

50 item types, complexity_offset=10 (L1 plays like Island L11).
Mechanics unlock gradually across L1-L26, all unlocked by L31+.
Run: python generate_space_levels.py [start_level end_level]
"""

import os
import sys
from level_generator import WorldConfig
from reverse_generator import generate_levels

# ── Space World Configuration ─────────────────────────────────────────────────
# 50 items grouped by category, unlocking progressively through L1-L20.
# Items unlock progressively L1-L20. complexity_offset=10 gives a gradual difficulty ramp.
# Each tuple: (unlock_level, [item_ids])

SPACE_CONFIG = WorldConfig(
    world_id="space",
    complexity_offset=10,  # L1 plays like Island L11
    item_groups=[
        # Wave 1: L1 - planets & celestial bodies (15 items)
        (1,  ["earth", "mars", "jupiter"]),
        (1,  ["saturn", "venus", "mercury"]),
        (1,  ["neptune", "uranus", "pluto"]),
        (1,  ["moon", "sun", "star"]),
        (1,  ["asteroid", "comet", "meteor"]),
        # Wave 2: L5 - rockets & aliens (9 items)
        (5,  ["blue_rocket", "rocket_pink", "rocket_red"]),
        (5,  ["rocket_yellow", "rocket1", "rocket3"]),
        (5,  ["alien", "alien_2", "black_hole"]),
        # Wave 3: L10 - weapons, UFOs & robots (9 items)
        (10, ["laser_gun_1", "laser_gun_2", "laser_gun_3"]),
        (10, ["laser_gun_4", "ufo_1", "ufo_2"]),
        (10, ["robot_1", "robot_2", "drone"]),
        # Wave 4: L15 - satellites, tech & equipment (11 items)
        (15, ["big_antenna", "radar_monitor", "satellite"]),
        (15, ["solar_satellite", "sputnik", "astronaut"]),
        (15, ["hologram", "jetpack", "module"]),
        (15, ["space_helmet", "land_drone"]),
        # Wave 5: L20 - misc & toys (6 items)
        (20, ["alien_ship_toy", "leika_toy", "rover"]),
        (20, ["smoothie", "telecope", "water"]),
    ],
)

assert len(SPACE_CONFIG.all_items) == 50, \
    f"Expected 50 items, got {len(SPACE_CONFIG.all_items)}"


if __name__ == "__main__":
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "Assets", "_Project", "Resources", "Data", "Levels", "Space")

    # Support range args: python generate_space_levels.py 1 10
    if len(sys.argv) == 3:
        start = int(sys.argv[1])
        end = int(sys.argv[2])
        generate_levels(SPACE_CONFIG, output_dir, start_level=start, end_level=end)
    else:
        generate_levels(SPACE_CONFIG, output_dir)
