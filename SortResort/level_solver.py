#!/usr/bin/env python3
"""
Sort Resort Level Solver - Python Port

Greedy solver that finds a move sequence to clear all items from a level.
Used by the level generator to validate levels and calculate exact star thresholds.

IMPORTANT: This solver is a Python port of the C# solver at:
    Assets/_Project/Scripts/Tools/LevelSolver.cs
If you modify the heuristics or algorithm here, UPDATE THE C# VERSION TOO (and vice versa).
The two implementations must stay in sync to produce identical results.
"""

import copy
import gc
import random
import time
import threading
from dataclasses import dataclass, field
from typing import List, Optional, Dict, Tuple


# ── Data Structures ──────────────────────────────────────────────────────────

class ContainerState:
    """State of a container during solving. slots[slot_idx][row_idx] = item_id or None."""

    def __init__(self, cid, slot_count, max_rows, is_locked=False,
                 unlock_matches_required=0):
        self.id = cid
        self.slot_count = slot_count
        self.max_rows = max_rows
        self.is_locked = is_locked
        self.unlock_matches_required = unlock_matches_required
        self.current_unlock_progress = 0
        # slots[s][r] = item_id or None
        self.slots = [[None] * max_rows for _ in range(slot_count)]

    def clone(self):
        c = ContainerState(self.id, self.slot_count, self.max_rows,
                           self.is_locked, self.unlock_matches_required)
        c.current_unlock_progress = self.current_unlock_progress
        c.slots = [list(s) for s in self.slots]
        return c

    def get_front_item(self, slot_idx):
        if 0 <= slot_idx < len(self.slots):
            return self.slots[slot_idx][0]
        return None

    def is_front_slot_empty(self, slot_idx):
        return self.get_front_item(slot_idx) is None

    def get_empty_front_slot_count(self):
        return sum(1 for s in range(self.slot_count) if self.is_front_slot_empty(s))

    def get_front_row_items(self):
        return [self.slots[s][0] for s in range(self.slot_count) if self.slots[s][0] is not None]

    def has_back_row_items(self):
        for s in self.slots:
            for r in range(1, len(s)):
                if s[r] is not None:
                    return True
        return False

    def get_back_row_item_count(self):
        count = 0
        for s in self.slots:
            for r in range(1, len(s)):
                if s[r] is not None:
                    count += 1
        return count

    def get_back_row_item_types(self):
        items = []
        for s in self.slots:
            for r in range(1, len(s)):
                if s[r] is not None:
                    items.append(s[r])
        return items

    def is_empty(self):
        for s in self.slots:
            for item in s:
                if item is not None:
                    return False
        return True

    def get_total_item_count(self):
        count = 0
        for s in self.slots:
            for item in s:
                if item is not None:
                    count += 1
        return count


class GameState:
    """Full game state during solving."""

    def __init__(self):
        self.containers: List[ContainerState] = []
        self.move_count = 0
        self.match_count = 0

    def clone(self):
        gs = GameState()
        gs.containers = [c.clone() for c in self.containers]
        gs.move_count = self.move_count
        gs.match_count = self.match_count
        return gs

    def get_total_item_count(self):
        return sum(c.get_total_item_count() for c in self.containers)

    def is_complete(self):
        return self.get_total_item_count() == 0


@dataclass
class Move:
    from_container: int
    from_slot: int
    to_container: int
    to_slot: int
    item_id: str
    score: int = 0
    reason: str = ""

    def __repr__(self):
        return (f"Move {self.item_id} from C[{self.from_container}].S[{self.from_slot}] "
                f"to C[{self.to_container}].S[{self.to_slot}]")


@dataclass
class SolveResult:
    success: bool = False
    total_moves: int = 0
    total_matches: int = 0
    move_sequence: List[Move] = field(default_factory=list)
    failure_reason: str = ""
    solve_time_ms: float = 0.0


@dataclass
class SolverStrategy:
    """Weight profile for ensemble solving. Defaults reproduce baseline behavior."""
    name: str = "Balanced"
    pair_weight: float = 1.0      # Scales pair creation/destruction bonuses
    reveal_weight: float = 1.0    # Scales reveal/row-advance bonuses
    caution_weight: float = 1.0   # Scales penalties (higher = more cautious)
    noise_magnitude: int = 0      # Random noise ±N added to each move score


BALANCED = SolverStrategy("Balanced")
PAIR_FOCUSED = SolverStrategy("PairFocused", pair_weight=1.4, reveal_weight=0.85)
REVEAL_FOCUSED = SolverStrategy("RevealFocused", pair_weight=0.85, reveal_weight=1.4)
CAUTIOUS = SolverStrategy("Cautious", reveal_weight=0.9, caution_weight=1.6)
AGGRESSIVE = SolverStrategy("Aggressive", pair_weight=1.1, reveal_weight=1.3, caution_weight=0.5)
ALL_STRATEGIES = [BALANCED, PAIR_FOCUSED, REVEAL_FOCUSED, CAUTIOUS, AGGRESSIVE]


# ── Solver ───────────────────────────────────────────────────────────────────

MAX_MOVES = 500
PATTERN_WINDOW = 10


def solve_level_best(level_dict, noise_runs_per_strategy=3, noise_magnitude=8, verbose=False):
    """Solve a level using multiple strategies + noise restarts, return the best result."""
    start = time.perf_counter()
    best = None
    move_limit = level_dict.get("construction_moves",
                 level_dict.get("_construction_moves", MAX_MOVES))
    if not move_limit or move_limit <= 0:
        move_limit = MAX_MOVES
    best_strategy_name = ""

    for strat in ALL_STRATEGIES:
        # Clean run (no noise)
        result = solve_level(level_dict, strategy=strat, move_limit=move_limit)
        if result.success and (best is None or result.total_moves < best.total_moves):
            best = result
            move_limit = best.total_moves
            best_strategy_name = strat.name
        else:
            del result
        gc.collect()

        # Noise restarts
        for run in range(1, noise_runs_per_strategy + 1):
            noise_strat = SolverStrategy(
                name=f"{strat.name}_n{run}",
                pair_weight=strat.pair_weight,
                reveal_weight=strat.reveal_weight,
                caution_weight=strat.caution_weight,
                noise_magnitude=noise_magnitude,
            )
            result = solve_level(level_dict, strategy=noise_strat, noise_seed=run,
                                 move_limit=move_limit)
            if result.success and (best is None or result.total_moves < best.total_moves):
                best = result
                move_limit = best.total_moves
                best_strategy_name = noise_strat.name
            else:
                del result
            gc.collect()

    if best is None:
        best = solve_level(level_dict)

    best.solve_time_ms = (time.perf_counter() - start) * 1000

    if verbose and best.success:
        print(f"  Best: {best.total_moves} moves via {best_strategy_name} "
              f"({best.solve_time_ms:.1f}ms total)")

    return best


def solve_level(level_dict, verbose=False, strategy=None, noise_seed=0, move_limit=0):
    """Solve a level from its JSON dict. Returns SolveResult."""
    start = time.perf_counter()
    result = SolveResult()

    effective_limit = move_limit if move_limit > 0 else MAX_MOVES

    # Set up noise RNG
    noise_rng = None
    if strategy and strategy.noise_magnitude > 0:
        noise_rng = random.Random(noise_seed)

    state = _initialize_state(level_dict)
    if state is None:
        result.failure_reason = "Failed to initialize state"
        result.solve_time_ms = (time.perf_counter() - start) * 1000
        return result

    # Process any immediate matches at start
    _process_all_matches(state)

    last_move = None
    recent_moves = []

    while not state.is_complete() and state.move_count < effective_limit:
        best = _find_best_move(state, last_move, recent_moves, verbose,
                               strategy=strategy, noise_rng=noise_rng)

        if best is None:
            result.failure_reason = f"No valid moves. {state.get_total_item_count()} items remaining."
            result.total_moves = state.move_count
            result.total_matches = state.match_count
            result.solve_time_ms = (time.perf_counter() - start) * 1000
            return result

        _execute_move(state, best)
        result.move_sequence.append(best)
        last_move = best

        recent_moves.append(best)
        if len(recent_moves) > PATTERN_WINDOW:
            recent_moves.pop(0)

        new_matches = _process_all_matches(state)
        if new_matches > 0:
            last_move = None
            recent_moves.clear()

    if state.is_complete():
        result.success = True
        result.total_moves = state.move_count
        result.total_matches = state.match_count
    else:
        result.failure_reason = "Max moves exceeded"
        result.total_moves = state.move_count
        result.total_matches = state.match_count

    result.solve_time_ms = (time.perf_counter() - start) * 1000
    return result


# ── State Initialization ─────────────────────────────────────────────────────

def _initialize_state(level_dict):
    """Build GameState from level JSON dict."""
    state = GameState()

    for cdef in level_dict.get("containers", []):
        slot_count = cdef.get("slot_count", 3)
        if slot_count <= 0:
            slot_count = 3
        max_rows = cdef.get("max_rows_per_slot", 4)
        if max_rows <= 0:
            max_rows = 4

        c = ContainerState(
            cid=cdef.get("id", ""),
            slot_count=slot_count,
            max_rows=max_rows,
            is_locked=cdef.get("is_locked", False),
            unlock_matches_required=cdef.get("unlock_matches_required", 0),
        )

        # Place initial items
        for item in cdef.get("initial_items", []):
            s = item.get("slot", 0)
            r = item.get("row", 0)
            if 0 <= s < slot_count and 0 <= r < max_rows:
                c.slots[s][r] = item.get("id")

        state.containers.append(c)

    return state


# ── Move Finding ─────────────────────────────────────────────────────────────

def _find_best_move(state, last_move, recent_moves, verbose=False,
                    strategy=None, noise_rng=None):
    """Find the best move using greedy heuristics."""
    # RULE 1: Always take 1-move matches
    one_move = _find_one_move_match(state)
    if one_move is not None:
        one_move.score = 999
        one_move.reason = "1-move match (always taken)"
        return one_move

    # RULE 2: Analyze item accessibility
    item_status = _analyze_item_accessibility(state)

    # RULE 3: Get all valid moves
    all_moves = _get_all_valid_moves(state)
    if not all_moves:
        return None

    # Build recent move pattern set
    recent_set = set()
    if recent_moves:
        for rm in recent_moves:
            recent_set.add(f"{rm.item_id}:{rm.from_container}->{rm.to_container}")

    # RULE 4: Score all moves
    scored = []
    for move in all_moves:
        score, reason = _score_move_unified(state, move, item_status, strategy=strategy)

        # RULE 5: Reversal penalty
        if last_move and _is_reversal_move(move, last_move):
            score -= 1000
            reason += ", REVERSAL PENALTY"

        # RULE 6: Pattern penalty
        reverse_key = f"{move.item_id}:{move.to_container}->{move.from_container}"
        if reverse_key in recent_set:
            score -= 500
            reason += ", PATTERN PENALTY"

        # Apply noise for ensemble diversity
        if noise_rng is not None and strategy and strategy.noise_magnitude > 0:
            noise = noise_rng.randint(-strategy.noise_magnitude, strategy.noise_magnitude)
            score += noise

        scored.append((score, move, reason))

    scored.sort(key=lambda x: -x[0])

    best_score, best_move, best_reason = scored[0]
    best_move.score = best_score
    best_move.reason = best_reason
    return best_move


# ── Item Accessibility ───────────────────────────────────────────────────────

def _analyze_item_accessibility(state):
    """Classify each item type: (accessible, nearly_accessible, total)."""
    result = {}  # item_id -> (accessible, nearly, total)

    # Containers close to unlocking
    near_unlock = set()
    for ci, c in enumerate(state.containers):
        if c.is_locked and c.unlock_matches_required - c.current_unlock_progress <= 2:
            near_unlock.add(ci)

    # Containers close to row advance
    near_advance = set()
    for ci, c in enumerate(state.containers):
        if c.is_locked:
            continue
        occupied = sum(1 for s in range(c.slot_count) if not c.is_front_slot_empty(s))
        if occupied <= 1 and c.has_back_row_items():
            near_advance.add(ci)

    for ci, container in enumerate(state.containers):
        for s in range(container.slot_count):
            for r in range(len(container.slots[s])):
                item_id = container.slots[s][r]
                if item_id is None:
                    continue

                if item_id not in result:
                    result[item_id] = [0, 0, 0]

                current = result[item_id]
                current[2] += 1  # total

                if r == 0 and not container.is_locked:
                    current[0] += 1  # accessible
                elif r == 0 and ci in near_unlock:
                    current[1] += 1  # nearly accessible
                elif r == 1 and ci in near_advance:
                    current[1] += 1  # nearly accessible

    return {k: tuple(v) for k, v in result.items()}


# ── Move Scoring ─────────────────────────────────────────────────────────────

def _score_move_unified(state, move, item_status, strategy=None):
    """Score a move on a unified scale. Returns (score, reason_string)."""
    score = 0
    reasons = []

    # Category subtotals for strategy weight adjustments
    pair_contrib = 0
    reveal_contrib = 0
    penalty_contrib = 0

    from_c = state.containers[move.from_container]
    to_c = state.containers[move.to_container]

    # Item accessibility
    acc, near, total = item_status.get(move.item_id, (0, 0, 0))
    is_actionable = (acc + near) >= 2

    # Destination info
    dest_items = to_c.get_front_row_items()
    matching_at_dest = sum(1 for i in dest_items if i == move.item_id)

    # === MATCH-ENABLING BONUSES ===
    test_state = state.clone()
    _execute_move(test_state, move)

    if _would_match(test_state):
        score += 200
        reasons.append("creates match")
    else:
        follow_up = _find_one_move_match(test_state)
        if follow_up is not None:
            if matching_at_dest >= 1:
                score += 120
                reasons.append("enables match + creates pair")
            elif len(dest_items) == 0:
                score += 80
                reasons.append("enables match (to empty)")
            else:
                score += 40
                reasons.append("enables match (temp location)")

            # Follow-up quality: evaluate how good the enabled match is
            fu_from_c = test_state.containers[follow_up.from_container]
            fu_from_occ = sum(1 for s in range(fu_from_c.slot_count)
                              if not fu_from_c.is_front_slot_empty(s))

            # Bonus if the follow-up move triggers row advance at its source
            if fu_from_occ == 1 and fu_from_c.has_back_row_items():
                revealed_by_fu = _get_items_that_would_advance(fu_from_c)
                fu_reveal_bonus = 20 + len(revealed_by_fu) * 10
                score += fu_reveal_bonus; reveal_contrib += fu_reveal_bonus
                reasons.append(f"follow-up reveals {len(revealed_by_fu)} items")

            # Check if follow-up creates chain matches
            fu_state = test_state.clone()
            _execute_move(fu_state, follow_up)
            _process_all_matches(fu_state)

            chain_match = _find_one_move_match(fu_state)
            if chain_match is not None:
                score += 15; reveal_contrib += 15
                reasons.append("follow-up chains into match")

    # === PAIRING BONUS ===
    already_credited_pair = "enables match + creates pair" in reasons
    pair_room_will_open = False

    if matching_at_dest == 1 and not already_credited_pair:
        third_accessible = acc >= 3
        third_nearly = (acc + near) >= 3
        empty_at_dest = to_c.get_empty_front_slot_count()
        has_room_for_third = empty_at_dest >= 2

        if third_accessible and has_room_for_third:
            score += 180; pair_contrib += 180
            reasons.append("creates completable pair")
        elif third_accessible and not has_room_for_third:
            score -= 50; pair_contrib -= 50
            reasons.append("creates BLOCKED pair (no room for 3rd)")
        elif third_nearly and has_room_for_third:
            hidden_at_dest = to_c.get_back_row_item_types()
            if move.item_id in hidden_at_dest:
                score -= 200; pair_contrib -= 200
                reasons.append("SELF-BLOCKING pair (3rd hidden HERE)")
            else:
                score += 100; pair_contrib += 100
                reasons.append("creates near-completable pair (3rd nearly accessible)")
        elif not third_accessible and has_room_for_third:
            score += 20; pair_contrib += 20
            reasons.append("creates waiting pair (3rd hidden)")
            if to_c.has_back_row_items():
                score -= 80; pair_contrib -= 80
                reasons.append("pair blocks reveals")
        else:
            # WORST PAIR: 3rd hidden AND no room
            non_matching = set(i for i in dest_items if i != move.item_id)
            room_will_open = bool(non_matching) and all(
                sum(1 for c2 in test_state.containers if not c2.is_locked
                    for fi in c2.get_front_row_items() if fi == item_type) >= 3
                for item_type in non_matching
            )

            if room_will_open:
                score += 30; pair_contrib += 30
                pair_room_will_open = True
                reasons.append("creates pair (room will open - blocking type clearable)")
            else:
                score -= 100; pair_contrib -= 100
                reasons.append("creates useless pair (hidden + blocked)")

    elif matching_at_dest == 0 and len(dest_items) > 0 and "enables match (temp location)" not in reasons:
        score -= 10; penalty_contrib -= 10
        reasons.append("mixes items")

    # === SELF-BLOCKING PAIR PENALTY (enables-match path) ===
    if matching_at_dest >= 1 and already_credited_pair:
        hidden_at_dest = to_c.get_back_row_item_types()
        if move.item_id in hidden_at_dest:
            score -= 200; pair_contrib -= 200
            reasons.append("SELF-BLOCKING pair (3rd hidden HERE)")

    # === ACTIONABILITY ===
    if is_actionable:
        score += 30
        reasons.append("actionable item")
    else:
        has_useful = any(r.startswith("creates match") or r.startswith("enables match") or
                         r == "creates pair" for r in reasons)
        if not has_useful:
            score -= 40; penalty_contrib -= 40
            reasons.append("stuck item shuffle")

    # === PAIR DESTRUCTION PENALTY ===
    source_items = from_c.get_front_row_items()
    matching_at_source = sum(1 for i in source_items if i == move.item_id)
    if matching_at_source == 2:
        completing_triple = matching_at_dest == 2
        if not completing_triple:
            source_empty = from_c.get_empty_front_slot_count()
            has_room = source_empty >= 1
            third_acc = acc >= 3
            if third_acc and has_room:
                score -= 150; pair_contrib -= 150
                reasons.append("DESTROYS completable pair")
            elif third_acc and not has_room:
                score -= 30; pair_contrib -= 30
                reasons.append("breaks blocked pair")

    # === ROW ADVANCEMENT BONUS ===
    from_occupied = sum(1 for s in range(from_c.slot_count) if not from_c.is_front_slot_empty(s))

    if from_occupied == 1 and from_c.has_back_row_items():
        revealed = _get_items_that_would_advance(from_c)
        row_adv_bonus = 100 + len(revealed) * 25
        score += row_adv_bonus; reveal_contrib += row_adv_bonus
        reasons.append(f"triggers row advance ({len(revealed)} items)")

        for rev_item in revealed:
            if _has_waiting_pair_for_item(state, rev_item):
                score += 80; reveal_contrib += 80
                reasons.append(f"reveals {rev_item} for waiting pair")
                break

        # Combo bonus: pair + reveal (widened to include near-completable)
        if matching_at_dest == 1 and acc >= 3:
            score += 60; reveal_contrib += 60
            reasons.append("combo: completable pair + reveal")
        elif matching_at_dest == 1 and (acc + near) >= 3:
            score += 50; reveal_contrib += 50
            reasons.append("combo: near-pair + reveal")

    elif from_c.has_back_row_items():
        back_count = from_c.get_back_row_item_count()
        progress_bonus = 30 + back_count * 10
        score += progress_bonus; reveal_contrib += progress_bonus
        reasons.append(f"progress toward reveal ({back_count} hidden)")

    # === SOURCE PAIR BONUS (Double-Pair Recognition) ===
    # When row advance reveals a pair at source
    if from_occupied == 1 and from_c.has_back_row_items():
        revealed_for_pair = _get_items_that_would_advance(from_c)
        revealed_counts = {}
        for ri in revealed_for_pair:
            revealed_counts[ri] = revealed_counts.get(ri, 0) + 1
        for ri_type, ri_cnt in revealed_counts.items():
            if ri_cnt >= 2:
                score += 40; pair_contrib += 40
                reasons.append(f"reveals source pair ({ri_type})")
                break
    elif from_occupied > 1:
        # Check if remaining front items at source form a pair
        remaining_counts = {}
        for s in range(from_c.slot_count):
            fi = from_c.get_front_item(s)
            if fi is not None and s != move.from_slot:
                remaining_counts[fi] = remaining_counts.get(fi, 0) + 1
        for ri_type, ri_cnt in remaining_counts.items():
            if ri_cnt >= 2:
                score += 25; pair_contrib += 25
                reasons.append(f"exposes source pair ({ri_type})")
                break

    # === DESTINATION QUALITY ===
    dest_empty = to_c.get_empty_front_slot_count()
    if dest_empty <= 1 and not pair_room_will_open:
        score -= 15; penalty_contrib -= 15
        reasons.append("fills container")

    # === DEADLOCK PREVENTION ===
    # Check if this move would leave dangerously few empty front slots globally
    deadlock_test = test_state.clone()
    matches_from_move = _process_all_matches(deadlock_test)
    total_empty_slots = 0
    for c in deadlock_test.containers:
        if not c.is_locked:
            total_empty_slots += c.get_empty_front_slot_count()

    # Count containers about to unlock (within 2 matches)
    near_unlock_count = sum(1 for c in deadlock_test.containers
                            if c.is_locked and c.unlock_matches_required - c.current_unlock_progress <= 2)

    if matches_from_move == 0:
        if total_empty_slots == 0 and near_unlock_count == 0:
            score -= 500; penalty_contrib -= 500
            reasons.append("DEADLOCK: leaves 0 empty slots")
        elif total_empty_slots == 0 and near_unlock_count > 0:
            score -= 150; penalty_contrib -= 150
            reasons.append("tight board (unlocks coming)")
        elif total_empty_slots == 1 and near_unlock_count == 0:
            score -= 100; penalty_contrib -= 100
            reasons.append("near-deadlock: only 1 empty slot left")
        elif total_empty_slots <= 2 and near_unlock_count == 0:
            score -= 30; penalty_contrib -= 30
            reasons.append("low slots remaining")

    # === STAGING MOVE ===
    if len(dest_items) == 0 and matching_at_dest == 0:
        if from_occupied == 1 and from_c.has_back_row_items():
            score += 20
            reasons.append("productive staging")
        else:
            # Pure staging with no reveal - not great but sometimes necessary
            score -= 5; penalty_contrib -= 5
            reasons.append("staging move")

    # === MATCH-IN-PLACE CONSIDERATION ===
    from_empty = from_c.get_empty_front_slot_count()
    making_good_pair = matching_at_dest == 1 and acc >= 3
    triggering_reveal = from_occupied == 1 and from_c.has_back_row_items()
    if from_empty >= 2 and is_actionable and matching_at_dest == 0 and not triggering_reveal:
        score -= 35; penalty_contrib -= 35
        reasons.append("disrupts match-in-place potential")

    # === MATCH-AT-REVEALING-CONTAINER BONUS ===
    will_complete = matching_at_dest >= 2 or (matching_at_dest == 1 and acc >= 3)
    if to_c.has_back_row_items() and matching_at_dest >= 1 and will_complete:
        hidden_count = to_c.get_back_row_item_count()
        triple_reveal_bonus = 50 + hidden_count * 20
        score += triple_reveal_bonus; reveal_contrib += triple_reveal_bonus
        reasons.append(f"triple reveals {hidden_count} hidden item(s)")

        hidden_items = to_c.get_back_row_item_types()
        unique_hidden = set(h for h in hidden_items if h != move.item_id)
        if unique_hidden:
            score += 30; reveal_contrib += 30
            reasons.append("clears container for revealed items")

    # === STRATEGY WEIGHT ADJUSTMENTS ===
    if strategy is not None:
        score += int(pair_contrib * (strategy.pair_weight - 1.0))
        score += int(reveal_contrib * (strategy.reveal_weight - 1.0))
        score += int(penalty_contrib * (strategy.caution_weight - 1.0))

    reason = ", ".join(reasons) if reasons else "neutral"
    return score, reason


# ── 1-Move Match Finding ─────────────────────────────────────────────────────

def _find_one_move_match(state):
    """Find a move that immediately results in a triple match.
    When multiple exist, prefer ones that reveal hidden items."""
    candidates = []

    for ci, container in enumerate(state.containers):
        if container.is_locked or container.slot_count < 3:
            continue

        empty_slot = -1
        for s in range(container.slot_count):
            if container.is_front_slot_empty(s):
                empty_slot = s
                break
        if empty_slot == -1:
            continue

        front = container.get_front_row_items()
        if len(front) < 2:
            continue

        # Count items in front row
        counts = {}
        for item in front:
            counts[item] = counts.get(item, 0) + 1

        for target_item, cnt in counts.items():
            if cnt < 2:
                continue

            # Look for 3rd item elsewhere
            for oci, other in enumerate(state.containers):
                if oci == ci or other.is_locked:
                    continue
                for os in range(other.slot_count):
                    if other.get_front_item(os) == target_item:
                        move = Move(oci, os, ci, empty_slot, target_item)

                        # Score by reveal potential
                        reveal_score = 0
                        from_occ = sum(1 for s2 in range(other.slot_count)
                                       if not other.is_front_slot_empty(s2))
                        if from_occ == 1 and other.has_back_row_items():
                            revealed = _get_items_that_would_advance(other)
                            reveal_score += 100 + len(revealed) * 20
                            for rev in revealed:
                                if _has_waiting_pair_for_item(state, rev):
                                    reveal_score += 50

                        if container.has_back_row_items():
                            dest_rev = _get_items_that_would_advance(container)
                            reveal_score += 50 + len(dest_rev) * 15

                        candidates.append((reveal_score, move))

    if not candidates:
        return None

    candidates.sort(key=lambda x: -x[0])
    return candidates[0][1]


# ── Move Enumeration ─────────────────────────────────────────────────────────

def _get_all_valid_moves(state):
    """Get all valid moves: front-row items to empty front slots.
    Optimizations to prevent combinatorial explosion when many containers are empty:
    1. Completely empty containers: only one representative per slot_count is used
       as a destination, since moves to different empty containers of the same shape
       are functionally identical.
    2. Multiple empty slots in a destination: only the first empty slot is used,
       since slot position doesn't affect the solver's scoring or matching logic
       (match checks all 3 front items, not specific positions).
    """
    # Identify completely empty unlocked containers and pick one representative per slot_count
    empty_representatives = {}  # slot_count -> container index
    empty_container_set = set()
    for ci, c in enumerate(state.containers):
        if not c.is_locked and c.is_empty():
            empty_container_set.add(ci)
            if c.slot_count not in empty_representatives:
                empty_representatives[c.slot_count] = ci

    moves = []
    for from_ci, from_c in enumerate(state.containers):
        if from_c.is_locked:
            continue
        for from_s in range(from_c.slot_count):
            item = from_c.get_front_item(from_s)
            if item is None:
                continue
            for to_ci, to_c in enumerate(state.containers):
                if to_ci == from_ci or to_c.is_locked:
                    continue
                # Skip non-representative empty containers
                if to_ci in empty_container_set and to_ci != empty_representatives.get(to_c.slot_count):
                    continue
                # Only consider first empty slot in each destination container
                first_empty = -1
                for to_s in range(to_c.slot_count):
                    if to_c.is_front_slot_empty(to_s):
                        first_empty = to_s
                        break
                if first_empty >= 0:
                    moves.append(Move(from_ci, from_s, to_ci, first_empty, item))
    return moves


# ── Move Execution ───────────────────────────────────────────────────────────

def _execute_move(state, move):
    """Execute a move: remove from source, place at dest, advance rows."""
    from_c = state.containers[move.from_container]
    to_c = state.containers[move.to_container]

    from_c.slots[move.from_slot][0] = None
    to_c.slots[move.to_slot][0] = move.item_id
    state.move_count += 1

    _check_and_advance_rows(from_c)


def _check_and_advance_rows(container):
    """If all front slots are empty and back items exist, advance rows forward."""
    # Check if all front slots empty
    for s in range(container.slot_count):
        if not container.is_front_slot_empty(s):
            return

    if not container.has_back_row_items():
        return

    # Advance all rows forward
    for s in range(container.slot_count):
        first_non_null = -1
        for r in range(1, len(container.slots[s])):
            if container.slots[s][r] is not None:
                first_non_null = r
                break

        if first_non_null > 0:
            slot = container.slots[s]
            num_rows = len(slot)
            for r in range(first_non_null, num_rows):
                slot[r - first_non_null] = slot[r]
                if r >= first_non_null:
                    slot[r] = None


# ── Match Processing ─────────────────────────────────────────────────────────

def _process_all_matches(state):
    """Process all matches repeatedly until none remain. Returns match count."""
    total = 0
    while True:
        found = False
        for ci, container in enumerate(state.containers):
            if container.slot_count < 3:
                continue
            if _process_container_match(state, container):
                found = True
                total += 1
        if not found:
            break
    return total


def _process_container_match(state, container):
    """Check if container front row is a complete triple match. Process if so."""
    front = [container.get_front_item(s) for s in range(container.slot_count)]

    if any(f is None for f in front):
        return False
    if len(set(front)) != 1:
        return False

    # Match! Clear front row
    for s in range(container.slot_count):
        container.slots[s][0] = None

    state.match_count += 1

    # Unlock progress
    for c in state.containers:
        if c.is_locked:
            c.current_unlock_progress += 1
            if c.current_unlock_progress >= c.unlock_matches_required:
                c.is_locked = False

    _check_and_advance_rows(container)
    return True


def _would_match(state):
    """Check if any container has a complete triple match."""
    for c in state.containers:
        if c.slot_count < 3:
            continue
        front = [c.get_front_item(s) for s in range(c.slot_count)]
        if all(f is not None for f in front) and len(set(front)) == 1:
            return True
    return False


# ── Utility ──────────────────────────────────────────────────────────────────

def _is_reversal_move(current, previous):
    return (current.item_id == previous.item_id and
            current.from_container == previous.to_container and
            current.to_container == previous.from_container)


def _get_items_that_would_advance(container):
    """Get items that would advance to front row if front were cleared."""
    items = []
    for s in range(container.slot_count):
        for r in range(1, len(container.slots[s])):
            if container.slots[s][r] is not None:
                items.append(container.slots[s][r])
                break
    return items


def _has_waiting_pair_for_item(state, item_id):
    """Check if any container has 2 of this item + empty slot (waiting for 3rd)."""
    for c in state.containers:
        if c.is_locked or c.slot_count < 3:
            continue
        match_count = 0
        empty_count = 0
        for s in range(c.slot_count):
            front = c.get_front_item(s)
            if front == item_id:
                match_count += 1
            elif front is None:
                empty_count += 1
        if match_count == 2 and empty_count >= 1:
            return True
    return False


# ── Standalone Test ──────────────────────────────────────────────────────────

if __name__ == "__main__":
    import json
    import sys

    if len(sys.argv) < 2:
        print("Usage: python level_solver.py <level_json_file> [--verbose] [--best]")
        sys.exit(1)

    path = sys.argv[1]
    verbose = "--verbose" in sys.argv
    use_best = "--best" in sys.argv

    with open(path) as f:
        level = json.load(f)

    print(f"Solving: {path}")
    if use_best:
        result = solve_level_best(level, verbose=True)
    else:
        result = solve_level(level, verbose=verbose)

    if result.success:
        print(f"SOLVED: {result.total_moves} moves, {result.total_matches} matches "
              f"({result.solve_time_ms:.1f}ms)")
        if verbose:
            for i, m in enumerate(result.move_sequence):
                print(f"  {i+1:3d}. {m.item_id:20s} : C[{m.from_container}].S[{m.from_slot}] -> "
                      f"C[{m.to_container}].S[{m.to_slot}]  (score: {m.score}, {m.reason})")
    else:
        print(f"FAILED: {result.failure_reason} ({result.solve_time_ms:.1f}ms)")
