# 노트북 이동 갱신 점검 (2026-10-12)

Unity 6000.3.18f1, SideScrollTestMap 활성/저장 상태, MCP 연결 확인.
승인 아트, 좌우 반전, 보행 거리, 골반 보정, 카메라 계수는 유지했다.

## 관측

- 속도 1.6, cycleDistance 0.813802: 보행 주기는 약 0.509초, 20장 그림은 평균 약 39.3장/초로 전환된다.
- Rigidbody2D Interpolate 활성, 물리 간격 약 0.02초.
- 기존 Update는 body.position의 변위를 읽어 보행을 진행하고, 카메라는 보간된 transform.position을 추적했다.
- baseline CSV의 안정 이동 관측 475프레임 중 417프레임은 물리 X 변위가 없었다. 최대 물리 X 변화는 약 0.032이다.
- 평지 물리 Y는 -0.1400017~-0.14: 해당 구간에서 루트가 크게 위아래 뛰는 현상은 관측하지 못했다.
- 골반 보정은 기존 하체 자세와 맞춘 최대 2px 변화다. 이 값이 사용자 위화감의 원인인지는 확정하지 않았다.

## 변경 및 확인

SideScrollMapPreview에서 보행을 LateUpdate로 옮기고 보간된 transform.position의 수평 변위로 진행한다. 실행 순서 -20으로 기존 HipLink(90), AimRig(100), ArmIK(110)보다 먼저 적용한다. 물리 이동은 FixedUpdate를 유지한다.

AssetDatabase.Refresh 후 Play에서 이동 2초, 정지 1초를 관측했다. aligned CSV에 791개의 서로 다른 게임 프레임을 기록했다. 표시 변위와 보행 위상 증가의 대응을 확인했고, 마지막에는 vx=0, pose=10, settling=False였다. 컴파일 실패 및 콘솔 오류 0.

기록은 LocalTools/Analysis/notebook-motion-baseline.csv 및 notebook-motion-aligned.csv다. EditorApplication.update에서 서로 다른 frameCount를 관측한 자료로, 모든 렌더 프레임을 보장하지 않는다. 초기 렌더 콜백 시도는 기록이 생성되지 않았다. 최초 수정 후 검사는 스크립트 재임포트 전의 결과여서 제외했다. 이동 중 낮은 발판에 닿아 정지하는 구간도 포함되므로 전체 기록을 순수 등속 보행으로 해석하지 않는다.

## 남은 검토

사용자가 말한 이동 느낌이 해결됐다는 미적 승인은 아직 없다. 20장 그림의 시간적 끊김, 속도/보행 주기, 골반 변화, 카메라 추적을 화면에서 각각 비교해야 한다. 이번에는 원화, 발 위치, 골반 높이, 카메라 계수를 변경하지 않았다. 횡스크롤 방향성도 미확정이다.
