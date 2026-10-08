# 골반 접합 연구

내장 이미지 생성으로 operator-hip-bridge-v1.png 제작. 프롬프트 hip-bridge-prompt.txt.
골반 상단 60%만 Sprite rect로 사용하여 기존 움직이는 허벅지 장갑 중복을 줄임. 45% 절단은 허벅지 사이 틈이 생겨 폐기. 원본 텍스처는 보존.
골반 위치 (0,.425,0), sortingOrder 3: 앞 허벅지 뒤 / 먼 다리 앞. 상체는 기존 그림 유지.
연구 씬의 실시간 샘플은 STANDING으로 시작하며 토글을 끄면 기존 보행 재생. 보행 궤적/뼈 길이/발목 50도 설정 유지.
컴파일 오류 없음. 8위상 유효 Transform 확인. 캡처 Assets/Temp/hip-standing-final.png, hip-walking-final.png.
허리 수평 절단을 골반 언더슈트 겹침으로 완화했지만 상하체 원근/화풍, 발목 임시 연결재와 굵은 하반신 비율은 여전히 연구 단계. 실제 게임 플레이어에 미적용.
