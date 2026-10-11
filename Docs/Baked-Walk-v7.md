# Uniform walk cycle v7

Current asset: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v7.png`, 2560×2048, twenty 512×512 cells in five columns and four rows. Read left to right, top to bottom.

Previously redrawn poses did not form one gait: the supporting foot stayed forward, moved backward, then returned to the middle. Timing edits could not fix incompatible positions.

This version uses user-authorized pixel compositing. Six source parts were cut from the first v5 pose. Each part moves and rotates, never resizes. Both boots share one source/pivot to keep sole height consistent. Fixed thigh/calf lengths determine knees during offline baking. Unity displays only sprites; there is no runtime rig or IK.

Both foot paths are identical with a half-cycle offset. Support occupies 60% of the cycle: ankle x340 to x190 at y430. Swing occupies 40%, returning smoothly with a 48px lift. Twenty equal samples replace uneven dwell weights. Default cycle 1.375 seconds; frame duration 68.75ms. GIF rounds to 1.37 seconds.

Verified all twenty frame midpoint selections in Unity; invariant thigh/calf lengths; twenty distinct lower poses; fixed upper pixels; transparent 16px borders; all twenty Unity camera renders. Joint seams and overlap silhouettes remain an art limitation of the cutout source.

Source parts: `Assets/RightWalkPrototype/Art/WalkPart-*-v7.png`.
Reproduction: `LocalTools/Analysis/bake_walk_v7.py`.
Preview: `LocalTools/Captures/baked-right-walk-v7.gif`.
Portable export: `Exports/RightWalkSheet.unitypackage`.

## Rejected built-in image generation attempt

Built-in image_gen was tried for a coherent five-pose second half. The output repeated poses and did not follow requested trajectories. It was not used. Final v7 uses deterministic compositing of existing artwork.

Rebuild the LOWER BODY walking sequence in this five-cell horizontal sprite strip. Critical: read left to right, chronological second HALF of a walk, NOT five independent poses. Each cell fixed512square, total2560x512 transparent. Upperbody facehairarmsrifle EXACT same as reference in same positions. Near leg has roundedwhite thighplate throughout; far thigh dark. Repair malformed dark angular pelvis patches into clean fitted suit armor, same component sizes.
NEAR leg swings from back to front; FAR support foot sweeps continuously BACKWARD left, never reverses. Desired ankle positions in local512cell coordinates:
Cell1: near ankle(170,365) liftedback, far ankle(310,440) groundedfront; near knee(225,330), far knee(300,345).
Cell2: near ankle(220,388) liftedunderhip, far ankle(265,440) groundedunderhip; near knee(260,330), far knee(275,345).
Cell3: near ankle(275,412) liftedforward, far ankle(220,440) groundedback; near knee(300,334), far knee(250,345).
Cell4: near ankle(330,438) heelabouttolandfront, far ankle(175,440) behindheelraising; near knee(315,340), far knee(220,345).
Cell5: near ankle(315,440) supportfootalready begins sweepingBACK afterlanding, far ankle(160,400) now liftsbacktowardsnextpassing; near knee(310,346), far knee(220,330).
Nearthighplate ROTATES WITHnear thigh: backdownleft in1, verticalin2, downrightin3-5, anchored fromhip(270,255) towardnear knee. Exactly two legs and two boots per cell. SAME femur/calf/boot lengths and armorplate shapes. No high marchknees. Toes point right, bootextends~55pxright fromankle. Feet never both plant in front, no repeated maxstrideholds, no teleportbackfoot tocenter. Modest anatomically valid knee bends and alternating leg overlap. Preserve clean transparent generousmargins at least16px, no ghosts or leftover armor, no shadows, no new background. This is one sequential gait half with deliberately different foot positions every frame.

