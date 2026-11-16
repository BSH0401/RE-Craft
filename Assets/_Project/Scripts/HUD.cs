using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReCraft
{
    /// <summary>HUD + 의뢰 게시판 / 업그레이드 상점 / 도감 / 정산 / 엔딩 화면.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD I;
        Canvas canvas;
        Text goldText, starText, itemText, stageText, hintText, toastText, tipText;
        RectTransform tipRect, workRoot, toolbar, palette, actions, brokenList, overlay;
        Image toastBg;
        readonly Button[] toolBtns = new Button[5];
        Button testBtn, deliverBtn, ultraBtn, boothBtn;
        float toastTime, refreshTimer;
        string tipValue;
        public List<Offer> offers;
        readonly System.Random rng = new();
        public string OverlayName { get; private set; }
        public Transform Root => canvas.transform;
        public Image ToolImage(int i) => toolBtns[i].GetComponent<Image>();

        static readonly Color[] Palette =
        {
            new(0.8f, 0.12f, 0.1f), new(0.95f, 0.55f, 0.1f), new(0.95f, 0.8f, 0.15f), new(0.2f, 0.6f, 0.3f),
            new(0.1f, 0.55f, 0.6f), new(0.15f, 0.3f, 0.7f), new(0.5f, 0.25f, 0.6f), new(0.92f, 0.9f, 0.85f), new(0.12f, 0.12f, 0.13f),
        };

        static Vector2 V(float x, float y) => new(x, y);

        void Awake()
        {
            I = this;
            canvas = UI.Canvas("HUD", 10);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            // 상단 바
            var top = UI.Panel(root, "Top", V(0.5f, 1), V(0.5f, 1), V(0, 0), V(1920, 64), UI.Bg).transform;
            UI.Label(top, "<b>RE:CRAFT</b>", 30, UI.Accent, TextAnchor.MiddleLeft, V(0, 0.5f), V(0, 0.5f), V(24, 0), V(220, 60));
            goldText = UI.Label(top, "", 26, UI.TextCol, TextAnchor.MiddleLeft, V(0, 0.5f), V(0, 0.5f), V(240, 0), V(220, 60));
            starText = UI.Label(top, "", 26, UI.TextCol, TextAnchor.MiddleLeft, V(0, 0.5f), V(0, 0.5f), V(450, 0), V(200, 60));
            itemText = UI.Label(top, "", 26, UI.TextCol, TextAnchor.MiddleCenter, V(0.5f, 0.5f), UI.Center, V(60, 0), V(700, 60));
            UI.Btn(top, "도감", V(1, 0.5f), V(1, 0.5f), V(-20, 0), V(120, 46), ShowCatalog);
            UI.Btn(top, "업그레이드", V(1, 0.5f), V(1, 0.5f), V(-150, 0), V(160, 46), ShowUpgrades);

            // 작업 중 UI
            workRoot = UI.Stretch(root, "Work");
            var stage = UI.Panel(workRoot, "Stage", V(0.5f, 1), V(0.5f, 1), V(0, -72), V(1180, 44), new Color(0.07f, 0.065f, 0.06f, 0.8f), false).transform;
            stageText = UI.Label(stage, "", 22, UI.TextCol, TextAnchor.MiddleCenter, UI.Center, UI.Center, Vector2.zero, V(1170, 40));

            actions = UI.Rect(workRoot, "Actions", V(1, 1), V(1, 1), V(-20, -130), V(210, 400));
            testBtn = UI.Btn(actions, "작동 테스트", V(0.5f, 1), V(0.5f, 1), V(0, 0), V(210, 52), () => Workbench.I.RunTest());
            deliverBtn = UI.Btn(actions, "납품하기", V(0.5f, 1), V(0.5f, 1), V(0, -60), V(210, 52), () => Workbench.I.Deliver(), new Color(0.2f, 0.42f, 0.22f, 0.96f));
            ultraBtn = UI.Btn(actions, "초음파 세척", V(0.5f, 1), V(0.5f, 1), V(0, -130), V(210, 46), () => Workbench.I.UltrasonicClean(), new Color(0.15f, 0.3f, 0.45f, 0.96f), 22);
            boothBtn = UI.Btn(actions, "자동 도색", V(0.5f, 1), V(0.5f, 1), V(0, -182), V(210, 46), () => Workbench.I.AutoPaint(), new Color(0.4f, 0.2f, 0.4f, 0.96f), 22);
            UI.Btn(actions, "의뢰 포기", V(0.5f, 1), V(0.5f, 1), V(0, -250), V(210, 40), () => Workbench.I.Abandon(), new Color(0.3f, 0.12f, 0.1f, 0.9f), 20);

            brokenList = UI.Rect(workRoot, "Broken", V(0, 1), V(0, 1), V(20, -130), V(330, 300));

            toolbar = UI.Rect(workRoot, "Toolbar", V(0.5f, 0), V(0.5f, 0), V(0, 16), V(5 * 172, 84));
            for (int i = 0; i < 5; i++)
            {
                Tool t = (Tool)i;
                toolBtns[i] = UI.Btn(toolbar, "", V(0, 0), V(0, 0), V(i * 172, 0), V(164, 84), () => Workbench.I.SetTool(t), null, 20);
            }
            palette = UI.Rect(workRoot, "Palette", V(0.5f, 0), V(0.5f, 0), V(0, 108), V(700, 50));
            UI.Btn(palette, "원래 색", V(0, 0), V(0, 0), V(0, 0), V(110, 46), () => SetPaint(null), null, 20);
            for (int i = 0; i < Palette.Length; i++)
            {
                Color c = Palette[i];
                UI.Btn(palette, "", V(0, 0), V(0, 0), V(120 + i * 56, 0), V(48, 46), () => SetPaint(c), c);
            }

            var hintBg = UI.Panel(workRoot, "Hint", V(0, 0), V(0, 0), V(16, 16), V(400, 84), new Color(0.07f, 0.065f, 0.06f, 0.8f), false).transform;
            hintText = UI.Label(hintBg, "우클릭 드래그: 회전 · 휠: 줌\nTab: X-Ray 스캐너 · 1~5: 도구 선택", 20, UI.Dim, TextAnchor.MiddleLeft, V(0, 0.5f), V(0, 0.5f), V(14, 0), V(380, 80));

            overlay = UI.Stretch(root, "Overlay");

            // 토스트 / 툴팁
            toastBg = UI.Panel(root, "Toast", V(0.5f, 0), V(0.5f, 0), V(0, 180), V(760, 54), UI.Bg, false);
            toastText = UI.Label(toastBg.transform, "", 24, UI.TextCol, TextAnchor.MiddleCenter, UI.Center, UI.Center, Vector2.zero, V(740, 50));
            toastBg.gameObject.SetActive(false);
            var tipBg = UI.Panel(root, "Tip", V(0, 0), V(0, 1), Vector2.zero, V(420, 70), new Color(0.05f, 0.05f, 0.05f, 0.88f), false);
            tipRect = tipBg.rectTransform;
            tipText = UI.Label(tipRect, "", 22, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(12, -8), V(400, 60));
            tipRect.gameObject.SetActive(false);

            G.Changed += RefreshTop;
            RefreshTop();
            OnToolChanged();
            OnItemChanged();
        }

        void OnDestroy() => G.Changed -= RefreshTop;

        void SetPaint(Color? c)
        {
            Workbench.I.paintColor = c;
            Toast(c.HasValue ? "선택한 색으로 도색합니다" : "원래 색으로 복원 도색합니다");
        }

        void RefreshTop()
        {
            goldText.text = $"<color=#ffd24a>●</color> {G.data.gold} G";
            starText.text = $"<color=#ffc24a>★</color> {G.data.stars}";
        }

        public void OnToolChanged()
        {
            var wb = Workbench.I;
            if (!wb) return;
            for (int i = 0; i < 5; i++)
            {
                Tool t = (Tool)i;
                bool sel = wb.tool == t;
                toolBtns[i].GetComponent<Image>().color = sel ? new Color(0.5f, 0.34f, 0.1f, 0.98f) : UI.BgLight;
                string lv = i == 0 ? "" : $" <color=#ffc24a>Lv.{G.Lv(t)}</color>";
                UI.TextOf(toolBtns[i]).text = $"<b>{i + 1}  {G.ToolNames[i]}</b>{lv}\n<size=16><color=#b8b0a4>{G.ToolName(t)}</color></size>";
            }
            palette.gameObject.SetActive(wb.tool == Tool.Spray && wb.item != null);
        }

        public void OnItemChanged()
        {
            var wb = Workbench.I;
            bool has = wb && wb.item != null;
            workRoot.gameObject.SetActive(has);
            if (!has) { itemText.text = ""; return; }
            OnToolChanged();
            // 파손 부품 목록
            foreach (Transform c in brokenList) Destroy(c.gameObject);
            int row = 0;
            foreach (var p in wb.item.parts)
            {
                if (p.condition != Condition.Broken && !p.replaced) continue;
                var bg = UI.Panel(brokenList, "Row", V(0, 1), V(0, 1), V(0, -row * 58), V(330, 52), new Color(0.07f, 0.065f, 0.06f, 0.85f), false).transform;
                string state = p.replaced ? "<color=#8fe08f>교체 완료</color>" : p.ordered ? "<color=#7fd4ff>배송 중...</color>" : p.attached ? "<color=#ff6a5a>파손 · 분리 필요</color>" : "<color=#ff6a5a>파손</color>";
                UI.Label(bg, $"{p.def.name}\n<size=17>{state}</size>", 20, UI.TextCol, TextAnchor.MiddleLeft, V(0, 0.5f), V(0, 0.5f), V(12, 0), V(210, 50));
                if (p.NeedsReplace && !p.ordered && !p.attached)
                {
                    Part part = p;
                    UI.Btn(bg, $"주문 {p.def.price}G", V(1, 0.5f), V(1, 0.5f), V(-6, 0), V(112, 40), () => Workbench.I.Order(part), new Color(0.2f, 0.35f, 0.5f, 0.96f), 19);
                }
                row++;
            }
            RefreshWork();
        }

        void RefreshWork()
        {
            var wb = Workbench.I;
            if (!wb || wb.item == null) return;
            var st = wb.Stats();
            var sc = wb.Score();
            itemText.text = $"{wb.item.def.name}   <color=#ffc24a>복원도 {sc.total}%</color>";
            string P(float v) => v >= 0.999f ? "<color=#8fe08f>완료</color>" : Mathf.FloorToInt(v * 100f) + "%";
            var sb = new System.Text.StringBuilder();
            sb.Append($"분해 {st.detached}/{st.detachable}");
            if (st.hasRust) sb.Append($"     녹 제거 {P(st.rustDone)}");
            if (st.hasDust) sb.Append($"     세척 {P(st.dustDone)}");
            if (st.hasPaint) sb.Append($"     도색 {P(st.paintDone)}");
            if (st.broken > 0) sb.Append($"     부품 교체 {st.replaced}/{st.broken}");
            sb.Append($"     조립 {st.attached}/{st.detachable}");
            if (st.screws > 0) sb.Append($" · 나사 {st.screwsIn}/{st.screws}");
            stageText.text = sb.ToString();
            testBtn.interactable = st.assembled && !wb.Busy;
            UI.TextOf(testBtn).text = wb.item.tested ? "작동 테스트 ✔" : "작동 테스트";
            deliverBtn.interactable = st.assembled && !wb.Busy;
            ultraBtn.gameObject.SetActive(G.Lv(Tool.Cloth) >= 3);
            boothBtn.gameObject.SetActive(G.Lv(Tool.Spray) >= 3);
            ultraBtn.interactable = boothBtn.interactable = !wb.Busy;
        }

        void Update()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f) { refreshTimer = 0.15f; RefreshWork(); }
            if (toastTime > 0f)
            {
                toastTime -= Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(toastTime / 0.4f);
                toastBg.color = new Color(UI.Bg.r, UI.Bg.g, UI.Bg.b, UI.Bg.a * a);
                toastText.color = new Color(UI.TextCol.r, UI.TextCol.g, UI.TextCol.b, a);
                if (toastTime <= 0f) toastBg.gameObject.SetActive(false);
            }
            bool show = !string.IsNullOrEmpty(tipValue) && OverlayName == null;
            if (tipRect.gameObject.activeSelf != show) tipRect.gameObject.SetActive(show);
            if (show && Mouse.current != null)
            {
                tipText.text = tipValue;
                float w = Mathf.Min(520f, tipText.preferredWidth + 28f), h = tipText.preferredHeight + 18f;
                tipRect.sizeDelta = V(w, h);
                Vector2 mp = Mouse.current.position.ReadValue() / canvas.scaleFactor;
                float cw = Screen.width / canvas.scaleFactor;
                tipRect.anchoredPosition = V(Mathf.Min(mp.x + 22f, cw - w - 8f), Mathf.Max(mp.y - 22f, h + 8f));
            }
        }

        public static void Toast(string msg)
        {
            if (!I) return;
            I.toastText.text = msg;
            I.toastBg.gameObject.SetActive(true);
            I.toastTime = 2.4f;
            if (DevBot.Active) Debug.Log("[Toast] " + msg);
        }

        public static void Tip(string s)
        {
            if (I) I.tipValue = s;
        }

        // ───────────────────────── 오버레이 ─────────────────────────

        Transform OpenOverlay(string name, Vector2 size, string title)
        {
            CloseOverlay();
            OverlayName = name;
            toastTime = 0f;
            toastBg.gameObject.SetActive(false);
            var dim = UI.Stretch(overlay, "Dim").gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.55f);
            var panel = UI.Panel(overlay, name, UI.Center, UI.Center, V(0, -10), size, UI.Bg).transform;
            UI.Panel(panel, "Bar", V(0.5f, 1), V(0.5f, 1), Vector2.zero, V(size.x, 6), UI.Accent, false);
            UI.Label(panel, $"<b>{title}</b>", 34, UI.Accent, TextAnchor.MiddleLeft, V(0, 1), V(0, 1), V(32, -18), V(size.x - 64, 50));
            return panel;
        }

        public void CloseOverlay()
        {
            foreach (Transform c in overlay) Destroy(c.gameObject);
            OverlayName = null;
        }

        void CloseToBoardOrWork()
        {
            if (Workbench.I.item == null) ShowBoard();
            else CloseOverlay();
        }

        static string Severity(float s)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(s * 5f), 1, 5);
            return "<color=#ff8a5a>" + new string('●', n) + "</color><color=#555555>" + new string('●', 5 - n) + "</color>";
        }

        public void ShowBoard()
        {
            offers ??= G.MakeOffers(rng);
            var p = OpenOverlay("Board", V(1500, 800), "의뢰 게시판");
            UI.Label(p, "낡은 물건을 받아 분해 → 녹 제거 → 세척 → 도색 → 부품 교체 → 재조립해 납품하세요. 복원도가 높을수록 보수와 명성(★)이 커집니다.", 21, UI.Dim, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(32, -76), V(1430, 60));
            float cw = offers.Count > 3 ? 345f : 460f;
            for (int i = 0; i < offers.Count; i++)
            {
                var o = offers[i];
                bool final = o.item.tier == 4;
                var card = UI.Panel(p, "Card", V(0, 1), V(0, 1), V(32 + i * (cw + 16), -150), V(cw, 470), final ? new Color(0.3f, 0.2f, 0.06f, 0.98f) : UI.BgLight).transform;
                string kind = final ? "<color=#ffd24a>전설의 의뢰</color>" : o.auction ? "<color=#7fd4ff>폐품 경매</color>" : "<color=#8fe08f>복원 의뢰</color>";
                UI.Label(card, kind, 22, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(20, -16), V(cw - 40, 30));
                UI.Label(card, $"<b>{o.item.name}</b>", 32, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(20, -50), V(cw - 40, 44));
                UI.Label(card, o.item.blurb, 19, UI.Dim, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(20, -100), V(cw - 40, 60));
                string tier = final ? "Tier 4 · 희귀 유물" : o.item.tier == 1 ? "Tier 1 · 일상 잡화" : "Tier 2 · 정밀 기기";
                var entry = G.Entry(o.item.id);
                string info = $"{tier}\n부품 {o.item.parts.Count}개 · 나사 {o.item.ScrewCount}개\n손상도  {Severity(o.severity)}\n" +
                              (o.auction ? $"매입가  <color=#ff8a5a>-{o.cost} G</color>\n예상 판매가  <color=#ffd24a>{o.reward} G</color>" : $"보수  <color=#ffd24a>{o.reward} G</color>") +
                              (entry != null ? $"\n<size=18><color=#9a948a>최고 복원도 {entry.best}% · {entry.count}회 납품</color></size>" : "");
                UI.Label(card, info, 22, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(20, -180), V(cw - 40, 200));
                Offer offer = o;
                UI.Btn(card, o.auction ? $"낙찰받기 (-{o.cost} G)" : "수주하기", V(0.5f, 0), V(0.5f, 0), V(0, 20), V(cw - 40, 56), () => Accept(offer), new Color(0.5f, 0.34f, 0.1f, 0.98f));
            }
            string unlock = G.data.stars < G.Tier2Stars ? $"다음 해금: ★{G.Tier2Stars} — Tier 2 정밀 기기 의뢰"
                : G.data.stars < G.FinalStars ? $"다음 해금: ★{G.FinalStars} — 전설의 의뢰 (엔딩)"
                : G.data.ended ? "모든 의뢰를 해금했습니다. 도감 100%에 도전해 보세요!" : "전설의 의뢰가 도착했습니다!";
            UI.Label(p, unlock, 22, UI.Accent, TextAnchor.MiddleLeft, V(0, 0), V(0, 0), V(32, 96), V(900, 36));
            UI.Label(p, "조작: 우클릭 드래그 회전 · 휠 줌 · Tab X-Ray · 1 손(분리/조립) · 2 드라이버(꾹 누르기) · 3 녹 제거 · 4 세척 · 5 도색", 19, UI.Dim, TextAnchor.MiddleLeft, V(0, 0), V(0, 0), V(32, 30), V(1100, 60));
            UI.Btn(p, "업그레이드 상점", V(1, 0), V(1, 0), V(-32, 28), V(220, 56), ShowUpgrades);
            UI.Btn(p, "도감", V(1, 0), V(1, 0), V(-262, 28), V(130, 56), ShowCatalog);
            if (!Tutorial.Running)
                UI.Btn(p, "튜토리얼 다시 보기", V(1, 1), V(1, 1), V(-196, -20), V(200, 46), () => { G.data.tutorialStep = 0; G.Save(); ShowBoard(); }, null, 20);
            UI.Btn(p, "게임 종료", V(1, 1), V(1, 1), V(-32, -20), V(150, 46), () => { G.Save(); Application.Quit(); }, new Color(0.3f, 0.12f, 0.1f, 0.9f), 20);
        }

        public void Accept(Offer o)
        {
            if (o.cost > 0 && !G.Spend(o.cost)) { Toast($"골드가 부족합니다 ({o.cost} G 필요)"); Sfx.Play("error"); return; }
            offers = null;
            CloseOverlay();
            Workbench.I.Spawn(o);
            Sfx.Play("whoosh", 0.7f);
            Toast($"'{o.item.name}' 작업 시작 — Tab으로 X-Ray 스캔");
        }

        public void ShowUpgrades()
        {
            var p = OpenOverlay("Upgrades", V(1200, 800), "업그레이드 상점");
            UI.Label(p, $"보유 골드  <color=#ffd24a>{G.data.gold} G</color>", 24, UI.TextCol, TextAnchor.MiddleRight, V(1, 1), V(1, 1), V(-32, -22), V(400, 40));
            string[] desc =
            {
                "",
                "나사 1개 3초 → 0.5초 → 주변 나사 동시 해체",
                "좁은 면적 연마 → 넓은 면적 → 광역 즉시 제거",
                "직접 문지르기 → 고압 세척 → 통째로 일괄 세척 버튼",
                "단일 분사 → 빠른 분사·메탈릭 → 원클릭 전체 도색 버튼",
            };
            int row = 0;
            for (int i = 1; i < 5; i++)
            {
                Tool t = (Tool)i;
                int lv = G.Lv(t), cost = G.NextCost(t);
                string next = cost < 0 ? "<color=#8fe08f>최고 등급</color>" : $"→ {G.LevelName(t, lv + 1)}";
                UpgradeRow(p, row++, $"<b>{G.ToolNames[i]} 도구</b>  <color=#ffc24a>Lv.{lv}</color>  {G.LevelName(t, lv)}  {next}", desc[i], cost, () => G.UpgradeTool(t));
            }
            UpgradeRow(p, row++, "<b>부품 자동 발주 단말기</b>", "파손 부품을 분리하는 즉시 자동 주문 · 드론 배송", G.data.autoOrder ? -1 : G.AutoOrderCost,
                () => { if (!G.Spend(G.AutoOrderCost)) return false; G.data.autoOrder = true; G.Save(); return true; });
            UpgradeRow(p, row++, "<b>자석 나사 정리함</b>", "나사가 트레이에 자동 정렬 — 나사 작업 속도 +65%", G.data.magTray ? -1 : G.MagTrayCost,
                () => { if (!G.Spend(G.MagTrayCost)) return false; G.data.magTray = true; G.Save(); return true; });
            UI.Btn(p, "닫기", V(1, 0), V(1, 0), V(-32, 28), V(160, 56), CloseToBoardOrWork);
        }

        void UpgradeRow(Transform p, int row, string title, string desc, int cost, System.Func<bool> buy)
        {
            var bg = UI.Panel(p, "Row", V(0, 1), V(0, 1), V(32, -90 - row * 98), V(1136, 88), UI.BgLight, false).transform;
            UI.Label(bg, title, 24, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(20, -10), V(860, 34));
            UI.Label(bg, desc, 19, UI.Dim, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(20, -48), V(860, 30));
            if (cost < 0)
                UI.Label(bg, "<color=#8fe08f>보유 중</color>", 22, UI.TextCol, TextAnchor.MiddleCenter, V(1, 0.5f), V(1, 0.5f), V(-20, 0), V(200, 50));
            else
            {
                var b = UI.Btn(bg, $"구매  {cost} G", V(1, 0.5f), V(1, 0.5f), V(-20, 0), V(200, 54), () =>
                {
                    if (buy()) { Sfx.Play("cash", 0.8f); OnToolChanged(); ShowUpgrades(); }
                    else { Toast("골드가 부족합니다"); Sfx.Play("error"); }
                }, new Color(0.5f, 0.34f, 0.1f, 0.98f));
                b.interactable = G.data.gold >= cost;
            }
        }

        public void ShowCatalog()
        {
            var p = OpenOverlay("Catalog", V(1300, 800), "복원 도감");
            int done = 0;
            var all = Items.All;
            for (int i = 0; i < all.Count; i++)
            {
                var it = all[i];
                var e = G.Entry(it.id);
                if (e != null) done++;
                var card = UI.Panel(p, "Card", V(0, 1), V(0, 1), V(32 + (i % 4) * 310, -90 - (i / 4) * 290), V(296, 274), UI.BgLight, false).transform;
                string tier = it.tier == 4 ? "희귀 유물" : $"Tier {it.tier}";
                bool known = e != null || it.tier == 1 || (it.tier == 2 && G.data.stars >= G.Tier2Stars) || (it.tier == 4 && G.data.stars >= G.FinalStars);
                UI.Label(card, $"<color=#9a948a>{tier}</color>", 19, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(18, -14), V(260, 26));
                UI.Label(card, known ? $"<b>{it.name}</b>" : "<b>???</b>", 28, UI.TextCol, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(18, -44), V(260, 40));
                UI.Label(card, known ? it.blurb : "아직 만나지 못한 물건", 18, UI.Dim, TextAnchor.UpperLeft, V(0, 1), V(0, 1), V(18, -92), V(260, 70));
                string rec = e != null ? $"최고 복원도  <color=#ffc24a>{e.best}%</color>\n납품 {e.count}회  {UI.Stars(e.best >= 95 ? 3 : e.best >= 80 ? 2 : e.best >= 60 ? 1 : 0)}" : "<color=#777777>미복원</color>";
                UI.Label(card, rec, 22, UI.TextCol, TextAnchor.LowerLeft, V(0, 0), V(0, 0), V(18, 16), V(260, 80));
            }
            UI.Label(p, $"도감 달성률  <color=#ffc24a>{done}/{all.Count}</color>    납품 {G.data.jobsDone}회    명성 ★{G.data.stars}", 24, UI.TextCol, TextAnchor.MiddleLeft, V(0, 0), V(0, 0), V(32, 36), V(800, 40));
            UI.Btn(p, "닫기", V(1, 0), V(1, 0), V(-32, 28), V(160, 56), CloseToBoardOrWork);
        }

        public void ShowResult(ScoreInfo sc)
        {
            OnItemChanged();
            var p = OpenOverlay("Result", V(820, 760), $"납품 완료 — {sc.itemName}");
            string Line(string name, float v, int weight) => $"{name}  <color=#9a948a>({weight}%)</color>";
            string[] names = { Line("녹 제거", sc.rust, 25), Line("세척", sc.dust, 20), Line("도색·광택", sc.paint, 20), Line("부품 교체", sc.parts, 15), Line("조립", sc.assembly, 20) };
            float[] vals = { sc.rust, sc.dust, sc.paint, sc.parts, sc.assembly };
            for (int i = 0; i < 5; i++)
            {
                float y = -100 - i * 52;
                UI.Label(p, names[i], 24, UI.TextCol, TextAnchor.MiddleLeft, V(0, 1), V(0, 1), V(40, y), V(300, 40));
                UI.Panel(p, "BarBg", V(0, 1), V(0, 1), V(340, y - 12), V(320, 16), new Color(0.2f, 0.2f, 0.2f, 1f), false);
                UI.Panel(p, "Bar", V(0, 1), V(0, 1), V(340, y - 12), V(320 * Mathf.Clamp01(vals[i]), 16), vals[i] >= 0.999f ? UI.Good : UI.Accent, false);
                UI.Label(p, Mathf.FloorToInt(vals[i] * 100f) + "%", 24, UI.TextCol, TextAnchor.MiddleRight, V(1, 1), V(1, 1), V(-40, y), V(100, 40));
            }
            UI.Label(p, $"<b>복원도 {sc.total}%</b>   {UI.Stars(sc.stars)}", 40, UI.TextCol, TextAnchor.MiddleCenter, V(0.5f, 1), V(0.5f, 1), V(0, -380), V(700, 60));
            string money = (sc.auction ? "판매가" : "보수") + $"  <color=#ffd24a>+{sc.payout} G</color>" + (sc.tested ? "  <color=#8fe08f>(작동 확인 +15%)</color>" : "  <color=#9a948a>(작동 테스트 생략)</color>");
            int net = sc.payout - sc.cost - sc.partsCost;
            if (sc.cost > 0) money += $"\n매입가  <color=#ff8a5a>-{sc.cost} G</color>";
            if (sc.partsCost > 0) money += $"\n교체 부품  <color=#ff8a5a>-{sc.partsCost} G</color>";
            money += $"\n<b>순이익  {(net >= 0 ? "+" : "")}{net} G</b>     명성 +{sc.stars}";
            UI.Label(p, money, 24, UI.TextCol, TextAnchor.UpperCenter, V(0.5f, 1), V(0.5f, 1), V(0, -460), V(700, 170));
            bool ending = sc.ending;
            UI.Btn(p, "계속", V(0.5f, 0), V(0.5f, 0), V(0, 28), V(260, 60), () => { if (ending) ShowEnding(); else ShowBoard(); }, new Color(0.5f, 0.34f, 0.1f, 0.98f));
        }

        public void ShowEnding()
        {
            var p = OpenOverlay("Ending", V(1000, 620), "최고의 공방");
            Sfx.Play("melody", 0.9f);
            UI.Label(p, "백 년 동안 잠들어 있던 오르골이 다시 노래합니다.\n\n녹슨 토스터 하나로 시작한 작은 작업대는\n이제 도시에서 가장 믿음직한 복원 공방이 되었습니다.\n\n<color=#ffc24a>RE:CRAFT — 복원 완료</color>", 30, UI.TextCol, TextAnchor.MiddleCenter, UI.Center, UI.Center, V(0, 30), V(900, 360));
            UI.Label(p, $"납품 {G.data.jobsDone}회 · 명성 ★{G.data.stars} · 도감은 계속 채울 수 있습니다", 22, UI.Dim, TextAnchor.MiddleCenter, V(0.5f, 0), V(0.5f, 0), V(0, 104), V(900, 40));
            UI.Btn(p, "계속 플레이", V(0.5f, 0), V(0.5f, 0), V(0, 28), V(260, 60), ShowBoard, new Color(0.5f, 0.34f, 0.1f, 0.98f));
        }
    }
}
