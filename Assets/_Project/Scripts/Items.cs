using System.Collections.Generic;
using UnityEngine;

namespace ReCraft
{
    /// <summary>아이템 데이터. 부품의 "Under" 관계와 나사만 지정하면 분해/조립 순서가 자동으로 정해진다.</summary>
    public static partial class Items
    {
        static readonly Quaternion AxZ = Quaternion.Euler(90, 0, 0);   // Y축 도형을 +Z로 눕힘 (아래면이 정면 -Z)
        static readonly Quaternion AxX = Quaternion.Euler(0, 0, -90);  // Y축 도형을 +X로 눕힘
        static readonly Quaternion AxY = Quaternion.identity;
        static readonly Vector3 Up = Vector3.up, Dn = Vector3.down, Fw = Vector3.forward, Bk = Vector3.back;

        static Color C(float r, float g, float b) => new(r, g, b, 1f);
        static readonly Color Steel = C(0.72f, 0.74f, 0.78f);
        static readonly Color Chrome = C(0.9f, 0.9f, 0.92f);
        static readonly Color Brass = C(0.85f, 0.65f, 0.25f);
        static readonly Color Dark = C(0.08f, 0.08f, 0.09f);
        static readonly Color Grey = C(0.35f, 0.36f, 0.38f);
        static readonly Color Pcb = C(0.1f, 0.42f, 0.2f);
        static readonly Color Cream = C(0.93f, 0.88f, 0.75f);
        static readonly Color Copper = C(0.8f, 0.45f, 0.25f);
        static readonly Color Wood = C(0.45f, 0.26f, 0.13f);

        static List<ItemDef> all;
        public static List<ItemDef> All => all ??= new List<ItemDef> { Radio(), Toaster(), Clock(), Flashlight(), Console(), Camera(), Typewriter(), Turntable(), Engine(), Arcade(), Generator(), MusicBox() };

        public static ItemDef Find(string id) => All.Find(i => i.id == id);

        // ───────────────────────── Tier 1 ─────────────────────────

        static ItemDef Radio()
        {
            var it = new ItemDef { id = "radio", name = "구형 라디오", tier = 1, value = 90, sound = TestSound.Hum, blurb = "지지직거리던 시절의 진공관 감성" };
            Color teal = C(0.1f, 0.55f, 0.55f);
            var body = it.Body("케이스").Rust();
            body.Box(.30f, .17f, .01f, 0, 0, -.045f, teal, .1f, .5f, true);
            body.Box(.30f, .01f, .08f, 0, .08f, .005f, teal, .1f, .5f, true);
            body.Box(.30f, .01f, .08f, 0, -.08f, .005f, teal, .1f, .5f, true);
            body.Box(.01f, .15f, .08f, -.145f, 0, .005f, teal, .1f, .5f, true);
            body.Box(.01f, .15f, .08f, .145f, 0, .005f, teal, .1f, .5f, true);
            for (int i = 0; i < 5; i++) body.Box(.11f, .008f, .003f, -.07f, -.04f + i * .02f, -.0515f, Dark, 0, .3f);

            it.Part("dial", "다이얼 창").Ext().Glow(C(1f, .7f, .2f))
                .Box(.10f, .035f, .004f, .07f, .035f, -.052f, Cream, 0, .7f)
                .Box(.002f, .03f, .002f, .085f, .035f, -.0545f, C(.8f, .1f, .1f), 0, .5f);
            it.Part("knob1", "튜닝 노브").Ext().Spin(Fw, 140, new Vector3(.045f, -.04f, 0))
                .Cyl(.016f, .014f, .045f, -.04f, -.057f, AxZ, Cream, 0, .6f)
                .Box(.003f, .012f, .002f, .045f, -.034f, -.0645f, Dark);
            it.Part("knob2", "볼륨 노브").Ext()
                .Cyl(.016f, .014f, .10f, -.04f, -.057f, AxZ, Cream, 0, .6f)
                .Box(.003f, .012f, .002f, .10f, -.034f, -.0645f, Dark);
            it.Part("antenna", "안테나").Ext().Rust().Wobble()
                .Cyl(.006f, .012f, .12f, .091f, .03f, AxY, Grey, .6f, .5f)
                .Cyl(.003f, .16f, .12f, .177f, .03f, AxY, Chrome, .95f, .8f)
                .Ball(.005f, .12f, .26f, .03f, Chrome, .95f, .8f);
            it.Part("back", "뒷판").Ext().Rust()
                .Box(.28f, .15f, .008f, 0, 0, .046f, C(.3f, .22f, .15f), 0, .3f)
                .Screw(-.125f, .06f, .05f, Fw).Screw(.125f, .06f, .05f, Fw).Screw(-.125f, -.06f, .05f, Fw).Screw(.125f, -.06f, .05f, Fw);
            it.Part("speaker", "스피커").Under("back").Breakable(18).Wobble()
                .Cone(.045f, .02f, .03f, -.07f, 0, -.024f, AxZ, C(.15f, .15f, .16f), 0, .3f)
                .Cyl(.02f, .012f, -.07f, 0, -.003f, AxZ, Steel, .8f, .5f);
            it.Part("board", "회로 기판").Under("back").Breakable(25).Rust()
                .Box(.11f, .10f, .004f, .075f, -.01f, 0, Pcb, 0, .5f)
                .Box(.025f, .02f, .008f, .06f, .01f, .006f, Dark, 0, .4f)
                .Cyl(.007f, .018f, .10f, .0f, .011f, AxZ, C(.1f, .2f, .6f), .3f, .6f)
                .Cyl(.005f, .014f, .085f, -.04f, .009f, AxZ, C(.7f, .5f, .1f), .3f, .6f)
                .Screw(.03f, .03f, .002f, Fw).Screw(.12f, -.05f, .002f, Fw);
            it.Part("battery", "배터리 팩").Under("back").Breakable(10)
                .Box(.035f, .03f, .035f, -.0f, -.055f, .016f, C(.75f, .6f, .1f), .2f, .5f)
                .Box(.037f, .008f, .037f, -.0f, -.055f, .016f, Dark, 0, .4f);
            return it;
        }

        static ItemDef Toaster()
        {
            var it = new ItemDef { id = "toaster", name = "녹슨 토스터기", tier = 1, value = 80, sound = TestSound.Click, blurb = "아침마다 빵을 튕겨 올리던 주방의 터줏대감" };
            Color red = C(.75f, .12f, .1f);
            var body = it.Body("본체").Rust();
            body.Box(.26f, .15f, .008f, 0, 0, -.071f, red, .1f, .6f, true);
            body.Box(.26f, .15f, .008f, 0, 0, .071f, red, .1f, .6f, true);
            body.Box(.008f, .15f, .134f, -.126f, 0, 0, Chrome, .95f, .75f);
            body.Box(.008f, .15f, .134f, .126f, 0, 0, Chrome, .95f, .75f);
            body.Box(.26f, .008f, .15f, 0, .079f, 0, Chrome, .95f, .75f);
            body.Box(.20f, .003f, .028f, 0, .0835f, -.03f, Dark, 0, .2f);
            body.Box(.20f, .003f, .028f, 0, .0835f, .03f, Dark, 0, .2f);
            body.Box(.002f, .09f, .008f, .1305f, .0f, 0, Dark, 0, .2f);

            it.Part("lever", "레버").Ext().Wobble()
                .Box(.014f, .02f, .03f, .138f, .03f, 0, Dark, 0, .6f);
            it.Part("knob", "굽기 조절 노브").Ext().Spin(Vector3.right, 120, new Vector3(.137f, -.045f, .04f))
                .Cyl(.014f, .012f, .137f, -.045f, .04f, AxX, Cream, 0, .6f)
                .Box(.002f, .012f, .003f, .144f, -.039f, .04f, Dark);
            it.Part("bottom", "바닥판").Ext().Rust()
                .Box(.25f, .006f, .14f, 0, -.078f, 0, Grey, .7f, .4f)
                .Cyl(.012f, .008f, -.10f, -.085f, -.05f, AxY, Dark, 0, .3f)
                .Cyl(.012f, .008f, .10f, -.085f, -.05f, AxY, Dark, 0, .3f)
                .Cyl(.012f, .008f, -.10f, -.085f, .05f, AxY, Dark, 0, .3f)
                .Cyl(.012f, .008f, .10f, -.085f, .05f, AxY, Dark, 0, .3f)
                .Screw(-.07f, -.081f, -.055f, Dn).Screw(.07f, -.081f, -.055f, Dn).Screw(-.07f, -.081f, .055f, Dn).Screw(.07f, -.081f, .055f, Dn);
            it.Part("crumb", "빵가루 받이").Under("bottom").Rust()
                .Box(.18f, .004f, .10f, 0, -.068f, 0, Steel, .8f, .5f);
            it.Part("heat1", "열선 A").Under("crumb").Breakable(15).Glow(C(1f, .35f, .05f))
                .Box(.19f, .10f, .006f, 0, 0, -.03f, C(.5f, .45f, .4f), .3f, .3f);
            it.Part("heat2", "열선 B").Under("crumb").Breakable(15).Glow(C(1f, .35f, .05f))
                .Box(.19f, .10f, .006f, 0, 0, .03f, C(.5f, .45f, .4f), .3f, .3f);
            it.Part("timer", "타이머 유닛").Under("crumb").Breakable(20)
                .Box(.03f, .05f, .036f, .105f, -.03f, 0, C(.2f, .25f, .5f), .2f, .5f)
                .Gear(.012f, .004f, 8, .105f, .002f, 0, AxY, Brass);
            return it;
        }

        static ItemDef Clock()
        {
            var it = new ItemDef { id = "clock", name = "태엽 알람시계", tier = 1, value = 110, sound = TestSound.Bell, blurb = "따르릉! 온 집안을 깨우던 쌍종 시계" };
            Color mint = C(.35f, .75f, .6f);
            var body = it.Body("케이스").Rust();
            body.Tube(.075f, .068f, .05f, 0, 0, 0, AxZ, mint, .2f, .5f, true);
            body.Cyl(.008f, .02f, -.04f, -.072f, 0, AxY, Brass, .9f, .6f);
            body.Cyl(.008f, .02f, .04f, -.072f, 0, AxY, Brass, .9f, .6f);
            body.Cyl(.003f, .02f, -.04f, .073f, 0, AxY, Brass, .9f, .6f);
            body.Cyl(.003f, .02f, .04f, .073f, 0, AxY, Brass, .9f, .6f);

            it.Part("bell1", "종 (왼쪽)").Ext().Rust().Wobble().Ball(.022f, -.045f, .098f, 0, Brass, .95f, .7f);
            it.Part("bell2", "종 (오른쪽)").Ext().Rust().Wobble().Ball(.022f, .045f, .098f, 0, Brass, .95f, .7f);
            it.Part("hammer", "해머").Ext().Wobble()
                .Cyl(.004f, .03f, 0, .09f, 0, AxY, Steel, .9f, .6f)
                .Ball(.007f, 0, .108f, 0, Steel, .9f, .6f);
            it.Part("bezel", "베젤").Ext().Rust()
                .Tube(.078f, .062f, .006f, 0, 0, -.028f, AxZ, Chrome, .95f, .8f);
            it.Part("hands", "시곗바늘").Under("bezel").Spin(Fw, -200, Vector3.zero)
                .Box(.004f, .05f, .002f, 0, .022f, -.021f, Dark, .3f, .5f)
                .Box(.032f, .005f, .002f, .014f, 0, -.0225f, Dark, .3f, .5f)
                .Cyl(.005f, .004f, 0, 0, -.022f, AxZ, C(.8f, .1f, .1f), .3f, .6f);
            it.Part("face", "문자판").Under("hands")
                .Cyl(.066f, .003f, 0, 0, -.016f, AxZ, Cream, 0, .5f)
                .Box(.004f, .012f, .001f, 0, .052f, -.018f, Dark).Box(.004f, .012f, .001f, 0, -.052f, -.018f, Dark)
                .Box(.012f, .004f, .001f, .052f, 0, -.018f, Dark).Box(.012f, .004f, .001f, -.052f, 0, -.018f, Dark);
            it.Part("key", "태엽 키").Ext().Spin(Fw, 220, new Vector3(0, -.02f, 0))
                .Cyl(.003f, .012f, 0, -.02f, .030f, AxZ, Brass, .9f, .6f)
                .Box(.03f, .014f, .003f, 0, -.02f, .0375f, Brass, .9f, .6f);
            var back = it.Part("back", "뒷판").Ext().Rust().Under("key")
                .Cyl(.068f, .004f, 0, 0, .022f, AxZ, Grey, .7f, .4f);
            for (int i = 0; i < 3; i++)
            {
                float a = (90 + i * 120) * Mathf.Deg2Rad;
                back.Screw(Mathf.Cos(a) * .055f, Mathf.Sin(a) * .055f, .024f, Fw);
            }
            it.Part("gear1", "큰 기어").Under("back").Rust().Breakable(12).Spin(Fw, 90, new Vector3(-.022f, .02f, 0))
                .Gear(.028f, .005f, 12, -.022f, .02f, 0, AxZ, Brass);
            it.Part("gear2", "작은 기어").Under("back").Rust().Spin(Fw, -150, new Vector3(.026f, -.006f, 0))
                .Gear(.02f, .005f, 9, .026f, -.006f, .006f, AxZ, Brass);
            it.Part("spring", "태엽통").Under("back").Breakable(22)
                .Cyl(.02f, .012f, -.012f, -.038f, .008f, AxZ, Steel, .9f, .6f)
                .Cyl(.005f, .016f, -.012f, -.038f, .008f, AxZ, Dark, .5f, .5f);
            return it;
        }

        static ItemDef Flashlight()
        {
            var it = new ItemDef { id = "flashlight", name = "손전등", tier = 1, value = 70, sound = TestSound.Click, blurb = "정전의 밤을 지켜 주던 든든한 불빛" };
            Color red = C(.8f, .15f, .1f);
            var body = it.Body("몸통").Rust();
            body.Tube(.02f, .016f, .13f, -.01f, 0, 0, AxX, red, .3f, .6f, true);

            it.Part("switch", "스위치").Ext().Breakable(8)
                .Box(.024f, .006f, .012f, 0, .023f, 0, Dark, 0, .5f)
                .Box(.008f, .004f, .008f, .005f, .028f, 0, C(.9f, .9f, .9f), 0, .6f)
                .Screw(-.007f, .026f, 0, Up);
            it.Part("tail", "꼬리 캡").Ext().Rust()
                .Cyl(.021f, .018f, -.084f, 0, 0, AxX, Chrome, .9f, .7f);
            it.Part("bat1", "건전지 A").Under("tail").Breakable(6)
                .Cyl(.0145f, .053f, -.045f, 0, 0, AxX, Copper, .6f, .6f)
                .Cyl(.005f, .003f, -.017f, 0, 0, AxX, Brass, .9f, .7f);
            it.Part("bat2", "건전지 B").Under("bat1").Breakable(6)
                .Cyl(.0145f, .053f, .012f, 0, 0, AxX, Copper, .6f, .6f)
                .Cyl(.005f, .003f, .040f, 0, 0, AxX, Brass, .9f, .7f);
            it.Part("ring", "렌즈 링").Ext().Rust()
                .Tube(.037f, .0295f, .026f, .106f, 0, 0, AxX, Chrome, .95f, .8f);
            it.Part("lens", "렌즈").Under("ring")
                .Cyl(.028f, .002f, .114f, 0, 0, AxX, C(.7f, .85f, .95f), .1f, .95f);
            it.Part("bulb", "전구").Under("lens").Breakable(9).Glow(C(1f, .95f, .7f))
                .Ball(.006f, .105f, 0, 0, C(.95f, .95f, .85f), 0, .9f);
            it.Part("reflector", "반사경").Under("bulb")
                .Cone(.012f, .027f, .006f, .098f, 0, 0, AxX, Chrome, 1f, .9f);
            it.Part("head", "헤드").Ext().Rust().Under("reflector")
                .Cone(.021f, .029f, .04f, .075f, 0, 0, AxX, Chrome, .9f, .7f);
            return it;
        }

        // ───────────────────────── Tier 2 ─────────────────────────

        static ItemDef Console()
        {
            var it = new ItemDef { id = "console", name = "레트로 게임기", tier = 2, value = 260, sound = TestSound.Beep, blurb = "건전지 네 개로 세상을 구하던 휴대용 게임기" };
            Color shell = C(.78f, .78f, .74f);
            var body = it.Body("앞 케이스");
            body.Box(.14f, .22f, .008f, 0, 0, -.016f, shell, 0, .5f, true);
            body.Box(.006f, .22f, .024f, -.067f, 0, 0, shell, 0, .5f, true);
            body.Box(.006f, .22f, .024f, .067f, 0, 0, shell, 0, .5f, true);
            body.Box(.128f, .006f, .024f, 0, .107f, 0, shell, 0, .5f, true);
            body.Box(.128f, .006f, .024f, 0, -.107f, 0, shell, 0, .5f, true);
            body.Box(.11f, .085f, .003f, 0, .05f, -.0215f, C(.25f, .26f, .3f), 0, .6f);

            it.Part("screen", "액정 화면").Ext().Breakable(40).Glow(C(.5f, .9f, .3f))
                .Box(.075f, .062f, .002f, 0, .052f, -.024f, C(.45f, .55f, .3f), 0, .85f);
            it.Part("dpad", "십자키").Ext()
                .Box(.036f, .012f, .006f, -.035f, -.035f, -.023f, Dark, 0, .5f)
                .Box(.012f, .036f, .006f, -.035f, -.035f, -.023f, Dark, 0, .5f);
            it.Part("btnA", "A 버튼").Ext().Cyl(.009f, .006f, .045f, -.025f, -.023f, AxZ, C(.65f, .1f, .35f), 0, .6f);
            it.Part("btnB", "B 버튼").Ext().Cyl(.009f, .006f, .022f, -.04f, -.023f, AxZ, C(.65f, .1f, .35f), 0, .6f);
            it.Part("start", "스타트·셀렉트").Ext()
                .Box(.016f, .005f, .004f, -.012f, -.078f, -.022f, Grey, 0, .4f, false, Quaternion.Euler(0, 0, 25))
                .Box(.016f, .005f, .004f, .012f, -.078f, -.022f, Grey, 0, .4f, false, Quaternion.Euler(0, 0, 25));
            it.Part("voldial", "볼륨 다이얼").Ext().Spin(Vector3.right, 160, new Vector3(.072f, .04f, 0))
                .Gear(.009f, .004f, 10, .072f, .04f, 0, AxX, Grey, .2f, .4f);
            it.Part("power", "전원 스위치").Ext()
                .Box(.014f, .005f, .006f, -.03f, .1125f, 0, Grey, 0, .4f);
            it.Part("cart", "게임 카트리지").Ext()
                .Box(.07f, .05f, .008f, 0, .07f, .020f, C(.5f, .5f, .52f), 0, .4f)
                .Box(.05f, .03f, .001f, 0, .072f, .0245f, C(.9f, .6f, .1f), 0, .6f);
            it.Part("batcover", "배터리 커버").Ext()
                .Box(.075f, .045f, .004f, 0, -.06f, .032f, shell, 0, .5f, true)
                .Box(.075f, .004f, .014f, 0, -.0375f, .023f, shell, 0, .5f, true)
                .Box(.075f, .004f, .014f, 0, -.0825f, .023f, shell, 0, .5f, true);
            it.Part("bat1", "건전지 A").Under("batcover").Breakable(6)
                .Cyl(.007f, .048f, 0, -.052f, .023f, AxX, Copper, .6f, .6f);
            it.Part("bat2", "건전지 B").Under("batcover").Breakable(6)
                .Cyl(.007f, .048f, 0, -.068f, .023f, AxX, Copper, .6f, .6f);
            it.Part("back", "뒷 케이스").Ext().Under("bat1", "bat2", "cart")
                .Box(.128f, .208f, .006f, 0, 0, .013f, shell, 0, .5f, true)
                .Screw(-.052f, .09f, .016f, Fw).Screw(.052f, .09f, .016f, Fw)
                .Screw(-.052f, 0, .016f, Fw).Screw(.052f, 0, .016f, Fw)
                .Screw(-.052f, -.095f, .016f, Fw).Screw(.052f, -.095f, .016f, Fw);
            it.Part("pcb", "메인 기판").Under("back").Breakable(55).Rust()
                .Box(.115f, .19f, .003f, 0, 0, .002f, Pcb, 0, .5f)
                .Box(.03f, .03f, .004f, 0, .03f, .0055f, Dark, 0, .5f)
                .Box(.02f, .012f, .004f, .03f, -.02f, .0055f, Dark, 0, .5f)
                .Cyl(.006f, .008f, -.035f, -.03f, .0075f, AxZ, C(.1f, .2f, .6f), .3f, .6f)
                .Screw(-.045f, .08f, .0035f, Fw).Screw(.045f, .08f, .0035f, Fw)
                .Screw(-.045f, -.08f, .0035f, Fw).Screw(.045f, -.08f, .0035f, Fw);
            it.Part("speaker", "스피커").Under("pcb").Breakable(20).Wobble()
                .Cyl(.018f, .006f, .035f, -.075f, -.008f, AxZ, C(.15f, .15f, .16f), .2f, .4f);
            return it;
        }

        static ItemDef Camera()
        {
            var it = new ItemDef { id = "camera", name = "필름 카메라", tier = 2, value = 300, sound = TestSound.Click, blurb = "찰칵, 한 장 한 장이 소중했던 시절" };
            Color leather = C(.16f, .14f, .13f);
            var body = it.Body("바디").Rust();
            body.Box(.15f, .085f, .006f, 0, 0, -.022f, leather, 0, .35f, true);
            body.Box(.15f, .006f, .044f, 0, -.0395f, .003f, Chrome, .9f, .7f);
            body.Box(.006f, .079f, .044f, -.072f, .003f, .003f, Chrome, .9f, .7f);
            body.Box(.006f, .079f, .044f, .072f, .003f, .003f, Chrome, .9f, .7f);

            it.Part("shutterbtn", "셔터 버튼").Ext()
                .Cyl(.006f, .006f, .045f, .0575f, -.008f, AxY, Chrome, .95f, .8f);
            it.Part("lever", "와인딩 레버").Ext().Rust().Spin(Up, 120, new Vector3(.058f, .0565f, .01f))
                .Cyl(.008f, .004f, .058f, .0565f, .01f, AxY, Chrome, .95f, .8f)
                .Box(.03f, .003f, .006f, .045f, .0575f, .016f, Chrome, .95f, .8f);
            it.Part("speeddial", "셔터 다이얼").Ext().Spin(Up, 150, new Vector3(.012f, .058f, 0))
                .Gear(.011f, .007f, 12, .012f, .058f, 0, AxY, Chrome, .95f, .7f);
            it.Part("finder", "뷰파인더").Ext()
                .Box(.032f, .016f, .05f, -.04f, .0625f, 0, Dark, .2f, .5f)
                .Box(.02f, .01f, .002f, -.04f, .0625f, -.0255f, C(.5f, .7f, .9f), .1f, .95f);
            it.Part("top", "상판").Ext().Rust().Under("shutterbtn", "lever", "speeddial", "finder")
                .Box(.15f, .012f, .05f, 0, .0485f, 0, Chrome, .95f, .75f)
                .Screw(-.068f, .0545f, -.019f, Up).Screw(.068f, .0545f, -.019f, Up)
                .Screw(-.068f, .0545f, .019f, Up).Screw(.0f, .0545f, .021f, Up);
            it.Part("ring", "포커스 링").Ext().Rust().Spin(Fw, 120, Vector3.zero)
                .Tube(.035f, .0305f, .012f, 0, 0, -.052f, AxZ, Dark, .4f, .5f)
                .Tube(.031f, .02f, .002f, 0, 0, -.0575f, AxZ, Chrome, .95f, .8f);
            it.Part("glass", "렌즈 유리").Under("ring").Breakable(35)
                .Cyl(.0235f, .003f, 0, 0, -.05f, AxZ, C(.4f, .55f, .75f), .2f, .97f);
            it.Part("barrel", "렌즈 경통").Ext().Rust().Under("glass")
                .Tube(.03f, .024f, .03f, 0, 0, -.040f, AxZ, C(.25f, .25f, .27f), .8f, .6f);
            it.Part("door", "필름실 덮개").Ext()
                .Box(.138f, .079f, .005f, 0, .003f, .0245f, leather, 0, .35f, true)
                .Screw(.062f, .03f, .027f, Fw).Screw(.062f, -.025f, .027f, Fw);
            it.Part("spool1", "필름 스풀 (좌)").Under("door").Spin(Up, 200, new Vector3(-.05f, 0, .004f))
                .Cyl(.011f, .06f, -.05f, 0, .004f, AxY, C(.6f, .5f, .2f), .2f, .5f)
                .Box(.024f, .002f, .004f, -.05f, .031f, .004f, Dark);
            it.Part("spool2", "필름 스풀 (우)").Under("door").Spin(Up, 200, new Vector3(.05f, 0, .004f))
                .Cyl(.011f, .06f, .05f, 0, .004f, AxY, C(.6f, .5f, .2f), .2f, .5f)
                .Box(.024f, .002f, .004f, .05f, .031f, .004f, Dark);
            it.Part("shutter", "셔터 유닛").Under("door").Breakable(45)
                .Box(.05f, .045f, .005f, 0, 0, -.0155f, Steel, .8f, .5f)
                .Cyl(.014f, .002f, 0, 0, -.012f, AxZ, Dark, .3f, .6f);
            it.Part("gear", "와인딩 기어").Under("top").Rust().Breakable(14).Spin(Up, -180, new Vector3(.05f, .037f, .004f))
                .Gear(.012f, .004f, 10, .05f, .037f, .004f, AxY, Brass);
            return it;
        }

        // ───────────────────────── 엔딩 유물 ─────────────────────────

        static ItemDef MusicBox()
        {
            var it = new ItemDef { id = "musicbox", name = "앤티크 오르골", tier = 4, value = 1500, sound = TestSound.Melody, blurb = "백 년을 잠들어 있던 멜로디. 공방의 마지막 의뢰." };
            var body = it.Body("나무 상자");
            body.Box(.22f, .008f, .15f, 0, -.046f, 0, Wood, 0, .35f, true);
            body.Box(.22f, .084f, .008f, 0, 0, -.071f, Wood, 0, .35f, true);
            body.Box(.22f, .084f, .008f, 0, 0, .071f, Wood, 0, .35f, true);
            body.Box(.008f, .084f, .134f, -.106f, 0, 0, Wood, 0, .35f, true);
            body.Box(.008f, .084f, .134f, .106f, 0, 0, Wood, 0, .35f, true);

            it.Part("feet", "받침 발").Ext().Rust()
                .Ball(.01f, -.095f, -.054f, -.06f, Brass, .95f, .7f).Ball(.01f, .095f, -.054f, -.06f, Brass, .95f, .7f)
                .Ball(.01f, -.095f, -.054f, .06f, Brass, .95f, .7f).Ball(.01f, .095f, -.054f, .06f, Brass, .95f, .7f);
            it.Part("crank", "크랭크").Ext().Rust().Spin(Vector3.right, 200, new Vector3(.125f, -.014f, -.015f))
                .Cyl(.004f, .03f, .125f, -.014f, -.015f, AxX, Brass, .95f, .7f)
                .Box(.004f, .03f, .006f, .141f, -.002f, -.015f, Brass, .95f, .7f)
                .Cyl(.005f, .016f, .150f, .012f, -.015f, AxX, Dark, 0, .5f);
            it.Part("clasp", "걸쇠").Ext().Rust()
                .Box(.022f, .022f, .004f, 0, .03f, -.077f, Brass, .95f, .7f)
                .Screw(0, .03f, -.079f, Bk);
            it.Part("lid", "뚜껑").Ext().Under("clasp")
                .Box(.224f, .01f, .154f, 0, .047f, 0, Wood, 0, .35f, true)
                .Box(.10f, .002f, .05f, 0, .053f, -.01f, Brass, .95f, .7f)
                .Screw(-.07f, .052f, .065f, Up).Screw(-.03f, .052f, .065f, Up).Screw(.03f, .052f, .065f, Up).Screw(.07f, .052f, .065f, Up);
            var drum = it.Part("drum", "실린더 드럼").Under("lid").Rust().Spin(Vector3.right, 60, new Vector3(-.03f, -.014f, -.015f))
                .Cyl(.016f, .10f, -.03f, -.014f, -.015f, AxX, Brass, .95f, .7f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 1.7f;
                drum.Box(.003f, .003f, .003f, -.07f + i * .013f, -.014f + Mathf.Sin(a) * .017f, -.015f + Mathf.Cos(a) * .017f, Steel, .9f, .6f);
            }
            it.Part("comb", "음계 빗").Under("lid").Rust().Breakable(80)
                .Box(.10f, .003f, .035f, -.03f, -.033f, .022f, Steel, .95f, .8f)
                .Box(.10f, .006f, .01f, -.03f, -.031f, .04f, Brass, .9f, .6f)
                .Screw(-.07f, -.028f, .04f, Up).Screw(.01f, -.028f, .04f, Up);
            it.Part("gear1", "구동 기어").Under("lid").Rust().Breakable(30).Spin(Vector3.right, 60, new Vector3(.035f, -.014f, -.015f))
                .Gear(.018f, .005f, 14, .035f, -.014f, -.015f, AxX, Brass);
            it.Part("gear2", "전달 기어").Under("lid").Rust().Spin(Vector3.right, -100, new Vector3(.045f, -.014f, .014f))
                .Gear(.011f, .005f, 9, .045f, -.014f, .014f, AxX, Brass);
            it.Part("spring", "태엽통").Under("lid").Breakable(60)
                .Cyl(.02f, .028f, .07f, -.014f, -.015f, AxX, Steel, .9f, .6f);
            it.Part("gov", "조속기").Under("lid").Spin(Vector3.right, 900, new Vector3(.045f, -.014f, .04f))
                .Box(.003f, .024f, .012f, .045f, -.014f, .04f, Brass, .95f, .7f);
            it.Part("doll", "발레리나 인형").Under("lid").Spin(Up, 120, new Vector3(.078f, 0, .042f))
                .Cyl(.014f, .004f, .078f, -.036f, .042f, AxY, C(.9f, .9f, .9f), 0, .6f)
                .Cone(.012f, .004f, .02f, .078f, -.024f, .042f, AxY, C(.95f, .6f, .7f), 0, .5f, true)
                .Cyl(.004f, .016f, .078f, -.006f, .042f, AxY, C(.95f, .8f, .7f), 0, .5f)
                .Ball(.006f, .078f, .008f, .042f, C(.95f, .8f, .7f), 0, .5f);
            it.Part("base", "황동 베이스").Rust().Under("drum", "comb", "gear1", "gear2", "spring", "gov", "doll")
                .Box(.19f, .004f, .12f, 0, -.040f, 0, Brass, .95f, .65f)
                .Screw(-.085f, -.038f, -.05f, Up).Screw(.085f, -.038f, -.05f, Up).Screw(-.085f, -.038f, .05f, Up).Screw(.085f, -.038f, .05f, Up);
            return it;
        }
    }
}
