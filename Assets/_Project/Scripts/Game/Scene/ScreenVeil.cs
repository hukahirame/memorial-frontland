using System.Collections;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所を移るときの暗転。画面を黒で覆い、BGM の音量も一緒に下げる。
    /// 始めは覆っておき、最初の場所に着いてから開ける。常駐シーンの UI だけが映る一瞬を見せないため
    /// </summary>
    public sealed class ScreenVeil : MonoBehaviour
    {
        /// <summary>1 フレームで進める上限。読み込みの重いフレームで一気に開かないように</summary>
        private const float MaxStepSeconds = 1f / 30f;

        [Tooltip("覆うのと開けるのに、それぞれかける秒数")]
        [SerializeField] private float fadeSeconds = 0.3f;

        [SerializeField] private CanvasGroup cover;

        [Tooltip("覆うあいだ音量を下げる。空なら画面だけ")]
        [SerializeField] private AudioSource bgm;

        /// <summary>開けたときの音量。覆う前の値に戻す</summary>
        private float _volume = 1f;

        private void Awake()
        {
            if (bgm != null) _volume = bgm.volume;
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
            float from = cover.alpha;
            if (Mathf.Approximately(from, target)) yield break;

            for (float t = 0f; t < fadeSeconds; t += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds))
            {
                Apply(Mathf.Lerp(from, target, t / fadeSeconds));
                yield return null;
            }

            Apply(target);
        }

        private void Apply(float alpha)
        {
            cover.alpha = alpha;
            if (bgm != null) bgm.volume = _volume * (1f - alpha);
        }
    }
}
