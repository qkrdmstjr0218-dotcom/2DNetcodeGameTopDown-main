# 해킹 구역 홀로그램 후속 — 2026-10-25

기존 단색 사각 면/외곽선을 절삭 모서리 발광, 바닥 경계, 옅은 가장자리 광과 내부 광 흐름으로 바꿨다. 내부에 발판처럼 보일 수 있는 반복 점선이나 수평 스캔선을 추가하지 않았다. 무주인 청록/단독 득점 녹색/경합 주황의 기존 색을 재사용하며, 다음 구역 예고는 옅은 주황 윤곽만 표시한다. 실제 교체3초전 밝기 변화와 종료 우선 시 점멸 거절도 유지한다.

ArenaHackingField는 실행 중 기존 Zone fill 아래에 충돌체 없는 쿼드를 만든다. 6곳이 Mesh1개/Material1개를 공유하며 보이는 구역만 그린다. 전용 Resources/FoundryArt/HackingField.shader는 텍스처 없이 윤곽과 빛을 계산하고, 경기 공통 시계의 위상을 MaterialPropertyBlock으로 받는다. 셰이더 미지원 시 기존 표시로 돌아간다. Editor 생성기의 Show 호출에서는 새 오브젝트를 만들지 않아 임시 Mesh/Material이 씬에 직렬화되지 않는다.

점령 판정은 기존 Zone fill SpriteRenderer의 Bounds를 그대로 읽는다. 해당 SpriteRenderer는 그림만 숨기며 크기/위치/스프라이트는 바꾸지 않는다. 현재6곳 모두 기존4×2.35와 새 표시의 XY Bounds 일치를 확인했다. 새 충돌체·RPC·득점/전환 시계·소리 변경은 없다.

ArenaVisualFollowupReview solo PASS:6곳 셰이더 지원/단일 공유 Mesh·Material/구형 면 숨김/충돌체 없음, 점멸 전후 점령 Bounds·플레이어 포함 판정 불변, 정상 시간과 경기 종료 우선의 점멸 거절. 기존 전환/픽업/지원 아트 검사도 함께 통과했다. 실제 게임 1366×768에서 활성/예고/경합 색과 경고 밝음·어두움 화면을 확인했다. 경합은 표시 색 fixture이며 실제2인 경합 검증이 아니다.

자료는 LocalTools/Analysis/VisualFollowup/site-active.png, site-preview.png, site-contested.png, site-warning-bright.png, site-warning-dim.png, result.txt, field-bounds.txt에 있다. 캡처 구간은 기존과 같이 AI/경기 Update 정지·지정 위치 fixture다. 실제 멀티/배포 빌드/전체 GPU 성능 검사는 하지 않았다. 외부 생성형AI/직접OS조작/가상 플레이어 활성화/커밋/푸시는 없다.
