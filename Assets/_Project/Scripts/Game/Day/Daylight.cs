using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所の日の光。場所のシーンの、向きのある光に付ける。
    /// 強さは常駐シーンの一日の巡り（Sun）が決める。場所のシーンからは常駐シーンへ参照を張れないので、読み込んだときに Atmosphere がつなぐ
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class Daylight : MonoBehaviour
    {
        private Sun _clock;

        /// <summary>一日の巡りにつなぐ。つないだときも、いまの強さにする</summary>
        public void Bind(Sun clock)
        {
            if (_clock != null) _clock.LightChanged -= Show;

            _clock = clock;
            _clock.LightChanged += Show;
            Show(_clock.LightIntensity);
        }

        private void OnDestroy()
        {
            if (_clock != null) _clock.LightChanged -= Show;
        }

        //読み込んだ直後は物が止まっていて Awake がまだのことがあるので、光は使うときに引く
        private void Show(float intensity)
        {
            GetComponent<Light>().intensity = intensity;
        }
    }
}
