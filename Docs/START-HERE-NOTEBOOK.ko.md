# 학원 노트북에서 이어하기

1. 집에서 커밋 및 Push origin이 완료된 뒤, 노트북에서 같은 저장소를 Pull 또는 Clone한다. 로컬 수정이 있으면 먼저 보존한다.
2. Unity Hub에서 저장소 루트를 Unity6000.3.18f1로 연다. Assets 폴더만 복사하지 않는다. 최초 패키지/Library 복원은 시간이 걸릴 수 있다.
3. Assets/RightWalkPrototype/Docs/HANDOFF-2026-10-11.ko.md를 읽는다.
4. Assets/RightWalkPrototype/SideScrollTestMap.unity를 열고 Play한다.
5. 다음 에이전트에게 아래 요청을 전달한다.

> 이 저장소의 Docs/START-HERE-NOTEBOOK.ko.md와 Assets/RightWalkPrototype/Docs/HANDOFF-2026-10-11.ko.md를 읽고 이어서 작업해줘. 현재 횡스크롤 시험 맵에서 이동이 '뾸뾸거리는' 느낌의 원인을 점검하자. 방향성은 아직 확정하지 않았고, 승인된 캐릭터 아트와 좌우 반전은 보존해줘. 먼저 이동·보행 주기·카메라·물리 갱신을 분리해서 확인하고, 검사하지 않은 부분을 해결됐다고 말하지 말아줘.

Exports의 패키지나 ZIP은 로컬 백업이며 GitHub에는 제외된다. 프로젝트 원본으로 재개하므로 해당 ZIP 없이 씬을 열 수 있다. Unity MCP 소스는 LocalTools/UnityMcpPackage에 포함된다. 노트북의 Codex/Unity 연결은 별도 설정이 필요할 수 있다.
