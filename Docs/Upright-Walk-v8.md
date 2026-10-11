# Upright walk posture v8

Asset: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v8.png` (20 frames, 5×4, 512px cells).

The v7 step spread and swing lift forced large knee bends. v8 raises the pelvis to y250 and moves the torso up 10px, narrows ankle travel from 150px to 80px, and reduces swing lift from 48px to 18px. Both legs now share the pelvis height. Original component sizes and fixed femur/calf lengths are preserved.

The torso mask excludes fixed thigh pixels and is composited after both legs. The original rifle aiming upper-body drawing is retained. This is visual gait correction rather than a physical center-of-mass simulation. Joint seams and silhouettes can still benefit from art refinement.

Implementation: user-authorized Pillow compositing of existing project artwork in `LocalTools/Analysis/bake_walk_v8.py`. No new image generation was used. Unity runs baked sprite frames, not runtime bones or IK. Default cycle remains 1.375 seconds.

Checks: constant thigh/calf lengths across all poses; knees remain below pelvis; twenty distinct poses; fixed upper pixels; transparent 16px cell borders; twenty Unity camera captures. Portable package: `Exports/RightWalkSheet.unitypackage`. Preview: `LocalTools/Captures/baked-right-walk-v8.gif`.
