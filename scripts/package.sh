#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-2.0-only
#
# Assembles the UGII_USER_DIR release zip: the deploy/ layout (startup/.men, README)
# plus the plugin's managed DLLs. The NXOpen stub is deliberately excluded — NX supplies
# the real NXOpen.dll at runtime, and shipping the stub would shadow it.
#
# Usage: scripts/package.sh <publish-dir> <version> <out-zip>
#   <publish-dir>  output of `dotnet publish src/Exporter.NX.Entry -p:UseNxStubs=false`
set -euo pipefail

PUBLISH="${1:?usage: package.sh <publish-dir> <version> <out-zip>}"
VERSION="${2:?missing version}"
OUTZIP="${3:?missing output zip path}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

STAGE="$(mktemp -d)"
cp -r "$ROOT/deploy/." "$STAGE/"
mkdir -p "$STAGE/startup"

# Copy the plugin assemblies and YamlDotNet; never the NXOpen stub.
shopt -s nullglob
for dll in "$PUBLISH"/Oblikovati.Exporter.NX*.dll "$PUBLISH"/YamlDotNet.dll; do
    base="$(basename "$dll")"
    [ "$base" = "NXOpen.dll" ] && continue
    cp "$dll" "$STAGE/startup/"
done

echo "$VERSION" > "$STAGE/VERSION"

rm -f "$OUTZIP"
( cd "$STAGE" && zip -r -q "$OUTZIP" . )
echo "packaged $OUTZIP:"
( cd "$STAGE" && find . -type f | sort | sed 's/^/  /' )
