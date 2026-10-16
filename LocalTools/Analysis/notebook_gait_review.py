import json
import math
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ANALYSIS = ROOT / 'LocalTools' / 'Analysis'
poses = json.loads((ANALYSIS / 'baked-walk-v32.json').read_text())
for side in ('near', 'far'):
    angles = []
    for pose in poses:
        leg = pose[side]
        a = [leg['hip'][i] - leg['knee'][i] for i in range(2)]
        b = [leg['ankle'][i] - leg['knee'][i] for i in range(2)]
        cosine = sum(x * y for x, y in zip(a, b)) / (math.hypot(*a) * math.hypot(*b))
        angles.append(180 - math.degrees(math.acos(max(-1, min(1, cosine)))))
    print(f'{side} knee flexion: {min(angles):.2f}..{max(angles):.2f} degrees')
distance = 100 / 204.8 / .6
print(f'Cycle distance: {distance:.6f}; cadence at 1.6: {2 * 1.6 / distance:.3f} steps/sec')
print(f'Cadence at 0.8: {2 * .8 / distance:.3f} steps/sec')
print(f'Root advances {distance / 20 * 204.8:.3f} source pixels/pose; stance ankle retreats 8.333')

# Turn the runtime contact sheet into a replay; this is sampled imagery, not a video recording.
sheet = Image.open(ANALYSIS / 'notebook-walk-contact-sheet.png').convert('RGB')
frames = []
for i in range(20):
    x, y = i % 5 * 320, i // 5 * 180
    frame = sheet.crop((x, y, x + 320, y + 180)).resize((640, 360))
    ImageDraw.Draw(frame).text((10, 10), f'Runtime sample {i + 1}/20 | speed 1.6 | fixed camera', fill='white')
    frames.append(frame)
frames[0].save(ANALYSIS / 'notebook-walk-sampled.gif', save_all=True,
               append_images=frames[1:], duration=25, loop=0)
