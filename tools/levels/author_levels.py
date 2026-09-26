#!/usr/bin/env python3
"""Expands the handmade themed object designs below into versioned level JSON.

Objects are authored by hand as primitives (box / cylinder / sphere), with blockers expressing which pieces must
come off first and how many screws sit on each face. The script lays screws out on those faces and assigns colors
by simulating one constructive play-through (so every level has at least one witness), then writes
Assets/ScrewGame/Resources/Levels/L01..L10.json. The C# pipeline (tools/dotnet ScrewGame.Cli validate) is the
authority on validity and solvability.
Run: python3 tools/levels/author_levels.py
"""
import json, math, os, random

R, B, Y, G, P, O, T, K = range(8)
# Part materials (Presentation/Palette.PartMaterial)
CREAM, SKY, GRASS, PINK, LILAC, SUN, ORANGE, WHITE, RED, BROWN, TEAL, GREY = range(12)
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "ScrewGame", "Resources", "Levels")
FACES = {"top": (0, 1, 0), "front": (0, 0, 1), "back": (0, 0, -1), "left": (-1, 0, 0), "right": (1, 0, 0), "bottom": (0, -1, 0)}
TRAYS, CAP, BUFFER = 4, 3, 5


def box(pid, pos, size, screws, material, blockers=(), rot=(0, 0, 0)):
    return dict(id=pid, shape="box", pos=list(pos), size=list(size), screws=screws, material=material, blockers=list(blockers), rot=list(rot))


def cyl(pid, pos, radius, height, screws, material, blockers=(), rot=(0, 0, 0)):
    return dict(id=pid, shape="cylinder", pos=list(pos), size=[radius * 2, height, radius * 2], screws=screws, material=material, blockers=list(blockers), rot=list(rot))


def ball(pid, pos, radius, screws, material, blockers=()):
    return dict(id=pid, shape="sphere", pos=list(pos), size=[radius * 2] * 3, screws=screws, material=material, blockers=list(blockers), rot=[0, 0, 0])


def grid(n, hu, hv, inset=0.28):
    """n points on a (2hu x 2hv) face, as evenly as the aspect allows."""
    hu, hv = max(0.0, hu - inset), max(0.0, hv - inset)
    best = None
    for rows in range(1, n + 1):
        cols = math.ceil(n / rows)
        su = 2 * hu / max(1, cols - 1) if cols > 1 else 9
        sv = 2 * hv / max(1, rows - 1) if rows > 1 else 9
        if rows > 1 and hv == 0:
            continue
        score = min(su, sv)
        if best is None or score > best[0]:
            best = (score, rows, cols)
    _, rows, cols = best
    pts = []
    for i in range(n):
        r, c = divmod(i, cols)
        in_row = cols if r < rows - 1 else n - cols * (rows - 1)
        fu = 0 if in_row == 1 else (c / (in_row - 1) - 0.5) * 2 * hu
        fv = 0 if rows == 1 else (r / (rows - 1) - 0.5) * 2 * hv
        pts.append((fu, fv))
    return pts


def layout(p):
    """Returns [(position, normal)] in the part's unrotated frame (object space offset by part position)."""
    x, y, z = p["pos"]
    sx, sy, sz = p["size"]
    out = []
    for face, n in p["screws"]:
        if p["shape"] == "sphere":
            r = sx / 2
            cx, cy, cz = FACES.get(face, (0, 0.5, 1))
            for i in range(n):
                # Spiral cap around the face direction.
                a = i * 2.39996
                t = math.sqrt((i + 0.5) / n) * 0.95
                d = [cx, cy, cz]
                l = math.sqrt(sum(v * v for v in d)); d = [v / l for v in d]
                u = [0, 1, 0] if abs(d[1]) < 0.9 else [1, 0, 0]
                u = [u[1] * d[2] - u[2] * d[1], u[2] * d[0] - u[0] * d[2], u[0] * d[1] - u[1] * d[0]]
                lu = math.sqrt(sum(v * v for v in u)); u = [v / lu for v in u]
                w = [d[1] * u[2] - d[2] * u[1], d[2] * u[0] - d[0] * u[2], d[0] * u[1] - d[1] * u[0]]
                ang = t * 1.05
                nrm = [math.cos(ang) * d[k] + math.sin(ang) * (math.cos(a) * u[k] + math.sin(a) * w[k]) for k in range(3)]
                out.append(([x + nrm[0] * r, y + nrm[1] * r, z + nrm[2] * r], nrm))
            continue
        if p["shape"] == "cylinder" and face == "side":
            r = sx / 2
            for i in range(n):
                ang = math.radians(-70 + 140 * (i / max(1, n - 1))) if n > 1 else 0
                nrm = [math.sin(ang), 0, math.cos(ang)]
                out.append(([x + nrm[0] * r, y, z + nrm[2] * r], nrm))
            continue
        nx, ny, nz = FACES[face]
        if p["shape"] == "cylinder":
            r = sx / 2 * 0.62
            top = y + ny * sy / 2
            pts = [(0, 0)] if n == 1 else [(r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n)) for i in range(n)]
            for a, b in pts:
                out.append(([x + a, top, z + b], [0, ny, 0]))
            continue
        if ny:
            axes = [((1, 0, 0), sx / 2), ((0, 0, 1), sz / 2)]
        elif nz:
            axes = [((1, 0, 0), sx / 2), ((0, 1, 0), sy / 2)]
        else:
            axes = [((0, 0, 1), sz / 2), ((0, 1, 0), sy / 2)]
        surface = (x + nx * sx / 2, y + ny * sy / 2, z + nz * sz / 2)
        (u, hu), (v, hv) = axes
        for fu, fv in grid(n, hu, hv):
            out.append(([surface[k] + u[k] * fu + v[k] * fv for k in range(3)], [nx, ny, nz]))
    return [([round(c, 3) for c in pos], [round(c, 4) for c in nrm]) for pos, nrm in out]


def assign_colors(parts, colors, seed):
    """Plays the object outside-in with the real routing rules and assigns each removed screw a color so that the
    play-through wins, mixing in buffer detours. Returns (per-part color lists, tray queue)."""
    rng = random.Random(seed)
    total = sum(n for p in parts for _, n in p["screws"])
    assert total % CAP == 0, "screw count must be a multiple of tray capacity"
    groups = total // CAP
    queue = []
    while len(queue) < groups:
        c = rng.choice(colors)
        if len(queue) >= 1 and queue[-1] == c:
            continue
        queue.append(c)
    counts = {p["id"]: sum(n for _, n in p["screws"]) for p in parts}
    remaining = dict(counts)
    released = set()
    assigned = {p["id"]: [] for p in parts}
    trays = queue[:TRAYS]
    tray_n = [0] * TRAYS
    cursor = TRAYS
    buffer = []
    left = {i: CAP for i in range(groups)}  # screws still needed per queue entry

    def need_color_counts():
        need = {}
        for gi, n in left.items():
            if n > 0:
                need[queue[gi]] = need.get(queue[gi], 0) + n
        return need

    need = {}
    for c in queue:
        need[c] = need.get(c, 0) + CAP
    tray_group = list(range(TRAYS))

    def stabilize():
        nonlocal cursor
        changed = True
        while changed:
            changed = False
            for t in range(TRAYS):
                if trays[t] is not None and tray_n[t] == CAP:
                    trays[t] = queue[cursor] if cursor < groups else None
                    tray_group[t] = cursor if cursor < groups else None
                    cursor += 1 if cursor < groups else 0
                    tray_n[t] = 0
                    changed = True
            for i, c in enumerate(list(buffer)):
                for t in range(TRAYS):
                    if trays[t] == c and tray_n[t] < CAP:
                        tray_n[t] += 1
                        buffer.remove(c)
                        changed = True
                        break
                if changed:
                    break

    order = []
    while len(released) < len(parts):
        avail = [p for p in parts if p["id"] not in released and all(b in released for b in p["blockers"]) and remaining[p["id"]] > 0]
        # Prefer finishing a started part sometimes so layers peel off naturally.
        p = rng.choice(avail)
        # Colors reachable now.
        open_tray = [trays[t] for t in range(TRAYS) if trays[t] is not None and tray_n[t] < CAP]
        # Buffer colors: future queue colors whose screws are not yet all spoken for by current trays/buffer.
        future = []
        pending = {}
        for c in buffer:
            pending[c] = pending.get(c, 0) + 1
        for t in range(TRAYS):
            if trays[t] is not None:
                pending[trays[t]] = pending.get(trays[t], 0) + (CAP - tray_n[t])
        for c, n in need.items():
            if n - pending.get(c, 0) > 0 and c not in open_tray:
                future.append(c)
        buffered_room = len(buffer) < BUFFER - 2
        if future and buffered_room and (not open_tray or rng.random() < 0.3):
            c = rng.choice(future)
            buffer.append(c)
        else:
            c = rng.choice(open_tray)
            t = next(t for t in range(TRAYS) if trays[t] == c and tray_n[t] < CAP)
            tray_n[t] += 1
        need[c] -= 1
        assigned[p["id"]].append(c)
        remaining[p["id"]] -= 1
        if remaining[p["id"]] == 0:
            released.add(p["id"])
        stabilize()
    assert all(v == 0 for v in need.values()), need
    # Shuffle each part's colors so the solution isn't readable from screw order on the face.
    for pid in assigned:
        rng.shuffle(assigned[pid])
    return assigned, queue


def level(num, name, obj, family, skill, difficulty, colors, parts, seed=1, pitch=24):
    lid = "L%02d" % num
    total = sum(n for p in parts for _, n in p["screws"])
    if total % CAP:
        raise SystemExit("%s: %d screws is not a multiple of %d" % (lid, total, CAP))
    for attempt in range(200):
        try:
            assigned, queue = assign_colors(parts, colors, seed + num * 101 + attempt * 7919)
            break
        except (IndexError, AssertionError, StopIteration):
            continue
    else:
        raise SystemExit(lid + ": no constructive color assignment found")
    screws = []
    n = 0
    for p in parts:
        spots = layout(p)
        for c, (sp, nrm) in zip(assigned[p["id"]], spots):
            screws.append(dict(Id="%s-s%02d" % (lid, n), PartId=p["id"], Color=c, Position=sp, Normal=nrm))
            n += 1
    return dict(
        SchemaVersion=1, Id=lid, ContentVersion=2, Name=name, ObjectId=obj, Family=family,
        TrayPositions=TRAYS, TrayCapacity=CAP, BufferSlots=BUFFER, TrayQueue=queue,
        Parts=[dict(Id=p["id"], Blockers=p["blockers"], Shape=p["shape"], Position=p["pos"], Rotation=p["rot"], Size=p["size"], Material=p["material"]) for p in parts],
        Screws=screws,
        Camera=dict(MinYaw=-180, MaxYaw=180, MinPitch=5, MaxPitch=70, Distance=9),
        Meta=dict(IntroducedSkill=skill, DifficultyHypothesis=difficulty, LayoutId="%s-layout" % obj, ReviewStatus="unreviewed"),
    )


LEVELS = [
    level(1, "Gift Box", "gift", "toys", "match colors into boxes", 1, [R, B, Y, G], [
        box("ribbon", [0, 0.86, 0], [0.5, 0.12, 2.3], [("top", 3)], RED),
        box("lid", [0, 0.7, 0], [2.4, 0.3, 2.4], [("top", 2), ("front", 1)], SUN, blockers=["ribbon"]),
        box("box", [0, -0.1, 0], [2.2, 1.4, 2.2], [("front", 4), ("right", 2)], SKY, blockers=["lid"]),
    ]),
    level(2, "Cupcake", "cupcake", "sweets", "first holding-slot use", 1, [R, B, Y, G, P], [
        ball("cherry", [0, 1.55, 0], 0.32, [("front", 1), ("top", 2)], RED),
        ball("icing", [0, 0.85, 0], 1.05, [("front", 6), ("top", 3)], PINK, blockers=["cherry"]),
        cyl("cup", [0, -0.35, 0], 1.0, 1.2, [("side", 5), ("top", 1)], SKY, blockers=["icing"]),
    ]),
    level(3, "Toy Car", "car", "toys", "rotate to find screws", 2, [R, B, Y, G, O], [
        box("roof", [0.1, 1.3, 0], [1.9, 0.2, 1.7], [("top", 3)], WHITE),
        box("cabin", [0.1, 0.8, 0], [2.0, 0.8, 1.6], [("front", 3), ("right", 2), ("left", 1)], SKY, blockers=["roof"]),
        box("body", [0, 0, 0], [3.8, 0.8, 1.9], [("front", 6), ("top", 2), ("right", 1)], RED, blockers=["cabin"]),
        cyl("wheel_fl", [-1.2, -0.35, 0.95], 0.45, 0.3, [("top", 1)], GREY, rot=(90, 0, 0)),
        cyl("wheel_fr", [1.2, -0.35, 0.95], 0.45, 0.3, [("top", 1)], GREY, rot=(90, 0, 0)),
        box("bumper", [2.0, -0.2, 0], [0.25, 0.35, 1.8], [("right", 2)], SUN),
        box("lights", [-2.0, 0.1, 0], [0.2, 0.3, 1.4], [("left", 2)], SUN),
    ]),
    level(4, "Cottage", "cottage", "houses", "layered blockers", 2, [R, B, Y, G, P, O], [
        box("chimney", [0.9, 2.15, -0.3], [0.45, 0.8, 0.45], [("front", 1), ("right", 1), ("top", 1)], RED),
        box("roof_l", [-0.72, 1.55, 0], [1.75, 0.18, 2.5], [("top", 4)], RED, blockers=["chimney"], rot=(0, 0, 32)),
        box("roof_r", [0.72, 1.55, 0], [1.75, 0.18, 2.5], [("top", 4)], RED, blockers=["chimney"], rot=(0, 0, -32)),
        box("door", [0, 0.05, 1.12], [0.7, 1.1, 0.1], [("front", 2)], BROWN),
        box("window_l", [-0.85, 0.55, 1.12], [0.55, 0.5, 0.08], [("front", 1)], SKY),
        box("window_r", [0.85, 0.55, 1.12], [0.55, 0.5, 0.08], [("front", 1)], SKY),
        box("walls", [0, 0.3, 0], [2.6, 1.6, 2.2], [("front", 4), ("right", 4), ("left", 2)], CREAM, blockers=["roof_l", "roof_r", "door", "window_l", "window_r"]),
        box("lawn", [0, -0.6, 0.3], [3.4, 0.2, 3.0], [("top", 5)], GRASS, blockers=["walls"]),
    ]),
    level(5, "Robot Buddy", "robot", "toys", "plan for new boxes", 3, [R, B, Y, G, P, T], [
        ball("antenna", [0, 2.55, 0], 0.18, [("top", 1)], RED),
        box("head", [0, 1.95, 0], [1.3, 0.95, 1.1], [("front", 3), ("top", 2), ("right", 1)], GREY, blockers=["antenna"]),
        box("chest", [0, 0.85, 0], [1.8, 1.2, 1.0], [("front", 5), ("right", 1)], SKY, blockers=["head"]),
        box("arm_l", [-1.2, 0.85, 0], [0.45, 1.2, 0.55], [("front", 2), ("left", 2)], ORANGE),
        box("arm_r", [1.2, 0.85, 0], [0.45, 1.2, 0.55], [("front", 2), ("right", 2)], ORANGE),
        box("leg_l", [-0.45, -0.25, 0], [0.55, 1.0, 0.65], [("front", 2)], GREY, blockers=["chest"]),
        box("leg_r", [0.45, -0.25, 0], [0.55, 1.0, 0.65], [("front", 2)], GREY, blockers=["chest"]),
        box("feet", [0, -0.9, 0.1], [1.7, 0.3, 1.0], [("top", 4), ("front", 1)], ORANGE, blockers=["leg_l", "leg_r"]),
    ]),
    level(6, "Flower Pot", "flower", "garden", "combine slots and blockers", 3, [R, B, Y, G, P, O, K], [
        ball("heart", [0, 2.1, 0.15], 0.35, [("front", 3)], SUN),
        ball("petal_n", [0, 2.7, 0], 0.4, [("front", 2)], PINK, blockers=["heart"]),
        ball("petal_e", [0.6, 2.1, 0], 0.4, [("front", 2)], PINK, blockers=["heart"]),
        ball("petal_w", [-0.6, 2.1, 0], 0.4, [("front", 2)], PINK, blockers=["heart"]),
        ball("petal_s", [0, 1.5, 0], 0.4, [("front", 2)], PINK, blockers=["heart"]),
        box("leaf_l", [-0.6, 0.85, 0], [0.9, 0.12, 0.5], [("top", 2)], GRASS, rot=(0, 0, 25)),
        box("leaf_r", [0.6, 0.85, 0], [0.9, 0.12, 0.5], [("top", 2)], GRASS, rot=(0, 0, -25)),
        box("stem", [0, 0.8, 0], [0.22, 1.4, 0.22], [("front", 3)], GRASS, blockers=["petal_s", "leaf_l", "leaf_r"]),
        cyl("pot", [0, -0.35, 0], 1.0, 1.1, [("side", 6), ("top", 3)], ORANGE, blockers=["stem"]),
        box("saucer", [0, -1.0, 0], [2.4, 0.2, 2.4], [("top", 6)], BROWN, blockers=["pot"]),
    ], seed=3),
    level(7, "Rocket", "rocket", "space", "watch the box queue", 3, [R, B, Y, G, P, O, T], [
        ball("nose", [0, 2.55, 0], 0.62, [("top", 3), ("front", 2)], RED),
        box("window", [0, 1.75, 0.66], [0.7, 0.7, 0.1], [("front", 3)], SKY),
        box("upper", [0, 1.55, 0], [1.3, 1.1, 1.3], [("front", 3), ("right", 2), ("left", 1)], WHITE, blockers=["nose", "window"]),
        box("band", [0, 0.8, 0], [1.45, 0.4, 1.45], [("front", 3), ("right", 1)], RED, blockers=["upper"]),
        box("lower", [0, 0.05, 0], [1.3, 1.1, 1.3], [("front", 6), ("right", 2)], WHITE, blockers=["band"]),
        box("fin_l", [-0.95, -0.25, 0], [0.6, 0.9, 0.14], [("front", 2)], ORANGE, rot=(0, 0, -12)),
        box("fin_r", [0.95, -0.25, 0], [0.6, 0.9, 0.14], [("front", 2)], ORANGE, rot=(0, 0, 12)),
        cyl("engine", [0, -0.75, 0], 0.55, 0.45, [("side", 3)], GREY, blockers=["lower", "fin_l", "fin_r"]),
    ], seed=5),
    level(8, "Windmill", "windmill", "houses", "deeper stacks", 4, [R, B, Y, G, P, O, T, K], [
        cyl("hub", [0, 2.05, 1.05], 0.28, 0.3, [("top", 1)], RED, rot=(90, 0, 0)),
        box("blade_h", [0, 2.05, 0.95], [3.4, 0.4, 0.1], [("front", 6)], WHITE, blockers=["hub"]),
        box("blade_v", [0, 2.05, 0.85], [0.4, 3.4, 0.1], [("front", 5)], WHITE, blockers=["hub"]),
        box("cap", [0, 2.35, 0], [1.3, 0.5, 1.4], [("top", 3), ("right", 2)], RED, blockers=["blade_v"]),
        box("tower", [0, 0.9, 0], [1.5, 2.4, 1.5], [("front", 5), ("right", 4), ("left", 2)], CREAM, blockers=["cap", "blade_h"]),
        box("door", [0, -0.1, 0.78], [0.6, 0.8, 0.08], [("front", 2)], BROWN),
        box("hill", [0, -0.45, 0], [3.2, 0.3, 2.6], [("top", 6)], GRASS, blockers=["tower", "door"]),
    ], seed=7),
    level(9, "Treehouse", "treehouse", "garden", "manage a crowded slot row", 4, [R, B, Y, G, P, O, T, K], [
        ball("crown", [0, 3.0, 0], 0.95, [("top", 5), ("front", 4)], GRASS),
        ball("bush_l", [-1.05, 2.45, 0], 0.65, [("front", 3), ("left", 2)], GRASS),
        ball("bush_r", [1.05, 2.45, 0], 0.65, [("front", 3), ("right", 2)], GRASS),
        box("roof", [0, 1.95, 0.25], [1.9, 0.22, 1.5], [("top", 3)], RED, blockers=["crown", "bush_l", "bush_r"]),
        box("hut", [0, 1.35, 0.25], [1.6, 1.0, 1.3], [("front", 4), ("right", 2)], SUN, blockers=["roof"]),
        box("ladder", [0.95, 0.35, 0.8], [0.4, 1.6, 0.1], [("front", 3)], BROWN),
        box("trunk", [0, 0.2, 0], [0.8, 2.0, 0.8], [("front", 3), ("left", 2)], BROWN, blockers=["hut", "ladder"]),
        box("ground", [0, -0.9, 0], [3.2, 0.25, 2.8], [("top", 6)], GRASS, blockers=["trunk"]),
    ], seed=11),
    level(10, "Castle", "castle", "houses", "full combination", 5, [R, B, Y, G, P, O, T, K], [
        box("flag", [0, 2.75, 0], [0.7, 0.4, 0.08], [("front", 2)], RED),
        cyl("tower_l", [-1.6, 0.75, 0.15], 0.5, 2.4, [("side", 4), ("top", 2)], LILAC),
        cyl("tower_r", [1.6, 0.75, 0.15], 0.5, 2.4, [("side", 4), ("top", 2)], LILAC),
        box("keep_top", [0, 2.2, 0], [1.4, 0.7, 1.2], [("front", 3), ("top", 2)], LILAC, blockers=["flag"]),
        box("gate", [0, 0.0, 0.83], [0.9, 1.1, 0.1], [("front", 3)], BROWN),
        box("keep", [0, 0.7, 0], [2.3, 2.2, 1.5], [("front", 7), ("right", 2), ("left", 2)], WHITE, blockers=["keep_top", "gate", "tower_l", "tower_r"]),
        box("moat", [0, -0.65, 0.35], [4.0, 0.25, 2.8], [("top", 9)], SKY, blockers=["keep"]),
    ], seed=13),
]

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for lv in LEVELS:
        with open(os.path.join(OUT, lv["Id"] + ".json"), "w") as f:
            json.dump(lv, f, indent=2)
            f.write("\n")
        print(lv["Id"], lv["Name"], len(lv["Parts"]), "parts", len(lv["Screws"]), "screws", len(lv["TrayQueue"]), "boxes")
