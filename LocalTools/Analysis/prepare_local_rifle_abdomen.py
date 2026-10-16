from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
SOURCE = Path('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-9ac857e7-79f5-43d3-b62d-bbaaf61f9a4f.png')
image = Image.open(SOURCE)
# Keep the head and legs out of the edit input entirely.
image.crop((395, 425, 650, 950)).save(ROOT / 'rifle-abdomen-local-input.png')
