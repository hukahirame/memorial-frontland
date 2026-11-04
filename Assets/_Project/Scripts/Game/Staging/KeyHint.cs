using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>操作の隅に、押せるキーを書く。隅は選べ、選ばなければ右下。指の当たりは取らない</summary>
    public static class KeyHint
    {
        /// <summary>キーの字の大きさ。参照解像度の画素</summary>
        public const float FontSize = 34f;

        /// <summary>字のまわりの余白。参照解像度の画素</summary>
        private static readonly Vector2 Padding = new Vector2(12f, 4f);

        /// <summary>下敷きの色。明るい絵の上でも字が読めるよう、暗く透かす</summary>
        private static readonly Color Back = new Color(0f, 0f, 0f, 0.55f);

        public static void Put(RectTransform target, string keys)
        {
            Put(target, keys, Vector2.right);
        }

        /// <summary>corner は target の隅。(0, 0) が左下、(1, 0) が右下</summary>
        public static void Put(RectTransform target, string keys, Vector2 corner)
        {
            Badge("KeyHint", target, keys, corner, FontSize);
        }

        /// <summary>target の隅に、暗い下敷きに載せた短い字を置く。キーのほかの札（持ち物の装備の印）にも使う</summary>
        public static RectTransform Badge(string name, RectTransform target, string words, Vector2 corner, float fontSize)
        {
            var back = new GameObject(name, typeof(RectTransform), typeof(Image));
            back.layer = target.gameObject.layer;

            var rect = (RectTransform)back.transform;
            rect.SetParent(target, false);
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
            rect.anchoredPosition = Vector2.zero;

            var image = back.GetComponent<Image>();
            image.color = Back;
            image.raycastTarget = false;

            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.layer = target.gameObject.layer;

            var textRect = (RectTransform)text.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var label = text.GetComponent<TextMeshProUGUI>();
            label.text = words;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            rect.sizeDelta = label.GetPreferredValues(words) + Padding * 2f;
            return rect;
        }
    }
}
