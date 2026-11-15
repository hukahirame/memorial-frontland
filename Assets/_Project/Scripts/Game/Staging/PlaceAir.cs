using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所 1 つの光と空気。空・霧・環境光・反射はシーンの設定から、日の光と後処理は場所のシーンの物から写す。
    /// シーンの設定は有効なシーンのものしか読めないので、写すあいだだけ場所のシーンを有効にする
    /// </summary>
    public sealed class PlaceAir
    {
        private PlaceAir(Area area)
        {
            Area = area;
        }

        public Area Area { get; }

        public Material Skybox { get; private set; }

        public bool Fog { get; private set; }

        public FogMode FogMode { get; private set; }

        public Color FogColor { get; private set; }

        public float FogDensity { get; private set; }

        public float FogStart { get; private set; }

        public float FogEnd { get; private set; }

        public AmbientMode AmbientMode { get; private set; }

        public Color AmbientSky { get; private set; }

        public Color AmbientEquator { get; private set; }

        public Color AmbientGround { get; private set; }

        public float AmbientIntensity { get; private set; }

        /// <summary>空から映り込む物。場所のシーンに焼いた反射が無ければ黒</summary>
        public Texture Reflection { get; private set; }

        public float ReflectionIntensity { get; private set; }

        /// <summary>場所のシーンに置いた日の光。無ければ null</summary>
        public Light Sun { get; private set; }

        public Quaternion SunRotation { get; private set; }

        public Color SunColor { get; private set; }

        public float SunShadow { get; private set; }

        /// <summary>場所だけに効く後処理の範囲。無ければ null</summary>
        public PostProcessVolume Post { get; private set; }

        /// <summary>読み込んだ場所の光と空気を写す。場所のシーンの設定に手を入れる前に呼ぶ</summary>
        public static PlaceAir Capture(Area area)
        {
            PlaceAir air = new PlaceAir(area);

            Scene was = SceneManager.GetActiveScene();
            bool swap = was != area.Scene;
            if (swap) SceneManager.SetActiveScene(area.Scene);

            try
            {
                air.Skybox = RenderSettings.skybox;
                air.Fog = RenderSettings.fog;
                air.FogMode = RenderSettings.fogMode;
                air.FogColor = RenderSettings.fogColor;
                air.FogDensity = RenderSettings.fogDensity;
                air.FogStart = RenderSettings.fogStartDistance;
                air.FogEnd = RenderSettings.fogEndDistance;
                air.AmbientMode = RenderSettings.ambientMode;
                air.AmbientSky = RenderSettings.ambientSkyColor;
                air.AmbientEquator = RenderSettings.ambientEquatorColor;
                air.AmbientGround = RenderSettings.ambientGroundColor;
                air.AmbientIntensity = RenderSettings.ambientIntensity;
                air.Reflection = ReflectionProbe.defaultTexture;
                air.ReflectionIntensity = RenderSettings.reflectionIntensity;
            }
            finally
            {
                if (swap && was.IsValid()) SceneManager.SetActiveScene(was);
            }

            air.Sun = DirectionalLight(area.Scene);
            if (air.Sun != null)
            {
                air.SunRotation = air.Sun.transform.rotation;
                air.SunColor = air.Sun.color;
                air.SunShadow = air.Sun.shadowStrength;
            }

            air.Post = area.Find<PostProcessVolume>();

            return air;
        }

        private static Light DirectionalLight(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional) return light;
                }
            }

            return null;
        }
    }
}
