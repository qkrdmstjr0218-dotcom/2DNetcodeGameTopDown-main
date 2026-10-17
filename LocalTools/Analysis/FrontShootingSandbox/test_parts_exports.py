"""Read-only checks for the PNGs exported from the editable Canvas masks."""
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image


HERE = Path(__file__).resolve().parent
config = json.loads((HERE / 'parts-v2.json').read_text(encoding='utf-8'))
source_path = HERE / config['source']
original_path = HERE.parent / 'front-proportion-local-trial.png'
assert hashlib.sha256(source_path.read_bytes()).digest() == hashlib.sha256(original_path.read_bytes()).digest()
source = np.asarray(Image.open(source_path).convert('RGBA')).astype(np.int16)
excluded = np.asarray(Image.open(HERE / config['weaponExclusion'])) >= 128
owners = np.zeros(source.shape[:2], dtype=np.uint8)
report = []
for part in config['parts']:
    path = HERE / (part['id'] + '-v2.png')
    image = Image.open(path).convert('RGBA')
    assert list(image.size) == config['sourceSize']
    pixels = np.asarray(image).astype(np.int16)
    visible = pixels[:, :, 3] > 0
    assert visible.any() and (~visible).any()
    assert not np.any(visible & excluded), part['id']
    assert np.array_equal(pixels[:, :, 3][visible], source[:, :, 3][visible])
    # Browser premultiplication may round raw RGB by one level; rendered reconstruction is checked in the page.
    opaque = visible & (source[:, :, 3] >= 200)
    rgb_delta = int(np.abs(pixels[:, :, :3][opaque] - source[:, :, :3][opaque]).max())
    assert rgb_delta <= 1, (part['id'], rgb_delta)
    owners += visible
    report.append({'part': part['id'], 'visible_pixels': int(visible.sum()), 'max_opaque_rgb_rounding': rgb_delta})
assert owners.max() == 1, 'Visible layers must not duplicate source pixels.'
assert {p['id'] for p in config['parts']} == {
    f'{side}-{segment}' for side in ['trigger', 'support'] for segment in ['upper', 'fore', 'hand']
}
print(json.dumps({'parts': report, 'source_file_unchanged': True, 'overlap_pixels': 0,
                  'weapon_pixels': 0, 'unity_runtime_installed': False}, indent=2))
