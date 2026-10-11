from pathlib import Path
import json,re,html,base64,io
from PIL import Image
p=Path('C:/Users/82102/.codex/visualizations/2026/10/09/01a12032-7676-7571-845e-57aac0c34cd2/walk-landmarks.html')
old=p.read_text(encoding='utf-8-sig')
image=re.search(r'data:image/png;base64,[A-Za-z0-9+/=]+',old).group()
atlas=Image.open(io.BytesIO(base64.b64decode(image.split(',',1)[1]))).convert('RGBA')
if atlas.height>110: atlas=atlas.crop((0,440,480,550))
buf=io.BytesIO();atlas.save(buf,format='PNG',optimize=True)
image='data:image/png;base64,'+base64.b64encode(buf.getvalue()).decode()
data=json.loads(Path('LocalTools/Analysis/right-walk-reference-joints.json').read_text(encoding='utf-8'))['frames']
def pose(i,main=False):
    attrs=f' data-main-frame="{i}"'+(' hidden' if i else '') if main else ''
    parts=[f'<div class="walk-pose"{attrs}><img alt="参考 프레임 {i+1}" src="{image}" style="left:{-100*i}%"><svg viewBox="0 0 60 110" role="img" aria-label="{i+1}번 관절 표시">']
    for key,series in [('b',2),('a',1)]:
        pts=data[i][key]
        parts.append(f'<g stroke="var(--viz-series-{series})" fill="var(--viz-series-{series})">')
        for j in range(3):
            (x,y),(u,v)=pts[j:j+2]
            dash=' stroke-dasharray="1.2 1.2"' if j==0 else ''
            parts.append(f'<line x1="{x}" y1="{y}" x2="{u}" y2="{v}" stroke-width="{1.15 if j==2 else .65}"{dash}/>')
        for x,y in pts[1:3]:
            parts.append(f'<circle cx="{x}" cy="{y}" r=".9"/>' if key=='a' else f'<rect x="{x-.9}" y="{y-.9}" width="1.8" height="1.8"/>')
        parts.append('</g>')
    parts.append('</svg></div>')
    return ''.join(parts)
t=Path('LocalTools/Analysis/walk-landmarks-safe.template.html').read_text(encoding='utf-8')
t=t.replace('__BUTTONS__',''.join(f'<button class="btn" type="button" data-frame="{i}" aria-label="프레임 {i+1}" aria-pressed="'+('true' if i==0 else 'false')+f'">{i+1}</button>' for i in range(8)))
t=t.replace('__MAIN__',''.join(pose(i,True) for i in range(8)))
t=t.replace('__GALLERY__',''.join(f'<figure>{pose(i)}<figcaption>{i+1}</figcaption></figure>' for i in range(8)))
t=t.replace('__CAPTIONS__',json.dumps([f['caption'] for f in data],ensure_ascii=False))
assert len(t.encode())<1000000
p.write_text(t,encoding='utf-8')
print('DOM/SVG version built; joints retained; bytes:',len(t.encode()))
