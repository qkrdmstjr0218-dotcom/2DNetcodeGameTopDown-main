# 60FPS 제한 — 2026-10-26

사용자 요청으로 RelayArenaPrototype.Awake에서 QualitySettings.vSyncCount=0, Application.targetFrameRate=60을 적용한다. 현재 게임의 로비/대기실/연습/튜토리얼/경기에 공통으로 적용하며, PC에서는 VSync가 targetFrameRate보다 우선하므로 게임 실행 중에는 VSync를 끈다. 모니터가144Hz라도60FPS목표를사용한다. 프레임 목표는 소프트웨어 제한이며 모든 프레임의 정확한 시간이나 60FPS 성능을 보장하는 설정은 아니다.

물리 Fixed Timestep/네트워크 Tick Rate/게임 시계·이동·피해·음량 설정은 변경하지 않는다. 제한은 실제 게임과 Editor Play의 Game View에 적용된다. Scene View/에셋 임포트/컴파일 등 Editor 전체 부하를 제한하지 않으며 2D라는 이유로 과열이나 강제 종료가 방지된다고 단정하지 않는다. 검증 전용 ArenaBuildPolishReview의 일시30FPS는 기존처럼 검증 종료 후 이전 목표값(이제60)을 복원한다.

검사/빌드 자료: LocalTools/Analysis/FrameLimit-2026-10-26. 실제 적용값과 프레임 샘플을 짧게 측정하고, 최신 Windows 빌드에 포함한다. 장시간 발열/실제 인터넷 멀티 검사는 하지 않는다. 직접 OS 조작·커밋·푸시는 하지 않는다.

Editor Play 로비의 적용값 target60/vSync0을 확인했다. 워밍업30프레임 후 180프레임/3.00017초를 측정해 평균59.9966FPS,중앙 프레임간격16.2083ms/95백분위19.4001ms였다. 포커스가 있는 정지 로비의 짧은 측정이며 경기 중 성능이나 장시간 발열 검증은 아니다. FixedDeltaTime 약.02,NGO TickRate30을 기록했다. 측정 후 플레이 모드를 정지했다.

Windows64/Mono 최신 빌드 성공: 오류0/기존경고5/46.47초/709650618bytes, Builds/Review-20261026/Foundry.exe. 소리 테스트 버튼 위12px간격도 포함한다. 실제 Player 실행에서 FPS를 측정한 것은 아니며 이번 프레임 샘플은 Editor였다. 최종 정지/씬clean/컴파일실패false다. 정지 후 Quality VSync는 프로젝트값1로 돌아왔으나 다음 게임 시작 때 Awake가 다시0/60을 적용한다.
