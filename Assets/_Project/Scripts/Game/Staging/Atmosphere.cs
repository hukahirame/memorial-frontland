using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 読み込んである場所の光と空気を、プレイヤーの位置で混ぜて画面に効かせる。常駐シーンに 1 つだけ置く。
    /// 場所の境目に近づくと、隣の場所の空・霧・環境光・日の光・後処理へ少しずつ寄せる。
    /// 日の光は常駐の 1 本だけを点け、場所のシーンの日の光は消す。強さは一日の巡りが Daylight で配る
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        /// <summary>場の範囲からこれだけ離れると、その場所の光と空気は効かなくなる。境目の両側でこの幅ずつ混ざる</summary>
        public const float BlendDistance = 5f;

        [SerializeField] private AreaTravel travel;

        private readonly List<PlaceAir> _airs = new List<PlaceAir>();

        private readonly List<float> _weights = new List<float>();

        private readonly List<float> _applied = new List<float>();

        private Light _sun;

        private Material _sky;

        private Scene _appliedTo;

        private bool _changed;

        private void Awake()
        {
            travel.Loaded += Capture;
            travel.Unloading += Forget;
        }

        private void OnDestroy()
        {
            travel.Loaded -= Capture;
            travel.Unloading -= Forget;
            if (_sky != null) Destroy(_sky);
        }

        private void Capture(Area area)
        {
            if (_airs.Exists(air => air.Area == area)) return;

            PlaceAir captured = PlaceAir.Capture(area);
            if (captured.Sun != null)
            {
                if (_sun == null) _sun = CoreSun(captured.Sun);
                captured.Sun.enabled = false;
            }

            _airs.Add(captured);
            _changed = true;
        }

        private void Forget(Area area)
        {
            if (_airs.RemoveAll(air => air.Area == area) > 0) _changed = true;
        }

        /// <summary>常駐の日の光。最初に読み込んだ場所の日の光の作りを写し、向きと色はあとで混ぜる</summary>
        private Light CoreSun(Light model)
        {
            GameObject holder = new GameObject("Sun");
            holder.transform.SetParent(transform, false);

            Light sun = holder.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = model.intensity;
            sun.shadows = model.shadows;
            sun.shadowResolution = model.shadowResolution;
            sun.shadowBias = model.shadowBias;
            sun.shadowNormalBias = model.shadowNormalBias;
            sun.shadowNearPlane = model.shadowNearPlane;
            sun.renderMode = model.renderMode;
            sun.cullingMask = model.cullingMask;
            sun.bounceIntensity = model.bounceIntensity;

            // 強さは一日の巡りが配る
            holder.AddComponent<Daylight>();

            return sun;
        }

        private void LateUpdate()
        {
            if (_airs.Count == 0) return;

            Weigh();

            //場所に着くと有効なシーンが替わり、そのシーンの設定が効く。替わったら書き直す
            Scene active = SceneManager.GetActiveScene();
            if (!_changed && active == _appliedTo && SameWeights()) return;

            Apply();

            _appliedTo = active;
            _applied.Clear();
            _applied.AddRange(_weights);
            _changed = false;
        }

        /// <summary>場所ごとの効き。場の範囲の中なら 1、範囲から BlendDistance 離れると 0。合わせて 1 にする</summary>
        private void Weigh()
        {
            _weights.Clear();
            Vector3 at = travel.Traveler.position;
            float total = 0f;

            foreach (PlaceAir air in _airs)
            {
                float weight = Mathf.Clamp01(1f - OutsideBy(air.Area.Exposition, at) / BlendDistance);
                _weights.Add(weight);
                total += weight;
            }

            //どの場所からも離れている（移っている途中など）ときは、今いる場所だけを効かせる
            if (total <= 0f)
            {
                for (int i = 0; i < _airs.Count; i++)
                {
                    float weight = _airs[i].Area == travel.Current || (travel.Current == null && i == 0) ? 1f : 0f;
                    _weights[i] = weight;
                    total += weight;
                }
            }

            if (total <= 0f) _weights[0] = total = 1f;

            for (int i = 0; i < _weights.Count; i++) _weights[i] /= total;
        }

        /// <summary>場の範囲（XL, XS, ZL, ZS）の外へ、横と奥行きでどれだけ出ているか</summary>
        private static float OutsideBy(Vector4 exposition, Vector3 at)
        {
            float dx = Mathf.Max(exposition.y - at.x, 0f, at.x - exposition.x);
            float dz = Mathf.Max(exposition.w - at.z, 0f, at.z - exposition.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private bool SameWeights()
        {
            if (_applied.Count != _weights.Count) return false;

            for (int i = 0; i < _weights.Count; i++)
            {
                if (Mathf.Abs(_applied[i] - _weights[i]) > 0.001f) return false;
            }

            return true;
        }

        private void Apply()
        {
            PlaceAir main = Dominant();

            Color fogColor = Color.clear, ambientSky = Color.clear, ambientEquator = Color.clear, ambientGround = Color.clear, sunColor = Color.clear;
            float fogDensity = 0f, fogStart = 0f, fogEnd = 0f, ambientIntensity = 0f, reflectionIntensity = 0f, sunShadow = 0f;
            bool fog = false;

            for (int i = 0; i < _airs.Count; i++)
            {
                PlaceAir air = _airs[i];
                float w = _weights[i];
                if (w <= 0f) continue;

                fog |= air.Fog;
                fogColor += air.FogColor * w;
                fogDensity += (air.Fog ? air.FogDensity : 0f) * w;
                fogStart += air.FogStart * w;
                fogEnd += air.FogEnd * w;
                ambientSky += air.AmbientSky * w;
                ambientEquator += air.AmbientEquator * w;
                ambientGround += air.AmbientGround * w;
                ambientIntensity += air.AmbientIntensity * w;
                reflectionIntensity += air.ReflectionIntensity * w;
                sunColor += (air.Sun != null ? air.SunColor : main.SunColor) * w;
                sunShadow += (air.Sun != null ? air.SunShadow : main.SunShadow) * w;
            }

            RenderSettings.fog = fog;
            RenderSettings.fogMode = main.FogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            RenderSettings.ambientMode = main.AmbientMode;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;

            // 反射の絵は混ぜられないので、いちばん効いている場所のものを使う
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = main.Reflection;
            RenderSettings.reflectionIntensity = reflectionIntensity;

            RenderSettings.skybox = Sky(main);

            if (_sun != null)
            {
                _sun.transform.rotation = SunRotation(main);
                _sun.color = sunColor;
                _sun.shadowStrength = sunShadow;
                RenderSettings.sun = _sun;
            }

            Post();
        }

        private PlaceAir Dominant()
        {
            int best = 0;
            for (int i = 1; i < _weights.Count; i++)
            {
                if (_weights[i] > _weights[best]) best = i;
            }

            return _airs[best];
        }

        /// <summary>日の光の向き。効きの重みで順に寄せていく</summary>
        private Quaternion SunRotation(PlaceAir main)
        {
            Quaternion rotation = main.SunRotation;
            float sum = 0f;

            for (int i = 0; i < _airs.Count; i++)
            {
                float w = _weights[i];
                if (w <= 0f || _airs[i].Sun == null) continue;

                sum += w;
                rotation = sum <= w ? _airs[i].SunRotation : Quaternion.Slerp(rotation, _airs[i].SunRotation, w / sum);
            }

            return rotation;
        }

        /// <summary>
        /// 空。効いている場所の空が同じシェーダーなら、数と色を重みで混ぜた素材を作って使う。
        /// シェーダーが違えば混ぜられないので、いちばん効いている場所の空をそのまま使う
        /// </summary>
        private Material Sky(PlaceAir main)
        {
            if (main.Skybox == null) return null;

            for (int i = 0; i < _airs.Count; i++)
            {
                Material other = _airs[i].Skybox;
                if (_weights[i] > 0f && (other == null || other.shader != main.Skybox.shader)) return main.Skybox;
            }

            if (_sky == null || _sky.shader != main.Skybox.shader)
            {
                if (_sky != null) Destroy(_sky);
                _sky = new Material(main.Skybox) { name = "BlendedSky" };
            }

            Shader shader = main.Skybox.shader;
            for (int p = 0; p < shader.GetPropertyCount(); p++)
            {
                int id = shader.GetPropertyNameId(p);

                switch (shader.GetPropertyType(p))
                {
                    case ShaderPropertyType.Color:
                        Color color = Color.clear;
                        for (int i = 0; i < _airs.Count; i++) if (_weights[i] > 0f) color += _airs[i].Skybox.GetColor(id) * _weights[i];
                        _sky.SetColor(id, color);
                        break;

                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        float number = 0f;
                        for (int i = 0; i < _airs.Count; i++) if (_weights[i] > 0f) number += _airs[i].Skybox.GetFloat(id) * _weights[i];
                        _sky.SetFloat(id, number);
                        break;

                    case ShaderPropertyType.Vector:
                        Vector4 vector = Vector4.zero;
                        for (int i = 0; i < _airs.Count; i++) if (_weights[i] > 0f) vector += _airs[i].Skybox.GetVector(id) * _weights[i];
                        _sky.SetVector(id, vector);
                        break;
                }
            }

            return _sky;
        }

        /// <summary>
        /// 後処理。場所の範囲はどれも全体に効く同じ優先度なので、並べた順に優先度を付け、重みで順に重ねる。
        /// 後ろの範囲ほど、それまでの重みの合計に対する自分の重みの割合で前の結果に重なり、全体で重みどおりに混ざる
        /// </summary>
        private void Post()
        {
            float sum = 0f;
            int order = 1;

            for (int i = 0; i < _airs.Count; i++)
            {
                if (_airs[i].Post == null) continue;

                float w = _weights[i];
                sum += w;
                _airs[i].Post.priority = order++;
                _airs[i].Post.weight = sum > 0f ? w / sum : 0f;
            }
        }
    }
}
