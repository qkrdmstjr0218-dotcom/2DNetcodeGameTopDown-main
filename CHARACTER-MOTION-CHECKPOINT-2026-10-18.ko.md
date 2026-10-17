# 캐릭터 움직임 저장 지점 · 2026-10-18

현재 작업 시작점은 **Assets/RightWalkPrototype/SideScrollFrontPlayableTrial.unity**다. Unity 6000.3.18f1을 사용한다. 이전 씬은 모두 현재 위치에서 보존한다.

## 저장한 범위

- 좌우 걷기/달리기, 뒤로 달리기=기존 뒤로 걷기 빠른 재생.
- 마우스 조준/임시 사격, 정지·이동·공중 하단 사격.
- 첫 점프는 앞발에 따른 자세 선택, 공중 하단 하체의 이륙 방향 유지.
- 더블점프와 두 번째 점프의 반대 다리 자세, 착지 후 횟수 초기화.
- 현재 씬 설정: 걷기1.6, 달리기2.4, jumpSpeed5. 캐릭터/총 크기와 카메라 설정 유지.
- 쓰러진 최종 자세는 `LocalTools/Analysis/HomeVerification/DeathPoseTrial/death-pose-v2.png` 검토 시안까지. 중간 자세와 플레이어 HP/사망/리스폰은 미구현.
- 위 움직임은 현재 로컬 시험 캐릭터 기준이다. 모든 기능의 네트워크 적용 완료를 뜻하지 않는다.

## 씬 길잡이

| 구분 | 씬 | 용도 |
|---|---|---|
| 현재 작업/직접 조작 | Assets/RightWalkPrototype/SideScrollFrontPlayableTrial.unity | 현재 움직임·조준·사격·더블점프 확인 |
| 기존 횡스크롤 네트워크 시험 | Assets/RightWalkPrototype/SideScrollNetworkTrial.unity | 이후 멀티플레이 작업 시 기존 구현 확인 |
| 이전 하단 사격 시험 | Assets/RightWalkPrototype/SideScrollFrontDownAimTrial.unity | 학원 인계 시점의 비교용 |
| 정지 자세 비교 | Assets/RightWalkPrototype/SideScrollFrontStandingReview.unity | 이전 정지 자세 시안 비교용 |
| 이전 횡스크롤 실험 | Assets/RightWalkPrototype/SideScroll*Trial.unity, SideScrollTestMap.unity | 달리기·착지·보폭·균형 등 과정 보존 |
| 걷기/파츠 실험 | Assets/RightWalkPrototype/RightWalk*.unity, LeftRightWalkPreview.unity, ReferenceLegStudy.unity | 초기 걷기 및 파츠 비교 |
| 초기 탑다운/SF 8방향 계열 · 보존 | Assets/Scenes/SFPlayground.unity | 이전 방향성의 SF 조작/사격 시제품 |
| 최초 템플릿 계열 · 보존 | Assets/Scenes/SampleScene.unity, Assets/Scenes/SampleScene 1.unity | 초기 탑다운/박스헤드 지향 작업 보존 |
| 초기 캐릭터 파츠 연구 · 보존 | Assets/Scenes/Operator*.unity | 원화 재조립/조준/관절 실험 |

이번 저장에서는 씬/폴더 이름과 위치를 변경하지 않았다. Build Settings도 기존 SFPlayground 등록을 유지한다. 표의 '현재 작업'은 빌드 시작 씬 변경을 뜻하지 않는다.

## 복원/인계

로컬 Git 저장 태그: `checkpoint/character-motion-2026-10-18`. 기존 main의 이력 뒤에 저장하며 이전 커밋을 교체하지 않는다.

별도 백업: `Exports/CharacterMotion-2026-10-18.zip`. Assets/Packages/ProjectSettings/Docs/Tools, 루트 문서, 브라우저 원본과 검토 자료가 있는 LocalTools/Analysis, manifest가 참조하는 LocalTools/UnityMcpPackage를 포함한다. Git 전체 이력은 ZIP 내부 GitHistory.bundle에도 담는다. Library/Temp/Builds/Logs/UserSettings와 기존 Exports는 포함하지 않는다.

복원은 ZIP을 **새 폴더**에 풀고 Unity Hub에서 열어 위 현재 작업 씬을 선택한다. Library는 Unity가 다시 생성한다. 기존 폴더에 덮어쓰지 않아도 비교할 수 있다. ZIP의 `CHECKPOINT-MANIFEST.json`에는 저장 파일별 SHA256이 있다.

Git 이력 자체를 복원할 때는 ZIP 안 GitHistory.bundle로 새 폴더에 clone한 후 위 태그를 선택한다. Git 제외 상태의 검토 이미지 등은 ZIP의 해당 경로에서 함께 복원한다.

초기 씬은 공용 스크립트도 참조하므로 씬 파일만 남기는 것과 당시 전체 동작을 보존하는 것은 다르다. 이전 동작을 정확히 비교하려면 새 폴더에서 당시 Git 시점을 함께 사용한다. 보존된 이전 이력: `4316aaf` 최초 템플릿, `d432f00` 이전 캐릭터/횡스크롤 저장, `93d952e` 학원 인계.

## 정리 시점

우선 이 문서를 입구로 사용한다. 기본 전투와 멀티플레이 작업 씬이 정해진 다음, Unity 참조와 의존성을 확인하며 실험 자료를 정리한다. 초기 탑다운 계열은 향후 정리에서도 보존 대상으로 유지한다.

최신 세부 기록: `Assets/RightWalkPrototype/Docs/FRONT-PLAYABLE-TRIAL.ko.md`. 전투 방향성: `LocalTools/Analysis/combat-system-design-notes.ko.md`.
