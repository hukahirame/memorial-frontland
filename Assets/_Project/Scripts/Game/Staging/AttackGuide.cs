using MemorialFloor.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 攻撃スティックで出せる行動と出し方を、スティックの左に小さく並べる。
    /// いまの行動は赤く、構えているあいだは離すと出る攻撃を金色にする
    /// </summary>
    public sealed class AttackGuide : MonoBehaviour
    {
        /// <summary>字の大きさ。参照解像度の画素</summary>
        public const float FontSize = 26f;

        /// <summary>スティックとのあいだ。参照解像度の画素</summary>
        public const float Gap = 16f;

        /// <summary>スティックの下端より下げる量。参照解像度の画素</summary>
        public const float Drop = 40f;

        /// <summary>いまの行動の色</summary>
        public static readonly Color DoingColor = new Color(1f, 0.32f, 0.32f);

        /// <summary>構えているあいだ、離すと出る攻撃の色</summary>
        public static readonly Color AimedColor = new Color(1f, 0.84f, 0.38f);

        private static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color Back = new Color(0f, 0f, 0f, 0.3f);
        private static readonly Vector2 Padding = new Vector2(12f, 8f);

        /// <summary>並べる順</summary>
        private static readonly AttackMove[] Moves =
        {
            AttackMove.Slash, AttackMove.Thrust, AttackMove.DiagonalThrust, AttackMove.Step, AttackMove.Roll,
            AttackMove.StanceWalk
        };

        private RectTransform _rect;
        private TextMeshProUGUI _text;
        private string _key;

        private bool _drawn;
        private AttackMove? _doing;
        private AttackMove? _aimed;

        /// <summary>stick の左に置く。key は攻撃のキーの名前</summary>
        public static AttackGuide Put(RectTransform stick, string key)
        {
            var back = new GameObject("AttackGuide", typeof(RectTransform), typeof(Image));
            back.layer = stick.gameObject.layer;

            var rect = (RectTransform)back.transform;
            rect.SetParent(stick, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.right;
            rect.anchoredPosition = new Vector2(-Gap, -Drop);

            var image = back.GetComponent<Image>();
            image.color = Back;
            image.raycastTarget = false;

            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.layer = stick.gameObject.layer;

            var textRect = (RectTransform)text.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Padding;
            textRect.offsetMax = -Padding;

            var label = text.GetComponent<TextMeshProUGUI>();
            label.fontSize = FontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            var guide = back.AddComponent<AttackGuide>();
            guide._rect = rect;
            guide._text = label;
            guide._key = key;
            guide.Show(null, null);
            return guide;
        }

        /// <summary>doing はいまの行動、aimed は構えている向きで離すと出る攻撃。どちらも無ければ null</summary>
        public void Show(AttackMove? doing, AttackMove? aimed)
        {
            if (_drawn && doing == _doing && aimed == _aimed) return;

            _drawn = true;
            _doing = doing;
            _aimed = aimed;

            string text = "";
            foreach (AttackMove move in Moves)
            {
                Color color = move == doing ? DoingColor : move == aimed ? AimedColor : IdleColor;
                if (text.Length > 0) text += "\n";
                text += Paint(color, Name(move) + "<pos=4.6em>" + HowTo(move));
            }

            _text.text = text;
            _rect.sizeDelta = _text.GetPreferredValues(text) + Padding * 2f;
        }

        private static string Name(AttackMove move)
        {
            switch (move)
            {
                case AttackMove.Slash: return "通常攻撃";
                case AttackMove.Thrust: return "突き";
                case AttackMove.DiagonalThrust: return "斜め突き";
                case AttackMove.Step: return "ステップ";
                case AttackMove.Roll: return "回避攻撃";
                default: return "構え歩き";
            }
        }

        //前と後は体の向きから見た向き。矢印キーでもスティックでも同じ
        private string HowTo(AttackMove move)
        {
            switch (move)
            {
                case AttackMove.Slash: return _key;
                case AttackMove.Thrust: return _key + " + 前矢印";
                case AttackMove.DiagonalThrust: return _key + " + 斜め前矢印";
                case AttackMove.Step: return _key + " + 後矢印";
                case AttackMove.Roll: return _key + " + 上下矢印";
                default: return _key + " hold";
            }
        }

        private static string Paint(Color color, string text)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";
        }
    }
}
