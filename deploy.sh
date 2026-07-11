#!/usr/bin/env bash
set -euo pipefail

MOD_ID="dumbwaiter"
ASSEMBLY="Dumbwaiter"
MODS_DIR="$HOME/.config/VintagestoryData/Mods"
SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SRC_DIR/bin/Debug"
STAGE_DIR="$SRC_DIR/bin/stage"

MOD_VERSION="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$SRC_DIR/modinfo.json")"
if [[ -z "$MOD_VERSION" ]]; then
  echo "ERROR: could not read version from modinfo.json" >&2
  exit 1
fi
ZIP_NAME="${MOD_ID}-${MOD_VERSION}.zip"
ZIP_PATH="$SRC_DIR/bin/${ZIP_NAME}"

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
rm -f "$SRC_DIR/bin/${MOD_ID}"*.zip
(cd "$STAGE_DIR" && zip -r -q "$ZIP_PATH" .)

echo "==> deploying to $MODS_DIR"
mkdir -p "$MODS_DIR"
# Clear out any previous versions so the game doesn't load the mod twice
rm -f "$MODS_DIR/${MOD_ID}.zip" "$MODS_DIR/${MOD_ID}-"*.zip
cp "$ZIP_PATH" "$MODS_DIR/${ZIP_NAME}"

echo "==> done: $MODS_DIR/${ZIP_NAME}"
