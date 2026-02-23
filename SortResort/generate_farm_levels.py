#!/usr/bin/env python3
"""
Farm Level Generator for Sort Resort
Uses the core level_generator module with Farm-specific item configuration.

53 item types, complexity_offset=10 (L1 plays like Island L11).
Mechanics unlock gradually across L1-L26, all unlocked by L31+.
Run: python generate_farm_levels.py [start_level end_level]
"""

import os
import sys
from level_generator import WorldConfig
from reverse_generator import generate_levels

# ── Farm World Configuration ─────────────────────────────────────────────────
# 53 items grouped by category, unlocking progressively through L1-L20.
# Items unlock progressively L1-L20. complexity_offset=10 gives a gradual difficulty ramp.
# Each tuple: (unlock_level, [item_ids])

FARM_CONFIG = WorldConfig(
    world_id="farm",
    complexity_offset=10,  # L1 plays like Island L11
    item_groups=[
        # Wave 1: L1 - core vegetables (15 items)
        (1,  ["carrot", "potato", "corncob"]),
        (1,  ["lettuce", "cabbage", "broccoli"]),
        (1,  ["cauliflower", "cucumber", "redtomato"]),
        (1,  ["greentomato", "eggplant", "mushroom"]),
        (1,  ["bellpeppergreen", "bellpepperred", "bellpepperyellow"]),
        # Wave 2: L5 - root veggies & greens (9 items)
        (5,  ["beet", "radish", "turnip"]),
        (5,  ["turnip2", "whiteradish", "celery"]),
        (5,  ["celery2", "romaine", "leek"]),
        # Wave 3: L10 - fruits (9 items)
        (10, ["greenapple", "redapple", "lemon"]),
        (10, ["lime", "pear", "strawberry"]),
        (10, ["raspberry", "mandarin", "avocado"]),
        # Wave 4: L15 - spices, remaining produce & animals (12 items)
        (15, ["garlic", "ginger", "jalapeno"]),
        (15, ["redpepper", "whiteonion", "peapod"]),
        (15, ["watermelon", "persimmon", "chicken"]),
        (15, ["chickinegg", "rooster", "rabbit"]),
        # Wave 5: L20 - farm objects & special items (8 items)
        (20, ["sheep", "pumpkin", "tallpumpkin"]),
        (20, ["sunflower", "haystack", "pitchfork"]),
        (20, ["brittytoy", "wilsontoy"]),
    ],
)

assert len(FARM_CONFIG.all_items) == 53, \
    f"Expected 53 items, got {len(FARM_CONFIG.all_items)}"


if __name__ == "__main__":
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "Assets", "_Project", "Resources", "Data", "Levels", "Farm")

    # Support range args: python generate_farm_levels.py 1 10
    if len(sys.argv) == 3:
        start = int(sys.argv[1])
        end = int(sys.argv[2])
        generate_levels(FARM_CONFIG, output_dir, start_level=start, end_level=end)
    else:
        generate_levels(FARM_CONFIG, output_dir)
