# Knee height and missing instep v12

Current asset: `Assets/RightWalkPrototype/Art/RightWalkSheet-Redrawn-v12.png`.

Extended the white knee shell upwards on both legs while retaining the original hinge. Added a textured white instep cover across the marked ankle-to-foot gap. This cover renders after the existing ankle patch, preventing that older patch from hiding the repair. Existing white armor texture is reused, with rounded alpha boundaries. All poses use the same source patches and size.

User-authorized Pillow compositing, no new image generation. Script: `LocalTools/Analysis/bake_walk_v12.py`. Checked joint coordinates identical to v11 and captured twenty Unity poses. Enlarged comparison: `LocalTools/Captures/knee-instep-v12.jpg` (top before, bottom after). Preview: `LocalTools/Captures/baked-right-walk-v12.gif`. Updated portable export: `Exports/RightWalkSheet.unitypackage`.
