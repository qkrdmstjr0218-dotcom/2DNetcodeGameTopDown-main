from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/RightWalkPrototype/Art/WalkStrideTrial.png'
sheet = Image.open(path).convert('RGBA')
output = Image.new('RGB', (1200, 600), '#526467')
for column, index in enumerate((0, 20)):
    x, y = index % 5 * 512, index // 5 * 512
    crop = sheet.crop((x+140, y+360, x+390, y+480)).resize((1000, 480))
    output.paste(crop.resize((600, 288)), (column*600, 40), crop.resize((600, 288)))
    ImageDraw.Draw(output).text((column*600+10, 10), 'Standing frame ' + str(index), fill='white')
output.save(root / 'LocalTools/Analysis/standing-ankle-detail.png')
boot = Image.open(root / 'Assets/RightWalkPrototype/Art/WalkPart-Boot-Refined-v49.png').convert('RGBA')
detail = Image.new('RGBA', (600,400), '#263c4a')
detail.alpha_composite(boot.crop((320,395,440,475)).resize((600,400)))
detail.convert('RGB').save(root / 'LocalTools/Analysis/boot-source-detail.png')
