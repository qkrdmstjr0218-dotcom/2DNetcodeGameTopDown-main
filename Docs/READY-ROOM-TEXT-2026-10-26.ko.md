# 대기방 표찰 제거 / Q·E 명칭 통일 / 인원 질문 — 2026-10-26

사용자 요청으로 상단의 '출격 대기 · 훈련 구역' 표찰 글자/면/테두리를 제거했다. 벽 가이드와 우측 하단 HUD의 Q/E 명칭은 ArenaUiTheme.SpecialAmmoSwitchLabel(특수탄 교체)를 공유한다. 기존 카드/키 배치와 선택 유지·특수탄 종류 안내는 그대로다.

최대 인원 질문은 읽기 확인만 했고 변경하지 않았다. 현재 방장 포함 총4명이다. 온라인 생성 SessionOptions.MaxPlayers=4,서버ConnectedClients.Count>=4시참가거절,준비명단·시작판정최대4,스폰Clamp1..4와로비2~4명표기를확인했다. 일반경기최소2/혼자연습최소1이다. 실제인터넷다인접속검사를한것은아니다.

확인: 제품API/구조검사8항목PASS(표찰3노드없음/공통문구/실제벽문구/전체9Collider동일/두해상도캡처). 1920·1366 Game View에서표찰없음·양쪽문구같음·글자영역을육안확인했다. 자료는LocalTools/Analysis/ReadyRoomText-2026-10-26의before/review.csx/result.txt/geometry.json/room-1920.png·room-1366.png다. 직접OS조작/실다인/장시간검사·커밋푸시는하지않았다.

최신 일반Windows64Mono빌드성공: Builds/BulletShot/2DNetcodeGame.exe,오류0/앞선빌드와같은경고6/28.39초/709645850bytes. 표찰제거/명칭통일·기존아이콘/60FPS를포함한다. 최종정지clean/컴파일실패false확인. 기존ServicesCore연결확인경고에대한실제인터넷접속검증은여전히남아있다.
