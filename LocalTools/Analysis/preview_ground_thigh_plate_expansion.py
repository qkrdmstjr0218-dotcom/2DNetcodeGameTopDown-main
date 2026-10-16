"""Apply approved thigh plate delta to gait previews only, preserving near-leg occlusion."""
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
out=root/'LocalTools/Analysis'
art=root/'Assets/RightWalkPrototype/Art'
scope={'__file__':str(out/'bake_stride_trial.py')}
exec(compile((out/'bake_stride_trial.py').read_text().split('\nns = read_parts()')[0],
             '<original-part-loader>','exec'),scope)
ns=scope['read_parts']()
old=np.array(ns['fixed']['far'])
new=np.array(Image.open(root/'Assets/RightWalkPrototype/Resources/JumpTransition/farThigh.png').convert('RGBA'))
delta=new.copy()
delta[~np.any(old!=new,axis=2)]=0
delta=Image.fromarray(delta)
data=json.loads((root/'Assets/RightWalkPrototype/Resources/JumpTransition/Joints.json').read_text())
comparison=Image.new('RGBA',(1440,1200),(45,62,74,255))
draw=ImageDraw.Draw(comparison)
for gait,name,row in [('walk','WalkStrideTrial',0),('run','RunRecoveryTrial',1)]:
    before=Image.open(art/(name+'.png')).convert('RGBA')
    after=before.copy()
    for index,pose in enumerate(data[gait]):
        far,near=pose['far'],pose['near']
        tile=before.crop((index%5*512,index//5*512,index%5*512+512,index//5*512+512))
        overlay=Image.new('RGBA',(512,512))
        rotation=ns['angle'](far['hip'],far['knee'])-ns['angle'](ns['farHip'],ns['farKnee'])
        ns['draw_part'](overlay,delta,ns['farHip'],far['hip'],rotation)
        cover=Image.new('RGBA',(512,512))
        # All existing foreground pieces stay in front of the modified far thigh.
        # Preserve the round hinge itself, not the old oversized rectangular cutout.
        hinge=Image.new('L',(512,512))
        kx,ky=far['knee']
        ImageDraw.Draw(hinge).ellipse((kx-16,ky-16,kx+16,ky+16),fill=255)
        cover.putalpha(hinge)
        ns['draw_part'](cover,ns['fixed']['near'],ns['nearHip'],near['hip'],
                        ns['angle'](near['hip'],near['knee'])-ns['angle'](ns['nearHip'],ns['nearKnee']))
        ns['draw_part'](cover,ns['calves']['near'],ns['nearKnee'],near['knee'],
                        ns['angle'](near['knee'],near['ankle'])-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
        ns['draw_part'](cover,ns['nearKneePatch'],ns['nearKnee'],near['knee'],
                        ns['angle'](near['knee'],near['ankle'])-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
        pixels=np.array(overlay)
        pixels[:,:,3]=np.rint(pixels[:,:,3].astype(float)*(1-np.asarray(cover)[:,:,3]/255)).astype('uint8')
        tile.alpha_composite(Image.fromarray(pixels))
        after.paste(tile,(index%5*512,index//5*512))
    after.save(out/(name+'-ThighPlatePreview.png'))
    if gait=='walk':
        detail=Image.new('RGBA',(800,360),(45,62,74,255))
        index=20
        for variant,sheet in enumerate((before,after)):
            tile=sheet.crop((index%5*512,index//5*512,index%5*512+512,index//5*512+512))
            detail.alpha_composite(tile.crop((220,270,320,355)).resize((400,340),Image.Resampling.NEAREST),(variant*400,20))
        detail.save(out/'standing-thigh-plate-expansion-before-after.png')
    for column,index in enumerate((0,10,20,30)):
        for variant,sheet in enumerate((before,after)):
            tile=sheet.crop((index%5*512,index//5*512,index%5*512+512,index//5*512+512))
            crop=tile.crop((175,245,355,425)).resize((270,270),Image.Resampling.NEAREST)
            comparison.alpha_composite(crop,(column*360,row*600+variant*285+25))
        draw.text((column*360+10,row*600+5),gait+' frame '+str(index)+' / before then after',fill='white')
comparison.save(out/'ground-thigh-plate-expansion-keyposes.png')
print('80 frame previews saved. Original sheets and Unity assets unchanged.')
