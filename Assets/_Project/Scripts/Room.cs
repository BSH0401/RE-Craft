using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ReCraft
{
    /// <summary>공방 환경(바닥·벽·창·작업대·소품·조명·후처리)을 코드로 만든다. 색은 전부 Palette에서.</summary>
    public static class Room
    {
        public const float BenchTop = 0.9f;
        public const float TrayTop = BenchTop + 0.011f;       // 부품 트레이 바닥 윗면
        public const float ScrewTrayTop = BenchTop + 0.012f;  // 나사 트레이 바닥 윗면
        public static DepthOfField Dof;
        static Transform standRod, standTop;

        static readonly Quaternion AxZ = Quaternion.Euler(90, 0, 0);

        static Material Make(float pattern = 0f, float wood = 0f, float noise = 30f, Color? emission = null)
        {
            var m = Mat.Surface();
            m.SetFloat("_Pattern", pattern);
            m.SetFloat("_Wood", wood);
            m.SetFloat("_NoiseScale", noise);
            if (emission.HasValue) m.SetColor("_Emission", emission.Value);
            return m;
        }

        public static void Build(Transform parent)
        {
            var root = new GameObject("Room").transform;
            root.SetParent(parent, false);

            var plain = Make();
            var wood = Make(0f, 1f);

            // ── 바닥 · 벽 ──
            Prop(root, "Floor", Make(1f, 0.5f), new PartDef().Box(7, .1f, 7, 0, -.05f, 0, Palette.Floor, 0, .45f));
            Prop(root, "Walls", Make(2f), new PartDef()
                .Box(7, 3.4f, .1f, 0, 1.7f, 1.7f, Palette.Wall, 0, .15f)
                .Box(.1f, 3.4f, 7, -3.2f, 1.7f, 0, Palette.Wall, 0, .15f)
                .Box(.1f, 3.4f, 7, 3.2f, 1.7f, 0, Palette.Wall, 0, .15f));
            var panel = new PartDef()
                .Box(6.3f, 1.0f, .03f, 0, .5f, 1.635f, Palette.Wainscot, 0, .35f)
                .Box(6.3f, .05f, .05f, 0, 1.02f, 1.625f, Palette.Trim, 0, .4f)
                .Box(6.3f, .12f, .04f, 0, .06f, 1.61f, Palette.Trim, 0, .35f);
            panel.Box(6.3f, .035f, .02f, 0, .55f, 1.615f, Palette.Trim, 0, .4f);
            Prop(root, "Wainscot", wood, panel);

            // ── 창문 (뒷벽 왼쪽): 차가운 하늘빛이 은은히 번진다 ──
            Prop(root, "WindowGlass", Make(emission: Palette.Sky * 1.7f), new PartDef()
                .Box(.92f, 1.02f, .01f, -1.55f, 1.95f, 1.648f, Palette.Sky, 0, .95f));
            Prop(root, "WindowFrame", wood, new PartDef()
                .Box(1.06f, .07f, .08f, -1.55f, 2.495f, 1.62f, Palette.Trim, 0, .4f)
                .Box(1.12f, .06f, .14f, -1.55f, 1.41f, 1.6f, Palette.Trim, 0, .4f)
                .Box(.07f, 1.1f, .08f, -2.045f, 1.95f, 1.62f, Palette.Trim, 0, .4f)
                .Box(.07f, 1.1f, .08f, -1.055f, 1.95f, 1.62f, Palette.Trim, 0, .4f)
                .Box(.035f, 1.0f, .04f, -1.55f, 1.95f, 1.63f, Palette.Trim, 0, .4f)
                .Box(.95f, .035f, .04f, -1.55f, 1.95f, 1.63f, Palette.Trim, 0, .4f));
            // 창턱 화분
            Prop(root, "Plant", plain, new PartDef()
                .Cone(.045f, .06f, .09f, -1.3f, 1.485f, 1.58f, Quaternion.identity, Palette.PropsAlt, 0, .35f)
                .Ball(.06f, -1.3f, 1.57f, 1.58f, Palette.Props, 0, .3f)
                .Ball(.045f, -1.26f, 1.62f, 1.56f, Palette.Props * 1.15f, 0, .3f)
                .Ball(.04f, -1.34f, 1.61f, 1.6f, Palette.Props * 0.9f, 0, .3f));

            // ── 작업대 ──
            Color benchDark = Palette.BenchDark;
            Prop(root, "Bench", wood, new PartDef()
                .Box(2.3f, .06f, 1.0f, 0, BenchTop - .03f, .02f, Palette.Bench, 0, .5f)
                .Box(2.2f, .12f, .03f, 0, BenchTop - .12f, -.45f, benchDark, 0, .35f)
                .Box(.09f, .84f, .09f, -1.05f, .42f, -.38f, benchDark, 0, .35f)
                .Box(.09f, .84f, .09f, 1.05f, .42f, -.38f, benchDark, 0, .35f)
                .Box(.09f, .84f, .09f, -1.05f, .42f, .42f, benchDark, 0, .35f)
                .Box(.09f, .84f, .09f, 1.05f, .42f, .42f, benchDark, 0, .35f)
                .Box(2.1f, .04f, .8f, 0, .18f, .02f, benchDark, 0, .35f));

            // 커팅 매트 (가운데)
            Prop(root, "CuttingMat", Make(4f), new PartDef()
                .Box(.6f, .004f, .52f, 0, BenchTop + .002f, -.02f, Palette.Mat, 0, .3f));
            // 부품 트레이 (좌우, 테두리가 있는 강철 판)
            var trays = new PartDef();
            foreach (float sx in new[] { -.6f, .6f })
            {
                trays.Box(.62f, .008f, .62f, sx, BenchTop + .007f, -.03f, Palette.Tray, .8f, .45f);
                trays.Box(.64f, .02f, .012f, sx, BenchTop + .013f, -.346f, Palette.Tray, .8f, .5f);
                trays.Box(.64f, .02f, .012f, sx, BenchTop + .013f, .286f, Palette.Tray, .8f, .5f);
                trays.Box(.012f, .02f, .62f, sx - .316f, BenchTop + .013f, -.03f, Palette.Tray, .8f, .5f);
                trays.Box(.012f, .02f, .62f, sx + .316f, BenchTop + .013f, -.03f, Palette.Tray, .8f, .5f);
            }
            // 자석 나사 트레이
            trays.Box(.46f, .01f, .1f, -.445f, BenchTop + .007f, .37f, Palette.Tray, .85f, .55f);
            trays.Box(.42f, .003f, .06f, -.445f, BenchTop + .0125f, .37f, Palette.PropsAlt, .2f, .5f);
            trays.Box(.47f, .02f, .01f, -.445f, BenchTop + .012f, .32f, Palette.Tray, .85f, .55f);
            trays.Box(.47f, .02f, .01f, -.445f, BenchTop + .012f, .42f, Palette.Tray, .85f, .55f);
            Prop(root, "Trays", plain, trays);

            // ── 작업 거치대: 물건 아래 받침대 (높이는 아이템마다 SetStand로 맞춘다) ──
            Prop(root, "StandBase", plain, new PartDef()
                .Cyl(.06f, .012f, 0, BenchTop + .01f, 0, Quaternion.identity, Palette.Tray, .85f, .55f)
                .Cyl(.045f, .004f, 0, BenchTop + .018f, 0, Quaternion.identity, Palette.Steel, .95f, .7f));
            standRod = Prop(root, "StandRod", plain, new PartDef()
                .Cyl(.008f, 1f, 0, .5f, 0, Quaternion.identity, Palette.Steel, .95f, .75f)).transform;
            standTop = Prop(root, "StandTop", plain, new PartDef()
                .Tube(.03f, .022f, .008f, 0, 0, 0, Quaternion.identity, Palette.Steel, .95f, .75f)
                .Cyl(.012f, .01f, 0, -.006f, 0, Quaternion.identity, Palette.Tray, .85f, .5f)).transform;
            SetStand(BenchTop + .08f);

            // ── 뒷벽 공구판 + 공구 ──
            Prop(root, "Pegboard", Make(3f), new PartDef().Box(1.6f, .8f, .02f, .55f, 1.78f, 1.6f, Palette.Peg, 0, .3f));
            Color steel = Palette.Steel, red = Palette.PropsAlt, yellow = Palette.Accent, olive = Palette.Props;
            var tools = new PartDef();
            for (int i = 0; i < 5; i++)
            {
                float x = -.1f + i * .1f;
                tools.Cyl(.005f, .16f, x, 1.83f, 1.575f, Quaternion.identity, steel, .95f, .75f);
                tools.Cyl(.016f, .1f, x, 1.96f, 1.575f, Quaternion.identity, i % 2 == 0 ? red : yellow, 0, .55f);
            }
            tools.Box(.03f, .26f, .025f, .5f, 1.75f, 1.575f, Palette.Wainscot, 0, .45f).Box(.13f, .05f, .045f, .5f, 1.89f, 1.575f, steel, .95f, .6f);
            tools.Box(.024f, .3f, .01f, .66f, 1.76f, 1.58f, steel, .95f, .7f).Tube(.032f, .018f, .01f, .66f, 1.93f, 1.58f, AxZ, steel, .95f, .7f);
            tools.Box(.02f, .24f, .01f, .78f, 1.74f, 1.58f, steel, .95f, .7f).Tube(.026f, .015f, .01f, .78f, 1.875f, 1.58f, AxZ, steel, .95f, .7f);
            tools.Box(.018f, .2f, .016f, .9f, 1.75f, 1.578f, olive, .1f, .5f, false, Quaternion.Euler(0, 0, 8)).Box(.018f, .2f, .016f, .93f, 1.75f, 1.578f, olive, .1f, .5f, false, Quaternion.Euler(0, 0, -8));
            tools.Box(.3f, .02f, .012f, 1.12f, 1.98f, 1.582f, Palette.Tray, .6f, .5f).Box(.02f, .3f, .012f, .985f, 1.84f, 1.582f, Palette.Tray, .6f, .5f);
            for (int i = 0; i < 4; i++) tools.Cyl(.035f - i * .005f, .02f, 1.15f, 1.6f + i * .045f, 1.58f, AxZ, i % 2 == 0 ? red : olive, .1f, .5f);
            Prop(root, "Tools", plain, tools);

            // ── 선반 ──
            var shelf = new PartDef()
                .Box(1.1f, .03f, .24f, 2.1f, 1.55f, 1.55f, Palette.Wainscot, 0, .4f)
                .Box(1.1f, .03f, .24f, 2.1f, 2.0f, 1.55f, Palette.Wainscot, 0, .4f)
                .Box(1.1f, .03f, .24f, -2.55f, 1.55f, 1.55f, Palette.Wainscot, 0, .4f);
            Color[] jar = { Palette.PropsAlt, Palette.Props, Palette.Accent, Palette.Tray, Palette.Wainscot };
            for (int i = 0; i < 4; i++)
            {
                shelf.Cyl(.05f, .13f + (i % 3) * .03f, 1.7f + i * .2f, 1.63f + (i % 3) * .015f, 1.55f, Quaternion.identity, jar[i], .1f, .7f);
                shelf.Box(.16f, .1f + (i % 2) * .05f, .16f, 1.75f + i * .22f, 2.065f + (i % 2) * .025f, 1.55f, jar[(i + 2) % 5], 0, .4f);
            }
            shelf.Box(.26f, .14f, .1f, -2.55f, 1.635f, 1.55f, Palette.Props, .1f, .5f).Box(.08f, .03f, .004f, -2.6f, 1.66f, 1.498f, Color.Lerp(Palette.Accent, Color.white, .5f), 0, .6f);
            shelf.Cyl(.008f, .2f, -2.45f, 1.8f, 1.55f, Quaternion.identity, steel, .95f, .7f);
            Prop(root, "Shelves", wood, shelf);

            // ── 스탠드 조명 ──
            var lampRoot = new Vector3(.95f, BenchTop, .38f);
            Prop(root, "Lamp", plain, new PartDef()
                .Cyl(.08f, .025f, lampRoot.x, lampRoot.y + .0125f, lampRoot.z, Quaternion.identity, Palette.Tray, .7f, .55f)
                .Cyl(.011f, .5f, lampRoot.x, lampRoot.y + .27f, lampRoot.z, Quaternion.identity, Palette.Tray, .7f, .55f)
                .Ball(.02f, lampRoot.x, lampRoot.y + .52f, lampRoot.z, Palette.Steel, .9f, .6f)
                .Cyl(.011f, .4f, lampRoot.x - .15f, lampRoot.y + .64f, lampRoot.z - .12f, Quaternion.Euler(-35, 0, -45), Palette.Tray, .7f, .55f)
                .Cone(.09f, .035f, .11f, lampRoot.x - .3f, lampRoot.y + .72f, lampRoot.z - .25f, Quaternion.Euler(-12, 0, -18), Palette.Accent, .4f, .65f));
            var bulbPos = new Vector3(lampRoot.x - .31f, lampRoot.y + .685f, lampRoot.z - .255f);
            Prop(root, "Bulb", Make(emission: Palette.LampLight * 6f), new PartDef().Ball(.03f, bulbPos.x, bulbPos.y, bulbPos.z, Color.white, 0, .9f));

            Lights(root, bulbPos);
            Environment();
        }

        /// <summary>거치대 윗면 높이(월드 Y)를 정한다.</summary>
        public static void SetStand(float topY)
        {
            if (!standRod) return;
            float baseY = BenchTop + .02f;
            float h = Mathf.Max(.005f, topY - .01f - baseY);
            standRod.position = new Vector3(0, baseY, 0);
            standRod.localScale = new Vector3(1f, h, 1f);
            standTop.position = new Vector3(0, topY - .004f, 0);
        }

        static GameObject Prop(Transform parent, string name, Material m, PartDef def)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshGen.Combine(def.shapes);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static void Lights(Transform root, Vector3 bulbPos)
        {
            // 키 라이트: 왼쪽 앞 창으로 드는 늦은 오후 햇살
            var sun = new GameObject("KeyLight").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(38f, 52f, 0f);
            sun.color = Palette.Sun;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.15f;

            // 스탠드: 손 주변을 데우는 따뜻한 점광원
            var lamp = new GameObject("LampLight").AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.type = LightType.Point;
            lamp.transform.position = bulbPos + new Vector3(0, -.03f, 0);
            lamp.color = Palette.LampLight;
            lamp.intensity = 0.9f;
            lamp.range = 1.5f;
            lamp.shadows = LightShadows.Soft;
            lamp.shadowStrength = 0.6f;

            // 뒷벽 창: 차가운 하늘빛 보조광 (림 라이트 역할)
            var win = new GameObject("WindowFill").AddComponent<Light>();
            win.transform.SetParent(root, false);
            win.type = LightType.Point;
            win.transform.position = new Vector3(-1.4f, 1.8f, 1.2f);
            win.color = Palette.Sky;
            win.intensity = 1.6f;
            win.range = 3.2f;
        }

        static void Environment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Custom;
            // 그라디언트 환경광: 위는 차가운 하늘, 옆은 중간, 아래는 어두운 나무색
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(Palette.AmbientEquator.linear * 0.9f);
            sh.AddDirectionalLight(Vector3.up, Palette.AmbientSky.linear, 0.9f);
            sh.AddDirectionalLight(Vector3.down, Palette.AmbientGround.linear, 0.4f);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = false;

            // 금속 반사용 공방 큐브맵: 벽·바닥 + 창 + 스탠드 + 천장 조명
            const int size = 64;
            var cube = new Cubemap(size, TextureFormat.RGBAHalf, true);
            var face = new Color[size * size];
            Vector3 winDir = new Vector3(-0.55f, 0.35f, 0.75f).normalized;
            Vector3 lampDir = new Vector3(0.45f, 0.75f, 0.35f).normalized;
            Vector3 topDir = new Vector3(0.1f, 1f, -0.2f).normalized;
            for (int f = 0; f < 6; f++)
            {
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                        Vector3 d = f switch
                        {
                            0 => new Vector3(1, -v, -u),
                            1 => new Vector3(-1, -v, u),
                            2 => new Vector3(u, 1, v),
                            3 => new Vector3(u, -1, -v),
                            4 => new Vector3(u, -v, 1),
                            _ => new Vector3(-u, -v, -1),
                        };
                        d.Normalize();
                        Color c = d.y < -0.05f ? Color.Lerp(Palette.Wainscot.linear * 0.5f, Palette.Floor.linear * 0.4f, Mathf.Clamp01(-d.y * 2f))
                                               : Color.Lerp(Palette.Wall.linear * 0.6f, Palette.AmbientSky.linear * 0.7f, Mathf.Clamp01(d.y * 1.4f));
                        c += Palette.Sky.linear * 1.3f * Mathf.SmoothStep(0.86f, 0.95f, Vector3.Dot(d, winDir));
                        c += Palette.LampLight.linear * 1.4f * Mathf.SmoothStep(0.9f, 0.97f, Vector3.Dot(d, lampDir));
                        c += Palette.Sun.linear * 0.45f * Mathf.SmoothStep(0.85f, 0.95f, Vector3.Dot(d, topDir));
                        c.a = 1f;
                        face[y * size + x] = c;
                    }
                cube.SetPixels(face, (CubemapFace)f);
            }
            cube.Apply(true);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 1f;
        }

        public static Camera MakeCamera(Transform parent)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Shadow;
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 30f;
            cam.allowMSAA = true;
            cam.allowHDR = true;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;   // MSAA 4x 사용 (파이프라인 에셋)
            data.renderShadows = true;

            var vol = new GameObject("PostFx").AddComponent<Volume>();
            vol.transform.SetParent(parent, false);
            vol.isGlobal = true;
            vol.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.25f);
            bloom.threshold.Override(1.25f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(Palette.LampLight);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.25f);
            vig.smoothness.Override(0.45f);
            vig.color.Override(Palette.Shadow * 0.3f);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.2f);
            ca.contrast.Override(8f);
            ca.saturation.Override(8f);
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(10f);
            Dof = profile.Add<DepthOfField>(true);
            Dof.mode.Override(DepthOfFieldMode.Gaussian);
            Dof.gaussianStart.Override(1.6f);
            Dof.gaussianEnd.Override(3.2f);
            Dof.gaussianMaxRadius.Override(1.0f);
            Dof.highQualitySampling.Override(true);
            vol.sharedProfile = profile;
            return cam;
        }

        /// <summary>카메라 줌에 맞춰 초점 거리를 옮긴다: 작업물은 선명, 뒷벽은 흐리게.</summary>
        public static void FocusAt(float dist)
        {
            if (Dof == null) return;
            Dof.gaussianStart.Override(dist + 0.35f);
            Dof.gaussianEnd.Override(dist + 1.8f);
        }
    }
}
