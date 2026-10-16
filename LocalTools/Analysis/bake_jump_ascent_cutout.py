"""Non-destructive ascent study using the existing walk rigid-part pipeline."""
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'LocalTools/Analysis'
ART = ROOT / 'Assets/RightWalkPrototype/Art'

# Load only the established read-only part loader, not the walk sheet bake.
loader = (OUT / 'bake_stride_trial.py').read_text().split('\nns = read_parts()')[0]
scope = {'__file__': str(OUT / 'bake_stride_trial.py')}
exec(compile(loader, '<walk-part-loader>', 'exec'), scope)
ns = scope['read_parts']()
ns['source'].crop((275, 275, 350, 360)).resize((450, 510), Image.Resampling.NEAREST).save(OUT / 'jump-knee-source-audit.png')
# Recover the entire original cap silhouette beyond the legacy x333 cut.
cap_mask = Image.new('L', (512, 512))
ImageDraw.Draw(cap_mask).polygon([(305, 294), (317, 296), (331, 310),
                                 (339, 324), (336, 334), (319, 334),
                                 (307, 323), (303, 309)], fill=255)
cap = ns['source'].copy()
cap.putalpha(ImageChops.multiply(cap.getchannel('A'), cap_mask))
knee_patch = ns['joint_patch'](ns['nearKnee'], 23)
knee_patch.alpha_composite(cap)
tile = Image.new('RGBA', (512, 512))
records = {}
for side, thigh_deg, shin_deg in [('far', 70, 102), ('near', 68, 132)]:
    hip, knee, ankle = [ns[side + name] for name in ('Hip', 'Knee', 'Ankle')]
    h = (hip[0] - 12, 254)
    l1, l2 = ns['dist'](hip, knee), ns['dist'](knee, ankle)
    ta, sa = math.radians(thigh_deg), math.radians(shin_deg)
    k = (h[0] + l1 * math.cos(ta), h[1] + l1 * math.sin(ta))
    a = (k[0] + l2 * math.cos(sa), k[1] + l2 * math.sin(sa))
    thigh = ns['trim_end'](ns['fixed'][side], hip, knee, 0)
    ns['draw_part'](tile, thigh, hip, h, ta - ns['angle'](hip, knee))
    ns['draw_part'](tile, ns['calves'][side], knee, k, sa - ns['angle'](knee, ankle))
    boot_angle = math.radians(12 if side == 'far' else 14)
    ns['draw_part'](tile, ns['new'], ns['nearAnkle'], (a[0] + 2, a[1]), boot_angle)
    # Original full knee cap goes last, in front of the thigh, with the shin.
    ns['draw_part'](tile, knee_patch, ns['nearKnee'], k,
                    sa - ns['angle'](ns['nearKnee'], ns['nearAnkle']))
    records[side] = {'hip': h, 'knee': k, 'ankle': a,
                     'thighLength': l1, 'shinLength': l2,
                     'bootAngleDegrees': math.degrees(boot_angle)}

accepted = Image.open(ART / 'WalkStrideTrial.png').convert('RGBA')
waist = accepted.crop((0, 0, 512, 250))
tile.alpha_composite(waist, (0, -6))
# Repair only the marked near-knee boundaries in registered tile coordinates.
draw = ImageDraw.Draw(tile)
draw.rectangle((320, 317, 324, 322), fill=(0, 0, 0, 0))
draw.line([(313, 324), (319, 322), (320, 324)], fill=(25, 27, 30, 255), width=1)
tile.save(OUT / 'jump-ascent-cutout-v6.png')
source = accepted.crop((0, 0, 512, 512))
preview = Image.new('RGBA', (1000, 600), (45, 62, 74, 255))
for i, frame in enumerate((source, tile)):
    crop = frame.crop((120, 225, 400, 500)).resize((480, 472), Image.Resampling.NEAREST)
    preview.alpha_composite(crop, (i * 500 + 10, 65))
ImageDraw.Draw(preview).text((20, 20), 'Original walk frame', fill='white')
ImageDraw.Draw(preview).text((520, 20), 'Ascent: original rigid parts', fill='white')
preview.save(OUT / 'jump-ascent-cutout-comparison-v6.png')
(OUT / 'jump-ascent-cutout-v6.json').write_text(json.dumps(records, indent=2))
print('Saved preview only; no runtime assets modified. Rigid transforms, no generated art.')
