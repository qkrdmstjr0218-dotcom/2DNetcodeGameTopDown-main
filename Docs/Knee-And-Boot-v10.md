# Knee and boot artwork v10

Current sheet: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v10.png`.

Removed proximal calf fragments extending above the knee joint. Both knees now reuse the complete near-knee armor source and its pivot, instead of the partially clipped far-knee patch. The boot extraction polygon includes the complete forefoot and sole, and the ankle cover radius increases from 12px to 14px. All parts remain fixed size.

Verified v9 and v10 hip/knee/ankle coordinate records are identical: gait, body alignment and timing are unchanged. Also verified fixed upper pixels and transparent borders, and captured all twenty Unity poses. Enlarged before/after: `LocalTools/Captures/knee-foot-before-after-v10.jpg` (top v9, bottom v10).

User-authorized Pillow compositing of existing project art, no new image generation. Script: `LocalTools/Analysis/bake_walk_v10.py`. Unity preview: `LocalTools/Captures/baked-right-walk-v10.gif`. Portable package: `Exports/RightWalkSheet.unitypackage`.
