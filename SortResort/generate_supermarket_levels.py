#!/usr/bin/env python3
"""
Supermarket Level Generator for Sort Resort
Uses the core level_generator module with Supermarket-specific item configuration.

51 item types, complexity_offset=10 (L1 plays like Island L11).
Mechanics unlock gradually across L1-L26, all unlocked by L31+.
Run: python generate_supermarket_levels.py [start_level end_level]
"""

import os
import sys
from level_generator import WorldConfig
from reverse_generator import generate_levels

# ── Supermarket World Configuration ──────────────────────────────────────────
# 51 items grouped by product category, unlocking progressively through L1-L20.
# Items unlock progressively L1-L20. complexity_offset=10 gives a gradual difficulty ramp.
# Each tuple: (unlock_level, [item_ids])

SUPERMARKET_CONFIG = WorldConfig(
    world_id="supermarket",
    complexity_offset=10,  # L1 plays like Island L11
    item_groups=[
        # Wave 1: L1 - starter items (15 items)
        (1,  ["milk", "milkbottle", "orangejuice"]),
        (1,  ["soda", "grapesoda", "energydrink"]),
        (1,  ["ketchup", "mustard", "mayo"]),
        (1,  ["cider", "lemonjuice", "carrotjuice"]),
        (1,  ["hotsauce", "garlicsauce", "cannedcorn"]),
        # Wave 2: L5 - canned & jars (9 items)
        (5,  ["cannedtomato", "peas", "cannedbeans"]),
        (5,  ["pickles", "olivejar", "oliveoil"]),
        (5,  ["greenapplejuice", "redapplejuice", "peachjuice"]),
        # Wave 3: L10 - snacks & spreads (9 items)
        (10, ["strawberryjuice", "vinegar", "chocolatebar"]),
        (10, ["chips", "popcornbucket", "peanutbutter"]),
        (10, ["nutbar", "raspberryjam", "strawberryjam"]),
        # Wave 4: L15 - yogurts, water, thermos (12 items)
        (15, ["honey", "bananayogurt", "coconutyogurt"]),
        (15, ["strawberryyogurt", "waterbottle", "greenwaterbottle"]),
        (15, ["orangewaterbottle", "coffeebeans", "bluethermos"]),
        (15, ["pinkthermos", "greenthermosstraw", "pinkthermosstraw"]),
        # Wave 5: L20 - final items (6 items)
        (20, ["togocoffeeblue", "togocoffeebrown", "togocoffeegreen"]),
        (20, ["eggs", "pinkflower", "whiteflower"]),
    ],
)

assert len(SUPERMARKET_CONFIG.all_items) == 51, \
    f"Expected 51 items, got {len(SUPERMARKET_CONFIG.all_items)}"


if __name__ == "__main__":
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "Assets", "_Project", "Resources", "Data", "Levels", "Supermarket")

    # Support range args: python generate_supermarket_levels.py 1 10
    if len(sys.argv) == 3:
        start = int(sys.argv[1])
        end = int(sys.argv[2])
        generate_levels(SUPERMARKET_CONFIG, output_dir, start_level=start, end_level=end)
    else:
        generate_levels(SUPERMARKET_CONFIG, output_dir)
