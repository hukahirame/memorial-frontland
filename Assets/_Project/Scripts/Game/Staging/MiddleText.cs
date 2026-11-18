using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 中くらいの文字。画面の上の帯に知らせを 1 件ずつ出す。続けて頼まれた知らせは順番に待たせ、前の知らせが消えてから出す。
    /// 後ろに知らせが待っているあいだは、出しておく長さを縮めて回す
    /// </summary>
    public sealed class MiddleText : MonoBehaviour
    {
        /// <summary>出るときに、透明から見えきって元の位置に寄りきるまでの秒数</summary>
        private const float FadeInSeconds = 0.5f;

        /// <summary>消えるときに、透明になりきるまでの秒数</summary>
        private const float FadeOutSeconds = 0.4f;

        /// <summary>後ろに知らせが待っているときに出しておく秒数。待っていなければ _holdSeconds 秒</summary>
        private const float HurriedSeconds = 1.5f;

        /// <summary>出るときに左から寄ってくる幅</summary>
        private const float SlideWidth = 60f;

        /// <summary>帯の左右の、文字の端から帯の端までの幅。帯は文字の長さに合わせて伸び縮みする</summary>
        private const float Padding = 160f;

        /// <summary>帯のいちばん短い幅</summary>
        private const float MinWidth = 640f;

        /// <summary>文字の縁取りの太さ。明るい空やもやの上でも字の形が残るように</summary>
        private const float OutlineWidth = 0.2f;

        private static readonly Color32 OutlineColor = new Color32(20, 12, 6, 255);

        [Tooltip("待っている知らせが無いときに出しておく秒数")]
        [SerializeField, FormerlySerializedAs("waittime")] private float _holdSeconds;

        [SerializeField, FormerlySerializedAs("txt")] private TextMeshProUGUI _text;

        private readonly Queue<string> _pending = new Queue<string>();
        private CanvasGroup _group;
        private RectTransform _band;
        private Vector2 _origin;
        private Coroutine _showing;
        private string _current;

        /// <summary>いちばん新しく頼まれた知らせ。出ている最中でも、順番待ちでも</summary>
        public string Latest { get; private set; }

        private void Awake()
        {
            _band = (RectTransform)transform;
            _origin = _band.anchoredPosition;

            //帯と字をまとめて薄くする。出ていないあいだも出ているあいだも、帯の下のボタンを押せるようにする
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _text.outlineWidth = OutlineWidth;
            _text.outlineColor = OutlineColor;
        }

        private void Start()
        {
            Show("管理地ーはじまりの地ー");
        }

        //止められたコルーチンは戻らない。次に頼まれたときに出し直す
        private void OnDisable()
        {
            _showing = null;
            _current = null;
        }

        /// <summary>知らせを出す。出ている知らせがあれば、消えてから出す。出ている最中か順番待ちの同じ知らせは重ねない</summary>
        public void Show(string message)
        {
            Latest = message;
            if (message == _current || _pending.Contains(message)) return;

            _pending.Enqueue(message);
            if (_showing == null && isActiveAndEnabled) _showing = StartCoroutine(ShowInTurn());
        }

        //経った時間で進める。時間止めのあいだも止めない
        private IEnumerator ShowInTurn()
        {
            while (_pending.Count > 0)
            {
                _current = _pending.Dequeue();
                _text.text = _current;
                Fit();

                for (float t = 0f; t < FadeInSeconds; t += Time.unscaledDeltaTime)
                {
                    float k = t / FadeInSeconds;
                    _band.anchoredPosition = _origin + Vector2.left * (SlideWidth * (1f - k));
                    _group.alpha = k;
                    yield return null;
                }

                _band.anchoredPosition = _origin;
                _group.alpha = 1f;

                for (float t = 0f; t < _holdSeconds; t += Time.unscaledDeltaTime)
                {
                    if (_pending.Count > 0 && t >= HurriedSeconds) break;
                    yield return null;
                }

                for (float t = 0f; t < FadeOutSeconds; t += Time.unscaledDeltaTime)
                {
                    _group.alpha = 1f - t / FadeOutSeconds;
                    yield return null;
                }

                _group.alpha = 0f;
            }

            _current = null;
            _showing = null;
        }

        /// <summary>帯の幅を文字の長さに合わせる。文字は帯いっぱいに広がっている</summary>
        private void Fit()
        {
            float width = Mathf.Max(MinWidth, _text.GetPreferredValues(_current, 10000f, 1000f).x + Padding * 2f);
            _band.sizeDelta = new Vector2(width, _band.sizeDelta.y);
        }
    }
}
