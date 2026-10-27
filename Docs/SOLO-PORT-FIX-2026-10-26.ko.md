# 혼자 연습 연결 및 점수 안내

7783/UDP를 다른 Unity 프로세스(PID18408)가 점유. 혼자 연습/튜토리얼이 동일 고정 포트로 호스트 시작하여 충돌. solo일 때 loopback port0(OS 자동 할당) 사용; 명시적 로컬 host/client7783과 온라인 Relay 불변. 다른 프로세스 종료 없음.

기존 포트 점유 상태에서 실제 StartSoloPractice: IsListening/IsHost/SoloPractice true, 플레이어1, notice빈값 확인. Disconnect후 재입장 동일조건 PASS. 실제 대기실 화면 room.png 확인. 첫 Play 전환 도구 timeout 후 완료 상태 확인하여 재실행. Play 최종 종료.

점수 규칙 한 줄 통합 및 하단 여백 보정. 빌드 미갱신. 커밋/푸시 없음.
