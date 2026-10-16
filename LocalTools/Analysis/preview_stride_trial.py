from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
sheet = Image.open(root / 'stride-trial-runtime-sheet.png').convert('RGB')
frames = []
for i in range(40):
    x, y = i % 5 * 320, i // 5 * 180
    frame = sheet.crop((x, y, x + 320, y + 180)).resize((640, 360))
    ImageDraw.Draw(frame).text((10, 10), 'Long stride trial | speed 1.6 | sampled runtime', fill='white')
    frames.append(frame)
frames[0].save(root / 'stride-trial-runtime.gif', save_all=True,
               append_images=frames[1:], duration=20, loop=0)
print('Saved sampled runtime preview')
