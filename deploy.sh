#!/usr/bin/env bash
set -euo pipefail

MOD_ID="dumbwaiter"
ASSEMBLY="Dumbwaiter"
MODS_DIR="$HOME/.config/VintagestoryData/Mods"
SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SRC_DIR/bin/Debug"
STAGE_DIR="$SRC_DIR/bin/stage"
ZIP_PATH="$SRC_DIR/bin/${MOD_ID}.zip"

if pgrep -x Vintagestory >/dev/null 2>&1; then
  echo "ERROR: Vintage Story is running. Close it before deploying — hot-replacing the mod zip leaves the assembly in a broken state and discards existing block-entity data." >&2
  exit 1
fi

echo "==> dotnet build"
dotnet build "$SRC_DIR/${ASSEMBLY}.csproj" -c Debug

DLL_PATH="$BUILD_DIR/${ASSEMBLY}.dll"
if [[ ! -f "$DLL_PATH" ]]; then
  echo "ERROR: built DLL not found at $DLL_PATH" >&2
  exit 1
fi

echo "==> staging zip contents"
rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR"
cp "$SRC_DIR/modinfo.json" "$STAGE_DIR/"
cp "$SRC_DIR/modicon.png" "$STAGE_DIR/"
cp -r "$SRC_DIR/assets" "$STAGE_DIR/"
cp "$DLL_PATH" "$STAGE_DIR/"

echo "==> packaging zip"
rm -f "$ZIP_PATH"
(cd "$STAGE_DIR" && zip -r -q "$ZIP_PATH" .)

echo "==> deploying to $MODS_DIR"
mkdir -p "$MODS_DIR"
cp "$ZIP_PATH" "$MODS_DIR/${MOD_ID}.zip"

echo "==> done: $MODS_DIR/${MOD_ID}.zip"
