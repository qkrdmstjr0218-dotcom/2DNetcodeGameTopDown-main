"""Assemble current source textures for art reference without altering Unity assets."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
scope={'__file__':str(HERE/'preview_jump_full_body.py')}
exec(compile((HERE/'preview_jump_full_body.py').read_text().split('\nwalk =')[0],'<source-compose>','exec'),scope)
art=ROOT/'Assets/RightWalkPrototype/Art'
legs=Image.open(art/'WalkStrideTrial.png').convert('RGBA').crop((0,0,512,512))
full=scope['compose'](legs)
full.save(HERE/'current-player-source-reference.png')
board=Image.new('RGBA',(1024,720),(45,62,74,255))
board.alpha_composite(full,(0,80))
items=[('RightWalk-WeaponHands-v44',20),('RightWalk-Torso-v42',250)]
for name,y in items:
    part=Image.open(art/(name+'.png')).convert('RGBA')
    part=part.crop(part.getchannel('A').getbbox())
    part.thumbnail((470,210),Image.Resampling.LANCZOS)
    board.alpha_composite(part,(540,y+50))
boot=Image.open(ROOT/'Assets/RightWalkPrototype/Resources/JumpTransition/Boot.png').convert('RGBA')
boot=boot.crop(boot.getchannel('A').getbbox());boot=boot.resize((boot.width*2,boot.height*2),Image.Resampling.NEAREST)
board.alpha_composite(boot,(560,520))
draw=ImageDraw.Draw(board)
draw.text((20,20),'Current source cutouts / static assembly',fill='white')
draw.text((540,20),'Weapon and hands / source pixels',fill='white')
draw.text((540,250),'Torso / source pixels',fill='white')
draw.text((540,490),'Boot / source pixels enlarged',fill='white')
board.save(HERE/'current-player-design-reference.png')
print('Saved source references; no Unity assets changed.')
