from pathlib import Path
from PIL import Image

here = Path(__file__).resolve().parent
before = Image.open(here / 'height-before.png').convert('RGB')
after = Image.open(here / 'height-after.png').convert('RGB')
assert before.size == after.size
board = Image.new('RGB', (before.width * 2, before.height))
board.paste(before, (0, 0))
board.paste(after, (before.width, 0))
board.save(here / 'height-placement-comparison.png')
