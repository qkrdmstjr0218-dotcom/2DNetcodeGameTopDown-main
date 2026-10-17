# 직접 조작 하단 사격 시험

씬: `Assets/RightWalkPrototype/SideScrollFrontPlayableTrial.unity`

최신 더블점프(2026-10-17): 사용자 승인으로 이 씬만 `SideScrollMapPreview.allowDoubleJump=true`. Space를 다시 누르면 공중에서 한 번 더 도약한다. `JumpCount`는 첫 점프1/두 번째2이며 착지/위치 초기화에서0이 된다. 발판에서 걸어 떨어지면 공중 점프1회만 남는다. 두 도약 모두 기존 jumpSpeed=5를 사용한다. 측면 점프는 기존 두 자세 사이를 전환 파츠로 보간하고, 공중 하단 사격은 첫 점프의 앞발 선택에 반대되는 승인 v9/반전 자세를 사용한다. 공중 하체 방향 고정은 유지한다. 세 번째 점프는 차단하며 다른 씬은 기본 false이다. 원화/체격/카메라/기존 이동 속도·사거리 변경 없음. 검사와 실제 1·2차 점프 캡처: `LocalTools/Analysis/HomeVerification/DoubleJumpTrial/`.

최신 뒤로 달리기(2026-10-17): 사용자 진행 승인으로 이 씬만 `RunWalkPreview.backwardRunUsesWalk=true`. 조준 방향과 반대로 달릴 때 기존 walk sheet/보행 주기 거리/hip bob을 유지하고, 실제 이동 거리에 따라 뒤로 걷기를 빠르게 재생한다. 현재 씬 속도는 사용자 설정 walk1.6/run2.4를 그대로 두어 보행 재생은 1.5배다. 앞으로 달리기는 기존 run sheet를 쓴다. 속도 판정 hysteresis와 표시 sheet 상태(IsRunning)를 분리해 조준 반전에서도 주기가 초기화되지 않고, 점프/착지는 실제 표시 중인 walk 관절 데이터를 사용한다. 기본값 false로 다른 씬/네트워크 설정은 유지한다. 검사·캡처: `LocalTools/Analysis/HomeVerification/BackwardRunTrial/`.

최신 공중 하체 방향 고정(2026-10-17): 사용자 승인으로 이 씬만 `FrontDownAimAirLegs.keepTakeoffLegFacing=true`. 공중 하단 사격의 승인 하체·중앙 바지 패널·안쪽 허벅지 천을 별도 표시 그룹으로 묶고, 이륙 시 방향을 기억해 부모의 조준 좌우 반전을 상쇄한다. 상체/양옆 허리 장비/머리/팔/총은 계속 조준 방향을 따른다. 앞발 기반 자세 선택은 유지하며, 공중 일반 조준으로 나갔다 하단 사격에 재진입해도 같은 하체 방향이다. 원본 그림/메시 좌표/물리/캐릭터 크기는 유지한다. 옵션 false로 기존 전체 반전 복원. `LocalTools/Analysis/HomeVerification/AirLegFacingLock/`에 좌우·두 자세·walk/run 8조건의 하체 world 정점 유지, 허리 시각 비교 및 실제 점프·착지 검사 기록이 있다.

최신 점프 선택(2026-10-17): 사용자가 현재 그림·자세를 잠정 수용한 뒤, 랜덤 대신 점프 직전 앞에 나온 발을 접는 방식의 연결을 승인했다. 이 씬만 `FrontDownAimAirLegs.selectPoseFromTakeoffFoot=true`. 지상 걷기/달리기의 원본 Joints 발목 X 위치로 앞발을 기억하고, 이륙 시 승인 v9 하체의 원본/UV 좌우 반전 중 하나를 고른다. 정지 시 마지막 걸음 선택을 유지하며 이력이 없으면 기존 mirrorApprovedLegs 기본값을 쓴다. 공중 조준 전환·하단 재진입·방향 전환 중에는 다시 뽑지 않는다. 기존 일반 측면 점프 선택/물리·그림·총·체격·카메라는 유지한다. 더블점프와 뒤로 달리기는 별도 후속 작업이다. 전 프레임 160조건과 실제 좌우 walk/run 점프·착지 검사(75 착지 프레임, 누락0)는 `LocalTools/Analysis/HomeVerification/TakeoffFootTrial/` 참고.

최신 색 복원: 이동 하단 사격에서 하체 흰 파츠가 검게 되는 원인이었던 MovingWaistUnderlay 색 억제를 제거했다. 이동 표시도 원래 gait의 재질/색/texture를 그대로 사용하며 착지용 각 파츠 역시 원래 재질을 유지한다. 이전의 '중복 복부 어둡게 처리'는 더 이상 현재 상태가 아니다. 상체 경계 정리는 유지한다. `LocalTools/Analysis/HomeVerification/MovingWaistColorFix/` 참고.

최신 경계 정정: 사용자가 좁은 흰 사타구니 파츠와 양옆 검은 의상은 하체라고 정정했다. 이전의 '하복부 장식까지 상체' 분리는 잘못된 것이다. 현재 이동 상체에는 복부·벨트/버클·양옆 장비만 복사하며, 벨트 아래 바지 영역은 제외한다. 장비 윤곽도 다시 잘라 섞여 들어온 흰 허벅지 파츠를 제거했다. 기존 이동 하체 및 정지/공중 원화는 유지한다. 원본 픽셀 경계 검사와 좌우 보행 캡처는 `LocalTools/Analysis/HomeVerification/MovingWaistSplitFix/` 참고. 뒤로 달리기=뒤로 걷기 빠른 재생 요청은 후속 메모에 재확인했으며 아직 미구현이다.

최신 상체·허리 정리: 사용자 요청으로 이동 하단 사격에서도 정면 몸통/벨트/하복부 흰 장식/치마형 파우치를 이어서 유지한다. 잘린 파우치 조각이 생기지 않게 밑단 윤곽을 따라 분리했고, 그 뒤의 중복된 측면 복부는 이동 전용 MovingWaistUnderlay 재질로 어두운 옷감처럼 처리한다. 기존 native gait mesh를 공유하는 별도 표시만 사용하며 원본 PNG/걷기 mesh/확정 정지·공중/총/루트·콜라이더는 수정하지 않았다. 착지 전환용 8개 파츠 표시와 원래 재질 복원도 확인했다. `LocalTools/Analysis/HomeVerification/MovingWaistTrial/`에 비교 이미지와 검사 기록이 있다. 시점 연결의 자연스러움은 사용자 확인 대상이다.

후속 버그 수정: 이동을 유지하며 착지할 때 하반신이 사라지는 현상을 재현했다. 착지 동안 native gait는 Waist만 표시하고 별도 전환 다리 8개를 사용하는데, 이동 정면 경로가 gait만 다시 켜던 문제였다. FrontDownAimMovingLegs가 착지 파츠도 함께 복원하도록 수정했다. 실제 좌우 walk/run 4조건의 착지 보간 63프레임에서 누락0, 다른 정면 자세/일반 측면의 visibility도 확인. `LocalTools/Analysis/HomeVerification/MovingDownLandingFix/` 참고. 허리 치마를 상체에 남기는 디자인 제안은 메모이며 이번에는 적용하지 않았다.

현재 이동 하단 사격 시험: 사용자 요청으로 이 씬만 `allowMovingDownAim=true`, `movingLegs=FrontDownAimMovingLegs`를 추가했다. 하체는 원래 측면 걷기/달리기 프레임(허리·장비 포함), 상체·머리·팔·총은 기존 정면 하단 리그를 재사용한다. 기존 HipLink bob을 정면 상체/총과 조준 역변환에 반영한다. HUD 오른쪽 `이동 중 정면 하단 사격 시험`을 끄면 이전 이동 하단 동작으로 복귀한다. 허리의 정면/측면 연결은 임시 비교 상태이며 최종 원화가 아니다. 정지·공중/원본 PNG/루트·콜라이더는 보존했다. 실제 좌우 walk/run/Fire/옵션 전환/점프·착지·정지 복귀 PASS, 근거는 `LocalTools/Analysis/HomeVerification/MovingDownTrial/`. 점프 랜덤/더블점프는 미구현이다.

최신 대칭 수정: 사용자가 second-v1의 차이가 과하다고 지적하여, 현재는 **첫 v9 하체만 UV 좌우 반전**한다. material=FrontDownAimAirApprovedV9, bounds=(420,735,700,1180), mirrorApprovedLegs=true. 처음 자세의 다리 길이/높이/굽힘을 그대로 사용하며 상체/골반 장비/총은 반전하지 않는다. 더블점프 기능 없이 기존 점프의 공중 하단 사격 그림만 바꾼 상태다. 원래 첫 자세 복원은 mirrorApprovedLegs=false. 후속 이력은 LocalTools/Analysis/HomeVerification/AirFrontTrial/REVIEW-STATUS.ko.md 맨 위가 최신이다.

현재 공중 시안(후속 교체 요청): 사용자가 두 번째 점프 그림을 우선 기존 점프에 넣어 비교하도록 요청하여 `FrontDownAimAirSecondV1.mat`를 사용한다. 더블점프 기능/횟수/물리 코드는 추가하지 않았다. 기존 v9는 보존하고 공중 하단 사격 그림만 교체했다. 새 texture crop bounds는 (410,700,710,1210). 이전 v9 복원은 material=FrontDownAimAirApprovedV9, bounds=(420,735,700,1180). 다른 등록값과 확정 지상 자세는 그대로다. 새 그림은 긴 쪽 발이 더 낮아 상승 첫 부분에 바닥 아래로 일부 겹치는 점이 있으며 사용자 비교 대상이다. 실제 점프·발사·착지 복귀 검사는 `second-v1-integration.txt` PASS.

이전 사격 캐릭터가 있는 `SideScrollRunRecoveryTrial.unity`를 별도 씬으로 복사했다. 기존 걷기·달리기·점프·착지·마우스 사격을 보존하고 새 정면 하단 리그와 의상/발/머리 시험값을 연결했다. 자동 비교 스크립트는 없다.

- Game 화면을 클릭해 입력 포커스를 둔다.
- A/D 또는 좌우 방향키: 이동. Shift: 달리기. Space: 점프.
- 마우스: 조준. 좌클릭 유지: 기존 임시 탄환 연사.
- 멈춰서 아래를 조준하면 정면 하단 -55~-90도 자세로 전환한다. 몸 안쪽 커서도 지원한다.
- 지상 수평 이동 중에는 기존 측면 자세를 사용한다. 이 씬에서는 공중 수평 이동 중에도 정면 하단 사격이 가능하다. 다른 각도의 기존 공중 사격/점프는 유지한다.

권장 확인 순서: 측면 사격 → 이동하며 사격 → 멈춰 하단 사격 → 아래를 겨눈 채 점프 → 측면 조준으로 복귀 → 반대 방향 반복.

전환은 실제 `RightFacingAimRig`가 처리한다. 중간 시점 원화나 전신 변형을 추가한 것이 아니므로, 시점/총 크기/총구 위치 변화의 자연스러움은 아직 조정·사용자 검토 대상이다.

비율 후속 수정: 이 씬에서는 머리 확대 1.08을 중단하고 1.0으로 되돌렸다. 의상 생성에서 원본보다 올라갔던 벨트/하복부 장식은 원본 픽셀을 그대로 아래로 64 source px 옮기고, 그 위 복부는 원본 atlas에서 복구했다. 새 material/atlas `FrontDownAimPlayableWaistV2.mat`, `front-down-atlas-playable-waist-v2.png`를 사용한다. 허벅지/종아리/무릎/발 및 루트/콜라이더 크기는 이 수정에서 바꾸지 않았다. 측면과 정면 -55도 비교 캡처에서 머리 꼭대기 차이는 약 13px→3px로 줄었으나 자연스러운 연결이라는 최종 승인은 아니다.

검증: `LocalTools/Analysis/HomeVerification/PlayableTrial/`에 실제 이동·정지·하단 발사·점프·복귀 캡처와 검사 기록을 저장했다. 초기 점프 중 원본/정면 그림 겹침도 확인했다. `Fire()`가 실제 탄환을 추가하고 하단 경로가 바닥을 만나는지 확인했다.

2026-10-17 허벅지/골반 시험(사용자 진행 요청): 이 씬에서만 `thighShorteningPixels=56`, `headScale=1.06`을 적용했다. source y720~905의 허벅지 구간을 56px 짧게 하고, 그 위 골반·상체·머리·팔·총은 같은 거리만큼 수직 이동한다. y905 아래 무릎·종아리·발, 가로 폭, 루트/콜라이더는 유지한다. 머리는 목 기준 균등 확대다. 원본 PNG/JSON은 수정하지 않았다. 이전 전신 비율 워프를 복구한 것이 아니며, 어깨 높이/허벅지 길이를 비교할 제한된 시험값이다. 원복은 shortening=0, headScale=1. 다른 씬의 shortening 기본값은 0이다.

`LocalTools/Analysis/HomeVerification/ThighTrial/comparison.png`는 측면 / 변경 전 / 변경 후 순서다. 무릎·발 좌표 유지, 총 길이 유지, 좌우 각 3개 조준점의 변경된 총구→타깃 역변환, 실제 `Fire()` 탄환 시작점을 확인했다. 전환 자연스러움과 의상 압축 정도는 사용자 확인 대상이다.

2026-10-17 머리/밝기 후속 시험: 이 씬의 `headScale`을 1.06→1.10으로 변경했다. material은 별도 `FrontDownAimPlayableToneV3.mat`로 교체했다. `FrontHighlightTone.shader`에서 밝은 픽셀만 휘도에 따라 최대 8% 낮추며, 검은 윤곽·투명도와 원본 atlas는 보존한다. 허벅지 조정값 56 및 다른 기하/조작은 유지한다. 밝기 원복은 material의 Highlight Reduction을 0으로 하거나 이전 WaistV2 material을 사용한다. `LocalTools/Analysis/HomeVerification/HeadToneTrial/`에 -55/-90도 캡처, 측면/변경 전/변경 후 비교, 같은 머리 크기의 톤 켜기/끄기 픽셀 검사를 저장했다. 셰이더 오류 없음, 밝아진 픽셀 0, 투명도 변경 0을 확인했다. 최종 색감/크기 수용은 사용자 확인 대상이다.

이후 사용자: 지상 하단 사격 비율·자세의 위화감이 거의 없어졌다고 평가했다. 이 지상 버전을 보존하고 공중 작업을 분리한다.

최신 공중 원화 적용(2026-10-17 후속): 여러 검토 후 사용자가 `knee-volume-review-v9.png`를 수용하고 Unity 적용을 요청했다. 시험 씬의 `FrontDownAimAirLegs.approvedLegMaterial`에 `FrontDownAimAirApprovedV9.mat`를 지정했다. 승인 그림에서 하체만 추출한 280×445 투명 PNG를 사용하며 상체/골반 장비/하복부 흰 장식/머리/팔/총은 기존 atlas와 등록을 보존한다. 기존 허벅지 흰 파츠 중복은 장비 아래 절단 경계를 분리해 제거했다. 총에 가려져 있던 천은 기존 atlas를 하체 뒤에 재사용했다. PNG는 무압축, 밉맵 없음, bilinear/clamp이며 원본 픽셀을 확대 재생성하지 않았다. root/콜라이더/PPU693.421/headScale1.10/thighShortening56 및 지상 ToneV3는 유지했다.

현재는 아래 초기 공중 회전 파츠가 아니라 승인 v9 하체를 표시한다. 원화 적용 후 실제 좌우 이동 점프의 상승/하강/하단 재진입, 일반 조준 복귀, 실제 탄환의 총구/방향, 이동 착지/정지 복귀 검사를 통과했다. 지상 전후 캡처는 픽셀 동일. `LocalTools/Analysis/HomeVerification/AirFrontTrial/approved-v9-integration.txt`와 `approved-*.png` 참고. 사용자의 실제 조작 후 자연스러움 평가는 남아 있다.

2026-10-17 공중 연결: 이동 점프의 실제 Rigidbody 속도 (1.6, 0.29)에서 수평속도 제한 때문에 정면 전환이 거부되는 것을 재현했다. 이 씬에서만 `allowAirborneDownAim=true`로 설정하여 공중에는 해당 제한을 적용하지 않는다. 지상 이동 제한과 다른 씬 기본값(false)은 유지한다.

공중 전용 `FrontDownAimAirLegs`는 같은 atlas의 몸통·골반을 유지하고, 원본 허벅지/무릎과 종아리/부츠 파츠를 관절에서 회전한다. 종아리는 강체 회전하며 휘는 변형은 없다. 생성 이미지 두 장은 종아리 축 및 비율/등록 문제로 모두 미적용했다. 이 공중 모양은 사용자 검토 전의 시험 자세다. 컴포넌트의 left/right는 그림의 source 좌우 기준이다.

검증: `LocalTools/Analysis/HomeVerification/AirFrontTrial/integration.txt` 및 상승/하강/착지 캡처. 실제 좌우 이동 점프의 상승·하강 정면 진입, 실제 탄환의 변경된 총구 시작점, 이동 착지의 측면 복귀, 정지 후 지상 정면 복귀, 일반 조준 복귀를 확인했다. 90도 캡처도 별도 저장했다. 초기 테스트는 고정된 먼 타깃이 캐릭터 이동으로 90도 범위를 벗어나는 조건 및 장애물에 정지한 상태를 이동으로 간주하는 가정 때문에 실패했으며, 실제 조준 방향/속도 기준으로 수정한 검사에서 통과했다. 생성 시안 PNG나 새 리그를 프로덕션/네트워크 플레이어에 연결하지 않았다.
