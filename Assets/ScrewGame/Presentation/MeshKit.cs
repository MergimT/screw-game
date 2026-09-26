using System.Collections.Generic;
using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>Procedural meshes (rounded boxes, cylinders, discs) built in real units so bevels never stretch.</summary>
    public static class MeshKit
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        /// <summary>Box with rounded edges: a subdivided cube whose vertices are pushed onto the rounded surface.</summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int segments = 6)
        {
            var key = $"rb{size.x:F3}_{size.y:F3}_{size.z:F3}_{radius:F3}_{segments}";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var half = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.98f);
            var inner = half - Vector3.one * radius;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            int n = segments * 2 + 1;
            Vector3[] faceN = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var fn in faceN)
            {
                var u = fn.y != 0f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(fn, u);
                int start = verts.Count;
                for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * 2f - 1f, b = j / (float)n * 2f - 1f;
                    var p = Vector3.Scale(fn + u * a + v * b, half);
                    var c = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var d = p - c;
                    var nrm = d.sqrMagnitude > 1e-8f ? d.normalized : fn;
                    verts.Add(c + nrm * radius);
                    normals.Add(nrm);
                }
                for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int k = start + j * (n + 1) + i;
                    tris.Add(k); tris.Add(k + 1); tris.Add(k + n + 1);
                    tris.Add(k + 1); tris.Add(k + n + 2); tris.Add(k + n + 1);
                }
            }
            return Store(key, verts, normals, tris);
        }

        /// <summary>Capped cylinder along +Y from 0 to height, optional taper and rounded top rim.</summary>
        public static Mesh Cylinder(float radius, float height, int sides = 28, float topRadius = -1f, float bevel = 0f)
        {
            if (topRadius < 0f) topRadius = radius;
            var key = $"cy{radius:F3}_{height:F3}_{sides}_{topRadius:F3}_{bevel:F3}";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            // Profile rings: (radius, y, normal) from bottom to top.
            var profile = new List<(float r, float y, Vector2 n)>
            {
                (radius, 0f, new Vector2(1f, 0f)),
                (topRadius, height - bevel, new Vector2(1f, 0f)),
            };
            if (bevel > 0f)
            {
                for (int i = 1; i <= 4; i++)
                {
                    float a = i / 4f * Mathf.PI * 0.5f;
                    profile.Add((topRadius - bevel + Mathf.Cos(a) * bevel, height - bevel + Mathf.Sin(a) * bevel, new Vector2(Mathf.Cos(a), Mathf.Sin(a))));
                }
            }
            for (int k = 0; k < profile.Count - 1; k++)
            {
                int start = verts.Count;
                for (int s = 0; s <= sides; s++)
                {
                    float ang = s / (float)sides * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    for (int e = 0; e < 2; e++)
                    {
                        var pr = profile[k + e];
                        verts.Add(dir * pr.r + Vector3.up * pr.y);
                        normals.Add((dir * pr.n.x + Vector3.up * pr.n.y).normalized);
                    }
                }
                for (int s = 0; s < sides; s++)
                {
                    int a = start + s * 2;
                    tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                    tris.Add(a + 2); tris.Add(a + 1); tris.Add(a + 3);
                }
            }
            var top = profile[profile.Count - 1];
            Cap(verts, normals, tris, top.r, top.y, sides, true);
            Cap(verts, normals, tris, radius, 0f, sides, false);
            return Store(key, verts, normals, tris);
        }

        public static Mesh Disc(float radius, int sides = 28)
        {
            var key = $"disc{radius:F3}_{sides}";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            Cap(verts, normals, tris, radius, 0f, sides, true);
            return Store(key, verts, normals, tris);
        }

        private static void Cap(List<Vector3> verts, List<Vector3> normals, List<int> tris, float r, float y, int sides, bool up)
        {
            int c = verts.Count;
            var n = up ? Vector3.up : Vector3.down;
            verts.Add(new Vector3(0f, y, 0f));
            normals.Add(n);
            for (int s = 0; s <= sides; s++)
            {
                float ang = s / (float)sides * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r));
                normals.Add(n);
            }
            for (int s = 0; s < sides; s++)
            {
                if (up) { tris.Add(c); tris.Add(c + s + 2); tris.Add(c + s + 1); }
                else { tris.Add(c); tris.Add(c + s + 1); tris.Add(c + s + 2); }
            }
        }

        private static Mesh Store(string key, List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            var m = new Mesh { name = key };
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            Cache[key] = m;
            return m;
        }

        public static GameObject Make(string name, Mesh mesh, Material mat, Transform parent, bool shadows = true)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
            return go;
        }
    }
}
