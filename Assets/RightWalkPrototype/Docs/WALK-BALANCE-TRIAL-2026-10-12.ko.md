# 걷기 상체 균형 비교

SideScrollWalkBalanceTrial.unity는 SideScrollStrideTrial을 복사한 비교 씬이다. 속도1.6, 하체40프레임, 보폭/박자/골반 높이 보정은 유지하고 상체만 골반 기준3도 앞으로 기울였다. 원래 씬/프리팹/승인PNG를 덮어쓰지 않았다. 새 씬의 캐릭터만Prefab연결을 해제하고 별도 회전 중심을 만들었다. 달리기는 수정하지 않았다.

회전 중심은 원본좌표(252,250). Upper Body Alignment 아래의 몸통/목/머리/팔/총을 함께 회전시켜 목이나 머리만 숙이는 변경을 피했다. 사격 방향을 유지하도록AimRig의restAngle=3을 이 씬에만 적용했다. 기존씬은restAngle기본0으로 기존동작을 유지한다. 조준제한 및목/머리추종은restAngle기준의 상대각으로 계산한다.

검증:좌우2방향x40자세x3조준각(-45,0,+45),240조합에서팔 도달불가0, 최대접점오차1.192093e-7월드단위. 실제Play4초관측774회에서40자세와정지/재출발 확인,HoldCycle0회. 이번검사 이후새콘솔오류0/컴파일실패0. 하체나발목궤적은수정하지 않았다. 허리/골반 연결을대표자세에서 확인했지만모든순간의외형이자연스럽다는미적승인과는구분한다.

LocalTools/Analysis/walk-balance-before.png 및walk-balance-after.png는같은카메라/정지표시자세의비교다. walk-balance-runtime.csv는실제보행관측이다. 새씬은사용자평가전시험본이며골반/허리겹침과장비기울기를비교해야한다.

Unity메뉴Tools/Right Walk Prototype/Build Walk Balance Trial로재생성한다. Play/dirty씬에서는거부한다. 다음작업은평가후결정하며뒤쪽다리밀어내기와달리기는이번수정범위에포함하지않았다.
