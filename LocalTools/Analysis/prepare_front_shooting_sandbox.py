from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
OUT = HERE / 'FrontShootingSandbox'
OUT.mkdir(exist_ok=True)
src = Image.open(HERE/'front-proportion-local-trial.png').convert('RGBA')
under = Image.open('C:/Users/PC/.codex/generated_images/01a12d87-d09d-7f20-a556-97e2f4b8fa44/exec-140ab6e5-d38b-4e6e-a532-d0b0f6b3c273.png').convert('RGBA')
weapon = Image.open(HERE/'FrontPartsSourceV3/weapon-folded-crisp-edges.png').convert('RGBA')
weapon.save(OUT/'weapon.png')
weapon_mask = np.array(Image.open(HERE/'FrontPartsSourceV3/weapon-selection-mask.png'))>0
polys = {
    'trigger-upper':[(387,345),(431,345),(454,378),(455,421),(452,450),(447,480),(425,506),(391,507),(366,487),(365,455),(374,416)],
    'trigger-fore':[(387,504),(428,501),(456,519),(478,541),(479,556),(472,578),(469,596),(448,605),(418,586),(393,566),(381,538)],
    'trigger-hand':[(475,542),(490,537),(502,550),(499,611),(487,621),(470,608),(463,583)],
    'support-upper':[(634,380),(674,388),(700,418),(710,460),(706,505),(699,544),(675,574),(642,569),(621,545),(617,508),(620,458)],
    'support-fore':[(624,563),(665,561),(677,588),(656,630),(639,662),(624,695),(606,718),(578,726),(554,708),(556,683),(576,645),(591,610)],
    'support-hand':[(569,699),(594,704),(603,731),(590,766),(573,802),(548,812),(533,791),(535,757),(550,726)],
}
owner=np.zeros(src.size[::-1],dtype=np.uint8)
for index,(name,poly) in enumerate(polys.items(),1):
    mask=Image.new('L',src.size)
    ImageDraw.Draw(mask).polygon(poly,fill=255)
    if name=='support-hand':
        ImageDraw.Draw(mask).polygon([(483,712),(499,702),(504,747),(495,795),(478,790),(470,754)],fill=255)
    selected=(np.array(mask)>0)&~weapon_mask
    owner[selected]=index
pixels=np.array(src)
for index,name in enumerate(polys,1):
    layer=np.zeros_like(pixels)
    layer[owner==index]=pixels[owner==index]
    Image.fromarray(layer).save(OUT/(name+'.png'))
replacement=(owner>0)|weapon_mask
body=pixels.copy()
body[replacement]=np.array(under)[replacement]
# Remove generated shoulder sockets behind the extracted upper-arm roots.
for polygon in [[(370,348),(432,338),(452,391),(445,455),(397,466),(370,424)],
                [(635,378),(683,392),(702,428),(685,491),(636,496),(623,444)]]:
    mask=Image.new('L',src.size);ImageDraw.Draw(mask).polygon(polygon,fill=255)
    selected=np.array(mask)>0
    body[selected]=0
Image.fromarray(body).save(OUT/'body.png')
manifest={'sourceSize':[1024,1536],'pivot':[519,590],'muzzle':[519,939],
          'arms':[{'side':'trigger','shoulder':[416,392],'elbow':[407,493],'wrist':[476,568],'bend':1},
                  {'side':'support','shoulder':[663,434],'elbow':[651,568],'wrist':[580,711],'bend':-1}],
          'previewOnly':True,'unityAssetsChanged':False,
          'limitations':['Manual arm boundaries are provisional.',
                         'Generated torso underlay is local placeholder art, not approved new design.',
                         'Arm art can expose gaps under rotation; reach mismatch is measured, not hidden.',
                         'Only down-aim sector +/-35 degrees is tested; no full Unity gameplay replacement.']}
(OUT/'rig.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(manifest))
