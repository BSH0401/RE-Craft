using System.Collections.Generic;
using UnityEngine;

namespace ReCraft
{
    public enum PaintResult { None, Changed, Blocked }

    /// <summary>아이템을 이루는 부품 하나. 녹/먼지/도색 마스크 텍스처를 CPU에서 직접 칠한다.</summary>
    public class Part : MonoBehaviour
    {
        public PartDef def;
        public ItemInstance item;
        public Condition condition;
        public bool replaced;
        public bool ordered;
        public bool attached = true;
        public bool busy;
        public Vector3 homePos;      // 아이템 루트 기준
        public Vector3 trayPos;      // 월드
        public float radius;
        public readonly List<Screw> screws = new();
        public Material mat;
        public MeshRenderer mr;
        public Mesh mesh;
        public Vector3[] verts;   // 거치대 높이 계산용 (로컬 정점)

        // 마스크: 텍셀마다 부품 로컬 좌표를 구워 두고, 브러시는 3D 거리로 판정한다.
        int res;
        Vector3[] tp;
        byte[] flag;        // 0 = 빈 칸, 1 = 유효, 2 = 가장자리 확장(집계 제외)
        bool[] paintable;
        Color32[] px;
        Texture2D tex;
        int nPaint;
        long rust0, rust, dust0, dust, paint;
        bool hasPaintable;
        float atlasSize;
        Color hover, glow;

        static Texture2D blackTex;
        static readonly int MaskId = Shader.PropertyToID("_MaskTex");
        static readonly int EmissionId = Shader.PropertyToID("_Emission");

        public bool HasMask => tex != null;
        public bool HasRust => rust0 > 0;
        public bool HasDust => dust0 > 0;
        public bool HasPaint => nPaint > 0;
        public float RustLeft => rust0 > 0 ? (float)rust / rust0 : 0f;
        public float DustLeft => dust0 > 0 ? (float)dust / dust0 : 0f;
        public float PaintDone => nPaint > 0 ? (float)paint / (255L * nPaint) : 1f;
        public long RustSum => rust; public long Rust0 => rust0;
        public long DustSum => dust; public long Dust0 => dust0;
        public long PaintSum => paint; public long PaintMax => 255L * nPaint;
        public bool NeedsReplace => condition == Condition.Broken && !replaced;

        public void Build(PartDef d, ItemInstance it, Mesh m, float atlas)
        {
            def = d; item = it; mesh = m; atlasSize = atlas;
            verts = m.vertices;
            gameObject.AddComponent<MeshFilter>().sharedMesh = m;
            mr = gameObject.AddComponent<MeshRenderer>();
            mat = Mat.Surface();
            mr.sharedMaterial = mat;
            gameObject.AddComponent<MeshCollider>().sharedMesh = m;
            radius = m.bounds.extents.magnitude;
            foreach (var s in d.shapes) hasPaintable |= s.paintable;
            if (!blackTex)
            {
                blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                blackTex.SetPixel(0, 0, new Color(0, 0, 0, 0));
                blackTex.Apply();
            }
            mat.SetTexture(MaskId, blackTex);
        }

        /// <summary>상태(녹/먼지/파손)에 맞춰 마스크를 초기화한다.</summary>
        public void InitState(Condition c, bool dusty, float rustAmount, System.Random rng)
        {
            condition = c;
            mat.SetFloat("_Broken", c == Condition.Broken ? 1f : 0f);
            bool rusty = c == Condition.Rusty;
            if (!rusty && !dusty && !hasPaintable) return;
            Bake();
            float o1 = (float)rng.NextDouble() * 50f, o2 = (float)rng.NextDouble() * 50f, o3 = (float)rng.NextDouble() * 50f;
            int n = res * res;
            float th = 0.56f - rustAmount * 0.12f;
            for (int pass = 0; pass < 6; pass++)
            {
                rust0 = 0; dust0 = 0; nPaint = 0;
                int counted = 0;
                for (int i = 0; i < n; i++)
                {
                    if (flag[i] == 0) { px[i] = new Color32(0, 0, 0, 0); continue; }
                    Vector3 p = tp[i];
                    byte r = 0, g = 0;
                    if (rusty)
                    {
                        float v = Noise3(p * 16f, o1, o2, o3);
                        r = (byte)(Mathf.SmoothStep(th - 0.05f, th + 0.05f, v) * 255f);
                    }
                    if (dusty)
                    {
                        float v = Noise3(p * 34f, o3, o1, o2);
                        g = (byte)Mathf.Clamp(120f + v * 180f, 0f, 255f);
                    }
                    px[i] = new Color32(r, g, 0, 255);
                    if (flag[i] == 1)
                    {
                        counted++;
                        rust0 += r; dust0 += g;
                        if (paintable[i]) nPaint++;
                    }
                }
                if (!rusty || counted == 0 || rust0 > 0.18f * 255f * counted) break;
                th -= 0.07f;
            }
            rust = rust0; dust = dust0; paint = 0;
            mat.SetFloat("_Worn", 1f);
            Upload();
        }

        static float Noise3(Vector3 p, float o1, float o2, float o3)
        {
            return (Mathf.PerlinNoise(p.x + o1, p.y + o2) + Mathf.PerlinNoise(p.y + o3, p.z + o1) + Mathf.PerlinNoise(p.z + o2, p.x + o3)) / 3f;
        }

        void Bake()
        {
            res = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(atlasSize / 0.0025f)), 32, 256);
            int n = res * res;
            tp = new Vector3[n];
            flag = new byte[n];
            paintable = new bool[n];
            px = new Color32[n];
            var verts = mesh.vertices;
            var uvs = mesh.uv;
            var cols = mesh.colors;
            var tris = mesh.triangles;
            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                Vector2 a = uvs[i0] * res, b = uvs[i1] * res, c = uvs[i2] * res;
                float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                if (Mathf.Abs(d) < 1e-9f) continue;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                int x1 = Mathf.Min(res - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                int y1 = Mathf.Min(res - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                bool pt = cols[i0].a > 0.5f;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float pxx = x + 0.5f, pyy = y + 0.5f;
                        float w0 = ((b.y - c.y) * (pxx - c.x) + (c.x - b.x) * (pyy - c.y)) / d;
                        float w1 = ((c.y - a.y) * (pxx - c.x) + (a.x - c.x) * (pyy - c.y)) / d;
                        float w2 = 1f - w0 - w1;
                        if (w0 < -0.01f || w1 < -0.01f || w2 < -0.01f) continue;
                        int idx = y * res + x;
                        tp[idx] = verts[i0] * w0 + verts[i1] * w1 + verts[i2] * w2;
                        flag[idx] = 1;
                        paintable[idx] = pt;
                    }
            }
            // 이중선형 필터 번짐 방지: 빈 칸을 이웃 값으로 2텍셀 확장
            for (int pass = 0; pass < 2; pass++)
            {
                var add = new List<(int idx, int src)>();
                for (int y = 0; y < res; y++)
                    for (int x = 0; x < res; x++)
                    {
                        int idx = y * res + x;
                        if (flag[idx] != 0) continue;
                        int src = -1;
                        if (x > 0 && flag[idx - 1] != 0) src = idx - 1;
                        else if (x < res - 1 && flag[idx + 1] != 0) src = idx + 1;
                        else if (y > 0 && flag[idx - res] != 0) src = idx - res;
                        else if (y < res - 1 && flag[idx + res] != 0) src = idx + res;
                        if (src >= 0) add.Add((idx, src));
                    }
                foreach (var (idx, src) in add)
                {
                    flag[idx] = 2;
                    tp[idx] = tp[src];
                    paintable[idx] = paintable[src];
                }
            }
            tex = new Texture2D(res, res, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            mat.SetTexture(MaskId, tex);
        }

        void Upload()
        {
            tex.SetPixels32(px);
            tex.Apply(false);
        }

        /// <summary>channel: 0 = 녹 제거, 1 = 먼지 제거, 2 = 도색. localPoint는 부품 로컬 좌표.</summary>
        public PaintResult Paint(int channel, Vector3 localPoint, float brushRadius, float amount)
        {
            if (tex == null) return PaintResult.None;
            float r2 = brushRadius * brushRadius;
            bool changed = false, blocked = false;
            int n = res * res;
            for (int i = 0; i < n; i++)
            {
                if (flag[i] == 0) continue;
                float d2 = (tp[i] - localPoint).sqrMagnitude;
                if (d2 > r2) continue;
                float f = 1f - Mathf.SmoothStep(0.55f, 1f, Mathf.Sqrt(d2) / brushRadius);
                int delta = (int)(amount * f * 255f) + 1;
                Color32 c = px[i];
                bool count = flag[i] == 1;
                if (channel == 0)
                {
                    if (c.r == 0) continue;
                    int nv = Mathf.Max(0, c.r - delta);
                    if (count) rust -= c.r - nv;
                    c.r = (byte)nv;
                }
                else if (channel == 1)
                {
                    if (c.g == 0) continue;
                    int nv = Mathf.Max(0, c.g - delta);
                    if (count) dust -= c.g - nv;
                    c.g = (byte)nv;
                }
                else
                {
                    if (!paintable[i] || c.b == 255) continue;
                    if (c.r > 30 || c.g > 60) { blocked = true; continue; }
                    int nv = Mathf.Min(255, c.b + delta);
                    if (count) paint += nv - c.b;
                    c.b = (byte)nv;
                }
                px[i] = c;
                changed = true;
            }
            if (!changed) return blocked ? PaintResult.Blocked : PaintResult.None;
            AutoComplete();
            Upload();
            return PaintResult.Changed;
        }

        /// <summary>95% 규칙: 거의 다 끝났으면 남은 픽셀을 자동으로 마무리한다.</summary>
        void AutoComplete()
        {
            if (rust0 > 0 && rust > 0 && rust < rust0 * 0.05f) Fill(0, 0);
            if (dust0 > 0 && dust > 0 && dust < dust0 * 0.05f) Fill(1, 0);
            if (nPaint > 0 && paint < 255L * nPaint && paint > 255L * nPaint * 0.95f && rust == 0) Fill(2, 255);
        }

        /// <summary>채널 전체를 한 값으로 채운다 (자동 완료/자동 도구용). 업로드는 호출자가 한다.</summary>
        void Fill(int channel, byte value)
        {
            int n = res * res;
            for (int i = 0; i < n; i++)
            {
                if (flag[i] == 0) continue;
                Color32 c = px[i];
                if (channel == 0) c.r = value;
                else if (channel == 1) c.g = value;
                else if (paintable[i]) c.b = value;
                px[i] = c;
            }
            if (channel == 0) rust = 0;
            else if (channel == 1) dust = 0;
            else paint = 255L * nPaint;
        }

        /// <summary>부품 전체에 균일하게 작업한다 (초음파 세척조 / 자동 도색 부스 / 개발용 봇).</summary>
        public void StepAll(int channel, float amount)
        {
            if (tex == null) return;
            int delta = (int)(amount * 255f) + 1;
            int n = res * res;
            for (int i = 0; i < n; i++)
            {
                if (flag[i] == 0) continue;
                Color32 c = px[i];
                bool count = flag[i] == 1;
                if (channel == 0) { int nv = Mathf.Max(0, c.r - delta); if (count) rust -= c.r - nv; c.r = (byte)nv; }
                else if (channel == 1) { int nv = Mathf.Max(0, c.g - delta); if (count) dust -= c.g - nv; c.g = (byte)nv; }
                else
                {
                    if (!paintable[i] || c.r > 30 || c.g > 60) continue;
                    int nv = Mathf.Min(255, c.b + delta); if (count) paint += nv - c.b; c.b = (byte)nv;
                }
                px[i] = c;
            }
            Upload();
        }

        public void SetPaintColor(Color? c)
        {
            mat.SetFloat("_UsePaintColor", c.HasValue ? 1f : 0f);
            if (c.HasValue) mat.SetColor("_PaintColor", c.Value);
            mat.SetFloat("_PaintMetal", G.Lv(Tool.Spray) >= 2 ? 0.35f : 0f);
        }

        /// <summary>교체 부품 도착: 새것으로 만든다.</summary>
        public void MakeNew()
        {
            condition = Condition.Normal;
            replaced = true;
            mat.SetFloat("_Broken", 0f);
            if (tex != null)
            {
                Fill(0, 0); Fill(1, 0); Fill(2, 255);
                Upload();
            }
            rust0 = 0; dust0 = 0;
        }

        public void SetHover(Color c) { hover = c; ApplyEmission(); }
        public void SetGlow(Color c) { glow = c; ApplyEmission(); }
        void ApplyEmission() => mat.SetColor(EmissionId, hover + glow);

        public void SetXray(Material m) => mr.sharedMaterial = m ? m : mat;

        void OnDestroy()
        {
            if (tex) Destroy(tex);
            if (mat) Destroy(mat);
            if (mesh) Destroy(mesh);
        }
    }

    /// <summary>나사. progress 0 = 완전히 조여짐, 1 = 풀려서 트레이로.</summary>
    public class Screw : MonoBehaviour
    {
        public Part owner;
        public ItemInstance item;
        public bool onTray;
        public bool tightening;
        public float progress;
        public Vector3 homePos;
        public Quaternion homeRot;
        public Vector3 trayPos;
        public Quaternion trayRot;
        public GameObject hole;
        public MeshRenderer mr;
        public Material mat;
        public bool driven;   // 이번 프레임에 드라이버가 닿았는가

        public bool FullyIn => !onTray && progress < 0.001f;

        public void ApplyPose()
        {
            if (onTray) return;
            transform.localPosition = homePos + homeRot * Vector3.up * (progress * 0.012f);
            transform.localRotation = homeRot * Quaternion.Euler(0f, progress * 1440f, 0f);
        }

        void OnDestroy()
        {
            if (mat) Destroy(mat);
        }
    }

    public class ItemInstance
    {
        public ItemDef def;
        public Offer offer;
        public Transform root;
        public readonly List<Part> parts = new();
        public readonly List<Screw> screws = new();
        public bool tested;
        public float scale = 1f;
        public int spentOnParts;

        public Part Get(string id) => parts.Find(p => p.def.id == id);
    }

    /// <summary>머티리얼/셰이더 캐시.</summary>
    public static class Mat
    {
        static Shader surface, xray, particle;

        public static Material Surface()
        {
            if (!surface) surface = Resources.Load<Shader>("Shaders/Surface");
            return new Material(surface);
        }

        public static Material XRay(Color c, float fill = 0.08f)
        {
            if (!xray) xray = Resources.Load<Shader>("Shaders/XRay");
            var m = new Material(xray);
            m.SetColor("_Color", c);
            m.SetFloat("_Fill", fill);
            return m;
        }

        public static Material Particle(bool additive)
        {
            if (!particle) particle = Resources.Load<Shader>("Shaders/Particle");
            var m = new Material(particle);
            m.SetFloat("_Additive", additive ? 1f : 0f);
            return m;
        }
    }
}
