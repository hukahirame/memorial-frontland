using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 曲を流す。常駐シーンに 1 つだけ置く。曲は Addressables で、曲の名前を住所にして裏で読み、鳴らし終えたら手放す。
    /// 曲を替えるときは、読み終えるまで前の曲を鳴らし続け、前の曲を下げながら次の曲を上げて重ねる。同じ曲をまた頼まれたら、止めずに流し続ける。
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

        /// <summary>最後に頼まれた曲。読み終える前に別の曲を頼まれたら、読み終えた前の曲は鳴らさずに手放す</summary>
        private string _wanted;

        /// <summary>最後に読み始めた曲の読み込み</summary>
        private AsyncOperationHandle<AudioClip> _loading;

        private string _loadingAddress;

        /// <summary>それぞれの AudioSource が鳴らしている曲の読み込み。鳴らし終えたら手放す</summary>
        private readonly Dictionary<AudioSource, AsyncOperationHandle<AudioClip>> _held =
            new Dictionary<AudioSource, AsyncOperationHandle<AudioClip>>();

        /// <summary>それぞれの AudioSource が鳴らしている曲の住所</summary>
        private readonly Dictionary<AudioSource, string> _addresses = new Dictionary<AudioSource, string>();

        /// <summary>最後に頼まれた曲を読んでいる途中か</summary>
        public bool IsLoading
        {
            get { return _loading.IsValid() && !_loading.IsDone; }
        }

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

        private void OnDestroy()
        {
            foreach (AsyncOperationHandle<AudioClip> held in _held.Values) Addressables.Release(held);
            _held.Clear();
        }

        /// <summary>
        /// 曲を替える。同じ曲が鳴っているか読んでいる途中なら何もしない。読み終えてから替える。
        /// atOnce なら重ねずにすぐ替える（画面を覆っているとき）
        /// </summary>
        public void Play(string address, bool atOnce)
        {
            _wanted = address;
            if (_current.isPlaying && Address(_current) == address) return;
            if (IsLoading && _loadingAddress == address) return;

            AsyncOperationHandle<AudioClip> loading = Addressables.LoadAssetAsync<AudioClip>(address);
            _loading = loading;
            _loadingAddress = address;

            loading.Completed += done =>
            {
                if (this == null || _wanted != address || done.Status != AsyncOperationStatus.Succeeded)
                {
                    Addressables.Release(done);
                    return;
                }

                Switch(address, done, atOnce);
            };
        }

        /// <summary>
        /// 読み終えた曲に替える。替えている途中でまた替えたときは、下げている側を次の曲に使い、
        /// どちらの音量も跳ねないように進みを付け直す
        /// </summary>
        private void Switch(string address, AsyncOperationHandle<AudioClip> loaded, bool atOnce)
        {
            bool wasPlaying = _current.isPlaying;
            AudioSource next = _previous;
            _previous = _current;
            _current = next;

            _current.clip = loaded.Result;
            Hold(_current, address, loaded);
            _current.Play();

            _cross = atOnce || !wasPlaying ? 1f : 1f - _cross;
            if (_cross >= 1f) Silence(_previous);
            Apply();
        }

        //設定の窓で時間を止めても曲は替わりきるよう、実時間で進める
        private void Update()
        {
            if (_cross >= 1f) return;

            _cross = Mathf.MoveTowards(_cross, 1f, Time.unscaledDeltaTime / CrossSeconds);
            if (_cross >= 1f) Silence(_previous);
            Apply();
        }

        /// <summary>止めて、鳴らしていた曲を手放す</summary>
        private void Silence(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            Hold(source, null, default);
        }

        /// <summary>その AudioSource が鳴らす曲の読み込みを付け替え、前の読み込みを手放す</summary>
        private void Hold(AudioSource source, string address, AsyncOperationHandle<AudioClip> loaded)
        {
            if (_held.TryGetValue(source, out AsyncOperationHandle<AudioClip> before)) Addressables.Release(before);

            if (loaded.IsValid()) _held[source] = loaded;
            else _held.Remove(source);

            _addresses[source] = address;
        }

        private string Address(AudioSource source)
        {
            return _addresses.TryGetValue(source, out string address) ? address : null;
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
