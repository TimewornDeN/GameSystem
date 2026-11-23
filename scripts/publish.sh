#!/usr/bin/env bash
# Builds release packages of GameSystem into the dist folder (Linux/macOS).
# Usage: ./scripts/publish.sh [runtime] [version]   e.g. ./scripts/publish.sh linux-x64 1.0.0
# The windowed program (WPF) is built for Windows only, so here only the console version is packaged.
set -euo pipefail
RUNTIME="${1:-linux-x64}"
VERSION="${2:-1.0.0}"
cd "$(dirname "$0")/.."

rm -rf dist && mkdir dist

dotnet test GameSystem.slnx -c Release

OUT="dist/GameSystem-Console-$VERSION-$RUNTIME"
dotnet publish GameSystem.CLI/GameSystem.CLI.csproj -c Release -r "$RUNTIME" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None -p:DebugSymbols=false -p:Version="$VERSION" -o "$OUT"
cp README.md docs/DESIGN.md "$OUT"
(cd dist && zip -qr "GameSystem-Console-$VERSION-$RUNTIME.zip" "GameSystem-Console-$VERSION-$RUNTIME")
echo "Done. Packages are in: dist"
