# Torso Fill Repair

Built-in ImageGen edit mode was used. Original source assets were not overwritten.
Only the torso-side mask in sandbox.mjs consumes the selected result, using destination-over compositing. Generated head, hair, limbs, center armor and backdrop are not installed as replacements.

## First Pass

Input: body-underlay-reference.png

Use case: precise-object-edit. Asset type: layered 2D game character torso repair. Edit target: the attached original 1024x1536 character image. Create an armless torso underlay for this same fully clothed science-fiction armored character. Repair ONLY the small torso side areas that would be hidden under the character's arms: from the shoulder-side seams down to the waist, y=430 to 640. Make complete continuous black fabric and white armor side panels matching the existing black/white/gold suit; no transparent holes inside the torso. Do not add arms, hands, elbows, shoulder skin, weapons or new accessories. Keep the exact torso position, size, silhouette, armor design and gold centerline in the existing canvas. Keep original hair, face, legs and boots unchanged; they are reference-only and will not be used from your result. Background must be truly transparent, including both sides of the torso. No environment, glow, smoky background, ground or text. This is a technical restoration of missing clothing behind separated arm sprites, not a new character design.

Output: torso-fill-v3.png. This remained too narrow at the exposed sides and is retained as an intermediate reference, not consumed by the runtime.

## Selected Pass

Input: torso-fill-v3.png

Use case: precise-object-edit. Edit target: this 1024x1536 armless armored game sprite. ONE precise change: complete and widen the missing dark fabric side panels of the armored torso beneath BOTH armholes. The torso waist at y=500 through 590 is currently too narrowly pinched, leaving missing areas when animated arms move away. Add continuous matte black/dark charcoal fabric panels under the white chest armor, on the image LEFT from x=410 to 460 and on the image RIGHT from x=595 to 645. At y=500 the outer silhouette should be approximately x=415 and x=642, at y=550 approximately x=421 and x=634, taper gently into the existing waist at y=600. Match the existing black suit shading, simple seams, no new gold ornaments. These must be opaque clothed side panels, not background, skin, arms, or hands. Keep the central chest and abdomen, belt, original head, hair, legs and boots EXACTLY at the same position and scale. No arms, no weapon, no empty holes inside the torso. Transparent outside the silhouette. Output same 1024x1536 aligned full canvas. This is ONLY a missing-suit-panel restoration; do not redesign anything else.

Output: torso-side-fill-v4.png. Full-canvas alignment is 1024x1536; compositing is restricted to the under-arm vest mask. Raw alpha at former missing suit points (425,530), (430,545), (430,560), (620,530) is respectively 232, 241, 247, 248, versus source body alpha 0, 1, 1, 2.
