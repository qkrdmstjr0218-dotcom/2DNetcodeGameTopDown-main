from pathlib import Path
import json,re,copy
q=Path('LocalTools/Analysis/right-walk-reference-joints.json')
data=json.loads(q.read_text(encoding='utf-8'))
unchanged=copy.deepcopy(data['frames'][4:])
f=data['frames']
for i,knee in enumerate(([25,83],[26,82],[25,81])):
    f[i]['a'][1]=knee
f[1]['b'][1]=[41,78]
f[1]['b'][2]=[41,93]
f[3]['b'][1]=[38,82]
for i in range(3): f[i]['caption']='파란 무릎 높이 수정'+(' · 붉은 종아리 선 중앙으로 이동' if i==1 else '')
f[3]['caption']='붉은 무릎 높이 수정'
assert f[4:]==unchanged
data['firstFourAdjustment']='User feedback: A knees 1-3 raised 4 px; B knee/ankle 2 moved right 2 px; B knee 4 raised 4 px. Other joints retained.'
q.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
p=Path('C:/Users/82102/.codex/visualizations/2026/10/09/01a12032-7676-7571-845e-57aac0c34cd2/walk-landmarks.html')
s=p.read_text(encoding='utf-8-sig')
s=re.sub(r' const data=\[.*?\];',' const data='+json.dumps(f,ensure_ascii=False)+';',s,flags=re.S)
s=s.replace('5–8번 수정 · 보강본과 원본 정렬','1–4번 무릎 높이·중심선 수정')
p.write_text(s,encoding='utf-8')
print('Adjusted requested joints; frames 5-8 unchanged.')
