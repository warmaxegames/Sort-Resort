#!/usr/bin/env python3
"""
Tavern Level Generator for Sort Resort
Uses the core level_generator module with Tavern-specific item configuration.

50 item types, complexity_offset=10 (L1 plays like Island L11).
Mechanics unlock gradually across L1-L26, all unlocked by L31+.
Run: python generate_tavern_levels.py [start_level end_level]
"""

import os
import sys
from level_generator import WorldConfig
from reverse_generator import generate_levels

# ── Tavern World Configuration ──────────────────────────────────────────
# 50 items grouped by tavern theme, unlocking progressively through L1-L20.
# Items unlock progressively L1-L20. complexity_offset=10 gives a gradual difficulty ramp.
# Each tuple: (unlock_level, [item_ids])

TAVERN_CONFIG = WorldConfig(
    world_id="tavern",
    complexity_offset=10,  # L1 plays like Island L11
    item_groups=[
        # Wave 1: L1 - common tavern items (15 items)
        (1,  ["ale_cup_1", "ale_cup_2", "ale_cup_3"]),
        (1,  ["barrel", "cheese", "ham"]),
        (1,  ["sausage", "pies", "candle"]),
        (1,  ["torch", "lamp", "wine_jar"]),
        (1,  ["coin_purse", "key", "soup_calderon"]),
        # Wave 2: L5 - more ale + potions (10 items)
        (5,  ["ale_cup_4", "ale_cup_5", "ale_cup_6", "ale_cup_7"]),
        (5,  ["blue_potion", "green_potion", "pink_potion"]),
        (5,  ["purple_potion", "yellow_potion", "ink"]),
        # Wave 3: L10 - weapons + armor (10 items)
        (10, ["sword", "axe", "bow"]),
        (10, ["mace", "mace_2", "helmet"]),
        (10, ["shield_1", "shield_2", "shield_3", "shield_4"]),
        # Wave 4: L15 - scrolls + regalia (10 items)
        (15, ["helmet_2", "scroll_earth", "scroll_fire"]),
        (15, ["scroll_water", "scroll_wind", "quivel"]),
        (15, ["horn", "cepter", "crown", "anvil"]),
        # Wave 5: L20 - music + tower (5 items)
        (20, ["flute", "harp", "lute", "trumpet", "tower"]),
    ],
)

assert len(TAVERN_CONFIG.all_items) == 50, \
    f"Expected 50 items, got {len(TAVERN_CONFIG.all_items)}"


if __name__ == "__main__":
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "Assets", "_Project", "Resources", "Data", "Levels", "Tavern")

    # Support range args: python generate_tavern_levels.py 1 10
    if len(sys.argv) == 3:
        start = int(sys.argv[1])
        end = int(sys.argv[2])
        generate_levels(TAVERN_CONFIG, output_dir, start_level=start, end_level=end)
    else:
        generate_levels(TAVERN_CONFIG, output_dir)
