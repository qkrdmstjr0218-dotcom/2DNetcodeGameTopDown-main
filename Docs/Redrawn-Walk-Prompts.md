# 새 원화 기반 걷기 시트

내장 image_gen 도구 사용. 첫 생성본 선택. 자세 가이드 후속 수정본은 다리 역할 교대를 충분히 반영하지 않아 미채택.

## 선택본 프롬프트

Create an eight-frame right-facing WALK CYCLE sprite sheet from the supplied approved single-character master. Same silver ponytail woman, same face compact proportions, armor design black suit white pale-blue panels gold circles, same rifle aimed right, same clean illustration. Preserve complete natural hair/neck/back, no square cutouts or halos.
Layout: EXACTLY4equalcolumns2equalrows,8fullbodycharacters, each cell same square size, centered hip at same local coordinate, same head height and foot groundline, generous margins. Chronological order left-to-right top row then bottom row. True transparent RGBA background, no scenery shadow glow outlines text borders labels. Simplified stable ponytail silhouette consistent allframes, same upperbody and rifle localposition, subtle body bob only.
Core motion requirement: two separate anatomically coherent legs alternate. Define NEAR leg as camera-facing thigh with large white outer thigh armor, FAR leg darker and partly occluded. Never swap identity and never mirror torso. Near leg foreground every frame.
1 CONTACT NEAR FOOT FORWARD RIGHT, heel grounded, far foot BACK LEFT toe grounded. Near white thighplate slopes down-right, far thigh down-left.
2 DOWN near foot comes under hip supporting slight knee flex; far knee flexes, back foot begins lifting.
3 PASS far knee moves forward right lifted, near support leg vertical underhip.
4 UP far foot extendsforward abouttoland, near leg pushesoffbehind left heelraised.
5 OPPOSITE CONTACT FAR FOOT FORWARD RIGHT grounded, NEAR FOOT BACK LEFT toe grounded. Near white thighplate now slopes down-LEFT, unlike1. This is NOT identical to1.
6 DOWN far foot underhip supports, near knee bends and back foot lifts.
7 PASS near knee moves forward right lifted, far support leg vertical underhip. Near white thighplate and knee liftedforward, unlike3.
8 UP near foot extendsforward abouttoland, far leg pushesoffbehind left heelraised. Returns seamlessly1.
Distinct knees ankles toes; same limb lengths throughout. Knees flex during swinging; lifted foot never drags ground. No duplicated halfcycles, no frozen front/back leg identity, no merging boots. The NEAR thigh plate must clearly reverse tilt between1 and5 and the lifted foreground knee must differ between3 and7. Keep character identity, rifle, top-down side-view camera, anatomy and clean alpha edges matched to master.

## 미채택 보정 프롬프트

Edit image1 eight-frame armored woman sprite sheet. Image2 is a precise chronological LEG POSE GUIDE. Redraw ONLY both legs and pelvis orientation according to corresponding8cells of image2. BLUE means the SAME camera-NEAR leg throughout all8frames, ORANGE same FAR leg. Do not paint blue/orange guide colors into output. Keep near leg white outer thigh armor prominent; far leg partly occluded. Blue leg must be in foreground when crossing.
Toprow1near extendsright,2near almostvertical,3near extendsbackleft whilefar knee liftsforward,4near fullybackleftfar fullyforwardright. Bottomrow5near fullybackleftfar fullyforwardright,6near bent calf foldsbackleftandfootlifted,7near knee liftedforwardrightandshinverticalfootlifted,8near extendsforwardrightfarfullybackleft. Specifically fix frame5 thighplate to slope downLEFT fromhip, frame3 thighplate downLEFT, frame7 thighplate nearlyhorizontal RIGHT withkneehigh. Never repeat1in5 or3in7: legs switch identity halfcycle. Legs remain same physical lengths as originalcharacter even where schematicguide lengths vary slightly.
Preserve freshly drawn upper body rifle face hair neck back, characterdesign, renderingstyle, allright-facing camera, stable scale4x2 equalcellgrid. No square notches, background contamination, text or guides. Real alpha transparency outside character silhouette, no floor/shadows/glow. Fullbody margin and samegroundbaseline.

알파 8 미만의 거의 투명한 픽셀만 정리. 사각 영역 삭제나 머리 형태 절삭 없음.

