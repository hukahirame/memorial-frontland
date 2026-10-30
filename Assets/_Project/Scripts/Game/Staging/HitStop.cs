using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 当たった瞬間に時間を止める。止めている間に次が来たら、遅く終わる方に合わせる。
    /// 時間は全体で 1 つなので、無効になったら止めたままにせず戻す
    /// </summary>
    public sealed class HitStop : MonoBehaviour
    {
        /// <summary>当てたとき</summary>
        public const float HitSeconds = 0.05f;

        /// <summary>倒したとき</summary>
        public const float KillSeconds = 0.1f;

        private float _resumeAt;
        private bool _isStopping;

        public void Stop(float seconds)
        {
            float resumeAt = Time.unscaledTime + seconds;
            if (_isStopping && resumeAt <= _resumeAt) return;

            _resumeAt = resumeAt;
            _isStopping = true;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (_isStopping && Time.unscaledTime >= _resumeAt) Resume();
        }

        private void OnDisable()
        {
            if (_isStopping) Resume();
        }

        private void Resume()
        {
            _isStopping = false;
            Time.timeScale = 1f;
        }
    }
}
