from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json
root=Path.cwd();p=root/'Assets/RightWalkPrototype/Art';ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')};s=(root/'LocalTools/Analysis/bake_walk_v32.py').read_text().split('frames=[];points=[]')[0];exec('\n'.join(l for l in s.splitlines() if '.save(' not in l),ns)
patches={}
polys={'near':[(234,247),(245,249),(259,308),(250,315),(227,279)],'far':[(233,261),(247,269),(241,299),(220,316),(202,301)]}
for side,poly in polys.items():
 a=np.array(ns['source']);mask=Image.new('L',(512,512));ImageDraw.Draw(mask).polygon(poly,fill=255);m=np.array(mask)>0
 # Only opaque existing black trouser texels are used as the fill source.
 y,x=np.indices((512,512));valid=m&(a[:,:,3]>245)&(a[:,:,:3].mean(2)<100)&(np.abs(a[:,:,0].astype(int)-a[:,:,1].astype(int))<18);vy,vx=np.where(valid);out=np.zeros_like(a)
 for yy,xx in zip(*np.where(m)):
  j=np.argmin((vy-yy)**2+(vx-xx)**2);out[yy,xx,:3]=a[vy[j],vx[j],:3];out[yy,xx,3]=255
 patches[side]=Image.fromarray(out)
pts=json.load(open(root/'LocalTools/Analysis/baked-walk-v32.json'));src=Image.open(p/'RightWalkSheet-Legs-EdgeCleanup-v58.png').convert('RGBA');out=Image.new('RGBA',src.size)
for i,f in enumerate(pts):
 im=Image.new('RGBA',(512,512))
 for side in ['far','near']:
  h=ns[side+'Hip'];k=ns[side+'Knee'];ns['draw_part'](im,patches[side],h,f[side]['hip'],ns['angle'](f[side]['hip'],f[side]['knee'])-ns['angle'](h,k))
 im.alpha_composite(src.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512)));out.alpha_composite(im,(i%5*512,i//5*512))
out.save(p/'RightWalkSheet-Legs-OpaqueThigh-v59.png')
preview=Image.new('RGBA',(1200,340),'white')
for j,i in enumerate([0,5,10,15]):preview.alpha_composite(out.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512)).crop((215,235,320,345)).resize((300,314)),(j*300,0))
preview.save(root/'LocalTools/Analysis/opaque-thigh-v59.png')
