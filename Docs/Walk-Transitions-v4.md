# 중복 정지와 중간 허벅지 보정

내장 image_gen으로 3·4·8번 개별 하체를 보정. Pillow로 고정 상체를 유지해 합성. 재생 코드나 속도를 변경하지 않음. 하체8종이 모두 다른지/순환 인접 중복이 없는지, 상체 동일 픽셀과16px투명 여백 확인.

## 3번 지지 허벅지

Edit ONLY legs/pelvis in this walk passing frame. NEAR leg is the straight supporting leg beneath hip, ankle at(215,435), knee(240,350). FAR leg is lifted in front right with knee(302,309), ankle(300,394). The rounded white thighplate MUST belong to NEAR support thigh, not the raised far thigh. Remove the current horizontal plate(center265,267) from raised far thigh; leave far thigh darkfabric. Draw the single largeplate vertically centered(250,290), longaxis aligned DOWN-LEFT fromhip(270,255) to supportknee(240,350). Its blacksurrounding thigh overlaps far thigh at crossing. Exactlyone largeplate, noextra limbs. Keep current two knee/ankle/boot positions, far knee not higher, natural consistentlengths. Preserve ENTIRE upperbody hair face armsrifle and exact512squarecanvas withtransparentmargins unchanged. Samearmorstylepalette, cleanalpha nohalo/background. Correct onlyattachment and near/fardepth.

## 4번 착지 직전

Edit ONLY both legs in this right-facing walking frame to create a PRE-CONTACT transitional pose, not fullstridecontact. Upperbodyhairrifle unchanged exactpixelposition512square.
NEAR leg is BACK LEFT with rounded white outer thighplate already correctly slopingdownleft. Keep plate attached to near thigh sameorientation. Near support knee at(188,345), ankle(136,431), back boot on left: raise the heel about15pixels while its toes stilltouchbaseliney462. FAR leg is forward RIGHT abouttoland: bend its knee slightly more than now and move forward boot backLEFT20pixels and UP25pixels fromcurrentbootcenter(379,438), so far heel floats ~20px aboveground and toes lift slightly. Far shin angles down-right, knee around(320,345). Sameleglengths. This must be visibly different frommaximumstride nextframe: slightlynarrowerlegspread, farfootnotlandedyet, nearheelraised. Do not change camera, torso,arms,weapon,hipposition orscale. Exactly2legs one largewhiteplateonbackleftnearthigh, darkfarforwardthigh. Realtransparentbackground with margins, no glowshadowhalo.

## 8번 착지 직전

Edit ONLY legs of this right-facing armored woman's walk frame to PRE-CONTACT pose before nearfootlandinginframe1. NEAR leg withroundedwhiteplate remainsforwardRIGHT but its kneebendsmore, nearbootmovesLEFT20pixelsandUP25pixels fromcurrentfrontbootcenter(384,440), heel not grounded, toes angledslightlyup. FAR legbackLEFTsupport pushesoff: backheelrisesabout15pixelswhiletoestouchgroundbaseline465. Legspread slightlynarrower than maximumcontact. The roundedwhiteplate should slope DOWN-RIGHT fromhip to nearforwardknee, attachedforeverto sameleg, rotatewiththigh, nothorizontalor stationaryonhip. Noextra legsor plates. Keep same leg lengths consistentanatomy. Preserve ENTIRE upperbody headhair armsrifle hips and exact512squarelayoutpositions, truealphatransparency nohalo/shadow. Must be visiblydifferent from fullstridemaximumcontact: nearforwardfoot floating, farbackheelraised.

