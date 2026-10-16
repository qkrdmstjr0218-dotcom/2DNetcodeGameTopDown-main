from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
sheet = Image.open(root / 'Assets/RightWalkPrototype/Art/RunRecoveryTrial.png')
indices = [0, 4, 8, 12, 16, 20, 24, 28, 32, 36]
montage = Image.new('RGB', (1000, 1650), '#526467')
for n, index in enumerate(indices):
    x, y = index % 5 * 512, index // 5 * 512
    crop = sheet.crop((x + 100, y + 300, x + 350, y + 465)).resize((500, 330))
    montage.paste(crop, (n % 2 * 500, n // 2 * 330), crop)
    ImageDraw.Draw(montage).text((n % 2 * 500 + 10, n // 2 * 330 + 5), str(index), fill='white')
montage.save(root / 'LocalTools/Analysis/run-boot-detail.png')
