"""Static registration study; does not reproduce runtime IK or head shaders."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/RightWalkPrototype/Art'
OUT = ROOT / 'LocalTools/Analysis'

def layer(canvas, name, offset):
    image = Image.open(ART / (name + '.png')).convert('RGBA')
    canvas.alpha_composite(image, offset)

def compose(legs):
    canvas = Image.new('RGBA', (512, 512))
    layer(canvas, 'RightWalk-Pelvis-Underlay-v46', (0, 0))
    layer(canvas, 'RightWalk-FarUpperArm-v48', (13, 3))
    layer(canvas, 'RightWalk-FarForearm-v43', (13, 3))
    canvas.alpha_composite(legs)
    layer(canvas, 'RightWalk-Hip-Clothing-v43', (0, 0))
    layer(canvas, 'RightWalk-Torso-v42', (10, 0))
    layer(canvas, 'RightWalk-HeadFace-v46', (2, 4))
    layer(canvas, 'RightWalk-CollarFront-v45', (10, 0))
    layer(canvas, 'RightWalk-WeaponHands-v44', (-5, 24))
    layer(canvas, 'RightWalk-NearUpperArm-v47', (3, 4))
    layer(canvas, 'RightWalk-NearForearm-v43', (3, 4))
    return canvas

walk = Image.open(ART / 'WalkStrideTrial.png').convert('RGBA').crop((0, 0, 512, 512))
jump = Image.open(OUT / 'jump-ascent-cutout-v29.png').convert('RGBA')
full = compose(jump)
full.save(OUT / 'jump-ascent-full-body-study-v19.png')
preview = Image.new('RGBA', (1024, 580), (45, 62, 74, 255))
preview.alpha_composite(compose(walk), (0, 45))
preview.alpha_composite(full, (512, 45))
draw = ImageDraw.Draw(preview)
draw.text((20, 15), 'Walk reference / static layers', fill='white')
draw.text((532, 15), 'Ascent study / static layers', fill='white')
preview.save(OUT / 'jump-ascent-full-body-comparison-v19.png')
full.crop((260, 295, 345, 365)).resize((680, 560), Image.Resampling.NEAREST).save(OUT / 'jump-knee-cleanup-detail-v1.png')
print('Static full-body previews saved; runtime scene and assets untouched.')
