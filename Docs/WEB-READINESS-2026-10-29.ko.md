# Unity Play 웹 배포 준비 점검 — 2026-10-29

현재 프로젝트를 웹으로 빌드해 배포하는 방향은 가능하지만, Windows 실행 파일을 올리는 방식은 아니다. Web 빌드의 `index.html`, Build, TemplateData 등을 함께 배포해야 한다. Unity Play에는 아직 업로드하지 않았다. [공식 배포 안내](https://learn.unity.com/tutorial/publish-on-unity-play)

## 현재 프로젝트와 조치

- Unity 6000.3.18f1에 Web Build Support가 설치되어 있다.
- 설치 패키지: Netcode for GameObjects 2.12.0, Multiplayer Services 2.3.3.
- 현재 경기 씬 하나가 빌드 목록에서 활성화되어 있다.
- 기존 Web 설정은 Brotli 압축, 브라우저 캐시 사용, decompression fallback 끔, 초기 메모리 32MB/최대 2048MB다. 이 수치는 실제 점유 메모리가 아니다.
- 웹의 직접 UDP 호스트는 사용할 수 없다. 설치된 UnityTransport도 웹에서 Relay가 아닌 서버 시작을 예외로 거절한다. 혼자 연습·튜토리얼에는 외부 소켓이 없는 내부 호스트 어댑터를 추가했다. Editor에서 15개 항목으로 동작과 UTP 복원을 확인했다.
- 앱의 방 목록 지연에 쓰던 `Task.Delay`를 프레임 기반 대기로 교체했다. SDK는 Web을 지원하지만 타이머·스레딩과 UDP/DTLS 제약이 있다. [SDK FAQ](https://docs.unity.com/mps-sdk/faq)
- 인터넷 경기는 기존 Relay 구성을 사용한다. 설치 SDK의 Web 기본 프로토콜은 WSS다. 실제 Web 방 생성/참가 및 Windows와의 혼합 접속은 별도 확인해야 한다. [네트워크 설정 안내](https://docs.unity.com/en-us/mps-sdk/manage-session-network-connection)
- 웹 로비의 게임 종료 버튼을 숨겼다. 브라우저 탭 종료는 사용자가 브라우저에서 한다.

## 배포 전에 해결·검증할 항목

| 항목 | 현재 판단 / 다음 확인 |
| --- | --- |
| 한글 닉네임·방 이름 입력 | 현재 IMGUI/Unity IME 사용. Unity의 Web IME 미지원 때문에 HTML 입력 요소와 조합 완료 이벤트를 연결하는 별도 입력 처리가 필요하다. 한글 글꼴 표시와 입력은 별개다. |
| 방장 탭이 배경으로 이동 | 브라우저는 배경 탭 갱신을 제한한다. 현재 호스트가 판정·적·시계를 진행하므로 다른 탭 이동 시 경기 지연 가능성이 있다. `runInBackground`만으로 해결되지 않는다. |
| Relay 온라인 | 브라우저끼리와 Windows/브라우저 혼합 조합 모두 방 생성·조회·참가·복귀·연결 해제 검사가 필요하다. 서비스 인증/할당을 이번에 실행하지 않았다. |
| 첫 소리 재생 | 브라우저는 사용자 상호작용 전 오디오를 제한한다. 입장 버튼 클릭 뒤 사격·결과음·설정 테스트음이 정상인지 확인해야 한다. |
| 글꼴·셰이더·메모리 | Web 빌드 성공 외에도 실제 브라우저에서 한글, 투명/클립 셰이더, 큰 맵과 AI 3명 실행을 확인해야 한다. Windows 빌드 크기로 웹 다운로드 크기를 추정하지 않는다. |
| 저장 | PlayerPrefs는 Web에서 IndexedDB를 사용하고 Windows 저장과 공유하지 않는다. 사이트 데이터 삭제·저장 차단 상황도 확인한다. 현재 이름·음량·난이도 저장량은 작다. |
| 조작·전체화면 | 현재 키보드/마우스용이다. 모바일 터치 조작은 없다. ESC는 브라우저 전체화면 해제에도 쓰일 수 있어 화면의 닫기 버튼도 함께 확인한다. |
| 배포 파일 | 현재 Brotli/fallback 끔 설정은 올바른 압축 응답 헤더가 필요하다. Unity Play 업로드 후 로더와 WASM/data 응답을 실제 확인해야 한다. |

공식 근거: [Unity 6.3 IME](https://docs.unity3d.com/6000.3/Documentation/Manual/IMEInput.html), [Web 성능과 배경 실행](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-performance.html), [Web 오디오](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-audio.html), [PlayerPrefs](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html), [Web 입력](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-input.html).

## 권장 진행 순서

1. Web 빌드와 로컬 브라우저에서 혼자 연습·튜토리얼·AI·결과 화면 확인.
2. 한글 입력을 위한 HTML 입력 연결 구현.
3. 비공개로 공유할 준비가 된 빌드를 Unity Play에 올린 뒤 실제 Relay 접속, 소리, 저장, 전체화면, 방장 탭 전환 검사.
4. 단독 Player 성능·메모리 측정 후 텍스처 크기·다운로드 용량 최적화 여부 결정.

## 실제 빌드·브라우저 확인 결과

- `Builds/WebAudit-20261029/` 일반 Web 빌드 성공. 오류 0, 기존 경고 5. 최초 빌드 1,443.313초(약 24분), 보고서 배포 크기 55,094,546 bytes(약 55.1MB).
- 주요 압축 파일: data 42,502,850 bytes, wasm 12,459,852 bytes, framework JS 82,893 bytes. 이는 압축 다운로드 크기이며 실행 메모리 수치가 아니다.
- 로컬 HTTP 서버에서 Brotli와 WASM MIME 헤더를 설정하고, 별도 비표시 브라우저 탭으로 실행했다. 로비의 한글 표시, 웹의 종료 버튼 숨김, 혼자 연습 진입, AI 3명 추가·준비·경기 시작·실제 AI 득점을 화면으로 확인했다.
- 콘솔 런타임 오류 0. Unity의 persistentDataPath 수동 동기화 방식이 향후 제거될 예정이라는 경고 1건이 있었다. 저장·재로드의 실제 지속성은 아직 별도 검사하지 않았다.
- **새로 확인한 배포 전 수정 항목:** 기본 템플릿 960×600에서는 대기실의 AI 제거 버튼과 우측 하단 탄약 HUD가 겹치고, 왼쪽 상단 제목/AI 인원 문구 일부가 잘린다. Windows의 기존 1366×768 검사와 별도로, 웹의 작은 캔버스에 맞춘 반응형 배치 또는 충분한 내부 렌더 해상도·16:9 표시를 적용해야 한다. 폰트 글리프 자체가 누락되는 현상으로 단정하지 않는다.
- **텍스처 메모리 최적화 후보:** Web BuildReport의 `WalkStrideTrial`/`RunRecoveryTrial` 및 P2~P4 팔레트 시트 항목은 각각 약 41,943,172 bytes로 잡힌다. 기본 시트 2개와 팔레트 6개만 약 320MiB의 보고서상 원본 자산 크기다. 대표 `WalkStrideTrial.png`는 기본 import가 Uncompressed/max4096이고 Web override가 없다. 다운로드 55MB로 실행 메모리가 충분하다고 판단해서는 안 된다. 이 숫자들을 실제 동시 GPU/힙 점유 측정치로 합산·단정하지는 않는다. Web 전용 압축/최대 크기/팔레트 공유를 검토하되, 픽셀 품질·스프라이트 경계·팔레트 렌더를 비교 검증해야 한다. 이번에는 원본 그림과 수입 설정을 바꾸지 않았다.
- 실제 브라우저에서 결과 화면·재입장·튜토리얼 전 구간, 한글 조합 입력, 사격 청감, Relay, 저장 지속성·성능은 이번 화면 검사 범위에 포함하지 않았다.
- 확인 후 임시 탭과 로컬 서버를 닫았고, Editor 빌드 대상을 `StandaloneWindows64`로 복원했다. 압축/메모리/캐시 설정과 프로젝트·조직 ID는 변경하지 않았다. 플랫폼 전환이 자동 추가한 WebGL batching 항목도 원래대로 되돌렸다.

화면: `LocalTools/Analysis/FullAudit-2026-10-29/web-ready-960.jpg`, `web-match-960.jpg`. 빌드 및 브라우저 로그: `web-build.json`, `web-console.json`. 웹 빌드 폴더는 확인용이며 Unity Play에 공개하지 않았다.

## 2026-10-30 추가 확인

하단 AI 버튼/탄약 HUD 겹침 및 제목·AI 인원 잘림을 수정하고 새 Web 빌드의 기본 캔버스에서 확인했다. 닉네임·음량의 새로고침 후 저장 유지, 솔로 나가기/재입장과 AI 초기화도 통과했다. 좁은 화면의 4인 명단이 월드 게시판 일부를 가리는 문제는 남아 있다. 한글 표시와 영문 입력을 확인했으며 실제 한글 IME 조합 입력을 검증하거나 해결한 것은 아니다.

Windows 두 프로세스의 비공개 인터넷 Relay 접속/사격/부활/결과/복귀 45개 확인은 통과했지만, Web끼리 및 Windows 혼합 접속을 대신하지 않는다. 별도 Windows 그래픽 Player의 AI 0/1/3명 30초 측정은 평균 약 60FPS였으며 Web 성능 측정은 아니다. 오늘 비압축 Web 시험 출력은 153,329,067 bytes이고 원래 Brotli 설정으로 복원했다. 상세 조건·남은 항목은 `FOLLOWUP-AUDIT-2026-10-30.ko.md`에 기록했다.

## 2026-10-30 후속 완료

앞 절의 미완료 항목 중 작은 화면 명단은 접기/펼치기로 보완했다. 실제 HTML 입력 폼을 닉네임/방 이름에 연결했고, 일반 Web 빌드에서 한글 길이 정리·저장 안내·새로고침 유지·취소를 확인했다. 합성 composition 이벤트 검사는 통과했지만 실제 OS 한영키 조합 입력은 별도 수동 확인이 필요하다.

Windows 방장↔Web 참가자, Web 방장↔Windows 참가자 양방향의 일반 방 목록 접속·준비·경기·원격 사격 피해·결과·복귀를 확인했다. 플랫폼별 Relay 프로토콜 선택과 Web 온라인 UseWebSockets 설정을 보완했고, 최종 재빌드 뒤 양방향 재접속 브라우저 오류 0을 확인했다. 같은 PC에서 인터넷 Relay를 경유한 검사이며 다른 PC/두 웹 클라이언트/배경 탭 호스트를 검증한 것은 아니다. 시험 방은 삭제했다.

최종 비압축 Web 출력은 153,351,407 bytes, 빌드 오류 0/기존 경고 5다. 프로젝트는 Windows64/Brotli로 복원했다. 실제 Unity Play 업로드·압축 헤더, OS IME·전체화면, 호스트 배경 탭 및 Web 성능/메모리 최적화는 배포 전 확인 범위로 남는다. 상세 자료는 `FOLLOWUP-AUDIT-2026-10-30.ko.md`, 사용자 확인 목록은 `WORK-STATUS-2026-10-30.ko.md`.
