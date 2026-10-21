# Working Preferences

> **2026-10-21 겹친 발판 하강 복귀 수정·해킹 콘솔:** 사용자 Foundry 피드백의 격자→아래 경사로 하강 후 위로 튀는 현상을 room-082916 실제 앉기+점프로 재현했다(루트y7.085→6.092→7.085). 원인은 다른 바닥 착지/1.5초만으로 원래 격자 충돌을 다시 켜 몸과 겹친 채 위로 밀어내던 처리였다. `ArenaPlatformMotor`는 통과 중 발판을 HashSet으로 유지하고 몸 전체가 아래/옆으로 벗어나거나 공중 점프로 발 전체가 위로 돌아왔을 때 개별 복구한다. 연속 하강과 발 아래의 같은 높이 단방향 조각도 함께 처리하며 고체/안전 착지 제한은 유지한다. 점프·이동·캐릭터 외형 수치는 보존.
>
> **배치/표시:** Foundry 해킹 후보6곳에 충돌 없는 콘솔 장식을 추가했다. 비활성 기기는 어두운 화면으로 남고 예고는 주황, 활성은 기존 점유 상태 색으로 화면·표시등·은은한 광을 켠다. 점유/F입력 규칙은 변경하지 않는다. 다리에 걸친 동쪽 하층 상자를x28→29로 이동해 밑면 전체가 고정 바닥 위에 놓이게 했다. 점프력/추가 낙사·다리/점선 상자·엄폐 밀도·상자 더미/하부 고체 경사로 확장/적 스폰·바닥 출현 터렛은 사용자 구상으로 별도 기록했으며 이번에 일괄 구현하지 않는다. 현재 단일점프 이론 높이약1.4(6.5/g*1.5)라는 낮은 높이 피드백도 기록만 했다.
>
> **검증:** room-083301 4인 PASS:호스트·참가자 좌우 상/중층4지점 하강 후2.2초 이상 재상승 없음·서기, 겹친 발판 연속 하강, 낮은 층 착지 후 원래 격자로 점프 재진입, 콘솔6곳 무충돌/비활성·예고·활성 구별과 화면 확인, 상자 다리 밖 배치. room-084434 4인 전체 Foundry PASS:경사10개·중층 연결·보조 발판·하강 회귀에 더해 아래 착지 전 공중 추가점프로 원래 격자 복귀, 참가자 이동/6점유·득점·부활·낙사·대기 복귀. arena-controls-084209 기존 맵 전체 이동/얕은 경사로 즉시 재진입/격자/총알·엄폐/카메라·메뉴/낙사·4인 연결 PASS.083624는 상단 경계에 발이 겹친 순간배치에서 내리막 검사 중단,084031은 정상 바닥 안쪽에서 좌우 내리막 걷기 진입 PASS였으며 이후 전체 검사의 시작점도 경계 안쪽으로 보완했다. 내리막용 게임 코드를 변경하거나 검사 우회하지 않았다. 임시 검사 요청 제거/새 씬 열기·재생 정지/오류0 확인.


> **2026-10-21 별도 대형 맵 Foundry 08:** 사용자 승인으로 `Scenes/Network/RelayFoundryPrototype.unity`를 새로 만들었다. 기존 RelayArenaPrototype은 유지. 가로72(기존46)/바닥0·3.6·7.2·10.8의4단, 좌우 외곽·내곽·최상층 경사로10개, 공중 추가 점프용 통과형 보조 발판4개, 고체 엄폐물5개, 동쪽24~28 접이식 낙사 다리와 중층 우회로다. 해킹 후보6곳(하단중앙→서쪽중층→서쪽상층→최상층→동쪽상층→동쪽중층), 좌우/층으로 분리한 시작·부활4곳. 기존 순찰 로봇2개와 샷건+3 보급3곳을 맵별 배치했다. 새 적 종류/새 트랩/회복/라이플·유탄 보급/맵 선택/실제 네트워크 씬 로딩은 이번 범위가 아니다. 현행 준비방→3초→3분 경기→결과→복귀를 그대로 사용한다. 평상시 확대4.8·캐릭터 크기·이동/점프·무기 수치 유지. 최종 미술/실대전 균형 수용 전의 동선 초안이다.
>
> **구현/검증:** optional `ArenaMapSettings`의 카메라 경계·표제·보급점·층별 순찰 경로를 사용하고 컴포넌트 없는 기존 씬은 원래 배치를 유지한다. 새 씬의 전원 도착 판정은 기존 x<30 대신 맵 경계로 검사하여 오른쪽 x31 시작도 처리한다. room-075342 4인 PASS:경사10개/중층 연결부 양방향 이동·정지 안정성/보조 발판4개 실제 공중 점프 착지/최상층 안전 하강/참가자 상승/6지점 접지·실제 상층 득점/카메라·자동부활·낙사·결과/대기 복귀. room-080323 4인 PASS:정상 플레이·전체맵 화면 확인과 실제 보급3곳 획득·전원 소멸 동기화. room-080039 기존 맵 준비방 연습·시작·카운트다운·전투·복귀 회귀 PASS. 첫074558에서 윗층 상자 밑에 걸리는 상승 동선은 중층 상자를 제거해 고쳤고,074810은 내려가기 후 정상적인 상자 위 착지를 실패로 판단한 검사 높이를 보완했다. 기존 맵075821은 검사 캐릭터가 고정 보급품 x-8을 주워 자동 보충처럼 보인 배치 오류로, 보급품에서 떨어져 검사하도록 수정했다. 최종 보급(3,7.75)은 상자 안에 겹쳤던(0,7.75)에서 옆으로 이동했다. 상세 `Docs/RELAY-FOUNDRY-PROTOTYPE.ko.md`. 검사 요청 제거/재생 정지 후 새 씬을 Main과VP 모두 열어둔다.


> **2026-10-21 유탄 벽 너머 폭발·거리 피해/라이플 명중 강화:** 사용자 요청으로 폭발 피해의 고체 차폐 검사를 제거했다. 유탄 자체는 여전히 고체 첫 접촉에 폭발하며 몸~총구 장애물 검사도 유지한다. 폭발점→대상 피격 bounds의 가장 가까운 점까지 거리0~반경2에서 플레이어40→10(중간25), 적/옛 표적3→1을 선형 보간 후 반올림한다. 반경 밖0, 자해도 동일하며 벽 너머 생존 피해자도 기존2.2/.14초 짧은 밀림을 받는다. 속도·반경·간격·탄약은 유지. 라이플 명중 섬광은.24초/길이.6/굵기.4, 양방향 세로 섬광.65/.14와8방향 파편(.32초/속도2.8/길이.48/굵기.085), 관통 방향 잔상.26초/.65/.12로 확대했다. 라이플 피해·탄속·관통 판정은 유지한다.
>
> **검증:** room-072351 4인 유탄 PASS:중심40/중간25/가장자리10/범위 밖0, 실제 두 대상 거리별 차등 피해, 고체 뒤 피해+반응, 자해/참가자 발사/예측 일치/고갈·서버 검증·종료/복구·보충. room-072517 4인 라이플·샷건 회귀 PASS와 rifle-impact.png 실제 큰 보라·흰 섬광 확인. 임시 요청 제거·재생 정지·오류0. 이전 고체 차폐 및 유탄 고정40피해 기록은 이 항목으로 대체하며 최종 밸런스는 사용자 평가 전.


> **2026-10-21 유탄 거리·폭발 반응 후속:** 사용자 피드백으로 발사 속도를 clamp(4+거리*1.1,4,18)로 상향했다(이전 .7/최대14). 같은 거리6·45도·높이1.4의 정적 계산 사거리는5.94→9.24이며 실제 지형·조준 각도에 따라 달라진다. 예측과 서버 비행은 같은 설정을 공유한다. 폭발 피해를 받고 살아 있는 플레이어는 폭발 중심→몸 중심 방향 속도2.2/.14초로 짧게 밀리며 이동이 제한된다. 자해에도 적용하고 고체 차폐·범위 밖·사망자는 제외한다. 기존 소유자 물리/빠른 피격 위치 전파를 재사용하며 연속 반응은 속도를 누적하지 않고 교체한다. 피해40·반경2·폭발 연출 및 다른 무기 수치는 유지한다.
>
> **검증:** room-071748 4인 PASS:거리별 세기/상한, 호스트·참가자 실제 발사/피해, 두 피격 소유자의 바깥 방향·작은 세기·짧은 반응 해제, 자해 반응, 범위 밖/차폐 반응 없음, 예측 착탄 일치/전원 폭발점 동기화, 적 피해/고갈/교체/서버 간격·생명·속도 검증/종료 비행 취소/대기 복구·보충. 비행 화면 확인. room-071618은 재생 중 재컴파일 뒤 주 에디터만 새 요청 폴더를 읽은 검증 시작 상태 불일치로 중단했으며, 전체 재생 정지 후071748에서 다시 실행했다. 최종 수치는 사용자 사용감 평가 전.


> **2026-10-21 총구 발사 유탄 시험판:** 사용자 승인으로 Q/E 순환에 유탄(일반→샷건→라이플→유탄)을 추가했다. 우클릭 유지+좌클릭 즉시 발사, 마우스 방향은 실제 총구 조준 방향/거리는 속도4~14(4+거리*.7)로 반영. 예상 곡선/첫 착탄점/폭발 반경을 연두색으로 표시한다. 서버와 예측은 같은 중력14·1/60초 구간 검사·반경.09를 사용. 고체 지형·엄폐물·살아 있는 다른 플레이어/적 첫 접촉에1회 범위 폭발. 격자는 기존 탄환처럼 통과. 3발/1초 간격/반경2/플레이어40(자해 포함)/적·옛 표적3피해, 가장 가까운 피격 범위 점까지의 고체 차폐가 피해를 막는다. 발사자는 비행 중 직접 접촉만 제외하며 근접 폭발에는 피해를 받는다. 총구 뒤 몸~총구 경로의 장애물도 검사하여 벽 속 발사 우회를 막는다. 빈 유탄은 붉은 고갈/발사 차단, 우클릭 해제 시 일반탄. 대기방만 사격 중단2초 후2초당1발, 시작/부활/복귀 만량. 경기 중 유탄 보급·도탄·시간지연/클러스터·머신건은 미구현. 아무것도 닿지 않으면3초 뒤 폭발 없이 제거. 기존 캐릭터·이동/일반탄·샷건·라이플 수치와 보급 보존, 일반탄 Q/E 제외는 후속 유지.
>
> **검증:** room-065604 4인 PASS:선택·거리별 세기 제한/호스트 및 참가자 실제 발사·한 폭발로2명 피해/범위 밖 제외/4화면 동일 폭발점/예측 착탄 일치/중복·클러스터 없음/자해/범위 안 엄폐 대상 차단/적 피해/고갈·해제 일반탄/잘못된 생명·속도/동시 발사 간격/종료 비행 취소/복귀·대기 보충. preview/flight/blast 화면 확인. room-065758 4인 기존 샷건·라이플 입력·간격·고갈·관통·복구 회귀 PASS. 065051에서 발견한 폭발 차폐는 반경0 CircleCast 대신 명확한 Raycast로 수정했다.065343은 적이 앞선 자해 시험 폭발로 이미1HP여서 새 명중 전에 검사가 넘어간 배치 오류로, 적을4HP/정지 상태로 초기화하고 실제 새 탄약 소모+피해를 기다리도록 보완했다. 임시 요청 제거 후 재생 정지·오류0. 상세 Docs/GRENADE-LAUNCHER-TRIAL.ko.md. 최종 사용감/수치 수용은 사용자 평가 전.

> **2026-10-21 특수탄 고갈 정책·탄환 외형 후속:** 사용자 승인으로 현재 RoomFlow의 빈 샷건/라이플은 우클릭 유지 중 해당 특수탄 상태를 유지하고 발사를 차단한다. 일반탄 자동 대체를 제거했으며 우클릭 해제 때만 일반탄으로 전환(.18초 교체 지연 보존). ActiveAmmoEmpty로 로컬 발사/HUD/조준점 상태를 함께 판단하며 서버 잔량 검사도 유지. 고갈은 붉은 맥동 링/중심·사거리 표식과 '탄약 없음' 문구, 빈 특수탄은 일반탄을 소모하지 않는다. 일반탄/라이플 선 끝을6분할 둥근 캡으로 바꿔 각진 탄두를 완화했다. 일반탄 꼬리.42→.8, 라이플1.15→2.1, 라이플 밝은 탄두.42·외곽광.5. 관통 명중 섬광.16초/.32길이/.23굵기,6방향 파편.22초로 확대했다. 피해·탄속·사거리·명중 판정·3발·일반탄 Q/E 선택은 유지(선택 제외는 후속).
>
> **검증:** room-053911 4인 PASS:빈 샷건/호스트·참가자 빈 라이플 우클릭 유지 중 일반탄·특수탄 발사/소모 없음, 선택 보존, 우클릭 해제 실제 적용+교체 지연 뒤 일반탄 발사, 기존 입력·간격·관통·탄약·복귀 보존. 053741 캡처에서 둥근 일반탄/긴 꼬리·확대 관통 섬광·빈 라이플 붉은 링/HUD 확인(동일 생산 코드). 053543은 짧은 참가자 입력 관찰 시간 실패로 모든 참가자 새 생명 동기화 대기 및 두 번 소모까지 입력 유지로 보완했고,053741은 해제 명령 적용 전 고정.24초만 기다리던 검사 실패로 실제 상태/지연 완료를 기다리도록 보완했다. 게임 코드에 검사 우회 없음. 임시 요청 제거 후 재생 정지·오류0.

> **2026-10-21 일반탄·라이플 시각 강화:** 사용자 승인으로 arena 일반탄 탄두 굵기.05→.075(1.5배), 꼬리.42 유지. 라이플은 밝은 길이.3 탄두/보라색 얇은 잔광1.15/은은한 외곽광을 분리하고 전용 보라·흰 총구 섬광과 기존2배 시각 반동, 대상별 관통 명중 섬광/파편을 추가했다. 피해·탄속·사거리·탄약·명중 범위·교체/재발사 규칙은 보존. 일반탄 Q/E 제외는 사용자 '나중에' 요청으로 후속 메모이며 아직 구현하지 않는다. 단순 스파크 및 라이플 시각물은 생성 프레임에 직전 프레임 deltaTime을 더해 즉시 사라질 수 있던 계산을 실제 생성시각 기준으로 수정했다. 공유 풀 재사용 시 sortingOrder30 복원, 라이플3개 선 정리 처리.
>
> **검증:** room-041831 4인 PASS:유지 입력 샷건/라이플·탄종별 간격·교체·관통·탄약·복귀. normal-projectile/ rifle-muzzle/ piercing-rifle/ rifle-impact.png에서 커진 일반탄·밝은 라이플 탄두/보라 잔광·총구 및 대상 명중 섬광 확인. 041251 실패는 추가 일반탄 캡처 후 캐릭터를 보급 위치x-8로 초기화해 샷건 탄수 검사 중 보급된 검사 배치 오류로, 다시 행렬 위치에 배치해 해결했다. 041445/041647도 기능 PASS였으나 짧은 명중 효과 캡처 누락을 보완하고 생성 프레임 수명 계산까지 수정했다. 임시 요청 제거 후 재생 정지·오류0. 상세 Docs/BULLET-VISUALS-TRIAL.ko.md.

> **2026-10-21 특수탄 실제 입력 회귀 수정:** 사용자가 특수탄 발사 불가를 발견했다. RightFacingShotPreview.LateUpdate가 매 프레임 SetSpecialHeld(false) 후 우클릭 값을 다시 적용하던 기존 경로 때문에 새0.18초 교체 지연이 끝없이 초기화된 것이 원인이다. 버튼 상태를 한 번 읽고 ProcessFireInput으로 프레임당 한 번만 적용하도록 수정했다. 이전035801 검사는 readMouse=false 상태의 명령 발사여서 실제 입력 루프 회귀를 놓쳤으며 충분한 입력 검증으로 취급하지 않는다. 샷건 조준 링/사거리 표시는 노란색(1,.82,.18), 고갈 표시는 붉은색으로 분리했다. 다른 탄종 수치/교체 지연/탄종별 간격/라이플3발·보라색은 유지.
>
> **검증:** room-040238 4인 PASS:실제 LateUpdate와 공유하는 ProcessFireInput에 우클릭+좌클릭 유지 상태를 매 프레임 공급하여 호스트 샷건2회·참가자 라이플2회 발사/간격 준수 확인. 기존 전환·관통·탄약·복귀 검사도 통과. shotgun-yellow.png에서 노란 링과 샷건 실제 발사 확인. 임시 요청 제거, 재생 정지·오류0 확인.

> **2026-10-21 발사 간격 분리·라이플 표시 후속:** 사용자 승인으로 RoomFlow의 모든 탄종 공통 발사 잠금을 제거하고 일반탄.23/샷건.7/라이플1.2초를 각각 따로 유지한다. Q/E 선택 변경 또는 우클릭으로 실제 탄종 전환 시.18초 교체 지연, 해당 탄종 재발사 대기는 교체로 초기화되지 않는다. 로컬 입력/서버 모두 검사하며 생명·라운드 초기화 때 복구한다. 옛 씬은 기존 공유 간격 유지. 조준점은 대기 중 중심을65%로 축소/흐리게 하고 안쪽 반경12px 회색 원호가12시부터 시계방향으로 차오른다. 바깥 탄약 링은 그대로이며 준비 완료 시 중심 복원/대기 원호 숨김. 라이플 탄두/꼬리 굵기1.5배(명중 범위 유지), 보라색 탄환/선택 링·사거리 표시, 탄창4→3으로 시험 변경. 피해·탄속·사거리·기존 샷건 보급 보존. 단일 교체시간 원본 SideScrollNetworkPlayer.FireTiming.cs의 WeaponSwitchSeconds=.18.
>
> **검증:** room-035801 4인 PASS:교체 직후 로컬/서버 발사 거절, 라이플 대기 중 일반탄·샷건 발사, 샷건 대기 중 일반탄 발사, 교체 복귀 시 라이플/샷건 잔여 간격 보존·해제·재발사, 조준점 상태 복원, 기존 관통·참가자 발사·3발 초기화·종료/대기 보충. switch-wait/ rifle-cooldown/ piercing-rifle.png 화면 확인. 가상 에디터 포커스 때문에 커서가 숨겨지는 캡처는 검사 전용 UNITY_EDITOR 컴포넌트가 동일한 실제 그리기 함수를 호출하여 확인하며 게임 포커스 정책은 보존한다. 035645 실패는 Editor 어셈블리 컴포넌트 부착 불가인 검사 도구 문제로, 빌드 제외 Tests 컴포넌트로 옮겨 수정했다. 임시 요청 제거 후 재생 정지.

> **2026-10-21 관통 라이플 추가:** 사용자 승인으로 현재 RoomFlow에 일반탄→샷건→관통 라이플 Q/E 순환을 추가했다. 우클릭 유지+좌클릭 발사, 고갈/우클릭 해제 시 일반탄. 사거리30/탄속48/간격1.2초/4발, 플레이어50·적/표적4피해. 서버가 실제 이동 구간을 매 프레임 검사하며 지형·엄폐물·여러 대상을 관통하고 대상별 한 발1회만 피해를 준다. 청백색 탄두/푸른 꼬리·라이플 잔량/파란 조준 링, 실제 지형 무시 사거리 표시. 대기방에서만 사격 중단2초 후2초당1발 보충, 시작/부활/대기 복귀 만량. 기존 샷건 보급/드롭은 유지하며 라이플 드롭·차징·넉백·지형 감쇠·FPSbasic 적 이식은 이번 범위에 없다. 상수 원본 SideScrollNetworkPlayer.Rifle.cs, 상세 Docs/PIERCING-RIFLE-TRIAL.ko.md.
>
> **검증:** room-033554 4인 PASS:기존 탄종 순환/독립 잔량·공통 간격/실제 비행/엄폐물2개+플레이어3명 관통·각1회 피해/낮은 로봇 하향 조준 파괴/참가자→호스트·다른 참가자 관통/오래된 생명·잘못된 원점/방향 거절/고갈 일반탄 전환/경기 보충 없음/종료 거절·대기 복원·자동 보충. piercing-rifle.png에서 실제 탄두/꼬리·HUD 확인. 최초033131의 로봇 실패는 수평탄이 낮은 로봇 위로 지난 검사 배치 오류였으며 하향 조준 검사로 수정했다. 임시 요청 제거 후 재생 정지.

> **2026-10-21 개발 우선순위 교정:** 사용자는 아직 실플레이 평가에 앞서 특수탄 종류 확보·현재보다 큰 별도 맵·추가 적·트랩이 필요하다고 설명했다. 회복 아이템→바로 경기 밸런스 테스트라는 이전 제안은 우선순위에서 내린다. 현재는 기능 확인 단계이며 필요한 전투 선택지/공간/방해 요소를 확보한 뒤 한 판 평가로 넘어간다. 관통 라이플·수류탄(머신건은 후보), 새 회색상자 맵, 포탑/드론·트랩 등의 구체적 구현 순서는 제안 단계이며 이번 발언만으로 일괄 제작에 착수하지 않는다.


> **2026-10-21 즉발 샷건 기본 채택:** 사용자가 새 샷건으로 교체하고 게임 완성을 우선하라고 요청하여 현재 RoomFlow의 샷건을 즉발판으로 통합했다. 기본 선택/Q·E는 일반탄↔즉발 샷건2종, UI 명칭은 샷건으로 정리하고 별도 시험 잔량 줄을 제거했다. ShotgunRounds/GrantShotgunAmmoServer가 현재 모드의 잔량을 참조하므로 기존 고정 보급(+3)·적 드롭(+1)/최대치·서버 동시 지급이 새 샷건에 연결된다. 기존 비행 산탄 코드는 보존하지만 현재 게임에서는 구형 발사RPC를 거절하여 숨은 별도 탄창을 쓰지 못한다. 옛 useReadyRoom=false 씬은 비행 샷건 유지. 대기 보충/부활·경기 자원 초기화도 현재 샷건에 적용한다. 일반탄·새 샷건의 수치/잔상/무작위 퍼짐은 유지한다.
>
> **다음 제안:** 회복 수단을 추가해 전투·보급·재진입 루프를 만든 뒤 한 판 단위 테스트를 우선한다. 이번에는 샷건 채택만 진행하며 회복 아이템/소지·R키 사용/피격 취소 등은 아직 미구현·방식 미확정이다.
> **검증:** room-030957 4인 PASS:기본 새 샷건/두 종류 순환·즉시 명중·구형 RPC 차단·잔상/퍼짐/엄폐·복구. room-031048 4인 PASS:새 샷건 잔량으로 고정/일회성 보급·동시1회 지급·만량 제한·재생성·사망 제외·새 경기 복구. 현재 샷건 HUD 화면 확인. 임시 요청 제거 후 재생 정지.



> **2026-10-21 즉발 색 방향:** 사용자 피드백으로 즉발 궤적의 총구 쪽을 가늘고 옅은 주황(.018/alpha.2), 명중점 쪽을 밝고 선명한 연노랑(.055/alpha1)으로 변경했다. 잔상.16초·무작위±.8도/전체12도 및 피해·기존 비행 샷건은 유지. 퍼짐이 눈에 덜 띈다는 의견은 기록하되 이번에 각도를 더 키우지는 않았다. room-030311 4인 기존 비교 PASS와 instant-shotgun 화면으로 색 방향 확인. 임시 요청 제거/재생 정지.


> **2026-10-21 즉발 잔상/무작위 퍼짐:** 사용자 피드백으로 즉발 궤적 .09→.16초로 연장. 즉발판 각 기본 산탄 방향에 최대±.8도(전체±6도 제한)를 서버에서만 무작위 적용하고 같은 PelletPath로 피격/전원 표시를 처리한다. 기존 비행 샷건의 고정 각도·탄속18과 양쪽 피해/사거리/간격/잔량 규칙은 유지한다.
>
> **검증:** room-025832/result.txt 4인 PASS:5개 각도 제한/0.09초 이후 잔상 존재/4화면 실제 렌더 방향 일치/발사마다 방향 변화, 즉시 피해·기존 비행 지연·엄폐·고갈·대기 복구. instant-shotgun 화면 확인. Verify Instant Shotgun 검사 보강. 임시 요청 제거 후 재생 정지.


> **2026-10-21 별도 즉발 샷건 시험:** 사용자 요청대로 기존 비행 샷건(탄속18)을 보존하고 AmmoKind.InstantShotgun을 현재 RoomFlow 선택지로 추가했다. Q/E3종 순환/우클릭 유지+좌클릭, 독립6발(기존6발 불변), 만량 초기화·대기방2초당1발 보충. 기존 드롭/보급은 원래 샷건만 채운다. 새 탄종은 기존과 같은5펠릿/퍼짐12/사거리5/플레이어8×펠릿·적1×펠릿/방향 넉백6·.28초/.7초 공통 재발사 제한이며 비행 지연만0이다. FireSpreadServer에서 경로·엄폐·대상별 집계를 공통 처리하고 기존 샷건의 travel/18 지연은 유지했다. 새 RPC는 생명 세대도 검증. 빈 선택탄은 기존처럼 일반탄으로 전환. 고갈·종료·메뉴 등 기존 입력 제한 유지.
>
> **표시/비교:** 총구→각 명중점 전체 선을 즉시 그려.09초에 지우고 총구 섬광/착탄을 동시에 표시. 기존 비행 연출과 분리된 풀/정리 경로. UI에 즉발 샷건(시험)/잔량/선택 표시, 새 링 주황색. 기존 샷건 대체나 부채꼴 전체 모든 대상 판정으로 바꾸지는 않았다. 새 탄종이 임시 비교 시안이며 최종 수용 전. 상세 Docs/INSTANT-SHOTGUN-TRIAL.ko.md.
> **검증:** `LocalTools/Analysis/NetworkCombat/room-025114/result.txt` 4인 PASS:서버 호출 중 즉시40피해/원래 탄약 독립/중복·간격 차단/참가자→호스트 즉발, 기존샷건 호출 직후 무피해→비행 후40피해 유지, 고체 엄폐 차단, 이전 생명 거절, 선택·고갈 일반탄 전환/종료·대기 복구·각자 보충. instant-shotgun 화면의 전체 궤적/주황 링/5줄HUD 확인.024916도 기능 PASS이며 촬영이 고갈 후 일반탄으로 덮인 조건을 수정하고 HUD높이를 보완해 최종 재검증했다. Verify Instant Shotgun 메뉴. 임시 요청 제거/재생 정지.



> **2026-10-21 샷건 탄속 시험:** 사용자 요청으로 ShotgunPelletSpeed12→18(1.5배)로 변경했다. 표시 이동과 서버 펠릿 명중 지연은 같은 상수를 사용한다. 최대5거리 비행시간은 약.42→.28초. 일반탄 탄속12/.23초 간격, 샷건 사거리5/간격.7/피해8×5/넉백6·.28초/잔량 및 드롭 규칙은 유지. 강한 타격감의 초기 속도 시험이며 최종 수용은 사용자 평가 전.
> **검증:** room-022549/result.txt 4인 PASS:참가자 실제 샷건 명중·파괴 및 기존 피해/드롭/재생성/경기 복구. 탄속 상수18을 표시와 서버 지연이 공유하는 참조 확인. 임시 요청 정리 후 재생 정지.



> **2026-10-21 사격감/접촉 넉백 후속:** 사용자 진행 승인 및 일반탄 재발사 간격 연장 요청으로 arena 일반탄 .15→.23초. 유일 설정 ArenaObjectiveMatch.prefab normalShotInterval을 서버 AmmoRules.ShotInterval로 공유하고 소유자 발사/서버 수락 모두 읽는다. 옛 씬/useReadyRoom=false 간격 유지. arena 꼬리/앞폭은 일반 .18→.42/.032→.05, 샷건 .18→.32/.032→.045, 진행거리까지 꼬리 제한. 탄속12/명중 지연/사거리/피해/샷건.7초·넉백6/.28초는 유지.
>
> **반응:** 적 전체 몸체.1초 피격 섬광, 파괴.35초 불꽃 조각(초기.12초 밝은 섬광)을 표시한다. 드롭 앞 sorting60이며 표시 전용/종료 정리. 접촉 피해 시 서버 승인으로 적 반대쪽 속도2.4/.12초만 밀고 상승속도를 더하지 않는다. 기존1초 피해 간격당1회, 사망 시 안 밂. 공통 ApplyHitPush로 샷건 값은 유지하고 기존 빠른 피격 위치 전파를 재사용한다. 수치 최종 수용은 사용자 평가 전.
> **최종 검증:** `LocalTools/Analysis/NetworkCombat/room-022032/result.txt` 4인 PASS:전원 프리팹 일반탄 간격.23 공유/실제 연사 수락, 참가자 좌측 즉시 넉백·상승 없음·.12초 해제/1초 피해 간격, 실제 일반탄/샷건 적 파괴·효과·드롭·재생성·종료/새 경기 복구. normal-tracer/patrol-hit-tracer/patrol-destruction 화면 확인. 최초021608도 기본 PASS.021827은 움직이는 적의 접촉 경계에 한 번만 배치해 피해 자체가 안 들어온 검사 실패이며 실제 접촉까지 배치를 유지하고 짧은 반응 기록을 보완했다(게임 규칙 변경 없음). Verify Patrol Enemies 메뉴, 임시 요청 제거/재생 정지.



> **2026-10-21 왕복 적 시안:** 사용자 승인으로 전투 지상(-16~-11 /18~21)을 왕복하는 소형 순찰 로봇2개를 추가했다. 이동1.2/체력4(일반탄1회1, 샷건펠릿1개1)/파괴10초 뒤 재생성/접촉10피해·플레이어별1초 간격. 몸은 통과하며 점프 회피/사격 파괴로 지나가는 이동 트랩 후보다. 서버가 체력·세대·시작/재생성 시각·접촉 피해와50% 샷건1발 일회성 드롭을 결정한다. 파괴 중 그림·탄환 충돌·접촉 피해 해제, 이전 생명 지연 타격/중복 처치 드롭 거절. NetworkList와 공유시계로 전원 위치 계산, 별도 네트워크 프리팹 없음. 종료/대기 제거·새 경기 복원. 기존 고정+3보급/일반탄·샷건 탄속/플레이어 피해·넉백/미술·이동은 유지했다. 포탑/비행드론/문·맵 밖 지상형 스폰과 아이템S표기·스태미나 아이템은 후속 구상이며 미구현.
>
> **검증:** `LocalTools/Analysis/NetworkCombat/room-011043/result.txt` 4인 PASS:위치/구간/시계, 참가자 접촉 피해·간격, 호스트 실제 일반탄·참가자 실제 샷건 파괴, 파괴 충돌 해제,10초 세대 재생성·이전 세대 거절, 드롭1/0 확률 분기(런타임 검사만 강제; 기본.5유지)/샷건1발/중복 방지, 종료·대기·새 경기 복구. patrol-alive 화면 확인. Verify Patrol Enemies 메뉴/Docs PATROL-ENEMY-TRIAL.ko.md. 초기 메뉴 실행은 재컴파일 대기 전 시도하여 미발견이었고 재컴파일 후 정상 실행, 최종 콘솔 오류0. 임시 검사 요청 제거/재생 정지.
>
> **탄환 질문:** 현재 꼬리.18·앞폭.032/뒤폭.004이며 탄속은 실제 명중 지연과 연결된다. 이번은 적 구현 우선으로 사용자에게 알리고 기존 수치 보존. 꼬리/앞폭 시각 확대부터 평가한 뒤 탄속을 별도로 조정하는 후속 후보다.


> **2026-10-20 샷건 보급 아이템 임시 배치:** 사용자 최종 계획은 적 처치 드롭이며 고정 생성은 임시 시험으로 승인했다. RoomFlow 경기 지상x±8에 샷건+3/최대6/15초 재생성2개를 추가했다. 서버가 생존/현재 생명/거리/잔량을 검사해 동시 접촉 중1명만 지급하고 가득 찬 사람은 아이템을 소비하지 않는다. 같은 서버 지급·동기화 경로로 `SpawnShotgunDropServer(position,amount)` 일회성 드롭도 생성 가능하며 획득 후 제거/재생성 없음. 적 AI·처치 연결·드롭 확률·회복 아이템은 미구현. NetworkList로 위치/양/활성/재생성 시각 공유, 로컬 시각물만 생성하여 별도 네트워크 프리팹 등록은 없다. 대기·전환·종료에는 지우고 새 경기 두 지점을 복원한다. 기존 대기방 자동보충/일반탄/전투 수치 보존. 설정은 ammoPickupAmount/RespawnSeconds, 상세 Docs/AMMO-PICKUP-TRIAL.ko.md.
>
> **검증:** `LocalTools/Analysis/NetworkCombat/room-104932/result.txt` 4인 PASS:두 지점 전원 일치/만량 보존/동시 접촉1회 지급/15초 재생성/참가자 수집/일회성 생성·최대치 제한·전원 제거/잘못된 위치 거절/사망자 제외/종료 정리·대기 복구·새 경기 초기화. ammo-pickup 화면 확인. Verify Ammo Pickups 메뉴. 임시 검사 요청 파일 제거 후 재생 정지.


> **2026-10-20 표시 위치/연습 발판·보급 후속:** 스태미나 막대를 발아래에서 머리 위 HP바 바로 아래(+11px)로 이동했다. 대기방에서는 준비 라벨 아래(+28px)이며 만량 숨김은 유지한다. 대기방의 수평 단방향 발판도 발바닥이 위에 도달하기 전까지 충돌을 무시하여 옆 진입 차단을 해소한다(경기맵의 기존 flat 규칙은 보존). 대기방에서만 샷건 사격 중단2초 후2초당1발 서버 보충, 경기/전환/종료에는 보충하지 않는다. 일반탄 자동충전과는 별도다. 대기방 커서는 버튼 조작 때문에 제한 해제를 유지한다. 수류탄은 별도 아이콘+개수 표시 후보이며 기능 미구현.
> **집중 검증:** `LocalTools/Analysis/NetworkCombat/room-103731/result.txt` 4인 PASS:양쪽 소유자 발판 옆 통과 및3단 순차 착지, 대기방 샷건 자동 보충/경기 중 보충 없음, 머리 위 스태미나 표시·만량 숨김, 기존 준비→전환→경기→대기 복구. practice-stamina 화면 확인.



> **2026-10-20 대기방 사격 연습/HUD 보완:** 사용자 요청으로 RoomFlow Waiting에서 일반탄/샷건 사격·피해·넉백을 허용하되 서버 HP 최소1로 사망을 막는다. Starting/Returning/Countdown/Finished는 기존 전투 차단 유지. 일반탄은 기존 자동충전, 샷건은 기존 유한6발이며 경기 진입/대기 복귀 때 체력/두 탄종/스태미나 초기화. 대기방 커서 제한은 해제 유지, 준비/시작/퇴장 UI 영역 위는 발사 입력 및 조준 링 차단. 연습 사격은 준비 상태를 풀지 않는다. 전환 중 캐릭터 준비 라벨/체력/탄약/세션 메뉴를 숨겨 검은 화면 잔상을 없앴다. 본인 발아래58x5 스태미나 막대는 소모/회복 중만 표시하고 만량·사망·전환·종료에 숨긴다. 기존 왼쪽 아래 검증 수치는 보존.
>
> **검증:** `LocalTools/Analysis/NetworkCombat/room-102242/result.txt` 4인 PASS:호스트 실제 일반탄→참가자 피해/양쪽 반복 치명피해 HP1/참가자 샷건·일반탄 충전/소모시 발아래 막대·만량 숨김/전원 준비·전환 차단/진입 자원 초기화/카운트다운 사격 거절/경기 치명피해 정상/종료·대기 복귀. practice-stamina/transition-clean 화면 확인. Verify Ready Room Practice 메뉴 추가, 기존 검사의 대기방 사격 차단 기대도 새 정책으로 변경. 현재 전환은 같은 씬 안 구역 이동용 임시 화면이며 실제 다른 씬 로딩은 후속. 무제한 보급 아이템/튜토리얼 공간 확장·탄약 UI 미술/특수탄 아이콘과 색·처치 점수는 사용자 후보로 기록만 했으며 미구현. 탄약 UI는 검증 시안이며 최종 디자인 수용 아님.


> **일반탄 자동충전/링(2026-10-20):** 현재 arena RoomFlow 일반탄12발/최종 유효 발사1초 뒤/초당4발, 원본은ArenaObjectiveMatch.prefab의 normalAmmo*3필드. 서버 AmmoRules/잔량 NV·생명 검증 사격, 과거∞RPC는 현재 경기에서 거절. 사망/종료 정지, 부활/경기/대기/접속 만량. 실제 마우스 위치12시→시계방향 분할 링(일반12청록/샷건 활성6노랑/부분 충전)과 우측 구석 숫자, 플레이 OS커서 숨김·메뉴/대기/관전 등 복구. 기존 사거리X/샷건 별도 잔량·조작·수치 유지. useReadyRoom=false/옛 씬은 일반탄∞. room-100016 4인 전체 탄약·복구·UI PASS 및 화면 확인, Verify Normal Ammo 메뉴/Docs NORMAL-AMMO-TRIAL.ko.md. 차징도 막대가 아닌12시→시계방향 원형이라는 사용자 교정 기록; 차징/수류탄 무기 자체는 아직 미구현. Relay 연결은 사용자 명시 요청으로 보류하며 다음 작업으로 자동 착수하지 않는다.

> **시간 설정 단일화:** 사용자10초 부활 질문은 의견 단계로 남기고 현재6초 유지. 시간의 유일 설정은 ArenaObjectiveMatch.prefab의 respawnSeconds6/countdownSeconds3이며 C# 숫자 기본값 중복 없음. 실행 시 서버가 두 값을 NetworkVariable로 공유하고 부활/각 화면 남은초/시작 타이머/검사는 RespawnSeconds/CountdownSeconds를 읽는다. 프리팹 수정은 다음 실행 반영(런타임 설정 UI 없음). room-093257 4인 프리팹=서버=참가자 값·3단·부활/관전/종료/복귀 PASS. 아래의 'C#에도6명시'는 이전 단계 기록이다.

> **최신 후속(2026-10-20):** 사용자 재평가로 대기방 좌우 각3단(윗면1.58/3.08/4.58, 중단 안쪽 배치)의 단방향/청록 점선 발판, 고체 주황 실선/배경 흐린 환기구. 전역 이동속도·점프력 보존. 전원 도착 후3초 카운트다운→경기 시작. 사망 대기는3초에서 임시6초로 연장하여 C#과ArenaObjectiveMatch.prefab에 명시 저장. 자동부활/수동 우회 차단/종료 시 부활 중지, 사망 대기 중 생존자 관전과 이전·다음/대상 사망 대체/부활 시 본인 복귀 유지. 관전자 전용 참가 아님. room-092858 4인 집중 PASS:양쪽3단 순차 등반/6초 대기·복귀/관전/종료 차단. 기본 전체흐름085323도 보존. 기존 Relay 참가코드 코드가 있으나 현재 arena localhost7783에는 미연결. 코드 입력 없는 입장은 Lobby/방 목록에서 내부 전달하는 흐름을 추가해야 하며 Relay만으로 자동 방 검색되는 것은 아님. 실제두PC Relay검증은 다음 후보(LAN 선행 필수 아님). 이번에 온라인 연결 변경 없음.

> **현재 arena 준비방 흐름(2026-10-20):** 일반 실행은 작은 준비 공간 → 본인 준비/취소 → 방장 포함 2~4명 전원 준비 후 방장 시작 → 전환/기존 3분 점유 경기 → 결과 → 방장 대기방 복귀다. 대기에서는 이동·점프 허용/사격·피해 차단, P/본인/방장/준비 표시와 나가기 제공. 참가 인원 변경/복귀 시 준비 해제. 경기 중 접속 거절 후 대기 복귀 시 재입장 가능, 1명만 남으면 종료. 준비 명단은 단일 NetworkVariable 스냅샷으로 동시 재입장 중복을 방지한다. `room-083830/result.txt` 4인 전체 순환 PASS 및 화면 확인. 상세 `Assets/RightWalkPrototype/Docs/READY-ROOM-FLOW.ko.md`. 기존 집중 검사 도구들은 `session.useReadyRoom=false`로 옛 절차 유지. 방 검색/튜토리얼/모드 선택/정비/전원 재경기 투표/방장 승계는 미구현 후속이다. 이전 '재대기 흐름 미구현' 메모는 이 항목으로 대체한다.

> **달리기 스태미나 추가 완료:** 사용자 명시 요청으로 arena 네트워크 플레이어에만 스태미나100/초당25소모/사용 중단1초 후 초당20회복을 연결했다.0이면 걷기로,20 이상 회복하면 다시 달리기 허용(Shift 유지 시 자동 재개 가능). 서버 잔량/고갈 상태와 소유자 달리기 의도를 동기화한다. 본인 왼쪽 아래HP 패널에 수치/청록 막대, 고갈 시 회복 중 표시. 걷기/앉기/점프 자체 소모 없음. 공중에서도Shift+방향으로 달리기 속도를 사용 중이면 지속 달리기 소모만 적용. 벽에 막혀 실제 달리기 속도가 안 나면 소모 안 함. 메뉴/종료는 달리기 차단, 사망 중 잔량 정지, 복귀/새 경기/새 접속은 최대치로 초기화. 단순 위치 초기화는 채우지 않는다. 옛 로컬/네트워크 시험 씬은 스태미나 제한 없이 유지.
>
> **검증:** `LocalTools/Analysis/NetworkCombat/objective-120200/result.txt` PASS:4인 만량 시작, 호스트 소모/타인 잔량 독립, 고갈 걷기/지연 회복/재허용, 걷기·앉기+Shift·점프 무소모, 메뉴 차단, 참가자 소모·서버 동기화, 사망 정지·본인 복귀 만량, 새 경기 전원 만량. stamina-running/empty 캡처 확인. Verify Sprint Stamina 메뉴가 집중 검사 경로. 임시 ObjectiveReviewRequest.json/.meta 제거.
>
> **아직 변경 안 한 후속 의견:** 비활성 다음 구역을 숨기거나 더 다른 색으로 구별하기, 경기 후 호스트 즉시 재시작 대신 재대기/준비공간 복귀/전원 동의 재경기·누군가 퇴장하면 재대기하는 흐름은 사용자 메모이며 방식 미확정/미구현. 이번 범위는 달리기 스태미나만이다. 점프 스태미나 소모는 사용자 요청이 아님.


> **점유 사각형/경사로 판정 수정 완료:** 사용자 승인으로 ArenaObjectivePoint.Contains의 발높이±.22 조건을 제거하고 실제 Zone fill 표시 bounds를 기준으로 발 중심(x=캐릭터 루트중심, y=충돌체 하단)을 검사한다. 발은 사각형 내부(바닥 접촉 여유.05)이고 접지/생존/현재 생명이면 인정하며, 머리/총/몸 전체 포함을 요구하지 않는다. 구역 안 경사로는 득점하고 점프·낙하·좌우 중심 밖·위아래 다른 층·시체는 제외한다. 캐릭터·지형·시체 물리·게임 규칙 후보는 변경 안 함.
>
> **최신 집중 검증:** `LocalTools/Analysis/NetworkCombat/objective-114604/result.txt` PASS:4인, 실제 서쪽 중단 West upper ramp에서 호스트/클라이언트 득점과 전원 점유 동기화, 실제 점프 상승 중 득점 정지/착지 재개, 좌우 경계 안/밖(몸 일부 겹쳐도 중심 밖이면 제외), 아래층과 구역 위 임시 고체 발판 제외, 경사로 사망자 제외. ramp-scoring 캡처 확인.114343은 위치 초기화 직후 잠깐 비접지인 상태를 점프 검사로 읽은 검사 오류이며 상승 속도/JumpCount 조건으로 보완했다. 메뉴 Verify Objective Bounds는 새 조건만 집중 검사, 기존 전체 경기 검증110225도 보존. 임시 ObjectiveReviewRequest.json/.meta 제거.


> **사용자 최신 후속:** 도그파이트식 목숨/킬 목표 모드는 후보 메모. 기본 구상은2~4인 대기실 전원 준비→게임방이며 난입 전제가 아니다(기존 중도 접속 검사는 복구 검증). F 상호작용 제약 의도는 점유자의 사격+득점 동시 이점을 줄이려던 것임을 보존한다. 자세한 메모는 combat-system-design-notes.ko.md 최신 항목.
>
> **알려진 미수정 문제:** ArenaObjectivePoint.Contains의 발높이±.22 조건이 표시 사각형 높이2.35와 불일치하여, 구역을 통과하는 경사로에 서면 득점 제외됨. 몸 전체 포함을 요구하는 판정은 아님. x는 루트 중심±halfWidth(1.4/1.0), 접지/생존도 요구. 이번 턴은 원인 확인/기록만 하였으며 게임 코드 변경 없음. 시체 경사로 미끄러짐은 사망 시 이동 중단으로 생전 미끄럼 방지가 해제된 물리 동작이며, 시체는 득점·경합·피격 후보에서 제외되어 현재 낮은 우선순위. 기존 objective-110225 PASS가 경사로 안의 점유/최종 사용감을 검증한 것은 아님.


> **구역 점유 경기 시안 완료:** 사용자 승인으로 자동 점유 득점/사격 가능 규칙을 별도 목표 프리팹 `ArenaObjectiveMatch.prefab`과 현재 arena 씬에 연결했다. 하단중앙→서쪽중단→상단→동쪽중단4후보 고정 순환,3분/30초이동/5초예고/단독1초1점, 같은 층 바닥 생존자만 인정/2명 이상 경합 시 중단. 서버가 점수/시간/지점/승자를 결정하며 호스트 수동 시작/다시 시작(체력·생명 세대·위치·점수 초기화). 퇴장 점수 제외/재접속0점, 중도 접속 상태 동기화. 종료 시 입력 차단/커서 해제/서버 새 사격·지연피해 거절, 결과·점수표 표시. Session의 optional arenaObjectivePrefab 필드로 생성하므로 옛 씬은 유지된다. NGO scene management는 기존 false이며 동적 프리팹을 전원 Start에서 등록한다. 상세 `Docs/HACKING-OBJECTIVE-TRIAL.ko.md`.
>
> **최신 검증:** `LocalTools/Analysis/NetworkCombat/objective-110225/result.txt` PASS:4화면 대기/클라이언트 시작 거절, 단독 득점/사격 가능/경합 중단, 사망 제외·복귀, 다른 층 제외, 지점 예고·이동 일치, 재접속 현재 경기/0점/명단 정리, 종료 점수·승자 일치/점수 고정/서버 전투 거절, 재시작 전체 체력·점수 초기화,0점 무승부, 호스트 종료 상태·입력 해제. 이전105921도 기본 PASS;105139는VP의 오래된 씬 참조로 NetworkConfig mismatch,105400은 재접속 검사 참조 경쟁,105616은 명단 동기화 이전 검사로 중단된 기록. 최신 씬을VP에도 다시 열었다. preview/result 캡처 확인(구역 이름은HP와 겹치지 않게 옆에 표시). 임시 ObjectiveReviewRequest.json/.meta 제거. 스태미나는 메모만 있으며 미구현. 최종 모드/수치 수용은 사용자 평가 전.


> **단일 고체 엄폐 높이 시험:** 사용자 승인으로 왼쪽 지상 `Ground west cover`만 높이1.8 고체로 바꾸고 주황 연속 윗선 표시. 나머지는 통과형 시험 상태. 측정 `cover-102409`:서기 피격 상단2.165/앉기1.745, 서기 수평 총구1.429/앉기.978. 따라서 완전한 앉기 보호와 서서 수평 사격을 동시에 허용하는 높이는 현재 자세로 불가능. 새 상자는 숨기용이며 공격은 옆 노출/점프/충분한 거리에서 상향 조준 필요. `cover-103032`에서 호스트/참가자 양방향 실사격:앉기 무피해·서기 상체피격·재앉기 무피해·몸 통과 차단 PASS, 캡처 확인. 102840도 판정 PASS이나 피격자가 자기 머리 위를 조준하던 검사 자세를 정리해 재검증했다. 102201은 초기 참가자 대기시간,102642는 검사 파일 읽기 경쟁 오류. 캐릭터/피격 범위/자세 변경 없음. 최종 엄폐 사용감은 사용자 평가 전. 임시 CoverReviewRequest.json/.meta 제거.


> **표시선 수정/엄폐 설계 재검토:** 사용자 요청으로 상자 아래 바닥선을 가리던 foot seal을 제거했다. 별도 표시선의 오래된 좌표를 사용하던 배치 오류를 Physics2D.SyncTransforms 후 생성으로 수정하고, 선은 해당 충돌체 자식/상자 원래 미술도 상자 자식으로 부착했다. 상자25개 선 조각 정렬·소유자 이동 추종/가림 제거 검사 및 `surface-marking-fix/layout.png` 화면 확인, 저장/재생 정지. 게임 충돌/사격 규칙은100049 시안 그대로이며 몸 통과+탄환 차단 엄폐물의 일방적 공격 위험이 사용자에게 지적되어 최종 수용 아님. 권장 후보: 몸/탄환 모두 막는 고체 엄폐물+점프/우회 공간, 통과형 발판은 이동용으로 분리. 특수 관통탄/상자 밀기·굴리기는 아이디어일 뿐 미구현.


> **고체 발판/통과형 엄폐물 시안:** 사용자 재설계 시험 승인으로2층 좌우·3층 데크를 고체로 복원했다. 중앙 격자·경사로는 단방향 유지. 상자는 ArenaCover+단방향 몸 충돌로 옆/아래 통과·위 착지/앉기+점프 내려가기, 탄환은 양방향 차단한다. 지상 상자 x±3.6(중앙 격자 아래)으로 이동해 고체 천장 간섭을 회피했다. 주황 연속선=고체 / 청록 점선=몸 통과, 상자 밑 접합부로 바닥 선을 가렸다. 검사 `arena-controls-100049`:상자 통과/착지/내려오기·양방향 탄환 차단·고체 데크 탄환 차단 및 기존4인 이동/낙사 복귀 PASS, 화면 확인. 최종 디자인/엄폐 규칙 수용은 아직 전이다. 빌더의 Apply Arena Traversal Layout 메뉴는 이 씬만 수정한다.


> **내려간 경사로 즉시 재진입:** 아래 다른 바닥에 착지하면 deliberate drop 상태를 즉시 해제한다. 기존에는 머리까지 원래 경사면 아래로 빠지거나1.5초가 지나야 해제하여 낮은 경사로 재진입이 늦었다. `arena-controls-092834`:좌우 하부 경사 x±19에서 내려가기 직후 점프/동일 경사로 착지 및 기존 이동·4인 연결·낙사 복귀 PASS. 092716은 단일 점프로 도달하지 못하는 x17에서 재진입을 요구한 검사 실패라 위치를 수정했다.


> **낮은 경사로 내려가기 수정:** TryDrop에서 아래 착지점까지 앉은 키 이상의 간격을 요구하던 제한을 최소 접촉 간격.15로 줄였다. 선택한 단방향 경사로를 무시하여 통과하므로 캐릭터 키만큼의 빈틈은 필요하지 않다. 동쪽 하부 경사 x17/x19에서 앉기+점프로 바닥 착지/서기 및 기존 전체 이동·4인 연결·낙사 복귀 검증 `arena-controls-090607` PASS. 상자와 발판 구조 변경 제안은 미확정으로 보류했다.


> **경사로/공중 발판 후속:** 경사로 아래 서기 검사에서 무시 중인 충돌체와 단방향 발판을 제외했다. 중간층 좌우 및 상층 얇은 발판도 단방향 충돌로 전환하여 상자 위에서 서기/위로 통과가 가능하다. 일반 고체 천장은 서기를 막는다. 새 맵 전용 걷기2.4/달리기3.6/앉기.9/점프 속도6.5(기존 프리팹·씬 및 그림/배율 보존). `arena-controls-083730`에서4인 연결 유지, 두 서기 회귀·고체 천장 차단·발판 더블점프 통과·전체 이동/경사로/낙사 복귀 PASS. 083537은 층 높이보다 낮은 단일 점프를 요구한 검사 실패이며 더블 점프 검사로 바로잡았다. 윗키 경로 선택/앉아 하강 경로 선택/점프 발판은 아이디어로 보류, 전체맵 입력 차단은 유지.


- Latest arena controls fix/verification: `LocalTools/Analysis/NetworkCombat/arena-controls-080531` (four peers, runtime+network PASS). Sole arena camera runs before aim, old movement camera is detached only in arena. Owner-only `ArenaPlatformMotor` prevents idle slope slide/auto-mount from below, admits jump landings, supports crouch+Space safe drop within4.2 on one-way surfaces. Arena-only range24; ESC menu/cursor confinement and focus/input gates. Life-pose generation guard prevents immediate pit re-death after revive. Movement/jump/character scale/camera zoom remain unchanged; the user's full-map-size remark was future scale planning, not rejection of the overview. Use compiled `RelayArenaEditorReview` for long connected tests; MCP CodeDom compilation can stall transports. Remove its temporary ArenaReviewRequest.json/.meta after testing.

- First arena prototype: `Assets/RightWalkPrototype/Scenes/Network/RelayArenaPrototype.unity`, guide `Assets/RightWalkPrototype/Docs/RELAY-ARENA-PROTOTYPE.ko.md`. Separate from the preserved network trial. Three levels, grated ramps/crossing, cover, timed retracting bridge and server fall death, four spawn points, arena camera/Tab overview. Four-editor verification `LocalTools/Analysis/NetworkCombat/arena-3` passed traversal, flat/double jump, grate/cover shot rules and pit revive. This is a playable design candidate, not user acceptance or authorization for enemy AI/scoring/final art. All MPPM peers must open the same arena scene.

- Current accepted character entry: `Assets/Characters/Current/Scenes/Character_Complete_Playground.unity`; reusable prefab: `Assets/Characters/Current/Prefabs/Character_Complete.prefab`. Shared source art/code remain in `Assets/RightWalkPrototype`. Old scene paths were organized under `Scenes/Archive` or `Scenes/Network`; see `Assets/Characters/Current/Docs/Scene-Catalog.json`. Preserve the accepted baseline and original archived scenes.
- Latest network character/combat work: `Assets/RightWalkPrototype/Docs/NETWORK-CHARACTER-CURRENT.ko.md`. The network trial uses current motion plus server player damage/HP and synchronized death/manual revive. MPPM Main + Player 2 verification: `LocalTools/Analysis/NetworkCombat/run-3`. Existing executable builds predate combat changes. Do not treat the older pre-connection audit as current status.
- Latest crouch feedback: the user reports multiple opinions that crouch movement looks unnatural ("duck feet") and should eventually be replaced. Preserve the saved checkpoint, but do not treat it as final design acceptance. Pose replacement is explicitly deferred; do not implement it during combat work.

- Minimize token use across model/mode changes and resumed sessions. Keep replies and updates concise; avoid repeating history, broad file dumps, speculative exploration, and unnecessary tool calls.
- Inspect only relevant code. Use focused verification proportional to risk; do not repeat successful checks without a change or unresolved concern. Do not skip essential correctness or visual checks.
- Do not launch subagents or change models/reasoning settings unless the user requests it.
- Preserve existing artwork, weapon geometry, character scale, and working behavior outside the requested change. Prefer small edits and reuse existing assets.
- Treat brainstorming and future requests as notes, not implementation authorization. Distinguish confirmed requirements, candidates, and unresolved choices.
- Read `HANDOFF-2026-10-19.ko.md` for the current home-to-academy handoff; `HANDOFF-2026-10-16.ko.md` is historical. On 2026-10-19 the user accepted the academy crouch/crouch-movement version (stable-v2 plus an 8px rearward offset of both crouching legs); earlier rejected trials are historical. Preserve working motion and original scenes; inspect current state before further edits. Browser source: `LocalTools/Analysis/FrontShootingSandbox`. Local pose trials are separate from the production/network player.
- Read `LocalTools/Analysis/combat-system-design-notes.ko.md` for the planned combat direction when relevant.
