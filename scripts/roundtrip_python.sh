#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-2.0-only
#
# Emits golden documents with the PYTHON exporter and opens each with the real
# oblikovati-cli. This binds the Python emitter to the actual Oblikovati reader, exactly
# as scripts/roundtrip.sh does for the C# exporter. Used by CI and runnable locally.
#
# Usage: scripts/roundtrip_python.sh <path-to-oblikovati-cli> [python]
set -euo pipefail

CLI="${1:?usage: roundtrip_python.sh <path-to-oblikovati-cli> [python]}"
PY="${2:-python3}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$(mktemp -d)"

PYTHONPATH="$ROOT/python" "$PY" "$ROOT/python/tools/goldengen.py" "$OUT"

# Fixtures whose round-trip is open-only (load/recompute, but not DOF-0 hand-authored).
OPEN_ONLY="$(PYTHONPATH="$ROOT/python" "$PY" -c \
    'from oblikovati_exporter_nx.fixtures.sample_parts import OPEN_ONLY; print(" ".join(sorted(OPEN_ONLY)))')"

status=0
for f in "$OUT"/*.opd "$OUT"/*.oad; do
    [ -e "$f" ] || continue
    name="$(basename "$f")"
    # 1) The file loads and recomputes in the real reader (for an .oad this also
    #    resolves and places its referenced component files).
    if ! "$CLI" open "$f" >/dev/null; then
        echo "FAIL $name (open)"
        status=1
        continue
    fi
    # 2) For parts (except open-only ones), every sketch is fully constrained (DOF 0)
    #    with a closed profile.
    if [[ "$f" == *.oad ]] || [[ " $OPEN_ONLY " == *" $name "* ]]; then
        echo "OK   $name"
    elif "$CLI" script run "$ROOT/scripts/validate_sketches.lua" --doc "$f" >/dev/null 2>&1; then
        echo "OK   $name"
    else
        echo "FAIL $name (sketch validation)"
        status=1
    fi
done

exit "$status"
