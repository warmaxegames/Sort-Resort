#!/usr/bin/env python3
"""
Post-build patch for WebGL builds when using the wasm-opt shim.

The wasm-opt shim skips import minification, so the WASM binary uses
standard import names ("env", "wasi_snapshot_preview1") while the
framework JS uses a single minified key ("a"). This script patches
the framework JS to provide both import modules.

Run after every WebGL build:
  python patch_webgl_build.py
"""

import gzip
import io
import os
import sys

BUILD_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                         "WebGL_Build", "Build")

MINIFIED = '{"a":wasmImports}'
PATCHED = '{"env":wasmImports,"wasi_snapshot_preview1":wasmImports}'


def patch_framework():
    # Find the framework file (compressed or uncompressed)
    framework_path = None
    is_compressed = False
    for f in os.listdir(BUILD_DIR):
        if f.endswith(".framework.js.unityweb"):
            framework_path = os.path.join(BUILD_DIR, f)
            is_compressed = True
            break
        elif f.endswith(".framework.js"):
            framework_path = os.path.join(BUILD_DIR, f)
            break

    if not framework_path:
        print("ERROR: No framework JS found in", BUILD_DIR)
        return False

    # Read file
    if is_compressed:
        with open(framework_path, "rb") as f:
            data = gzip.decompress(f.read()).decode("utf-8")
    else:
        with open(framework_path, "r") as f:
            data = f.read()

    # Check current state
    if PATCHED in data:
        print("Already patched!")
        return True

    if MINIFIED not in data:
        print("ERROR: Could not find minified import pattern")
        print("  Looking for:", MINIFIED)
        return False

    # Apply patch
    data = data.replace(MINIFIED, PATCHED)
    print(f"Patched: {MINIFIED} -> {PATCHED}")

    # Write back
    if is_compressed:
        filename = os.path.basename(framework_path).replace(".unityweb", "")
        buf = io.BytesIO()
        with gzip.GzipFile(filename=filename, mode="wb",
                           fileobj=buf, mtime=0) as gz:
            gz.write(data.encode("utf-8"))
        with open(framework_path, "wb") as f:
            f.write(buf.getvalue())
    else:
        with open(framework_path, "w", newline="\n") as f:
            f.write(data)

    final_size = os.path.getsize(framework_path)
    print(f"Written: {framework_path} ({final_size:,} bytes)")
    return True


if __name__ == "__main__":
    if patch_framework():
        print("Patch complete!")
    else:
        print("Patch FAILED")
        sys.exit(1)
