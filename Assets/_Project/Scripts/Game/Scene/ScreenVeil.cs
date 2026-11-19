using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所を移るときの暗転。画面を黒で覆い、曲の音量も一緒に下げる。
    /// 始めは覆っておき、最初の場所に着いてから開ける。常駐シーンの UI だけが映る一瞬を見せないため
    /// </summary>
    public sealed class ScreenVeil : MonoBehaviour
    {
        /// <summary>1 フレームで進める上限。読み込みの重いフレームで一気に開かないように</summary>
        private const float MaxStepSeconds = 1f / 30f;

        [Tooltip("覆うのと開けるのに、それぞれかける秒数")]
        [SerializeField, FormerlySerializedAs("fadeSeconds")] private float _fadeSeconds = 0.3f;

        [SerializeField, FormerlySerializedAs("cover")] private CanvasGroup _cover;

        [Tooltip("覆うあいだ音量を下げる。空なら画面だけ")]
        [SerializeField, FormerlySerializedAs("music")] private Music _music;

        private void Awake()
        {
            Apply(1f);
        }

        public IEnumerator Close()
        {
            yield return Fade(1f);
        }

        public IEnumerator Open()
        {
            yield return Fade(0f);
        }

        //止めた時間の中でも進むよう実時間で数える
        private IEnumerator Fade(float target)
        {
            float from = _cover.alpha;
            if (Mathf.Approximately(from, target)) yield break;

            for (float t = 0f; t < _fadeSeconds; t += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds))
            {
                Apply(Mathf.Lerp(from, target, t / _fadeSeconds));
                yield return null;
            }

            Apply(target);
        }

        private void Apply(float alpha)
        {
            _cover.alpha = alpha;
            if (_music != null) _music.Veil = alpha;
        }
    }
}
