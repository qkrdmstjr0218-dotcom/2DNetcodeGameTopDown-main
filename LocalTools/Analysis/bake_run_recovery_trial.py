"""Separate run revision: change only the trailing-foot recovery path."""
from pathlib import Path
import ast

root = Path(__file__).resolve().parents[2]
source = (root / 'LocalTools/Analysis/bake_run_trial.py').read_text()
tree = ast.parse(source)
replacement = ast.parse('''
def foot(phase):
    t = phase % 1
    if t < .4:
        lift = 28 * ns['ease']((t - .30) / .10)
        return (312 - 130 * t / .4, 430 - lift), .06 * ns['ease']((t - .28) / .12)
    if t < .6:
        u = (t - .4) / .2
        blend = ns['ease'](u)
        return (182 - 32 * blend, 402 - 65 * blend), .06 + .54 * blend
    u = (t - .6) / .4
    blend = u*u
    landing = ns['ease']((u - .35) / .65)
    return (150 + 162 * blend, 337 + 93 * landing), .6 * (1 - blend)
''').body[0]
for index, node in enumerate(tree.body):
    if isinstance(node, ast.FunctionDef) and node.name == 'foot':
        tree.body[index] = replacement


class TrialPaths(ast.NodeTransformer):
    def visit_Constant(self, node):
        if node.value == 'RunTrial.png':
            node.value = 'RunRecoveryTrial.png'
        elif node.value == 'run-trial-joints.json':
            node.value = 'run-recovery-trial-joints.json'
        return node


tree = ast.fix_missing_locations(TrialPaths().visit(tree))
# Rotate the accepted boot offset with the shin, rather than leaving it on world X.
helper = ast.parse('''
def boot_transform(side, ankle, knee, phase, tilt):
    shin_rotation = ns['angle'](knee, ankle) - ns['angle'](ns['nearKnee'], ns['nearAnkle'])
    shin_rotation = math.atan2(math.sin(shin_rotation), math.cos(shin_rotation))
    t = phase % 1
    follow = ns['ease']((t-.4)/.2) * (1-ns['ease']((t-.75)/.25))
    rotation = tilt + follow * (max(-1.45, min(1.45, shin_rotation)) - tilt)
    def rotated(vector, angle):
        x, y = vector
        return (x*math.cos(angle)-y*math.sin(angle), x*math.sin(angle)+y*math.cos(angle))
    import numpy as np
    source_knee, source_ankle = ns[side+'Knee'], ns[side+'Ankle']
    ys, xs = np.nonzero(np.asarray(ns['calves'][side])[:,:,3]>220)
    axis = np.array(source_ankle)-source_knee
    axis = axis / np.linalg.norm(axis)
    projection = (xs-source_knee[0])*axis[0]+(ys-source_knee[1])*axis[1]
    end = projection > projection.max()-12
    center = (float(xs[end].mean()),float(ys[end].mean()))
    calf_rotation = ns['angle'](knee,ankle)-ns['angle'](source_knee,source_ankle)
    calf_socket = rotated((center[0]-source_ankle[0],center[1]-source_ankle[1]),calf_rotation)
    boot_socket = rotated((350-ns['nearAnkle'][0],417-ns['nearAnkle'][1]),rotation)
    position = (ankle[0]+calf_socket[0]-boot_socket[0], ankle[1]+calf_socket[1]-boot_socket[1])
    return position, rotation

def clear_swing_foot(hip, ankle, l1, l2, phase, tilt, side):
    t = phase % 1
    if t < .4:
        return ankle
    import numpy as np
    ys, xs = np.nonzero(np.asarray(ns['new'])[:, :, 3] > 0)
    xs = xs - ns['nearAnkle'][0]
    ys = ys - ns['nearAnkle'][1]
    clearance = (20 * (1-ns['ease']((t-.8)/.2))
                 + 12 * math.sin(math.pi*(t-.4)/.6) ** 2)
    limit = 465 - clearance
    for iteration in range(12):
        distance = ns['dist'](hip, ankle)
        phi = ns['angle'](hip, ankle)
        bend = math.acos(max(-1, min(1, (l1*l1+distance*distance-l2*l2)/(2*l1*distance))))
        knee = (hip[0]+l1*math.cos(phi-bend), hip[1]+l1*math.sin(phi-bend))
        position, rotation = boot_transform(side, ankle, knee, phase, tilt)
        bottom = position[1] + (xs*math.sin(rotation)+ys*math.cos(rotation)).max()
        if bottom <= limit + .1:
            return ankle
        ankle = (ankle[0], ankle[1]-(bottom-limit))
    raise ValueError('Swing foot clearance did not converge')

def draw_following_boot(tile, side, ankle, knee, phase, tilt):
    position, rotation = boot_transform(side, ankle, knee, phase, tilt)
    t = phase % 1
    boot = Image.new('RGBA', (512, 512))
    ns['draw_part'](boot, ns['new'], ns['nearAnkle'], position, rotation)
    import numpy as np
    calf = Image.new('RGBA', (512,512))
    ns['draw_part'](calf, ns['calves'][side], ns[side+'Knee'], knee,
                    ns['angle'](knee,ankle)-ns['angle'](ns[side+'Knee'],ns[side+'Ankle']))
    ys, xs = np.nonzero(np.asarray(calf)[:,:,3]>220)
    axis = np.array(ankle)-knee
    axis /= np.linalg.norm(axis)
    distal = (xs-ankle[0])*axis[0]+(ys-ankle[1])*axis[1] > -28
    xs, ys = xs[distal], ys[distal]
    coverage = np.asarray(boot)[:,:,3]
    exposed = int(np.count_nonzero(coverage[ys,xs]<200))
    tile.alpha_composite(boot)
    bounds = boot.getbbox()
    boot_checks.append({'side': side, 'phase': t, 'bottomY': bounds[3], 'rotation': rotation, 'cuffExposedPixels': exposed})
''').body


class FollowBoot(ast.NodeTransformer):
    def visit_Assign(self, node):
        if len(node.targets) == 1 and isinstance(node.targets[0], ast.Name) and node.targets[0].id == 'd':
            correction = ast.parse("a = clear_swing_foot(h, a, l1, l2, p + (.5 if side=='far' else 0), tilt, side)").body[0]
            return [correction, node]
        return self.generic_visit(node)

    def visit_Expr(self, node):
        call = node.value
        if isinstance(call, ast.Call) and isinstance(call.func, ast.Subscript):
            if isinstance(call.func.slice, ast.Constant) and call.func.slice.value == 'draw_part':
                if len(call.args) > 1 and isinstance(call.args[1], ast.Subscript):
                    if isinstance(call.args[1].slice, ast.Constant) and call.args[1].slice.value == 'new':
                        return ast.parse("draw_following_boot(tile, side, a, k, p + (.5 if side=='far' else 0), tilt)").body[0]
        return self.generic_visit(node)


tree = FollowBoot().visit(tree)
tree.body[0:0] = helper
tree.body.insert(0, ast.parse('boot_checks = []').body[0])
tree = ast.fix_missing_locations(tree)
scope = {'__file__': str(root / 'LocalTools/Analysis/bake_run_trial.py')}
exec(compile(tree, '<run-recovery-trial>', 'exec'), scope)
import json
(root / 'LocalTools/Analysis/run-boot-clearance.json').write_text(json.dumps(scope['boot_checks'], indent=2))
for side in ('far', 'near'):
    poses = [pose for pose in scope['boot_checks'] if pose['side'] == side]
    ground = next(pose['bottomY'] for pose in poses if pose['phase'] == 0)
    swing = [ground-pose['bottomY'] for pose in poses if .425 <= pose['phase'] <= .9]
    print(f'{side}: contact sole Y={ground}, swing minimum clearance={min(swing)}px')
import subprocess
import sys
subprocess.run([sys.executable, str(root / 'LocalTools/Analysis/repair_run_contour.py'), '--fresh'], check=True)
