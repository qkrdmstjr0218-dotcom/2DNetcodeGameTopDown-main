from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import numpy as np,json,math
root=Path.cwd();p=root/'Assets/RightWalkPrototype/Art';ns={'__file__':str(root/'LocalTools/Analysis/bake_walk_v32.py')};s=(root/'LocalTools/Analysis/bake_walk_v32.py').read_text().split('frames=[];points=[]')[0];exec('\n'.join(l for l in s.splitlines() if '.save(' not in l),ns)
pts=json.load(open(root/'LocalTools/Analysis/baked-walk-v32.json'));new=Image.open(p/'WalkPart-Boot-Refined-v49.png').convert('RGBA').transform((512,512),Image.Transform.AFFINE,(1/.95,0,343.7-342/.95,0,1/.927,430-430/.927),Image.Resampling.BICUBIC)
fixed={}
for side in ['far','near']:
 a=np.array(ns[side+'Thigh']);y,x=np.indices((512,512));region=(y>249)&(y<311)&(x<(255 if side=='near' else 247));dark=(a[:,:,:3].mean(2)<135)&(a[:,:,3]>180)
 a[region&dark,3]=255
 for j in range(4):
  old=a.copy();nb=np.roll(old,-1,axis=1);take=region&(old[:,:,3]<240)&(nb[:,:,3]>245)&(nb[:,:,:3].mean(2)<115);a[take]=nb[take]
 # Smooth only the newly expanded outer silhouette.
 alpha=np.array(Image.fromarray(a[:,:,3]).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3)));a[:,:,3][region]=alpha[region];fixed[side]=Image.fromarray(a)
# Fill the rear upper corner in the thigh's local coordinates, before outlining it.
for side in ['near','far']:
 a=np.array(fixed[side]);mask=Image.new('L',(512,512));poly=[(231,244),(243,244),(236,280),(229,278)] if side=='near' else [(231,263),(243,267),(228,293),(224,288)]
 ImageDraw.Draw(mask).polygon(poly,fill=255);m=np.array(mask)>0;y,x=np.indices((512,512));valid=(a[:,:,3]>245)&(a[:,:,:3].mean(2)<110)&(x<265)&(y>242)&(y<312);vy,vx=np.where(valid);original=np.array(ns['source'])
 for yy,xx in zip(*np.where(m&(a[:,:,3]<250))):
  color=original[yy,xx,:3]
  if original[yy,xx,3]<245 or color.mean()>120:
   j=np.argmin((vy-yy)**2+(vx-xx)**2);color=a[vy[j],vx[j],:3]
  a[yy,xx,:3]=color;a[yy,xx,3]=255
 fixed[side]=Image.fromarray(a)
# Restore a rounded, fuller rear hip silhouette under the pouch, separate from hole filling.
for side in ['near','far']:
 a=np.array(fixed[side]);control=[(231,245),(211,244),(211,273),(243,296)] if side=='near' else [(232,263),(213,257),(211,286),(238,304)]
 curve=[]
 for t in np.linspace(0,1,40):
  q=(1-t)**3*np.array(control[0])+3*(1-t)**2*t*np.array(control[1])+3*(1-t)*t*t*np.array(control[2])+t**3*np.array(control[3]);curve.append(tuple(q))
 curve += [(254,298),(253,250)] if side=='near' else [(255,302),(252,268)]
 mask=Image.new('L',(512,512));ImageDraw.Draw(mask).polygon(curve,fill=255);m=np.array(mask)>0;y,x=np.indices((512,512));valid=(a[:,:,3]>245)&(a[:,:,:3].mean(2)<115)&(x<267)&(y>245)&(y<315);vy,vx=np.where(valid);original=np.array(ns['source'])
 for yy,xx in zip(*np.where(m&(a[:,:,3]<250))):
  color=original[yy,xx,:3]
  if original[yy,xx,3]<245 or color.mean()>115 or int(color[0])-int(color[1])>18:
   j=np.argmin((vy-yy)**2+(vx-xx)**2);color=a[vy[j],vx[j],:3]
  a[yy,xx,:3]=color;a[yy,xx,3]=255
 fixed[side]=Image.fromarray(a)
def outline(part):
 a=np.array(part);alpha=Image.fromarray(a[:,:,3]);dilate=np.array(alpha.filter(ImageFilter.MaxFilter(3)));back=np.zeros_like(a);back[:,:,:3]=[8,9,10];back[:,:,3]=dilate
 return Image.alpha_composite(Image.fromarray(back),part)
for side in ['near','far']:fixed[side]=outline(fixed[side])
calves={side:outline(ns[side+'Calf']) for side in ['near','far']}
def render(i,repaired=False,sideOnly=None):
 im=Image.new('RGBA',(512,512))
 for side in ([sideOnly] if sideOnly else ['far','near']):
  f=pts[i][side];hip=ns[side+'Hip'];knee=ns[side+'Knee'];ankle=ns[side+'Ankle'];h=f['hip'];k=f['knee'];a=f['ankle'];th=ns['angle'](h,k)-ns['angle'](hip,knee);theta=ns['angle'](k,a)-ns['angle'](knee,ankle)
  ns['draw_part'](im,fixed[side] if repaired else ns[side+'Thigh'],hip,h,th);ns['draw_part'](im,calves[side] if repaired else ns[side+'Calf'],knee,k,theta);ns['draw_part'](im,new,ns['nearAnkle'],(a[0]+(2 if side=='far' else 10),a[1]),ns['ankle'](i/20+(.5 if side=='far' else 0))[1]);ns['draw_part'](im,ns['nearKneePatch'],ns['nearKnee'],k,ns['angle'](k,a)-ns['angle'](ns['nearKnee'],ns['nearAnkle']))
 return im
src=np.array(Image.open(p/'RightWalkSheet-Legs-LeftBootShift-v56.png'));orig=np.array(Image.open(p/'RightWalkSheet-Legs-v40.png'));line=np.array(Image.open(p/'RightWalkSheet-Legs-StraightLine-v52.png'));oldLine=np.any(line!=orig,axis=2);src[oldLine]=orig[oldLine];out=Image.new('RGBA',(2560,2048))
base=np.array(pts[0]['far']['knee']);t0=ns['angle'](pts[0]['far']['knee'],pts[0]['far']['ankle'])
for i,f in enumerate(pts):
 tile=src[i//5*512:(i//5+1)*512,i%5*512:(i%5+1)*512].copy();old=np.array(render(i));repair=np.array(render(i,True));scope=np.any(old!=repair,axis=2);tile[scope]=repair[scope]
 # Far-leg seam is clipped by the actual near-leg alpha, before final composition.
 k=np.array(f['far']['knee']);t=ns['angle'](k,f['far']['ankle'])-t0;m=np.array([[math.cos(t),-math.sin(t)],[math.sin(t),math.cos(t)]]);a=k+m@(np.array([255.,351.])-base);b=k+m@(np.array([239.,377.])-base);v=b-a;L=np.linalg.norm(v);n=np.array([-v[1],v[0]])/L;y,x=np.indices((512,512));u=((x-a[0])*v[0]+(y-a[1])*v[1])/(L*L);d=(x-a[0])*n[0]+(y-a[1])*n[1];near=np.array(render(i,True,'near'));cov=np.clip(1.3-abs(d),0,1)*((u>=0)&(u<=1))*(1-near[:,:,3]/255.)*(tile[:,:,3]/255.);tile[:,:,:3]=(tile[:,:,:3]*(1-cov[:,:,None])+np.array([23,27,28])*cov[:,:,None]).round().astype('uint8');out.alpha_composite(Image.fromarray(tile),(i%5*512,i//5*512))
out.save(p/'RightWalkSheet-Legs-MovingThighFill-v64.png')
preview=Image.new('RGBA',(1200,500),'white')
for j,i in enumerate([0,5,10,15]):preview.alpha_composite(out.crop((i%5*512,i//5*512,(i%5+1)*512,(i//5+1)*512)).crop((195,235,355,425)).resize((300,356)),(j*300,0))
preview.save(root/'LocalTools/Analysis/moving-thigh-fill-v64.png')
