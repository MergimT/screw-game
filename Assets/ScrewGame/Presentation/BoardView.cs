using System.Collections;
using System.Collections.Generic;
using ScrewGame.Contracts;
using ScrewGame.Core;
using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>
    /// Builds the 3D object from a LevelDefinition and mirrors authoritative state. Logical state is committed before events
    /// arrive; animations are cosmetic, and Rebuild() always reconstructs the settled view from PuzzleState.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public float MoveSeconds = 0.28f;
        public bool ReducedMotion;
        public Transform TrayAnchor;

        private readonly Dictionary<string, ScrewView> _screws = new Dictionary<string, ScrewView>();
        private readonly Dictionary<string, PartView> _parts = new Dictionary<string, PartView>();
        private readonly List<Transform> _traySlots = new List<Transform>();
        private readonly List<Transform> _bufferSlots = new List<Transform>();
        private readonly List<Renderer> _trayRenderers = new List<Renderer>();
        private Transform _objectRoot;
        private Transform _holdRoot;
        private CompiledLevel _level;
        private Material _litTemplate;
        private int _animating;
        private PuzzleState _lastSettled;

        public bool IsAnimating => _animating > 0;
        public IReadOnlyDictionary<string, ScrewView> Screws => _screws;

        public void Build(CompiledLevel level, Material litTemplate)
        {
            Clear();
            _level = level;
            _litTemplate = litTemplate;
            _objectRoot = new GameObject("Object").transform;
            _objectRoot.SetParent(transform, false);
            _holdRoot = new GameObject("Holding").transform;
            _holdRoot.SetParent(TrayAnchor != null ? TrayAnchor : transform, false);

            foreach (var p in level.Definition.Parts)
            {
                var go = GameObject.CreatePrimitive(p.Shape == "cylinder" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                go.name = "Part " + p.Id;
                go.transform.SetParent(_objectRoot, false);
                go.transform.localPosition = V(p.Position);
                go.transform.localEulerAngles = V(p.Rotation);
                go.transform.localScale = V(p.Size);
                go.GetComponent<Renderer>().sharedMaterial = Mat(Palette.PartMaterial[Mathf.Abs(p.Material) % Palette.PartMaterial.Length]);
                var pv = go.AddComponent<PartView>();
                pv.PartId = p.Id;
                pv.Collider = go.GetComponent<Collider>();
                _parts[p.Id] = pv;
            }

            foreach (var s in level.Definition.Screws)
            {
                var root = new GameObject("Screw " + s.Id);
                root.transform.SetParent(_objectRoot, false);
                var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                head.name = "Head";
                head.transform.SetParent(root.transform, false);
                head.transform.localScale = new Vector3(0.34f, 0.05f, 0.34f);
                head.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                head.GetComponent<Renderer>().sharedMaterial = Mat(Palette.ScrewColor(s.Color));
                Object.Destroy(head.GetComponent<Collider>());

                var slotMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slotMark.name = "Slot";
                slotMark.transform.SetParent(root.transform, false);
                slotMark.transform.localScale = new Vector3(0.24f, 0.02f, 0.05f);
                slotMark.transform.localPosition = new Vector3(0f, 0.105f, 0f);
                slotMark.GetComponent<Renderer>().sharedMaterial = Mat(Palette.ScrewColor(s.Color) * 0.55f);
                Object.Destroy(slotMark.GetComponent<Collider>());

                var col = root.AddComponent<CapsuleCollider>();
                col.direction = 1;
                col.radius = 0.2f;
                col.height = 0.3f;
                col.center = new Vector3(0f, 0.08f, 0f);
                var sv = root.AddComponent<ScrewView>();
                sv.ScrewId = s.Id;
                sv.Color = s.Color;
                sv.Collider = col;
                _screws[s.Id] = sv;
                level.TryGetScrew(s.Id, out var si);
                ResetScrewOnPart(si, sv);
            }

            BuildHolding(level);
        }

        private void BuildHolding(CompiledLevel level)
        {
            for (int t = 0; t < level.TrayPositions; t++)
            {
                var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tray.name = "Tray " + t;
                Object.Destroy(tray.GetComponent<Collider>());
                tray.transform.SetParent(_holdRoot, false);
                tray.transform.localScale = new Vector3(1.5f, 0.12f, 0.6f);
                tray.transform.localPosition = new Vector3((t - (level.TrayPositions - 1) * 0.5f) * 1.9f, 0f, 0f);
                _trayRenderers.Add(tray.GetComponent<Renderer>());
                _traySlots.Add(tray.transform);
            }
            for (int b = 0; b < level.BufferSlots; b++)
            {
                var slot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                slot.name = "Buffer " + b;
                Object.Destroy(slot.GetComponent<Collider>());
                slot.transform.SetParent(_holdRoot, false);
                slot.transform.localScale = new Vector3(0.5f, 0.03f, 0.5f);
                slot.transform.localPosition = new Vector3((b - (level.BufferSlots - 1) * 0.5f) * 0.62f, 0f, -0.9f);
                slot.GetComponent<Renderer>().sharedMaterial = Mat(Palette.Slot);
                _bufferSlots.Add(slot.transform);
            }
        }

        public void Clear()
        {
            StopAllCoroutines();
            _animating = 0;
            _screws.Clear();
            _parts.Clear();
            _traySlots.Clear();
            _bufferSlots.Clear();
            _trayRenderers.Clear();
            if (_objectRoot != null) Destroy(_objectRoot.gameObject);
            if (_holdRoot != null) Destroy(_holdRoot.gameObject);
            _objectRoot = null;
            _holdRoot = null;
        }

        /// <summary>Snaps every view to the settled state. Safe after undo, restart, relaunch or interrupted animations.</summary>
        public void Rebuild(PuzzleState st)
        {
            _lastSettled = st.Clone();
            StopAllCoroutines();
            _animating = 0;
            for (int p = 0; p < _level.PartCount; p++)
            {
                var pv = _parts[_level.PartIds[p]];
                pv.gameObject.SetActive(!st.Released[p]);
                pv.transform.localPosition = V(_level.Definition.Parts[p].Position);
                pv.transform.localEulerAngles = V(_level.Definition.Parts[p].Rotation);
                pv.Collider.enabled = !st.Released[p];
            }
            for (int s = 0; s < _level.ScrewCount; s++)
            {
                var sv = _screws[_level.ScrewIds[s]];
                var place = st.Place[s];
                if (place == ScrewPlace.OnPart)
                {
                    ResetScrewOnPart(s, sv);
                    sv.gameObject.SetActive(true);
                    sv.Collider.enabled = true;
                }
                else if (place == ScrewPlace.Collected)
                {
                    sv.gameObject.SetActive(false);
                }
                else
                {
                    sv.Collider.enabled = false;
                    sv.gameObject.SetActive(true);
                    sv.transform.SetParent(_holdRoot, false);
                    sv.transform.localRotation = Quaternion.identity;
                    sv.transform.localScale = Vector3.one;
                    sv.transform.localPosition = place == ScrewPlace.Tray ? TrayPos(st.Slot[s], IndexInTray(st, s)) : BufferPos(st.Slot[s]);
                }
            }
            RefreshTrays(st);
        }

        private void ResetScrewOnPart(int s, ScrewView sv)
        {
            var def = _level.Definition.Screws[s];
            var partDef = _level.Definition.Parts[_level.ScrewPart[s]];
            var partPos = V(partDef.Position);
            var partRot = Quaternion.Euler(V(partDef.Rotation));
            var normal = partRot * V(def.Normal).normalized;
            sv.transform.SetParent(_objectRoot, false);
            sv.transform.localPosition = partPos + partRot * (V(def.Position) - partPos) + normal * 0.02f;
            sv.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            sv.transform.localScale = Vector3.one;
        }

        private int IndexInTray(PuzzleState st, int screw)
        {
            int i = 0;
            for (int s = 0; s < screw; s++) if (st.Place[s] == ScrewPlace.Tray && st.Slot[s] == st.Slot[screw]) i++;
            return i;
        }

        private Vector3 TrayPos(int tray, int index)
        {
            var c = _traySlots[tray].localPosition;
            return c + new Vector3((index - 1) * 0.45f, 0.1f, 0f);
        }

        private Vector3 BufferPos(int slot) => _bufferSlots[slot].localPosition + new Vector3(0f, 0.05f, 0f);

        private void RefreshTrays(PuzzleState st)
        {
            for (int t = 0; t < _trayRenderers.Count; t++)
            {
                var c = st.TrayColor[t] >= 0 ? Color.Lerp(Palette.ScrewColor(st.TrayColor[t]), Color.white, 0.55f) : Palette.Slot;
                _trayRenderers[t].sharedMaterial = Mat(c);
            }
        }

        /// <summary>Animates the ordered events of one accepted command, then snaps to the settled state.</summary>
        public void Play(IReadOnlyList<VisualEvent> events, PuzzleState settled)
        {
            if (_animating > 0 && _lastSettled != null) Rebuild(_lastSettled);
            // Released parts and the removed screw stop being pickable immediately.
            foreach (var e in events)
            {
                if (e is ScrewRemovedEvent r && _screws.TryGetValue(r.ScrewId, out var sv)) sv.Collider.enabled = false;
                if (e is PartReleasedEvent pr && _parts.TryGetValue(pr.PartId, out var pv)) pv.Collider.enabled = false;
            }
            if (ReducedMotion) { Rebuild(settled); return; }
            _lastSettled = settled.Clone();
            StartCoroutine(PlayRoutine(events, _lastSettled));
        }

        private IEnumerator PlayRoutine(IReadOnlyList<VisualEvent> events, PuzzleState settled)
        {
            _animating++;
            foreach (var e in events)
            {
                if (e is ScrewRemovedEvent r && _screws.TryGetValue(r.ScrewId, out var sv))
                {
                    var from = sv.transform.position;
                    var lift = from + sv.transform.up * 0.6f;
                    yield return Tween(sv.transform, from, lift, MoveSeconds * 0.6f);
                    sv.transform.SetParent(_holdRoot, true);
                    int s;
                    _level.TryGetScrew(r.ScrewId, out s);
                    var target = _holdRoot.TransformPoint(r.Destination == DestinationKind.Tray ? TrayPos(r.DestinationIndex, IndexInTray(settled, s)) : BufferPos(r.DestinationIndex));
                    yield return Tween(sv.transform, lift, target, MoveSeconds);
                }
                else if (e is PartReleasedEvent pr && _parts.TryGetValue(pr.PartId, out var pv))
                {
                    StartCoroutine(Fall(pv.transform));
                }
                else if (e is TrayCompletedEvent || e is BufferTransferredEvent || e is TrayRefilledEvent)
                {
                    yield return new WaitForSeconds(MoveSeconds * 0.5f);
                }
            }
            _animating = 0;
            Rebuild(settled);
        }

        private IEnumerator Tween(Transform t, Vector3 a, Vector3 b, float seconds)
        {
            float k = 0f;
            while (k < 1f)
            {
                k += Time.deltaTime / Mathf.Max(0.01f, seconds);
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f);
                t.position = Vector3.LerpUnclamped(a, b, e);
                yield return null;
            }
        }

        private IEnumerator Fall(Transform part)
        {
            var start = part.position;
            float v = 0f, y = 0f;
            while (y > -8f && part != null)
            {
                v -= 18f * Time.deltaTime;
                y += v * Time.deltaTime;
                part.position = start + new Vector3(0f, y, 0f);
                part.Rotate(40f * Time.deltaTime, 0f, 25f * Time.deltaTime, Space.Self);
                yield return null;
            }
            if (part != null) part.gameObject.SetActive(false);
        }

        private readonly Dictionary<Color, Material> _mats = new Dictionary<Color, Material>();

        private Material Mat(Color c)
        {
            if (_mats.TryGetValue(c, out var m)) return m;
            m = new Material(_litTemplate) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            _mats[c] = m;
            return m;
        }

        public bool ShowSymbols;
        private Transform _highlight;

        /// <summary>Marks a hinted screw with a floating ring; null clears it.</summary>
        public void Highlight(string screwId)
        {
            if (_highlight != null) Destroy(_highlight.gameObject);
            _highlight = null;
            if (screwId == null || !_screws.TryGetValue(screwId, out var sv) || !sv.gameObject.activeInHierarchy) return;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "HintRing";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(sv.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localScale = new Vector3(0.55f, 0.01f, 0.55f);
            ring.GetComponent<Renderer>().sharedMaterial = Mat(Color.white);
            _highlight = ring.transform;
        }

        private void LateUpdate()
        {
            if (_highlight != null) _highlight.localScale = new Vector3(1f, 0.02f, 1f) * (0.5f + 0.08f * Mathf.Sin(Time.time * 6f));
        }

        public ScrewView Find(string id) => _screws.TryGetValue(id, out var v) ? v : null;

        public Transform ObjectRoot => _objectRoot;

        private static Vector3 V(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;
    }
}
