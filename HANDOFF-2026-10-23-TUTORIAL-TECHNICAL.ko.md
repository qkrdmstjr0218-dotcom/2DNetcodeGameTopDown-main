# 작성 중 튜토리얼 기술 인계 — 2026-10-23

이 문서는 `HANDOFF-2026-10-23-HOME.ko.md`의 보조다. **전체 플레이 검증 전**이며 코드가 존재한다는 사실을 완료 근거로 삼지 않는다.

## 변경 파일과 구조

`Assets/RightWalkPrototype/Scripts/` 기준:

- `ArenaTutorial.cs`: 세션 GameObject에 붙이는 local-only MonoBehaviour, static Instance/Active, Lesson0~6/완료7, 구간별 목표 판정·체크포인트·실제 NGO 표적·정리.
- `ArenaTutorial.Space.cs`: x194~216 런타임 바닥/벽/점선 발판/엄폐/낙하 구간/의료 장식. 원래 씬 지형을 저장 변경하지 않는다.
- `ArenaTutorial.UI.cs`: 7구간 안내·달성/건너뛰기·이전/재시도/로비/실제 맵 혼자연습. 무기 구간 실제경기 특수탄 자동충전 없음/연습 예외 문구 추가. IMGUI는 pendingLesson/pendingExit만 기록하고 Update에서 실행한다.
- `SideScrollNetworkSession.cs`: StartTutorial→ArenaTutorial.Begin 후 기존 StartLocal(true,true). SoloPractice로 외부 참가 거절. SpawnTargets는 objective만 생성하고 옛 고정 표적은 생략. Disconnect/ClientStopped/OnDestroy에서 튜토리얼 정리. TutorialToPractice는 세션 코루틴에서 종료→NGO shutdown 완료→StartSoloPractice.
- `SideScrollNetworkSession.LobbyUI.cs`: 별도 튜토리얼 버튼, 방 목록 영역 높이 조정.
- `SideScrollNetworkPlayer.cs`: Tutorial.Active일 때 SpawnPosition=Checkpoint. 의료패드 GrantHealthServer는 튜토리얼 PracticeRoom 예외만 허용. 원래 일반 대기방 회복 검증을 약화시키지 말 것. 튜토리얼에서는 준비 문구 대신 HP/stamina 표시.
- `ArenaObjectiveMatch.Room.cs`: 튜토리얼에서는 기존 준비방 UI 숨김/CanStart 거절/UI 포인터 판정 연결. 실제 MatchState는 Waiting이며 PracticeRoom 안전/탄약 보충 규칙을 쓴다.
- `RelayArenaPrototype.cs`: 튜토리얼 카메라 별도 추적/clamp/ortho4.8, Foundry 맵 한계 적용하지 않음.
- `ArenaPlatformMotor.cs`: Awake의 단방향 발판 탐색을 public RefreshPlatforms로 분리. 구간 교체 후 이전 ignore collision 복원, 플랫폼/접촉 캐시 정리 후 새 단방향 발판 수집. 기존 일반 보행/경사 기능 회귀가 필요하다.

초기 발견한 컴파일 오류 `NetworkObject.IsServer`는 ClearLesson에서 `NetworkManager.Singleton.IsServer` 확인으로 수정했다. 마지막 컴파일/실행 상태는 메인 인계 문서의 검증 메모를 확인한다.

## 목표 판정과 위치

0. 기본체크포인트(200,.04), x>=203 이동, stamina<95 달리기, x>=209 도달.
1. 실제 SideScrollMapPreview.JumpCount>=2 관측 후 Grounded/rootY>2. 발판204.5/y1.2와208.5/y2.4.
2. 체크포인트208.5/2.44, 높은 곳에서 CrouchTrial.IsCrouched 관측 후 Grounded/rootY<.6.
3. (207,1) 일반 표적HP3. NormalRounds 감소 관측+표적 파괴+만량 자동충전.
4. 체크포인트203/.04, 표적(208,1)HP20에 Rigidbody2D. 실제 S 수신/Target.Hits 증가 때만 ShotgunPush 방향 속도를 줌. 209~213 구멍으로 표적 y<-2 낙하. **일반 적 전체 넉백 규칙을 바꾼 것이 아닌 튜토리얼 물리 표적**이다.
5. (208,1)HP3 표적을 x205.5~209.5/y0~3.2 고체 상자 안에 배치. 표적 Sprite sortingOrder10으로 내부 표적이 보이게 함. 실제 R 수신+표적HP0.
6. x204/h2 엄폐 너머(207,1)HP3 표적. 실제 G 폭발 수신+파괴, 자해 관측, 의료패드x198/수평반경.85/rootY<.6에서25HP/.75초로 완전 회복.

체크포인트마다 ResetForArenaRound로 생명·체력·스태미나·탄약/타이머 초기화. MatchState.Version도 올려 이전 구간 라이플/유탄 코루틴이 새 구간에 영향을 주지 않게 한다. 완료 배열은 재시도/이전 이동에도 보존, 건너뛰기는 달성 수를 늘리지 않는다. 플레이어 y<-2.5/x193밖/218밖이면 해당 구간 복귀.

## 바로 할 검증

먼저 컴파일/Console 확인. 다음 순서는 아직 실행 완료되지 않았다:

1. 오프라인 로비 버튼 진입, solo=true/roster1/Waiting/CanStart=false, 인터넷 방/UGS 호출 없음, 기존 맵 적 생성 없음.
2. 실제 걷기/달리기 목표, 더블점프 착지, 앉기+Space 발판 하강. 구간 교체 뒤 새 PlatformEffector 탐색 확인.
3. 실제 일반탄3발 표적 파괴/충전, S 실제 피격→표적 물리 이동→낙하, R 일반탄 차단/R 관통, G 곡사 엄폐 넘기/자해/패드에서만 회복.
4. 안내 가독성/레이아웃/탄약 보충 문구, 카메라·HP바/선택 안내, ESC 메뉴·낙하 복귀·재시도·건너뛰기 달성수 확인.
5. 종료→로비 정리 및 실제 맵 혼자 연습 연결. Tutorial.Instance/임시지형/표적·Sprite 누수 없음.
6. StartLocal(true) 일반 멀티용 로컬 호스트는 solo=false/최소2인 유지, Waiting 일반 회복 거절 등 기존 조건 유지. 변경한 모터의 Foundry 점선 발판/실선 경사 회귀.
7. 결과/잔여 콘솔 오류/재생 정지·씬 clean 확인. 인터넷/멀티/빌드는 검사하지 않으면 미검증이라고 보고.

## 자동화에 쓸 기존 도구

`Assets/RightWalkPrototype/Editor/ArenaReadyRoomReview.cs`가 기존 코루틴 검사 harness. 새 Tutorial partial은 아직 작성하지 않았다. 기존 `ArenaReadyRoomReview.WeaponGuides.cs`가 오프라인 solo 분기 예시이며 `BeginWeaponGuides`, marker파일, Run 분기를 참고한다.

- Actor는 기본으로 이동입력을0으로 덮는다. 튜토리얼 주행 검사 시 `foundryDriving=true` 등 실제 도구 흐름에 맞게 처리한다.
- 실제 조작: `SideScrollMapPreview.SetInput(horizontal,jump,sprint)`, `CrouchTrial.SetRequested`, `RightFacingAimRig.SetAimTarget`, `RightFacingShotPreview.Fire`, `SelectAmmo`, `SetSpecialHeld`.
- 프로그램 Fire는 실제 탄약/간격 판정을 통과하고 Live focus 입력을 요구하지 않는다. 물리 가속은 벽시계가 아니라 시뮬레이션 Time.time 충분한 경과로 확인한다.
- **유탄 테스트는 SetAimTarget 실제좌표 사용.** SetAimDirection은 먼 목표를 만들어 최대속도로 쏠 수 있다. PredictGrenade 예측점과 BlastDamage로 후보 조준을 찾은 후 실제 발사해 검증한다.
- 기존 SideScrollDamageTarget prefab을 Session.targetPrefab에서 생성, Spawn 전에 maxHealth 설정. NGO NetworkObject를 서버에서 Spawn/Despawn한다.
- 표적/플레이어 텔레포트 fixture를 쓰면 자연 플레이 전체를 검증했다고 하지 않는다. 안내 접근 가능성은 실제 입력으로 확인한다.

학원 메인 Unity bridge는 마지막 확인 포트6400. 가상6403/6405/6406은 강제종료 후 접속 거절이었음. 집 환경의 포트는 다시 확인한다. Python bridge 경로:
`LocalTools/Analysis/HomeVerification/CrouchSeam/bridge.py`.

예시: `call('execute_code', {'action':'execute','code':'UnityEditor.AssetDatabase.Refresh(); return "refresh";'}, 6400)`.
동적 코드에는 return 필요. Refresh 뒤 컴파일 시간을 둔다. 자동 코루틴 검사 중 execute_code를 연속 호출하면 NGO 진행이 정지될 수 있으므로 progress/result 파일을 읽어 기다린다.

## 아직 남은 위험

실제 더블점프 높이/발판 하강·G 조준 가능성·샷건 표적 낙하의 튜토리얼 달성 판정은 전부 실플레이 확인이 필요하다. 화면비/작은 화면에서 안내와 게임공간 겹침도 확인한다. 네트워크 시작 실패 시 임시 튜토리얼 Active 정리가 필요한지도 검토한다. 튜토리얼에는 학습용 안전/보충 규칙이 있어 실제 경기 밸런스 테스트를 대신하지 않는다.
