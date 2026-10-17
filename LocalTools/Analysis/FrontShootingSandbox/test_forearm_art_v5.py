import json
import math
import sys
from pathlib import Path

from PIL import Image

root = Path(__file__).resolve().parent
config_name = sys.argv[1] if len(sys.argv) > 1 else "forearm-art-v5.json"
config = json.loads((root / config_name).read_text(encoding="utf-8"))
checks = {}
for side, art in config.items():
    image = Image.open(root / (art["image"] + ".png")).convert("RGBA")
    assert image.size == (1254, 1254)
    start, end = art["elbow"], art["wrist"]
    delta = [end[i] - start[i] for i in range(2)]
    size = math.hypot(*delta)
    normal = [-delta[1] / size, delta[0] / size]
    left, top, width, height = art["clip"]
    count = 0
    for step in range(1, 20):
        for offset in range(-10, 11):
            point = [round(start[i] + delta[i] * step / 20 + normal[i] * offset / art["widthScale"]) for i in range(2)]
            assert left <= point[0] < left + width and top <= point[1] < top + height
            assert image.getpixel(tuple(point))[3] >= 180, (side, point)
            count += 1
    checks[side] = count
print(json.dumps({"config": config_name, "source_inner_surface_samples": checks, "new_assets_have_closed_inner_corridors": True}))
