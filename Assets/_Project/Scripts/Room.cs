using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ReCraft
{
    /// <summary>공방 환경(바닥·벽·작업대·조명·후처리)을 코드로 만든다.</summary>
    public static class Room
    {
        public const float BenchTop = 0.9f;
        static Material plain, wood;

        static Color C(float r, float g, float b) => new(r, g, b, 1f);

        public static void Build(Transform parent)
        {
            plain = Mat.Surface();
            wood = Mat.Surface();
            wood.SetFloat("_Wood", 1f);
            wood.SetFloat("_NoiseScale", 30f);

            var root = new GameObject("Room").transform;
            root.SetParent(parent, false);

            // 바닥 · 벽
            Prop(root, "Floor", wood, new PartDef().Box(7, .1f, 7, 0, -.05f, 0, C(.2f, .13f, .08f), 0, .35f));
            Prop(root, "Walls", plain, new PartDef()
                .Box(7, 3.4f, .1f, 0, 1.7f, 1.65f, C(.3f, .38f, .37f), 0, .15f)
                .Box(.1f, 3.4f, 7, -3.2f, 1.7f, 0, C(.28f, .35f, .34f), 0, .15f)
                .Box(.1f, 3.4f, 7, 3.2f, 1.7f, 0, C(.28f, .35f, .34f), 0, .15f)
                .Box(7, .12f, .04f, 0, .06f, 1.58f, C(.15f, .1f, .07f), 0, .3f));

            // 작업대
            Prop(root, "Bench", wood, new PartDef()
                .Box(2.3f, .06f, 1.0f, 0, BenchTop - .03f, .02f, C(.62f, .43f, .25f), 0, .45f)
                .Box(.09f, .84f, .09f, -1.05f, .42f, -.38f, C(.3f, .2f, .12f), 0, .3f)
                .Box(.09f, .84f, .09f, 1.05f, .42f, -.38f, C(.3f, .2f, .12f), 0, .3f)
                .Box(.09f, .84f, .09f, -1.05f, .42f, .42f, C(.3f, .2f, .12f), 0, .3f)
                .Box(.09f, .84f, .09f, 1.05f, .42f, .42f, C(.3f, .2f, .12f), 0, .3f)
                .Box(2.1f, .08f, .04f, 0, .76f, -.4f, C(.3f, .2f, .12f), 0, .3f));
            Prop(root, "Mats", plain, new PartDef()
                .Box(.52f, .004f, .46f, 0, BenchTop + .002f, 0, C(.1f, .3f, .22f), 0, .3f)
                .Box(.56f, .004f, .6f, -.6f, BenchTop + .002f, -.02f, C(.2f, .2f, .22f), 0, .25f)
                .Box(.56f, .004f, .6f, .6f, BenchTop + .002f, -.02f, C(.2f, .2f, .22f), 0, .25f)
                .Box(.44f, .01f, .1f, -.45f, BenchTop + .004f, .36f, C(.55f, .1f, .1f), .3f, .5f));

            // 뒷벽 공구판 + 선반
            var peg = new PartDef().Box(2.0f, 1.0f, .03f, 0, 1.85f, 1.58f, C(.55f, .42f, .28f), 0, .3f);
            Color steel = C(.7f, .72f, .75f), red = C(.7f, .15f, .1f), yellow = C(.85f, .65f, .1f), blue = C(.15f, .3f, .6f);
            for (int i = 0; i < 5; i++)
            {
                float x = -.8f + i * .16f;
                peg.Cyl(.008f, .2f, x, 1.95f, 1.55f, Quaternion.identity, steel, .9f, .7f);
                peg.Cyl(.018f, .12f, x, 2.11f, 1.55f, Quaternion.identity, i % 2 == 0 ? red : yellow, 0, .5f);
            }
            peg.Box(.05f, .3f, .03f, .1f, 1.95f, 1.55f, C(.5f, .32f, .15f), 0, .4f).Box(.16f, .07f, .06f, .1f, 2.12f, 1.55f, steel, .9f, .6f);
            peg.Box(.03f, .34f, .012f, .32f, 1.97f, 1.555f, steel, .95f, .7f).Tube(.035f, .02f, .012f, .32f, 2.16f, 1.555f, Quaternion.Euler(90, 0, 0), steel, .95f, .7f);
            peg.Box(.03f, .28f, .012f, .5f, 1.95f, 1.555f, steel, .95f, .7f).Tube(.03f, .017f, .012f, .5f, 2.11f, 1.555f, Quaternion.Euler(90, 0, 0), steel, .95f, .7f);
            peg.Box(.3f, .02f, .01f, .8f, 2.1f, 1.555f, blue, .2f, .5f).Box(.02f, .3f, .01f, .66f, 1.96f, 1.555f, blue, .2f, .5f);
            Prop(root, "Pegboard", plain, peg);

            var shelf = new PartDef().Box(1.3f, .03f, .24f, -2.0f, 1.55f, 1.48f, C(.4f, .27f, .15f), 0, .4f)
                .Box(1.3f, .03f, .24f, 2.0f, 1.55f, 1.48f, C(.4f, .27f, .15f), 0, .4f);
            Color[] jar = { C(.75f, .2f, .15f), C(.2f, .5f, .6f), C(.8f, .65f, .2f), C(.3f, .55f, .3f), C(.6f, .6f, .62f) };
            for (int i = 0; i < 5; i++)
            {
                shelf.Cyl(.05f, .13f + (i % 3) * .03f, -2.5f + i * .25f, 1.63f + (i % 3) * .015f, 1.48f, Quaternion.identity, jar[i], .1f, .7f);
                shelf.Box(.16f, .1f + (i % 2) * .05f, .16f, 1.5f + i * .25f, 1.615f + (i % 2) * .025f, 1.48f, jar[(i + 2) % 5], 0, .4f);
            }
            Prop(root, "Shelves", plain, shelf);

            // 스탠드
            Prop(root, "Lamp", plain, new PartDef()
                .Cyl(.08f, .02f, .95f, BenchTop + .01f, .38f, Quaternion.identity, C(.12f, .12f, .13f), .6f, .6f)
                .Cyl(.012f, .55f, .95f, BenchTop + .28f, .38f, Quaternion.identity, C(.12f, .12f, .13f), .6f, .6f)
                .Cyl(.012f, .4f, .8f, BenchTop + .62f, .3f, Quaternion.Euler(0, 30, 60), C(.12f, .12f, .13f), .6f, .6f)
                .Cone(.09f, .04f, .1f, .63f, BenchTop + .7f, .2f, Quaternion.Euler(0, 30, 20), C(.8f, .55f, .1f), .5f, .7f));

            Lights(root);
            Environment();
        }

        static void Prop(Transform parent, string name, Material m, PartDef def)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshGen.Combine(def.shapes);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
        }

        static void Lights(Transform root)
        {
            var sun = new GameObject("KeyLight").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.intensity = 0.95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;

            var lamp = new GameObject("LampLight").AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.type = LightType.Point;
            lamp.transform.position = new Vector3(0.55f, BenchTop + 0.62f, 0.12f);
            lamp.color = new Color(1f, 0.78f, 0.5f);
            lamp.intensity = 0.35f;
            lamp.range = 2.2f;

            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(root, false);
            fill.type = LightType.Point;
            fill.transform.position = new Vector3(-0.9f, 1.7f, -0.9f);
            fill.color = new Color(0.6f, 0.75f, 1f);
            fill.intensity = 0.7f;
            fill.range = 3.5f;
        }

        static void Environment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Custom;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(0.16f, 0.17f, 0.2f));
            sh.AddDirectionalLight(Vector3.up, new Color(0.32f, 0.3f, 0.27f), 0.6f);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = false;

            // 금속 반사용 스튜디오 큐브맵: 위쪽 소프트박스 + 따뜻한 측광
            const int size = 64;
            var cube = new Cubemap(size, TextureFormat.RGBAHalf, true);
            var face = new Color[size * size];
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
                        float up = Mathf.Clamp01(d.y * 0.5f + 0.5f);
                        Color c = Color.Lerp(new Color(0.07f, 0.055f, 0.045f), new Color(0.3f, 0.32f, 0.34f), up);
                        float box = Mathf.SmoothStep(0.75f, 0.92f, Vector3.Dot(d, new Vector3(0.25f, 0.9f, -0.35f).normalized));
                        c += new Color(1.1f, 1.05f, 0.95f) * box;
                        float side = Mathf.SmoothStep(0.8f, 0.95f, Vector3.Dot(d, new Vector3(0.9f, 0.25f, 0.3f).normalized));
                        c += new Color(0.6f, 0.4f, 0.22f) * side;
                        float win = Mathf.SmoothStep(0.85f, 0.97f, Vector3.Dot(d, new Vector3(-0.8f, 0.35f, -0.5f).normalized));
                        c += new Color(0.3f, 0.42f, 0.6f) * win;
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
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.04f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 30f;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var vol = new GameObject("PostFx").AddComponent<Volume>();
            vol.transform.SetParent(parent, false);
            vol.isGlobal = true;
            vol.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.0f);
            bloom.scatter.Override(0.6f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f);
            vig.smoothness.Override(0.5f);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.1f);
            ca.contrast.Override(8f);
            ca.saturation.Override(10f);
            vol.sharedProfile = profile;
            return cam;
        }
    }
}
