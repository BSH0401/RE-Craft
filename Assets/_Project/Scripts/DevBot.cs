using System.Collections;
using System.IO;
using UnityEngine;

namespace ReCraft
{
    /// <summary>
    /// 개발용 자동 플레이 봇. 실행 인자:
    /// -autoplay [-jobs N] [-item id] : 의뢰 수주 → 분해 → 복원 → 조립 → 테스트 → 납품을 자동 수행
    /// -showcase : 모든 아이템을 조립/분해 상태로 촬영
    /// -shots dir : 단계별 스크린샷 저장, -quitafter : 끝나면 종료
    /// </summary>
    public class DevBot : MonoBehaviour
    {
        public static bool Active;
        string shotDir;
        int shotIdx;

        void Start()
        {
            Active = true;
            shotDir = Boot.Arg("-shots");
            if (shotDir != null) Directory.CreateDirectory(shotDir);
            StartCoroutine(Boot.Flag("-showcase") ? Showcase() : AutoPlay());
        }

        IEnumerator Shot(string name)
        {
            if (shotDir == null) yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, $"{shotIdx++:00}_{name}.png"));
            yield return null;
            yield return null;
        }

        static WaitForSecondsRealtime Wait(float s) => new(s);

        IEnumerator Disassemble(Workbench wb)
        {
            bool progress = true;
            while (progress)
            {
                progress = false;
                foreach (var p in wb.item.parts)
                {
                    if (p.def.isBody || !p.attached) continue;
                    bool blocked = false;
                    foreach (string id in p.def.blockedBy) { var q = wb.item.Get(id); if (q != null && q.attached) blocked = true; }
                    if (blocked) continue;
                    foreach (var s in p.screws) if (!s.onTray) wb.ScrewOut(s, true);
                    if (wb.CanDetach(p, out _)) { wb.Detach(p); progress = true; yield return Wait(0.05f); }
                }
            }
            foreach (var p in wb.item.parts)
                if (!p.def.isBody && p.attached) Debug.LogError($"[Dev] STUCK part {wb.item.def.id}/{p.def.id}");
        }

        IEnumerator Reassemble(Workbench wb)
        {
            bool progress = true;
            while (progress)
            {
                progress = false;
                foreach (var p in wb.item.parts)
                {
                    if (p.attached) continue;
                    if (!wb.CanAttach(p, out _)) continue;
                    wb.Attach(p, true);
                    foreach (var s in p.screws) wb.ScrewIn(s, true);
                    progress = true;
                    yield return Wait(0.05f);
                }
            }
            if (!wb.Stats().assembled) Debug.LogError($"[Dev] NOT ASSEMBLED {wb.item.def.id}");
        }

        IEnumerator AutoPlay()
        {
            var wb = Workbench.I;
            int jobs = (int)Boot.ArgF("-jobs", 1);
            string forced = Boot.Arg("-item");
            bool relics = Boot.Flag("-relics");   // 게시판의 전설 의뢰만 연속으로 → 엔딩 확인
            if (relics) jobs = G.Relics.Count;
            HUD.I.ShowBoard();
            yield return Wait(1.0f);
            yield return Shot("board");
            for (int j = 0; j < jobs; j++)
            {
                if (HUD.I.OverlayName != "Board") HUD.I.ShowBoard();
                yield return Wait(0.3f);
                Offer offer = HUD.I.offers[j % HUD.I.offers.Count];
                if (relics) offer = HUD.I.offers.Find(o => o.item.tier == 4) ?? offer;
                if (forced != null) offer = new Offer { item = Items.Find(forced), severity = 0.7f, reward = Items.Find(forced).value, seed = 12345 + j };
                HUD.I.Accept(offer);
                if (wb.item == null) { Debug.LogError("[Dev] accept failed"); break; }
                yield return Wait(0.8f);
                yield return Shot("start");
                for (int f = 0; f < 20; f++) { wb.item.root.Rotate(Vector3.up, 6f, Space.World); yield return null; }
                yield return Shot("rotated");
                wb.forceXray = true;
                yield return Wait(0.3f);
                yield return Shot("xray");
                wb.forceXray = false;

                yield return Disassemble(wb);
                yield return Wait(1.0f);
                yield return Shot("disassembled");

                foreach (var p in wb.item.parts)
                    if (p.NeedsReplace) { wb.Order(p); if (!p.ordered) wb.ReplaceNow(p); }
                yield return Wait(2.8f);

                // 실제 브러시 경로 검증
                foreach (var p in wb.item.parts)
                {
                    if (!p.HasRust) continue;
                    float before = p.RustLeft;
                    var verts = p.mesh.vertices;
                    for (int k = 0; k < verts.Length; k += Mathf.Max(1, verts.Length / 12))
                        for (int n = 0; n < 6; n++)
                            wb.PaintAt(p.transform.TransformPoint(verts[k]), p, 0, 0.03f, 0.3f);
                    Debug.Log($"[Dev] brush {p.def.id} rust {before:0.00} -> {p.RustLeft:0.00}");
                    break;
                }
                yield return Shot("brushed");
                foreach (var p in wb.item.parts) p.StepAll(0, 1f);
                yield return Shot("derusted");
                foreach (var p in wb.item.parts) p.StepAll(1, 1f);
                yield return Shot("cleaned");
                foreach (var p in wb.item.parts) { p.SetPaintColor(null); p.StepAll(2, 1f); }
                yield return Shot("painted");

                yield return Reassemble(wb);
                yield return Wait(0.6f);
                yield return Shot("assembled");
                wb.RunTest();
                yield return Wait(1.6f);
                yield return Shot("testing");
                while (wb.testing) yield return null;
                var sc = wb.Score();
                wb.Deliver();
                Debug.Log($"[Dev] job {j} item={sc.itemName} score={sc.total} payout={sc.payout} stars={sc.stars} gold={G.data.gold} totalStars={G.data.stars} tested={sc.tested}");
                yield return Wait(0.5f);
                yield return Shot("result");
                if (relics) Debug.Log($"[Dev] relics {G.RelicsDone}/{G.Relics.Count} ended={G.data.ended}");
            }
            if (relics && G.data.ended)
            {
                HUD.I.ShowEnding();
                yield return Wait(0.5f);
                yield return Shot("ending");
                Debug.Log("[Dev] ending shown");
            }
            HUD.I.ShowUpgrades();
            yield return Wait(0.3f);
            yield return Shot("upgrades");
            HUD.I.ShowCatalog();
            yield return Wait(0.3f);
            yield return Shot("catalog");
            Finish();
        }

        IEnumerator Showcase()
        {
            var wb = Workbench.I;
            string only = Boot.Arg("-item");
            yield return Wait(0.5f);
            foreach (var it in Items.All)
            {
                if (only != null && it.id != only) continue;
                wb.Spawn(new Offer { item = it, severity = 0.7f, reward = it.value, seed = 777 });
                yield return Wait(0.5f);
                yield return Shot(it.id + "_front");
                wb.item.root.rotation = Quaternion.Euler(-20f, 150f, 0f);
                yield return Wait(0.2f);
                yield return Shot(it.id + "_back");
                wb.item.root.rotation = Quaternion.Euler(8f, -24f, 0f);
                yield return Disassemble(wb);
                yield return Wait(1.0f);
                yield return Shot(it.id + "_parts");
                foreach (var p in wb.item.parts) { if (p.NeedsReplace) wb.ReplaceNow(p); p.StepAll(0, 1f); p.StepAll(1, 1f); p.SetPaintColor(null); p.StepAll(2, 1f); }
                yield return Reassemble(wb);
                yield return Wait(0.3f);
                yield return Shot(it.id + "_restored");
                Debug.Log($"[Dev] showcase {it.id} parts={it.parts.Count} screws={it.ScrewCount} score={wb.Score().total}");
            }
            Finish();
        }

        void Finish()
        {
            Debug.Log("[Dev] done");
            if (Boot.Flag("-quitafter")) Application.Quit();
        }
    }
}
