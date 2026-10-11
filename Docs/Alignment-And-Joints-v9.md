# Torso alignment and joint seams v9

Current asset: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v9.png`, twenty 512px cells in a 5×4 sheet.

Both legs (hips, knees, ankles and rendered parts) move 12px left relative to the unchanged upper body. The torso/belt is composited after both leg layers. Original knee and ankle armor patches cover the segment cut edges, with a soft alpha boundary. Patches keep fixed size and follow the corresponding joint; both ankles reuse the same source.

The accepted v8 gait is retained: same hip height, step spread, lift, 20 samples and 1.375-second default cycle. Checked every joint against v8: exactly -12px x and unchanged y, proving this edit preserves the motion. Also checked fixed upper pixels, transparent borders, all twenty Unity camera renders and no Unity console errors.

This uses user-authorized Pillow compositing of existing art. No new image generation. Reproduction: `LocalTools/Analysis/bake_walk_v9.py`. Preview: `LocalTools/Captures/baked-right-walk-v9.gif`. Updated portable package: `Exports/RightWalkSheet.unitypackage`.
