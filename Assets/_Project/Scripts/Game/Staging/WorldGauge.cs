using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 物の上に浮かべる、体力や耐久のゲージ。残りの割合だけ左から伸ばし、いつもカメラに正面を向ける。
    /// 物の子に置くので、物が隠れると一緒に隠れる。大きさは物の大きさによらずそろえる
    /// </summary>
    public sealed class WorldGauge : MonoBehaviour
    {
        /// <summary>ゲージの 1 画素の場の大きさ。ゲージは画素で組み、これで場の大きさへ縮める</summary>
        public const float Pixel = 0.01f;

        /// <summary>見える絵のてっぺんから、ゲージまでのあいだ</summary>
        public const float Margin = 0.25f;

        /// <summary>幅と高さ（画素）</summary>
        private static readonly Vector2 Size = new Vector2(100f, 12f);

        /// <summary>描く順。場の絵より手前に出す</summary>
        private const int Order = 5000;

        private static readonly Color Back = new Color(0f, 0f, 0f, 0.6f);

        private RectTransform _fill;
        private Image _fillImage;

        /// <summary>owner の見える絵のてっぺんの少し上に、満タンで置く</summary>
        public static WorldGauge Put(Transform owner, Color color)
        {
            var root = new GameObject("Gauge", typeof(RectTransform), typeof(Canvas));
            root.layer = owner.gameObject.layer;

            var rect = (RectTransform)root.transform;
            rect.SetParent(owner, false);
            rect.sizeDelta = Size;
            Vector3 scale = owner.lossyScale;
            rect.localScale = new Vector3(Pixel / scale.x, Pixel / scale.y, Pixel / scale.z);
            rect.position = Above(owner);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = Order;

            Stretch(Bar("Back", rect, Back));
            Image fill = Bar("Fill", rect, color);
            Stretch(fill);

            var gauge = root.AddComponent<WorldGauge>();
            gauge._fill = fill.rectTransform;
            gauge._fillImage = fill;
            return gauge;
        }

        /// <summary>残りの割合 ratio（0〜1）を color で見せる</summary>
        public void Show(float ratio, Color color)
        {
            _fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            _fillImage.color = color;
        }

        private void LateUpdate()
        {
            Camera eye = Camera.main;
            if (eye != null) transform.rotation = eye.transform.rotation;
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

        private static Image Bar(string name, Transform parent, Color color)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.layer = parent.gameObject.layer;
            bar.transform.SetParent(parent, false);

            var image = bar.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(Graphic graphic)
        {
            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
