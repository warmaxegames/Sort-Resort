#!/usr/bin/env python3
"""
Island Level Generator for Sort Resort
Uses the core level_generator module with Island-specific item configuration.

50 item types introduced gradually (all unlocked by L36).
Run: python generate_island_levels.py
"""

import os
from level_generator import WorldConfig
from reverse_generator import generate_levels

# ── Island World Configuration ───────────────────────────────────────────────
# 50 items grouped by visual family, unlocking progressively through L1-L36.
# Each tuple: (unlock_level, [item_ids])

ISLAND_CONFIG = WorldConfig(
    world_id="island",
    item_groups=[
        (1,  ["sandpale_red", "sandpale_green", "sandpale_blue"]),
        (1,  ["beachball_mixed", "beachball_redblue", "beachball_yellowblue"]),
        (1,  ["coconut", "greencoconut", "pineapple"]),
        (5,  ["margarita", "maitai", "strawberrydaiquiri"]),
        (7,  ["lemonade", "orangedrink", "watermelondrink"]),
        (9,  ["vanilladrink", "lemonadepitcher"]),
        (11, ["icecreamcone", "vanillaicecreamcone", "sparkleicecreamcone"]),
        (13, ["piratepopsicle", "icecreamparfait", "icecreamconechocolate", "popsiclemintchocolate"]),
        (16, ["sandals", "flipflops", "flipflopsgreen"]),
        (19, ["surfboardblueyellow", "surfboardbluepink", "surfboardpalmtrees"]),
        (22, ["surfboardbluewhitestripes", "surfboardredorange", "surfboardbluewhitepeace"]),
        (25, ["pinkpolkadotbikini", "bluesunglasses", "scubatanks", "swimmask"]),
        (28, ["sunscreen", "sunscreenspf50", "sunhat"]),
        (30, ["sunglassesorange", "sunglassespurple"]),
        (32, ["hibiscusflower", "watermelonslice", "messageinabottle"]),
        (34, ["suitcaseyellow", "lifepreserver", "beachbag"]),
        (36, ["oysterblue", "camera"]),
    ],
    # container_image defaults to "island_container"
    # single_slot_image defaults to "island_single_slot_container"
    # lock_overlay_image defaults to "island_lockoverlay"
)

assert len(ISLAND_CONFIG.all_items) == 50, \
    f"Expected 50 items, got {len(ISLAND_CONFIG.all_items)}"


if __name__ == "__main__":
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "Assets", "_Project", "Resources", "Data", "Levels", "Island")
    generate_levels(ISLAND_CONFIG, output_dir)
