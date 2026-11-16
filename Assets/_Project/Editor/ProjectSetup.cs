using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ReCraft.EditorTools
{
    /// <summary>
    /// 원클릭(또는 batchmode) 셋업: 빈 메인 씬 · 빌드 설정 · 플레이어 설정.
    /// 게임 내용은 전부 런타임 코드(Boot)가 만든다. 메뉴 RE-Craft → Setup Project.
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Workshop.unity";
        const string BuildPath = "Builds/Windows/RECraft.exe";
        public const string Version = "0.7.0";

        [MenuItem("RE-Craft/Setup Project")]
        public static void Setup()
        {
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            // 템플릿 샘플 정리
            foreach (string path in new[] { "Assets/Scenes", "Assets/TutorialInfo", "Assets/Readme.asset" })
                if (AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);

            PlayerSettings.companyName = "BSH0401";
            PlayerSettings.productName = "RE-CRAFT";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.BSH0401.ReCraft");
            // D3D12는 종료 시 크래시가 있어 D3D11로 고정
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            ConfigureGraphics();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] RE-Craft setup complete");
        }

        /// <summary>책상 위 작은 물건 기준으로 그림자 · AA · SSAO를 맞춘다.</summary>
        static void ConfigureGraphics()
        {
            const string pipePath = "Assets/Settings/PC_RPAsset.asset";
            var pipe = AssetDatabase.LoadMainAssetAtPath(pipePath);
            if (pipe != null)
            {
                var so = new SerializedObject(pipe);
                void F(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; else Debug.LogWarning("[Setup] missing " + n); }
                void I(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; else Debug.LogWarning("[Setup] missing " + n); }
                void B(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; else Debug.LogWarning("[Setup] missing " + n); }
                F("m_ShadowDistance", 4f);
                I("m_ShadowCascadeCount", 2);
                F("m_Cascade2Split", 0.4f);
                I("m_MainLightShadowmapResolution", 4096);
                I("m_AdditionalLightsShadowmapResolution", 2048);
                B("m_AdditionalLightShadowsSupported", true);
                B("m_SupportsHDR", true);
                I("m_MSAA", 4);
                F("m_ShadowDepthBias", 0.6f);
                F("m_ShadowNormalBias", 0.4f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Settings/PC_Renderer.asset"))
            {
                if (obj == null || obj.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                var so = new SerializedObject(obj);
                so.FindProperty("m_Settings.Intensity").floatValue = 1.6f;
                so.FindProperty("m_Settings.Radius").floatValue = 0.035f;
                so.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0.35f;
                so.FindProperty("m_Settings.Falloff").floatValue = 20f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [MenuItem("RE-Craft/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath)!);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[Build] {BuildPath} {report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        /// <summary>batchmode 진입점: 셋업 후 빌드.</summary>
        public static void SetupAndBuild()
        {
            Setup();
            BuildWindows();
        }
    }
}
