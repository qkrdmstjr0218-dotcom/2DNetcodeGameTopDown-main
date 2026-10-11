# Right-facing full-body walk sheet

Generated with the built-in image generation tool. Initial variant was rejected because the second half did not clearly exchange leg roles. Final selected source is the second generated image, copied to Assets/RightWalkPrototype/Art/RightWalkSheet-v2.png.

## Initial prompt

Use case: stylized-concept. Asset type: Unity 2D full-body walking sprite sheet with real transparent alpha.
Transform the character design in reference 1 into a NEW unified redrawn 8-frame RIGHT-FACING walking animation. Reference 1 is DESIGN/STYLE only, ignore its eight direction layout. Reference 2 is POSE/TIMING only: its eight numbered poses are one right-facing walk cycle, and purple/yellow show the two same legs throughout; ignore the drawn guide lines in final art.
One consistent silver-white ponytailed adult female sci-fi operator, black bodysuit, white/light-blue armored plates and restrained gold details, holding the same rifle aimed horizontally right. Same illustrated anime game art style for ENTIRE body, coherent anatomy and scale. All eight sprites face right, the same slightly elevated top-down side viewpoint. Redraw each as ONE complete character, not collage, no residual static legs on the torso.
Exact layout: 4 equal columns x 2 equal rows, 8 full-body sprites, chronological order left-to-right upper row then lower row. Prefer canvas 2048x1024 (each cell512x512) or proportional. Every sprite fully contained in its cell, centered at same horizontal pelvis coordinate with same standing foot baseline, same head size/camera/equipment/lighting. Transparent gutters. No words, no numbers, no panels, no guide lines, no shadows, no background.
Most important: true leg alternation, NOT pose wobbling! The SAME near leg (brighter, visible thigh plate) changes from behind to in front through the cycle; the SAME far leg (darker, partially occluded thigh) does the opposite. First half far leg steps forward; second half near leg steps forward. 
Frame1: near leg supports below/slightly behind pelvis; far knee lifted forward, far ankle trailing that knee.
Frame2: near support leg moves behind, far leg swings forward with knee bent and ankle underneath it.
Frame3: widest stride, near foot BEHIND left and far foot IN FRONT right.
Frame4: near thigh swings forward and knee points front right; near calf folds BACK toward left, heel lifted and toe points downward. Far leg supports beneath hip.
Frame5: exchanged passing pose, near knee forward and foot lowering; far support leg beneath/slightly behind pelvis.
Frame6: near foot advances right, far foot trailing left.
Frame7: widest stride with ROLES REVERSED relative to3: near foot IN FRONT right, far foot BEHIND left.
Frame8: far knee swings forward but far calf folds back left and foot lifts; near leg supports beneath hip. Flow back to1.
Keep knees distinct from bulging calves. Each thigh connects hip to knee, calf connects knee to ankle, foot separate ankle to toe. No extra legs, detached joints, repeated frame3 silhouette for7 with same legs, inconsistent armor, or independent resize of characters. Visible stance-side change and occlusion at half cycle are required.

## Targeted correction prompt

Edit target: reference1 is the current sprite sheet. Supporting reference2 is the exact 8-phase walking pose guide.
Keep same 4x2 layout, all-right facing, all eight same female character, head/torso/rifle/design/style/proportions, genuinely transparent alpha. Correct ONLY lower-body gait poses. Draw anatomically connected full-body sprites without additional static legs.
The current sheet erroneously keeps the brighter NEAR leg behind in nearly all frames. This is the defect to fix.
NEAR leg is the larger visible bright thigh plate on viewer-facing hip. FAR leg emerges from hidden inner hip, darker. Preserve this identity.
Frames1-3: NEAR leg planted/back left; FAR leg swings forwards/right.
Frame4: NEAR leg now RECOVERS: its thigh points DOWN AND FORWARD RIGHT from visible hip to knee on RIGHT of pelvis; calf points BACKWARD LEFT from knee to ankle LEFT of knee; near heel is raised and toes point DOWN. FAR leg is straight under pelvis planted.
Frame5: NEAR thigh continues forward, near knee to RIGHT of hip, near ankle under knee coming down; FAR leg behind/under hip. This is a PASSING pose with feet close, not wide stride.
Frame6: NEAR leg steps forwards/right, far leg trails left. The visible white thigh should point clearly forward/right, no longer backwards.
Frame7: NEAR leg strongly extended FORWARDS RIGHT from visible outer hip, near bright boot is RIGHTMOST/front. FAR dark leg extended backwards left, partially occluded thigh. This must be opposite anatomical-leg assignment to frame3. Visible near thigh runs from outer hip DOWN-RIGHT, far thigh down-left.
Frame8: NEAR leg planted directly under hip; FAR leg bent in recovery, far thigh/knee forward right, calf folded back left, heel raised.
Follow guide purple line as NEAR leg A, yellow as FAR leg B. Frame4 purple has knee forward right and ankle back left; frame8 yellow same configuration. No guide lines in output. Ensure cycle1..8 actual leg crossover, no stationary wobble or same wide stance in5,6,7. Maintain exact sprite cell spacing, baseline and scale.
