#!/usr/bin/env python3
"""Expands the handmade prototype level designs below into versioned level JSON.

Designs are authored by hand (parts, blockers, screw colors, placement faces). This script only
computes screw head coordinates on the chosen face so positions stay consistent with part sizes.
Run: python3 tools/levels/author_levels.py  (writes Assets/ScrewGame/Content/Levels/L01..L10.json)
Validation/solving is done by the C# production pipeline (tools/dotnet), never here.
"""
import json, os

R, B, Y, G, P, O, T, K = range(8)
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "ScrewGame", "Content", "Levels")

FACES = {"top": (0, 1, 0), "front": (0, 0, 1), "back": (0, 0, -1), "left": (-1, 0, 0), "right": (1, 0, 0)}


def part(pid, pos, size, colors, face="top", blockers=(), material=0, shape="box", rot=(0, 0, 0)):
    return dict(id=pid, pos=pos, size=size, colors=colors, face=face, blockers=list(blockers), material=material, shape=shape, rot=rot)


def screw_positions(p):
    n = len(p["colors"])
    x, y, z = p["pos"]
    sx, sy, sz = p["size"]
    nx, ny, nz = FACES[p["face"]]
    # Two in-face axes with half extents, inset from the edges.
    if ny:
        axes = [((1, 0, 0), sx / 2), ((0, 0, 1), sz / 2)]
    elif nz:
        axes = [((1, 0, 0), sx / 2), ((0, 1, 0), sy / 2)]
    else:
        axes = [((0, 0, 1), sz / 2), ((0, 1, 0), sy / 2)]
    surface = (x + nx * sx / 2, y + ny * sy / 2, z + nz * sz / 2)
    (u, hu), (v, hv) = axes
    if hv > hu:
        (u, hu), (v, hv) = (v, hv), (u, hu)
    rows = 1 if n <= 3 or hv < 0.45 else 2
    cols = (n + rows - 1) // rows
    out = []
    for i in range(n):
        r, c = divmod(i, cols)
        in_row = cols if r < rows - 1 else n - cols * (rows - 1)
        fu = 0 if in_row == 1 else (c / (in_row - 1) - 0.5) * 2 * (hu - 0.3)
        fv = 0 if rows == 1 else (r / (rows - 1) - 0.5) * 2 * (hv - 0.3)
        out.append([round(surface[k] + u[k] * fu + v[k] * fv, 3) for k in range(3)])
    return out, [nx, ny, nz]


def level(num, name, obj, family, skill, difficulty, queue, parts, cam=None):
    lid = "L%02d" % num
    screws = []
    n = 0
    for p in parts:
        pos, normal = screw_positions(p)
        for c, sp in zip(p["colors"], pos):
            screws.append(dict(Id="%s-s%02d" % (lid, n), PartId=p["id"], Color=c, Position=sp, Normal=normal))
            n += 1
    return dict(
        SchemaVersion=1, Id=lid, ContentVersion=1, Name=name, ObjectId=obj, Family=family,
        TrayPositions=2, TrayCapacity=3, BufferSlots=5, TrayQueue=queue,
        Parts=[dict(Id=p["id"], Blockers=p["blockers"], Shape=p["shape"], Position=p["pos"], Rotation=list(p["rot"]), Size=p["size"], Material=p["material"]) for p in parts],
        Screws=screws,
        Camera=cam or dict(MinYaw=-180, MaxYaw=180, MinPitch=15, MaxPitch=75, Distance=9),
        Meta=dict(IntroducedSkill=skill, DifficultyHypothesis=difficulty, LayoutId="%s-layout" % obj, ReviewStatus="unreviewed"),
    )


LEVELS = [
    level(1, "Workbench Plank", "plank", "workshop", "match colors into trays", 1, [R, B],
          [part("plank", [0, 0, 0], [4, 0.3, 1.2], [R, B, R, B, R, B], material=0)]),
    level(2, "Hinge Plate", "hinge", "workshop", "first buffer use", 1, [R, B, Y],
          [part("latch", [0, 0.35, 0], [1.6, 0.25, 1.0], [Y, R, R], material=1),
           part("base", [0, 0, 0], [3.6, 0.4, 2.2], [R, B, B, B, Y, Y], blockers=["latch"], material=0)]),
    level(3, "Tool Box", "toolbox", "workshop", "rotate to find screws", 2, [R, B, Y, G],
          [part("lid", [0, 0.75, 0], [3, 0.2, 1.6], [G, R, B], material=2),
           part("front", [0, 0, 0.75], [3, 1.3, 0.15], [R, R, Y], material=1, blockers=[]),
           part("back", [0, 0, -0.75], [3, 1.3, 0.15], [B, B, Y, G], material=1),
           part("floor", [0, -0.7, 0], [3, 0.15, 1.5], [Y, G], face="top", blockers=["lid", "front", "back"], material=0)],
          ),
    level(4, "Toy Car", "car", "toys", "layered blockers", 2, [R, Y, B, G],
          [part("roof", [0, 1.0, 0], [1.8, 0.2, 1.4], [B, Y], material=3),
           part("cabin", [0, 0.55, 0], [2.4, 0.5, 1.5], [R, G, Y], face="front", blockers=["roof"], material=4),
           part("hood", [0, 0.2, 0], [3.8, 0.2, 1.6], [R, B, G, R], blockers=["cabin"], material=5),
           part("chassis", [0, -0.15, 0], [4, 0.3, 1.8], [Y, B, G], face="front", blockers=["hood"], material=0)]),
    level(5, "Spinning Top", "top", "toys", "plan for tray replacement", 3, [R, B, R, Y, B, Y],
          [part("cap", [0, 0.9, 0], [1.2, 0.2, 1.2], [Y, B, Y], material=6),
           part("ring", [0, 0.5, 0], [2.4, 0.25, 2.4], [R, Y, B, R], blockers=["cap"], material=4),
           part("disc", [0, 0.1, 0], [3.2, 0.25, 3.2], [R, R, B, Y, B, R], blockers=["ring"], material=5),
           part("spike", [0, -0.35, 0], [0.8, 0.6, 0.8], [Y, B, R, Y, B], face="front", material=0)]),
    level(6, "Robot Friend", "robot", "toys", "combine buffer and blockers", 3, [G, R, B, Y, G, R],
          [part("head", [0, 1.6, 0], [1.2, 0.8, 1.0], [Y, B], face="front", material=6),
           part("chest", [0, 0.6, 0], [2.0, 1.1, 0.4], [G, R, G, B], face="front", blockers=["head"], material=4),
           part("arm_l", [-1.3, 0.6, 0], [0.4, 1.0, 0.4], [R, Y], face="left", material=5),
           part("arm_r", [1.3, 0.6, 0], [0.4, 1.0, 0.4], [G, B], face="right", material=5),
           part("back", [0, 0.6, -0.3], [2.0, 1.1, 0.2], [R, R, G], face="back", material=1),
           part("hips", [0, -0.3, 0], [1.8, 0.5, 0.8], [R, G, R, Y, G], face="front", blockers=["chest", "back"], material=0)]),
    level(7, "Garden Shed", "shed", "buildings", "watch the queue", 3, [R, B, Y, R, G, B],
          [part("roof_l", [-0.7, 1.35, 0], [1.6, 0.15, 2.2], [B, Y, R], material=5, rot=(0, 0, 25)),
           part("roof_r", [0.7, 1.35, 0], [1.6, 0.15, 2.2], [G, R, B], material=5, rot=(0, 0, -25)),
           part("door", [0, 0.2, 1.0], [0.9, 1.4, 0.1], [R, G, B], face="front", material=3),
           part("wall_f", [0, 0.3, 0.95], [2.6, 1.6, 0.1], [Y, R, G], face="front", blockers=["door"], material=1),
           part("wall_b", [0, 0.3, -0.95], [2.6, 1.6, 0.1], [B, B, R], face="back", material=1),
           part("floor", [0, -0.55, 0], [2.8, 0.15, 2.0], [R, B, Y], blockers=["roof_l", "roof_r", "wall_f", "wall_b"], material=0)]),
    level(8, "Windmill", "windmill", "buildings", "deeper stacks", 4, [Y, P, R, B, P, Y, R],
          [part("blade_a", [0, 1.6, 0.6], [3.2, 0.3, 0.1], [P, Y, R], face="front", material=6),
           part("blade_b", [0, 1.6, 0.5], [0.3, 3.2, 0.1], [B, P, Y], face="front", blockers=["blade_a"], material=6),
           part("hub", [0, 1.6, 0.4], [0.8, 0.8, 0.2], [R, B, P], face="front", blockers=["blade_b"], material=4),
           part("tower", [0, 0.3, 0.2], [1.4, 2.4, 0.2], [Y, R, P, R], face="front", blockers=["hub"], material=1),
           part("tower_b", [0, 0.3, -0.4], [1.4, 2.4, 0.2], [B, Y, P, Y], face="back", material=1),
           part("base", [0, -1.0, 0], [2.2, 0.3, 1.6], [R, R, P, Y], blockers=["tower", "tower_b"], material=0)]),
    level(9, "Lighthouse", "lighthouse", "buildings", "manage a crowded buffer", 4, [T, R, B, T, Y, R, B, Y],
          [part("lamp", [0, 2.1, 0], [0.9, 0.5, 0.9], [T, R, Y], face="front", material=7),
           part("gallery", [0, 1.7, 0], [1.6, 0.2, 1.6], [B, T, R, Y], blockers=["lamp"], material=4),
           part("upper", [0, 1.0, 0.55], [1.1, 1.1, 0.1], [T, B, Y, R], face="front", blockers=["gallery"], material=1),
           part("upper_b", [0, 1.0, -0.55], [1.1, 1.1, 0.1], [R, T, B], face="back", blockers=["gallery"], material=1),
           part("lower", [0, -0.2, 0.7], [1.5, 1.2, 0.1], [Y, B, T, R], face="front", blockers=["upper"], material=5),
           part("lower_b", [0, -0.2, -0.7], [1.5, 1.2, 0.1], [B, Y, T, R], face="back", blockers=["upper_b"], material=5),
           part("rock", [0, -1.0, 0], [2.6, 0.4, 2.2], [Y, B], blockers=["lower", "lower_b"], material=0)]),
    level(10, "Clock Tower", "clocktower", "buildings", "full combination", 5, [R, K, G, B, Y, R, K, G, B, Y],
          [part("spire", [0, 2.5, 0], [0.8, 0.6, 0.8], [K, R, G], face="front", material=6),
           part("clock_f", [0, 1.6, 0.6], [1.3, 1.3, 0.1], [B, K, Y, R], face="front", blockers=["spire"], material=7),
           part("clock_b", [0, 1.6, -0.6], [1.3, 1.3, 0.1], [G, Y, K, B], face="back", blockers=["spire"], material=7),
           part("side_l", [-0.75, 0.6, 0], [0.1, 2.0, 1.2], [R, B, Y, K, G], face="left", material=1),
           part("side_r", [0.75, 0.6, 0], [0.1, 2.0, 1.2], [G, R, K, B, Y], face="right", material=1),
           part("door", [0, 0.1, 0.65], [0.7, 1.0, 0.1], [Y, G, B], face="front", blockers=["clock_f"], material=3),
           part("base", [0, -0.6, 0], [2.4, 0.4, 2.0], [R, K, B, G, Y, R], blockers=["door", "clock_b"], material=0)]),
]

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for lv in LEVELS:
        with open(os.path.join(OUT, lv["Id"] + ".json"), "w") as f:
            json.dump(lv, f, indent=2, sort_keys=False)
            f.write("\n")
        counts = {}
        for s in lv["Screws"]:
            counts[s["Color"]] = counts.get(s["Color"], 0) + 1
        q = {}
        for c in lv["TrayQueue"]:
            q[c] = q.get(c, 0) + 3
        print(lv["Id"], len(lv["Screws"]), "screws", "OK" if counts == q else "COUNT MISMATCH %s vs %s" % (counts, q))
