# Right walk v5: passing poses and shared thigh plate

Current sheet: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v5.png`.
Ten 512×512 cells arranged in five columns and two rows, read left to right.

Two passing poses were inserted between former frames 6 and 7. Initially these poses shared one old frame's time, causing an abrupt fast swing. They now keep a full beat each. The two similar maximum-stride poses 4 and 5 share one beat to avoid dwelling at the front extreme. Current weights: `1,1,1,0.5,0.5,1,1,1,1,1`. The default cycle remains 1.375 seconds; full beats last 152.8ms and poses 4/5 last 76.4ms each. Timing selection was checked at all ten interval midpoints in Unity. GIF timing is rounded to centiseconds with a 1.37-second total cycle.

The upper 220 rows are identical across all frames. Every cell has transparent 16px borders. The near thigh plate uses one 59×57 source, `ThighPlate-Canonical-v1.png`, with rotation and translation only; it is baked into the sheet, not a runtime rig. Calf and boot silhouettes still come from individual drawn poses, so they are not yet rigid copies of a single source.

Unity demo and prefab: `RightWalkSheetDemo.unity`, `RightWalkSheetWalker.prefab`. Portable export: `Exports/RightWalkSheet.unitypackage`. Unity capture: `LocalTools/Captures/swing-plate-right-walk-v5.gif`.

## Generation and compositing

Built-in image generation tool edited two passing poses individually. A two-cell generation repeated the extremes and was rejected. The selected poses were aligned to the fixed upper body. User-authorized Pillow compositing reused the canonical thigh plate and assembled the transparent sheet. Reproduction scripts: `LocalTools/Analysis/smooth_swing_and_unify_plate.py` and `verify_v5.py`.

### Passing under pelvis prompt

Edit the NEAR swinging leg of this single walkframe. The rearleft lifted boot is MUCH too far behind. Bring that boot FORWARD RIGHT by80px on512canvas, so ankle goes from(135,365) to(215,395), toe goesfrom(160,400)to(245,420). Knee goesfrom(211,331)to(254,328), underhip(264,250). Thigh now nearlyvertical; calf folds modestlybackleft. This is the PASSING pose with liftedfoot UNDER THE PELVIS, not atrear and not atfront. Draw near boot in front of far support shin, slight occlusion. Roundednearwhiteplate verticalcenter(252,286) follows newnear thigh. Keep nearfootsole35-45pxaboveground, far supporting leg unchanged atx292 withgroundedboot. Consistentbone lengths, modestwalking kneeheight, no march. Exactlytwolegs, no duplicated leftover rearboot. Remove originalrearlegfootpixels and reconstruct transparent background there. Entireupperbodyriflehairhipcamera/size/512canvasunchanged. Cleantransparentalphanohalo/shadow.

### Passing forward prompt

Edit ONLY NEAR swinging leg, one next walk passing pose. In current image the lifted near boot is left of supporting leg. Swing it FORWARD RIGHT by50pixels on512equivalentcanvas and slightlyUP10px, now underpelvis/infrontof far support shin. Near knee forwardRIGHT30px stayingbelowhip, nearcalfnearvertical. Desired near hip(264,255), knee(281,326), ankle(260,392), toe(292,418). Far support leg knee(290,343), ankle(299,440), toe(340,464) unchanged. Lifted near foot should visibly OVERLAP and occlude farsupportshin aroundy400, not remainrearleft. Exactly2legs and2boots, no oldrearfootleftovers. Near roundedwhiteplate tiltsdownRIGHT attachednear thigh. Same bone lengths modestwalkingpose; nohighknee. Preserve entireupperbodyhairriflehipposition image scale andmargin. True transparency nobackgroundglow.
