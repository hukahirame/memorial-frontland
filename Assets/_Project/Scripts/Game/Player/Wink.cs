using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 目のまばたき。ときどき閉じ、打たれたときは少し長く閉じる。倒れているあいだは開いたまま止める
    /// </summary>
    public sealed class Wink : MonoBehaviour
    {
        private const float OpenSeconds = 4f;
        private const float ClosedSeconds = 0.2f;

        /// <summary>打たれたときに、ふだんより長く閉じている分</summary>
        private const float FlinchExtraSeconds = 0.3f;

        [SerializeField, FormerlySerializedAs("sr")] private SpriteRenderer _eyes;
        [SerializeField, FormerlySerializedAs("open")] private Sprite _open;
        [SerializeField, FormerlySerializedAs("close")] private Sprite _closed;

        private float _elapsed;
        private bool _isClosed;
        private bool _isStopped;

        /// <summary>打たれた。目をつむる</summary>
        public void Flinch()
        {
            Show(true);
            _elapsed = -FlinchExtraSeconds;
        }

        /// <summary>倒れた。目を開いたまま、まばたきを止める</summary>
        public void Stop()
        {
            Show(false);
            _isStopped = true;
        }

        /// <summary>起き上がった。まばたきを戻す</summary>
        public void Resume()
        {
            Show(false);
            _isStopped = false;
        }

        private void Update()
        {
            if (_isStopped) return;

            _elapsed += Time.deltaTime;
            if (!_isClosed && _elapsed > OpenSeconds) Show(true);
            else if (_isClosed && _elapsed > ClosedSeconds) Show(false);
        }

        private void Show(bool closed)
        {
            _eyes.sprite = closed ? _closed : _open;
            _isClosed = closed;
            _elapsed = 0f;
        }
    }
}
