using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// Android の書き出し。tools/webgl/build.ps1 -Target Android が、プロジェクトを写した先からバッチモードで呼ぶ。
    /// 書き出しの設定を入れて、手で入れる APK を作る。設定は写した先にだけ入り、元のプロジェクトの設定は変えない
    /// </summary>
    public static class AndroidBuild
    {
        /// <summary>書き出し先を渡す引数。無ければプロジェクトの Builds/Android/MemorialFrontLand.apk</summary>
        private const string OutputArgument = "-androidOutput";

        public static void Build()
        {
            string output = WebGLBuild.Argument(OutputArgument) ?? "Builds/Android/MemorialFrontLand.apk";

            // 最近の機種（Pixel 7 以降など）は 32 ビットのアプリを入れられない。64 ビットは IL2CPP でしか作れない
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // ストアに出すなら AAB と署名の鍵が要る。鍵が無ければ Unity のデバッグ用の鍵で署名される
            EditorUserBuildSettings.buildAppBundle = false;

            // 根源3は static batching で描く回数を減らす前提で組んである
            PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.Android, true);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log("AndroidBuild: " + summary.result + " " + (summary.totalSize / (1024f * 1024f)).ToString("F1") + " MB " +
                      summary.totalTime.TotalMinutes.ToString("F1") + " 分 → " + output);

            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
