"""Restrict preview edits to the two boot uppers, preserving all other pixels."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
SOURCE = Path('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-90c460bb-2f71-403d-9396-00fe42e694f6.png')
BOX = (900, 800, 1325, 930)
base = Image.open(SOURCE).convert('RGBA')
crop = base.crop(BOX)
if len(sys.argv) == 1:
    crop.resize((1275, 390), Image.Resampling.LANCZOS).save(ROOT / 'downshot-boots-edit-input.png')
else:
    edited = Image.open(sys.argv[1]).convert('RGBA')
    pixels = np.asarray(edited)
    rows = np.flatnonzero(np.any(pixels[:, :, :3] > 12, axis=(1, 2)))
    edited = edited.crop((0, int(rows[0]), edited.width, int(rows[-1]) + 1))
    edited = edited.resize(crop.size, Image.Resampling.LANCZOS)
    mask = Image.new('L', crop.size)
    draw = ImageDraw.Draw(mask)
    # Only the white boot uppers and their adjacent edging; exclude shins and soles.
    draw.polygon([(946-900,833-800),(992-900,834-800),(1005-900,886-800),(931-900,886-800)], fill=255)
    draw.polygon([(1240-900,834-800),(1288-900,833-800),(1304-900,886-800),(1229-900,886-800)], fill=255)
    patch = Image.composite(edited, crop, mask)
    result = base.copy()
    result.paste(patch, BOX[:2])
    result.save(ROOT / 'downshot-boots-local-outline-preview.png')
    result.crop(BOX).resize((1275,390), Image.Resampling.NEAREST).save(ROOT / 'downshot-boots-local-outline-detail.png')
    changed = np.any(np.asarray(result) != np.asarray(base), axis=2)
    allowed = np.zeros(changed.shape, dtype=bool)
    allowed[BOX[1]:BOX[3], BOX[0]:BOX[2]] = np.asarray(mask) > 0
    assert not np.any(changed & ~allowed)
    print('Verified: zero changed pixels outside the two boot-upper masks.')
