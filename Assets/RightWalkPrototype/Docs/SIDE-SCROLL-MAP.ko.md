# 횡스크롤 시험 맵

SideScrollTestMap.unity는 게임 방향성을 평가하는 별도 시험 씬이다. 기존 오른쪽/좌우 씬은 유지한다. 횡스크롤 확정이나 기존 게임 전체를 전환한 것은 아니다.

Play 후 A/D 또는 좌우 방향키 이동, Space 점프, 마우스 좌우 전환 및 ±45도 조준. 화면의 속도 슬라이더로0.8~2.4 비교, 초기값1.6. 시작 위치 복귀 버튼 제공. 기본 바닥, 좌우 경계벽, 낮은 발판3개, 따라가는 카메라 포함. 충돌/중력은 Rigidbody2D로 처리하고 보행은 실제 수평 이동량으로 진행한다. 공중에서도 현재 하체 그림을 사용하므로 점프 전용 동작은 아직 없다. 사격/적/게임 목표도 없다.

발바닥이 원본 pivot보다 약0.152월드 단위 위에 있으므로 충돌체 하단을 그 높이에 맞췄다. 카메라 orthographicSize=3. 임시 충돌체는 직사각형이며 발의 세부 모양을 따르지 않는다.

메뉴 Tools/Right Walk Prototype/Build Side Scroll Test Map으로 재생성. SideScrollMapPreview가 이동을 담당하고 RightFacingWalker.externalMovement=true로 기존 Transform 이동을 차단한다. externalMovement=false인 기존 두 씬은 기존 동작을 유지한다. 걷기 궤적·원본 PNG는 바꾸지 않았다.

실제 Play모드에서 이동/정지/점프/착지 확인, 최초6.5초 관측2239회에서 정지 안정 구간 vx=0, 점프와 grounded 전환 확인. 원본 좌우 아트에 적용된 점검 기록과 구분한다.
