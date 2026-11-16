using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReCraft
{
    /// <summary>
    /// 기본 도형. 모든 모서리를 살짝 둥글려(베벨) 빛을 받는 하이라이트가 생기게 한다.
    /// 면마다 "차트"(실제 크기를 가진 UV 조각)를 가지며,
    /// MeshGen.Combine이 모든 차트를 균일한 텍셀 밀도로 아틀라스에 배치한다 (마스크 페인팅용).
    /// </summary>
    public class Geo
    {
        public readonly List<Vector3> v = new();
        public readonly List<Vector3> n = new();
        public readonly List<Vector2> uv = new();      // 차트 내부 0..1
        public readonly List<int> chart = new();       // 정점이 속한 차트
        public readonly List<Vector2> chartSize = new(); // 차트의 실제 크기 (m)
        public readonly List<int> t = new();

        public const float MaxBevel = 0.004f;

        int NewChart(float w, float h)
        {
            chartSize.Add(new Vector2(Mathf.Max(w, 0.004f), Mathf.Max(h, 0.004f)));
            return chartSize.Count - 1;
        }

        int Add(Vector3 p, Vector3 nr, Vector2 u, int ch)
        {
            v.Add(p); n.Add(nr); uv.Add(u); chart.Add(ch);
            return v.Count - 1;
        }

        static readonly Vector3[] Dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        static readonly float[] BevelT = { 1f, 0.55f, 0.2f, 0f };

        /// <summary>한 축의 격자 좌표: 평평한 면 + 양쪽 베벨 구간.</summary>
        static List<float> Axis(float h, float r)
        {
            var c = new List<float>();
            if (r <= 1e-6f) { c.Add(-h); c.Add(h); return c; }
            float a = h - r;
            foreach (float t in BevelT) c.Add(-a - r * t);
            for (int i = BevelT.Length - 1; i >= 0; i--) c.Add(a + r * BevelT[i]);
            return c;
        }

        /// <summary>모서리가 둥근 상자 (bevel &lt; 0 이면 크기에 맞춰 자동).</summary>
        public static Geo Box(Vector3 size, float bevel = -1f)
        {
            var g = new Geo();
            Vector3 h = size * 0.5f;
            float minDim = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            float r = bevel >= 0f ? Mathf.Min(bevel, minDim * 0.45f) : Mathf.Min(minDim * 0.3f, MaxBevel);
            Vector3 inner = new(h.x - r, h.y - r, h.z - r);
            for (int i = 0; i < 6; i++)
            {
                Vector3 nr = Dirs[i];
                Vector3 u = Mathf.Abs(nr.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, nr);
                Vector3 w = Vector3.Cross(nr, u);
                float hn = Half(h, nr), hu = Half(h, u), hw = Half(h, w);
                var cu = Axis(hu, r);
                var cw = Axis(hw, r);
                int ch = g.NewChart(hu * 2f, hw * 2f);
                int start = g.v.Count;
                foreach (float b in cw)
                    foreach (float a in cu)
                    {
                        Vector3 p = nr * hn + u * a + w * b;
                        Vector3 c = new(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 d = p - c;
                        Vector3 nn = d.sqrMagnitude > 1e-14f ? d.normalized : nr;
                        Vector3 pos = r > 1e-6f ? c + nn * r : p;
                        g.Add(pos, r > 1e-6f ? nn : nr, new Vector2((a + hu) / (2f * hu), (b + hw) / (2f * hw)), ch);
                    }
                int nu = cu.Count;
                for (int y = 0; y < cw.Count - 1; y++)
                    for (int x = 0; x < nu - 1; x++)
                    {
                        int i0 = start + y * nu + x, i1 = i0 + 1, i2 = i0 + nu, i3 = i2 + 1;
                        g.t.AddRange(new[] { i0, i1, i3, i0, i3, i2 });
                    }
            }
            return g;
        }

        static float Half(Vector3 h, Vector3 axis) => Mathf.Abs(axis.x) * h.x + Mathf.Abs(axis.y) * h.y + Mathf.Abs(axis.z) * h.z;

        // ───────────── 회전체 (Y축 선반 가공) ─────────────

        struct PP { public float r, y, nr, ny; public PP(float r, float y, float nr, float ny) { this.r = r; this.y = y; this.nr = nr; this.ny = ny; } }

        static int Segs(float r) => Mathf.Clamp(Mathf.RoundToInt(r * 650f), 16, 44);

        /// <summary>프로파일(아래 중심 → 바깥 → 위 중심 순서)을 Y축으로 회전시킨다. 프로파일 전체가 하나의 차트.</summary>
        void Lathe(List<PP> prof, int segs, Func<int, float> mod)
        {
            float len = 0f, maxR = 0f;
            var acc = new float[prof.Count];
            for (int i = 0; i < prof.Count; i++)
            {
                if (i > 0) len += Vector2.Distance(new Vector2(prof[i].r, prof[i].y), new Vector2(prof[i - 1].r, prof[i - 1].y));
                acc[i] = len;
                maxR = Mathf.Max(maxR, prof[i].r);
            }
            int ch = NewChart(2f * Mathf.PI * maxR, len);
            int start = v.Count;
            int row = segs + 1;
            for (int i = 0; i < prof.Count; i++)
            {
                var p = prof[i];
                for (int s = 0; s <= segs; s++)
                {
                    float a = s * Mathf.PI * 2f / segs;
                    float m = mod != null ? mod(s % segs) : 1f;
                    float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                    Vector3 nrm = new Vector3(cs * p.nr, p.ny, sn * p.nr).normalized;
                    Add(new Vector3(cs * p.r * m, p.y, sn * p.r * m), nrm, new Vector2(s / (float)segs, len > 0 ? acc[i] / len : 0f), ch);
                }
            }
            for (int i = 0; i < prof.Count - 1; i++)
                for (int s = 0; s < segs; s++)
                {
                    int a0 = start + i * row + s, a1 = a0 + 1, b0 = a0 + row, b1 = b0 + 1;
                    t.AddRange(new[] { a0, b0, a1, b0, b1, a1 });
                }
        }

        static void Arc(List<PP> prof, float cr, float cy, float rad, float from, float to)
        {
            const int steps = 3;
            for (int k = 0; k <= steps; k++)
            {
                float th = Mathf.Lerp(from, to, k / (float)steps) * Mathf.Deg2Rad;
                float c = Mathf.Cos(th), s = Mathf.Sin(th);
                prof.Add(new PP(cr + c * rad, cy + s * rad, c, s));
            }
        }

        /// <summary>Y축 원뿔대. r0 = 아래 반지름, r1 = 위 반지름. mod는 세그먼트별 반지름 배율(기어 톱니용).</summary>
        public static Geo Frustum(float r0, float r1, float h, int segs = 0, Func<int, float> mod = null)
        {
            var g = new Geo();
            float y0 = -h / 2f, y1 = h / 2f;
            var prof = new List<PP>();
            bool straight = Mathf.Abs(r0 - r1) < 1e-5f;
            float b = straight && mod == null ? Mathf.Min(MaxBevel, Mathf.Min(r0, h * 0.5f) * 0.3f) : 0f;
            if (r0 > 1e-5f) { prof.Add(new PP(0f, y0, 0, -1)); prof.Add(new PP(r0 - b, y0, 0, -1)); }
            if (b > 0f)
            {
                Arc(prof, r0 - b, y0 + b, b, -90f, 0f);
                Arc(prof, r1 - b, y1 - b, b, 0f, 90f);
            }
            else
            {
                float slope = (r0 - r1) / Mathf.Max(h, 1e-5f);
                var sn = new Vector2(1f, slope).normalized;
                prof.Add(new PP(r0, y0, sn.x, sn.y));
                prof.Add(new PP(r1, y1, sn.x, sn.y));
            }
            if (r1 > 1e-5f) { prof.Add(new PP(r1 - b, y1, 0, 1)); prof.Add(new PP(0f, y1, 0, 1)); }
            g.Lathe(prof, segs > 0 ? segs : Segs(Mathf.Max(r0, r1)), mod);
            return g;
        }

        public static Geo Cyl(float r, float h, int segs = 0) => Frustum(r, r, h, segs);

        public static Geo Gear(float r, float h, int teeth) => Frustum(r, r, h, teeth * 4, s => (s % 4) < 2 ? 1f : 0.8f);

        /// <summary>속이 빈 관. Y축. 바깥 모서리를 둥글린다.</summary>
        public static Geo Tube(float rOut, float rIn, float h, int segs = 0)
        {
            var g = new Geo();
            float y0 = -h / 2f, y1 = h / 2f;
            float b = Mathf.Min(MaxBevel, Mathf.Min((rOut - rIn) * 0.45f, h * 0.3f));
            var prof = new List<PP>
            {
                new(rIn, y0, 0, -1),
                new(rOut - b, y0, 0, -1),
            };
            Arc(prof, rOut - b, y0 + b, b, -90f, 0f);
            Arc(prof, rOut - b, y1 - b, b, 0f, 90f);
            prof.Add(new PP(rIn, y1, 0, 1));
            prof.Add(new PP(rIn, y1, -1, 0));
            prof.Add(new PP(rIn, y0, -1, 0));
            g.Lathe(prof, segs > 0 ? segs : Segs(rOut), null);
            return g;
        }

        public static Geo Sphere(float r, int lon = 0, int lat = 0)
        {
            var g = new Geo();
            if (lon <= 0) lon = Mathf.Clamp(Mathf.RoundToInt(r * 900f), 14, 32);
            if (lat <= 0) lat = lon / 2 + 1;
            int ch = g.NewChart(2f * Mathf.PI * r, Mathf.PI * r);
            for (int j = 0; j <= lat; j++)
            {
                float phi = -Mathf.PI / 2f + Mathf.PI * j / lat;
                float y = Mathf.Sin(phi), rr = Mathf.Cos(phi);
                for (int i = 0; i <= lon; i++)
                {
                    float a = i * Mathf.PI * 2f / lon;
                    Vector3 nr = new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
                    g.Add(nr * r, nr, new Vector2(i / (float)lon, j / (float)lat), ch);
                }
            }
            int row = lon + 1;
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    int a = j * row + i, b = a + row, c = a + 1, d = b + 1;
                    g.t.AddRange(new[] { a, b, c, b, d, c });
                }
            return g;
        }
    }

    /// <summary>부품을 이루는 도형 하나: 형상 + 자세 + 재질.</summary>
    public class Shape
    {
        public Geo geo;
        public Vector3 pos;
        public Quaternion rot = Quaternion.identity;
        public Color color = Color.white;
        public float metallic;
        public float smooth = 0.4f;
        public bool paintable;
    }

    public static class MeshGen
    {
        struct ChartRef { public int shape, chart; public Vector2 size; public Vector2 pos; }

        /// <summary>도형들을 하나의 메시로 합친다. atlasSize = 아틀라스 한 변의 실제 길이(m).</summary>
        public static Mesh Combine(List<Shape> shapes, Vector3 origin, out float atlasSize)
        {
            // 1) 차트 수집 + 선반(shelf) 패킹
            var charts = new List<ChartRef>();
            float area = 0f, maxW = 0f;
            for (int si = 0; si < shapes.Count; si++)
                for (int ci = 0; ci < shapes[si].geo.chartSize.Count; ci++)
                {
                    Vector2 sz = shapes[si].geo.chartSize[ci];
                    charts.Add(new ChartRef { shape = si, chart = ci, size = sz });
                    area += sz.x * sz.y;
                    maxW = Mathf.Max(maxW, sz.x);
                }
            charts.Sort((a, b) => b.size.y.CompareTo(a.size.y));
            float side = Mathf.Max(Mathf.Sqrt(area) * 1.15f, maxW * 1.05f);
            for (int attempt = 0; attempt < 40; attempt++)
            {
                float pad = side * 0.012f;
                float x = pad, y = pad, rowH = 0f;
                bool ok = true;
                for (int i = 0; i < charts.Count; i++)
                {
                    var c = charts[i];
                    if (x + c.size.x + pad > side) { x = pad; y += rowH + pad; rowH = 0f; }
                    if (x + c.size.x + pad > side || y + c.size.y + pad > side) { ok = false; break; }
                    c.pos = new Vector2(x, y);
                    charts[i] = c;
                    x += c.size.x + pad;
                    rowH = Mathf.Max(rowH, c.size.y);
                }
                if (ok) break;
                side *= 1.1f;
            }
            atlasSize = side;
            var lookup = new Dictionary<long, ChartRef>();
            foreach (var c in charts) lookup[((long)c.shape << 16) | (uint)c.chart] = c;

            // 2) 정점 병합
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var col = new List<Color>();
            var t = new List<int>();
            for (int si = 0; si < shapes.Count; si++)
            {
                var s = shapes[si];
                int baseIdx = v.Count;
                // 정점 색은 자동 변환이 없으므로 sRGB로 지정한 색을 선형 공간으로 바꿔 넣는다
                Color lin = s.color.linear;
                var c = new Color(lin.r, lin.g, lin.b, s.paintable ? 1f : 0f);
                var pbr = new Vector2(s.metallic, s.smooth);
                for (int i = 0; i < s.geo.v.Count; i++)
                {
                    var ch = lookup[((long)si << 16) | (uint)s.geo.chart[i]];
                    v.Add(s.rot * s.geo.v[i] + s.pos - origin);
                    n.Add(s.rot * s.geo.n[i]);
                    uv.Add((ch.pos + Vector2.Scale(s.geo.uv[i], ch.size)) / side);
                    uv1.Add(pbr);
                    col.Add(c);
                }
                foreach (int idx in s.geo.t) t.Add(baseIdx + idx);
            }
            var m = new Mesh { name = "PartMesh", indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetUVs(1, uv1);
            m.SetColors(col);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        public static Mesh Combine(List<Shape> shapes) => Combine(shapes, Vector3.zero, out _);

        public static Bounds BoundsOf(List<Shape> shapes)
        {
            bool first = true;
            var b = new Bounds();
            foreach (var s in shapes)
                foreach (var p in s.geo.v)
                {
                    Vector3 w = s.rot * p + s.pos;
                    if (first) { b = new Bounds(w, Vector3.zero); first = false; }
                    else b.Encapsulate(w);
                }
            return b;
        }
    }
}
