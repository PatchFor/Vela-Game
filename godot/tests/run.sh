#!/usr/bin/env bash
# Runs the logic tests and the bot smoke test headless. Fails on any failed check or script error.
# Usage: GODOT=/path/to/godot tests/run.sh   (from the godot/ folder)
set -u
GODOT="${GODOT:-godot}"
cd "$(dirname "$0")/.."
status=0
for scene in run_tests smoke_test; do
  echo "== $scene"
  out=$(timeout 300 "$GODOT" --headless --path . --fixed-fps 60 "res://tests/$scene.tscn" 2>&1)
  code=$?
  # The dummy (headless) renderer logs harmless mesh warnings; hide them.
  echo "$out" | grep -v 'Parameter "m" is null' | grep -v 'mesh_storage.h' | grep -v '^\s*$'
  if [ $code -ne 0 ] || echo "$out" | grep -q "SCRIPT ERROR"; then
    echo "!! $scene FAILED"
    status=1
  fi
done
exit $status
