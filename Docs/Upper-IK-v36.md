# v36: 머리 추적 + 양팔 IK

머리(머리카락 포함), 몸통/허리옷, 총, 양팔의 위팔/아래팔/손을 분리했습니다. 어깨는 몸통 좌표에 고정하고 손은 총의 Trigger Hand Grip/Support Hand Grip 위치를 따릅니다. 머리는 목 기준으로 조준 각도의 75%를 따라갑니다.

현재 각도 범위는 ±12도입니다. 위팔/아래팔은 길이를 보존하는 두 관절 해석식 IK이며, 손 이미지는 총 회전을 따릅니다. 걷기 20프레임과 조준 25각도 조합 500개에서 양손 도달, 팔 길이, 몸통 고정, 총구의 목표점 정렬을 검증했습니다.

배/겨드랑이처럼 원래 팔에 가려진 몸통 원화는 아직 보강하지 않았습니다. 조준 범위를 크게 넓히려면 해당 원화와 방향별 실루엣 보강이 필요합니다. 파츠 경계 역시 현 시점에서는 원본을 잘라 사용한 시제품입니다.

재현: LocalTools/Analysis/split_upper_v36.py → Tools/Right Walk Prototype/Build Sprite Sheet Demo.
소스: RightFacingAimRig.cs, RightFacingArmIK.cs.
프리팹/씬 및 Exports/RightWalkSheet.unitypackage에 반영했습니다.
