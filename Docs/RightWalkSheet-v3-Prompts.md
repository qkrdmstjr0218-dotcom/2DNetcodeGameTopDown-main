# 보행 시트 3차 보정

내장 image_gen 도구 사용. v2와 사용자 관절선 참조. 기존 v2는 보존.

## 전체 다리 보정

Use case: precise-object-edit. Asset: corrected transparent 8-frame right-facing game walk sprite sheet.
Image 1 is the EDIT TARGET armored silver ponytail rifle woman. Image 2 is the EXACT POSE REFERENCE in eight chronological cells. Redraw the legs in image 1 to follow image 2. Keep character design, armor colors, weapon and aiming upper body, camera angle, scale and 4-column 2-row grid unchanged. Real transparent alpha background, no text, guides, borders, colored outlines or shadow floor. Remove stray colored fringe.
Critical: Existing sheet duplicates the SAME leg placement in both half-cycles. Do not repeat that error. Purple line in reference means the SAME CAMERA-NEAR leg throughout ALL eight frames. Yellow means the SAME FAR leg. Keep the near leg visually identifiable by its larger exposed white/blue thigh armor plate and bright shin, far leg partially occluded and darker. Frame 3 and 7 must visibly SWAP which leg extends forward: in frame 3 the near thigh angles LEFT/BACK and its boot is LEFT of hip while far leg angles RIGHT/FORWARD. In frame 7 the NEAR thigh angles RIGHT/FORWARD and near boot is RIGHT of hip while FAR leg extends LEFT/BACK. Never mirror torso or change facing.
Match each corresponding reference:
1 near leg straight supporting slightly behind hip, far knee bent forward with foot lifted below hip.
2 near planted behind, far thigh extends forward knee high, far shin vertical, far toe right.
3 near fully back straight, far fully forward straight.
4 near knee bent FORWARD under hip, near calf folds BACK to left with heel raised; far leg supports under hip.
5 near knee and foot move forward in passing, far leg now supports behind; reverse leg identities relative frame1.
6 near thigh forward knee raised, near shin drops vertical and toe right; far extends back planted.
7 near fully forward straight, far fully back straight.
8 near leg planted under hip, far knee forward with calf folding BACK to left and lifted heel.
Both knees are between hips and ankles, not at the calf bulge. Each leg has exactly one thigh, one shin and one foot. Flex knees during swing. Consistent limb lengths and anatomy, grounded supporting feet, lifted swinging feet. Keep head, hip and rifle at stable local coordinates per cell, only subtle 2-3 pixel bob. Same ground baseline in each row. Room around all boots and weapon. Output a clean polished full-body sprite sheet of eight distinct chronological walk poses, left-to-right then second row; not eight independent character illustrations.

## 후속 발 보정 (선택본)

Use case: precise-object-edit. EDIT TARGET image1 transparent 4x2 eight-frame rifle-woman walk sheet. Image2 anatomical gait reference, purple is always the camera-near leg, yellow always the far leg. Correct legs ONLY, preserve all eight upper bodies, armor design, silver hair, rifles, proportions, grid and transparency.
The central defect is frames4 and8 being the SAME pose. They MUST be opposites.
Frame4 TOP RIGHT: The CAMERA-NEAR thigh with large visible white-blue armor plate points forward RIGHT, knee in front of hip. This near knee bends about90degrees, calf slopes backward LEFT from knee, near foot floats at rear LEFT, toe down. The FAR leg bears weight vertically beneath hip with its foot flat. Draw bright near shin diagonally from forward knee BACK to the floating rear foot, in front of the far straight support shin.
Frame8 BOTTOM RIGHT: The CAMERA-NEAR leg is now the straight weight-bearing leg under hip, large near thigh plate above straight bright shin and flat boot. The FAR leg alone bends knee forward and calf backward LEFT, floating far boot at rear partially occluded behind the straight near shin. Keep near support thigh visibly in front of far swinging thigh.
Frame5 BOTTOM LEFT: NEAR swinging leg passing forward across the FAR supporting leg, near knee moving forward right and foot lifted; do NOT duplicate frame4 or8.
Frame1 TOP LEFT: FAR swinging leg passing forward across NEAR straight supporting leg, so opposite occlusion from5.
Frames2&6 and3&7 retain opposite leg identities. Near leg behind in2/3, ahead in6/7. Keep near plate attached to near thigh. Exactly two legs per figure, no merged boots, coherent hips knees ankles, same limb lengths. A full loop is near stance1-3 near swing4-6 near front contact7 near stance8, far opposite. Foot soles supporting on same ground baseline. Do not mirror character, do not add colored annotations. Remove colored fringes on alpha edges. Real transparent background. All body parts inside equal cells.

