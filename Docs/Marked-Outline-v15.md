# Marked outline repair v15

Current sheet: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v15.png`. The user's red lines in two screenshots mark small missing portions of the existing knee and instep silhouettes.

Extended only adjacent transparent pixels in restricted knee and boot regions by three pixel iterations, propagating original neighbouring RGBA values. No new armor component or white overlay was introduced. Both legs reuse their corrected source artwork at fixed size.

Verified joint coordinates match v14 exactly, upper pixels stay fixed and cell borders remain transparent. Unity reported no compilation errors; captured all twenty poses. User-authorized Pillow compositing; no new image generation. Script: `LocalTools/Analysis/bake_walk_v15.py`. Before/after: `LocalTools/Captures/outline-repair-v15.jpg` (top before, bottom after). Preview: `LocalTools/Captures/baked-right-walk-v15.gif`. Export: `Exports/RightWalkSheet.unitypackage`.
