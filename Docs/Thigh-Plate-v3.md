# 허벅지 파츠 보정

내장 image_gen으로 뒤로 뻗기와 뒤쪽 스윙의 하체를 보정. Pillow로 총 기준 위치를 맞추고 기존 고정 상체를 유지해 합성. 4·5번은 동일 보정 하체 사용. 중간 연결 자세는 아직 미완성.

## 뒤로 뻗기

Edit ONLY hip-and-leg anatomy of this transparent512x512 right-facing armored woman. The large rounded WHITE/BLUE THIGH PLATE is wrongly always attached to the forward leg. This frame must have the CAMERA-NEAR leg reaching BACK LEFT, FAR leg reaching FORWARD RIGHT. The plate belongs to the NEAR leg, so relocate it from its current center about(270,270) to about(225,285), rotate its long axis to slope DOWN-LEFT, and attach it to the thigh that runs from hip(265,250) toward back knee(190,355). Remove the old plate entirely from the forward-right thigh; that is now FAR thigh dark black fabric with a small inner armor detail, not another big white plate. NEAR thigh and near shin MUST occlude FAR leg at the hip crossing. Near knee is on LEFT/back; near ankle/boot LEFT/back. Far knee and boot RIGHT/front. Do not merely slide a white sticker without changing the thigh anatomy. Repaint pelvis, both thighs and correct depth occlusion; white plate follows the backward thigh. Keep both legs same lengths and foot positions, no extra limbs or plates.
Preserve all pixels of upper body head hair arms and rifle above waist, exact camera scale position silhouette and transparent margin. Clean true alpha no background, no shadows or halos. One corrected chronological walk frame, not a sheet. The output MUST visibly show white thigh plate to LEFT of pelvis along backward leg, not in front on right.

## 뒤쪽 스윙

Edit image1 near-leg swinging walk frame. Image2 corrected near-leg-back design is reference for white rounded thigh plate belonging to same near leg. In image1 the plate is wrongly on the straight front supporting thigh. Relocate it to the NEAR bent-back swinging thigh: runs from hip(265,250) down-left to knee(225,325), shin folds backleft to liftedankle(145,365). Rounded plate should center(238,282) and slope DOWN-LEFT/vertical rather than down-right. Redraw near thigh foreground correctly occluding the FAR straight support leg at hip crossing. Remove originalplate from forward support leg leaving dark inner thighfabric; exactly one largewhiteplate. Preserve near bentcalf lifted rearboot position and far straightsupportboot position; no duplicated or extra legs. Preserve complete upperbodyriflehairfacescaleandlocation, transparentmargin. Same artwork andlighting. True transparentbackground nohalo orshadow. One512x512squareframe.

