using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 敵がプレイヤーに気づいたときに頭上へ出す「！」。名札と体力と同じ、敵の頭上の表示の Canvas に置き、
    /// 弾むように出て、少し浮きながら薄れて消える
    /// </summary>
    public sealed class AlertMark : MonoBehaviour
    {
        /// <summary>出してから、もう一度出せるまでの秒数。見つけたり見失ったりを繰り返す境目で出し続けないように</summary>
        public const float CooldownSeconds = 3f;

        /// <summary>弾み終えるまでの秒数</summary>
        public const float PopSeconds = 0.18f;

        /// <summary>弾み終えてから薄れ始めるまでの秒数</summary>
        private const float HoldSeconds = 0.55f;

        private const float FadeSeconds = 0.25f;

        /// <summary>弾むときにいちばん大きくなる倍率。出始めから PopSeconds の 6 割でここまで膨らみ、残りで 1 に戻る</summary>
        private const float PopScale = 1.35f;

        /// <summary>Canvas の中の高さ。名札（220）の上</summary>
        private const float Height = 380f;

        /// <summary>消えるまでに浮き上がる高さ（Canvas の中の長さ）</summary>
        private const float Rise = 40f;

        private const int FontSize = 240;

        private static readonly Color Fill = new Color(1f, 0.82f, 0.2f);

        private static readonly Color Edge = new Color(0.25f, 0.1f, 0f);

        private RectTransform rect;
        private Text text;
        private float age;

        /// <summary>canvas は敵の頭上の表示の Canvas。無ければ出さない。書体は中の文字（名札）から借りる</summary>
        public static void Show(Transform canvas)
        {
            if (canvas == null) return;

            Text label = canvas.GetComponentInChildren<Text>(true);
            GameObject mark = new GameObject("AlertMark", typeof(RectTransform));
            mark.layer = canvas.gameObject.layer;
            mark.transform.SetParent(canvas, false);

            Text text = mark.AddComponent<Text>();
            text.font = label != null ? label.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = FontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Fill;
            text.text = "!";

            Outline outline = mark.AddComponent<Outline>();
            outline.effectColor = Edge;
            outline.effectDistance = new Vector2(12f, -12f);

            AlertMark alert = mark.AddComponent<AlertMark>();
            alert.text = text;
            alert.rect = (RectTransform)mark.transform;
            alert.rect.anchoredPosition = new Vector2(0f, Height);
            alert.rect.localScale = Vector3.zero;
        }

        private void Update()
        {
            age += Time.deltaTime;

            float swell = PopSeconds * 0.6f;
            float scale = age < swell ? Mathf.Lerp(0f, PopScale, age / swell)
                : age < PopSeconds ? Mathf.Lerp(PopScale, 1f, (age - swell) / (PopSeconds - swell))
                : 1f;
            rect.localScale = Vector3.one * scale;

            float life = PopSeconds + HoldSeconds + FadeSeconds;
            rect.anchoredPosition = new Vector2(0f, Height + Rise * Mathf.Clamp01(age / life));

            float fadeFrom = PopSeconds + HoldSeconds;
            if (age > fadeFrom)
            {
                Color color = Fill;
                color.a = 1f - Mathf.Clamp01((age - fadeFrom) / FadeSeconds);
                text.color = color;
            }

            if (age >= life) Destroy(gameObject);
        }
    }
}
