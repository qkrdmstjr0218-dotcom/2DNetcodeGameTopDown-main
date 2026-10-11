# v35: 어깨 조준과 고정 몸통

- 머리, 몸통, 허리, 엉덩이쪽 옷은 Torso and Hip Clothing에 고정.
- 두 팔과 총은 Aim Rig 아래에서 어깨 (234,164) 기준 ±12도 회전.
- v32의 20프레임 하체 궤적과 접지 속도는 유지.
- 몸통과 옷의 절단 경계는 원래 v32 upperRaised 마스크를 사용하여 복원.
- 현재 두 팔은 하나의 조준 묶음이다. 팔꿈치마다 별개로 굽히는 리깅은 아직 아님.
- 원화의 가려진 가슴/겨드랑이는 새로 그리지 않아 큰 각도에서는 추가 원화가 필요함.
- 위/정면/아래 조준 및 20프레임에서 몸통 위치와 회전이 고정이고 총구 방향이 목표점을 통과하는지 검증.
- 재현: LocalTools/Analysis/split_arms_v35.py, Unity Tools/Right Walk Prototype/Build Sprite Sheet Demo.
