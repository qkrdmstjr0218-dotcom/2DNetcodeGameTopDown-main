# 현재 완성 캐릭터 · 2026-10-19

## 먼저 열기

- 대표 씬: `Assets/Characters/Current/Scenes/Character_Complete_Playground.unity`
- 재사용 프리팹: `Assets/Characters/Current/Prefabs/Character_Complete.prefab`
- Unity 검색 라벨: `l:CharacterComplete`

보존 시점에 사용자가 확인한 로컬 모션 체크포인트다. 걷기/달리기, 점프/더블 점프, 측면·정면 하단 사격, 쓰러짐/복구, 앉기/앉기 이동을 포함한다. 앉기 허벅지 교대와 양쪽 다리 후방8px 조정까지 반영했다. 이후 앉기 이동은 오리발처럼 보이는 등 자세 교체가 필요하다는 피드백이 나왔으며, 나중에 고치기로 보류했다. 이 보존본을 최종 자세 디자인으로 간주하지 않는다. 네트워크 전투 시험은 별도 씬/안내를 따른다.

## 사용

대표 씬을 열고 Play한다. A/D 이동, Shift 달리기, Space 점프/더블 점프, 마우스 조준, 좌클릭 사격, S/↓/왼쪽 Ctrl 유지로 앉는다. 화면의 HP0/복구 버튼은 로컬 모션 시험용이다.

다른 씬에는 프리팹을 배치한다. 캐릭터 크기와 원화 배율을 유지했다. `SideScrollMapPreview.followCamera`에 따라갈 카메라를 연결한다. `RightFacingAimRig.aimCamera`는 사용할 카메라를 연결하거나 MainCamera를 사용한다. 지형의 충돌 레이어/접지 조건은 대표 씬을 참고한다.

## 파일 구분

- 이 폴더: 현재 사용할 프리팹·대표 씬·안내와 자산 목록.
- `Assets/RightWalkPrototype/Art`, `Resources`, `Scripts`: 현재 프리팹이 참조하는 공유 원화·모션 데이터·코드. 기존 에셋을 재사용하며 중복 생성하지 않았다. 이 폴더도 캐릭터의 일부다.
- `Assets/RightWalkPrototype/Scenes/Network`: 최신 모션과 서버 HP/피해·쓰러짐·수동 복귀를 연결한 별도 네트워크 시험 씬. 사용과 검증은 `Assets/RightWalkPrototype/Docs/NETWORK-CHARACTER-CURRENT.ko.md` 참고.
- `Assets/RightWalkPrototype/Scenes/Archive`: 기존 이동·사격·앉기 시험 씬. 원본 내용과 GUID를 보존했다.
- `Assets/Scenes/Archive/CharacterStudies`: 초기 캐릭터/관절 연구 씬.
- `Docs/Scene-Catalog.json`: 옛 경로와 현재 경로 대응표.
- `Docs/Character-Contents.json`:10/19 로컬 보존 패키지 생성 당시 에셋·해시 목록. 후속 네트워크 작업 이후 현재 소스의 해시 목록과는 구별한다.

옛 문서와 과거 검사 코드에 나오는 씬 경로는 대응표를 참고한다. 기존 생성 도구의 활성 코드 경로는 새 위치로 맞췄다. 기본 게임 씬 SFPlayground와 Build Settings는 유지했다.

## 보존본

복원용 폴더: `Exports/Character_Complete_2026-10-19/`

- `Character_Complete.unitypackage`: 현재 프리팹·대표 씬과 필요한 원화/소스, Resources로 동적 로드하는 데이터 포함.
- `Source/`: 같은 시점 에셋과 .meta의 원래 상대 경로 사본, 브라우저 조립 원본, 프로젝트 패키지/버전 설정.
- `MANIFEST-SHA256.json`: 보존 파일 해시.
- `START-HERE.ko.md`: 복원·이동 안내.

폴더를 묶은 ZIP도 Exports에 둔다. 이 패키지는 Unity6000.3.18f1과 현재 프로젝트의 패키지 구성을 기준으로 보존한 캐릭터 체크포인트다. `.git`/Library를 포함한 전체 프로젝트 백업은 아니다. 프로젝트 안의 공유 코드는 후속 작업으로 바뀔 수 있지만, 이 별도 보존본은 이번 상태를 담는다.
