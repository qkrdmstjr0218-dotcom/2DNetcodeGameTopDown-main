from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import numpy as np,json,math
root=Path.cwd();p=root/'Assets/RightWalkPrototype/Art';ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')};s=(root/'LocalTools/Analysis/bake_walk_v32.py').read_text().split('frames=[];points=[]')[0];exec('\n'.join(l for l in s.splitlines() if '.save(' not in l),ns)
new=Image.open(p/'WalkPart-Boot-Refined-v49.png').convert('RGBA')
new=new.transform((512,512),Image.Transform.AFFINE,(1/.95,0,343.7-342/.95,0,1/.927,430-430/.927),Image.Resampling.BICUBIC)
pts=json.load(open(root/'LocalTools/Analysis/baked-walk-v32.json'));sheet=Image.open(p/'RightWalkSheet-Legs-BootOnly-v53.png').convert('RGBA')
def render(i,boot,shift=True):
 im=Image.new('RGBA',(512,512))
 for side in ['far','near']:
  f=pts[i][side];hip=ns[side+'Hip'];knee=ns[side+'Knee'];ankle=ns[side+'Ankle'];h=f['hip'];k=f['knee'];a=f['ankle'];theta=ns['angle'](k,a)-ns['angle'](knee,ankle)
  ns['draw_part'](im,ns[side+'Thigh'],hip,h,ns['angle'](h,k)-ns['angle'](hip,knee))
  calf=ns[side+'Calf']
  ns['draw_part'](im,calf,knee,k,theta)
  if boot is not None:
   offset=2 if (shift and boot is new and side=='far') else 10
   ns['draw_part'](im,boot,ns['nearAnkle'],(a[0]+offset,a[1]),ns['ankle'](i/20+(.5 if side=='far' else 0))[1])
  ns['draw_part'](im,ns['nearKneePatch'],ns['nearKnee'],k,ns['angle'](k,a)-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
 return im
out=Image.new('RGBA',sheet.size)
for i in range(20):
 box=(i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512);current=np.array(sheet.crop(box));old=np.array(render(i,new,False));replacement=np.array(render(i,new,True));scope=np.any(old!=replacement,axis=2);current[scope]=replacement[scope];out.alpha_composite(Image.fromarray(current),(box[0],box[1]))
out.save(p/'RightWalkSheet-Legs-LeftBootShift-v56.png')
preview=Image.new('RGBA',(1200,320),'white')
for j,i in enumerate([0,5,10,15]):preview.alpha_composite(out.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512)).crop((190,320,415,478)).resize((300,211)),(300*j,0))
preview.save(root/'LocalTools/Analysis/left-boot-shift-v56.png')


