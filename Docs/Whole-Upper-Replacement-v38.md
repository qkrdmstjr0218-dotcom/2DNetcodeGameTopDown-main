# v38 몸통 전체 교체와 머리/팔 복원

v37은 생성 몸통의 일부만 덧댔고 이전 절단 파츠를 위에 남겨 결손이 유지되었습니다. 생성 이미지에 매우 낮은 alpha의 넓은 여백이 있어 단순 getbbox로 크기를 잡는 문제도 있었습니다.

v38은 불투명 영역을 기준으로 원화를 정렬하고 생성했던 몸통 전체를 적용합니다. 머리의 턱과 짧은 목, 가까운 윗팔의 어깨/팔꿈치, 먼쪽 팔 전체를 별도 원화로 복원했습니다. 먼쪽 어깨는 얼굴 아래로 노출되지 않게 몸통 뒤 (250,160)에 두었습니다. 새 원화의 관절 좌표를 기존 IK에 등록하여 사용합니다.

Unity의 위/정면/아래 렌더를 확대 확인했고 ±12도 전체에서 양손이 총의 Grip에 도달함을 확인했습니다. 실제 씬의 몸통 Sprite는 RightWalk-Torso-v38입니다. 데모 씬, 프리팹, Exports/RightWalkSheet.unitypackage에 저장했습니다.

재현: LocalTools/Analysis/assemble_upper_v38.py. 원화: Head-Repair-Reference-v38, Near-Upper-Repair-Reference-v38, Far-Arm-Repair-Reference-v38, Torso-Repair-Reference-v37. 기존 버전은 보존합니다.

단일 포즈 원화보다 머리/팔의 일부 모양이 바뀌었습니다. 큰 각도 조준이나 8방향까지 완성된 상태는 아닙니다.
