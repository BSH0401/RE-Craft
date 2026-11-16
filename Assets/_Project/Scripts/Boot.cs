using System;
using UnityEngine;

namespace ReCraft
{
    /// <summary>씬에 아무것도 없어도 게임 전체를 코드로 조립하는 진입점.</summary>
    public static class Boot
    {
        public static string[] Args = Array.Empty<string>();

        public static bool Flag(string name) => Array.IndexOf(Args, name) >= 0;

        public static string Arg(string name, string fallback = null)
        {
            int i = Array.IndexOf(Args, name);
            return i >= 0 && i + 1 < Args.Length ? Args[i + 1] : fallback;
        }

        public static float ArgF(string name, float fallback)
        {
            return float.TryParse(Arg(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            Args = Environment.GetCommandLineArgs();
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;
            Application.runInBackground = true;

            G.noSave = Flag("-nosave");
            G.Load();
            if (Arg("-gold") != null) G.data.gold = (int)ArgF("-gold", 0);
            if (Arg("-stars") != null) G.data.stars = (int)ArgF("-stars", 0);
            if (Arg("-toollv") != null)
                for (int i = 1; i < 5; i++) G.data.toolLv[i] = Mathf.Clamp((int)ArgF("-toollv", 1), 1, 3);

            var root = new GameObject("Game");
            UnityEngine.Object.DontDestroyOnLoad(root);
            Sfx.Init(root);
            Fx.Init(root);
            Room.Build(root.transform);
            var cam = Room.MakeCamera(root.transform);
            var wbGo = new GameObject("Workbench");
            wbGo.transform.SetParent(root.transform, false);
            var wb = wbGo.AddComponent<Workbench>();
            wb.cam = cam;
            var hudGo = new GameObject("HUDRoot");
            hudGo.transform.SetParent(root.transform, false);
            hudGo.AddComponent<HUD>();
            if (Flag("-notutorial")) G.data.tutorialStep = Tutorial.Done;
            if (Arg("-tutorial") != null) G.data.tutorialStep = (int)ArgF("-tutorial", 0);
            else if ((Flag("-autoplay") || Flag("-showcase")) && !Flag("-withtutorial")) G.data.tutorialStep = Tutorial.Done;
            hudGo.AddComponent<Tutorial>();

            Time.timeScale = ArgF("-timescale", 1f);
            if (Flag("-autoplay") || Flag("-showcase")) root.AddComponent<DevBot>();
            else HUD.I.ShowBoard();
        }
    }
}
