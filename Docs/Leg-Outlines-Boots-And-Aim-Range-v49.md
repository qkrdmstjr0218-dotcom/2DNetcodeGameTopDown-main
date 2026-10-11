# Lower-body repair v49

- Rebuilt the 20-frame leg sheet using the existing v32 gait trajectory.
- Replaced calf artwork with a continuous dark panel outline, registered to the existing knee and ankle anchors.
- Replaced boots with enclosed black ankle collars, white tongues and white instep panels; removed the exposed pink heel bands.
- Extended the upper thigh overlap using existing dark trouser texture.
- Reproduction script: `LocalTools/Analysis/bake_legs_v49.py`.
- Unity captures: `LocalTools/Captures/lower-v49-{0,5,10,15}.png`.

## Aim range inspection

The current configured limit is 12 degrees. Temporary inspection at 30 and 45 degrees in both directions did not change that default. Arm targets remained reachable through all 20 gait samples at tested angles up to 60 degrees, but reachability alone does not establish a natural pose. Around 30 degrees the current parts remain reasonably usable; around 45 degrees the stock-to-shoulder relationship and chest/head overlap require additional compensation or pose artwork.

Scene, walker prefab and `Exports/RightWalkSheet.unitypackage` saved after restoring frame zero and neutral aim.
