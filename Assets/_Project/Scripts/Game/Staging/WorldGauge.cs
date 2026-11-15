using System.Collections.Generic;
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

        private Layers _layers;

        /// <summary>中身を _filledColor で塗った絵</summary>
        private Color32[] _filled;
        private Color _filledColor;

        /// <summary>前に描いた、中身と白く残ったぶんの列の数。どちらも同じなら絵は変わらない</summary>
        private int _paintedFill = -1;
        private int _paintedTrail = -1;

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

        //減るたび、戻るたびに呼ばれる（柵は戻るあいだ毎コマ）。画素ごとの計算はせず、塗っておいた絵から列を写すだけにする
        private void Paint()
        {
            if (_layers == null) _layers = Layers.Of(_texture.width, _texture.height);
            if (_filled == null || !_filledColor.Equals(_color))
            {
                _filled = _layers.Filled(_color);
                _filledColor = _color;
                _paintedFill = -1;
            }

            int fill = _layers.ColumnsBelow(_ratio);
            int trail = _layers.ColumnsBelow(_trail);
            if (fill == _paintedFill && trail == _paintedTrail) return;

            int width = _texture.width;
            for (int row = 0; row < _pixels.Length; row += width)
            {
                System.Array.Copy(_filled, row, _pixels, row, fill);
                System.Array.Copy(_layers.Trailing, row + fill, _pixels, row + fill, trail - fill);
                System.Array.Copy(_layers.Empty, row + trail, _pixels, row + trail, width - trail);
            }

            _paintedFill = fill;
            _paintedTrail = trail;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        /// <summary>
        /// 中身・白く残ったぶん・空きの 3 通りに塗った絵。縁・透け・刻み・艶は画素ごとに決まっていて、残りの割合では変わらず、
        /// 変わるのは列ごとにどれを塗るかだけ。絵は大きさと中身の色で決まるので、同じもののゲージどうしで使い回す。書き換えない
        /// </summary>
        private sealed class Layers
        {
            private static readonly Dictionary<Vector2Int, Layers> Made = new Dictionary<Vector2Int, Layers>();

            public readonly Color32[] Trailing;
            public readonly Color32[] Empty;

            private readonly int _width;
            private readonly int _height;

            /// <summary>列ごとの、中身の左端を 0、右端を 1 とした位置。左から右へ増える</summary>
            private readonly float[] _along;

            /// <summary>列ごとの、刻みの上か</summary>
            private readonly bool[] _tick;

            /// <summary>行ごとの、中身の高さ。下端が 0、上端が 1</summary>
            private readonly float[] _up;

            /// <summary>画素ごとの、中身を縁の色と混ぜる割合（縁の内側で 1）と、不透明度（帯の外で 0）。中身の色によらない</summary>
            private readonly float[] _edge;
            private readonly float[] _alpha;

            private readonly Dictionary<Color, Color32[]> _filled = new Dictionary<Color, Color32[]>();

            public static Layers Of(int width, int height)
            {
                var size = new Vector2Int(width, height);
                if (!Made.TryGetValue(size, out Layers layers))
                {
                    layers = new Layers(width, height);
                    Made[size] = layers;
                }

                return layers;
            }

            //縁・透け・刻み・艶の形は色によらないので、画素ごとの計算はここで 1 度だけにする。縁の外は透明にし、境目はなめらかにする
            private Layers(int width, int height)
            {
                _width = width;
                _height = height;

                float rim = Rim * Density;
                float inside = width - 2f * rim;
                _along = new float[width];
                _tick = new bool[width];
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    _along[x] = (px - rim) / inside;
                    _tick[x] = OnTick(_along[x], inside);
                }

                _up = new float[height];
                _edge = new float[width * height];
                _alpha = new float[width * height];
                for (int y = 0; y < height; y++)
                {
                    float py = y + 0.5f;
                    _up[y] = Mathf.Clamp01((py - rim) / (height - 2f * rim));

                    for (int x = 0; x < width; x++)
                    {
                        float px = x + 0.5f;
                        _edge[y * width + x] = Mathf.Clamp01(0.5f - Pill(px, py, width, height, rim));
                        _alpha[y * width + x] = Mathf.Clamp01(0.5f - Pill(px, py, width, height, 0f)) * Opacity;
                    }
                }

                Trailing = Paint(up => TrailColor);
                Empty = Paint(up => EmptyColor);
            }

            /// <summary>中身を color で塗った絵</summary>
            public Color32[] Filled(Color color)
            {
                if (!_filled.TryGetValue(color, out Color32[] layer))
                {
                    layer = Paint(up => Shade(color, up));
                    _filled[color] = layer;
                }

                return layer;
            }

            /// <summary>位置が value に届かない列の数。白く残ったぶんは中身より短くならないので、中身の数以上になる</summary>
            public int ColumnsBelow(float value)
            {
                int count = 0;
                while (count < _along.Length && _along[count] < value) count++;
                return count;
            }

            //中身を body で塗り、刻みを暗くし、縁と混ぜて透けを入れる。body は中身の高さから色を返す
            private Color32[] Paint(System.Func<float, Color> body)
            {
                var layer = new Color32[_width * _height];
                for (int y = 0; y < _height; y++)
                {
                    Color fill = body(_up[y]);
                    Color ticked = Color.Lerp(fill, Color.black, 0.4f);

                    for (int x = 0; x < _width; x++)
                    {
                        int i = y * _width + x;
                        Color color = Color.Lerp(RimColor, _tick[x] ? ticked : fill, _edge[i]);
                        color.a = _alpha[i];
                        layer[i] = color;
                    }
                }

                return layer;
            }
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
