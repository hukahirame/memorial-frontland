using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 場所のシーンの光と空気。空・環境光・霧・日の光と、その場所だけに効く後処理の範囲を、場所のシーンに書き込む。
    /// 後処理の層は常駐シーンのカメラにあり、Post Processing のレイヤーの範囲を見る。場所に着くとその場所のシーンが
    /// 有効になるので、空・環境光・霧も場所ごとに切り替わる。
    /// Android（中の画質で 60fps）でも重くならないよう、物の際を暗くする効果（AO）は使わず、にじみは軽い作りにする
    /// </summary>
    public static class PlaceLooks
    {
        private const string ArtFolder = "Assets/_Project/Art/Places";

        private const string ShotFolder = "Temp/PlaceShots";

        private const string MainSiteScene = "Assets/_Project/Scenes/MainSite.unity";

        private const string Root1Scene = "Assets/_Project/Scenes/Root1.unity";

        private const string Root2Scene = "Assets/_Project/Scenes/Root2.unity";

        /// <summary>撮り比べる場所。並びは行き来の順（拠点⇔根源1⇔根源2⇔根源3）</summary>
        private static readonly string[] Places = { MainSiteScene, Root1Scene, Root2Scene, Root3Builder.ScenePath };

        /// <summary>後処理の範囲を置くレイヤー。常駐シーンのカメラはこのレイヤーだけを見る</summary>
        private const string PostLayer = "Post Processing";

        /// <summary>撮るカメラに足す後処理の層が使う、シェーダーとテクスチャの一覧</summary>
        private const string PostResources = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";

        /// <summary>常駐シーンのカメラにも全体に効く範囲があり、その優先度は 0。場所の範囲はそれより上に置き、場所の設定を勝たせる</summary>
        private const float PlacePriority = 1f;

        private const int Width = 1280;

        private const int Height = 720;

        /// <summary>場所 1 つの光と空気。空・環境光・霧・日の光と、後処理の色の調子</summary>
        private sealed class Look
        {
            /// <summary>空と後処理の素材の名前の頭</summary>
            public string Name;

            public Color SkyTint;
            public Color SkyGround;
            public float SkyThickness;
            public float SkyExposure;

            /// <summary>環境光。空・地平・地面の 3 色</summary>
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;

            public Color Fog;
            public float FogDensity;

            public Vector3 SunAngles;
            public Color SunColor;
            public float SunIntensity;
            public float SunShadow;

            public float Exposure;
            public float Temperature;
            public float Tint;
            public float Saturation;
            public float Contrast;
            public float Bloom;
            public float Vignette;
        }

        /// <summary>
        /// 拠点。日が傾いた夕方の、暖かく霞んだ光。日は右の後ろから差し、物の左奥へ影が伸びる。
        /// 空の素材が抜けていて環境光がほぼ黒だったので、空を作り、環境光は空・地平・地面の 3 色で決める
        /// </summary>
        private static readonly Look MainSiteLook = new Look
        {
            Name = "MainSite",
            SkyTint = new Color(0.62f, 0.56f, 0.5f), SkyGround = new Color(0.36f, 0.31f, 0.26f), SkyThickness = 1.5f, SkyExposure = 1.15f,
            AmbientSky = new Color(0.45f, 0.5f, 0.58f), AmbientEquator = new Color(0.42f, 0.37f, 0.31f), AmbientGround = new Color(0.2f, 0.17f, 0.13f),
            Fog = new Color(0.64f, 0.58f, 0.5f), FogDensity = 0.018f,
            SunAngles = new Vector3(42f, -35f, 0f), SunColor = new Color(1f, 0.82f, 0.6f), SunIntensity = 1.5f, SunShadow = 0.85f,
            Exposure = 0.25f, Temperature = 10f, Tint = 0f, Saturation = 5f, Contrast = 14f, Bloom = 0.7f, Vignette = 0.3f,
        };

        /// <summary>根源1（はじまり）。森の朝の、少し緑がかった霞。日は左の後ろから白っぽく差す</summary>
        private static readonly Look Root1Look = new Look
        {
            Name = "Root1",
            SkyTint = new Color(0.56f, 0.63f, 0.62f), SkyGround = new Color(0.32f, 0.33f, 0.27f), SkyThickness = 1f, SkyExposure = 1.15f,
            AmbientSky = new Color(0.5f, 0.58f, 0.6f), AmbientEquator = new Color(0.44f, 0.47f, 0.38f), AmbientGround = new Color(0.2f, 0.19f, 0.14f),
            Fog = new Color(0.62f, 0.68f, 0.64f), FogDensity = 0.024f,
            SunAngles = new Vector3(40f, 25f, 0f), SunColor = new Color(1f, 0.93f, 0.78f), SunIntensity = 1.45f, SunShadow = 0.8f,
            Exposure = 0.3f, Temperature = -4f, Tint = 4f, Saturation = 8f, Contrast = 12f, Bloom = 0.6f, Vignette = 0.3f,
        };

        /// <summary>
        /// 根源2（水）。湿った、青緑の空気。日は高く、冷たい。水面の照り返しだけ少しにじませる。
        /// 空の大気を厚くすると地平が夕焼けの色になるので、薄くする
        /// </summary>
        private static readonly Look Root2Look = new Look
        {
            Name = "Root2",
            SkyTint = new Color(0.5f, 0.6f, 0.64f), SkyGround = new Color(0.25f, 0.32f, 0.34f), SkyThickness = 0.9f, SkyExposure = 1.1f,
            AmbientSky = new Color(0.48f, 0.58f, 0.66f), AmbientEquator = new Color(0.4f, 0.48f, 0.52f), AmbientGround = new Color(0.2f, 0.23f, 0.25f),
            Fog = new Color(0.56f, 0.66f, 0.7f), FogDensity = 0.03f,
            SunAngles = new Vector3(50f, -15f, 0f), SunColor = new Color(0.88f, 0.94f, 1f), SunIntensity = 1.3f, SunShadow = 0.7f,
            Exposure = 0.35f, Temperature = -12f, Tint = -3f, Saturation = 0f, Contrast = 14f, Bloom = 0.8f, Vignette = 0.35f,
        };

        [MenuItem("Tools/Places/Apply MainSite Look")]
        public static void ApplyMainSite()
        {
            Apply(MainSiteScene, MainSiteLook);

            foreach (string path in Shoot(MainSiteScene))
            {
                Debug.Log("撮った: " + path);
            }
        }

        /// <summary>根源1・2 の光と空気。入れる前は空気が無く、環境光は空任せ、霧も後処理も無かった。入れたあとで 4 つの場所を撮り比べる</summary>
        [MenuItem("Tools/Places/Apply Root Looks")]
        public static void ApplyRoots()
        {
            Apply(Root1Scene, Root1Look);
            Apply(Root2Scene, Root2Look);
            ShootAll();
        }

        /// <summary>場所のシーンに光と空気を書き込む。空と後処理の素材は在れば書き換える</summary>
        private static void Apply(string scenePath, Look look)
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Places");

            Material sky = Sky(look.Name + "Sky", look.SkyTint, look.SkyGround, look.SkyThickness, look.SkyExposure);
            PostProcessProfile post = PostProfiles.Load(ArtFolder + "/" + look.Name + "Post.asset");

            ColorGrading grading = PostProfiles.Effect<ColorGrading>(post);
            grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.postExposure.Override(look.Exposure);
            grading.temperature.Override(look.Temperature);
            grading.tint.Override(look.Tint);
            grading.saturation.Override(look.Saturation);
            grading.contrast.Override(look.Contrast);

            // 軽い作りのにじみ。Android の 60fps を守る
            Bloom bloom = PostProfiles.Effect<Bloom>(post);
            bloom.intensity.Override(look.Bloom);
            bloom.threshold.Override(1.1f);
            bloom.softKnee.Override(0.5f);
            bloom.diffusion.Override(6f);
            bloom.color.Override(Color.white);
            bloom.fastMode.Override(true);

            Vignette vignette = PostProfiles.Effect<Vignette>(post);
            vignette.intensity.Override(look.Vignette);
            vignette.smoothness.Override(0.4f);

            // 物の際を暗くする効果は Android で重い。ほかの範囲から混ざらないよう、はっきり切る
            PostProfiles.Effect<AmbientOcclusion>(post).enabled.Override(false);

            EditorUtility.SetDirty(post);
            AssetDatabase.SaveAssets();

            Edit(scenePath, scene =>
            {
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = look.AmbientSky;
                RenderSettings.ambientEquatorColor = look.AmbientEquator;
                RenderSettings.ambientGroundColor = look.AmbientGround;

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = look.Fog;
                RenderSettings.fogDensity = look.FogDensity;

                Light sun = Sun(scene);
                sun.transform.rotation = Quaternion.Euler(look.SunAngles);
                sun.color = look.SunColor;
                sun.intensity = look.SunIntensity;
                sun.shadowStrength = look.SunShadow;
                RenderSettings.sun = sun;

                Volume(scene, post);
            });
        }

        /// <summary>拠点と根源の見た目を、遊ぶときのカメラの位置関係でまとめて撮る。見た目をそろえるときの見比べに使う。シーンは保存しない</summary>
        [MenuItem("Tools/Places/Shoot All Places")]
        public static void ShootAll()
        {
            foreach (string place in Places)
            {
                foreach (string path in Shoot(place)) Debug.Log("撮った: " + path);
            }
        }

        /// <summary>
        /// 場所のシーンを書き換えて保存する。開いていればそのまま使い、開いていなければ足して開き、終わったら閉じる。
        /// 空・環境光・霧は有効なシーンのものが書き換わるので、書き換えるあいだだけ場所のシーンを有効にする。
        /// 保存していない手直しは消さない
        /// </summary>
        internal static void Edit(string scenePath, Action<Scene> change)
        {
            Scene open = SceneManager.GetSceneByPath(scenePath);
            bool reuse = open.isLoaded;
            if (reuse && open.isDirty)
                throw new InvalidOperationException(scenePath + " に保存していない変更がある。保存するか捨ててから書き込む");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = reuse ? open : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);
                change(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (!reuse) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>場所の空。手続きで描く空の素材を、在れば書き換える。作り直すと GUID が変わり、シーンからの参照が切れる</summary>
        private static Material Sky(string name, Color tint, Color ground, float thickness, float exposure)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetFloat("_AtmosphereThickness", thickness);
            material.SetColor("_SkyTint", tint);
            material.SetColor("_GroundColor", ground);
            material.SetFloat("_Exposure", exposure);
            EditorUtility.SetDirty(material);

            return material;
        }

        /// <summary>場所の日の光。場所のシーンにただ 1 つ置かれた、向きのある光</summary>
        private static Light Sun(Scene scene)
        {
            List<Light> found = new List<Light>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional) found.Add(light);
                }
            }

            if (found.Count != 1) throw new InvalidOperationException(scene.name + " に向きのある光が " + found.Count + " 個ある。1 個にする");

            return found[0];
        }

        /// <summary>場所だけに効く後処理の範囲。シーンのルートに 1 つ置き、何度書き込んでも増えない</summary>
        private static void Volume(Scene scene, PostProcessProfile profile)
        {
            int layer = LayerMask.NameToLayer(PostLayer);
            if (layer < 0) throw new InvalidOperationException(PostLayer + " のレイヤーが無い");

            GameObject holder = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == PostLayer) holder = root;
            }

            // 新しく作った物は有効なシーン（書き換え中の場所のシーン）に入る
            if (holder == null) holder = new GameObject(PostLayer);
            holder.layer = layer;

            PostProcessVolume volume = holder.GetComponent<PostProcessVolume>();
            if (volume == null) volume = holder.AddComponent<PostProcessVolume>();

            volume.isGlobal = true;
            volume.priority = PlacePriority;
            volume.sharedProfile = profile;
        }

        /// <summary>
        /// 場所のシーンを、遊ぶときのカメラの位置関係で撮る。立ち位置は地形の撮り比べと同じ（場の左端から右端までと、見渡す 1 枚）。
        /// シーンはプレビューで開き、保存しない。プレイヤーと UI は写らない。絵は Temp/PlaceShots/場所の名前/ に出る
        /// </summary>
        internal static List<string> Shoot(string scenePath)
        {
            string folder = Path.Combine(ShotFolder, Path.GetFileNameWithoutExtension(scenePath));
            Directory.CreateDirectory(folder);

            Scene scene = EditorSceneManager.OpenPreviewScene(scenePath);
            RenderTexture target = new RenderTexture(Width, Height, 24);
            List<string> written = new List<string>();

            // 空・環境光・霧は有効なシーンのものが使われる。プレビューのシーンのものに差し替える
            bool overridden = Unsupported.SetOverrideLightingSettings(scene);

            try
            {
                Camera camera = Eye(scene);
                List<Pose> poses = FieldPreview.Poses(scene);

                for (int i = 0; i < poses.Count; i++)
                {
                    camera.transform.SetPositionAndRotation(poses[i].position, poses[i].rotation);
                    string path = Path.Combine(folder, i + ".png");
                    LookDevShots.Capture(camera, target, path);
                    written.Add(path.Replace('\\', '/'));
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

        /// <summary>撮るためのカメラ。常駐シーンのカメラと同じく後処理の層を持ち、Post Processing のレイヤーの範囲を見る</summary>
        private static Camera Eye(Scene scene)
        {
            GameObject eye = new GameObject("PlaceShotCamera");
            SceneManager.MoveGameObjectToScene(eye, scene);

            Camera camera = eye.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.fieldOfView = PlayerView.FieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;

            PostProcessResources resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(PostResources);
            if (resources == null) throw new InvalidOperationException(PostResources + " が無い。PPv2 のパッケージを入れる");

            PostProcessLayer post = eye.AddComponent<PostProcessLayer>();
            post.Init(resources);
            post.volumeTrigger = eye.transform;
            post.volumeLayer = LayerMask.GetMask(PostLayer);

            return camera;
        }
    }
}
