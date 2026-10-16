from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent / 'FrontPartsSourceV3'
source = Image.open(OUT.parent / 'front-proportion-local-trial.png').convert('RGBA')
regions = {'stock': (475,425,565,540), 'receiver': (475,535,565,705), 'barrel': (475,695,565,950)}
for name, box in regions.items():
    detail = source.crop(box).resize((450,(box[3]-box[1])*5), Image.Resampling.NEAREST)
    board = Image.new('RGBA', (510,detail.height+35), '#33434d')
    board.alpha_composite(detail, (50,30))
    draw = ImageDraw.Draw(board)
    for x in range(480,566,10):
        xx=50+(x-box[0])*5
        draw.text((xx,10),str(x),fill='white')
    for y in range(((box[1]+9)//10)*10,box[3],10):
        yy=30+(y-box[1])*5
        draw.text((0,yy),str(y),fill='white')
        draw.line((45,yy,50,yy),fill='#ff6060')
    board.convert('RGB').save(OUT / (name+'-source-edge-detail.png'))
