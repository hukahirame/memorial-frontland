using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 物の上に浮かべる、体力や耐久のゲージ。残りの割合だけ左から伸ばし、いつもカメラに正面を向ける。
    /// 縁取りした両端の丸い帯で、中身は上ほど明るく艶があり、4 等分の刻みが入る。減ったぶんは白く残り、少し待ってから縮む。
    /// 全体が少し透け、後ろの物が見える。物の子に置くので、物が隠れると一緒に隠れる
    /// </summary>
    public sealed class WorldGauge : MonoBehaviour
    {
        /// <summary>ゲージの 1 画素の場の大きさ。ゲージは画素で組み、これで場の大きさへ縮める</summary>
        public const float Pixel = 0.01f;

        /// <summary>見える絵のてっぺんから、ゲージまでのあいだ</summary>
        public const float Margin = 0.25f;

        /// <summary>ゲージ全体の不透明度。後ろに居る敵やプレイヤーが透けて見えるよう、少し下げる</summary>
        public const float Opacity = 0.8f;

        /// <summary>減ったぶんが白く残る秒数。過ぎると縮み始める</summary>
        public const float TrailHoldSeconds = 0.4f;

        /// <summary>白く残ったぶんが縮む速さ（満タンに対する割合 / 秒）</summary>
        public const float TrailSpeed = 1.2f;

        /// <summary>ふつうの幅と高さ（画素）</summary>
        public static readonly Vector2 Standard = new Vector2(100f, 12f);

        /// <summary>縁の太さ（画素）</summary>
        private const float Rim = 1.5f;

        /// <summary>刻みで等分する数</summary>
        private const int Segments = 4;

        /// <summary>刻みの太さ（画素）</summary>
        private const float TickWidth = 1f;

        /// <summary>
        /// 1 画素を描く絵の点の数。透けを全体でそろえるため、縁も中身も 1 枚の絵に描く
        /// （重ねた絵はそれぞれが透けるので、重なった所だけ透けなくなる）
        /// </summary>
        private const int Density = 4;

        /// <summary>描く順。場の絵より手前に出す</summary>
        private const int Order = 5000;

        private static readonly Color RimColor = new Color(0.05f, 0.04f, 0.03f);
        private static readonly Color EmptyColor = new Color(0.16f, 0.14f, 0.12f);
        private static readonly Color TrailColor = new Color(1f, 0.95f, 0.8f);

        private Texture2D _texture;
        private Color32[] _pixels;
        private Color _color;
        private float _ratio = 1f;
        private float _trail = 1f;

        /// <summary>白く残ったぶんが縮み始める時刻</summary>
        private float _shrinkAt;

        /// <summary>owner の見える絵のてっぺんの少し上に、ふつうの大きさで満タンで置く</summary>
        public static WorldGauge Put(Transform owner, Color color)
        {
            return Put(owner, color, Standard);
        }

        /// <summary>owner の見える絵のてっぺんの少し上に、size（画素）の大きさで満タンで置く</summary>
        public static WorldGauge Put(Transform owner, Color color, Vector2 size)
        {
            var root = new GameObject("Gauge", typeof(RectTransform), typeof(Canvas), typeof(RawImage));
            root.layer = owner.gameObject.layer;

            var rect = (RectTransform)root.transform;
            rect.SetParent(owner, false);
            rect.sizeDelta = size;
            Vector3 scale = owner.lossyScale;
            rect.localScale = new Vector3(Pixel / scale.x, Pixel / scale.y, Pixel / scale.z);
            rect.position = Above(owner);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = Order;

            var texture = new Texture2D(Mathf.RoundToInt(size.x * Density), Mathf.RoundToInt(size.y * Density), TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var image = root.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;

            var gauge = root.AddComponent<WorldGauge>();
            gauge._texture = texture;
            gauge._pixels = new Color32[texture.width * texture.height];
            gauge.Show(1f, color);
            return gauge;
        }

        /// <summary>残りの割合 ratio（0〜1）を color で見せる。減ったときは、減ったぶんを白く残す</summary>
        public void Show(float ratio, Color color)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio < _ratio) _shrinkAt = Time.time + TrailHoldSeconds;

            _ratio = ratio;
            _trail = Mathf.Max(_trail, ratio);
            _color = color;
            Paint();
        }

        private void LateUpdate()
        {
            Camera eye = Camera.main;
            if (eye != null) transform.rotation = eye.transform.rotation;

            if (_trail <= _ratio || Time.time < _shrinkAt) return;

            _trail = Mathf.MoveTowards(_trail, _ratio, TrailSpeed * Time.deltaTime);
            Paint();
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }

        //縁・空き・白く残ったぶん・中身と刻みを、1 枚の絵に描き直す。縁の外は透明にし、境目はなめらかにする
        private void Paint()
        {
            int width = _texture.width;
            int height = _texture.height;
            float rim = Rim * Density;
            float inside = width - 2f * rim;

            for (int y = 0; y < height; y++)
            {
                float py = y + 0.5f;
                float up = Mathf.Clamp01((py - rim) / (height - 2f * rim));

                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float along = (px - rim) / inside;

                    Color body = along < _ratio ? Shade(_color, up) : along < _trail ? TrailColor : EmptyColor;
                    if (OnTick(along, inside)) body = Color.Lerp(body, Color.black, 0.4f);

                    Color color = Color.Lerp(RimColor, body, Mathf.Clamp01(0.5f - Pill(px, py, width, height, rim)));
                    color.a = Mathf.Clamp01(0.5f - Pill(px, py, width, height, 0f)) * Opacity;
                    _pixels[y * width + x] = color;
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        //中身の色。下ほど暗く、上寄りに白い艶の帯を入れる。up は中身の下端が 0、上端が 1
        private static Color Shade(Color color, float up)
        {
            Color shaded = color * Mathf.Lerp(0.7f, 1.15f, up);
            shaded = Color.Lerp(shaded, Color.white, 0.35f * Mathf.Clamp01(1f - Mathf.Abs(up - 0.72f) / 0.18f));
            shaded.a = 1f;
            return shaded;
        }

        //刻みの上か。along は中身の左端が 0、右端が 1
        private static bool OnTick(float along, float inside)
        {
            for (int i = 1; i < Segments; i++)
            {
                if (Mathf.Abs(along - (float)i / Segments) * inside < TickWidth * Density / 2f) return true;
            }

            return false;
        }

        //両端が半円の横長の帯を inset だけ内へ縮めた形の、縁までの距離（絵の点）。内側なら負
        private static float Pill(float x, float y, float width, float height, float inset)
        {
            float straight = width / 2f - height / 2f;
            float dx = Mathf.Max(Mathf.Abs(x - width / 2f) - straight, 0f);
            float dy = y - height / 2f;
            return Mathf.Sqrt(dx * dx + dy * dy) - (height / 2f - inset);
        }

        //見える絵をすべて囲む箱の、てっぺんの真ん中の上。絵が無ければ物の位置の上
        private static Vector3 Above(Transform owner)
        {
            Renderer[] parts = owner.GetComponentsInChildren<Renderer>();
            if (parts.Length == 0) return owner.position + Vector3.up * Margin;

            Bounds bounds = parts[0].bounds;
            foreach (Renderer part in parts) bounds.Encapsulate(part.bounds);
            return new Vector3(bounds.center.x, bounds.max.y + Margin, bounds.center.z);
        }
    }
}
