using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ReCraft
{
    public struct ScoreInfo
    {
        public string itemName;
        public float rust, dust, paint, parts, assembly;
        public bool tested, auction, ending, relic;
        public int total, payout, stars, reward, cost, partsCost;
    }

    public struct WorkStats
    {
        public int detachable, detached, screws, screwsOut, screwsIn, attached;
        public int broken, replaced;
        public float rustDone, dustDone, paintDone;
        public bool hasRust, hasDust, hasPaint;
        public bool assembled;
    }

    /// <summary>작업대: 아이템 생성, 회전/줌, 도구 조작, 분해·조립 규칙, 정산.</summary>
    public class Workbench : MonoBehaviour
    {
        public static Workbench I;
        public ItemInstance item;
        public Tool tool = Tool.Hand;
        public Camera cam;
        public Color? paintColor;   // null = 원래 색 복원
        public bool xray, forceXray;
        public bool testing, autoBusy;
        public bool Busy => testing || autoBusy;

        const int ScrewLayer = 8, HoleLayer = 9;
        static readonly Vector3 Target = new(0f, Room.BenchTop + 0.17f, 0f);
        static readonly Vector3 CamDir = new Vector3(0f, 0.46f, -1f).normalized;
        static readonly Vector3 ItemCenter = new(0f, Room.BenchTop + 0.2f, 0f);
        float zoom = 1.3f, zoomTarget = 1.3f;
        bool rotating;

        readonly Dictionary<Collider, Screw> screwByCol = new();
        Mesh screwMesh;
        Material xrBase, xrYellow, xrRed, xrOrange, xrGhost, xrHole;
        GameObject ghost;
        MeshFilter ghostFilter;
        Part hoverPart, dragPart;
        Screw hoverScrew;
        Vector2 dragStart;
        bool dragMoved;
        bool lastXray;
        float blockedToastTime, tickTimer;

        void Awake()
        {
            I = this;
            Physics.autoSyncTransforms = true;
            xrBase = Mat.XRay(new Color(0.15f, 0.6f, 0.9f, 0.5f), 0.04f);
            xrYellow = Mat.XRay(new Color(2.5f, 2.0f, 0.2f, 1f), 0.6f);
            xrRed = Mat.XRay(new Color(2.5f, 0.25f, 0.15f, 1f), 0.45f);
            xrOrange = Mat.XRay(new Color(1.6f, 0.7f, 0.1f, 0.8f), 0.15f);
            xrGhost = Mat.XRay(new Color(0.3f, 1.6f, 0.5f, 0.9f), 0.2f);
            xrHole = Mat.XRay(new Color(2.0f, 1.6f, 0.2f, 1f), 0.5f);
            var sd = new PartDef();
            Color steel = new(0.75f, 0.76f, 0.8f);
            // 십자 나사: 둥근 머리 + 십자 홈 + 나사산
            Color slot = new(0.12f, 0.12f, 0.13f);
            sd.Cyl(0.006f, 0.0022f, 0, 0.0011f, 0, Quaternion.identity, steel, 0.95f, 0.75f);
            sd.Cone(0.0058f, 0.0036f, 0.0014f, 0, 0.0029f, 0, Quaternion.identity, steel, 0.95f, 0.8f);
            sd.Box(0.0075f, 0.0012f, 0.0013f, 0, 0.0033f, 0, slot, 0.6f, 0.3f);
            sd.Box(0.0013f, 0.0012f, 0.0075f, 0, 0.0033f, 0, slot, 0.6f, 0.3f);
            sd.Cyl(0.0022f, 0.012f, 0, -0.006f, 0, Quaternion.identity, steel, 0.95f, 0.6f);
            for (int i = 0; i < 5; i++) sd.Cyl(0.0027f, 0.0008f, 0, -0.0015f - i * 0.0022f, 0, Quaternion.identity, steel, 0.95f, 0.6f);
            sd.Cone(0.0004f, 0.0022f, 0.0018f, 0, -0.0129f, 0, Quaternion.identity, steel, 0.95f, 0.6f);
            screwMesh = MeshGen.Combine(sd.shapes);
            ghost = new GameObject("Ghost");
            ghost.transform.SetParent(transform, false);
            ghostFilter = ghost.AddComponent<MeshFilter>();
            ghost.AddComponent<MeshRenderer>().sharedMaterial = xrGhost;
            ghost.SetActive(false);
        }

        // ───────────────────────── 생성 / 제거 ─────────────────────────

        public void Spawn(Offer offer)
        {
            Clear();
            var rng = new System.Random(offer.seed);
            var def = offer.item;
            var it = new ItemInstance { def = def, offer = offer };
            it.root = new GameObject("Item_" + def.id).transform;
            it.root.SetParent(transform, false);
            it.root.position = ItemCenter;
            it.root.rotation = Quaternion.Euler(8f, -24f, 0f);

            // 파손 부품 선정
            var breakable = new List<PartDef>();
            foreach (var pd in def.parts) if (pd.price > 0) breakable.Add(pd);
            for (int i = breakable.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (breakable[i], breakable[j]) = (breakable[j], breakable[i]);
            }
            int nBroken = Mathf.Min(breakable.Count, 1 + (offer.severity > 0.55f ? 1 : 0) + (def.tier >= 2 ? 1 : 0) + (def.tier == 3 ? 1 : 0));
            var broken = new HashSet<PartDef>(breakable.GetRange(0, nBroken));

            bool anyRust = false;
            int screwIdx = 0;
            foreach (var pd in def.parts)
            {
                Bounds b = MeshGen.BoundsOf(pd.shapes);
                Mesh mesh = MeshGen.Combine(pd.shapes, b.center, out float atlas);
                var go = new GameObject(pd.id);
                go.transform.SetParent(it.root, false);
                go.transform.localPosition = b.center;
                var p = go.AddComponent<Part>();
                p.Build(pd, it, mesh, atlas);
                p.homePos = b.center;
                Condition c = Condition.Normal;
                if (broken.Contains(pd)) c = Condition.Broken;
                else if (pd.canRust && rng.NextDouble() < 0.5 + offer.severity * 0.5) { c = Condition.Rusty; anyRust = true; }
                p.InitState(c, pd.external, offer.severity, rng);
                it.parts.Add(p);

                foreach (var sdef in pd.screws)
                {
                    var sgo = new GameObject("Screw") { layer = ScrewLayer };
                    sgo.transform.SetParent(it.root, false);
                    sgo.transform.localScale = Vector3.one * def.screwScale;
                    sgo.AddComponent<MeshFilter>().sharedMesh = screwMesh;
                    var s = sgo.AddComponent<Screw>();
                    s.mr = sgo.AddComponent<MeshRenderer>();
                    s.mat = Mat.Surface();
                    s.mr.sharedMaterial = s.mat;
                    var col = sgo.AddComponent<SphereCollider>();
                    col.radius = 0.011f;
                    col.center = new Vector3(0, 0.002f, 0);
                    s.owner = p; s.item = it;
                    s.homePos = sdef.pos;
                    s.homeRot = Quaternion.FromToRotation(Vector3.up, sdef.dir);
                    s.trayPos = new Vector3(-0.64f + (screwIdx % 14) * 0.03f, Room.BenchTop + 0.016f, 0.345f + (screwIdx / 14) * 0.03f);
                    s.trayRot = Quaternion.Euler(0f, 90f + (screwIdx % 3 - 1) * 12f, 90f);
                    screwIdx++;
                    s.ApplyPose();
                    var hole = new GameObject("Hole") { layer = HoleLayer };
                    hole.transform.SetParent(it.root, false);
                    hole.transform.localPosition = s.homePos;
                    hole.transform.localRotation = s.homeRot;
                    hole.transform.localScale = Vector3.one * def.screwScale;
                    var hc = hole.AddComponent<SphereCollider>();
                    hc.radius = 0.011f;
                    hole.AddComponent<MeshFilter>().sharedMesh = screwMesh;
                    hole.AddComponent<MeshRenderer>().sharedMaterial = xrHole;
                    hole.SetActive(false);
                    s.hole = hole;
                    screwByCol[col] = s;
                    screwByCol[hc] = s;
                    p.screws.Add(s);
                    it.screws.Add(s);
                }
            }
            // 녹슨 부품이 하나도 없으면 하나는 보장
            if (!anyRust)
                foreach (var p in it.parts)
                    if (p.def.canRust && p.condition == Condition.Normal)
                    {
                        p.InitState(Condition.Rusty, p.def.external, offer.severity, rng);
                        break;
                    }
            // 작은 물건은 작업하기 좋게 확대해서 올려놓는다
            Bounds all = MeshGen.BoundsOf(def.parts[0].shapes);
            foreach (var pd in def.parts) all.Encapsulate(MeshGen.BoundsOf(pd.shapes));
            float ext = Mathf.Max(all.extents.x, Mathf.Max(all.extents.y, all.extents.z));
            it.scale = Mathf.Clamp(0.15f / ext, 1f, 2.5f);
            it.root.localScale = Vector3.one * it.scale;
            float bound = 0f;
            foreach (var p in it.parts)
            {
                bound = Mathf.Max(bound, p.homePos.magnitude + p.radius);
                p.radius *= it.scale;
            }
            // 거치대를 물건 바로 아래까지 올린다 (어느 방향으로 돌려도 닿지 않게 외접구 기준)
            Room.SetStand(ItemCenter.y - bound * it.scale - 0.004f);
            // 트레이 위 나사는 옆으로 눕힌다
            foreach (var s in it.screws) s.trayPos.y = Room.ScrewTrayTop + 0.0055f * it.scale * def.screwScale;
            item = it;
            // 부품이 많은 큰 물건은 작업대 전체가 보이도록 조금 물러선다
            zoomTarget = def.parts.Count >= 20 && ext * it.scale > 0.17f ? 1.6f : 1.3f;
            SetTool(Tool.Hand);
            HUD.I?.OnItemChanged();
        }

        public void Clear()
        {
            StopAllCoroutines();
            testing = autoBusy = false;
            Sfx.StopLoops();
            if (ghost) { ghost.transform.SetParent(transform, false); ghost.SetActive(false); }
            lastXray = false;
            if (item != null)
            {
                foreach (var p in item.parts) if (p) Destroy(p.gameObject);
                foreach (var s in item.screws) if (s) Destroy(s.gameObject);
                if (item.root) Destroy(item.root.gameObject);
            }
            screwByCol.Clear();
            item = null;
            hoverPart = null; hoverScrew = null; dragPart = null;
            if (ghost) ghost.SetActive(false);
        }

        public void SetTool(Tool t)
        {
            tool = t;
            dragPart = null;
            ghost.SetActive(false);
            HUD.I?.OnToolChanged();
        }

        // ───────────────────────── 규칙 ─────────────────────────

        public bool CanDetach(Part p, out string why)
        {
            why = null;
            if (p.def.isBody) { why = "본체는 분리할 수 없습니다"; return false; }
            if (!p.attached || p.busy) { why = ""; return false; }
            int left = 0;
            foreach (var s in p.screws) if (!s.onTray) left++;
            if (left > 0) { why = $"나사를 먼저 푸세요 ({left}개 남음)"; return false; }
            foreach (string id in p.def.blockedBy)
            {
                var q = item.Get(id);
                if (q != null && q.attached) { why = $"'{q.def.name}'부터 분리하세요"; return false; }
            }
            return true;
        }

        public bool CanAttach(Part p, out string why)
        {
            why = null;
            if (p.attached || p.busy) { why = ""; return false; }
            foreach (var q in item.parts)
            {
                if (!q.def.blockedBy.Contains(p.def.id)) continue;
                if (!q.attached) { why = $"'{q.def.name}'부터 조립하세요"; return false; }
                foreach (var s in q.screws)
                    if (!s.FullyIn) { why = $"'{q.def.name}'의 나사를 먼저 조이세요"; return false; }
            }
            return true;
        }

        bool ScrewReachable(Screw s)
        {
            if (!s.owner.attached) return false;
            foreach (string id in s.owner.def.blockedBy)
            {
                var q = item.Get(id);
                if (q != null && q.attached) return false;
            }
            return true;
        }

        // ───────────────────────── 분해 / 조립 ─────────────────────────

        public void Detach(Part p)
        {
            p.attached = false;
            p.transform.SetParent(transform, true);
            p.trayPos = FindSpot(p);
            item.tested = false;
            Sfx.Play("pop", 0.7f, Random.Range(0.9f, 1.15f));
            if (p.NeedsReplace && G.data.autoOrder) Order(p);
            HUD.I?.OnItemChanged();
        }

        public void Attach(Part p, bool instant = false)
        {
            if (instant) { FinishAttach(p, false); return; }
            p.busy = true;
            StartCoroutine(AttachRoutine(p));
        }

        IEnumerator AttachRoutine(Part p)
        {
            Vector3 start = p.transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.22f)
            {
                float k = t * t * (3f - 2f * t);
                p.transform.position = Vector3.Lerp(start, item.root.TransformPoint(p.homePos), k);
                p.transform.rotation = item.root.rotation;
                yield return null;
            }
            FinishAttach(p, true);
        }

        void FinishAttach(Part p, bool juice)
        {
            p.transform.SetParent(item.root, false);
            p.transform.localPosition = p.homePos;
            p.transform.localRotation = Quaternion.identity;
            p.transform.localScale = Vector3.one;
            p.attached = true;
            p.busy = false;
            item.tested = false;
            if (juice)
            {
                Sfx.Play("snap", 0.9f, Random.Range(0.92f, 1.08f));
                Fx.Shake(0.006f);
                Fx.Spark(p.transform.position, new Color(1f, 0.9f, 0.6f), 10, Mathf.Min(p.radius, 0.06f));
            }
            HUD.I?.OnItemChanged();
        }

        Vector3 FindSpot(Part p)
        {
            float r = Foot(p);
            float y = Room.BenchTop + Mathf.Clamp(r * 0.75f, 0.02f, 0.12f) + 0.012f;
            Vector3 best = new(0.6f, y, 0f);
            float bestScore = float.MaxValue;
            // 빈자리가 하나도 없을 때를 대비해 가장 덜 겹치는 자리도 기억한다
            Vector3 fallback = best;
            float fallbackOverlap = float.MaxValue;
            bool found = false;
            // 트레이를 먼저 채우고, 넘치면 작업대 나머지 공간으로
            for (float z = -0.4f; z <= 0.28f; z += 0.02f)
                for (float ax = 0.3f; ax <= 1.08f; ax += 0.02f)
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        if (ax - r < 0.26f || ax + r > 1.12f || z - r < -0.45f || z + r > 0.3f) continue;
                        float x = ax * sign;
                        float overlap = 0f;
                        foreach (var q in item.parts)
                        {
                            if (q == p || q.attached) continue;
                            float dx = q.trayPos.x - x, dz = q.trayPos.z - z;
                            float min = r + Foot(q) + 0.008f;
                            float d = Mathf.Sqrt(dx * dx + dz * dz);
                            if (d < min) overlap += min - d;
                        }
                        if (overlap > 0f)
                        {
                            if (!found && overlap < fallbackOverlap) { fallbackOverlap = overlap; fallback = new Vector3(x, y, z); }
                            continue;
                        }
                        found = true;
                        bool onTray = ax - r > 0.29f && ax + r < 0.91f && z - r > -0.34f && z + r < 0.28f;
                        float score = ax + (z + 0.26f) * 0.35f + (sign < 0 ? 0.01f : 0f) + (onTray ? 0f : 2f);
                        if (score < bestScore) { bestScore = score; best = new Vector3(x, y, z); }
                    }
            return found ? best : fallback;
        }

        /// <summary>트레이 위 점유 반지름: 가장 긴 변 기준 (얇은 원판이 대각선 크기로 자리를 낭비하지 않게).</summary>
        float Foot(Part p)
        {
            Vector3 e = p.mesh.bounds.extents;
            return Mathf.Min(Mathf.Max(e.x, Mathf.Max(e.y, e.z)) * item.scale * 1.1f, 0.2f);
        }

        public void ScrewOut(Screw s, bool quiet = false)
        {
            s.onTray = true;
            s.progress = 0f;
            s.tightening = false;
            s.transform.SetParent(transform, true);
            item.tested = false;
            if (!quiet)
            {
                Sfx.Play("drop", 0.6f, Random.Range(0.9f, 1.2f));
                Fx.Spark(s.transform.position, new Color(1f, 0.95f, 0.7f), 4, 0.004f);
            }
            HUD.I?.OnItemChanged();
        }

        public void ScrewIn(Screw s, bool quiet = false)
        {
            s.onTray = false;
            s.tightening = false;
            s.progress = 0f;
            s.transform.SetParent(item.root, false);
            s.transform.localScale = Vector3.one * item.def.screwScale;
            s.ApplyPose();
            item.tested = false;
            if (!quiet)
            {
                Sfx.Play("click", 0.8f, Random.Range(0.9f, 1.1f));
                Fx.Shake(0.003f, 0.1f);
            }
            HUD.I?.OnItemChanged();
        }

        void Drive(Screw s, float dt)
        {
            s.driven = true;
            if (s.onTray)
            {
                s.onTray = false;
                s.tightening = true;
                s.progress = 1f;
                s.transform.SetParent(item.root, false);
                s.transform.localScale = Vector3.one * item.def.screwScale;
                Sfx.Play("tick", 0.5f);
            }
            float step = dt / G.ScrewTime;
            if (s.tightening)
            {
                s.progress -= step;
                if (s.progress <= 0f) { ScrewIn(s); return; }
            }
            else
            {
                s.progress += step;
                if (s.progress >= 1f) { ScrewOut(s); return; }
            }
            s.ApplyPose();
        }

        // ───────────────────────── 부품 주문 ─────────────────────────

        public void Order(Part p)
        {
            if (!p.NeedsReplace || p.ordered) return;
            if (p.attached) { HUD.Toast("파손 부품을 먼저 분리하세요"); return; }
            if (!G.Spend(p.def.price)) { HUD.Toast($"골드가 부족합니다 ({p.def.price}G 필요)"); Sfx.Play("error"); return; }
            p.ordered = true;
            item.spentOnParts += p.def.price;
            Sfx.Play("click");
            HUD.Toast($"'{p.def.name}' 주문 완료 — 드론 배송 중...");
            StartCoroutine(DeliverPart(p));
            HUD.I?.OnItemChanged();
        }

        IEnumerator DeliverPart(Part p)
        {
            yield return new WaitForSeconds(2.2f);
            ReplaceNow(p);
            Sfx.Play("whoosh", 0.6f);
            Sfx.Play("sparkle", 0.8f);
            Fx.Spark(p.transform.position, new Color(0.6f, 0.9f, 1f), 24, Mathf.Min(p.radius, 0.05f));
            HUD.Toast($"새 '{p.def.name}' 도착!");
        }

        public void ReplaceNow(Part p)
        {
            p.MakeNew();
            HUD.I?.OnItemChanged();
        }

        // ───────────────────────── 자동 도구 (Lv.3) ─────────────────────────

        public void UltrasonicClean()
        {
            if (item == null || Busy || G.Lv(Tool.Cloth) < 3) return;
            StartCoroutine(AutoRoutine(1, "ultra", "초음파 세척 완료!"));
        }

        public void AutoPaint()
        {
            if (item == null || Busy || G.Lv(Tool.Spray) < 3) return;
            StartCoroutine(AutoRoutine(2, "spray", "자동 도색 완료!"));
        }

        IEnumerator AutoRoutine(int channel, string loop, string done)
        {
            autoBusy = true;
            Sfx.Loop(loop, 0.6f);
            const float dur = 2.2f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                foreach (var p in item.parts)
                {
                    if (channel == 2) p.SetPaintColor(paintColor);
                    p.StepAll(channel, Time.deltaTime / dur * 1.3f);
                    if (Random.value < 0.15f)
                    {
                        Vector3 pos = p.transform.position + Random.insideUnitSphere * Mathf.Min(p.radius, 0.08f);
                        if (channel == 1) Fx.Dust(pos, new Color(0.7f, 0.85f, 1f, 0.7f), 1);
                        else Fx.Mist(pos, paintColor ?? new Color(1f, 1f, 1f, 0.5f), 1);
                    }
                }
                yield return null;
            }
            foreach (var p in item.parts) p.StepAll(channel, 1f);
            Sfx.Loop(loop, 0f);
            Sfx.Play("chime", 0.7f);
            HUD.Toast(done);
            autoBusy = false;
            HUD.I?.OnItemChanged();
        }

        // ───────────────────────── 작동 테스트 / 납품 ─────────────────────────

        public void RunTest()
        {
            if (item == null || Busy) return;
            var st = Stats();
            if (!st.assembled) { HUD.Toast("조립을 모두 마친 뒤 테스트할 수 있습니다"); Sfx.Play("error"); return; }
            StartCoroutine(TestRoutine());
        }

        IEnumerator TestRoutine()
        {
            testing = true;
            bool ok = true;
            foreach (var p in item.parts) if (p.NeedsReplace) ok = false;
            Sfx.Play("click");
            yield return new WaitForSeconds(0.35f);
            if (!ok)
            {
                Sfx.Play("error");
                HUD.Toast("작동하지 않습니다 — 파손 부품을 교체하세요");
                testing = false;
                yield break;
            }
            float dur = 3.2f;
            switch (item.def.sound)
            {
                case TestSound.Hum: Sfx.Loop("static", 0.7f); break;
                case TestSound.Bell: Sfx.Play("bell", 0.8f); dur = 2.4f; break;
                case TestSound.Melody: Sfx.Play("melody", 1f); dur = 9f; break;
                case TestSound.Beep: Sfx.Play("beep", 0.8f); dur = 2.2f; break;
                case TestSound.Engine: Sfx.Play("clunk", 0.9f); Sfx.Loop("engine", 0.75f); dur = 3.6f; break;
                case TestSound.Arcade: Sfx.Play("arcade", 0.8f); dur = 3.0f; break;
                case TestSound.Typewriter: Sfx.Play("typing", 0.9f); dur = 2.4f; break;
                case TestSound.Record: Sfx.Play("record", 0.9f); dur = 4.2f; break;
                case TestSound.Watch: Sfx.Play("ticking", 0.9f); dur = 3.2f; break;
                case TestSound.Robot: Sfx.Play("robot", 0.9f); dur = 3.2f; break;
                default: Sfx.Play("clunk", 0.9f); Sfx.Loop("hum", 0.5f); break;
            }
            var root = item.root;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                foreach (var p in item.parts)
                {
                    switch (p.def.anim)
                    {
                        case AnimKind.Spin:
                            p.transform.RotateAround(root.TransformPoint(p.def.animPivot), root.TransformDirection(p.def.animAxis), p.def.animSpeed * Time.deltaTime);
                            break;
                        case AnimKind.Glow:
                            p.SetGlow(p.def.glow * (1.6f + 0.5f * Mathf.Sin(t * 9f)) * Mathf.Clamp01(t * 3f));
                            break;
                        case AnimKind.Wobble:
                            p.transform.localPosition = p.homePos + new Vector3(Mathf.Sin(t * 61f), Mathf.Sin(t * 47f), 0f) * 0.0016f;
                            break;
                    }
                }
                yield return null;
            }
            foreach (var p in item.parts)
            {
                if (p.def.anim == AnimKind.None) continue;
                p.transform.localPosition = p.homePos;
                p.transform.localRotation = Quaternion.identity;
                p.SetGlow(Color.black);
            }
            Sfx.StopLoops();
            Sfx.Play("chime", 0.8f);
            item.tested = true;
            testing = false;
            HUD.Toast("작동 테스트 성공! 납품 보너스 +15%");
            HUD.I?.OnItemChanged();
        }

        public WorkStats Stats()
        {
            var st = new WorkStats();
            if (item == null) return st;
            long r = 0, r0 = 0, d = 0, d0 = 0, pt = 0, ptMax = 0;
            foreach (var p in item.parts)
            {
                if (!p.def.isBody) { st.detachable++; if (!p.attached) st.detached++; else st.attached++; }
                if (p.condition == Condition.Broken || p.replaced) { st.broken++; if (p.replaced) st.replaced++; }
                r += p.RustSum; r0 += p.Rust0;
                d += p.DustSum; d0 += p.Dust0;
                pt += p.PaintSum; ptMax += p.PaintMax;
            }
            foreach (var s in item.screws)
            {
                st.screws++;
                if (s.onTray) st.screwsOut++;
                if (s.FullyIn) st.screwsIn++;
            }
            st.hasRust = r0 > 0; st.hasDust = d0 > 0; st.hasPaint = ptMax > 0;
            st.rustDone = r0 > 0 ? 1f - (float)r / r0 : 1f;
            st.dustDone = d0 > 0 ? 1f - (float)d / d0 : 1f;
            st.paintDone = ptMax > 0 ? (float)pt / ptMax : 1f;
            st.assembled = st.attached == st.detachable && st.screwsIn == st.screws;
            return st;
        }

        public ScoreInfo Score()
        {
            var st = Stats();
            var sc = new ScoreInfo
            {
                itemName = item.def.name,
                rust = st.rustDone, dust = st.dustDone, paint = st.paintDone,
                parts = st.broken > 0 ? (float)st.replaced / st.broken : 1f,
                assembly = (st.attached + st.screwsIn) / (float)Mathf.Max(1, st.detachable + st.screws),
                tested = item.tested,
                auction = item.offer.auction,
                reward = item.offer.reward, cost = item.offer.cost, partsCost = item.spentOnParts,
            };
            sc.total = Mathf.RoundToInt(100f * (0.25f * sc.rust + 0.2f * sc.dust + 0.2f * sc.paint + 0.15f * sc.parts + 0.2f * sc.assembly));
            float q = Mathf.Pow(sc.total / 100f, 1.5f);
            sc.payout = Mathf.RoundToInt(sc.reward * q * (sc.tested ? 1.15f : 1f));
            sc.stars = sc.total >= 95 ? 3 : sc.total >= 80 ? 2 : sc.total >= 60 ? 1 : 0;
            return sc;
        }

        public void Deliver()
        {
            if (item == null || Busy) return;
            var st = Stats();
            if (!st.assembled) { HUD.Toast("조립이 끝나지 않았습니다 — 부품과 나사를 모두 제자리에!"); Sfx.Play("error"); return; }
            var sc = Score();
            G.Earn(sc.payout, sc.stars);
            G.Record(item.def.id, sc.total);
            if (item.def.tier == 4 && sc.total >= G.RelicScore) sc.relic = true;
            if (item.def.tier == 4 && G.RelicsDone == G.Relics.Count && !G.data.ended)
            {
                G.data.ended = true;
                sc.ending = true;
            }
            G.Save();
            Sfx.Play("cash");
            Clear();
            HUD.I?.ShowResult(sc);
        }

        public void Abandon()
        {
            if (item == null) return;
            Clear();
            HUD.Toast("의뢰를 포기했습니다");
            HUD.I?.ShowBoard();
        }

        // ───────────────────────── 입력 ─────────────────────────

        void Update()
        {
            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            Room.FocusAt(zoom);
            cam.transform.position = Target + CamDir * zoom + Fx.ShakeOffset;
            cam.transform.rotation = Quaternion.LookRotation(Target - (Target + CamDir * zoom), Vector3.up);

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (item == null || mouse == null || kb == null) { HUD.Tip(null); return; }

            Vector2 mp = mouse.position.ReadValue();
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (mouse.rightButton.wasPressedThisFrame && !overUI) rotating = true;
            if (!mouse.rightButton.isPressed) rotating = false;
            if (rotating)
            {
                Vector2 dlt = mouse.delta.ReadValue();
                item.root.Rotate(Vector3.up, -dlt.x * 0.35f, Space.World);
                item.root.Rotate(cam.transform.right, dlt.y * 0.35f, Space.World);
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (!overUI && Mathf.Abs(scroll) > 0.01f)
                zoomTarget = Mathf.Clamp(zoomTarget * (1f - Mathf.Sign(scroll) * 0.12f), 0.38f, 1.9f);

            if (kb.digit1Key.wasPressedThisFrame) SetTool(Tool.Hand);
            if (kb.digit2Key.wasPressedThisFrame) SetTool(Tool.Driver);
            if (kb.digit3Key.wasPressedThisFrame) SetTool(Tool.Sander);
            if (kb.digit4Key.wasPressedThisFrame) SetTool(Tool.Cloth);
            if (kb.digit5Key.wasPressedThisFrame) SetTool(Tool.Spray);
            xray = kb.tabKey.isPressed || forceXray;
            ApplyXray();

            foreach (var s in item.screws) s.driven = false;
            float sand = 0f, cloth = 0f, spray = 0f, drill = 0f;
            string tip = null;

            if (dragPart != null) UpdateDrag(mouse, mp);
            else if (!Busy && !overUI && !rotating)
            {
                Pick(mp, out Part part, out Screw screw, out RaycastHit hit);
                SetHover(part, screw);
                if (tool == Tool.Hand) tip = HandleHand(mouse, mp, part);
                else if (tool == Tool.Driver) tip = HandleDriver(mouse, screw, part, ref drill);
                else tip = HandleBrush(mouse, part, hit, ref sand, ref cloth, ref spray);
            }
            else SetHover(null, null);

            // 손을 뗀 나사는 원위치로
            foreach (var s in item.screws)
                if (!s.onTray && !s.tightening && !s.driven && s.progress > 0f)
                {
                    s.progress = Mathf.Max(0f, s.progress - Time.deltaTime * 2f);
                    s.ApplyPose();
                }

            if (!Busy)
            {
                Sfx.Loop("sand", sand, G.Lv(Tool.Sander) == 1 ? 1f : 1.3f);
                Sfx.Loop("cloth", cloth);
                Sfx.Loop("spray", spray);
                Sfx.Loop("drill", drill, G.Lv(Tool.Driver) == 3 ? 1.35f : 1f);
            }
            HUD.Tip(tip);
        }

        void LateUpdate()
        {
            if (item == null) return;
            UpdateStand();
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
            foreach (var p in item.parts)
            {
                if (p.attached || p.busy || p == dragPart) continue;
                // 트레이 바닥에 닿도록 높이를 맞춘다 (회전에 따라 바뀌는 바닥면 기준)
                bool onTray = Mathf.Abs(Mathf.Abs(p.trayPos.x) - 0.6f) < 0.31f && p.trayPos.z > -0.34f && p.trayPos.z < 0.28f;
                p.trayPos.y = (onTray ? Room.TrayTop : Room.BenchTop) + 0.001f + (p.transform.position.y - p.mr.bounds.min.y);
                p.transform.position = Vector3.Lerp(p.transform.position, p.trayPos, k);
                p.transform.rotation = item.root.rotation;
            }
            foreach (var s in item.screws)
            {
                bool showHole = s.onTray && s.owner.attached && tool == Tool.Driver && !xray;
                if (s.hole.activeSelf != showHole) s.hole.SetActive(showHole);
                if (!s.onTray) continue;
                s.transform.position = Vector3.Lerp(s.transform.position, s.trayPos, k);
                s.transform.rotation = Quaternion.Slerp(s.transform.rotation, s.trayRot, k);
            }
        }

        /// <summary>거치대가 물건의 실제 가장 낮은 점을 받치도록 매 프레임 높이를 맞춘다.</summary>
        void UpdateStand()
        {
            float minY = float.MaxValue;
            foreach (var p in item.parts)
            {
                if (!p.attached || p.busy) continue;
                var m = p.transform.localToWorldMatrix;
                foreach (var v in p.verts)
                {
                    float y = m.m10 * v.x + m.m11 * v.y + m.m12 * v.z + m.m13;
                    if (y < minY) minY = y;
                }
            }
            if (minY < float.MaxValue) Room.SetStand(Mathf.Max(minY - 0.0015f, Room.BenchTop + 0.03f));
        }

        void Pick(Vector2 mp, out Part part, out Screw screw, out RaycastHit hit)
        {
            part = null; screw = null; hit = default;
            Ray ray = cam.ScreenPointToRay(mp);
            int mask = tool == Tool.Driver ? ~0 : 1;
            var hits = Physics.RaycastAll(ray, 6f, mask);
            if (hits.Length == 0) return;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            hit = hits[0];
            if (tool == Tool.Driver)
            {
                foreach (var h in hits)
                {
                    if (h.distance > hits[0].distance + 0.02f) break;
                    if (screwByCol.TryGetValue(h.collider, out var s)) { screw = s; hit = h; return; }
                }
            }
            part = hit.collider.GetComponent<Part>();
        }

        void SetHover(Part p, Screw s)
        {
            if (hoverPart != p)
            {
                if (hoverPart) hoverPart.SetHover(Color.black);
                hoverPart = p;
            }
            if (hoverPart)
            {
                bool good = tool != Tool.Hand || !hoverPart.attached || CanDetach(hoverPart, out _);
                hoverPart.SetHover(tool == Tool.Hand ? (good ? new Color(0.1f, 0.14f, 0.06f) : new Color(0.14f, 0.03f, 0.02f)) : new Color(0.03f, 0.03f, 0.03f));
            }
            if (hoverScrew != s)
            {
                if (hoverScrew) hoverScrew.mat.SetColor("_Emission", Color.black);
                hoverScrew = s;
                if (hoverScrew) hoverScrew.mat.SetColor("_Emission", new Color(0.5f, 0.4f, 0.05f));
            }
        }

        string Describe(Part p)
        {
            if (p == null) return null;
            string s = p.def.name;
            if (p.NeedsReplace) s += p.ordered ? "  ·  <color=#7fd4ff>교체 부품 배송 중</color>" : $"  ·  <color=#ff6a5a>파손 (교체 {p.def.price}G)</color>";
            else if (p.replaced) s += "  ·  <color=#8fe08f>새 부품</color>";
            if (p.HasRust && p.RustLeft > 0f) s += $"  ·  녹 {Mathf.CeilToInt(p.RustLeft * 100f)}%";
            if (p.HasDust && p.DustLeft > 0f) s += $"  ·  먼지 {Mathf.CeilToInt(p.DustLeft * 100f)}%";
            if (p.HasPaint && p.PaintDone < 1f) s += $"  ·  도색 {Mathf.FloorToInt(p.PaintDone * 100f)}%";
            return s;
        }

        string HandleHand(Mouse mouse, Vector2 mp, Part part)
        {
            if (part == null) return null;
            string tip = Describe(part);
            if (part.attached) { if (!CanDetach(part, out string why) && !string.IsNullOrEmpty(why)) tip += "\n<size=20>" + why + "</size>"; else tip += "\n<size=20>클릭: 분리</size>"; }
            else tip += "\n<size=20>드래그 또는 클릭: 조립</size>";
            if (mouse.leftButton.wasPressedThisFrame && !part.busy)
            {
                if (part.attached)
                {
                    if (CanDetach(part, out string why)) Detach(part);
                    else if (!string.IsNullOrEmpty(why)) { HUD.Toast(why); Sfx.Play("error", 0.6f); }
                }
                else
                {
                    dragPart = part;
                    dragStart = mp;
                    dragMoved = false;
                    ghostFilter.sharedMesh = part.mesh;
                    ghost.transform.SetParent(item.root, false);
                    ghost.transform.localPosition = part.homePos;
                    ghost.transform.localRotation = Quaternion.identity;
                    ghost.SetActive(true);
                }
            }
            return tip;
        }

        void UpdateDrag(Mouse mouse, Vector2 mp)
        {
            var p = dragPart;
            Vector3 homeW = item.root.TransformPoint(p.homePos);
            if ((mp - dragStart).sqrMagnitude > 49f) dragMoved = true;
            if (dragMoved)
            {
                var plane = new Plane(-cam.transform.forward, homeW);
                Ray ray = cam.ScreenPointToRay(mp);
                if (plane.Raycast(ray, out float dist)) p.transform.position = ray.GetPoint(dist);
                p.transform.rotation = item.root.rotation;
            }
            Vector2 hs = cam.WorldToScreenPoint(homeW);
            bool near = (hs - mp).magnitude < 90f * Screen.height / 1080f;
            if (mouse.leftButton.isPressed) return;
            dragPart = null;
            ghost.SetActive(false);
            if (!dragMoved || near)
            {
                if (CanAttach(p, out string why)) Attach(p);
                else if (!string.IsNullOrEmpty(why)) { HUD.Toast(why); Sfx.Play("error", 0.6f); }
            }
        }

        string HandleDriver(Mouse mouse, Screw screw, Part part, ref float drill)
        {
            if (screw == null) return part ? Describe(part) : null;
            string tip = screw.onTray ? "빈 나사 구멍\n<size=20>꾹 눌러 조이기</size>" : "나사\n<size=20>꾹 눌러 풀기</size>";
            if (!mouse.leftButton.isPressed) return tip;
            if (!screw.owner.attached) return tip;
            bool tighten = screw.onTray || screw.tightening;
            float before = screw.progress;
            Vector3 center = item.root.TransformPoint(screw.homePos);
            float radius = G.ScrewRadius;
            var targets = new List<Screw> { screw };
            if (radius > 0f)
                foreach (var s in item.screws)
                {
                    if (s == screw || !s.owner.attached) continue;
                    if ((s.onTray || s.tightening) != tighten) continue;
                    if (!tighten && !ScrewReachable(s)) continue;
                    if ((item.root.TransformPoint(s.homePos) - center).sqrMagnitude < radius * radius) targets.Add(s);
                }
            foreach (var s in targets) Drive(s, Time.deltaTime);
            if (G.Lv(Tool.Driver) >= 2) drill = 0.5f;
            else
            {
                tickTimer -= Time.deltaTime;
                if (tickTimer <= 0f) { tickTimer = 0.16f; Sfx.Play("tick", 0.55f, Random.Range(0.85f, 1.15f)); }
            }
            return tip;
        }

        string HandleBrush(Mouse mouse, Part part, RaycastHit hit, ref float sand, ref float cloth, ref float spray)
        {
            if (part == null) return null;
            string tip = Describe(part);
            if (!mouse.leftButton.isPressed) return tip;
            int ch = tool == Tool.Sander ? 0 : tool == Tool.Cloth ? 1 : 2;
            float r = G.BrushRadius(tool);
            var res = PaintAt(hit.point, part, ch, r, G.BrushPower(tool) * Time.deltaTime);
            bool active = res == PaintResult.Changed;
            if (tool == Tool.Sander)
            {
                sand = active ? 0.55f : 0.12f;
                if (active) Fx.Dust(hit.point + hit.normal * 0.004f, new Color(0.6f, 0.3f, 0.12f, 0.9f), 3);
            }
            else if (tool == Tool.Cloth)
            {
                cloth = active ? 0.6f : 0.15f;
                if (active && Random.value < 0.5f) Fx.Dust(hit.point + hit.normal * 0.004f, new Color(0.75f, 0.72f, 0.65f, 0.6f), 1);
            }
            else
            {
                spray = 0.45f;
                Color c = paintColor ?? new Color(0.9f, 0.9f, 0.9f);
                c.a = 0.35f;
                Fx.Mist(Vector3.Lerp(hit.point, cam.transform.position, 0.04f), c, 2);
                if (res == PaintResult.Blocked && Time.time > blockedToastTime)
                {
                    blockedToastTime = Time.time + 2.5f;
                    HUD.Toast("녹과 먼지를 먼저 제거해야 도색할 수 있습니다");
                }
            }
            return tip;
        }

        /// <summary>월드 좌표 wp에 브러시를 적용한다. 넓은 브러시는 주변 부품에도 닿는다.</summary>
        public PaintResult PaintAt(Vector3 wp, Part main, int ch, float r, float amount)
        {
            var result = PaintResult.None;
            foreach (var p in item.parts)
            {
                if (p != main && (r < 0.03f || p.mr.bounds.SqrDistance(wp) > r * r)) continue;
                if (!p.HasMask) continue;
                float before = ch == 0 ? p.RustLeft : ch == 1 ? p.DustLeft : 1f - p.PaintDone;
                if (ch == 2) p.SetPaintColor(paintColor);
                var pr = p.Paint(ch, p.transform.InverseTransformPoint(wp), r / item.scale, amount);
                if (pr == PaintResult.Changed) result = PaintResult.Changed;
                else if (pr == PaintResult.Blocked && result == PaintResult.None) result = PaintResult.Blocked;
                float after = ch == 0 ? p.RustLeft : ch == 1 ? p.DustLeft : 1f - p.PaintDone;
                if (before > 0f && after <= 0f)
                {
                    Sfx.Play("sparkle", 0.7f);
                    Fx.Spark(p.mr.bounds.center, ch == 2 ? new Color(1f, 1f, 1f) : new Color(1f, 0.9f, 0.5f), 18, Mathf.Min(p.radius, 0.06f));
                    HUD.I?.OnItemChanged();
                }
            }
            return result;
        }

        void ApplyXray()
        {
            if (!xray && !lastXray) return;
            foreach (var p in item.parts)
                p.SetXray(!xray ? null : p.NeedsReplace ? xrRed : p.RustLeft > 0.02f ? xrOrange : xrBase);
            foreach (var s in item.screws)
                s.mr.sharedMaterial = !xray ? s.mat : (!s.onTray && ScrewReachable(s)) ? xrYellow : xrBase;
            lastXray = xray;
        }
    }
}
