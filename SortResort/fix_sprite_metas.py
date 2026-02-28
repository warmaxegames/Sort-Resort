#!/usr/bin/env python3
"""
fix_sprite_metas.py - Batch fix corrupted Unity sprite .png.meta files.

Scans all .png.meta files in all 5 world sprite folders and:
- Detects items with >1 sub-sprite in the spriteSheet.sprites array
- Keeps only the LARGEST sub-sprite by area (width * height), removes tiny artifacts
- Renames the kept sub-sprite to {itemname}_0 for consistency
- Updates internalIDToNameTable to match (single entry)
- Updates nameFileIdTable to match (single entry)
- Normalizes spriteMode to 2 for all items
- Reports all changes made

Usage: python fix_sprite_metas.py
"""

import os
import re
import glob


ITEMS_ROOT = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "Assets", "_Project", "Resources", "Sprites", "Items"
)

WORLDS = ["Island", "Supermarket", "Farm", "Space", "Tavern"]


def parse_sprites(lines, sprites_start_idx):
    """Parse the sprites array from meta file lines starting at the '    sprites:' line.
    Returns list of dicts with: start_line, end_line, name, width, height, area, internalID, raw_lines.
    """
    sprites = []
    i = sprites_start_idx + 1  # skip the '    sprites:' line

    while i < len(lines):
        line = lines[i]
        # Each sprite entry starts with '    - serializedVersion: 2'
        if line.strip().startswith('- serializedVersion:') and '      ' not in line[:6]:
            sprite = {'start_line': i, 'raw_lines': []}
            sprite['raw_lines'].append(lines[i])
            i += 1

            # Read all lines of this sprite entry until next sprite or end of sprites array
            while i < len(lines):
                # Check if this is the start of a new sprite entry at the same indent level
                stripped = lines[i].strip()
                if stripped.startswith('- serializedVersion:') and lines[i].startswith('    - '):
                    break
                # Check if we've exited the sprites array (line at lower indent that's not a continuation)
                if not lines[i].startswith('      ') and not lines[i].startswith('    - '):
                    break
                sprite['raw_lines'].append(lines[i])
                i += 1

            sprite['end_line'] = i - 1

            # Extract name, rect dimensions, internalID from raw lines
            raw = '\n'.join(sprite['raw_lines'])

            name_match = re.search(r'name:\s+(\S+)', raw)
            sprite['name'] = name_match.group(1) if name_match else ''

            width_match = re.search(r'width:\s+(\d+)', raw)
            height_match = re.search(r'height:\s+(\d+)', raw)
            sprite['width'] = int(width_match.group(1)) if width_match else 0
            sprite['height'] = int(height_match.group(1)) if height_match else 0
            sprite['area'] = sprite['width'] * sprite['height']

            id_match = re.search(r'internalID:\s+(-?\d+)', raw)
            sprite['internalID'] = id_match.group(1) if id_match else '0'

            sprites.append(sprite)
        else:
            break

    return sprites


def fix_meta_file(meta_path):
    """Fix a single .png.meta file. Returns description of changes or None if no changes needed."""
    with open(meta_path, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.split('\n')

    # Find the sprites array
    sprites_start = None
    for i, line in enumerate(lines):
        if line.strip() == 'sprites:' and 'spriteSheet' not in line:
            # Make sure this is under spriteSheet (the one at indent 4)
            if line.startswith('    sprites:'):
                sprites_start = i
                break

    if sprites_start is None:
        return None

    sprites = parse_sprites(lines, sprites_start)

    if len(sprites) <= 1:
        # Only 1 or 0 sprites - check if spriteMode needs normalizing
        sprite_mode_changed = False
        for i, line in enumerate(lines):
            if line.strip().startswith('spriteMode:') and '  spriteMode:' in line:
                current_mode = line.strip().split(':')[1].strip()
                if current_mode != '2':
                    lines[i] = line.replace(f'spriteMode: {current_mode}', 'spriteMode: 2')
                    sprite_mode_changed = True
                break

        if sprite_mode_changed:
            with open(meta_path, 'w', encoding='utf-8', newline='\n') as f:
                f.write('\n'.join(lines))
            return f"  spriteMode normalized to 2 (was {current_mode}), 1 sprite (no artifact removal needed)"
        return None

    # Multiple sprites found - keep the largest by area
    item_name = os.path.splitext(os.path.splitext(os.path.basename(meta_path))[0])[0]

    # Find the positional end of all sprites BEFORE sorting
    last_sprite_end_line = max(s['end_line'] for s in sprites)

    sprites.sort(key=lambda s: s['area'], reverse=True)
    keeper = sprites[0]
    removed = sprites[1:]

    removed_info = ', '.join(
        f"{s['name']} ({s['width']}x{s['height']})" for s in removed
    )

    # The canonical name for the kept sprite
    canonical_name = f"{item_name}_0"

    # Build new sprite entry with canonical name
    new_sprite_lines = []
    for raw_line in keeper['raw_lines']:
        # Replace the name field
        if 'name:' in raw_line and keeper['name'] in raw_line:
            raw_line = raw_line.replace(keeper['name'], canonical_name)
        new_sprite_lines.append(raw_line)

    # Reconstruct the file
    # 1. Everything before sprites array
    new_lines = lines[:sprites_start + 1]

    # 2. The single kept sprite
    new_lines.extend(new_sprite_lines)

    # 3. Everything after the last sprite entry (outline:, customData:, etc. that belong to spriteSheet)
    last_sprite_end = last_sprite_end_line + 1
    # Find where spriteSheet's own fields continue after the sprites array
    new_lines.extend(lines[last_sprite_end:])

    # Now fix internalIDToNameTable - keep only the keeper's entry
    result_lines = []
    in_id_table = False
    id_table_done = False
    skip_until_next_section = False

    for i, line in enumerate(new_lines):
        if '  internalIDToNameTable:' in line and not line.strip().startswith('#'):
            in_id_table = True
            result_lines.append(line)
            # Write single entry
            result_lines.append('  - first:')
            result_lines.append(f'      213: {keeper["internalID"]}')
            result_lines.append(f'    second: {canonical_name}')
            skip_until_next_section = True
            continue

        if skip_until_next_section:
            # Skip old entries until we hit a line that's not part of the table
            if line.startswith('  - first:') or line.startswith('      213:') or line.startswith('    second:'):
                continue
            else:
                skip_until_next_section = False
                # Fall through to add this line

        result_lines.append(line)

    # Fix nameFileIdTable - keep only the keeper's entry
    final_lines = []
    in_name_table = False

    for i, line in enumerate(result_lines):
        if '    nameFileIdTable:' in line:
            in_name_table = True
            final_lines.append(line)
            final_lines.append(f'      {canonical_name}: {keeper["internalID"]}')
            continue

        if in_name_table:
            # Skip old entries (they look like "      name: id")
            if line.startswith('      ') and ':' in line and not line.strip().startswith('{'):
                # Check if this looks like a nameFileIdTable entry (name: number)
                stripped = line.strip()
                parts = stripped.split(':')
                if len(parts) == 2 and parts[1].strip().lstrip('-').isdigit():
                    continue
            in_name_table = False

        final_lines.append(line)

    # Fix spriteMode to 2
    sprite_mode_info = ""
    for i, line in enumerate(final_lines):
        if line.strip().startswith('spriteMode:') and '  spriteMode:' in line:
            current_mode = line.strip().split(':')[1].strip()
            if current_mode != '2':
                final_lines[i] = line.replace(f'spriteMode: {current_mode}', 'spriteMode: 2')
                sprite_mode_info = f", spriteMode {current_mode}->2"
            break

    with open(meta_path, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(final_lines))

    return (
        f"  Kept: {canonical_name} ({keeper['width']}x{keeper['height']}, area={keeper['area']})\n"
        f"  Removed artifacts: {removed_info}{sprite_mode_info}"
    )


def main():
    total_fixed = 0
    total_scanned = 0

    print(f"Scanning item sprite metas in: {ITEMS_ROOT}\n")

    for world in WORLDS:
        world_dir = os.path.join(ITEMS_ROOT, world)
        if not os.path.isdir(world_dir):
            print(f"[SKIP] {world}/ not found")
            continue

        meta_files = sorted(glob.glob(os.path.join(world_dir, "*.png.meta")))
        world_fixed = 0

        for meta_path in meta_files:
            total_scanned += 1
            item_name = os.path.splitext(os.path.splitext(os.path.basename(meta_path))[0])[0]

            result = fix_meta_file(meta_path)
            if result:
                print(f"[FIXED] {world}/{item_name}:")
                print(result)
                print()
                world_fixed += 1
                total_fixed += 1

        print(f"--- {world}: {world_fixed} fixed / {len(meta_files)} scanned ---\n")

    print(f"=== TOTAL: {total_fixed} files fixed / {total_scanned} scanned ===")


if __name__ == '__main__':
    main()
