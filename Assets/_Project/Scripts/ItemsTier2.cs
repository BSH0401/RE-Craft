using UnityEngine;

namespace ReCraft
{
    /// <summary>Tier 2 (정밀 기기) 추가분: 타자기 · 턴테이블. 부품 20개 안팎.</summary>
    public static partial class Items
    {
        // ───────────── 타자기 ─────────────
        static ItemDef Typewriter()
        {
            var it = new ItemDef { id = "typewriter", name = "타자기", tier = 2, value = 280, sound = TestSound.Typewriter, blurb = "타닥타닥, 띵! 원고지 대신 활자를 두드리던 시절" };
            Color enamel = C(.09f, .09f, .1f), gold = C(.85f, .68f, .3f);

            var body = it.Body("프레임").Rust();
            body.Box(.3f, .03f, .28f, 0, -.045f, 0, enamel, 0, .6f, true);
            body.Box(.02f, .08f, .24f, -.14f, -.01f, .01f, enamel, 0, .6f, true);
            body.Box(.02f, .08f, .24f, .14f, -.01f, .01f, enamel, 0, .6f, true);
            body.Box(.26f, .06f, .07f, 0, 0, .065f, enamel, 0, .6f, true);

            // 앞: 자판 4열 · 스페이스 바 / 앞 덮개 → 활자대
            for (int r = 0; r < 4; r++)
            {
                var row = it.Part("row" + (r + 1), $"자판 {r + 1}열").Ext().Rust();
                float capY = -.018f + r * .01f, z = -.12f + r * .02f;
                for (int k = 0; k < 10; k++)
                {
                    float x = -.099f + k * .022f + r * .005f;
                    float top = capY - .002f, bottom = -.03f;
                    row.Box(.003f, top - bottom, .003f, x, (top + bottom) * .5f, z, Steel, .9f, .6f);
                    row.Cyl(.009f, .004f, x, capY, z, AxY, Cream, 0, .75f);
                    row.Tube(.0105f, .0088f, .005f, x, capY, z, AxY, Chrome, .95f, .75f);
                }
            }
            it.Part("space", "스페이스 바").Ext().Rust()
                .Box(.16f, .006f, .012f, 0, -.024f, -.142f, Chrome, .95f, .75f)
                .Box(.003f, .006f, .003f, -.06f, -.028f, -.142f, Steel).Box(.003f, .006f, .003f, .06f, -.028f, -.142f, Steel);
            it.Part("front", "앞 덮개").Ext()
                .Box(.26f, .045f, .006f, 0, .012f, -.035f, enamel, 0, .6f, true)
                .Box(.08f, .012f, .002f, 0, .02f, -.0385f, gold, .9f, .7f)
                .Screw(-.11f, .012f, -.039f, Bk).Screw(.11f, .012f, -.039f, Bk);
            var bars = it.Part("typebars", "활자대").Under("front").Rust().Breakable(30).Wobble();
            for (int i = 0; i < 13; i++)
            {
                var rot = Quaternion.Euler(0, 0, -54f + i * 9f);
                Vector3 c = new Vector3(0, -.005f, 0) + rot * new Vector3(0, .02f, 0);
                bars.Box(.0025f, .04f, .003f, c.x, c.y, -.01f, Steel, .9f, .6f, false, rot);
            }

            // 위: 덮개 → 리본 스풀 · 이스케이프먼트 · 종
            it.Part("lid", "윗덮개").Ext()
                .Box(.26f, .008f, .07f, 0, .047f, .065f, enamel, 0, .6f, true)
                .Screw(-.11f, .051f, .04f, Up).Screw(.11f, .051f, .04f, Up).Screw(-.11f, .051f, .09f, Up).Screw(.11f, .051f, .09f, Up);
            it.Part("spoolL", "리본 스풀 (왼쪽)").Under("lid").Breakable(8).Spin(Up, 180, new Vector3(-.08f, .037f, .05f))
                .Cyl(.024f, .012f, -.08f, .037f, .05f, AxY, C(.6f, .1f, .1f), 0, .4f)
                .Cyl(.026f, .002f, -.08f, .044f, .05f, AxY, Steel, .9f, .6f);
            it.Part("spoolR", "리본 스풀 (오른쪽)").Under("lid").Breakable(8).Spin(Up, 180, new Vector3(.08f, .037f, .05f))
                .Cyl(.024f, .012f, .08f, .037f, .05f, AxY, Dark, 0, .4f)
                .Cyl(.026f, .002f, .08f, .044f, .05f, AxY, Steel, .9f, .6f);
            it.Part("escape", "이스케이프먼트").Under("lid").Rust().Breakable(35).Spin(Up, -240, new Vector3(0, .037f, .05f))
                .Gear(.015f, .005f, 14, 0, .037f, .05f, AxY, Brass, .9f, .6f);
            it.Part("bell", "벨").Under("lid").Rust().Cone(.012f, .004f, .01f, .04f, .04f, .085f, AxY, Brass, .95f, .75f);

            // 뒤: 노브 · 용지 받침 · 종이 누름대 → 플래튼 → 캐리지 레일 → 태엽
            it.Part("knobL", "플래튼 노브 (왼쪽)").Ext().Cyl(.018f, .02f, -.16f, .075f, .115f, AxX, Dark, .1f, .6f);
            it.Part("knobR", "플래튼 노브 (오른쪽)").Ext().Cyl(.018f, .02f, .16f, .075f, .115f, AxX, Dark, .1f, .6f);
            it.Part("bail", "종이 누름대").Ext().Rust()
                .Cyl(.003f, .24f, 0, .098f, .1f, AxX, Chrome, .95f, .75f)
                .Cyl(.006f, .01f, -.05f, .098f, .1f, AxX, Dark, 0, .4f)
                .Cyl(.006f, .01f, .05f, .098f, .1f, AxX, Dark, 0, .4f);
            it.Part("table", "용지 받침").Ext().Rust()
                .Box(.26f, .06f, .004f, 0, .11f, .14f, Chrome, .85f, .6f, false, Quaternion.Euler(-20, 0, 0));
            it.Part("platen", "플래튼 (고무 롤러)").Under("knobL", "knobR", "bail", "table").Breakable(25).Spin(Rt, 120, new Vector3(0, .075f, .115f))
                .Cyl(.02f, .26f, 0, .075f, .115f, AxX, C(.12f, .11f, .1f), 0, .35f);
            it.Part("lever", "캐리지 리턴 레버").Ext().Rust()
                .Box(.006f, .006f, .06f, -.15f, .095f, .085f, Chrome, .95f, .75f, false, Quaternion.Euler(0, 25, 0))
                .Ball(.006f, -.163f, .095f, .058f, Dark, 0, .6f);
            it.Part("rail", "캐리지 레일").Rust().Under("platen", "lever")
                .Box(.3f, .01f, .03f, 0, .055f, .115f, Chrome, .9f, .65f)
                .Screw(-.14f, .06f, .115f, Up).Screw(.14f, .06f, .115f, Up);
            it.Part("spring", "태엽 드럼").Under("rail").Breakable(40).Cyl(.018f, .01f, .1f, .037f, .115f, AxY, Steel, .9f, .6f);

            var feet = it.Part("feet", "고무 발").Ext();
            foreach (float x in new[] { -.13f, .13f })
                foreach (float z in new[] { -.11f, .11f })
                    feet.Cyl(.012f, .01f, x, -.065f, z, AxY, Dark, 0, .3f);
            return it;
        }

        // ───────────── 턴테이블 ─────────────
        static ItemDef Turntable()
        {
            var it = new ItemDef { id = "turntable", name = "턴테이블", tier = 2, value = 320, sound = TestSound.Record, blurb = "바늘을 내리면 지직— 거실을 채우던 LP의 온기" };
            Color walnut = C(.42f, .25f, .13f), plate = C(.26f, .26f, .28f);
            const float px = -.04f;   // 플래터 중심 X

            var body = it.Body("플린스");
            body.Box(.42f, .06f, .34f, 0, -.03f, 0, walnut, 0, .55f, true);
            body.Box(.41f, .004f, .33f, 0, .002f, 0, plate, .85f, .5f);

            // 플래터 계열: 레코드 → 매트 → 플래터 → 스핀들 · 벨트 → 풀리 → 모터
            it.Part("record", "LP 레코드").Ext().Spin(Up, 200, new Vector3(px, 0, 0))
                .Cyl(.145f, .002f, px, .022f, 0, AxY, C(.025f, .025f, .028f), 0, .55f)
                .Cyl(.045f, .0025f, px, .022f, 0, AxY, C(.8f, .2f, .15f), 0, .5f);
            it.Part("mat", "고무 매트").Under("record").Spin(Up, 200, new Vector3(px, 0, 0))
                .Cyl(.148f, .003f, px, .0195f, 0, AxY, Dark, 0, .25f);
            var platter = it.Part("platter", "플래터").Under("mat").Rust().Spin(Up, 200, new Vector3(px, 0, 0))
                .Cyl(.15f, .014f, px, .011f, 0, AxY, Alu, .9f, .65f);
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                platter.Box(.004f, .006f, .002f, px + Mathf.Cos(a) * .1505f, .011f, Mathf.Sin(a) * .1505f, Dark, .2f, .4f, false, Quaternion.Euler(0, -i * 15f + 90f, 0));
            }
            it.Part("spindle", "스핀들").Under("platter").Cyl(.003f, .016f, px, .03f, 0, AxY, Chrome, .95f, .8f);
            it.Part("belt", "구동 벨트").Under("platter").Breakable(12).Tube(.06f, .056f, .004f, px, -.004f, 0, AxY, Dark, 0, .3f);
            it.Part("pulley", "모터 풀리").Under("belt").Cyl(.008f, .01f, .06f, -.004f, .08f, AxY, Brass, .9f, .65f);
            it.Part("motor", "모터").Under("pulley", "bottom").Breakable(45).Cyl(.02f, .03f, .06f, -.03f, .08f, AxY, Steel, .8f, .5f);

            // 톤암: 바늘 카트리지 → 헤드쉘 · 카운터웨이트 → 톤암 → 피벗
            Vector3 pivot = new(.15f, .03f, .1f), tip = new(.045f, .03f, -.065f);
            Vector3 dir = (tip - pivot).normalized;
            Quaternion armRot = Quaternion.FromToRotation(Vector3.up, dir);
            Quaternion yaw = Quaternion.LookRotation(dir, Vector3.up);
            it.Part("stylus", "바늘 카트리지").Ext().Breakable(20)
                .Box(.01f, .008f, .014f, tip.x, tip.y - .004f, tip.z, C(.75f, .15f, .1f), .2f, .6f, false, yaw);
            it.Part("headshell", "헤드쉘").Ext().Under("stylus")
                .Box(.016f, .004f, .03f, tip.x, tip.y + .002f, tip.z - dir.z * .004f, Alu, .9f, .7f, false, yaw)
                .Box(.004f, .004f, .012f, tip.x + .011f, tip.y + .004f, tip.z, Alu, .9f, .7f, false, yaw);
            Vector3 cw = pivot - dir * .03f;
            it.Part("weight", "카운터웨이트").Ext().Cyl(.012f, .02f, cw.x, cw.y, cw.z, armRot, Dark, .4f, .55f);
            float len = Vector3.Distance(pivot, tip);
            Vector3 mid = (pivot + tip) * .5f;
            it.Part("tonearm", "톤암").Ext().Rust().Under("headshell", "weight").Wobble()
                .Cyl(.004f, len, mid.x, mid.y, mid.z, armRot, Chrome, .95f, .75f);
            it.Part("armbase", "톤암 베이스").Rust().Under("tonearm")
                .Cyl(.018f, .02f, pivot.x, .012f, pivot.z, AxY, Chrome, .9f, .7f)
                .Cyl(.008f, .01f, pivot.x, .027f, pivot.z, AxY, Dark, .4f, .5f);
            it.Part("rest", "암 레스트").Ext().Cyl(.003f, .022f, .18f, .013f, -.01f, AxY, Chrome, .9f, .7f).Box(.01f, .004f, .006f, .18f, .025f, -.01f, Dark);
            it.Part("cue", "큐잉 레버").Ext().Box(.008f, .01f, .02f, .19f, .009f, .06f, Chrome, .9f, .7f);

            // 조작부
            it.Part("speed", "회전수 선택 노브").Ext().Rust()
                .Cyl(.012f, .008f, -.18f, .008f, -.15f, AxY, Chrome, .9f, .7f)
                .Box(.002f, .002f, .01f, -.18f, .013f, -.155f, Dark);
            it.Part("start", "시작/정지 버튼").Ext().Box(.024f, .006f, .016f, -.135f, .007f, -.152f, Chrome, .9f, .7f);
            it.Part("pitch", "피치 슬라이더").Ext().Box(.01f, .008f, .016f, .175f, .008f, -.12f, Dark, .2f, .5f);
            it.Part("strobe", "스트로보 램프").Ext().Glow(C(1f, .35f, .2f)).Cyl(.006f, .006f, .12f, .007f, -.15f, AxY, C(.7f, .2f, .15f), 0, .9f);

            // 아래 · 뒤
            it.Part("bottom", "바닥판").Ext().Rust()
                .Box(.4f, .004f, .32f, 0, -.062f, 0, plate, .7f, .45f)
                .Screw(-.17f, -.064f, -.13f, Dn).Screw(.17f, -.064f, -.13f, Dn).Screw(-.17f, -.064f, .13f, Dn).Screw(.17f, -.064f, .13f, Dn);
            var feet = it.Part("feet", "방진 인슐레이터").Ext();
            foreach (float x in new[] { -.18f, .18f })
                foreach (float z in new[] { -.14f, .14f })
                    feet.Cyl(.022f, .015f, x, -.0715f, z, AxY, Dark, .3f, .5f);
            it.Part("rca", "RCA 단자").Ext()
                .Box(.06f, .02f, .006f, .12f, -.03f, .173f, Dark, .2f, .4f)
                .Cyl(.004f, .008f, .105f, -.03f, .178f, AxZ, C(.85f, .1f, .1f), .5f, .6f)
                .Cyl(.004f, .008f, .135f, -.03f, .178f, AxZ, Cream, .5f, .6f);
            return it;
        }
    }
}
