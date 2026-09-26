export const SCREW_COLORS = [0xe53935, 0x1e88e5, 0x43a047, 0xfdd835, 0x8e24aa, 0xfb8c00, 0xec407a, 0x00acc1];
export const PLANK_COLORS = [0xffcc80, 0x80deea, 0xc5e1a5, 0xf48fb1, 0xb39ddb, 0xfff59d, 0x90caf9, 0xffab91];
export const TRAY_SIZE = 5;
export const BOX_SIZE = 3;
export const BOARD_HALF = 4.4;

export function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function shuffle(arr, rng) {
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
  return arr;
}

function buildLevel(level, seed) {
  const rng = mulberry32(seed);
  const nPlanks = Math.min(3 + level, 16);
  const nColors = Math.min(2 + Math.ceil(level / 2), SCREW_COLORS.length);
  const planks = [];
  const screws = [];

  const tryAddPlank = () => {
    for (let attempt = 0; attempt < 80; attempt++) {
      const isPlate = rng() < 0.22;
      let halfL, halfW, radius, local;
      if (isPlate) {
        halfL = halfW = 0.95 + rng() * 0.35;
        radius = 0.35;
        const i = halfL - 0.45;
        local = rng() < 0.5
          ? [[-i, -i], [i, -i], [i, i], [-i, i]]
          : [[-i, -i], [i, i]];
      } else {
        const len = 2.4 + rng() * 3.4;
        halfL = len / 2;
        halfW = 0.5;
        radius = halfW;
        local = [[-halfL + 0.5, 0], [halfL - 0.5, 0]];
        if (len > 4.4 && rng() < 0.5) local.push([0, 0]);
      }
      const angle = isPlate ? rng() * Math.PI : Math.floor(rng() * 8) * (Math.PI / 8);
      const cx = (rng() * 2 - 1) * 3.2;
      const cy = (rng() * 2 - 1) * 3.2;
      const c = Math.cos(angle), s = Math.sin(angle);
      const toWorld = ([lx, ly]) => [cx + lx * c - ly * s, cy + lx * s + ly * c];
      const corners = [[-halfL, -halfW], [halfL, -halfW], [halfL, halfW], [-halfL, halfW]].map(toWorld);
      if (corners.some(([x, y]) => Math.abs(x) > BOARD_HALF || Math.abs(y) > BOARD_HALF)) continue;
      const world = local.map(toWorld);
      const tooClose = world.some(([x, y]) => screws.some((o) => Math.hypot(o.x - x, o.y - y) < 0.85));
      if (tooClose) continue;
      const id = planks.length;
      const screwIds = [];
      world.forEach(([x, y], k) => {
        const sid = screws.length;
        screws.push({ id: sid, plankId: id, x, y, lx: local[k][0], ly: local[k][1], color: 0 });
        screwIds.push(sid);
      });
      planks.push({ id, cx, cy, angle, halfL, halfW, radius, screwIds });
      return true;
    }
    return false;
  };

  for (let i = 0; i < nPlanks; i++) tryAddPlank();
  let guard = 0;
  while (screws.length % BOX_SIZE !== 0 && guard++ < 30) tryAddPlank();
  if (screws.length % BOX_SIZE !== 0 || planks.length < 2) return null;

  const colorList = [];
  for (let g = 0; g < screws.length / BOX_SIZE; g++) {
    for (let k = 0; k < BOX_SIZE; k++) colorList.push(g % nColors);
  }
  shuffle(colorList, rng);
  screws.forEach((s, i) => { s.color = colorList[i]; });
  return { level, seed, planks, screws };
}

export class Game {
  constructor(data) {
    this.data = data;
    this.planks = data.planks.map((p) => ({ ...p, removed: false, remaining: p.screwIds.length }));
    this.screws = data.screws.map((s) => ({ ...s, state: 'board' }));
    this.tray = Array(TRAY_SIZE).fill(null);
    this.boxes = [null, null];
    this.status = 'playing';
    for (let i = 0; i < this.boxes.length; i++) this.boxes[i] = this.newBox();
  }

  covers(p, x, y, margin) {
    const dx = x - p.cx, dy = y - p.cy;
    const c = Math.cos(-p.angle), s = Math.sin(-p.angle);
    const lx = dx * c - dy * s, ly = dx * s + dy * c;
    const qx = Math.abs(lx) - (p.halfL - p.radius);
    const qy = Math.abs(ly) - (p.halfW - p.radius);
    const outside = Math.hypot(Math.max(qx, 0), Math.max(qy, 0));
    const dist = outside + Math.min(Math.max(qx, qy), 0) - p.radius;
    return dist < margin;
  }

  isAccessible(s) {
    if (s.state !== 'board') return false;
    return !this.planks.some((p) => !p.removed && p.id > s.plankId && this.covers(p, s.x, s.y, 0.28));
  }

  accessibleScrews() {
    return this.screws.filter((s) => this.isAccessible(s));
  }

  countColor(color, pred) {
    return this.screws.reduce((n, s) => n + (s.color === color && pred(s) ? 1 : 0), 0);
  }

  trayCount(color) {
    return this.countColor(color, (s) => s.state === 'tray');
  }

  openBoxFor(color) {
    return this.boxes.findIndex((b) => b && b.color === color && b.slots.length < BOX_SIZE);
  }

  newBox() {
    const colors = [...new Set(this.screws.map((s) => s.color))];
    let best = null, bestScore = -Infinity;
    for (const c of colors) {
      const unboxed = this.countColor(c, (s) => s.state === 'board' || s.state === 'tray');
      const free = this.boxes.reduce((n, b) => n + (b && b.color === c ? BOX_SIZE - b.slots.length : 0), 0);
      if (unboxed - free <= 0) continue;
      const acc = this.countColor(c, (s) => this.isAccessible(s));
      const score = this.trayCount(c) * 100 + acc * 10 + unboxed;
      if (score > bestScore) { bestScore = score; best = c; }
    }
    return best === null ? null : { color: best, slots: [] };
  }

  click(id) {
    const s = this.screws[id];
    if (this.status !== 'playing' || !s) return null;
    if (!this.isAccessible(s)) return [{ type: 'blocked', screw: id }];
    if (this.openBoxFor(s.color) < 0 && !this.tray.includes(null)) {
      this.status = 'lost';
      return [{ type: 'blocked', screw: id }, { type: 'lose' }];
    }
    const events = [{ type: 'unscrew', screw: id }];
    const p = this.planks[s.plankId];
    p.remaining--;
    s.state = 'moving';
    this.place(s, events);
    if (p.remaining === 0) {
      p.removed = true;
      events.push({ type: 'plankFall', plank: p.id });
    }
    if (this.screws.every((o) => o.state === 'box')) {
      this.status = 'won';
      events.push({ type: 'win' });
    } else if (!this.tray.includes(null) && !this.accessibleScrews().some((o) => this.openBoxFor(o.color) >= 0)) {
      this.status = 'lost';
      events.push({ type: 'lose' });
    }
    return events;
  }

  place(s, events) {
    const bi = this.openBoxFor(s.color);
    if (bi >= 0) {
      const b = this.boxes[bi];
      b.slots.push(s.id);
      s.state = 'box';
      events.push({ type: 'toBox', screw: s.id, box: bi, slot: b.slots.length - 1 });
      if (b.slots.length === BOX_SIZE) this.completeBox(bi, events);
      return;
    }
    const ti = this.tray.indexOf(null);
    this.tray[ti] = s.id;
    s.state = 'tray';
    events.push({ type: 'toTray', screw: s.id, slot: ti });
  }

  completeBox(bi, events) {
    events.push({ type: 'boxDone', box: bi });
    this.boxes[bi] = null;
    const nb = this.newBox();
    this.boxes[bi] = nb;
    events.push({ type: 'boxNew', box: bi, color: nb ? nb.color : null });
    if (!nb) return;
    for (let i = 0; i < this.tray.length && nb.slots.length < BOX_SIZE; i++) {
      const id = this.tray[i];
      if (id === null || this.screws[id].color !== nb.color) continue;
      this.tray[i] = null;
      nb.slots.push(id);
      this.screws[id].state = 'box';
      events.push({ type: 'trayToBox', screw: id, box: bi, slot: nb.slots.length - 1 });
    }
    if (nb.slots.length === BOX_SIZE) this.completeBox(bi, events);
  }
}

export function solve(data) {
  const g = new Game(data);
  let guard = 0;
  while (g.status === 'playing' && guard++ < 2000) {
    const acc = g.accessibleScrews();
    if (!acc.length) return false;
    let best = null, bestScore = -Infinity;
    for (const s of acc) {
      let score = g.openBoxFor(s.color) >= 0 ? 1000 : 0;
      score += g.trayCount(s.color) * 50;
      score += acc.filter((o) => o.color === s.color).length * 5;
      score -= g.planks[s.plankId].remaining;
      if (score > bestScore) { bestScore = score; best = s; }
    }
    g.click(best.id);
  }
  return g.status === 'won';
}

export function generateLevel(level) {
  let fallback = null;
  for (let attempt = 0; attempt < 200; attempt++) {
    const data = buildLevel(level, level * 7919 + attempt * 104729);
    if (!data) continue;
    fallback = fallback || data;
    if (solve(data)) return data;
  }
  return fallback;
}
