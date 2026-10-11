# Original boot seam restoration v14

The user's white marks indicated missing artwork, not a new armor component. Removed the separate v13 instep guard and earlier white saddle overlays. Expanded the original boot cutout around the cuff and instep, restoring source pixels that the previous polygon excluded. The intact boot cuff overlaps the calf; the additional ankle overlay is no longer applied.

Verified every visible RGB pixel of the boot cutout comes directly from the original source. The joint records match v11 exactly: knees, alignment and gait remain unchanged. Captured all twenty Unity poses. Current sheet: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v14.png`. Script: `LocalTools/Analysis/bake_walk_v14.py`. Detail: `LocalTools/Captures/instep-detail-v14.jpg`. Preview: `LocalTools/Captures/baked-right-walk-v14.gif`. Updated package: `Exports/RightWalkSheet.unitypackage`, excluding the rejected instep guard.

User-authorized Pillow pixel compositing; no new image generation.
