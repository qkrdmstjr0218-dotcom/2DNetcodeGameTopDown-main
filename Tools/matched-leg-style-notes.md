# 상하체 화풍 맞춤 1차

내장 이미지 생성 도구로 기존 6파츠 아틀라스를 상체 참고 이미지에 맞춰 재채색/재묘사.
이미지: Assets/Art/OperatorPrototype/operator-joint-parts-matched-v1.png
프롬프트: Tools/matched-leg-style-prompt.txt
설치: Tools/InstallMatchedLegStyle.cs

연구 씬의 9개 샘플에 적용. 기존 Sprite rect, pivot, pixelsPerUnit과 뼈 길이, 보행 궤적, 발목 50도 설정 유지. 실제 SFPlayer는 변경하지 않음.
8위상에서 유효한 Transform 확인. 알파 투명 픽셀 확인. 캡처: Assets/Temp/matched-full-body-v1.png.
파란 결정 표현을 차분한 청회색 금속으로 변경했으나 상하체 화풍/원근은 아직 완전히 일치하지 않음. 허리 수평 절단선과 큼직한 장갑 실루엣은 남아 있음.
골반을 .43으로 낮춘 실험은 파우치 중복 경계가 악화되어 채택하지 않음. .465 유지.
