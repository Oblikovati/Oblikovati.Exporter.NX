#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-2.0-only
#
# Assembles the UGII_USER_DIR release zip for the PYTHON journal edition: the
# deploy-python/ layout (startup/.men, README) plus the journal and the exporter package.
# No compilation and no NXOpen stub — the journal is shipped as source and played by NX.
#
# Usage: scripts/package_python.sh <version> <out-zip>
set -euo pipefail

VERSION="${1:?usage: package_python.sh <version> <out-zip>}"
OUTZIP="${2:?missing output zip path}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Resolve the output path to absolute before we cd into the staging dir.
case "$OUTZIP" in
    /*) ;;
    *) OUTZIP="$PWD/$OUTZIP" ;;
esac

STAGE="$(mktemp -d)"
cp -r "$ROOT/deploy-python/." "$STAGE/"
mkdir -p "$STAGE/startup"

# The journal and the package it imports both live in startup/, beside the .men.
cp "$ROOT/python/journal/oblikovati_export.py" "$STAGE/startup/"
cp -r "$ROOT/python/oblikovati_exporter_nx" "$STAGE/startup/"
# Ship only source; drop any compiled caches that may exist locally.
find "$STAGE/startup/oblikovati_exporter_nx" -name '__pycache__' -type d -prune -exec rm -rf {} +
# The NXOpen adapter is the only part that can't be exercised off-NX, but it is REQUIRED
# at runtime inside NX, so it is intentionally shipped.

echo "$VERSION" > "$STAGE/VERSION"

rm -f "$OUTZIP"
( cd "$STAGE" && zip -r -q "$OUTZIP" . )
echo "packaged $OUTZIP:"
( cd "$STAGE" && find . -type f | sort | sed 's/^/  /' )
