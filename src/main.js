import * as THREE from 'three';
import { OrbitControls } from 'three/addons/OrbitControls.js';
import { Game, generateLevel, SCREW_COLORS, PLANK_COLORS, TRAY_SIZE, BOX_SIZE } from './logic.js';

const LAYER_GAP = 0.32;
const PLANK_DEPTH = 0.22;
const BOX_X = [-2.3, 2.3];
const BOX_Y = 7.1;
const BOX_SLOT_X = [-0.9, 0, 0.9];
const TRAY_Y = 5.55;
const TRAY_SLOT_X = Array.from({ length: TRAY_SIZE }, (_, i) => (i - (TRAY_SIZE - 1) / 2) * 1.05);
const UI_Z = 0.42;

const canvas = document.getElementById('scene');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 200);
const controls = new OrbitControls(camera, canvas);
controls.target.set(0, 1.4, 0);
controls.enablePan = false;
controls.enableDamping = true;
controls.minAzimuthAngle = -0.6;
controls.maxAzimuthAngle = 0.6;
controls.minPolarAngle = Math.PI / 2 - 0.7;
controls.maxPolarAngle = Math.PI / 2 + 0.35;

scene.add(new THREE.HemisphereLight(0xffffff, 0x3a4a5e, 1.3));
const sun = new THREE.DirectionalLight(0xffffff, 2.2);
sun.position.set(6, 9, 16);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
Object.assign(sun.shadow.camera, { left: -11, right: 11, top: 11, bottom: -11, near: 1, far: 50 });
sun.shadow.bias = -0.0005;
scene.add(sun);

function roundedRectShape(hw, hh, r) {
  const s = new THREE.Shape();
  s.moveTo(-hw + r, -hh);
  s.lineTo(hw - r, -hh);
  s.absarc(hw - r, -hh + r, r, -Math.PI / 2, 0, false);
  s.lineTo(hw, hh - r);
  s.absarc(hw - r, hh - r, r, 0, Math.PI / 2, false);
  s.lineTo(-hw + r, hh);
  s.absarc(-hw + r, hh - r, r, Math.PI / 2, Math.PI, false);
  s.lineTo(-hw, -hh + r);
  s.absarc(-hw + r, -hh + r, r, Math.PI, Math.PI * 1.5, false);
  return s;
}

function slab(hw, hh, r, depth, color, extra = {}) {
  const geo = new THREE.ExtrudeGeometry(roundedRectShape(hw, hh, r), {
    depth, bevelEnabled: true, bevelThickness: 0.04, bevelSize: 0.04, bevelSegments: 2, curveSegments: 12,
  });
  const mesh = new THREE.Mesh(geo, new THREE.MeshStandardMaterial({ color, roughness: 0.6, ...extra }));
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  return mesh;
}

const board = slab(5, 5, 0.6, 0.5, 0xb9824f, { roughness: 0.8 });
board.position.z = -0.54;
board.castShadow = false;
scene.add(board);
const boardHoleGeo = new THREE.CircleGeometry(0.12, 16);
const boardHoleMat = new THREE.MeshBasicMaterial({ color: 0x5d3b1c });
for (let x = -4; x <= 4; x += 1) {
  for (let y = -4; y <= 4; y += 1) {
    const h = new THREE.Mesh(boardHoleGeo, boardHoleMat);
    h.position.set(x, y, 0.001);
    scene.add(h);
  }
}

const holeGeo = new THREE.CircleGeometry(0.26, 24);
const holeMat = new THREE.MeshBasicMaterial({ color: 0x1b252f });

const tray = slab(2.85, 0.55, 0.45, 0.3, 0x455a64);
tray.position.set(0, TRAY_Y, 0.05);
scene.add(tray);
TRAY_SLOT_X.forEach((x) => {
  const h = new THREE.Mesh(holeGeo, holeMat);
  h.position.set(x, TRAY_Y, UI_Z - 0.01);
  scene.add(h);
});

const headGeo = new THREE.CylinderGeometry(0.3, 0.3, 0.14, 28).rotateX(Math.PI / 2).translate(0, 0, 0.07);
const slotGeo = new THREE.BoxGeometry(0.42, 0.07, 0.05).translate(0, 0, 0.14);
const slotMat = new THREE.MeshStandardMaterial({ color: 0x222222 });
const shaftMat = new THREE.MeshStandardMaterial({ color: 0xb0bec5, metalness: 0.85, roughness: 0.3 });
const threadGeo = new THREE.TorusGeometry(0.1, 0.025, 6, 16);
const screwMats = SCREW_COLORS.map((c) => new THREE.MeshStandardMaterial({ color: c, metalness: 0.25, roughness: 0.35 }));

function makeScrew(color, shaftLen) {
  const g = new THREE.Group();
  const head = new THREE.Mesh(headGeo, screwMats[color]);
  head.castShadow = true;
  g.add(head);
  const s1 = new THREE.Mesh(slotGeo, slotMat);
  const s2 = new THREE.Mesh(slotGeo, slotMat);
  s2.rotation.z = Math.PI / 2;
  g.add(s1, s2);
  const shaft = new THREE.Mesh(new THREE.CylinderGeometry(0.09, 0.05, shaftLen, 10).rotateX(Math.PI / 2).translate(0, 0, -shaftLen / 2), shaftMat);
  shaft.castShadow = true;
  g.add(shaft);
  for (let z = -0.12; z > -shaftLen + 0.08; z -= 0.12) {
    const t = new THREE.Mesh(threadGeo, shaftMat);
    t.position.z = z;
    g.add(t);
  }
  return g;
}

function makePlank(p) {
  const shape = roundedRectShape(p.halfL, p.halfW, p.radius);
  p.screwIds.forEach((sid) => {
    const s = game.screws[sid];
    const hole = new THREE.Path();
    hole.absarc(s.lx, s.ly, 0.16, 0, Math.PI * 2, true);
    shape.holes.push(hole);
  });
  const geo = new THREE.ExtrudeGeometry(shape, {
    depth: PLANK_DEPTH, bevelEnabled: true, bevelThickness: 0.03, bevelSize: 0.03, bevelSegments: 2, curveSegments: 16,
  });
  const mat = new THREE.MeshStandardMaterial({
    color: PLANK_COLORS[p.id % PLANK_COLORS.length], roughness: 0.35, transparent: true, opacity: 0.86,
  });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  mesh.position.set(p.cx, p.cy, plankBase(p.id));
  mesh.rotation.z = p.angle;
  return mesh;
}

const plankBase = (id) => 0.05 + id * LAYER_GAP;

function makeBox(color) {
  const g = new THREE.Group();
  const base = slab(1.45, 0.55, 0.3, 0.3, SCREW_COLORS[color], { roughness: 0.45 });
  base.position.z = 0.05;
  g.add(base);
  BOX_SLOT_X.forEach((x) => {
    const h = new THREE.Mesh(holeGeo, holeMat);
    h.position.set(x, 0, UI_Z - 0.01);
    g.add(h);
  });
  return g;
}

// --- tweening ---
let tweens = [];
let clock = 0;
let lockUntil = 0;
const ease = (k) => 1 - Math.pow(1 - k, 3);
const easeBack = (k) => 1 + 2.7 * Math.pow(k - 1, 3) + 1.7 * Math.pow(k - 1, 2);
function tween(duration, delay, fn, done) {
  tweens.push({ t: -delay, duration, fn, done, started: false });
}
function updateTweens(dt) {
  const active = tweens;
  tweens = [];
  for (const tw of active) {
    tw.t += dt;
    if (tw.t < 0) { tweens.push(tw); continue; }
    if (!tw.started) { tw.started = true; tw.start && tw.start(); }
    const k = Math.min(1, tw.t / tw.duration);
    tw.fn(k);
    if (k >= 1) { if (tw.done) tw.done(); } else tweens.push(tw);
  }
}
function flyTo(mesh, target, delay, duration, done) {
  let from;
  let fromRot;
  tweens.push({
    t: -delay, duration, started: false,
    start: () => { from = mesh.position.clone(); fromRot = mesh.rotation.z; },
    fn: (k) => {
      const e = ease(k);
      mesh.position.lerpVectors(from, target, e);
      mesh.position.z += Math.sin(Math.PI * k) * 2.2;
      mesh.rotation.z = fromRot + e * Math.PI * 2;
    },
    done,
  });
}

// --- audio ---
let audio;
function beep(freq, dur = 0.08, type = 'sine', vol = 0.15) {
  try {
    audio = audio || new AudioContext();
    const o = audio.createOscillator();
    const gn = audio.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, audio.currentTime);
    o.frequency.exponentialRampToValueAtTime(freq * 1.6, audio.currentTime + dur);
    gn.gain.setValueAtTime(vol, audio.currentTime);
    gn.gain.exponentialRampToValueAtTime(0.001, audio.currentTime + dur);
    o.connect(gn).connect(audio.destination);
    o.start();
    o.stop(audio.currentTime + dur);
  } catch { /* audio unavailable */ }
}

// --- level state ---
let game;
let level = Number(localStorage.getItem('screw-level')) || 1;
let levelGroup;
let screwMeshes = [];
let plankMeshes = [];
let boxGroups = [null, null];

const $ = (id) => document.getElementById(id);
const overlay = $('overlay');

function disposeGroup(g) {
  g.traverse((o) => {
    if (o.geometry && ![headGeo, slotGeo, threadGeo, holeGeo].includes(o.geometry)) o.geometry.dispose();
  });
}

function addBox(bi, color, animate) {
  if (color === null) { boxGroups[bi] = null; return; }
  const g = makeBox(color);
  g.position.set(BOX_X[bi], BOX_Y, 0);
  levelGroup.add(g);
  boxGroups[bi] = g;
  if (animate) {
    g.scale.setScalar(0.001);
    return g;
  }
  return g;
}

function loadLevel(n) {
  level = n;
  localStorage.setItem('screw-level', String(n));
  if (levelGroup) { scene.remove(levelGroup); disposeGroup(levelGroup); }
  tweens = [];
  lockUntil = 0;
  levelGroup = new THREE.Group();
  scene.add(levelGroup);
  game = new Game(generateLevel(n));
  plankMeshes = game.planks.map((p) => {
    const m = makePlank(p);
    levelGroup.add(m);
    return m;
  });
  screwMeshes = game.screws.map((s) => {
    const top = plankBase(s.plankId) + PLANK_DEPTH + 0.03;
    const m = makeScrew(s.color, top + 0.1);
    m.position.set(s.x, s.y, top);
    m.rotation.z = Math.random() * Math.PI;
    m.userData.id = s.id;
    levelGroup.add(m);
    return m;
  });
  game.boxes.forEach((b, i) => addBox(i, b ? b.color : null, false));
  overlay.classList.add('hidden');
  updateHud();
}

function updateHud() {
  $('level').textContent = `Level ${level}`;
  const left = game.screws.filter((s) => s.state !== 'box').length;
  $('left').textContent = `${left} screw${left === 1 ? '' : 's'} left`;
}

function showOverlay(title, msg, action, fn, delay) {
  setTimeout(() => {
    $('title').textContent = title;
    $('msg').textContent = msg;
    $('action').textContent = action;
    $('action').onclick = fn;
    overlay.classList.remove('hidden');
  }, delay * 1000);
}

function shake(mesh) {
  const x0 = mesh.position.x;
  tween(0.35, 0, (k) => { mesh.position.x = x0 + Math.sin(k * Math.PI * 6) * 0.08 * (1 - k); }, () => { mesh.position.x = x0; });
}

function handleEvents(events) {
  const UNSCREW = 0.25;
  const FLY = 0.45;
  let arrival = 0;
  let boxReady = [0, 0];
  let endTime = 0;
  for (const ev of events) {
    const mesh = ev.screw !== undefined ? screwMeshes[ev.screw] : null;
    switch (ev.type) {
      case 'blocked':
        shake(mesh);
        beep(140, 0.12, 'square', 0.06);
        break;
      case 'unscrew': {
        const z0 = mesh.position.z;
        const r0 = mesh.rotation.z;
        tween(UNSCREW, 0, (k) => { mesh.position.z = z0 + ease(k) * 0.9; mesh.rotation.z = r0 + k * Math.PI * 4; });
        beep(520, 0.09, 'triangle');
        break;
      }
      case 'toBox':
      case 'trayToBox': {
        const delay = ev.type === 'toBox' ? UNSCREW : boxReady[ev.box];
        const target = new THREE.Vector3(BOX_X[ev.box] + BOX_SLOT_X[ev.slot], BOX_Y, UI_Z);
        const g = boxGroups[ev.box];
        flyTo(mesh, target, delay, FLY, () => { g.attach(mesh); beep(880, 0.07); });
        arrival = Math.max(arrival, delay + FLY);
        break;
      }
      case 'toTray': {
        const target = new THREE.Vector3(TRAY_SLOT_X[ev.slot], TRAY_Y, UI_Z);
        flyTo(mesh, target, UNSCREW, FLY, () => { levelGroup.attach(mesh); beep(330, 0.07); });
        arrival = Math.max(arrival, UNSCREW + FLY);
        break;
      }
      case 'boxDone': {
        const g = boxGroups[ev.box];
        const delay = Math.max(arrival, boxReady[ev.box]) + 0.15;
        const y0 = g.position.y;
        tween(0.45, delay, (k) => {
          g.position.y = y0 + ease(k) * 4;
          g.scale.setScalar(1 - k * 0.6);
        }, () => { levelGroup.remove(g); disposeGroup(g); beep(660, 0.15, 'triangle'); beep(990, 0.2, 'sine', 0.1); });
        boxReady[ev.box] = delay + 0.45;
        endTime = Math.max(endTime, boxReady[ev.box]);
        break;
      }
      case 'boxNew': {
        const g = addBox(ev.box, ev.color, true);
        const delay = boxReady[ev.box];
        if (g) tween(0.35, delay, (k) => g.scale.setScalar(Math.max(0.001, easeBack(k))));
        boxReady[ev.box] = delay + 0.35;
        endTime = Math.max(endTime, boxReady[ev.box]);
        break;
      }
      case 'plankFall': {
        const m = plankMeshes[ev.plank];
        const vx = (Math.random() - 0.5) * 4;
        const w = (Math.random() - 0.5) * 6;
        let p0, r0;
        tweens.push({
          t: -UNSCREW, duration: 1.6, started: false,
          start: () => { p0 = m.position.clone(); r0 = m.rotation.clone(); },
          fn: (k) => {
            const t = k * 1.6;
            m.position.set(p0.x + vx * t, p0.y + 3 * t - 14 * t * t, p0.z + 3 * t);
            m.rotation.z = r0.z + w * t;
            m.rotation.x = r0.x + w * 0.3 * t;
            m.material.opacity = 0.86 * (1 - k * 0.5);
          },
          done: () => { levelGroup.remove(m); m.geometry.dispose(); },
        });
        setTimeout(() => beep(200, 0.2, 'sawtooth', 0.05), UNSCREW * 1000);
        break;
      }
      case 'win':
        showOverlay('Level Complete!', `All screws sorted on level ${level}.`, 'Next Level', () => loadLevel(level + 1), Math.max(endTime, arrival) + 0.6);
        break;
      case 'lose':
        showOverlay('Tray Full!', 'No room left for more screws.', 'Try Again', () => loadLevel(level), Math.max(endTime, arrival) + 0.6);
        break;
    }
  }
  lockUntil = clock + endTime;
  updateHud();
}

// --- input ---
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
let downPos = null;

function pickScrew(ev) {
  const rect = canvas.getBoundingClientRect();
  pointer.set(((ev.clientX - rect.left) / rect.width) * 2 - 1, -((ev.clientY - rect.top) / rect.height) * 2 + 1);
  raycaster.setFromCamera(pointer, camera);
  const candidates = screwMeshes.filter((m, i) => game.screws[i].state === 'board');
  const hits = raycaster.intersectObjects(candidates, true);
  for (const h of hits) {
    let o = h.object;
    while (o && o.userData.id === undefined) o = o.parent;
    if (o) return o.userData.id;
  }
  return null;
}

canvas.addEventListener('pointerdown', (e) => { downPos = [e.clientX, e.clientY]; });
canvas.addEventListener('pointerup', (e) => {
  if (!downPos || Math.hypot(e.clientX - downPos[0], e.clientY - downPos[1]) > 8) return;
  downPos = null;
  if (clock < lockUntil || game.status !== 'playing') return;
  const id = pickScrew(e);
  if (id === null) return;
  const events = game.click(id);
  if (events) handleEvents(events);
  $('hint').style.opacity = '0';
});
canvas.addEventListener('pointermove', (e) => {
  if (e.buttons) return;
  const id = pickScrew(e);
  canvas.style.cursor = id !== null && game.isAccessible(game.screws[id]) ? 'pointer' : 'default';
});
$('restart').addEventListener('click', () => loadLevel(level));

// --- resize & loop ---
function resize() {
  const w = window.innerWidth, h = window.innerHeight;
  renderer.setSize(w, h, false);
  camera.aspect = w / h;
  const fit = 2 * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2));
  const dist = Math.max(15.5 / fit, 11.5 / (fit * camera.aspect));
  camera.position.set(0, -2.5, dist).add(new THREE.Vector3(0, 1.4, 0));
  controls.minDistance = dist * 0.7;
  controls.maxDistance = dist * 1.3;
  camera.updateProjectionMatrix();
  controls.update();
}
window.addEventListener('resize', resize);
resize();
loadLevel(level);

let last = performance.now();
renderer.setAnimationLoop((now) => {
  const dt = Math.min(0.05, (now - last) / 1000);
  last = now;
  clock += dt;
  updateTweens(dt);
  controls.update();
  renderer.render(scene, camera);
});

window.screwGame = {
  get game() { return game; },
  screenPos(id) {
    const v = new THREE.Vector3();
    screwMeshes[id].getWorldPosition(v);
    v.z += 0.1;
    v.project(camera);
    return [(v.x + 1) / 2 * window.innerWidth, (1 - v.y) / 2 * window.innerHeight];
  },
};
