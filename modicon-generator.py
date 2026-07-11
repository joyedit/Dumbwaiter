#!/usr/bin/env python3
"""Pixel-art modicon for the Dumbwaiter mod: wooden lift frame, pulley,
rope and a crate on the platform, inside a dark stone shaft. 64x64 grid."""
import random
import struct
import zlib

W = H = 64

# Palette (warm, desaturated, VS-ish)
STONE = [(58, 54, 50), (69, 64, 58), (78, 72, 65), (56, 51, 46)]
STONE_DARK = [(44, 41, 38), (50, 46, 42), (39, 36, 33)]
WOOD_L = (164, 121, 74)   # lit face
WOOD_M = (138, 98, 56)    # mid
WOOD_D = (107, 74, 40)    # shaded
WOOD_O = (60, 42, 22)     # outline
ROPE_L = (184, 154, 98)
ROPE_D = (151, 121, 74)
IRON = (74, 74, 80)
IRON_D = (46, 46, 51)
CRATE_L = (156, 116, 68)
CRATE_M = (128, 92, 52)
CRATE_D = (98, 68, 36)

px = [[None] * W for _ in range(H)]

def put(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        px[y][x] = c

def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(x, y, c)

# --- Stone wall background, mottled in 2x2 chunks ---
rng = random.Random(42)
for by in range(0, H, 2):
    for bx in range(0, W, 2):
        c = rng.choice(STONE)
        for dy in range(2):
            for dx in range(2):
                put(bx + dx, by + dy, c)

# Darker shaft column between the posts (depth)
rng2 = random.Random(7)
for by in range(0, H, 2):
    for bx in range(12, 52, 2):
        c = rng2.choice(STONE_DARK)
        for dy in range(2):
            for dx in range(2):
                put(bx + dx, by + dy, c)

# --- Wooden frame ---
# Posts (left, right), y 4..61
for (x0, x1) in [(5, 10), (53, 58)]:
    rect(x0, 4, x1, 61, WOOD_M)
    rect(x0, 4, x0, 61, WOOD_L)          # lit left edge
    rect(x1 - 1, 4, x1, 61, WOOD_D)      # shaded right edge
    # outline
    rect(x0 - 1, 4, x0 - 1, 61, WOOD_O)
    rect(x1 + 1, 4, x1 + 1, 61, WOOD_O)

# Top crossbeam spanning posts, y 4..10
rect(5, 4, 58, 10, WOOD_M)
rect(5, 4, 58, 4, WOOD_L)                # lit top
rect(5, 9, 58, 10, WOOD_D)               # shaded bottom
rect(4, 3, 59, 3, WOOD_O)                # outline top
rect(4, 3, 4, 11, WOOD_O)
rect(59, 3, 59, 11, WOOD_O)
rect(5, 11, 10, 11, WOOD_O)
rect(53, 11, 58, 11, WOOD_O)

# Corner gussets (solid stepped triangles under the beam, against the posts)
for i in range(4):
    rect(11, 11 + i, 17 - i * 2, 11 + i, WOOD_D)
    rect(46 + i * 2, 11 + i, 52, 11 + i, WOOD_D)

# --- Pulley wheel under the beam ---
cx, cy, r = 32, 18, 6
for y in range(cy - r, cy + r + 1):
    for x in range(cx - r, cx + r + 1):
        d2 = (x - cx) ** 2 + (y - cy) ** 2
        if d2 <= r * r:
            put(x, y, WOOD_L if d2 > (r - 2) ** 2 else WOOD_D)
        elif d2 <= (r + 1) ** 2:
            put(x, y, WOOD_O)
# bracket from beam to wheel
rect(30, 11, 33, 12, IRON)
# axle
rect(cx - 1, cy - 1, cx + 1, cy + 1, IRON)
put(cx, cy, IRON_D)

# --- Rope from pulley down to the platform ---
for y in range(cy + r + 1, 46):
    put(31, y, ROPE_L)
    put(32, y, ROPE_D)

# --- Platform, hanging mid-low ---
rect(17, 46, 46, 50, WOOD_M)
rect(17, 46, 46, 46, WOOD_L)
rect(17, 50, 46, 50, WOOD_D)
# plank gaps
for gx in (27, 37):
    rect(gx, 46, gx, 50, WOOD_O)
# outline
rect(16, 45, 47, 45, WOOD_O)
rect(16, 51, 47, 51, WOOD_O)
rect(16, 45, 16, 51, WOOD_O)
rect(47, 45, 47, 51, WOOD_O)
# iron corner brackets
rect(17, 46, 18, 47, IRON)
rect(45, 46, 46, 47, IRON)

# --- Crate sitting on the platform ---
rect(24, 34, 39, 44, CRATE_M)
# frame
rect(24, 34, 39, 35, CRATE_L)
rect(24, 43, 39, 44, CRATE_D)
rect(24, 34, 25, 44, CRATE_L)
rect(38, 34, 39, 44, CRATE_D)
# X brace
for i in range(7):
    put(27 + i, 36 + i, CRATE_D)
    put(28 + i, 36 + i, CRATE_D)
    put(36 - i, 36 + i, CRATE_D)
    put(35 - i, 36 + i, CRATE_D)
# outline
rect(23, 33, 40, 33, WOOD_O)
rect(23, 33, 23, 44, WOOD_O)
rect(40, 33, 40, 44, WOOD_O)

# --- Counterweight hanging on the right ---
for y in range(11, 30):
    put(48, y, ROPE_D)
rect(45, 30, 51, 36, (128, 122, 112))
rect(45, 30, 51, 30, (150, 143, 131))
rect(45, 36, 51, 36, (100, 95, 87))
put(47, 32, (108, 103, 95))
put(49, 34, (108, 103, 95))
rect(44, 29, 52, 29, IRON_D)
rect(44, 29, 44, 37, IRON_D)
rect(52, 29, 52, 37, IRON_D)
rect(44, 37, 52, 37, IRON_D)

# --- Encode PNG ---
def write_png(path):
    raw = b""
    for y in range(H):
        raw += b"\x00"
        for x in range(W):
            r_, g_, b_ = px[y][x]
            raw += bytes((r_, g_, b_))

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", W, H, 8, 2, 0, 0, 0)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", ihdr))
        f.write(chunk(b"IDAT", zlib.compress(raw, 9)))
        f.write(chunk(b"IEND", b""))

import sys
write_png(sys.argv[1] if len(sys.argv) > 1 else "modicon64.png")
