from pathlib import Path
import re, json

p=Path('C:/Users/82102/.codex/visualizations/2026/10/09/01a12032-7676-7571-845e-57aac0c34cd2/walk-landmarks.html')
s=p.read_text(encoding='utf-8')
data=[
dict(a=[[26,73],[23,86],[21,98],[31,102]],b=[[31,73],[36,79],[28,91],[34,94]],hiddenB=True,caption='B 무릎은 앞으로, 발목은 무릎 뒤쪽'),
dict(a=[[29,73],[24,86],[21,98],[31,102]],b=[[32,73],[41,79],[41,92],[50,94]],caption='B 발목과 발끝을 분리'),
dict(a=[[29,74],[22,87],[16,97],[25,102]],b=[[33,74],[43,85],[49,94],[57,90]],caption='A 뒤 / B 앞 · 발끝은 종아리 선에 포함하지 않음'),
dict(a=[[32,73],[36,81],[24,90],[21,101]],b=[[35,74],[38,87],[38,98],[49,101]],caption='A: 무릎은 앞쪽, 발목은 뒤쪽 · 아래로 향한 발은 별도'),
dict(a=[[26,73],[32,82],[24,97],[33,102]],b=[[29,74],[22,87],[22,98],[31,102]],hiddenB=True,caption='A 무릎을 앞으로 회수 · 겹치는 B는 추정'),
dict(a=[[29,73],[34,86],[34,98],[45,102]],b=[[26,74],[24,86],[23,94],[31,96]],caption='A 발은 앞쪽으로 / B 발은 뒤쪽으로'),
dict(a=[[31,74],[37,86],[43,98],[53,94]],b=[[27,74],[18,87],[15,96],[23,100]],caption='A 앞 / B 뒤 · 3번과 다리 역할 교대'),
dict(a=[[34,74],[36,87],[36,98],[47,102]],b=[[29,73],[33,84],[20,91],[18,100]],caption='B: 무릎은 앞쪽, 발목은 뒤쪽 · 발은 아래로')
]
s=re.sub(r' const data=\[.*?\n \];', ' const data='+json.dumps(data,ensure_ascii=False)+';', s, flags=re.S)
s=s.replace('실제 윤곽에 맞춘 관절 검토','사용자 표시 기준 · 발목과 발끝 분리')
s=s.replace('표시 겹치기</span></label>', '표시 겹치기</span></label><label class="form-check"><input class="form-check-input" type="checkbox" data-bones><span class="form-check-label">뼈대만 보기</span></label>')
s=s.replace("const show=root.querySelector('[data-overlay]'),play=", "const bones=root.querySelector('[data-bones]');\n const show=root.querySelector('[data-overlay]'),play=")
s=s.replace('  c.drawImage(image,60*i,440,60,110,x,y,60*s,110*s);','  if(!bones.checked)c.drawImage(image,60*i,440,60,110,x,y,60*s,110*s);')
s=s.replace('if(!show.checked)return;', 'if(!show.checked&&!bones.checked)return;')
s=s.replace('a=point(points[2]);','a=point(points[2]),t=point(points[3]);')
s=s.replace('   [k,a].forEach', '   c.lineWidth=labels?4:3;c.beginPath();c.moveTo(...a);c.lineTo(...t);c.stroke();c.lineWidth=labels?2:1.5;\n   [k,a].forEach')
s=s.replace(' show.onchange=draw;', ' show.onchange=draw;bones.onchange=draw;')
s=s.replace("const tone=n=>getComputedStyle(root).getPropertyValue(n).trim();", "const probe=document.createElement('span');probe.hidden=true;root.appendChild(probe);\n const tone=n=>{probe.style.color='var('+n+')';return getComputedStyle(probe).color;};")
s=s.replace('골반 연결점·가려진 지점: 점선/빈 표시. 다리 A/B의 연속 대응은 추정.', '가는 선: 허벅지·종아리 / 굵은 선: 발목→발끝. 골반·가려진 지점과 A/B 대응은 추정.')
p.write_text(s,encoding='utf-8')
Path('LocalTools/Analysis/right-walk-reference-joints.json').write_text(json.dumps({'coordinates':'60x110 cell, top-left origin; hip, knee, ankle, toe','status':'manual estimates guided by user markup; occluded joints uncertain','frames':data},ensure_ascii=False,indent=2),encoding='utf-8')
print('Updated 8 frames; each leg has separate ankle and toe.')
