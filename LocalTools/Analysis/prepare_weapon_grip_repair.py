from pathlib import Path
from PIL import Image

OUT=Path(__file__).resolve().parent
source=Image.open(OUT/'front-receiver-corrected-preview.png').convert('RGBA')
source.crop((435,425,630,940)).resize((585,1545),Image.Resampling.LANCZOS).save(OUT/'front-weapon-grips-repair-input.png')
