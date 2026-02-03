# Level Configuration Reference

This document describes all available options for configuring levels in Sort Resort.

---

## Level Structure

```json
{
  "id": 1,
  "world_id": "resort",
  "name": "level_001",
  "star_move_thresholds": [10, 15, 20],
  "containers": [ ... ]
}
```

### Level Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | int | Yes | Unique level number |
| `world_id` | string | Yes | Which world's items to use: `"resort"`, `"supermarket"`, `"farm"` |
| `name` | string | Yes | Level identifier string |
| `star_move_thresholds` | int[3] | Yes | Move counts for [3-star, 2-star, 1-star] ratings |
| `containers` | array | Yes | Array of container definitions |

---

## Container Configuration

```json
{
  "id": "container_1",
  "position": {"x": 300, "y": 400},
  "container_type": "standard",
  "container_image": "base_shelf",
  "slot_count": 3,
  "max_rows_per_slot": 4,
  "initial_items": [ ... ]
}
```

### Basic Container Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `id` | string | Yes | - | Unique container identifier |
| `position` | {x, y} | Yes | - | Position in Godot pixel coordinates (see Coordinate System below) |
| `container_type` | string | Yes | - | `"standard"` (3 slots) or `"single_slot"` (1 slot) |
| `container_image` | string | Yes | - | Sprite name (see Container Images below) |
| `slot_count` | int | No | 3 | Number of horizontal slots |
| `max_rows_per_slot` | int | No | 4 | Maximum items stacked per slot |
| `initial_items` | array | No | [] | Items to place at level start |

### Container Images

| Image Name | Description |
|------------|-------------|
| `base_shelf` | Standard 3-slot shelf |
| `base_single_slot_container` | Single slot container |
| `supermarket_container` | Supermarket-themed shelf |
| `supermarket_single_slot_container` | Supermarket single slot |
| `farm_container` | Farm-themed shelf |
| `farm_single_slot_container` | Farm single slot |

---

## Lock System

Locked containers require a certain number of matches (anywhere in the level) before they unlock.

```json
{
  "is_locked": true,
  "unlock_matches_required": 3,
  "lock_overlay_image": "base_lockoverlay",
  "unlock_animation": "fade_in"
}
```

### Lock Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `is_locked` | bool | No | false | Whether container starts locked |
| `unlock_matches_required` | int | If locked | - | Matches needed to unlock (any container counts) |
| `lock_overlay_image` | string | If locked | - | Lock overlay sprite name |
| `unlock_animation` | string | No | "fade_in" | Animation type: `"fade_in"` or `"fade_out"` |

### Lock Overlay Images

| Image Name | Description |
|------------|-------------|
| `base_lockoverlay` | Standard shelf lock overlay |
| `base_single_slot_lockoverlay` | Single slot lock overlay |

---

## Movement System

Containers can move in various patterns.

### Carousel Movement

Moves in one direction, then snaps back to start position (conveyor belt effect).

```json
{
  "is_moving": true,
  "move_type": "carousel",
  "move_direction": "right",
  "move_speed": 80,
  "move_distance": 1800
}
```

### Back and Forth Movement

Smoothly moves in one direction, then reverses (ping-pong effect).

```json
{
  "is_moving": true,
  "move_type": "back_and_forth",
  "move_direction": "down",
  "move_speed": 30,
  "move_distance": 200
}
```

### Movement Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `is_moving` | bool | No | false | Whether container moves |
| `move_type` | string | If moving | - | `"carousel"` or `"back_and_forth"` |
| `move_direction` | string | If moving | "right" | `"left"`, `"right"`, `"up"`, `"down"` |
| `move_speed` | float | No | 50 | Movement speed in pixels per second |
| `move_distance` | float | No | 200 | Total distance to travel in pixels |

### Movement Types Explained

| Type | Behavior |
|------|----------|
| `carousel` | Moves in direction, snaps back to start when distance reached. Good for "train" effects. |
| `back_and_forth` | Moves in direction, smoothly reverses at endpoints. Good for patrolling containers. |

---

## Falling System

Containers can fall from above and optionally despawn when cleared.

```json
{
  "is_falling": true,
  "fall_speed": 100,
  "fall_target_y": 400,
  "despawn_on_match": true
}
```

### Falling Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `is_falling` | bool | No | false | Whether container falls from starting position |
| `fall_speed` | float | No | 100 | Fall speed in pixels per second |
| `fall_target_y` | float | If falling | - | Y position to fall to (Godot coordinates) |
| `despawn_on_match` | bool | No | false | If true, container disappears when all items matched |

---

## Item Placement

Items are placed in containers using slot and row indices.

```json
{
  "initial_items": [
    {"id": "coconut", "row": 0, "slot": 0},
    {"id": "pineapple", "row": 0, "slot": 1},
    {"id": "coconut", "row": 1, "slot": 0}
  ]
}
```

### Item Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string | Yes | Item ID (must match sprite filename without extension) |
| `row` | int | Yes | Row index (0 = front/interactive, 1+ = back rows) |
| `slot` | int | Yes | Slot index (0 = leftmost) |

### Row Behavior

- **Row 0**: Front row - items are interactive and fully visible
- **Row 1+**: Back rows - items are grayed out and not interactive
- Items advance from back to front when ALL front row slots are empty

---

## Coordinate System

Positions use Godot pixel coordinates, converted to Unity world units:

```
Unity X = (Godot X - 540) / 100
Unity Y = (600 - Godot Y) / 100
```

### Reference Points (Portrait Mode)

| Description | Godot X | Godot Y |
|-------------|---------|---------|
| Screen center | 540 | 600 |
| Top of screen | 540 | 0 |
| Bottom of screen | 540 | 1200 |
| Left edge | 0 | 600 |
| Right edge | 1080 | 600 |

### Typical Container Positions

| Position | Godot X | Godot Y |
|----------|---------|---------|
| Top-left | 300 | 300 |
| Top-right | 800 | 300 |
| Middle-left | 300 | 600 |
| Middle-right | 800 | 600 |
| Bottom-left | 300 | 900 |
| Bottom-right | 800 | 900 |

### Off-Screen Positions (for carousel spawning)

| Position | Godot X |
|----------|---------|
| Off-screen left | -200 to 0 |
| Off-screen right | 1100 to 1300 |

---

## Available Items by World

### Resort World (`world_id: "resort"`)
coconut, greencoconut, pineapple, beachball_mixed, beachball_redblue, beachball_yellowblue, sandpale_green, sandpale_blue, sandpale_red, sunscreen, sunscreenspf50, margarita, maitai, strawberrydaiquiri, lemonade, orangedrink, watermelondrink, vanilladrink, icecreamcone, vanillaicecreamcone, sparkleicecreamcone, icecreamconechocolate, icecreamparfait, piratepopsicle, popsiclemintchocolate, sandals, flipflops, flipflopsgreen, surfboardblueyellow, surfboardbluepink, surfboardpalmtrees, surfboardbluewhitestripes, surfboardredorange, surfboardbluewhitepeace, pinkpolkadotbikini, bluesunglasses, sunglassespurple, sunglassesorange, scubatanks, swimmask, sunhat, hibiscusflower, watermelonslice, messageinabottle, suitcaseyellow, lifepreserver, beachbag, camera, oysterblue, lemonadepitcher

### Supermarket World (`world_id: "supermarket"`)
honey, milk, milkbottle, chips, soda, grapesoda, ketchup, eggs, pickles, mustard, mayo, hotsauce, garlicsauce, peanutbutter, raspberryjam, strawberryjam, cannedcorn, cannedtomato, peas, olivejar, coffeebeans, chocolatebar, nutbar, popcornbucket, bananayogurt, coconutyogurt, orangejuice, greenapplejuice, redapplejuice, peachjuice, strawberryjuice, carrotjuice, lemonjuice, energydrink, cider, greenwaterbottle, orangewaterbottle, bluethermos, pinkthermos, greenthermosstraw, pinkthermosstraw, whiteflower, pinkflower

### Farm World (`world_id: "farm"`)
greentomato, redtomato, potato, romaine, whiteonion, carrot, turnip

---

## Example Configurations

### Static Container (Basic)
```json
{
  "id": "static_shelf",
  "position": {"x": 540, "y": 400},
  "container_type": "standard",
  "container_image": "base_shelf",
  "initial_items": [
    {"id": "coconut", "row": 0, "slot": 0},
    {"id": "coconut", "row": 0, "slot": 1},
    {"id": "coconut", "row": 0, "slot": 2}
  ]
}
```

### Locked Container
```json
{
  "id": "locked_shelf",
  "position": {"x": 540, "y": 700},
  "container_type": "standard",
  "container_image": "base_shelf",
  "is_locked": true,
  "unlock_matches_required": 3,
  "lock_overlay_image": "base_lockoverlay",
  "initial_items": [
    {"id": "pineapple", "row": 0, "slot": 0},
    {"id": "pineapple", "row": 0, "slot": 1},
    {"id": "pineapple", "row": 0, "slot": 2}
  ]
}
```

### Moving Carousel (Train Effect)
```json
{
  "id": "train_car",
  "position": {"x": -200, "y": 400},
  "container_type": "standard",
  "container_image": "base_shelf",
  "is_moving": true,
  "move_type": "carousel",
  "move_direction": "right",
  "move_speed": 80,
  "move_distance": 1800,
  "initial_items": [
    {"id": "honey", "row": 0, "slot": 0}
  ]
}
```

### Back-and-Forth Patrol
```json
{
  "id": "patrol_shelf",
  "position": {"x": 540, "y": 500},
  "container_type": "standard",
  "container_image": "base_shelf",
  "is_moving": true,
  "move_type": "back_and_forth",
  "move_direction": "left",
  "move_speed": 40,
  "move_distance": 300,
  "initial_items": [
    {"id": "chips", "row": 0, "slot": 1}
  ]
}
```

### Falling + Despawn Container
```json
{
  "id": "falling_bonus",
  "position": {"x": 540, "y": 100},
  "container_type": "standard",
  "container_image": "base_shelf",
  "max_rows_per_slot": 1,
  "is_falling": true,
  "fall_speed": 100,
  "fall_target_y": 500,
  "despawn_on_match": true,
  "initial_items": [
    {"id": "eggs", "row": 0, "slot": 0},
    {"id": "eggs", "row": 0, "slot": 1},
    {"id": "eggs", "row": 0, "slot": 2}
  ]
}
```

### Multi-Row Container (Items Behind)
```json
{
  "id": "deep_shelf",
  "position": {"x": 540, "y": 600},
  "container_type": "standard",
  "container_image": "base_shelf",
  "max_rows_per_slot": 4,
  "initial_items": [
    {"id": "milk", "row": 0, "slot": 0},
    {"id": "soda", "row": 1, "slot": 0},
    {"id": "juice", "row": 2, "slot": 0},
    {"id": "milk", "row": 0, "slot": 1},
    {"id": "milk", "row": 1, "slot": 1}
  ]
}
```

### Combined: Locked + Moving
```json
{
  "id": "locked_patrol",
  "position": {"x": 540, "y": 800},
  "container_type": "single_slot",
  "container_image": "base_single_slot_container",
  "is_locked": true,
  "unlock_matches_required": 2,
  "lock_overlay_image": "base_single_slot_lockoverlay",
  "is_moving": true,
  "move_type": "back_and_forth",
  "move_direction": "up",
  "move_speed": 25,
  "move_distance": 150,
  "initial_items": [
    {"id": "honey", "row": 0, "slot": 0},
    {"id": "honey", "row": 1, "slot": 0}
  ]
}
```

---

## Design Tips

1. **Item Count**: Always use multiples of 3 for each item type (3, 6, 9, etc.) so all items can be matched.

2. **Difficulty Progression**:
   - Early levels: Static containers, few item types
   - Mid levels: Add locks, simple movement
   - Late levels: Multiple moving containers, more item types, deeper rows

3. **Train Configurations**: Space containers ~450px apart horizontally for a connected train look.

4. **Star Thresholds** (based on solver optimal):
   - 3-star: solver moves (optimal)
   - 2-star: solver × 1.15 (15% more moves)
   - 1-star: solver × 1.30 (30% more moves)
   - Fail: solver × 1.40 (40% more moves)

   Use `Tools > Sort Resort > Solver > Update All Level Thresholds` to recalculate.

5. **Locked Containers**: Place instant-match items (3 of same) in locked containers for satisfying unlocks.

6. **Falling Containers**: Use `max_rows_per_slot: 1` with `despawn_on_match: true` for bonus containers.
