using System.Collections.Generic;
using UnityEngine;

namespace ReCraft
{
    public enum Condition { Normal, Rusty, Broken }
    public enum AnimKind { None, Spin, Glow, Wobble }
    public enum TestSound { Hum, Bell, Melody, Beep, Click }

    public class ScrewDef
    {
        public Vector3 pos;
        public Vector3 dir;
    }

    /// <summary>부품 정의. 모든 좌표는 아이템 공간(원점 = 아이템 중심, -Z = 정면).</summary>
    public class PartDef
    {
        public string id;
        public string name;
        public readonly List<Shape> shapes = new();
        public readonly List<ScrewDef> screws = new();
        public readonly List<string> blockedBy = new();
        public bool isBody;
        public bool external;      // 겉면 부품: 먼지가 쌓인다
        public bool canRust;
        public int price;          // > 0 이면 파손될 수 있고, 교체 부품 가격
        public AnimKind anim;
        public Vector3 animAxis = Vector3.forward;
        public Vector3 animPivot;
        public float animSpeed = 180f;
        public Color glow = Color.white;

        // ── 도형 추가 ──
        public PartDef Box(float sx, float sy, float sz, float x, float y, float z, Color c, float metal = 0f, float smooth = 0.4f, bool paint = false, Quaternion? rot = null)
        {
            shapes.Add(new Shape { geo = Geo.Box(new Vector3(sx, sy, sz)), pos = new Vector3(x, y, z), rot = rot ?? Quaternion.identity, color = c, metallic = metal, smooth = smooth, paintable = paint });
            return this;
        }

        public PartDef Cyl(float r, float h, float x, float y, float z, Quaternion rot, Color c, float metal = 0f, float smooth = 0.4f, bool paint = false)
        {
            shapes.Add(new Shape { geo = Geo.Cyl(r, h), pos = new Vector3(x, y, z), rot = rot, color = c, metallic = metal, smooth = smooth, paintable = paint });
            return this;
        }

        public PartDef Cone(float r0, float r1, float h, float x, float y, float z, Quaternion rot, Color c, float metal = 0f, float smooth = 0.4f, bool paint = false)
        {
            shapes.Add(new Shape { geo = Geo.Frustum(r0, r1, h), pos = new Vector3(x, y, z), rot = rot, color = c, metallic = metal, smooth = smooth, paintable = paint });
            return this;
        }

        public PartDef Tube(float rOut, float rIn, float h, float x, float y, float z, Quaternion rot, Color c, float metal = 0f, float smooth = 0.4f, bool paint = false)
        {
            shapes.Add(new Shape { geo = Geo.Tube(rOut, rIn, h), pos = new Vector3(x, y, z), rot = rot, color = c, metallic = metal, smooth = smooth, paintable = paint });
            return this;
        }

        public PartDef Gear(float r, float h, int teeth, float x, float y, float z, Quaternion rot, Color c, float metal = 0.9f, float smooth = 0.6f)
        {
            shapes.Add(new Shape { geo = Geo.Gear(r, h, teeth), pos = new Vector3(x, y, z), rot = rot, color = c, metallic = metal, smooth = smooth });
            return this;
        }

        public PartDef Ball(float r, float x, float y, float z, Color c, float metal = 0f, float smooth = 0.4f, bool paint = false)
        {
            shapes.Add(new Shape { geo = Geo.Sphere(r), pos = new Vector3(x, y, z), color = c, metallic = metal, smooth = smooth, paintable = paint });
            return this;
        }

        // ── 속성 ──
        public PartDef Ext() { external = true; return this; }
        public PartDef Rust() { canRust = true; return this; }
        public PartDef Breakable(int partPrice) { price = partPrice; return this; }
        public PartDef Under(params string[] ids) { blockedBy.AddRange(ids); return this; }
        public PartDef Screw(float x, float y, float z, Vector3 dir) { screws.Add(new ScrewDef { pos = new Vector3(x, y, z), dir = dir }); return this; }
        public PartDef Spin(Vector3 axis, float speed, Vector3 pivot) { anim = AnimKind.Spin; animAxis = axis; animSpeed = speed; animPivot = pivot; return this; }
        public PartDef Glow(Color c) { anim = AnimKind.Glow; glow = c; return this; }
        public PartDef Wobble() { anim = AnimKind.Wobble; return this; }
    }

    public class ItemDef
    {
        public string id;
        public string name;
        public int tier;           // 1, 2, 4(엔딩 유물)
        public int value;          // 기준 보수
        public TestSound sound;
        public string blurb;
        public readonly List<PartDef> parts = new();

        public PartDef Part(string id, string name)
        {
            var p = new PartDef { id = id, name = name };
            parts.Add(p);
            return p;
        }

        public PartDef Body(string name)
        {
            var p = Part("body", name);
            p.isBody = true;
            p.external = true;
            return p;
        }

        public int ScrewCount
        {
            get { int n = 0; foreach (var p in parts) n += p.screws.Count; return n; }
        }
    }
}
