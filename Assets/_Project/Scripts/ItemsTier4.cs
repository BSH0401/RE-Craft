using UnityEngine;

namespace ReCraft
{
    /// <summary>Tier 4 (희귀 유물) 추가분: 수동 기계식 시계 · 낡은 로봇 팔. 오르골과 함께 셋을 모두 복원하면 엔딩.</summary>
    public static partial class Items
    {
        static readonly Color Gold = C(.86f, .68f, .32f);

        // ───────────── 수동 기계식 시계 (회중시계) ─────────────
        static ItemDef Watch()
        {
            var it = new ItemDef { id = "watch", name = "수동 기계식 시계", tier = 4, value = 1300, sound = TestSound.Watch, blurb = "할아버지의 할아버지가 태엽을 감던 회중시계. 전설의 유물", screwScale = 0.4f };

            var body = it.Body("케이스").Rust();
            body.Tube(.05f, .045f, .016f, 0, 0, 0, AxZ, Gold, .95f, .7f);

            // 줄 → 고리 → 용두 → 태엽 축
            var chain = it.Part("chain", "시계줄").Ext().Rust();
            for (int i = 0; i < 8; i++)
            {
                float x = .008f + i * .009f, y = .084f - i * i * .0012f;
                chain.Tube(.0045f, .003f, .0015f, x, y, 0, i % 2 == 0 ? AxZ : AxX, Gold, .95f, .7f);
            }
            it.Part("bow", "고리").Ext().Rust().Under("chain").Tube(.012f, .009f, .003f, 0, .073f, 0, AxZ, Gold, .95f, .7f);
            it.Part("crown", "용두").Ext().Rust().Under("bow")
                .Gear(.007f, .01f, 16, 0, .057f, 0, AxY, Gold, .95f, .7f)
                .Cyl(.003f, .006f, 0, .064f, 0, AxY, Gold, .95f, .7f);
            it.Part("stem", "태엽 축").Under("crown").Cyl(.002f, .016f, 0, .044f, 0, AxY, Steel, .9f, .6f);

            // 앞: 베젤 → 유리 → 바늘 → 문자판
            it.Part("bezel", "베젤").Ext().Rust().Tube(.051f, .043f, .004f, 0, 0, -.009f, AxZ, Gold, .95f, .75f);
            // 유리: 불투명 셰이더라 원판이면 문자판을 가린다 → 가장자리 링만 두어 유리 테두리처럼 보이게
            it.Part("crystal", "유리").Under("bezel").Breakable(25).Tube(.044f, .039f, .002f, 0, 0, -.0075f, AxZ, C(.75f, .82f, .88f), .1f, .97f);
            it.Part("hands", "시곗바늘").Under("crystal").Spin(Fw, -240, Vector3.zero)
                .Box(.003f, .022f, .0006f, 0, .009f, -.0058f, Dark, .5f, .6f)
                .Box(.002f, .034f, .0006f, .013f, .0f, -.0062f, Dark, .5f, .6f, false, Quaternion.Euler(0, 0, -90))
                .Cyl(.0025f, .0012f, 0, 0, -.0064f, AxZ, Gold, .95f, .7f);
            var dial = it.Part("dial", "문자판").Under("hands")
                .Cyl(.043f, .0015f, 0, 0, -.0045f, AxZ, Cream, 0, .6f)
                .Tube(.009f, .0082f, .0006f, 0, -.022f, -.0054f, AxZ, Dark, .2f, .5f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                bool major = i % 3 == 0;
                dial.Box(major ? .002f : .0012f, major ? .007f : .004f, .0005f, Mathf.Sin(a) * .036f, Mathf.Cos(a) * .036f, -.0054f, Dark, .2f, .5f, false, Quaternion.Euler(0, 0, -i * 30f));
            }

            // 뒤: 뒷뚜껑 → 브리지 3개 → 무브먼트
            it.Part("back", "뒷뚜껑").Ext().Rust()
                .Cyl(.05f, .003f, 0, 0, .0095f, AxZ, Gold, .95f, .7f)
                .Tube(.03f, .028f, .0006f, 0, 0, .0112f, AxZ, Gold * .85f, .95f, .7f)
                .Screw(0, .042f, .011f, Fw).Screw(-.036f, -.021f, .011f, Fw).Screw(.036f, -.021f, .011f, Fw);
            it.Part("ratchet", "래칫 휠").Under("back").Rust().Gear(.01f, .0015f, 20, -.018f, .01f, .0068f, AxZ, Steel, .95f, .75f);
            it.Part("bbridge", "배럴 브리지").Under("back", "ratchet").Rust()
                .Cyl(.018f, .0015f, -.018f, .01f, .0052f, AxZ, Brass, .9f, .65f)
                .Ball(.0016f, -.006f, .02f, .0058f, C(.8f, .1f, .15f), .1f, .9f)
                .Screw(-.032f, .012f, .006f, Fw).Screw(-.012f, -.004f, .006f, Fw);
            it.Part("tbridge", "기어 트레인 브리지").Under("back").Rust()
                .Box(.026f, .034f, .0015f, .014f, .003f, .0052f, Brass, .9f, .65f)
                .Ball(.0016f, .014f, .012f, .0058f, C(.8f, .1f, .15f), .1f, .9f)
                .Ball(.0016f, .019f, -.002f, .0058f, C(.8f, .1f, .15f), .1f, .9f)
                .Screw(.006f, .016f, .006f, Fw).Screw(.022f, -.01f, .006f, Fw);
            it.Part("lbridge", "밸런스 브리지").Under("back").Rust()
                .Box(.024f, .008f, .0015f, .022f, -.024f, .0052f, Brass, .9f, .65f, false, Quaternion.Euler(0, 0, 20))
                .Ball(.0016f, .022f, -.024f, .0058f, C(.8f, .1f, .15f), .1f, .9f)
                .Screw(.012f, -.028f, .006f, Fw).Screw(.033f, -.02f, .006f, Fw);
            it.Part("barrel", "태엽통").Under("bbridge").Breakable(70).Spin(Fw, 30, new Vector3(-.018f, .01f, 0))
                .Cyl(.015f, .004f, -.018f, .01f, .002f, AxZ, Brass, .9f, .6f)
                .Gear(.0155f, .001f, 40, -.018f, .01f, .0035f, AxZ, Brass, .9f, .6f);
            it.Part("center", "센터 휠").Under("tbridge").Rust().Spin(Fw, -90, Vector3.zero)
                .Gear(.012f, .0012f, 30, 0, 0, .0012f, AxZ, Brass, .9f, .65f);
            it.Part("wheel3", "3번 휠").Under("tbridge").Rust().Spin(Fw, 200, new Vector3(.014f, .012f, 0))
                .Gear(.008f, .0012f, 24, .014f, .012f, .0026f, AxZ, Brass, .9f, .65f);
            it.Part("wheel4", "4번 휠").Under("tbridge").Rust().Spin(Fw, -360, new Vector3(.019f, -.002f, 0))
                .Gear(.007f, .0012f, 20, .019f, -.002f, .0034f, AxZ, Brass, .9f, .65f);
            it.Part("escape", "이스케이프 휠").Under("tbridge").Breakable(40).Spin(Fw, 600, new Vector3(.008f, -.015f, 0))
                .Gear(.006f, .001f, 15, .008f, -.015f, .0036f, AxZ, Steel, .95f, .75f);
            it.Part("pallet", "앵커 (팔레트 포크)").Under("tbridge").Wobble()
                .Box(.012f, .002f, .001f, .015f, -.019f, .0036f, Steel, .95f, .75f, false, Quaternion.Euler(0, 0, 30));
            it.Part("balance", "밸런스 휠").Under("lbridge").Rust().Spin(Fw, 900, new Vector3(.022f, -.024f, 0))
                .Tube(.011f, .0095f, .0015f, .022f, -.024f, .0025f, AxZ, Gold, .95f, .7f)
                .Box(.02f, .0012f, .001f, .022f, -.024f, .0025f, Gold, .95f, .7f);
            it.Part("hairspring", "헤어스프링").Under("balance").Breakable(60)
                .Tube(.006f, .0052f, .0006f, .022f, -.024f, .0036f, AxZ, C(.25f, .35f, .7f), .9f, .7f)
                .Tube(.0042f, .0036f, .0006f, .022f, -.024f, .0036f, AxZ, C(.25f, .35f, .7f), .9f, .7f);
            return it;
        }

        // ───────────── 낡은 로봇 팔 ─────────────
        static ItemDef RobotArm()
        {
            var it = new ItemDef { id = "robotarm", name = "낡은 로봇 팔", tier = 4, value = 1700, sound = TestSound.Robot, blurb = "폐공장에서 건져 낸 산업용 로봇 팔. 다시 움직일 수 있을까?" };
            Color orange = C(.93f, .5f, .1f), joint = C(.22f, .23f, .25f);
            Vector3 swivel = Vector3.zero;   // 시험 가동: 터렛 위 전부가 Y축으로 함께 돈다
            const float sw = 60f;

            var body = it.Body("베이스").Rust();
            body.Cyl(.085f, .04f, 0, -.15f, 0, AxY, orange, .3f, .55f, true);
            body.Box(.22f, .01f, .22f, 0, -.175f, 0, joint, .6f, .45f);

            // 그리퍼 → 손목
            it.Part("fingerL", "그리퍼 손가락 (왼쪽)").Ext().Rust().Spin(Up, sw, swivel)
                .Box(.008f, .04f, .012f, -.016f, .03f, -.145f, Steel, .9f, .6f)
                .Box(.012f, .006f, .012f, -.012f, .012f, -.145f, joint, .5f, .5f);
            it.Part("fingerR", "그리퍼 손가락 (오른쪽)").Ext().Rust().Spin(Up, sw, swivel)
                .Box(.008f, .04f, .012f, .016f, .03f, -.145f, Steel, .9f, .6f)
                .Box(.012f, .006f, .012f, .012f, .012f, -.145f, joint, .5f, .5f);
            it.Part("gripper", "그리퍼 본체").Rust().Under("fingerL", "fingerR").Spin(Up, sw, swivel)
                .Box(.05f, .02f, .03f, 0, .06f, -.145f, joint, .6f, .5f);
            it.Part("wcover", "손목 서보 덮개").Ext().Rust().Spin(Up, sw, swivel)
                .Box(.034f, .004f, .034f, 0, .122f, -.135f, orange, .3f, .55f, true)
                .Screw(-.012f, .124f, -.135f, Up).Screw(.012f, .124f, -.135f, Up);
            it.Part("wservo", "손목 서보").Under("wcover").Breakable(50).Spin(Up, sw, swivel)
                .Box(.03f, .02f, .03f, 0, .11f, -.135f, Dark, .2f, .45f);
            it.Part("wrist", "손목 관절").Rust().Under("gripper", "wservo").Spin(Up, sw, swivel)
                .Cyl(.02f, .04f, 0, .08f, -.135f, AxX, joint, .6f, .5f);

            // 아래팔
            it.Part("fcover", "아래팔 덮개").Ext().Rust().Spin(Up, sw, swivel)
                .Box(.032f, .004f, .11f, 0, .0985f, -.07f, orange, .3f, .55f, true)
                .Screw(0, .1005f, -.03f, Up).Screw(0, .1005f, -.11f, Up);
            it.Part("harness", "배선 묶음").Ext().Breakable(20).Spin(Up, sw, swivel)
                .Cyl(.004f, .15f, .028f, .01f, .022f, AxY, C(.75f, .15f, .1f), 0, .45f)
                .Cyl(.004f, .15f, .02f, .01f, .026f, AxY, Dark, 0, .45f)
                .Cyl(.004f, .12f, .022f, .085f, -.065f, AxZ, C(.75f, .15f, .1f), 0, .45f);
            it.Part("forearm", "아래팔").Rust().Under("fcover", "wrist", "harness").Spin(Up, sw, swivel)
                .Box(.036f, .034f, .13f, 0, .08f, -.065f, joint, .6f, .5f);

            // 팔꿈치
            it.Part("ecover", "팔꿈치 서보 덮개").Ext().Rust().Spin(Up, sw, swivel)
                .Cyl(.026f, .005f, .065f, .08f, 0, AxX, orange, .3f, .55f, true)
                .Screw(.068f, .095f, 0, Rt).Screw(.068f, .065f, 0, Rt);
            it.Part("eservo", "팔꿈치 서보").Under("ecover").Breakable(60).Spin(Up, sw, swivel)
                .Cyl(.022f, .025f, .05f, .08f, 0, AxX, Dark, .2f, .45f);
            it.Part("elbow", "팔꿈치 관절").Rust().Under("forearm", "eservo").Spin(Up, sw, swivel)
                .Cyl(.028f, .07f, 0, .08f, 0, AxX, joint, .6f, .5f);

            // 위팔
            it.Part("ucoverL", "위팔 덮개 (왼쪽)").Ext().Rust().Spin(Up, sw, swivel)
                .Box(.004f, .11f, .036f, -.0245f, .01f, 0, orange, .3f, .55f, true)
                .Screw(-.027f, .05f, 0, Lf).Screw(-.027f, -.03f, 0, Lf);
            it.Part("ucoverR", "위팔 덮개 (오른쪽)").Ext().Rust().Spin(Up, sw, swivel)
                .Box(.004f, .11f, .036f, .0245f, .01f, 0, orange, .3f, .55f, true)
                .Screw(.027f, .05f, 0, Rt).Screw(.027f, -.03f, 0, Rt);
            it.Part("upper", "위팔").Rust().Under("ucoverL", "ucoverR", "elbow").Spin(Up, sw, swivel)
                .Box(.045f, .14f, .04f, 0, .01f, 0, joint, .6f, .5f);

            // 어깨 → 터렛
            var scover = it.Part("scover", "어깨 서보 덮개").Ext().Rust().Spin(Up, sw, swivel)
                .Cyl(.032f, .006f, -.083f, -.06f, 0, AxX, orange, .3f, .55f, true);
            scover.Screw(-.087f, -.04f, 0, Lf).Screw(-.087f, -.07f, -.017f, Lf).Screw(-.087f, -.07f, .017f, Lf);
            it.Part("sservo", "어깨 서보").Under("scover").Breakable(70).Spin(Up, sw, swivel)
                .Cyl(.028f, .03f, -.065f, -.06f, 0, AxX, Dark, .2f, .45f);
            it.Part("shoulder", "어깨 관절").Rust().Under("upper", "sservo").Spin(Up, sw, swivel)
                .Cyl(.035f, .09f, 0, -.06f, 0, AxX, joint, .6f, .5f);
            it.Part("turret", "회전 터렛").Rust().Under("shoulder").Spin(Up, sw, swivel)
                .Cyl(.06f, .04f, 0, -.11f, 0, AxY, orange, .3f, .55f, true)
                .Box(.012f, .05f, .05f, -.05f, -.07f, 0, orange, .3f, .55f, true)
                .Box(.012f, .05f, .05f, .05f, -.07f, 0, orange, .3f, .55f, true);
            it.Part("bearing", "선회 베어링").Rust().Under("turret").Tube(.065f, .058f, .006f, 0, -.128f, 0, AxY, Steel, .9f, .6f);

            // 베이스 주변
            it.Part("mcover", "베이스 모터 덮개").Ext().Rust()
                .Box(.004f, .025f, .05f, .088f, -.15f, 0, orange, .3f, .55f, true)
                .Screw(.09f, -.15f, -.018f, Rt).Screw(.09f, -.15f, .018f, Rt);
            it.Part("bmotor", "베이스 모터").Under("mcover", "bearing").Breakable(50).Cyl(.018f, .03f, .07f, -.15f, 0, AxX, Dark, .2f, .45f);
            it.Part("panel", "제어 패널 덮개").Ext().Rust()
                .Box(.06f, .025f, .004f, 0, -.15f, -.087f, joint, .5f, .5f)
                .Screw(-.026f, -.14f, -.089f, Bk).Screw(.026f, -.14f, -.089f, Bk).Screw(-.026f, -.16f, -.089f, Bk).Screw(.026f, -.16f, -.089f, Bk);
            it.Part("board", "제어 기판").Under("panel").Rust().Breakable(60).Box(.05f, .02f, .003f, 0, -.15f, -.08f, Pcb, 0, .5f);
            it.Part("estop", "비상 정지 버튼").Ext()
                .Cyl(.014f, .003f, .055f, -.1285f, -.04f, AxY, C(.95f, .8f, .1f), .1f, .6f)
                .Cyl(.009f, .008f, .055f, -.123f, -.04f, AxY, C(.85f, .1f, .1f), 0, .7f);
            it.Part("led", "상태 표시등").Ext().Glow(C(.3f, 1f, .4f)).Ball(.005f, -.055f, -.128f, -.045f, C(.3f, .8f, .4f), 0, .9f);
            it.Part("plug", "전원 커넥터").Ext().Cyl(.008f, .012f, .05f, -.15f, .088f, AxZ, Dark, .3f, .5f);
            var anchors = it.Part("anchors", "고정 볼트").Ext().Rust();
            foreach (float x in new[] { -.095f, .095f })
                foreach (float z in new[] { -.095f, .095f })
                    anchors.Gear(.008f, .008f, 6, x, -.166f, z, AxY, Steel, .9f, .6f);
            return it;
        }
    }
}
