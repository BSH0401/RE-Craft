using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReCraft
{
    /// <summary>
    /// 기본 도형. 면마다 "차트"(실제 크기를 가진 UV 조각)를 가지며,
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

        public static Geo Box(Vector3 size)
        {
            var g = new Geo();
            for (int i = 0; i < 6; i++)
            {
                Vector3 nr = Dirs[i];
                Vector3 u = Mathf.Abs(nr.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, nr);
                Vector3 w = Vector3.Cross(nr, u);
                float hn = Half(size, nr), hu = Half(size, u), hw = Half(size, w);
                int ch = g.NewChart(hu * 2f, hw * 2f);
                int a = g.Add(nr * hn - u * hu - w * hw, nr, new Vector2(0, 0), ch);
                int b = g.Add(nr * hn + u * hu - w * hw, nr, new Vector2(1, 0), ch);
                int c = g.Add(nr * hn + u * hu + w * hw, nr, new Vector2(1, 1), ch);
                int d = g.Add(nr * hn - u * hu + w * hw, nr, new Vector2(0, 1), ch);
                g.t.AddRange(new[] { a, b, c, a, c, d });
            }
            return g;
        }

        static float Half(Vector3 size, Vector3 axis) =>
            (Mathf.Abs(axis.x) * size.x + Mathf.Abs(axis.y) * size.y + Mathf.Abs(axis.z) * size.z) * 0.5f;

        /// <summary>Y축 원뿔대. r0 = 아래 반지름, r1 = 위 반지름. mod는 세그먼트별 반지름 배율(기어 톱니용).</summary>
        public static Geo Frustum(float r0, float r1, float h, int segs = 24, Func<int, float> mod = null)
        {
            var g = new Geo();
            g.Side(r0, r1, h, segs, mod, true);
            if (r1 > 1e-5f) g.Cap(r1, h / 2f, true, segs, 0f, mod);
            if (r0 > 1e-5f) g.Cap(r0, -h / 2f, false, segs, 0f, mod);
            return g;
        }

        public static Geo Cyl(float r, float h, int segs = 24) => Frustum(r, r, h, segs);

        public static Geo Gear(float r, float h, int teeth) => Frustum(r, r, h, teeth * 4, s => (s % 4) < 2 ? 1f : 0.8f);

        /// <summary>속이 빈 관. Y축.</summary>
        public static Geo Tube(float rOut, float rIn, float h, int segs = 28)
        {
            var g = new Geo();
            g.Side(rOut, rOut, h, segs, null, true);
            g.Side(rIn, rIn, h, segs, null, false);
            g.Cap(rOut, h / 2f, true, segs, rIn / rOut, null);
            g.Cap(rOut, -h / 2f, false, segs, rIn / rOut, null);
            return g;
        }

        void Side(float r0, float r1, float h, int segs, Func<int, float> mod, bool outward)
        {
            float slope = (r0 - r1) / Mathf.Max(h, 1e-5f);
            float slant = Mathf.Sqrt(h * h + (r0 - r1) * (r0 - r1));
            int ch = NewChart(2f * Mathf.PI * Mathf.Max(r0, r1), slant);
            int start = v.Count;
            for (int s = 0; s <= segs; s++)
            {
                float a = s * Mathf.PI * 2f / segs;
                float m = mod != null ? mod(s % segs) : 1f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                Vector3 nr = new Vector3(cs, slope, sn).normalized * (outward ? 1f : -1f);
                float ux = s / (float)segs;
                Add(new Vector3(cs * r0 * m, -h / 2f, sn * r0 * m), nr, new Vector2(ux, 0f), ch);
                Add(new Vector3(cs * r1 * m, h / 2f, sn * r1 * m), nr, new Vector2(ux, 1f), ch);
            }
            for (int s = 0; s < segs; s++)
            {
                int b0 = start + 2 * s, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
                if (outward) t.AddRange(new[] { b0, t0, b1, t0, t1, b1 });
                else t.AddRange(new[] { b0, b1, t0, t0, b1, t1 });
            }
        }

        /// <summary>원형(또는 고리형) 뚜껑.</summary>
        void Cap(float r, float y, bool up, int segs, float inner, Func<int, float> mod)
        {
            int ch = NewChart(2f * r, 2f * r);
            Vector3 nr = up ? Vector3.up : Vector3.down;
            var c = new Vector2(0.5f, 0.5f);
            int start = v.Count;
            for (int s = 0; s <= segs; s++)
            {
                float a = s * Mathf.PI * 2f / segs;
                float m = mod != null ? mod(s % segs) : 1f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                Add(new Vector3(cs * r * m, y, sn * r * m), nr, c + new Vector2(cs, sn) * 0.5f * m, ch);
                Add(new Vector3(cs * r * inner, y, sn * r * inner), nr, c + new Vector2(cs, sn) * 0.5f * inner, ch);
            }
            for (int s = 0; s < segs; s++)
            {
                int o0 = start + 2 * s, i0 = o0 + 1, o1 = o0 + 2, i1 = o0 + 3;
                if (up) t.AddRange(new[] { i0, o1, o0, i0, i1, o1 });
                else t.AddRange(new[] { i0, o0, o1, i0, o1, i1 });
            }
        }

        public static Geo Sphere(float r, int lon = 18, int lat = 10)
        {
            var g = new Geo();
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
                var c = new Color(s.color.r, s.color.g, s.color.b, s.paintable ? 1f : 0f);
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
            var m = new Mesh { name = "PartMesh" };
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
