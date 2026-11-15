using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MemorialFloor.Game
{
    /// <summary>
    /// タイトルの背景。絵の上に、同じ構図で撮った数秒の動画を重ねて繰り返し流し、全体をゆっくり寄せては戻し、
    /// 光の粒を下から上へ漂わせる。動画が流れ始めるまでと、流せないときは絵が見えている
    /// </summary>
    public sealed class TitleBackdrop : MonoBehaviour
    {
        /// <summary>
        /// 背景の動画。StreamingAssets に置いて URL で流す（WebGL は動画の資産を流せない）。
        /// 撮り直すのは PlayMode の `TitleMovie.タイトルの動画を撮る`（名指しで回したときだけ走る）
        /// </summary>
        public const string MovieFile = "TitleBackground.mp4";

        /// <summary>動画の大きさ。撮るときもこの大きさで撮る</summary>
        public const int MovieWidth = 1920;

        public const int MovieHeight = 1080;

        /// <summary>動画が流れ始めてから、絵の上に濃くなりきるまでの秒数</summary>
        private const float MovieFadeSeconds = 0.6f;

        /// <summary>いちばん寄せたときの大きさ。1 で元の大きさ</summary>
        private const float MaxZoom = 1.06f;

        /// <summary>寄せて戻る 1 往復の秒数</summary>
        private const float ZoomSeconds = 40f;

        private const int MoteCount = 18;

        /// <summary>光の粒が昇る速さ（参照解像度の画素毎秒）</summary>
        private const float RiseMin = 14f;
        private const float RiseMax = 36f;

        /// <summary>光の粒の大きさ（参照解像度の画素）</summary>
        private const float SizeMin = 6f;
        private const float SizeMax = 16f;

        /// <summary>光の粒を出す横の範囲（画面の幅に対する比）。題字の下を避け、吹き抜けの側に寄せる</summary>
        private const float SpreadFrom = 0.3f;

        [Tooltip("背景の絵。ゆっくり寄せては戻す")]
        [SerializeField] private RectTransform picture;

        [Tooltip("光の粒を漂わせる範囲。画面いっぱいに広げておく")]
        [SerializeField] private RectTransform motes;

        [Tooltip("光の粒の絵。ぼけた丸")]
        [SerializeField] private Sprite mote;

        [SerializeField] private Color moteColor = new Color(1f, 0.95f, 0.8f, 0.75f);

        private Mote[] flying;

        private RawImage movie;
        private VideoPlayer player;
        private RenderTexture screen;

        /// <summary>動画が流れ始めた時刻。始まる前は負</summary>
        private float movieStartedAt = -1f;

        private sealed class Mote
        {
            public RectTransform Rect;
            public Image Image;
            public float Rise;
            public float Sway;
            public float Phase;
            public float Height;
        }

        private void Start()
        {
            flying = new Mote[MoteCount];
            for (int i = 0; i < MoteCount; i++)
            {
                GameObject dot = new GameObject("Mote", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(motes, false);

                Image image = dot.GetComponent<Image>();
                image.sprite = mote;
                image.raycastTarget = false;

                flying[i] = new Mote { Rect = (RectTransform)dot.transform, Image = image };
                Respawn(flying[i], Random.value);
            }

            PlayMovie();
        }

        private void OnDestroy()
        {
            if (screen != null) screen.Release();
        }

        private void Update()
        {
            float k = (1f - Mathf.Cos(Time.unscaledTime / ZoomSeconds * 2f * Mathf.PI)) / 2f;
            picture.localScale = Vector3.one * Mathf.Lerp(1f, MaxZoom, k);

            if (movieStartedAt >= 0f)
            {
                movie.color = new Color(1f, 1f, 1f, Mathf.Clamp01((Time.unscaledTime - movieStartedAt) / MovieFadeSeconds));
            }

            Rect area = motes.rect;
            foreach (Mote dot in flying)
            {
                Vector2 at = dot.Rect.anchoredPosition;
                at.y += dot.Rise * Time.unscaledDeltaTime;
                at.x += Mathf.Sin(Time.unscaledTime * 0.6f + dot.Phase) * dot.Sway * Time.unscaledDeltaTime;
                dot.Rect.anchoredPosition = at;

                //下から現れ、上に近づくほど消える
                float climbed = Mathf.InverseLerp(area.yMin, area.yMax, at.y);
                float alpha = Mathf.Clamp01(climbed / 0.15f) * Mathf.Clamp01((1f - climbed) / 0.35f);
                Color color = moteColor;
                color.a *= alpha;
                dot.Image.color = color;

                if (at.y > area.yMax) Respawn(dot, 0f);
            }
        }

        /// <summary>
        /// 動画を絵の子に重ねて繰り返し流す。絵と同じ枠に広げるので、絵の切り抜き方と寄せる動きがそのまま効く。
        /// 流れ始めるまでは透明にしておき、始まったら薄く浮かび上がらせる。手元のファイルが無ければ流さない
        /// （WebGL とスマホは URL なので、確かめずに流し、流せなければ絵のまま）
        /// </summary>
        private void PlayMovie()
        {
            string url = Path.Combine(Application.streamingAssetsPath, MovieFile);
            if (!url.Contains("://") && !File.Exists(url)) return;

            var holder = new GameObject("Movie", typeof(RectTransform), typeof(RawImage));
            holder.layer = picture.gameObject.layer;

            var rect = (RectTransform)holder.transform;
            rect.SetParent(picture, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            screen = new RenderTexture(MovieWidth, MovieHeight, 0);
            movie = holder.GetComponent<RawImage>();
            movie.texture = screen;
            movie.raycastTarget = false;
            movie.color = new Color(1f, 1f, 1f, 0f);

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.skipOnDrop = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = screen;
            player.source = VideoSource.Url;
            player.url = url;
            player.prepareCompleted += prepared => prepared.Play();
            player.started += _ => movieStartedAt = Time.unscaledTime;
            player.Prepare();
        }

        /// <summary>粒を下に戻す。height は戻す高さ（範囲の高さに対する比）。始めは高さをばらして、一斉に昇らないようにする</summary>
        private void Respawn(Mote dot, float height)
        {
            Rect area = motes.rect;
            float size = Random.Range(SizeMin, SizeMax);

            dot.Rect.anchorMin = dot.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            dot.Rect.sizeDelta = new Vector2(size, size);
            dot.Rect.anchoredPosition = new Vector2(
                Mathf.Lerp(area.xMin, area.xMax, Random.Range(SpreadFrom, 1f)),
                Mathf.Lerp(area.yMin, area.yMax, height));
            dot.Rise = Random.Range(RiseMin, RiseMax);
            dot.Sway = Random.Range(4f, 14f);
            dot.Phase = Random.Range(0f, 6.28f);
        }
    }
}
