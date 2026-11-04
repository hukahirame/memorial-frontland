using System.Collections.Generic;
using System.IO;
using System.Text;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 場所のシーンに置いてある地形と、地図から組んだ地形を、遊ぶときのカメラで撮り比べる。
    /// シーンはプレビューで開いて保存しない。編集中のシーンには触れない。
    /// 撮った絵と、違う画素の数は Temp/FieldShots/場所の名前/ に書く
    /// </summary>
    public static class FieldPreview
    {
        private const string OutputFolder = "Temp/FieldShots";

        private const int Width = 1280;

        private const int Height = 720;

        /// <summary>場の左端から右端まで、何か所に立って撮るか</summary>
        private const int Stops = 5;

        /// <summary>場の端から、端の立つ位置までの間</summary>
        private const float EdgeMargin = 2.5f;

        /// <summary>場全体を見渡す 1 枚の、場の真ん中からの高さ</summary>
        private const float OverviewHeight = 15f;

        /// <summary>場全体を見渡す 1 枚の、場の真ん中から手前への引き</summary>
        private const float OverviewBack = 27.5f;

        /// <summary>常駐シーンのカメラの背景色。プレビューでは空を描かないので、これで塗る</summary>
        private static readonly Color Background = new Color(0.19215687f, 0.3019608f, 0.4745098f);

        [MenuItem("Tools/Field/Compare Maps With Scenes")]
        public static void CompareAll()
        {
            FieldPalette palette = FieldMaps.LoadPalette();

            foreach (string mapPath in FieldMaps.All())
            {
                string folder = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(mapPath));
                Debug.Log(Compare(FieldMaps.SceneOf(mapPath), FieldMaps.Load(mapPath), palette, folder));
            }
        }

        private static string Compare(string scenePath, FieldMap map, FieldPalette palette, string folder)
        {
            Directory.CreateDirectory(folder);

            Scene scene = EditorSceneManager.OpenPreviewScene(scenePath);
            RenderTexture target = new RenderTexture(Width, Height, 24);

            try
            {
                Camera camera = Eye(scene);
                List<Pose> poses = Poses(scene);

                List<Color32[]> before = new List<Color32[]>();
                for (int i = 0; i < poses.Count; i++)
                {
                    before.Add(Shoot(camera, target, poses[i], Path.Combine(folder, "scene_" + i + ".png")));
                }

                List<GameObject> placed = FieldMaps.Cubes(scene);
                foreach (GameObject cube in placed) Hide(cube);

                GameObject root = new GameObject("FieldTerrain");
                SceneManager.MoveGameObjectToScene(root, scene);
                List<GameObject> built = FieldBuilder.Build(map, palette, root.transform);

                StringBuilder report = new StringBuilder();
                report.Append(scene.name).Append(": シーンの Cube ").Append(placed.Count)
                      .Append(" 個を隠し、地図から ").Append(built.Count).Append(" 個を組んだ\n");

                for (int i = 0; i < poses.Count; i++)
                {
                    Color32[] after = Shoot(camera, target, poses[i], Path.Combine(folder, "map_" + i + ".png"));
                    int differ = Differ(before[i], after, Path.Combine(folder, "diff_" + i + ".png"));

                    report.Append(i).Append(' ').Append(poses[i].position.ToString("F2"))
                          .Append(" 違う画素 ").Append(differ).Append(" / ").Append(after.Length).Append('\n');
                }

                File.WriteAllText(Path.Combine(folder, "report.txt"), report.ToString(), new UTF8Encoding(false));
                return report.ToString();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(target);
            }
        }

        /// <summary>撮るためのカメラ。プレビューのシーンだけを写す</summary>
        private static Camera Eye(Scene scene)
        {
            GameObject eye = new GameObject("FieldPreviewCamera");
            SceneManager.MoveGameObjectToScene(eye, scene);

            Camera camera = eye.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.fieldOfView = PlayerView.FieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;

            return camera;
        }

        /// <summary>入口の高さと奥行きに立ち、場の左端から右端まで見る絵と、場全体を見渡す 1 枚。場所の光を撮る道具も使う</summary>
        internal static List<Pose> Poses(Scene scene)
        {
            Area area = AreaOf(scene);
            if (area == null) throw new System.InvalidOperationException(scene.name + " に Area が無い");

            FieldBounds bounds = Exposition.ToBounds(area.Exposition);
            Vector3 stand = area.PointFor(Arrival.FromPrevious);
            Quaternion look = Quaternion.Euler(PlayerView.Pitch, 0f, 0f);

            List<Pose> poses = new List<Pose>();

            for (int i = 0; i < Stops; i++)
            {
                float x = Mathf.Lerp(bounds.MinX + EdgeMargin, bounds.MaxX - EdgeMargin, i / (float)(Stops - 1));
                poses.Add(new Pose(new Vector3(x, stand.y, stand.z) + PlayerView.Offset, look));
            }

            Vector3 center = new Vector3(bounds.CenterX, 0f, bounds.CenterZ);
            Vector3 overview = center + new Vector3(0f, OverviewHeight, -OverviewBack);
            poses.Add(new Pose(overview, Quaternion.LookRotation(center - overview)));

            return poses;
        }

        private static Area AreaOf(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Area area = root.GetComponent<Area>();
                if (area != null) return area;
            }

            return null;
        }

        /// <summary>描画だけを切る。GameObject を止めると、子に付いた別の物まで消える</summary>
        private static void Hide(GameObject cube)
        {
            cube.GetComponent<MeshRenderer>().enabled = false;
        }

        private static Color32[] Shoot(Camera camera, RenderTexture target, Pose pose, string path)
        {
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);

            return LookDevShots.Capture(camera, target, path);
        }

        /// <summary>違う画素の数。1 つでもあれば、違う所を赤く、同じ所を暗くした絵を書く</summary>
        private static int Differ(Color32[] before, Color32[] after, string path)
        {
            Color32[] marks = new Color32[before.Length];
            int count = 0;

            for (int i = 0; i < before.Length; i++)
            {
                Color32 a = before[i];
                Color32 b = after[i];

                if (a.r == b.r && a.g == b.g && a.b == b.b)
                {
                    marks[i] = new Color32((byte)(a.r / 3), (byte)(a.g / 3), (byte)(a.b / 3), 255);
                    continue;
                }

                marks[i] = new Color32(255, 0, 0, 255);
                count++;
            }

            if (count == 0)
            {
                if (File.Exists(path)) File.Delete(path);
                return 0;
            }

            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.SetPixels32(marks);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);

            return count;
        }
    }
}
