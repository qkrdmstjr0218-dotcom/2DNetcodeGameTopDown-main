"""Show unmodified runtime cutouts as a design reference, not new pose art."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
PARTS = ROOT / 'Assets/RightWalkPrototype/Resources/JumpTransition'
OUT = Path(__file__).resolve().parent / 'downshot-original-parts-reference.png'

board = Image.new('RGB', (1200, 560), (45, 62, 74))
draw = ImageDraw.Draw(board)
for index, (filename, label) in enumerate([
    ('Boot.png', 'Original boot / unchanged pixels'),
    ('nearThigh.png', 'Near thigh / original side plate'),
    ('farThigh.png', 'Far thigh / original side plate'),
]):
    part = Image.open(PARTS / filename).convert('RGBA')
    part = part.crop(part.getchannel('A').getbbox())
    scale = min(350 // part.width, 440 // part.height)
    part = part.resize((part.width * scale, part.height * scale), Image.Resampling.NEAREST)
    x = index * 400 + (400 - part.width) // 2
    y = 65 + (440 - part.height) // 2
    board.paste(part, (x, y), part)
    draw.text((index * 400 + 20, 20), label, fill='white')
draw.text((20, 535), 'Source-part reference only. No front-view artwork or Unity asset changes.', fill='white')
board.save(OUT)
print(OUT)
