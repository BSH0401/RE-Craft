using UnityEngine;
using UnityEngine.UI;

namespace ReCraft
{
    /// <summary>
    /// 첫 의뢰를 따라가는 단계형 튜토리얼. 게임 상태를 관찰해 조건이 충족되면 다음 단계로 넘어간다.
    /// 진행도는 SaveData.tutorialStep 에 저장된다 (Done 이상이면 표시하지 않음).
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public const int Done = 99;
        const int Accept = 0, Rotate = 1, XRay = 2, Unscrew = 3, Detach = 4, Order = 5, Derust = 6, Clean = 7, Paint = 8, Assemble = 9, Test = 10, Deliver = 11, Upgrade = 12;
        const int Total = 12;

        public static int Step => G.data.tutorialStep;
        public static bool Running => G.data.tutorialStep < Done;

        RectTransform panel;
        Text title, body;
        Quaternion lastRot;
        bool hasRot;
        float rotated, xrayHeld;
        int shownStep = -1;
        bool shownOverlay;
        int baseJobs = -1;
        Button skipBtn;   // 튜토리얼 의뢰를 받을 때의 납품 횟수

        void Start()
        {
            baseJobs = G.data.jobsDone;
            var root = HUD.I.Root;
            var bg = UI.Panel(root, "Tutorial", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(470, 150), new Color(0.1f, 0.085f, 0.05f, 0.95f), false);
            panel = bg.rectTransform;
            UI.Panel(panel, "Bar", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(6, 150), UI.Accent, false).name = "Bar";
            title = UI.Label(panel, "", 19, UI.Accent, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -10), new Vector2(300, 26));
            body = UI.Label(panel, "", 21, UI.TextCol, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -40), new Vector2(435, 100));
            skipBtn = UI.Btn(panel, "건너뛰기", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(96, 30), Skip, new Color(0.25f, 0.22f, 0.18f, 0.95f), 16);
            panel.gameObject.SetActive(false);
        }

        public static void Skip()
        {
            SetStep(Done);
        }

        static void SetStep(int s)
        {
            if (G.data.tutorialStep == s) return;
            G.data.tutorialStep = s;
            G.Save();
            if (s != Done && s != Accept) Sfx.Play("sparkle", 0.5f);
            HUD.I.OnToolChanged();
            if (DevBot.Active) Debug.Log("[Tutorial] step " + s);
        }

        void Update()
        {
            if (!Running)
            {
                if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
                return;
            }
            var wb = Workbench.I;
            var item = wb.item;
            Advance(wb, item);
            if (!Running) return;

            int step = Step;
            bool waiting = item == null && step > Accept && step < Upgrade;   // 의뢰를 포기했거나 아직 받지 않음
            string text = waiting ? Text(Accept, default) : Text(step, item != null ? wb.Stats() : default);
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            title.text = step >= Upgrade ? "튜토리얼 완료" : $"튜토리얼  {Mathf.Min(step + 1, Total)} / {Total}";
            body.text = text;
            UI.TextOf(skipBtn).text = step >= Upgrade ? "닫기" : "건너뛰기";

            // 오버레이(게시판 등)가 떠 있으면 상단 띠, 작업 중에는 좌하단 카드
            bool overlay = HUD.I.OverlayName != null;
            if (overlay != shownOverlay || shownStep != step)
            {
                shownOverlay = overlay; shownStep = step;
                var bar = (RectTransform)panel.Find("Bar");
                if (overlay)
                {
                    panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
                    panel.pivot = new Vector2(0.5f, 1f);
                    panel.anchoredPosition = new Vector2(0, -68);
                    panel.sizeDelta = new Vector2(1100, 74);
                    body.rectTransform.sizeDelta = new Vector2(940, 36);
                    body.rectTransform.anchoredPosition = new Vector2(20, -34);
                    bar.sizeDelta = new Vector2(6, 74);
                }
                else
                {
                    panel.anchorMin = panel.anchorMax = Vector2.zero;
                    panel.pivot = Vector2.zero;
                    panel.anchoredPosition = new Vector2(16, 110);
                    panel.sizeDelta = new Vector2(470, 150);
                    body.rectTransform.sizeDelta = new Vector2(435, 100);
                    body.rectTransform.anchoredPosition = new Vector2(20, -40);
                    bar.sizeDelta = new Vector2(6, 150);
                }
            }

            // 지금 필요한 도구 버튼을 깜빡여 안내
            int toolIdx = waiting ? -1 : ToolFor(step);
            if (toolIdx >= 0 && item != null && (int)wb.tool != toolIdx)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                HUD.I.ToolImage(toolIdx).color = Color.Lerp(UI.BgLight, new Color(0.75f, 0.5f, 0.12f, 1f), k);
            }
        }

        static int ToolFor(int step)
        {
            switch (step)
            {
                case Unscrew: return (int)Tool.Driver;
                case Detach: return (int)Tool.Hand;
                case Derust: return (int)Tool.Sander;
                case Clean: return (int)Tool.Cloth;
                case Paint: return (int)Tool.Spray;
                default: return -1;
            }
        }

        void Advance(Workbench wb, ItemInstance item)
        {
            // 관찰 값 갱신
            if (item != null)
            {
                if (hasRot) rotated += Quaternion.Angle(lastRot, item.root.rotation);
                lastRot = item.root.rotation;
                hasRot = true;
                if (wb.xray) xrayHeld += Time.unscaledDeltaTime;
            }
            else { hasRot = false; rotated = 0f; xrayHeld = 0f; }

            for (int guard = 0; guard < 16; guard++)
            {
                int step = Step;
                if (step >= Done) return;
                if (step == Upgrade)
                {
                    if (HUD.I.OverlayName == "Upgrades" || item != null) SetStep(Done);
                    return;
                }
                // 어느 단계에서든 납품을 마치면 마지막 안내로
                if (step > Accept && G.data.jobsDone > baseJobs) { SetStep(Upgrade); continue; }
                if (item == null) return;
                if (step == Accept) { rotated = 0f; xrayHeld = 0f; baseJobs = G.data.jobsDone; SetStep(Rotate); continue; }
                if (step == Deliver) return;
                var st = wb.Stats();
                bool ok;
                switch (step)
                {
                    case Rotate: ok = rotated > 60f; break;
                    case XRay: ok = xrayHeld > 0.25f; break;
                    case Unscrew: ok = st.screws == 0 || st.screwsOut > 0 || st.detached > 0; break;
                    case Detach: ok = st.detached >= Mathf.Min(2, st.detachable); break;
                    case Order:
                        ok = st.broken == 0;
                        foreach (var p in item.parts) if (p.ordered || p.replaced) ok = true;
                        break;
                    case Derust: ok = !st.hasRust || st.rustDone >= 0.3f; break;
                    case Clean: ok = !st.hasDust || st.dustDone >= 0.3f; break;
                    case Paint: ok = !st.hasPaint || st.paintDone >= 0.2f; break;
                    case Assemble: ok = st.assembled; break;
                    case Test: ok = item.tested; break;
                    default: ok = false; break;
                }
                if (!ok) return;
                SetStep(step + 1);
            }
        }

        static string Pct(float v) => Mathf.FloorToInt(Mathf.Clamp01(v) * 100f) + "%";

        static string Text(int step, WorkStats st)
        {
            switch (step)
            {
                case Accept: return "게시판에서 마음에 드는 의뢰를 골라 <color=#ffc24a>수주하기</color>를 누르세요.";
                case Rotate: return "<color=#ffc24a>우클릭 드래그</color>로 물건을 돌려 보세요.\n<color=#ffc24a>휠</color>로 확대·축소할 수 있습니다.";
                case XRay: return "<color=#ffc24a>Tab</color>을 누르고 있으면 X-Ray 스캐너가 켜집니다.\n노랑 = 풀 수 있는 나사, 빨강 = 파손 부품.";
                case Unscrew: return "<color=#ffc24a>2 드라이버</color>를 고르고, 나사 위에서\n좌클릭을 <color=#ffc24a>꾹 눌러</color> 풀어 보세요.";
                case Detach: return "<color=#ffc24a>1 손</color>으로 부품을 클릭하면 트레이로 분리됩니다.\n덮개를 떼야 안쪽 부품에 닿습니다.  (" + Mathf.Min(st.detached, 2) + "/2)";
                case Order: return "<color=#ff6a5a>파손 부품</color>을 분리한 뒤, 왼쪽 위 목록의\n<color=#ffc24a>주문</color> 버튼으로 새 부품을 받으세요.";
                case Derust: return "<color=#ffc24a>3 녹 제거</color>로 녹슨 곳을 문지르세요.\n95%를 넘기면 자동으로 마무리됩니다.  (" + Pct(st.rustDone / 0.3f) + ")";
                case Clean: return "<color=#ffc24a>4 세척</color>으로 먼지를 닦아 내세요.  (" + Pct(st.dustDone / 0.3f) + ")";
                case Paint: return "<color=#ffc24a>5 도색</color>으로 색을 입히세요. 녹과 먼지를\n지운 곳에만 칠해집니다.  (" + Pct(st.paintDone / 0.2f) + ")";
                case Assemble: return "트레이의 부품을 <color=#ffc24a>1 손</color>으로 드래그(또는 클릭)해 조립하고,\n<color=#ffc24a>2 드라이버</color>로 빈 구멍을 꾹 눌러 조이세요.\n부품 " + st.attached + "/" + st.detachable + " · 나사 " + st.screwsIn + "/" + st.screws;
                case Test: return "오른쪽 위 <color=#ffc24a>작동 테스트</color>를 눌러 확인하세요.\n성공하면 납품 보수가 15% 늘어납니다.";
                case Deliver: return "더 다듬고 싶은 곳이 없다면 <color=#ffc24a>납품하기</color>!\n복원도가 높을수록 보수와 명성(★)이 커집니다.";
                default: return "첫 납품 완료! 번 골드로 <color=#ffc24a>업그레이드 상점</color>에서 도구를 강화하면 작업이 훨씬 빨라집니다.";
            }
        }
    }
}
