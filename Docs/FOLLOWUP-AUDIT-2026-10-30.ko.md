# 추가 점검 — 2026-10-30

## Windows 단독 Player 성능

Unity 6000.3.18f1, Direct3D 11, NVIDIA GeForce RTX 4050 Laptop GPU. 전용 계측 빌드를 1366×768, 목표 60FPS로 실행했다. `-nographics`/`-batchmode`를 사용하지 않았고 Windows 창은 비표시 상태로 두었다. Editor는 정지한 상태이며, 이 측정 중 다른 빌드나 브라우저 Web 게임 로딩을 실행하지 않았다. 전면에 표시된 창의 GPU/present 성능 및 다른 기기 성능까지 보장하는 검사는 아니다.

보통 AI 0·1·3명 순서로 각 45초 경기를 실행했다. 경기 시작 후 8초를 준비 구간으로 두고 다음 30초를 계측했다. 인간 플레이어는 입력 없이 대기했고, NPC와 AI는 정상 판단·전투를 수행했다. 세 경우 모두 결과·복귀·재접속까지 완료, 런타임 오류 0.

| AI | 평균 FPS | P95 프레임 시간 | P99 프레임 시간 | 프로세스 CPU 합계/프레임 | Unity 할당 메모리 |
| --- | --- | --- | --- | --- | --- |
| 0명 | 59.96 | 16.72ms | 16.90ms | 3.20ms | 90.5MiB |
| 1명 | 59.77 | 16.75ms | 17.04ms | 6.31ms | 92.9MiB |
| 3명 | 59.83 | 16.71ms | 17.36ms | 6.13ms | 98.1MiB |

P99는 측정 프레임의 약 99%가 해당 시간 이내였다는 뜻이다. 평균 FPS가 60 근처여도 긴 프레임이 전혀 없다는 뜻은 아니다. AI 1명에서 최대 61.70ms, 3명에서 최대 31.82ms를 관측했다. AI 1명의 CPU 수치가 3명보다 큰 것은 전투·파밍·경로 상황과 스케줄링이 같지 않기 때문이며, AI 수와 부하의 정밀한 선형 관계를 측정한 실험은 아니다.

3명 구간에서 1,795프레임 동안 AI Update 합계는 약 595ms(전체 AI 합산, 프레임당 약 0.33ms), 경기 Update 합계는 약 1,241ms(프레임당 약 0.69ms)였다. 경로 Route의 최대 단일 호출은 약 21.80ms였다. 계측 구간은 중첩되므로 각 항목을 합쳐 전체 CPU 사용량으로 해석하면 안 된다. 지속적으로 프레임을 떨어뜨리는 AI 병목은 이번 표본에서 재현되지 않았으나, 경로 재검사·전투 판단의 일시적인 긴 호출은 최적화 후보로 남는다.

Unity 할당 메모리는 전체 프로세스 또는 GPU 메모리가 아니다. Mono의 `Process.WorkingSet64`가 0을 반환해 그 값은 미지원으로 취급했다. 외부 Windows 조회 표본은 작업 집합 392,900,608 bytes, private bytes 1,143,312,384 bytes였다. GPU 및 시스템 예약·공유 영역 등을 포함한 정확한 분해나 장시간 누수 여부는 측정하지 않았다.

자료: `LocalTools/Analysis/Followup-2026-10-30/Before/result.json`, `external-memory-sample.json`. 전용 빌드는 `ARENA_PERFORMANCE_AUDIT`에서만 계측 코드를 포함한다. 일반 빌드에는 runner와 계측 호출이 포함되지 않는다.

## 작은 캔버스 UI

하단 조작 패널을 탄약 HUD 왼쪽의 남는 공간에 맞춰 배치하고, 마우스 입력 차단도 같은 Rect를 사용하도록 통일했다. 제목과 AI 인원은 줄바꿈을 끄고 필요한 폭을 확보했다. 800~1920 폭의 6개 크기에서 화면 경계·HUD 비겹침 검사 28개가 통과했다. 1366×768 및 1920×1080의 기존 640폭/중앙 정렬은 유지했다.

실제 브라우저 기본 캔버스 CSS 960×600, backing 1200×750(devicePixelRatio 1.25)에서 제목, AI 3/3과 세 버튼, 하단 탄약 HUD의 분리를 확인했다. 6개 크기의 28개 검사는 Rect 경계 검사이며 모든 해상도의 실제 GUILayout/시각 검사를 뜻하지 않는다.

**남은 문제:** 좁은 화면에서 참가자 4명의 좌측 명단이 벽 안내판의 첫 조작 카드 일부를 가린다. 이번에는 하단 패널/글자 잘림만 수정했고, 명단과 월드 게시판 전체의 반응형 배치는 후속 작업으로 남겼다.

## 웹 저장·재입장

- 새 Web 빌드 `Builds/WebFollowup-20261030/` 성공: 오류 0, 기존 경고 5, 993.844초, 153,329,067 bytes. 로컬 검사 시간을 위한 비압축 출력이며 전일 Brotli 배포 크기 55,094,546 bytes와 직접 비교하지 않는다. 검사 뒤 설정은 원래 Brotli로 복원했다.
- 숨김 로컬 브라우저에서 영문 닉네임 `web1030` 저장 안내, 새로고침 후 이름 유지, 효과음 65→50% 변경 및 새로고침 후 유지 확인. 이후 기본 이름 `플레이어`, 전체 100%/효과음 65%로 복원하고 화면으로 확인했다. 실제 한글 조합 입력 검사는 아니며 HTML IME 연결은 여전히 필요하다.
- 혼자 연습, AI 3명 추가, 로비 나가기 확인창, 재입장 후 AI 0명 초기화, 다시 로비 복귀를 확인했다. 전일의 실제 AI 경기 시작/득점 확인과 별개의 재입장 검사다.
- 브라우저 오류 0. persistentDataPath 수동 동기화 폐기 예정 경고가 2회 기록됐다. 자동 동기화 전환·사이트 데이터 차단/삭제·다른 브라우저의 저장은 미검증이다.
- 증거: `web-room-fixed.jpg`, `web-reentry.jpg`, `web-name-saved.jpg`, `web-name-reloaded.jpg`, `web-volume-saved.jpg`, `web-volume-reloaded.jpg`, `web-settings-restored.jpg`, `web-console.json`.

## 비공개 인터넷 Relay

검사용 Windows 두 프로세스를 별도 익명 인증 프로필로 실행했다. SDK에서 실제 비공개 세션임을 확인한 뒤 세션 ID로 참가했다. 일반 빌드는 기존 방 공개 설정을 그대로 사용하며, 비공개 강제 및 파일 명령 runner는 `ARENA_NETWORK_AUDIT`에서만 포함된다.

`Relay/result.txt`의 45개 항목 통과(명령 수신 확인 포함): 접속, 같은 한글 이름의 서로 다른 참가자, 준비/시작, 지난 생명·NaN 사격 거절, 원격 일반탄의 실제 피해 및 양쪽 HP 일치, 보호막 50 복제, 같은 프레임 8회 요청 연사 제한, 라이플/유탄 이벤트, 사망/자동 부활, 양쪽 결과·점수 일치, 대기방 복귀, 접속 해제. 일반탄 피해 검사는 NPC를 제거한 격리 배치에서 승인된 원격 사격 횟수 증가와 HP 감소를 함께 확인했다. 별도 앱 두 개지만 동일 PC이며 실제 인터넷 Relay 경유 시험이다.

종료 때 두 프로필의 로컬 인증 토큰을 지우고 로그아웃했으며, 방장 세션의 Deleted 상태를 확인했다. 기존 사용자의 Unity 로그인·프로젝트/조직 ID는 바꾸지 않았다. 익명 계정 자체 삭제를 뜻하지 않는다. 검사용 프로그램과 임시 서버는 종료한다. 실제 다른 PC, 지연/패킷 손실, 장시간 연결, Web/Windows 혼합 Relay는 미검증이다. 공개 업로드는 하지 않았다.

## 최종 빌드·복원

- `Builds/BulletShot/2DNetcodeGame.exe` 일반 Windows64 Mono 빌드 성공: 오류 0, 기존 경고 5, 60.152초, 710,041,050 bytes, Development 꺼짐. `windows-build.json` 및 메시지 기록 보관.
- 배포 `Assembly-CSharp.dll`에 `ArenaNetworkAudit`, `ArenaPerformanceAudit`, `ArenaPerformanceCounters`가 없음을 확인했다. 대응 전용 빌드 DLL에서는 각 이름이 존재하는 양성 대조도 확인했다. 일반 빌드에 검사 runner/파일 명령/계측이 들어가지 않는다.
- Editor PID 18992: Windows64 대상, 재생 정지, 컴파일 오류 없음, 활성 씬 dirty 아님. Console 최종 오류 0/경고 5. `git diff --check` 통과(줄끝 변환 안내만 있음).
- Web compression Brotli 및 플랫폼 전환이 추가한 WebGL batching 항목을 복원한 후 `ProjectSettings/ProjectSettings.asset`의 Git 내용 차이가 없음을 확인했다. 전일 작업과 이번 변경은 미커밋 상태로 보존했다. 밸런스·음원·지형·물리·프로토콜 v7은 이번에 변경하지 않았다.
- 임시 브라우저 탭과 로컬 서버를 닫았다. Relay 두 프로세스는 테스트 종료 후 정리됐고 양쪽 cleanupDone 및 빈 오류 문자열을 확인했다. 공개 업로드·커밋·푸시는 하지 않았다.

다음 확인 우선순위는 좁은 화면의 명단/월드 안내 겹침, Web 한글 IME 연결, Web끼리/Windows 혼합 Relay 및 호스트 배경 탭, 실제 전경 Player의 장시간 성능·메모리다. 이번에 평균 60FPS가 나온 것은 과열·멈춤이 재발하지 않는다는 보장은 아니다.

## 계속 작업 — 작은 화면·웹 입력·연속 경기

사용자 재개 요청 뒤 작업이다. 자료는 `LocalTools/Analysis/Continue-2026-10-30/`에 구분했다. 앞 절의 빌드는 이 후속 소스 반영 전 결과이며, 최신 최종 빌드는 아래에 별도로 기록한다.

- 폭 1280 미만에서는 명단을 178×72 요약 패널로 접어 두고, 클릭하면 기존 4인 카드를 펼친다. 각 클라이언트의 로컬 표시 상태이며 준비/인원은 그대로 보인다. 960/1200/1366 Editor 실화면과 입력 경계·숨긴 행의 입력 해제 등 14개 확인 통과. 처음 250×104 요약안은 제목·천장 표적을 가려 철회하고 더 작은 최종안으로 재검사했다. 펼친 상태의 가림은 사용자가 명단을 연 동안의 의도된 표시다. `room-result.txt`, `room-closed-960.png`, `room-open-1200.png`.
- `ArenaBrowserTextInput`과 Web 전용 jslib로 실제 HTML 입력 폼을 연결했다. 브라우저 입력 동안 Unity의 전역 키 캡처를 잠시 해제하고, 저장/취소·세대 번호·비활성 정리로 뒤늦은 콜백을 막는다. 한글 조합 Enter와 저장 Enter를 구분한다. 기본 데스크톱 입력은 유지한다. 닉네임은 기존 12문자 요소/60 UTF-8 bytes 정리를 사용한다. 방 이름의 32 UTF-16 길이 경계에서 상위 surrogate만 남는 경우도 정리한다.
- 원본 jslib와 SHA-256이 같은 격리 HTML에서 조합 이벤트 중 Enter의 기본 제출 방지, 조합 완료 한글 문자열 전달, Escape 취소를 확인했다. 이는 합성 composition 이벤트 시험이며 실제 OS IME 조합 또는 Unity C# 연동 완료를 대신하지 않는다. 새 일반 Web 빌드에서 통합 확인을 이어간다. `text-harness.jpg`.
- 설치 SDK `NetworkModule.GetRelayProtocol()`이 별도 NetworkOptions 없이 호스트의 Relay 메타데이터로 폴백하는 경로를 확인했다. 참가 옵션에 플랫폼별 `RelayProtocol.Default`를 명시하여 Web은 WSS, Windows는 DTLS를 선택하게 했다. 실제 혼합 시험 결과는 최종 기록에 기재한다.

브라우저 키 캡처와 jslib 연결은 [Unity 6.3 키보드 캡처](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/webglinput/captureallkeyboardinput), [브라우저 스크립트 연결](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/develop/interactingwithbrowserscripting)을 참고했다.

### AI 3명 연속 경기

검사 전용 그래픽 Windows Player에서 보통 AI 3명, 3분 경기 5회를 순서대로 완료했다. 각 회 결과→대기방→연결 해제→새 솔로 방 진입을 포함하며 오류/예외 수집 0, 결과 PASS다. 기존 일반 경기 코드의 점령/몹/부활/자원 규칙을 사용했다. 검사에서만 라운드와 점령 지점 이동 주기를 지정했고, 기존 플레이어 입력은 정지했다. `Soak/result.json`, `Soak/player.log`, `Soak/memory-samples.json`, `soak-summary.json`.

각 경기 8초 준비 뒤 160초 표본의 평균 FPS는 59.85/59.48/59.76/59.86/59.90였다. 이번에는 Web 빌드가 동시에 진행되어 별도의 부하 비교 벤치마크로 쓰지 않는다. 이전 단독 성능 표본과도 조건이 다르다.

Unity 할당 메모리는 97.83→99.45→99.87→100.16→100.12MiB, Mono는 4.08~4.37MiB였다. 외부 Windows에서 측정 구간 종료 뒤 작업 집합 표본은 약 366.73→368.85MiB, private bytes는 약 1074.92→1077.21MiB였다(97개 주기 표본 보관). 초기 증가 뒤 후반 평탄해지는 양상으로 급격한 누적 증가는 재현되지 않았다. GPU 메모리 분해·전경 창·수시간 누수·발열 센서 측정 결과는 아니다.

### 일반 Web 통합과 혼합 Relay

- 후속 일반 Web 빌드 성공: 오류 0/경고 6, 1,729.911초, 153,351,311 bytes(비압축 시험 출력). 추가 경고는 Editor 미컴파일 변경의 후처리/임포터 반영 시점 안내였다. 생성된 IL2CPP 코드에 새 HTML 입력 클래스와 JoinSessionOptions.WithNetworkOptions 호출이 포함됨을 확인했고, 실제 브라우저에서 새 기능을 확인했다. 후처리 코드는 이번에 변경하지 않았다.
- 게임 안의 HTML 입력으로 한글 14자를 제출했을 때 기존 규칙대로 `가나다라마바사아자차카타` 12자 저장, 저장 완료 표시, 새로고침 후 유지, 수정 뒤 취소 시 기존 값 유지, 방 이름 적용을 확인했다. 실제 Windows 한영키 조합을 재현한 입력은 아니며 OS IME 수동 확인은 남는다. `web-hangul-saved.jpg`, `web-hangul-reloaded.jpg`.
- 일반 방 목록으로 Windows 방장↔Web 참가자와 Web 방장↔Windows 참가자 양방향 접속을 확인했다. 각각 한글 이름·준비·경기 시작·결과·대기방 복귀를 확인했다. 두 방향 모두 실제 원격 일반탄 승인과 HP 감소가 있었다. Windows 방장 시험에서는 NPC를 제거한 배치에서 Web 일반탄 4회가 승인되고 방장 HP가 100→60으로 감소했다. Web 방장 시험에서는 Windows 일반탄 2회 승인과 Web 방장 HP 100→80을 기록했다(이쪽 NPC는 격리하지 않았음). Web 결과 화면의 참가자/무승부 표시도 Native 스냅샷과 일치했다.
- Windows 방장 쪽에서 Web 참가자의 보호막 50, 치명 피해 및 생명 세대 증가를 통한 자동 부활도 확인했다. 처음 사격 시도는 종료 시점과 겹쳐 근거에서 제외했으며, 이후 재경기에서 명중을 확인했다. `WindowsHost/after-web-shot.json`, `shield-state.json`, `revived-state.json`, `WebHost/after-shot.json`, 양쪽 결과 사진.
- 두 임시 공개 시험 방은 종료 후 삭제했다. Windows 방장은 삭제 상태 확인/cleanupDone, Web 방장은 Windows 참가자에서 Deleted 이벤트 수신과 접속 종료를 확인했다. 두 검사 프로세스의 별도 익명 프로필 토큰도 정리했다. 새 웹 입력값은 `플레이어`/`해킹 점령전`으로 복원했다. 게임을 외부에 업로드하지 않았다.
- Windows 방장 fixture에서 명령 파일 교체 순간 `UnauthorizedAccessException` 1건이 있었다. 명령이 나중에 수신된 뒤 게임 검사는 진행됐으며, 이 사례를 '전체 오류 0'으로 보고하지 않는다. fixture의 공유 파일 읽기에서 해당 일시 잠금을 재시도하도록 보완했다. Web 방장 Native 참가자 fixture 오류는 0이었다.
- 최종 브라우저 로그에서 WebSocket 옵션 불일치 오류 2건을 발견했다. 접속 자체는 성공했지만 `UnityTransport.UseWebSockets`가 꺼진 상태에서 WSS Relay 데이터를 받았다는 진단이다. Web 온라인 설정 전에 이 옵션을 켜도록 후속 수정했다. 이 수정의 Web 재빌드/검증 결과는 아래 최종 절에 기록한다.
- 배경 탭 시험은 자동화 브라우저의 `document.visibilityState`가 계속 visible이어서 유효한 hidden 조건을 만들지 못했다. 실제 브라우저에서 탭을 바꿀 때의 호스트 지연은 미검증이다. 브라우저 두 개/다른 PC/고지연·패킷 손실 및 Unity Play 업로드도 별도다.

검사 과정의 브라우저 연결 오류는 세션 초기화 후 복구했다. Unity의 백그라운드 컴파일 중 검사 API 시간초과 로그가 빌드 오류 수에 섞인 실행도 보관한다. 코드 컴파일 오류와 구분한다. 마지막 clean 빌드 시도는 오류 0/경고 1, 63.799초였으며 이후 WebSocket 옵션 수정이 추가되었다. 최종 배포본은 아래 최신 기록을 기준으로 한다. 직접 Windows 입력은 Unity 포커스 복구용 Shift 1회였고, OS 마우스 클릭은 하지 않았다.

### 후속 최종 결과 및 정리

- WebSocket 수정 후 일반 Web 빌드 `build_106396971e40`: 오류 0/기존 경고 5, 327.894초, 153,351,407 bytes. 비압축 로컬 시험 출력이다. 앞선 추가 임포트 시점 경고는 이 빌드에서 재발하지 않았다. `web-wss-build.json`.
- 새 브라우저에서 일반 방 목록으로 Windows 방장에 Web 참가자가 접속하고, 반대로 Web 방장에 Windows 참가자가 접속하는 두 경우를 재확인했다. 참가자 2명과 방 이름을 확인했으며 브라우저 오류 로그는 빈 배열이다. 이전의 Use WebSockets 불일치 오류가 사라졌다. 이 마지막 검사는 연결 설정만 바꾼 뒤의 접속 회귀 검사이며 앞선 실제 사격/경기 시험을 반복한 것은 아니다. `wss-web-client-clean.jpg`, `wss-web-host-clean.jpg`, `wss-browser-errors.json`.
- 첫 native 재실행은 샌드박스가 PlayerPrefs 기록을 차단하여 Services 초기화가 실패했다. 종료·cleanup 후 허용된 실행으로 다시 시작한 `WssWindowsHost2`와 `WssWebHost`는 오류 없이 접속했다. 게임 코드 결함으로 오인하거나 최초 실패 기록을 삭제하지 않았다. Windows 방장 삭제/cleanupDone, Web 방장 삭제 이벤트를 Native 참가자가 수신한 뒤 Disconnected/count0/cleanupDone을 확인했다. 양쪽 별도 검사 인증 토큰 정리 완료. `WssWindowsHost/`, `WssWindowsHost2/`, `WssWebHost/`.
- 최종 일반 Windows 빌드 `build_073f80152dfb`: 오류 0/기존 경고 5, 122.184초, 710,045,354 bytes. 출력 `Builds/BulletShot/2DNetcodeGame.exe`, Development 꺼짐. `windows-wss-final-build.json`. 초기 API 시간초과가 섞인 두 시도는 각각 오류 2/1로 별도 보관하며 최종 결과와 구분한다.
- 일반 DLL에서 ArenaNetworkAudit/ArenaPerformanceAudit/ArenaPerformanceCounters 부재, 검사 DLL에서 존재를 다시 확인했다. `git diff --check` 통과. Windows64/Brotli 및 원래 batching 설정 복원 뒤 ProjectSettings.asset 내용 차이 없음. 컴파일 실패 false/재생 정지/씬 dirty false.
- 임시 시험 탭·HTTP 서버·검사 프로세스 종료, 웹 닉네임/방 이름 원래 값 복원. 프로젝트/조직 ID 변경, 공개 게임 업로드, 커밋·푸시 없음. 상세 사용자 확인 목록은 `WORK-STATUS-2026-10-30.ko.md`.
- 마지막 정리 확인에서 PowerShell의 JSON 날짜 자동 변환 때문에 프로세스 시작 시각 비교가 9시간 어긋나 종료가 건너뛰어진 것을 발견했다. 원본 UTC 문자열로 비교하도록 검사 helper를 수정한 뒤 기록된 PID/이름/시각이 일치하는 이번 시험 프로세스만 종료했다. 종료 확인은 단순 요청이 아닌 WaitForExit 및 남은 검사 프로세스 0개/8767 포트 리스너 0개로 검증했다. 게임 런타임 코드는 이 보정과 무관하다.

기존 5개 경고는 Pipeline 설정 자산 부재 1개와 기존 ServerRpc/Physics2D API 폐기 예정 4개다. 이번 범위에서 프로젝트의 Pipeline 기능이나 RPC 형식을 새로 변경하지 않았다. 실제 OS IME, 두 웹 클라이언트, 다른 PC, 배경 탭 호스트, 지연/패킷 손실, 수시간 사용, Unity Play 배포 조건은 여전히 별도 확인 사항이다.
