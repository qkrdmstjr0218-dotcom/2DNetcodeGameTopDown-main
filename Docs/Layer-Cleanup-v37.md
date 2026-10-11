# v37 파츠 경계 수정

- 머리 영역에 포함되어 있던 총의 윗부분을 총 파츠로 돌려놓음.
- 머리/옷깃, 가슴/팔 경계를 다시 자르고 떠 있는 작은 조각 제거.
- 팔꿈치는 원래 이미지의 가장자리 8px가 겹치도록 조정.
- 팔에 가려진 몸통은 Torso-Repair-Reference-v37.png를 정렬하여 원래 몸통 윤곽 안에서 보강. 기존 가슴 패널과 허리옷을 우선 유지.
- 위/정면/아래 ±12도 Unity 렌더를 확대 확인. 팔 IK, 걷기, 조준 계산은 유지.
- 프리팹, 데모 씬, Exports/RightWalkSheet.unitypackage에 반영.
- 재현: LocalTools/Analysis/split_upper_v37.py.

추가적인 경계 보정이 필요할 수 있어 v36 이미지는 그대로 보존했습니다.
