# 마무리 기능: 시야·종료·닉네임·로비

## 적용
- 경기 추적 카메라 4.8→5.6. 캐릭터 Transform/충돌/무기 수치는 유지. 카운트다운부터 동일 배율·끝까지 유지, 확대된 반높이로 맵 가장자리 clamp도 수정. 대기방6.5/튜토리얼4.8 유지. 종료 시 마지막 카메라 위치 유지.
- 제한시간 도달 시 아직 Playing인 다음 서버 점수 틱 전에도 Ended/전투차단을 적용. 자동부활에도 같은 시각 경계 가드 추가. 격리된 expired Playing 상태에서 피해·발사·가로이동 차단과 부활기간 초과 후 사망 유지 2조건 PASS(deadline-result.txt).
- 기존 Stage 숫자는 보존하고 Ending을 마지막에 추가. 서버 종료 시 점수 확정/1.5초 종료 안내→Finished 결과창. Ended는 Ending/Finished 둘 다 포함하여 기존 이동·조준·사격·피해·부활 차단 재사용. 종료 안내 중 대기방 복귀 거절. 종료음은 Ending, 승패음은 Finished에 재생. 인원 부족 종료도 같은 안내 흐름.
- 로비 상단 닉네임 입력/저장. PlayerPrefs RightWalk.Nickname, 기본 플레이어. 최대12 Unicode text elements/UTF8 60bytes·제어/방향제어/각괄호 제거. 빈 이름 기본값. 소유자 전용 Rpc(InvokePermission.Owner)로 제출/서버 재정리. 경기 중 변경 거절. NetworkVariable FixedString64 이름, 준비명단/Score 스냅샷에 이름 포함. 대기 명단·Tab·결과행·승자 요약에 표시. 같은 이름은 P번호로 구별. 결과 이름은 오브젝트 검색에 의존하지 않음.
- 대기 명단은 기존250폭 안에서 P/준비 위줄·닉네임 아래줄, 길이에 맞춘 폰트. 결과 공동승자 최장 문자열도 폭에 맞춰 한 줄. 입력/이름 렌더 richText false.
- 로비/대기방/튜토리얼에서 다리는 닫힌 상태로 휴지, 종료 때는 현재 위치 유지. 경기/카운트다운은 기존 공통시계로 동작. Time.timeScale 변경 없음. 하나의 씬 구조 유지.
- 네트워크 필드가 달라 OnlineRoomKind foundry-score-20261027-v2로 구분. 이전 버전 온라인방을 목록에서 섞지 않음. 모든 참가자는 같은 최신 빌드 사용.

## 검증
LocalTools/Analysis/FinalFeatures-2026-10-27.
- 실제 solo 통합36조건 PASS: 이름/길이/서버반영/경기중거절, 카운트다운·Playing5.6, Ending1.5초/피해거절/점수고정/결과전복귀차단, 죽은채종료후부활기간초과해도부활없음, 복귀회복·재경기·종료·로비휴지.
- 기존 BuildPolish89조건 PASS: 자원28PCM/한글/셰이더/실제양쪽상층8단일점프/하강/보행 및 실제사격, 4종 결과음. 첫 결과음1회검사는 실패하여 보존(build-polish-first.txt). 재로딩 후 재생수 계측을 추가한 최종검사는 4종 모두 종료1회·결과1회 확인(audio-timing.txt); 별도 격리phase실험도 동일(audio-focused.txt). 첫실패 원인은 확정하지 않음.
- NGO FastBufferWriter/Reader로 Score/ReadyRoster 이름12한글 직렬화 왕복 PASS.
- 1366/1920 실제GameView. 4행/UI명단은 명시적 fixture이며 실제4인/인터넷 아님. 최초 ui-review에서 match.enabled=false가 OnGUI까지 숨겨 배경만 촬영했던 이미지는 검증 제외. 최종 명단은 해당 Behaviour의 IsServer 캐시만 임시 false로 설정해 서버 명단갱신을 멈춘 렌더fixture/원복. 결과는 Finished 상태 실제 OnGUI. 초기 result summary 줄잘림을 자동폭맞춤으로 수정. 초기 검사용 NetworkList.ToArray 컴파일 오류는 검사 csx만 수정.
- 테스트 닉네임 PlayerPrefs·GameView해상도 복원, 마지막 Play 정지. 실제2~4PC/온라인/장시간/출력기기청취는 미검증.

Windows 빌드 결과는 build.json 참조. 커밋/푸시 없음.

## 최종 빌드
일반 Windows64 Mono / Builds/BulletShot/2DNetcodeGame.exe / Succeeded / 오류0 / 경고5 / 709650810bytes / 106.8초. 기존 deprecated RPC2·Physics2D2 및 Player Pipeline 자동화설정1. Services Refresh 후 해당 연결경고는 이번 빌드에서 없음. 실제 Player실행/인터넷접속 성공의 증거는 아님.
