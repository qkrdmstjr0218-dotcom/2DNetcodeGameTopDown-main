from pathlib import Path
import json

HERE = Path(__file__).resolve().parent
scope={'__file__':str(HERE/'audit_existing_aim_path.py')}
code=(HERE/'audit_existing_aim_path.py').read_text(encoding='utf-8')
exec(compile(code.split('\nrows=')[0],'<existing aim audit>', 'exec'),scope)
weapon=scope['weapon']
grips=scope['grips']
ppu=204.8
texture_pivot=(256.,499.2)
aim_pixel=[texture_pivot[0]-weapon[0]*ppu,texture_pivot[1]+weapon[1]*ppu]
# The stock landmark is a manually inspected source-art estimate, not a live
# camera-space capture. Align the new art without resizing the rifle.
stock_pixel_x=272.
stock_local_x=(stock_pixel_x-aim_pixel[0])/ppu
preview_scale=(939.-432.)/(grips['muzzle'][0]-stock_local_x)
rotation_pivot=[519.-grips['muzzle'][1]*preview_scale,939.-grips['muzzle'][0]*preview_scale]
rig_path=HERE/'FrontShootingSandbox/rig.json'
rig=json.loads(rig_path.read_text(encoding='utf-8'))
rig['legacyPivot']=rig.get('legacyPivot',rig['pivot'])
rig['pivot']=rotation_pivot
rig['referenceAxisX']=519
rig['trajectory']={'defaultMode':'ground-extension','previewPixelsPerUnit':preview_scale,
 'sourceAimPixel':aim_pixel,'sourceStockRearPixelXEstimate':stock_pixel_x,
 'localMuzzle':grips['muzzle'].tolist(),
 'groundedDownAngle':scope['scalar']('groundedDownAngle'),
 'groundedDownLean':scope['scalar']('groundedDownLean'),
 'groundedDownGunOffset':scope['ground_offset'].tolist(),
 'airOffsets':[[45,0,0],[60,.10,0],[75,.225,.06],[90,.45,.10]],
 'groundExtensionNote':'New fixed-pivot extension beyond existing -55 clamp, not an existing Unity ground pose.',
 'liveUnityVerified':False,'mapping':'Right-facing serialized aim geometry, normalized to existing front rifle length; not validated world-scale matching.'}
rig_path.write_text(json.dumps(rig,indent=2),encoding='utf-8')
(HERE/'FrontShootingSandbox/trajectory-source-check.json').write_text(json.dumps({'rotationPivot':rotation_pivot,'previewPixelsPerUnit':preview_scale,'unityMuzzleLocal':grips['muzzle'].tolist(),'sourceAimPixel':aim_pixel,'unityFilesChanged':False,'liveCapture':False},indent=2),encoding='utf-8')
print(json.dumps({'pivot':rotation_pivot,'sourceAimPixel':aim_pixel,'previewScale':preview_scale,'unityLocalMuzzle':grips['muzzle'].tolist()}))
