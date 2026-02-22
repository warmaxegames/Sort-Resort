
import json, sys, gc, tracemalloc
import level_solver

tracemalloc.start()

with open("Assets/_Project/Resources/Data/Levels/Island/level_060.json") as f:
    data = json.load(f)

print(f"Level 60: {len(data['containers'])} containers")

# Track memory per move
orig_find_best = level_solver._find_best_move
move_num = [0]
def tracked_find_best(*args, **kwargs):
    result = orig_find_best(*args, **kwargs)
    move_num[0] += 1
    if move_num[0] % 5 == 0:
        current, peak = tracemalloc.get_traced_memory()
        print(f"  Move {move_num[0]}: current={current/1024/1024:.1f}MB, peak={peak/1024/1024:.1f}MB", flush=True)
        gc.collect()
    return result
level_solver._find_best_move = tracked_find_best

try:
    result = level_solver.solve_level(data, strategy=level_solver.BALANCED, move_limit=300)
    print(f"Result: success={result.success}, moves={result.total_moves}")
except Exception as e:
    print(f"Error: {e}")
    traceback.print_exc()
finally:
    current, peak = tracemalloc.get_traced_memory()
    print(f"Final: current={current/1024/1024:.1f}MB, peak={peak/1024/1024:.1f}MB")
