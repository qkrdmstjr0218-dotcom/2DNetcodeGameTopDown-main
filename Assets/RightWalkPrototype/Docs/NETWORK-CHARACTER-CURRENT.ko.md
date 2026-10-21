# 최신 캐릭터 네트워크 시험

2026-10-20: arena 일반 실행에 [작은 준비 공간·준비/취소·방장 시작·결과 후 복귀](READY-ROOM-FLOW.ko.md)가 추가됐다. 방 검색 로비와 모드 선택은 후속이며 기존 로컬 연결 버튼을 사용한다.

첫 전투 맵은 별도 `Scenes/Network/RelayArenaPrototype.unity`이며 [중계 시설 시안 안내](RELAY-ARENA-PROTOTYPE.ko.md)를 따른다. 아래의 기존 시험 씬과 캐릭터는 유지했다.

## 열기

- 씬: `Assets/RightWalkPrototype/Scenes/Network/SideScrollNetworkTrial.unity`
- 네트워크 프리팹: `Assets/RightWalkPrototype/SideScrollNetworkPlayer.prefab`
- 선택적 빌드 경로: `Builds/SideScrollNetworkCurrent/SideScrollNetworkCurrent.exe`. 기존 파일은 이전 모션 연결 단계의 실행본이며 이번 전투 변경은 아직 빌드하지 않았다.

Unity에서 이 씬을 연다. `Window > Multiplayer > Multiplayer Play Mode`에서 Player 2를 켜고 준비된 뒤 Play한다. Main Editor의 Game 화면에서 Local Host, Player 2의 Game 화면에서 Local Client를 누르면 빌드 없이2인 시험이 된다. 주소는127.0.0.1:7783이며 다른 PC 접속 설정은 별도다. 실행본으로 시험하려면 먼저 아래 빌드 메뉴로 최신 코드를 다시 빌드한다.

A/D 이동, Shift 달리기, Space 점프/더블 점프, S/↓/왼쪽 Ctrl 유지로 앉기, 마우스 조준, 좌클릭 사격. 각 실행 화면에서 자신의 캐릭터만 조작한다. Reset Targets는 서버의 기존 연습 표적을 복구한다. Disconnect로 종료한다.

현재 시험값은 **HP100 / 플레이어에게 발당 피해25**다. 서버가 먼저 맞는 지형/표적/플레이어를 선택한다. HP0이면 기존 쓰러짐 모션과 함께 이동·사격을 막고, 본인 화면 왼쪽 아래 `복귀 (시험)` 버튼으로 HP와 시작 위치를 복구한다. 자동 부활·승패 규칙은 아직 넣지 않았다.

임시 식별은 기존 머리 위 HP 표시와 같은 줄의 P번호다. 본인 번호만 녹색이며 왼쪽 아래 내 HP에도 번호를 표시한다. 서버가 비어 있는 가장 작은 번호를 배정하므로 재접속 때 접속 ID가 증가해도 P번호가 계속 증가하지 않는다. 기존 참가자의 번호는 유지하고 퇴장한 참가자의 번호는 재사용한다. 캐릭터 색/디자인과 닉네임은 변경하지 않았다. 4인 번호 동기화·재접속 검사와 표시 캡처: `LocalTools/Analysis/NetworkCombat/lifecycle-7-player-numbers/`.

네트워크 씬에서는 캐릭터 프리팹에 남은 옛 로컬 시험 HUD를 비활성화한다. 옛 `복귀 · 다시 조작`은 서버 HP를 바꾸지 않아 즉시 다시 쓰러지는 문제가 있었다. 이제 공개 복귀 함수를 호출해도 생성된 네트워크 플레이어는 소유자만 서버 복귀를 요청하며, 다른 캐릭터를 일괄 복구하지 않는다. 로컬 대표 씬의 시험 HUD는 유지한다.

사용자는3명 접속에서도 각자 본인만 복귀함을 확인했다. 현재는 각 화면의 소유자만 이동 물리를 실행하므로 플레이어끼리 몸으로 길을 막지 않는다. 바닥·벽 이동 충돌과 서버의 총알 피격 범위는 존재한다. 몸으로 밀기/길막은 미확정 규칙이며 추가하지 않았다. 호스트 연결 종료 시 접속 버튼 위에 안내가 남고, 직접 퇴장/최초 접속 실패와 문구를 구분한다. 재접속하면 안내는 사라진다. 3인 검증과 표시 캡처는 `LocalTools/Analysis/NetworkCombat/lifecycle-4-notice/`에 있다.

## 이번 연결 범위

사용자가 수용한 로컬 완성 캐릭터를 바탕으로 네트워크 프리팹을 갱신했다. 위치/속도/걷기·달리기 프레임, 좌우 방향, 앉기 여부/주기/보행 강도, 점프 횟수/도약 프레임, 하단 사격 자세/각도, 공중 하체 방향을 하나의 상태로 전송한다. 원격 캐릭터는 키보드·마우스·이동 물리를 실행하지 않고 같은 모션 표시 코드를 사용한다. 사격은 기존 서버 승인 경로를 유지한다.

`Assets/Characters/Current`의 로컬 프리팹·대표 씬과 원화/모션 데이터, 기존 보존 ZIP은 유지했다. 서버가 플레이어 HP와 사망/복귀 세대를 관리하고, 기존 쓰러짐 표시를 두 화면에 적용한다. 이동 물리가 꺼진 원격 플레이어도 마지막 수신 위치와 서기/앉기 충돌 크기로 서버가 판정한다. 복귀 전 생명 세대의 지연 피해와 이동 상태는 새 생명에 적용하지 않는다. 이름/색상,4인 참가 규칙과 승패는 포함하지 않았다.

앉기 이동은 후속 피드백에서 자세 교체가 필요하다고 평가됐으며 사용자가 나중에 하기로 했다. 이번 전투 작업에서는 자세/원화를 바꾸지 않는다.

## 재생성·검사 도구

`Tools/Right Walk Prototype/Build Network Trial`은 현재 로컬 대표 씬으로부터 위 시험 씬/네트워크 프리팹을 다시 만든다. 저장된 로컬 원본을 덮어쓰지 않는다. `Build Network Test Player`는 위 실행본 경로에 Development 빌드를 만든다. 옛 `Builds/SideScrollNetwork` 실행본은 이전 버전이다.

개발 빌드의 `--side-motion-test`는 정상 플레이에서는 실행되지 않는2프로세스 검사다. 실행 방법과 출력은 `LocalTools/Analysis/NetworkMotion/`에 있다. 기존 `--side-damage-test`는 서버 연습 표적 피해의 별도 회귀 검사다.

## 2026-10-19 전투 연결 검증 (최신)

Main Editor + Multiplayer Play Mode Player 2에서 양방향 피해와 동일한 서버 HP, 엄폐물 차단, 앉기 피격 범위, 앉은 클라이언트의 사망/복귀, 사망한 소유자의 직접 사격 RPC 거절, 이전 생명 세대의 피해 무시를 확인했다. 호스트의 공중 사망 이후 낙하/착지, 죽어 있는 호스트에 클라이언트가 중도 접속했을 때 쓰러짐 표시, 호스트 복귀와 앉기 이동 재개도 통과했다. 연습 표적2개도 기존처럼 HP0/명중3회로 처리됐다.

기록과 실제 렌더 캡처는 `LocalTools/Analysis/NetworkCombat/run-3/`에 있다. `NetworkCombatEditorReview`와 `SideScrollNetworkCombatVerification`은 요청 파일이 있을 때만 실행하는 검사 도구다. 평상시에는 검사 입력이나 자동 Host/Client 실행을 하지 않는다. NetworkObject 프리팹 식별값을 저장해 Main/Player 2에서 동일하게 읽는 것도 확인했다. 아래는 전투 연결 이전의 모션 검사 기록이다.

## 2026-10-19 모션 연결 검증 (이전 단계)

동일 PC의 새 Development 실행본2개를 Host/Client로 실행했다. 양쪽 모두 다음 검사를 통과했다.

- 걷기/달리기, 앉기 전진/후진/정지와 앉기 주기 진행.
- 지상 정지/이동 하단 사격, 첫 점프/더블 점프의 서로 다른 하체 자세, 공중 하단 조준/방향 전환과 사격 표시.
- 원격 캐릭터의 입력·물리 분리, 앉아 있는 호스트에 클라이언트가 재접속한 뒤 초기 자세 표시.
- 별도 서버 표적 회귀 검사: 양쪽에서 표적2개 모두 HP0, 명중3회, 충돌 비활성화를 확인했다.

현재 로컬 대표 씬에서도 앉기 이동/하단 사격/점프·더블 점프/공중HP0/쓰러짐/복구를 다시 실행해 통과했다. 정지 앉기 캡처는 보존본과 픽셀이 동일했다. 네트워크 상태 구현으로 원화/Resources/로컬 프리팹·씬의 내용을 바꾸지 않았다. 다른 PC나 인터넷 연결 및 플레이어 간 전투 검증은 이번 검사에 포함하지 않는다.

기록: `LocalTools/Analysis/NetworkMotion/run-5/{host,client}-result.txt`, `target-regression/{host,client}-result.txt`, `LocalTools/Analysis/CharacterOrganization/Verification/network-regression/result.txt`. 원격 캐릭터의 실제 렌더 캡처도 run-5에 있다. 이전 실패 기록은 위치 초기화 직후 접지 갱신 전에 점프를 넣던 검사 준비 순서의 기록이며, 최신 결과는 run-5를 기준으로 한다.


## 2026-10-20 근거리 사격·겹침 수정

총구보다 몸 쪽에 있는 가까운 상대도 발사선에서 판정한다. 몸에서 총구 사이의 지형도 차단하므로 총구만 벽 밖으로 뺀 발사는 벽에 막힌다. 총구 밖 사거리·탄속·피해는 유지한다. 캐릭터 루트 SortingGroup으로 한 명의 부위를 묶고10+P번호 순서로 전 화면 동일하게 정렬한다. 기존 포즈 내부/다리 중첩 정렬은 유지한다.

검증: `LocalTools/Analysis/NetworkCombat/objective-011842/result.txt` PASS(4인).호스트/참가자 양방향 서기·앉기 초근거리/완전 겹침 명중, 총구 전 벽 차단, 뒤 상대 제외, 첫 대상만 명중, 체력 동기화, 서기·앉기·사망·복귀의 캐릭터 묶음과 실제 화면 확인. 메뉴 Verify Close Combat.
