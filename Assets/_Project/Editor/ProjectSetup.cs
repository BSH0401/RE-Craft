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
        public const string Version = "0.2.0";

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
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] RE-Craft setup complete");
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
