# 머리 위 닉네임 줄바꿈 수정 — 2026-10-27

대기실 머리 위 이름의 GUIStyle이 GUI.skin.label의 자동 줄바꿈 설정을 상속하고 있었고, 표시 폭은 소수점 CalcSize 계산값 그대로였습니다. 글꼴 메트릭과 픽셀 반올림 차이로 마지막 글자가 다음 줄로 밀릴 수 있었습니다.

readyPlayerStyle에 wordWrap=false, clipping=Clip과 명시적 GUI.skin.font를 지정했습니다. 이름 스타일은 이를 상속합니다. P번호·이름 모두 Ceil(CalcSize.x)+4로 폭에 여유를 주며 기존 이름 폭 상한210px과 P/이름 8px 간격, 두 영역을 합친 중앙 정렬·화면 가장자리 clamp·그림자는 유지합니다. 닉네임 입력/저장/네트워크·다른 점수/결과 UI·게임 규칙은 변경하지 않았습니다.

solo에서 4글자 '유릴레이', 12글자 '가나다라마바사아자차카타', 폭이 넓은 영문 12글자를 1366×768/1920×1080에서 확인했습니다. 실제 서버에 반영된 이름으로 머리 위 GUI 스타일·전체 텍스트 폭·1줄 높이·8px 간격·중앙 정렬을 검사하고 캡처했습니다. 4한글은 텍스트47.33px/표시52px, 12한글은141.98px/146px, 영문은128.63px/133px, 높이는 모두18.83px/영역24px였습니다. 44개 개별 확인 PASS. 테스트 중 네트워크 이름은 복원했고 저장된 PlayerPrefs 닉네임은 그대로 유지했습니다. 실제 원격 멀티나 Player 실창 조작을 검증한 것은 아닙니다.

자료: LocalTools/Analysis/NicknameWrap-2026-10-27 (before.cs.txt, review.csx, result.txt, measurements.json, name-1366/1920-0..2.png). 직접 OS 조작·커밋·푸시 없음.

최신 일반 Windows64 Mono 빌드 성공: 오류0/기존경고5, 28.60초, 710003914bytes. Builds/BulletShot/2DNetcodeGame.exe 갱신.
