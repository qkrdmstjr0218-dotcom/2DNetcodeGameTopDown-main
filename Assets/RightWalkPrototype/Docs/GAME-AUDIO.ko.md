# 게임 효과음 1차 적용 (2026-10-25)

사용자 요청에 따라 사격·명중·폭발·적 사격/파괴·피격·보호막 피격·사망·이동·획득·특수탄 선택·UI 클릭·빈 탄창·경기 신호에 효과음24종을 연결했다. BGM은 아직 추가하지 않았다. 외부 샘플이나 생성형AI 대신 `LocalTools/Audio/build_effects.py`의 결정적 합성으로 제작한 짧은 PCM 음원이다.

`ArenaAudio`는 시작할 때 Resources/ArenaAudio를 읽고 월드12/UI4개의 AudioSource를 재사용한다. 멀리 있는 소리는22유닛에서 제외하고 카메라 기준 거리 감쇠/좌우 패닝을 적용한다. 발걸음과 명중음은 작게, 중복 이벤트는 짧은 재생 간격으로 제한한다. 발걸음은 접지/가로 이동 중에만 재생하며 피격 반동은 제외한다. 산탄은 한 번의 발사에 발사음 하나를 낸다. 게임 난수에 영향을 주지 않는 순환 피치 변화를 사용한다.

기존 사격 표시/서버 결과 이벤트를 재사용하며 새 RPC나 네트워크 직렬화 필드는 없다. 탄약/회복/보호막 획득음은 기존 NetworkList의 실제 소비 변경을 관찰한다. 만량으로 남은 아이템, 만료된 드롭, 몸에 닿지 않고 구덩이로 사라진 드롭에는 획득음을 내지 않는다. 같은 프레임에 발사와 보충이 함께 일어나도 수량 차이만 보고 놓치지 않는다. 튜토리얼 의료패드 회복은 로컬 HP 증가로 처리한다.

ESC 설정에 기존 전체 음량과 별도로 효과음 음량을 추가했다. 기본65%, PlayerPrefs의 RightWalk.SfxVolume에 저장한다. 소리 테스트는 실제 경기 시작음을 재생한다. 포커스가 없는 동안 새 효과음을 시작하지 않는다. 문장에 마우스를 올리는 동작에는 소리를 추가하지 않았다.

ArenaAudioReview의 단일 Editor solo 검사 PASS:24개 PCM 디코딩/범위,16개 고정 음원, 실제 일반/S/R/G 사격, 이동·점프·착지, 아이템 소비/만량/만료, 피격·보호막·사망, 카운트다운/시작/종료, 중복/거리/최대 혼합 제한, 음량0 및 Unity AudioSource의 비영 DSP 출력. 테스트는 적 공격을 정지하고 지정 위치/모터/API 발사를 사용하는 fixture이며 실제 멀티나 스피커 음질 검사라고 해석하지 않는다. 초기 탄약 검사는 이미 보충된 만량 상태였으므로 위치를 옮겨 실제1발 소비 후 획득하도록 수정했다. Unity6의 preloadAudioData는 AudioImporterSampleSettings에 설정했다.

검사 자료: LocalTools/Analysis/Audio/result.txt, qe-and-audio.png, audio-settings.png. 효과음 개별 수치/재현은 LocalTools/Audio/validation.json 및 README.ko.md 참조. 실제 멀티/배포 빌드/출력기기 청취는 미검증이다. 무기 수치와 게임 규칙은 바꾸지 않았다.

최종 ArenaCombatHudReview도 실제 InputSystem 발사/QE/Tab/ESC/결과/복귀/시계 회귀 PASS. LocalTools/Analysis/CombatHud/result.txt 참조. 효과음 이벤트 전후의 게임 입력 차단 규칙을 유지했다.
