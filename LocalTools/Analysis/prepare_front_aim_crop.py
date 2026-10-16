from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parent
image = Image.open(out / 'downshot-boot-gap-filled-preview.png')
image.crop((880,60,1340,940)).save(out / 'front-aim-source-crop.png')
