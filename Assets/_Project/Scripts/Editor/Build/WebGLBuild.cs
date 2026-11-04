using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// WebGL の書き出し。tools/webgl/build.ps1 が、プロジェクトを写した先からバッチモードで呼ぶ。
    /// 書き出しの設定を入れて書き出す。写した先でだけ、3D の物に貼る絵を縮める。
    /// 素材集の Resources から使わない一覧を外すのは build.ps1 の側
    /// </summary>
    public static class WebGLBuild
    {
        /// <summary>
        /// 写した先にだけ置く目印（build.ps1 が置く）。絵の取り込み設定を書き換える処理は、これが無いプロジェクトでは動かない。
        /// 元のプロジェクトの素材集の .meta は Git の管理外で、書き換えると戻せない
        /// </summary>
        private const string WorkspaceMarker = "WebGLWorkspace.txt";

        /// <summary>
        /// WebGL で 3D の物に貼る絵（スプライトでない絵）に掛ける大きさの上限。書き出しの大半が 2048 の模様と空の絵で、
        /// 置き場所の上限（zip で 100 MB）に収めるため。スプライトと UI は画面で大きく見えるので元のまま
        /// </summary>
        private const int SurfaceTextureCap = 1024;

        /// <summary>
        /// Assets/WebGLTemplates の中のひな形。既定のひな形は Application.persistentDataPath を
        /// ブラウザ（IndexedDB）へ書き戻さず、ページを開き直すと保存が消える
        /// </summary>
        private const string Template = "PROJECT:MemorialFloor";

        /// <summary>PC のブラウザで描く大きさ。UI の基準（1920×1080）と同じ 16:9。スマホでは画面いっぱいに広げる</summary>
        private const int Width = 1280;

        private const int Height = 720;

        /// <summary>書き出し先を渡す引数。無ければプロジェクトの Builds/WebGL</summary>
        private const string OutputArgument = "-webglOutput";

        public static void Build()
        {
            string output = Argument(OutputArgument) ?? "Builds/WebGL";

            PlayerSettings.WebGL.template = Template;
            PlayerSettings.defaultWebScreenWidth = Width;
            PlayerSettings.defaultWebScreenHeight = Height;

            // 置き場所のサーバーが Content-Encoding を付けなくても読めるよう、展開の処理を同梱する
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // 根源3は static batching で描く回数を減らす前提で組んである。切れていると描く回数が倍ほどになる
            PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.WebGL, true);

            if (File.Exists(WorkspaceMarker)) CapSurfaceTextures();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log("WebGLBuild: " + summary.result + " " + (summary.totalSize / (1024f * 1024f)).ToString("F1") + " MB " +
                      summary.totalTime.TotalMinutes.ToString("F1") + " 分 → " + output);

            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>スプライトでない絵の WebGL の上限を SurfaceTextureCap にし、読み込み直す。既に小さい物には触れない</summary>
        private static void CapSurfaceTextures()
        {
            List<TextureImporter> capped = new List<TextureImporter>();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture", new[] { "Assets" }))
            {
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;

                TextureImporterPlatformSettings webgl = importer.GetPlatformTextureSettings("WebGL");
                int limit = webgl.overridden ? webgl.maxTextureSize : importer.maxTextureSize;
                if (limit <= SurfaceTextureCap) continue;

                webgl.overridden = true;
                webgl.maxTextureSize = SurfaceTextureCap;
                importer.SetPlatformTextureSettings(webgl);
                capped.Add(importer);
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (TextureImporter importer in capped) importer.SaveAndReimport();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log("WebGLBuild: スプライトでない絵 " + capped.Count + " 枚の上限を " + SurfaceTextureCap + " にした");
        }

        /// <summary>バッチモードに渡された引数の値。無ければ null</summary>
        internal static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, name);

            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }
    }
}
