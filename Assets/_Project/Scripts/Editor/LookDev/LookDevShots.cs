using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// シーンに置いたカメラで、そのシーンだけを撮る。シーンはプレビューで開き、保存しない。
    /// 光・空・霧はそのシーンのものを使う。絵は Temp/LookDevShots/シーンの名前/カメラの名前.png に出る
    /// </summary>
    public static class LookDevShots
    {
        private const string OutputFolder = "Temp/LookDevShots";

        private const int Width = 1280;

        private const int Height = 720;

        /// <summary>シーンにあるカメラすべてで撮る。止めてあるカメラも撮る。書いた絵の道を返す</summary>
        public static List<string> Shoot(string scenePath)
        {
            string folder = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(scenePath));
            Directory.CreateDirectory(folder);

            Scene scene = EditorSceneManager.OpenPreviewScene(scenePath);
            RenderTexture target = new RenderTexture(Width, Height, 24);
            List<string> written = new List<string>();

            // 光・空・霧は有効なシーンのものが使われる。プレビューのシーンのものに差し替える
            bool overridden = Unsupported.SetOverrideLightingSettings(scene);

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        camera.scene = scene;
                        string path = Path.Combine(folder, camera.name + ".png");
                        Capture(camera, target, path);
                        written.Add(path.Replace('\\', '/'));
                    }
                }
            }
            finally
            {
                if (overridden) Unsupported.RestoreOverrideLightingSettings();
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(target);
            }

            return written;
        }

        /// <summary>カメラの今の位置で撮り、絵に書く。比べるために画素も返す</summary>
        public static Color32[] Capture(Camera camera, RenderTexture target, string path)
        {
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = previousTarget;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;

            File.WriteAllBytes(path, image.EncodeToPNG());
            Color32[] pixels = image.GetPixels32();
            Object.DestroyImmediate(image);

            return pixels;
        }
    }
}
