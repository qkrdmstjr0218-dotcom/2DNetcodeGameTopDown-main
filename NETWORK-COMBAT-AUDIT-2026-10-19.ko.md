# 현재 캐릭터와 네트워크 전투 연결 점검 · 2026-10-19

> 이 문서는 **최신 캐릭터와 전투를 연결하기 전**의 점검 기록이다. 이후 네트워크 모션과 서버 HP/플레이어 피해/사망·수동 복귀를 연결했다. 현재 안내와 MPPM 검증은 `Assets/RightWalkPrototype/Docs/NETWORK-CHARACTER-CURRENT.ko.md` 및 `LocalTools/Analysis/NetworkCombat/run-3/`를 따른다. 아래 ‘없음/미연결’ 표는 과거 상태다.

## 결론

네트워크 시험 기반과 표적 사격은 이미 있다. 그러나 현재 사용자가 확인한 앉기/앉기 이동/쓰러짐 등 최신 로컬 캐릭터가 네트워크 플레이어에 연결돼 있지 않다. 현재 캐릭터를 네트워크에 연결하는 작업이 다음 단계이며, 완성된 모션을 다시 만드는 작업은 아니다.

## 확인 범위

- 저장된 씬/프리팹/코드 확인 및 Unity AssetDatabase로 실제 네트워크 프리팹 컴포넌트·설정 조회.
- 점검 당시 열린 씬은 SideScrollCrouchTrial, Play 중, dirty=false. 현재 재생과 씬을 그대로 유지했다.
- 새2인 호스트/클라이언트를 실행하거나 빌드하지 않았다. 아래 ‘기반 있음’은 소스/설정 확인이며 이번 점검에서 실시간 동기화가 통과했다는 뜻은 아니다.
- 게임 코드/프리팹/씬을 변경하지 않았다.

## 연결 현황

| 항목 | 확인 결과 |
|---|---|
| 네트워크 세션 | SideScrollNetworkTrial의 NetworkManager가 SideScrollNetworkPlayer.prefab을 생성하도록 저장돼 있다. 세션/캐릭터에 NetworkObject/NetworkBehaviour가 있다. |
| 기본 동기화 | 위치·조준 방향·걷기 프레임·달리기·접지·도약 시작 프레임/달리기 상태 전송 코드가 있다. |
| 사격 | 소유자가 요청하고 서버가 발사 간격·방향·첫 장애물을 확인한 뒤 승인 경로를 모든 클라이언트에 표시한다. |
| 표적 HP | 별도 SideScrollDamageTarget의 HP/명중 수가 NetworkVariable이다. 서버가 피해를 주고, HP0이면 표적 충돌이 꺼진다. |
| 앉기/앉기 이동 | 네트워크 프리팹 전체 자식에 CrouchTrial이 없다. 네트워크 데이터에도 앉기 상태/주기 적용 경로가 없다. |
| 쓰러짐/복귀 | 네트워크 프리팹 전체 자식에 CharacterDeathTrial이 없다. 네트워크 플레이어에 HP/사망/복귀 동기화가 없다. |
| 최신 하단 사격 | 네트워크 프리팹에 FrontDownAimOverlay 등 최신 정면 하단 자세 컴포넌트가 없고, aim.frontDownAim도 null이다. 구형 측면 하단 조준 설정은 있다. |
| 더블 점프 | 네트워크 프리팹 allowDoubleJump=false. 현재 로컬 씬은 true다. 네트워크 상태에 JumpCount/두 번째 점프 자세 선택도 없다. |
| 플레이어끼리 피해 | 서버 사격의 피해 대상은 SideScrollDamageTarget뿐이다. 플레이어 HP를 줄이는 연결은 없다. |
| 참가자 구분/참가 | 시험 UI는 Host/Client/인원 수와 표적 초기화 정도다. 최대4인용 식별/서로 다른 스폰 지점/참가 제한 규칙은 별도 준비가 필요하다. 현재 스폰은 호스트-1.5, 나머지 모두+1.5다. |
| 접속 주소 | StartLocal이127.0.0.1:7783으로 지정한다. 같은PC의 호스트/클라이언트 시험용이다. 다른PC 접속 설정은 다음 범위에서 정해야 한다. |

## 다른 기존 코드와 구별할 점

- Assets/Scripts/Gameplay/Combat/NetworkHealth.cs는 존재하지만 현재 네트워크 시험 프리팹에 붙어 있지 않다. 기존 NetworkPlayerController/NetworkGameManager 및 웨이브 부활과 연결된 체계다.
- 기존 NetworkProjectile.OnTriggerEnter2D는 NetworkPlayerController를 가진 플레이어를 아군으로 보고 통과시킨다. 옛 협동 전투 코드를 그대로 사용하면 원하는 PvP 명중 규칙과 다르다.
- 현재 CharacterDeathTrial의 HP0/Revive는 로컬 시험이다. FrontDownAimPlaytestHud의 버튼이 HP0/복귀를 호출한다. 서버 HP와 이 연출을 연결하는 계층이 필요하다.
- SideScrollNetworkPlayer는 비소유자 Rigidbody2D.simulated를 false로 둔다. 이후 서버가 상대 플레이어에게 사격 피해를 주려면 원격 캐릭터의 피격 콜라이더가 서버 물리에 참여하는지 별도로 실행 확인해야 한다.
- 네트워크 씬 빌더는 SideScrollRunRecoveryTrial을 출발점으로 사용한다. 현재 CrouchTrial 씬을 기준으로 만들지 않으므로 기존 빌드 메뉴 실행만으로 최신 캐릭터가 옮겨지지 않는다.
- Builds/SideScrollNetwork/SideScrollNetwork.exe의 파일 날짜는10/12다. 이 파일을 이번 최신 모션의 동기화 검증 결과로 취급하지 않는다.

## 다음 작업 제안 — 아직 구현 승인 아님

1. 수용된 현재 캐릭터를 별도 네트워크 시험 프리팹에 연결하고, 소유자 입력과 상대 캐릭터 표시를 구분한다. 앉기/더블 점프/하단 사격 상태의 전송과 원격 자세 적용을 함께 연결한다. 현재 로컬 시험 씬과 원화는 보존한다.
2. 서버 기준의 플레이어 명중/HP를 로컬 쓰러짐·복귀 연출에 연결한다. 표적 피해와 플레이어 피해를 구분한다.
3. 새 호스트/클라이언트2개를 실행해 양쪽에서 같은 상태가 보이는지 확인한다. 색상/닉네임 및4인 참가 규칙은 필요한 범위를 정한 뒤 추가한다.

## 주요 근거 파일

- Assets/RightWalkPrototype/Scenes/Network/SideScrollNetworkTrial.unity:813 — 실제 생성 프리팹 참조 (정리 후 경로)
- Assets/RightWalkPrototype/SideScrollNetworkPlayer.prefab — 실제 네트워크 캐릭터 구성
- Assets/RightWalkPrototype/Scripts/SideScrollNetworkPlayer.cs:9 — 전송 데이터 / :27 소유권 설정 / :76 서버 사격 / :89 표적 피해
- Assets/RightWalkPrototype/Scripts/SideScrollNetworkSession.cs:20 — 접속 주소
- Assets/RightWalkPrototype/Editor/SideScrollNetworkTrialBuilder.cs:16 — 구형 출발 씬
- Assets/RightWalkPrototype/Scripts/CharacterDeathTrial.cs:29 — 로컬 피해/쓰러짐
- Assets/RightWalkPrototype/Scripts/FrontDownAimPlaytestHud.cs:21 — 시험용 HP0/복귀 버튼
- Assets/Scripts/Gameplay/Combat/NetworkProjectile.cs:133 — 기존 아군 플레이어 피해 제외
