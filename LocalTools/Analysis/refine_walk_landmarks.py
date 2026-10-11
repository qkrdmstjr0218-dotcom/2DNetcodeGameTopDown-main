from pathlib import Path
import json, re
p=Path('C:/Users/82102/.codex/visualizations/2026/10/09/01a12032-7676-7571-845e-57aac0c34cd2/walk-landmarks.html')
s=p.read_text(encoding='utf-8-sig')
# Approximate manual readings of the second user markup, not silhouette extrema.
poses=[
([[28,71],[25,87],[23,100],[34,103]],[[30,68],[36,78],[27,91],[34,96]]),
([[29,71],[25,86],[22,99],[33,102]],[[30,68],[39,78],[39,93],[49,94]]),
([[30,70],[24,85],[17,98],[27,103]],[[31,68],[42,81],[49,94],[58,90]]),
([[32,71],[36,81],[19,88],[23,101]],[[33,71],[38,86],[38,98],[49,100]]),
([[26,71],[33,83],[23,100],[33,104]],[[24,71],[25,87],[20,100],[29,102]]),
([[28,73],[36,86],[33,100],[44,103]],[[26,68],[26,83],[19,96],[29,99]]),
([[30,74],[40,85],[43,99],[53,95]],[[28,72],[23,85],[15,95],[25,103]]),
([[34,74],[39,89],[37,101],[48,104]],[[32,75],[28,87],[19,94],[24,101]])]
captions=['B: 앞쪽 무릎 → 뒤쪽 발목 → 앞쪽 발끝','B 무릎과 종아리 윤곽 돌출점을 구분','A 뒤 / B 앞','A: 앞쪽 무릎 → 뒤쪽 발목 → 아래쪽 발끝','골반 아래에서 두 다리 교차','B: 뒤로 디딘 다리의 무릎과 발목 구분','A 앞 / B 뒤 · 3번과 역할 교대','B 무릎은 윤곽 돌출점이 아닌 관절 꺾임 위치']
data=[dict(a=a,b=b,caption=c) for (a,b),c in zip(poses,captions)]
s=re.sub(r' const data=\[.*?\];',' const data='+json.dumps(data,ensure_ascii=False)+';',s,flags=re.S)
s=s.replace('사용자 표시 기준 · 발목과 발끝 분리','보강한 표시 기준 · 관절 중심 연결')
s=s.replace('A: 가까운 다리 · 원','A: 보라색 표시에 대응 · 원').replace('B: 먼 다리 · 네모','B: 노란색 표시에 대응 · 네모')
lines=s.splitlines()
s='\n'.join(line for i,line in enumerate(lines) if i==0 or line!=lines[i-1])+'\n'
p.write_text(s,encoding='utf-8')
Path('LocalTools/Analysis/right-walk-reference-joints.json').write_text(json.dumps({'coordinates':'60x110 cell, top-left origin; hip, knee, ankle, toe','source':'user second markup 2026-10-09 234213','status':'manual approximate readings; occlusion and identity uncertain','frames':data},ensure_ascii=False,indent=2),encoding='utf-8')
assert s.count('const bones=')==1
print('Updated eight poses from second user markup.')

