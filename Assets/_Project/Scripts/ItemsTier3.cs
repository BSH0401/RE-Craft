using UnityEngine;

namespace ReCraft
{
    /// <summary>Tier 3 (기계·엔진): 부품 30개 이상, 나사 20개 안팎의 큰 물건들.</summary>
    public static partial class Items
    {
        static readonly Vector3 Lf = Vector3.left, Rt = Vector3.right;
        static readonly Color Alu = C(0.7f, 0.71f, 0.72f);
        static readonly Color CastIron = C(0.34f, 0.35f, 0.37f);

        // ───────────── 단기통 오토바이 엔진 ─────────────
        static ItemDef Engine()
        {
            var it = new ItemDef { id = "engine", name = "단기통 오토바이 엔진", tier = 3, value = 520, sound = TestSound.Engine, blurb = "툴툴툴— 한때 골목을 누비던 125cc 심장" };

            var body = it.Body("크랭크케이스").Rust();
            body.Box(.14f, .12f, .15f, 0, -.065f, 0, Alu, .85f, .45f);
            body.Cyl(.068f, .15f, 0, -.07f, .005f, AxX, Alu, .85f, .45f);
            body.Box(.11f, .015f, .11f, 0, -.128f, 0, Alu, .85f, .4f);

            // 왼쪽: 변속 레버 → 클러치 커버 → 압력판 → 클러치 판 3장 → 클러치 바스켓
            it.Part("shift", "변속 레버").Ext().Rust()
                .Box(.008f, .012f, .09f, -.122f, -.112f, -.01f, Dark, .5f, .4f)
                .Box(.008f, .03f, .012f, -.122f, -.097f, -.055f, Dark, .5f, .4f)
                .Cyl(.008f, .012f, -.118f, -.112f, .035f, AxX, Steel, .9f, .6f);
            var lcover = it.Part("lcover", "클러치 커버").Ext().Rust().Under("shift")
                .Cyl(.066f, .016f, -.103f, -.07f, .005f, AxX, Alu, .85f, .55f)
                .Cyl(.03f, .006f, -.114f, -.07f, .005f, AxX, Alu, .9f, .65f);
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                lcover.Screw(-.111f, -.07f + Mathf.Sin(a) * .047f, .005f + Mathf.Cos(a) * .047f, Lf);
            }
            var pplate = it.Part("pplate", "압력판").Under("lcover")
                .Cyl(.033f, .005f, -.0915f, -.07f, .005f, AxX, Steel, .9f, .6f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120 * Mathf.Deg2Rad;
                pplate.Screw(-.094f, -.07f + Mathf.Sin(a) * .021f, .005f + Mathf.Cos(a) * .021f, Lf);
            }
            for (int i = 0; i < 3; i++)
                it.Part("plate" + (i + 1), $"클러치 판 {(char)('A' + i)}").Under("pplate").Rust().Spin(Rt, 600, new Vector3(-.083f, -.07f, .005f))
                    .Gear(.041f, .003f, 16, -.087f + i * .004f, -.07f, .005f, AxX, i % 2 == 0 ? C(.35f, .25f, .18f) : Steel, .7f, .4f);
            it.Part("basket", "클러치 바스켓").Under("plate1", "plate2", "plate3").Spin(Rt, 600, new Vector3(-.084f, -.07f, .005f))
                .Tube(.046f, .043f, .018f, -.084f, -.07f, .005f, AxX, Alu, .85f, .5f);
            it.Part("sprocket", "구동 스프로킷").Ext().Rust().Spin(Rt, 400, new Vector3(-.079f, -.11f, .075f))
                .Gear(.022f, .008f, 13, -.079f, -.11f, .075f, AxX, CastIron, .8f, .5f);

            // 오른쪽: 킥 레버 → 발전기 커버 → 플라이휠 → 스테이터
            it.Part("kick", "킥 스타트 레버").Ext().Rust()
                .Box(.008f, .1f, .012f, .122f, -.075f, .03f, Steel, .9f, .6f, false, Quaternion.Euler(30, 0, 0))
                .Box(.01f, .012f, .045f, .122f, -.118f, .07f, Dark, .2f, .4f)
                .Cyl(.009f, .012f, .118f, -.03f, .005f, AxX, Steel, .9f, .6f);
            var rcover = it.Part("rcover", "발전기 커버").Ext().Rust().Under("kick")
                .Cyl(.066f, .016f, .103f, -.07f, .005f, AxX, Alu, .85f, .55f)
                .Cyl(.026f, .006f, .114f, -.07f, .005f, AxX, Alu, .9f, .65f);
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                rcover.Screw(.111f, -.07f + Mathf.Sin(a) * .047f, .005f + Mathf.Cos(a) * .047f, Rt);
            }
            it.Part("flywheel", "플라이휠").Under("rcover").Rust().Breakable(40).Spin(Rt, 900, new Vector3(.088f, -.07f, .005f))
                .Cyl(.05f, .014f, .088f, -.07f, .005f, AxX, CastIron, .85f, .5f)
                .Cyl(.012f, .008f, .097f, -.07f, .005f, AxX, Steel, .9f, .6f);
            it.Part("stator", "스테이터 코일").Under("flywheel").Breakable(30)
                .Gear(.034f, .008f, 9, .079f, -.07f, .005f, AxX, Copper, .7f, .5f);

            // 위쪽: 점화 플러그 · 밸브 커버 → 로커 암 → 헤드 → 실린더 → 피스톤 → 커넥팅 로드
            it.Part("plug", "점화 플러그").Ext().Breakable(15).Glow(C(.6f, .75f, 1f))
                .Cyl(.007f, .02f, 0, .125f, -.058f, AxZ, Steel, .9f, .6f)
                .Cyl(.009f, .024f, 0, .125f, -.08f, AxZ, Cream, 0, .8f)
                .Cyl(.004f, .01f, 0, .125f, -.097f, AxZ, Steel, .9f, .6f);
            it.Part("vcover", "밸브 커버").Ext().Rust()
                .Box(.08f, .024f, .07f, 0, .162f, 0, Alu, .85f, .55f)
                .Screw(-.03f, .174f, 0, Up).Screw(.03f, .174f, 0, Up);
            it.Part("rocker1", "로커 암 (흡기)").Under("vcover").Wobble().Box(.05f, .008f, .012f, 0, .154f, -.015f, Steel, .9f, .6f);
            it.Part("rocker2", "로커 암 (배기)").Under("vcover").Wobble().Box(.05f, .008f, .012f, 0, .154f, .015f, Steel, .9f, .6f);
            var head = it.Part("head", "실린더 헤드").Rust().Under("vcover", "rocker1", "rocker2", "plug", "expipe")
                .Box(.1f, .05f, .1f, 0, .125f, 0, CastIron, .7f, .45f);
            for (int i = 0; i < 3; i++) head.Box(.13f, .004f, .13f, 0, .105f + i * .013f, 0, CastIron, .7f, .45f);
            head.Screw(-.045f, .15f, -.045f, Up).Screw(.045f, .15f, -.045f, Up).Screw(-.045f, .15f, .045f, Up).Screw(.045f, .15f, .045f, Up);
            var cyl = it.Part("cyl", "실린더").Rust().Under("head").Cyl(.042f, .1f, 0, .05f, 0, AxY, CastIron, .7f, .45f);
            for (int i = 0; i < 7; i++) cyl.Cyl(.06f, .004f, 0, .008f + i * .014f, 0, AxY, CastIron, .7f, .45f);
            it.Part("piston", "피스톤").Under("cyl").Breakable(45).Wobble()
                .Cyl(.04f, .035f, 0, .075f, 0, AxY, Steel, .9f, .65f)
                .Cyl(.0405f, .002f, 0, .085f, 0, AxY, Dark, .6f, .4f)
                .Cyl(.0405f, .002f, 0, .079f, 0, AxY, Dark, .6f, .4f);
            it.Part("conrod", "커넥팅 로드").Under("piston").Box(.012f, .07f, .008f, 0, .03f, 0, Steel, .9f, .6f);

            // 뒤쪽 흡기: 에어 필터 → 카뷰레터 → 흡기 매니폴드
            it.Part("airbox", "에어 필터").Ext()
                .Cyl(.035f, .05f, 0, .12f, .135f, AxZ, Dark, 0, .3f)
                .Cyl(.036f, .006f, 0, .12f, .163f, AxZ, Steel, .9f, .6f);
            it.Part("carb", "카뷰레터").Under("airbox").Rust().Breakable(35)
                .Box(.04f, .04f, .045f, 0, .12f, .085f, Alu, .8f, .5f)
                .Cyl(.008f, .02f, .03f, .12f, .085f, AxX, Brass, .9f, .6f)
                .Screw(-.012f, .14f, .085f, Up).Screw(.012f, .14f, .085f, Up);
            it.Part("intake", "흡기 매니폴드").Under("carb").Cyl(.012f, .02f, 0, .12f, .057f, AxZ, Alu, .8f, .5f);
            var starter = it.Part("starter", "스타터 모터").Breakable(40)
                .Cyl(.024f, .07f, -.04f, .022f, .09f, AxX, Dark, .3f, .45f)
                .Cyl(.025f, .01f, -.005f, .022f, .09f, AxX, Alu, .85f, .55f);
            starter.Screw(-.06f, .046f, .09f, Up).Screw(-.02f, .046f, .09f, Up);

            // 앞쪽 배기: 머플러 → 배기관
            it.Part("muffler", "머플러").Ext().Rust()
                .Cyl(.026f, .16f, 0, -.04f, -.115f, AxX, Chrome, .9f, .7f)
                .Cyl(.012f, .02f, .09f, -.04f, -.115f, AxX, Dark, .5f, .4f)
                .Screw(-.06f, -.014f, -.115f, Up).Screw(.06f, -.014f, -.115f, Up);
            it.Part("expipe", "배기관").Ext().Rust().Under("muffler")
                .Cyl(.011f, .13f, .03f, .045f, -.075f, AxY, Chrome, .9f, .7f)
                .Ball(.011f, .03f, .11f, -.075f, Chrome, .9f, .7f)
                .Cyl(.011f, .025f, .03f, .11f, -.06f, AxZ, Chrome, .9f, .7f);

            // 바닥 · 기타
            it.Part("oilcap", "오일 주입구 캡").Ext().Cyl(.012f, .014f, .05f, .007f, -.05f, AxY, Brass, .9f, .6f);
            it.Part("drain", "드레인 볼트").Ext().Rust().Gear(.009f, .01f, 6, 0, -.14f, 0, AxY, Steel, .9f, .6f);
            it.Part("mount1", "엔진 마운트 (앞)").Ext().Rust()
                .Box(.1f, .012f, .025f, 0, -.141f, -.05f, Dark, .5f, .4f)
                .Screw(-.035f, -.147f, -.05f, Dn).Screw(.035f, -.147f, -.05f, Dn);
            it.Part("mount2", "엔진 마운트 (뒤)").Ext().Rust()
                .Box(.1f, .012f, .025f, 0, -.141f, .05f, Dark, .5f, .4f)
                .Screw(-.035f, -.147f, .05f, Dn).Screw(.035f, -.147f, .05f, Dn);
            return it;
        }

        // ───────────── 아케이드 오락기 (바탑) ─────────────
        static ItemDef Arcade()
        {
            var it = new ItemDef { id = "arcade", name = "아케이드 오락기", tier = 3, value = 600, sound = TestSound.Arcade, blurb = "동전 하나로 해 질 녘까지. 동네 오락실의 바탑 기판" };
            Color cab = C(.36f, .14f, .55f), trim = C(.95f, .75f, .12f);

            var body = it.Body("캐비닛");
            body.Box(.012f, .36f, .24f, -.124f, 0, 0, cab, 0, .5f, true);
            body.Box(.012f, .36f, .24f, .124f, 0, 0, cab, 0, .5f, true);
            body.Box(.236f, .012f, .24f, 0, -.174f, 0, cab, 0, .5f, true);
            body.Box(.236f, .012f, .2f, 0, .174f, .02f, cab, 0, .5f, true);
            body.Box(.236f, .09f, .008f, 0, -.125f, -.116f, cab, 0, .5f, true);
            body.Box(.236f, .006f, .23f, 0, .118f, 0, Dark, 0, .3f);

            // 위: 마키 간판 → 형광등 · 스피커
            it.Part("marquee", "마키 간판").Ext().Glow(C(1f, .45f, .75f))
                .Box(.236f, .048f, .006f, 0, .144f, -.117f, C(.95f, .4f, .6f), 0, .7f)
                .Box(.15f, .016f, .002f, 0, .146f, -.1205f, trim, 0, .7f)
                .Screw(-.105f, .144f, -.12f, Bk).Screw(.105f, .144f, -.12f, Bk);
            it.Part("tube", "마키 형광등").Under("marquee").Breakable(12).Glow(C(1f, .92f, .8f))
                .Cyl(.006f, .2f, 0, .144f, -.1f, AxX, Cream, 0, .8f);
            it.Part("speaker", "스피커").Under("marquee").Breakable(15).Wobble()
                .Cone(.022f, .01f, .016f, .06f, .144f, -.075f, AxZ, Dark, .2f, .4f);

            // 화면: 베젤 → 유리 → 모니터
            var bezel = it.Part("bezel", "화면 베젤").Ext()
                .Box(.236f, .02f, .008f, 0, .1f, -.112f, Dark, 0, .5f, true)
                .Box(.236f, .02f, .008f, 0, -.01f, -.112f, Dark, 0, .5f, true)
                .Box(.03f, .09f, .008f, -.103f, .045f, -.112f, Dark, 0, .5f, true)
                .Box(.03f, .09f, .008f, .103f, .045f, -.112f, Dark, 0, .5f, true);
            bezel.Screw(-.105f, .1f, -.116f, Bk).Screw(.105f, .1f, -.116f, Bk).Screw(-.105f, -.01f, -.116f, Bk).Screw(.105f, -.01f, -.116f, Bk);
            it.Part("glass", "화면 유리").Under("bezel").Box(.176f, .09f, .003f, 0, .045f, -.107f, C(.15f, .2f, .25f), .1f, .95f);
            it.Part("crt", "브라운관 모니터").Under("glass").Breakable(90).Glow(C(.35f, .9f, 1f))
                .Box(.17f, .085f, .004f, 0, .045f, -.103f, C(.08f, .12f, .1f), 0, .85f)
                .Box(.15f, .08f, .09f, 0, .045f, -.055f, Grey, .2f, .4f);

            // 컨트롤 패널: 조이스틱 · 버튼 → 패널
            var under = new System.Collections.Generic.List<string> { "joy", "start" };
            it.Part("joy", "조이스틱").Ext().Wobble()
                .Cyl(.012f, .004f, -.06f, -.047f, -.135f, AxY, Dark, 0, .3f)
                .Cyl(.004f, .04f, -.06f, -.028f, -.135f, AxY, Steel, .9f, .6f)
                .Ball(.012f, -.06f, -.006f, -.135f, C(.85f, .1f, .1f), 0, .75f);
            Color[] btn = { C(.9f, .15f, .15f), C(.95f, .8f, .1f), C(.15f, .4f, .95f), C(.2f, .8f, .3f), Cream, C(.9f, .15f, .15f) };
            for (int i = 0; i < 6; i++)
            {
                string id = "btn" + (i + 1);
                under.Add(id);
                it.Part(id, $"버튼 {i + 1}").Ext().Cyl(.009f, .008f, .01f + (i % 3) * .03f, -.045f, -.14f + (i / 3) * .025f, AxY, btn[i], 0, .7f);
            }
            it.Part("start", "스타트 버튼").Ext()
                .Cyl(.006f, .006f, .1f, -.046f, -.15f, AxY, Cream, 0, .7f)
                .Cyl(.006f, .006f, .1f, -.046f, -.125f, AxY, Cream, 0, .7f);
            it.Part("cpanel", "컨트롤 패널").Ext().Under(under.ToArray())
                .Box(.236f, .012f, .07f, 0, -.055f, -.13f, Dark, 0, .4f)
                .Box(.22f, .002f, .06f, 0, -.048f, -.13f, C(.2f, .3f, .8f), 0, .5f, true)
                .Screw(-.105f, -.047f, -.155f, Up).Screw(.105f, -.047f, -.155f, Up).Screw(-.105f, -.047f, -.105f, Up).Screw(.105f, -.047f, -.105f, Up);

            // 아래: 코인 도어 → 코인 메크 → 동전통
            it.Part("coindoor", "코인 도어").Ext().Rust()
                .Box(.1f, .07f, .006f, 0, -.12f, -.123f, Chrome, .9f, .6f)
                .Box(.012f, .03f, .004f, -.025f, -.11f, -.127f, Dark, .3f, .3f)
                .Box(.012f, .03f, .004f, .025f, -.11f, -.127f, Dark, .3f, .3f)
                .Cyl(.005f, .004f, -.025f, -.135f, -.127f, AxZ, C(.9f, .15f, .1f), 0, .7f)
                .Cyl(.005f, .004f, .025f, -.135f, -.127f, AxZ, C(.9f, .15f, .1f), 0, .7f)
                .Screw(-.042f, -.15f, -.126f, Bk).Screw(.042f, -.15f, -.126f, Bk);
            it.Part("coinmech", "코인 메크").Under("coindoor").Rust().Breakable(25).Box(.06f, .04f, .04f, 0, -.11f, -.095f, Steel, .8f, .5f);
            it.Part("coinbox", "동전통").Under("coinmech").Box(.08f, .03f, .05f, 0, -.152f, -.09f, Grey, .6f, .4f);

            // 뒤: 전원 스위치 → 뒷판 → 기판 · 전원부 · 팬 · 배선
            it.Part("pwr", "전원 스위치").Ext().Box(.025f, .015f, .01f, .08f, .14f, .124f, Dark, 0, .4f);
            it.Part("back", "뒷판").Ext().Under("pwr")
                .Box(.236f, .33f, .006f, 0, 0, .117f, Dark, 0, .35f)
                .Screw(-.105f, .14f, .12f, Fw).Screw(.105f, .14f, .12f, Fw)
                .Screw(-.105f, 0, .12f, Fw).Screw(.105f, 0, .12f, Fw)
                .Screw(-.105f, -.14f, .12f, Fw).Screw(.105f, -.14f, .12f, Fw);
            it.Part("pcb", "게임 기판").Under("back").Rust().Breakable(70)
                .Box(.15f, .1f, .004f, 0, -.03f, .1f, Pcb, 0, .5f)
                .Box(.03f, .025f, .005f, -.03f, -.02f, .0955f, Dark, 0, .4f)
                .Box(.02f, .02f, .005f, .02f, -.04f, .0955f, Dark, 0, .4f)
                .Box(.05f, .012f, .006f, .04f, .0f, .0955f, C(.25f, .25f, .27f), 0, .4f)
                .Screw(-.065f, .01f, .103f, Fw).Screw(.065f, .01f, .103f, Fw).Screw(-.065f, -.07f, .103f, Fw).Screw(.065f, -.07f, .103f, Fw);
            it.Part("psu", "전원부").Under("back").Breakable(40).Box(.07f, .05f, .06f, .07f, -.135f, .07f, Steel, .8f, .5f);
            it.Part("fan", "냉각 팬").Under("back").Spin(Fw, 900, new Vector3(-.065f, -.12f, .1f))
                .Gear(.025f, .008f, 7, -.065f, -.12f, .1f, AxZ, Grey, .3f, .4f)
                .Box(.06f, .06f, .006f, -.065f, -.12f, .107f, Dark, .2f, .4f);
            it.Part("harness", "배선 묶음").Under("back")
                .Cyl(.004f, .2f, -.095f, .0f, .09f, AxY, C(.8f, .2f, .2f), 0, .5f)
                .Cyl(.004f, .2f, -.086f, .0f, .09f, AxY, C(.2f, .3f, .8f), 0, .5f)
                .Cyl(.004f, .2f, -.077f, .0f, .09f, AxY, C(.9f, .8f, .2f), 0, .5f);

            // 옆면 장식
            it.Part("tml", "T-몰딩 (왼쪽)").Ext().Box(.016f, .36f, .006f, -.124f, 0, -.123f, trim, .2f, .6f);
            it.Part("tmr", "T-몰딩 (오른쪽)").Ext().Box(.016f, .36f, .006f, .124f, 0, -.123f, trim, .2f, .6f);
            it.Part("artl", "사이드 아트 (왼쪽)").Ext()
                .Box(.002f, .2f, .16f, -.131f, .02f, 0, C(.95f, .5f, .1f), 0, .5f, true)
                .Box(.002f, .03f, .16f, -.1325f, -.04f, 0, C(.9f, .2f, .5f), 0, .5f, true);
            it.Part("artr", "사이드 아트 (오른쪽)").Ext()
                .Box(.002f, .2f, .16f, .131f, .02f, 0, C(.95f, .5f, .1f), 0, .5f, true)
                .Box(.002f, .03f, .16f, .1325f, -.04f, 0, C(.9f, .2f, .5f), 0, .5f, true);
            var feet = it.Part("feet", "고무 받침").Ext();
            foreach (float x in new[] { -.1f, .1f })
                foreach (float z in new[] { -.1f, .1f })
                    feet.Cyl(.012f, .008f, x, -.184f, z, AxY, Dark, 0, .3f);
            return it;
        }

        // ───────────── 소형 발전기 ─────────────
        static ItemDef Generator()
        {
            var it = new ItemDef { id = "generator", name = "소형 발전기", tier = 3, value = 560, sound = TestSound.Click, blurb = "정전된 밤 골목을 밝히던 휴대용 발전기" };
            Color frame = C(.13f, .13f, .14f), yellow = C(.92f, .62f, .08f), red = C(.75f, .15f, .1f);

            var body = it.Body("프레임").Rust();
            foreach (float x in new[] { -.165f, .165f })
                foreach (float z in new[] { -.11f, .11f })
                    body.Cyl(.008f, .28f, x, 0, z, AxY, frame, .5f, .45f);
            foreach (float y in new[] { -.14f, .14f })
            {
                body.Cyl(.008f, .33f, 0, y, -.11f, AxX, frame, .5f, .45f);
                body.Cyl(.008f, .33f, 0, y, .11f, AxX, frame, .5f, .45f);
                body.Cyl(.008f, .22f, -.165f, y, 0, AxZ, frame, .5f, .45f);
                body.Cyl(.008f, .22f, .165f, y, 0, AxZ, frame, .5f, .45f);
            }
            body.Box(.3f, .008f, .2f, 0, -.135f, 0, frame, .5f, .4f);

            // 위: 주유구 캡 · 연료계 → 연료 탱크
            it.Part("fuelcap", "주유구 캡").Ext().Cyl(.022f, .014f, .06f, .137f, -.03f, AxY, Dark, .3f, .5f);
            it.Part("gauge", "연료계").Ext().Glow(C(1f, .8f, .3f)).Cyl(.014f, .004f, -.07f, .132f, -.03f, AxY, Cream, 0, .8f);
            it.Part("tank", "연료 탱크").Ext().Rust().Under("fuelcap", "gauge")
                .Box(.26f, .06f, .17f, 0, .1f, 0, yellow, .2f, .6f, true)
                .Screw(-.115f, .13f, -.07f, Up).Screw(.115f, .13f, -.07f, Up).Screw(-.115f, .13f, .07f, Up).Screw(.115f, .13f, .07f, Up);

            // 엔진 (왼쪽): 점화 플러그 · 헤드 / 리코일 → 플라이휠 → 점화 코일 / 보호판 → 머플러 / 에어 클리너 → 필터 → 카뷰레터
            it.Part("plug", "점화 플러그").Ext().Breakable(15)
                .Cyl(.006f, .02f, -.07f, .02f, -.045f, AxZ, Steel, .9f, .6f)
                .Cyl(.008f, .02f, -.07f, .02f, -.065f, AxZ, Cream, 0, .8f);
            var head = it.Part("head", "실린더 헤드").Rust().Under("tank", "plug").Box(.07f, .05f, .07f, -.07f, .02f, 0, Alu, .8f, .45f);
            for (int i = 0; i < 3; i++) head.Box(.09f, .004f, .09f, -.07f, .002f + i * .014f, 0, Alu, .8f, .45f);
            var recoil = it.Part("recoil", "리코일 스타터").Ext().Rust()
                .Cyl(.05f, .02f, -.14f, -.06f, 0, AxX, red, .2f, .5f, true)
                .Box(.01f, .012f, .045f, -.155f, -.02f, -.06f, Dark, 0, .4f);
            recoil.Screw(-.15f, -.025f, 0, Lf).Screw(-.15f, -.0775f, -.03f, Lf).Screw(-.15f, -.0775f, .03f, Lf);
            it.Part("flywheel", "플라이휠").Under("recoil").Rust().Spin(Rt, 800, new Vector3(-.125f, -.06f, 0))
                .Gear(.045f, .01f, 10, -.125f, -.06f, 0, AxX, Steel, .85f, .55f);
            it.Part("coil", "점화 코일").Under("flywheel").Breakable(25).Box(.02f, .03f, .02f, -.123f, -.005f, -.045f, Dark, .2f, .4f);
            it.Part("shield", "머플러 보호판").Ext().Rust()
                .Box(.11f, .07f, .006f, -.07f, -.06f, .132f, frame, .4f, .4f)
                .Screw(-.115f, -.06f, .135f, Fw).Screw(-.025f, -.06f, .135f, Fw);
            it.Part("muffler", "머플러").Rust().Under("shield").Cyl(.03f, .09f, -.07f, -.06f, .095f, AxX, CastIron, .7f, .4f);
            it.Part("airbox", "에어 클리너").Ext().Box(.05f, .05f, .035f, -.07f, -.04f, -.1f, Dark, 0, .3f).Screw(-.07f, -.04f, -.1175f, Bk);
            it.Part("filter", "필터 엘리먼트").Under("airbox").Box(.04f, .04f, .01f, -.07f, -.04f, -.08f, C(.9f, .85f, .7f), 0, .2f);
            it.Part("carb", "카뷰레터").Under("filter").Rust().Breakable(30).Box(.028f, .028f, .01f, -.07f, -.04f, -.07f, Alu, .8f, .5f);
            it.Part("oilcap", "오일 주입구").Ext().Cyl(.01f, .02f, -.03f, -.1f, -.075f, AxZ, C(.9f, .7f, .1f), .2f, .6f);
            it.Part("block", "엔진 블록").Rust().Under("head", "recoil", "flywheel", "coil", "muffler", "carb", "oilcap")
                .Box(.1f, .11f, .13f, -.07f, -.06f, 0, Alu, .8f, .45f);

            // 발전부 (오른쪽): 엔드 캡 → 브러시 → 로터 → 스테이터 → 하우징
            var endcap = it.Part("endcap", "엔드 캡").Ext().Rust()
                .Cyl(.056f, .012f, .117f, -.06f, 0, AxX, Alu, .85f, .55f);
            for (int i = 0; i < 4; i++) endcap.Box(.002f, .006f, .05f, .1235f, -.075f + i * .01f, 0, Dark, .3f, .3f);
            endcap.Screw(.123f, -.025f, 0, Rt).Screw(.123f, -.095f, 0, Rt).Screw(.123f, -.06f, -.035f, Rt).Screw(.123f, -.06f, .035f, Rt);
            it.Part("brush", "브러시 홀더").Under("endcap").Breakable(20).Box(.015f, .012f, .02f, .1f, -.03f, 0, Dark, .2f, .4f);
            it.Part("rotor", "로터").Under("endcap", "brush").Spin(Rt, 900, new Vector3(.06f, -.06f, 0))
                .Gear(.038f, .08f, 12, .06f, -.06f, 0, AxX, Copper, .7f, .5f)
                .Cyl(.008f, .11f, .06f, -.06f, 0, AxX, Steel, .9f, .6f);
            it.Part("stator", "스테이터 권선").Under("rotor").Breakable(55).Tube(.047f, .041f, .07f, .06f, -.06f, 0, AxX, Copper, .7f, .5f);

            // 앞: 콘센트 · 차단기 · 전압계 · 스위치 → 컨트롤 패널 → 전압 조정기
            var panelUnder = new[] { "outlet1", "outlet2", "breaker", "meter", "switch" };
            it.Part("outlet1", "콘센트 A").Ext()
                .Box(.03f, .035f, .006f, .045f, -.055f, -.106f, Cream, 0, .6f)
                .Box(.004f, .01f, .002f, .04f, -.055f, -.1095f, Dark).Box(.004f, .01f, .002f, .05f, -.055f, -.1095f, Dark);
            it.Part("outlet2", "콘센트 B").Ext()
                .Box(.03f, .035f, .006f, .095f, -.055f, -.106f, Cream, 0, .6f)
                .Box(.004f, .01f, .002f, .09f, -.055f, -.1095f, Dark).Box(.004f, .01f, .002f, .1f, -.055f, -.1095f, Dark);
            it.Part("breaker", "차단기").Ext().Breakable(18).Box(.012f, .02f, .012f, .035f, -.012f, -.106f, Dark, .1f, .4f);
            it.Part("meter", "전압계").Ext().Glow(C(.6f, 1f, .6f))
                .Cyl(.013f, .006f, .1f, -.012f, -.106f, AxZ, Cream, 0, .85f)
                .Tube(.015f, .013f, .007f, .1f, -.012f, -.106f, AxZ, Chrome, .9f, .7f);
            it.Part("switch", "시동 스위치").Ext().Box(.014f, .02f, .008f, .067f, -.012f, -.107f, red, 0, .6f);
            it.Part("panel", "컨트롤 패널").Ext().Under(panelUnder)
                .Box(.12f, .08f, .006f, .07f, -.035f, -.1f, C(.25f, .27f, .3f), .3f, .5f, true)
                .Box(.006f, .08f, .045f, .013f, -.035f, -.077f, frame, .4f, .4f)
                .Box(.006f, .08f, .045f, .127f, -.035f, -.077f, frame, .4f, .4f)
                .Screw(.02f, -.005f, -.103f, Bk).Screw(.12f, -.005f, -.103f, Bk).Screw(.02f, -.065f, -.103f, Bk).Screw(.12f, -.065f, -.103f, Bk);
            it.Part("avr", "전압 조정기").Under("panel").Breakable(45).Box(.06f, .04f, .008f, .07f, -.035f, -.088f, Pcb, 0, .5f);
            it.Part("alt", "발전기 하우징").Rust().Under("stator", "panel", "avr")
                .Tube(.055f, .048f, .1f, .06f, -.06f, 0, AxX, yellow, .2f, .55f, true);

            // 바닥
            var feet = it.Part("feet", "방진 고무").Ext();
            foreach (float x in new[] { -.14f, .14f })
                foreach (float z in new[] { -.09f, .09f })
                    feet.Cyl(.012f, .012f, x, -.147f, z, AxY, Dark, 0, .3f);
            it.Part("ground", "접지 단자").Ext().Cyl(.006f, .015f, .135f, -.115f, -.1f, AxZ, Brass, .9f, .6f);
            return it;
        }
    }
}
