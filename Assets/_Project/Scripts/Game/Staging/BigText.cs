using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 大きい文字。画面の真ん中の帯に見出しを、リボンに一言を出し、透明から浮かび上がらせて、しばらく置いてから消す。
    /// 帯・リボン・字は、はじめから透明にしてある
    /// </summary>
    public sealed class BigText : MonoBehaviour
    {
        [Tooltip("見えきってから消え始めるまでの秒数")]
        [SerializeField, FormerlySerializedAs("waittime")] private float _holdSeconds;

        [Tooltip("浮かび上がるのと消えるのを、それぞれ何段で進めるか。1 段は 1/段数 秒")]
        [SerializeField, FormerlySerializedAs("flame")] private int _fadeSteps;

        [Tooltip("真ん中の帯")]
        [SerializeField, FormerlySerializedAs("image1")] private Image _band;

        [Tooltip("リボン")]
        [SerializeField, FormerlySerializedAs("image2")] private Image _ribbon;

        [Tooltip("真ん中の帯の見出し")]
        [SerializeField, FormerlySerializedAs("txt1")] private TextMeshProUGUI _headline;

        [Tooltip("リボンの一言")]
        [SerializeField, FormerlySerializedAs("txt2")] private TextMeshProUGUI _ribbonText;

        private Coroutine _shown;

        /// <summary>見出しとリボンの一言を出す。出ている最中なら止めて、透明から出し直す</summary>
        public void Show(string headline, string ribbonText)
        {
            //開いている持ち物や地図より手前に出す。Canvas の子は後に並ぶものほど手前に描かれる
            transform.SetAsLastSibling();
            transform.localScale = Vector3.one;
            _headline.text = headline;
            _ribbonText.text = ribbonText;

            //続けて出すと、前のものの終わりで後のものまで消える。前のものは止めて、透明から出し直す
            if (_shown != null) StopCoroutine(_shown);
            foreach (Graphic part in Parts())
            {
                Color color = part.color;
                color.a = 0f;
                part.color = color;
            }

            _shown = StartCoroutine(FadeInAndOut());
        }

        private IEnumerator FadeInAndOut()
        {
            for (int i = 0; i < _fadeSteps; i++)
            {
                AddAlpha(1f / _fadeSteps);
                yield return new WaitForSeconds(1f / _fadeSteps);
            }

            yield return new WaitForSeconds(_holdSeconds);

            for (int i = 0; i < _fadeSteps; i++)
            {
                AddAlpha(-1f / _fadeSteps);
                yield return new WaitForSeconds(1f / _fadeSteps);
            }

            transform.localScale = new Vector3(0f, 1f, 1f);
        }

        private void AddAlpha(float amount)
        {
            foreach (Graphic part in Parts()) part.color += new Color(0f, 0f, 0f, amount);
        }

        private Graphic[] Parts()
        {
            return new Graphic[] { _band, _ribbon, _headline, _ribbonText };
        }
    }
}
