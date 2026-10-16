from pathlib import Path
import argparse
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument('--recovery', action='store_true')
args = parser.parse_args()
name = 'run-recovery' if args.recovery else 'run-trial'
sheet = Image.open(root / (name + '-runtime-sheet.png')).convert('RGB')
frames = []
for i in range(40):
    x, y = i % 5 * 320, i // 5 * 180
    frame = sheet.crop((x, y, x + 320, y + 180)).resize((640, 360))
    ImageDraw.Draw(frame).text((10, 10), 'Run trial | speed 2.4 | sampled runtime', fill='white')
    frames.append(frame)
frames[0].save(root / (name + '-runtime.gif'), save_all=True,
               append_images=frames[1:], duration=17, loop=0)
