#!/usr/bin/env python3
"""Subprocess solver wrapper - reads level JSON from file arg or stdin, outputs result JSON to stdout.
Uses only solve_level (single strategy) to minimize memory usage and avoid CPython segfaults."""
import sys
import json
import gc

# Disable GC to reduce memory corruption chance
gc.disable()

from level_solver import solve_level

# Read level data from file path argument (preferred) or stdin fallback
if len(sys.argv) >= 2 and sys.argv[1] != 'single':
    with open(sys.argv[1], 'r') as f:
        level_data = json.load(f)
elif len(sys.argv) >= 3 and sys.argv[1] == 'single':
    # Edge case: 'single' as first arg, file path missing
    level_data = json.loads(sys.stdin.read())
else:
    level_data = json.loads(sys.stdin.read())
result = solve_level(level_data)

output = {
    "success": result.success,
    "total_moves": result.total_moves,
    "total_matches": result.total_matches,
    "failure_reason": result.failure_reason,
    "solve_time_ms": result.solve_time_ms,
}
sys.stdout.write(json.dumps(output))
sys.stdout.flush()
