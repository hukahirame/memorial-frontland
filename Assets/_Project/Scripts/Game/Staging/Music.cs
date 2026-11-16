using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 曲を流す。常駐シーンに 1 つだけ置く。
    /// 曲を替えるときは、前の曲を下げながら次の曲を上げて重ねる。同じ曲をまた頼まれたら、止めずに流し続ける。
    /// 画面を覆っているあいだは、覆いの濃さに合わせて全体の音量を下げる（ScreenVeil）
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        /// <summary>前の曲から次の曲へ重ねて移るのにかける秒数</summary>
        public const float CrossSeconds = 1.5f;

        [Tooltip("曲を鳴らす。重ねて移るための 2 つめは、これを写して作る")]
        [SerializeField] private AudioSource source;

        /// <summary>今の曲を鳴らしている側</summary>
        private AudioSource _current;

        /// <summary>下げている前の曲を鳴らしている側</summary>
        private AudioSource _previous;

        /// <summary>覆っていないときの音量。置いた AudioSource の音量</summary>
        private float _volume = 1f;

        private float _veil;

        /// <summary>重ねて移る進み。0 で前の曲だけ、1 で今の曲だけが鳴る</summary>
        private float _cross = 1f;

        /// <summary>今の曲。替えている途中なら、上げている側の曲</summary>
        public AudioClip Clip
        {
            get { return _current.clip; }
        }

        public bool IsPlaying
        {
            get { return _current.isPlaying; }
        }

        /// <summary>画面を覆っている割合。1 で鳴らない</summary>
        public float Veil
        {
            get { return _veil; }
            set
            {
                _veil = Mathf.Clamp01(value);
                Apply();
            }
        }

        private void Awake()
        {
            _volume = source.volume;
            _current = source;
            _previous = Twin(source);
            Apply();
        }

        /// <summary>
        /// 曲を替える。同じ曲が鳴っていれば何もしない。atOnce なら重ねずにすぐ替える（画面を覆っているとき）。
        /// 替えている途中でまた替えたときは、下げている側を次の曲に使い、どちらの音量も跳ねないように進みを付け直す
        /// </summary>
        public void Play(AudioClip clip, bool atOnce)
        {
            if (clip == null || (_current.clip == clip && _current.isPlaying)) return;

            bool wasPlaying = _current.isPlaying;
            AudioSource next = _previous;
            _previous = _current;
            _current = next;

            _current.clip = clip;
            _current.Play();

            _cross = atOnce || !wasPlaying ? 1f : 1f - _cross;
            if (_cross >= 1f) _previous.Stop();
            Apply();
        }

        //設定の窓で時間を止めても曲は替わりきるよう、実時間で進める
        private void Update()
        {
            if (_cross >= 1f) return;

            _cross = Mathf.MoveTowards(_cross, 1f, Time.unscaledDeltaTime / CrossSeconds);
            if (_cross >= 1f) _previous.Stop();
            Apply();
        }

        private void Apply()
        {
            if (_current == null) return;

            float volume = _volume * (1f - _veil);
            _current.volume = volume * _cross;
            _previous.volume = volume * (1f - _cross);
        }

        private AudioSource Twin(AudioSource model)
        {
            AudioSource twin = gameObject.AddComponent<AudioSource>();
            twin.playOnAwake = false;
            twin.loop = model.loop;
            twin.outputAudioMixerGroup = model.outputAudioMixerGroup;
            twin.priority = model.priority;
            twin.pitch = model.pitch;
            twin.panStereo = model.panStereo;
            twin.spatialBlend = model.spatialBlend;
            twin.volume = 0f;
            return twin;
        }
    }
}
