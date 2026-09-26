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
        public float MoveSeconds = 0.3f;
        public bool ReducedMotion;
        /// <summary>Camera-attached parent for the trays and buffer so they stay fixed on screen while the object orbits.</summary>
        public Transform TrayAnchor;

        private const float HoldScrewScale = 1.15f;
        private const float TraySpacing = 1.85f;
        private const float TrayRowZ = 0.55f;
        private const float BufferRowZ = -1.05f;
        private const float BufferSpacing = 0.62f;
        private const float HoldingDepth = 6f;
        private const float TrayTop = 0.36f;
        private static readonly Vector2[] TrayHoles = { new Vector2(0f, 0.27f), new Vector2(-0.3f, -0.22f), new Vector2(0.3f, -0.22f) };

        private readonly Dictionary<string, ScrewView> _screws = new Dictionary<string, ScrewView>();
        private readonly Dictionary<string, PartView> _parts = new Dictionary<string, PartView>();
        private readonly List<Transform> _trays = new List<Transform>();
        private readonly List<Renderer> _trayRenderers = new List<Renderer>();
        private readonly List<Renderer> _handleRenderers = new List<Renderer>();
        private readonly List<Vector3> _bufferPos = new List<Vector3>();
        private Transform _objectRoot;
        private Transform _holdRoot;
        private CompiledLevel _level;
        private Material _litTemplate;
        private int _animating;
        private PuzzleState _lastSettled;

        public bool IsAnimating => _animating > 0;
        public IReadOnlyDictionary<string, ScrewView> Screws => _screws;
        public Bounds ObjectBounds { get; private set; }

        public void Build(CompiledLevel level, Material litTemplate)
        {
            Clear();
            _level = level;
            _litTemplate = litTemplate;
            _objectRoot = new GameObject("Object").transform;
            _objectRoot.SetParent(transform, false);
            _holdRoot = new GameObject("Holding").transform;
            _holdRoot.SetParent(TrayAnchor != null ? TrayAnchor : transform, false);

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var p in level.Definition.Parts)
            {
                var size = V(p.Size);
                float minDim = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                var color = Palette.PartMaterial[Mathf.Abs(p.Material) % Palette.PartMaterial.Length];
                bool cylinder = p.Shape == "cylinder";
                Mesh mesh = cylinder
                    ? MeshKit.Cylinder(Mathf.Max(size.x, size.z) * 0.5f, size.y, 40, -1f, Mathf.Min(0.12f, minDim * 0.3f))
                    : p.Shape == "sphere"
                        ? MeshKit.Sphere(size.x * 0.5f)
                        : MeshKit.RoundedBox(size, Mathf.Min(0.16f, minDim * 0.42f));
                var go = MeshKit.Make("Part " + p.Id, mesh, Mat(color, 0.8f, 0f), _objectRoot);
                AddOutline(go, mesh, Color.Lerp(color, Palette.Outline, 0.62f), 0.05f);
                go.transform.localPosition = V(p.Position);
                go.transform.localEulerAngles = V(p.Rotation);
                if (cylinder)
                {
                    var holder = new GameObject("Part " + p.Id);
                    holder.transform.SetParent(_objectRoot, false);
                    holder.transform.localPosition = V(p.Position);
                    holder.transform.localEulerAngles = V(p.Rotation);
                    go.name = "Mesh";
                    go.transform.SetParent(holder.transform, false);
                    go.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
                    go.transform.localRotation = Quaternion.identity;
                    go = holder;
                }
                Collider col;
                if (p.Shape == "sphere")
                {
                    var sc = go.AddComponent<SphereCollider>();
                    sc.radius = size.x * 0.5f;
                    col = sc;
                }
                else if (cylinder)
                {
                    var cc = go.AddComponent<BoxCollider>();
                    cc.size = size;
                    col = cc;
                }
                else
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.size = size;
                    col = bc;
                }
                var pv = go.AddComponent<PartView>();
                pv.PartId = p.Id;
                pv.Collider = col;
                _parts[p.Id] = pv;

                var rot = Quaternion.Euler(V(p.Rotation));
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3((c & 1) == 0 ? -0.5f : 0.5f, (c & 2) == 0 ? -0.5f : 0.5f, (c & 4) == 0 ? -0.5f : 0.5f);
                    var wp = V(p.Position) + rot * Vector3.Scale(corner, size);
                    if (first) { bounds = new Bounds(wp, Vector3.zero); first = false; }
                    else bounds.Encapsulate(wp);
                }
            }
            bounds.Expand(0.3f);
            ObjectBounds = bounds;

            var holeMat = Mat(Palette.Hole, 0.1f, 0f);
            foreach (var s in level.Definition.Screws)
            {
                level.TryGetScrew(s.Id, out var si);
                var partDef = level.Definition.Parts[level.ScrewPart[si]];
                var partView = _parts[partDef.Id];
                var hole = MeshKit.Make("Hole " + s.Id, MeshKit.Disc(0.12f, 20), holeMat, partView.transform, false);
                var partPos = V(partDef.Position);
                var normalLocal = V(s.Normal).normalized;
                hole.transform.localPosition = V(s.Position) - partPos + normalLocal * 0.004f;
                hole.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normalLocal);

                var sv = MakeScrew(s.Id, s.Color, _objectRoot);
                _screws[s.Id] = sv;
                ResetScrewOnPart(si, sv);
            }

            BuildHolding(level);
        }

        private ScrewView MakeScrew(string id, int color, Transform parent)
        {
            var root = new GameObject("Screw " + id);
            root.transform.SetParent(parent, false);
            var baseColor = Palette.ScrewColor(color);
            var paint = Mat(baseColor, 0.88f, 0f);
            var steel = Mat(Palette.Steel, 0.8f, 0.9f);
            var cross = Mat(Color.Lerp(baseColor, Color.white, 0.75f), 0.6f, 0f);
            MeshKit.Make("Washer", MeshKit.Cylinder(0.21f, 0.03f, 32, 0.21f, 0.012f), steel, root.transform);
            var head = MeshKit.Make("Head", MeshKit.Cylinder(0.18f, 0.09f, 32, 0.15f, 0.04f), paint, root.transform);
            head.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            var slotMesh = MeshKit.RoundedBox(new Vector3(0.2f, 0.03f, 0.05f), 0.015f, 2);
            var s1 = MeshKit.Make("Slot", slotMesh, cross, root.transform, false);
            s1.transform.localPosition = new Vector3(0f, 0.115f, 0f);
            var s2 = MeshKit.Make("Slot", slotMesh, cross, root.transform, false);
            s2.transform.localPosition = new Vector3(0f, 0.115f, 0f);
            s2.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var shaft = MeshKit.Make("Shaft", MeshKit.Cylinder(0.025f, 0.42f, 12, 0.055f), steel, root.transform);
            shaft.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            var thread = MeshKit.Cylinder(0.072f, 0.022f, 14, 0.072f, 0.01f);
            for (int i = 0; i < 4; i++)
            {
                var t = MeshKit.Make("Thread", thread, steel, root.transform, false);
                t.transform.localPosition = new Vector3(0f, -0.08f - i * 0.085f, 0f);
                t.transform.localScale = new Vector3(1f - i * 0.1f, 1f, 1f - i * 0.1f);
            }
            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.24f;
            col.center = new Vector3(0f, 0.05f, 0f);
            var sv = root.AddComponent<ScrewView>();
            sv.ScrewId = id;
            sv.Color = color;
            sv.Collider = col;
            return sv;
        }

        private void BuildHolding(CompiledLevel level)
        {
            var holeMat = Mat(Palette.Hole, 0.2f, 0f);
            var faceMat = Mat(Palette.TrayFace, 0.5f, 0f);
            var boxMesh = MeshKit.RoundedBox(new Vector3(1.55f, 0.6f, 1.45f), 0.22f);
            var handleMesh = MeshKit.RoundedBox(new Vector3(0.62f, 0.34f, 0.3f), 0.1f);
            var faceMesh = MeshKit.RoundedBox(new Vector3(1.2f, 0.06f, 1.1f), 0.18f);
            float shelfW = level.TrayPositions * TraySpacing + 0.35f;
            var shelfMesh = MeshKit.RoundedBox(new Vector3(shelfW, 0.22f, 2.05f), 0.2f);
            var shelf = MeshKit.Make("Shelf", shelfMesh, Mat(Palette.Shelf, 0.7f, 0f), _holdRoot, false);
            shelf.transform.localPosition = new Vector3(0f, -0.42f, TrayRowZ + 0.05f);
            AddOutline(shelf, shelfMesh, Palette.Outline, 0.05f);
            float barW = level.BufferSlots * BufferSpacing + 0.3f;
            var barMesh = MeshKit.RoundedBox(new Vector3(barW, 0.16f, 0.78f), 0.2f);
            var bar = MeshKit.Make("BufferBar", barMesh, Mat(Palette.BufferBar, 0.7f, 0f), _holdRoot, false);
            bar.transform.localPosition = new Vector3(0f, -0.24f, BufferRowZ);
            AddOutline(bar, barMesh, Palette.Outline, 0.05f);
            for (int t = 0; t < level.TrayPositions; t++)
            {
                var tray = MeshKit.Make("Tray " + t, boxMesh, Mat(Palette.Slot, 0.75f, 0f), _holdRoot);
                AddOutline(tray, boxMesh, Palette.Outline, 0.045f);
                tray.transform.localPosition = TrayHome(t);
                var handle = MeshKit.Make("Handle", handleMesh, tray.GetComponent<Renderer>().sharedMaterial, tray.transform);
                handle.transform.localPosition = new Vector3(0f, 0.12f, 0.78f);
                var face = MeshKit.Make("Face", faceMesh, faceMat, tray.transform, false);
                face.transform.localPosition = new Vector3(0f, 0.3f, -0.02f);
                foreach (var h2 in TrayHoles)
                {
                    var h = MeshKit.Make("Hole", MeshKit.Disc(0.19f), holeMat, tray.transform, false);
                    h.transform.localPosition = new Vector3(h2.x, TrayTop - 0.025f, h2.y);
                }
                _trayRenderers.Add(tray.GetComponent<Renderer>());
                _handleRenderers.Add(handle.GetComponent<Renderer>());
                _trays.Add(tray.transform);
            }
            var socketMat = Mat(Palette.Socket, 0.5f, 0f);
            var socketInner = Mat(Palette.SlotInner, 0.3f, 0f);
            var socketMesh = MeshKit.Cylinder(0.24f, 0.08f, 32, 0.24f, 0.035f);
            for (int b = 0; b < level.BufferSlots; b++)
            {
                var pos = new Vector3((b - (level.BufferSlots - 1) * 0.5f) * BufferSpacing, 0f, BufferRowZ);
                var sock = MeshKit.Make("Socket", socketMesh, socketMat, _holdRoot);
                sock.transform.localPosition = pos - new Vector3(0f, 0.12f, 0f);
                var inner = MeshKit.Make("Hole", MeshKit.Disc(0.17f), socketInner, _holdRoot, false);
                inner.transform.localPosition = pos + new Vector3(0f, 0.004f, 0f);
                _bufferPos.Add(pos);
            }
        }

        private Vector3 TrayHome(int t) => new Vector3((t - (_level.TrayPositions - 1) * 0.5f) * TraySpacing, 0f, TrayRowZ);

        /// <summary>Places the holding area between two viewport heights, scaled to fit the screen width.</summary>
        public void LayoutHolding(Camera cam, float viewportBottom, float viewportTop)
        {
            if (_holdRoot == null || cam == null) return;
            float mid = (viewportBottom + viewportTop) * 0.5f;
            var center = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(0.5f, mid, HoldingDepth)));
            var left = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(0f, mid, HoldingDepth)));
            var top = cam.transform.InverseTransformPoint(cam.ViewportToWorldPoint(new Vector3(0.5f, viewportTop, HoldingDepth)));
            float halfW = Mathf.Abs(center.x - left.x);
            float halfH = Mathf.Abs(top.y - center.y);
            float layoutHalfW = Mathf.Max(_level.TrayPositions * TraySpacing * 0.5f, _bufferPos.Count * BufferSpacing * 0.5f) + 0.15f;
            const float layoutHalfH = 1.55f;
            float scale = Mathf.Min(halfW * 0.8f / layoutHalfW, halfH * 0.95f / layoutHalfH);
            _holdRoot.localPosition = center + new Vector3(0f, halfH * 0.02f, 0f);
            _holdRoot.localRotation = Quaternion.Euler(-62f, 0f, 0f);
            _holdRoot.localScale = Vector3.one * scale;
        }

        public void Clear()
        {
            StopAllCoroutines();
            _animating = 0;
            _screws.Clear();
            _parts.Clear();
            _trays.Clear();
            _bufferPos.Clear();
            _trayRenderers.Clear();
            _handleRenderers.Clear();
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
            for (int t = 0; t < _trays.Count; t++)
            {
                _trays[t].localPosition = TrayHome(t);
                _trays[t].localScale = Vector3.one;
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
                    sv.transform.SetParent(_holdRoot, false);
                    sv.gameObject.SetActive(false);
                }
                else
                {
                    sv.Collider.enabled = false;
                    sv.gameObject.SetActive(true);
                    if (place == ScrewPlace.Tray) PlaceInTray(sv.transform, st.Slot[s], IndexInTray(st, s));
                    else
                    {
                        sv.transform.SetParent(_holdRoot, false);
                        sv.transform.localPosition = _bufferPos[st.Slot[s]];
                    }
                    sv.transform.localRotation = Quaternion.identity;
                    sv.transform.localScale = Vector3.one * HoldScrewScale;
                }
            }
            RefreshTrays(st);
        }

        private void PlaceInTray(Transform screw, int tray, int index)
        {
            screw.SetParent(_trays[tray], false);
            screw.localPosition = TrayHolePos(index);
        }

        private static Vector3 TrayHolePos(int index)
        {
            var h = TrayHoles[Mathf.Clamp(index, 0, TrayHoles.Length - 1)];
            return new Vector3(h.x, TrayTop - 0.03f, h.y);
        }

        private void ResetScrewOnPart(int s, ScrewView sv)
        {
            var def = _level.Definition.Screws[s];
            var partDef = _level.Definition.Parts[_level.ScrewPart[s]];
            var partPos = V(partDef.Position);
            var partRot = Quaternion.Euler(V(partDef.Rotation));
            var normal = partRot * V(def.Normal).normalized;
            sv.transform.SetParent(_objectRoot, false);
            sv.transform.localPosition = partPos + partRot * (V(def.Position) - partPos) + normal * 0.005f;
            sv.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            sv.transform.localScale = Vector3.one;
        }

        private int IndexInTray(PuzzleState st, int screw)
        {
            int i = 0;
            for (int s = 0; s < screw; s++) if (st.Place[s] == ScrewPlace.Tray && st.Slot[s] == st.Slot[screw]) i++;
            return i;
        }

        private static int ScrewsIn(Transform tray)
        {
            int n = 0;
            foreach (Transform c in tray) if (c.gameObject.activeSelf && c.GetComponent<ScrewView>() != null) n++;
            return n;
        }

        private void RefreshTrays(PuzzleState st)
        {
            for (int t = 0; t < _trayRenderers.Count; t++) SetTrayColor(t, st.TrayColor[t]);
        }

        private void SetTrayColor(int t, int color)
        {
            var c = color >= 0 ? Palette.ScrewColor(color) : Palette.Slot;
            c.a = 1f;
            var m = Mat(c, 0.75f, 0f);
            _trayRenderers[t].sharedMaterial = m;
            _handleRenderers[t].sharedMaterial = m;
        }

        /// <summary>Animates the ordered events of one accepted command, then snaps to the settled state.</summary>
        public void Play(IReadOnlyList<VisualEvent> events, PuzzleState settled)
        {
            if (_animating > 0 && _lastSettled != null) Rebuild(_lastSettled);
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
                    yield return Unscrew(sv.transform, MoveSeconds * 0.7f);
                    yield return FlyTo(sv.transform, r.Destination == DestinationKind.Tray ? _trays[r.DestinationIndex] : _holdRoot,
                        r.Destination == DestinationKind.Tray ? TrayHolePos(ScrewsIn(_trays[r.DestinationIndex])) : _bufferPos[r.DestinationIndex], MoveSeconds);
                }
                else if (e is PartReleasedEvent pr && _parts.TryGetValue(pr.PartId, out var pv))
                {
                    StartCoroutine(Fall(pv.transform));
                }
                else if (e is TrayCompletedEvent tc)
                {
                    yield return new WaitForSeconds(0.08f);
                    yield return TrayLeave(tc.TrayIndex);
                }
                else if (e is TrayRefilledEvent tr)
                {
                    yield return TrayArrive(tr.TrayIndex, tr.Color);
                }
                else if (e is BufferTransferredEvent bt && _screws.TryGetValue(bt.ScrewId, out var bsv))
                {
                    yield return FlyTo(bsv.transform, _trays[bt.ToTray], TrayHolePos(ScrewsIn(_trays[bt.ToTray])), MoveSeconds * 0.8f);
                }
            }
            _animating = 0;
            Rebuild(settled);
        }

        private IEnumerator Unscrew(Transform t, float seconds)
        {
            var start = t.localPosition;
            var up = t.localRotation * Vector3.up;
            var rot = t.localRotation;
            for (float k = 0f; k < 1f;)
            {
                k = Mathf.Min(1f, k + Time.deltaTime / seconds);
                t.localPosition = start + up * (0.55f * Ease(k));
                t.localRotation = rot * Quaternion.Euler(0f, -900f * k, 0f);
                yield return null;
            }
        }

        private IEnumerator FlyTo(Transform t, Transform parent, Vector3 localTarget, float seconds)
        {
            t.SetParent(parent, true);
            var p0 = t.localPosition;
            var r0 = t.localRotation;
            var s0 = t.localScale;
            var s1 = Vector3.one * HoldScrewScale;
            var lift = Vector3.up * 1.2f;
            for (float k = 0f; k < 1f;)
            {
                k = Mathf.Min(1f, k + Time.deltaTime / seconds);
                float e = Ease(k);
                t.localPosition = Vector3.LerpUnclamped(p0, localTarget, e) + lift * Mathf.Sin(k * Mathf.PI) * 0.6f;
                t.localRotation = Quaternion.Slerp(r0, Quaternion.identity, e);
                t.localScale = Vector3.LerpUnclamped(s0, s1, e);
                yield return null;
            }
            yield return Punch(t, s1, 0.12f);
        }

        private IEnumerator Punch(Transform t, Vector3 baseScale, float seconds)
        {
            for (float k = 0f; k < 1f;)
            {
                k = Mathf.Min(1f, k + Time.deltaTime / seconds);
                t.localScale = baseScale * (1f + 0.18f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            t.localScale = baseScale;
        }

        private IEnumerator TrayLeave(int tray)
        {
            var t = _trays[tray];
            yield return Punch(t, Vector3.one, 0.14f);
            var p0 = t.localPosition;
            for (float k = 0f; k < 1f;)
            {
                k = Mathf.Min(1f, k + Time.deltaTime / (MoveSeconds * 1.1f));
                float e = k * k;
                t.localPosition = p0 + new Vector3(0f, 0.6f * e, 4.5f * e);
                t.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, e);
                yield return null;
            }
            var done = new List<Transform>();
            foreach (Transform c in t) if (c.GetComponent<ScrewView>() != null) done.Add(c);
            foreach (var c in done) { c.SetParent(_holdRoot, false); c.gameObject.SetActive(false); }
            t.localScale = Vector3.zero;
            t.localPosition = TrayHome(tray);
        }

        private IEnumerator TrayArrive(int tray, int color)
        {
            var t = _trays[tray];
            SetTrayColor(tray, color);
            t.localPosition = TrayHome(tray);
            for (float k = 0f; k < 1f;)
            {
                k = Mathf.Min(1f, k + Time.deltaTime / (MoveSeconds * 0.9f));
                t.localScale = Vector3.one * EaseBack(k);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator Fall(Transform part)
        {
            var start = part.position;
            var spin = new Vector3(Random.Range(-90f, 90f), 0f, Random.Range(-120f, 120f));
            var drift = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            float v = 3.5f, y = 0f, time = 0f;
            while (y > -12f && part != null)
            {
                time += Time.deltaTime;
                v -= 20f * Time.deltaTime;
                y += v * Time.deltaTime;
                part.position = start + new Vector3(0f, y, 0f) + drift * time;
                part.Rotate(spin * Time.deltaTime, Space.Self);
                yield return null;
            }
            if (part != null) part.gameObject.SetActive(false);
        }

        private static float Ease(float k) => 1f - Mathf.Pow(1f - k, 3f);
        private static float EaseBack(float k) => 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f);

        private readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        /// <summary>Inverted-hull cartoon outline: a slightly enlarged, front-culled, dark copy of the mesh.</summary>
        private void AddOutline(GameObject go, Mesh mesh, Color color, float width)
        {
            var key = "outline" + ColorUtility.ToHtmlStringRGB(color);
            if (!_mats.TryGetValue(key, out var m))
            {
                m = new Material(_litTemplate) { color = color };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", color * 0.6f);
                }
                _mats[key] = m;
            }
            var b = mesh.bounds;
            var scale = new Vector3(
                (b.size.x + 2f * width) / Mathf.Max(0.01f, b.size.x),
                (b.size.y + 2f * width) / Mathf.Max(0.01f, b.size.y),
                (b.size.z + 2f * width) / Mathf.Max(0.01f, b.size.z));
            var o = MeshKit.Make("Outline", mesh, m, go.transform, false);
            o.transform.localScale = scale;
            o.transform.localPosition = b.center - Vector3.Scale(b.center, scale);
        }

        public void SetHoldingVisible(bool visible)
        {
            if (_holdRoot != null) _holdRoot.gameObject.SetActive(visible);
        }

        private Material Mat(Color c, float smoothness, float metallic)
        {
            var key = ColorUtility.ToHtmlStringRGBA(c) + smoothness.ToString("F2") + metallic.ToString("F2");
            if (_mats.TryGetValue(key, out var m)) return m;
            m = new Material(_litTemplate) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            _mats[key] = m;
            return m;
        }

        public bool ShowSymbols;
        private Transform _highlight;

        /// <summary>Marks a hinted screw with a pulsing ring; null clears it.</summary>
        public void Highlight(string screwId)
        {
            if (_highlight != null) Destroy(_highlight.gameObject);
            _highlight = null;
            if (screwId == null || !_screws.TryGetValue(screwId, out var sv) || !sv.gameObject.activeInHierarchy) return;
            var ring = MeshKit.Make("HintRing", MeshKit.Cylinder(0.3f, 0.02f, 32), Mat(new Color(1f, 1f, 0.85f), 0.9f, 0f), sv.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            _highlight = ring.transform;
        }

        private void LateUpdate()
        {
            if (_highlight != null) _highlight.localScale = new Vector3(1f, 1f, 1f) * (1f + 0.2f * Mathf.Sin(Time.time * 7f));
        }

        public ScrewView Find(string id) => _screws.TryGetValue(id, out var v) ? v : null;

        public Transform ObjectRoot => _objectRoot;

        private static Vector3 V(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;
    }
}
