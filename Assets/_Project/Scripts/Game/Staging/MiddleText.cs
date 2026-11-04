using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 中くらいの文字。画面の上の帯に知らせを 1 件ずつ出す。続けて頼まれた知らせは順番に待たせ、前の知らせが消えてから出す。
/// 後ろに知らせが待っているあいだは、出しておく長さを縮めて回す
/// </summary>
public class MiddleText : MonoBehaviour
{
    /// <summary>出るときに、透明から見えきって元の位置に寄りきるまでの秒数</summary>
    private const float FadeInSeconds = 0.5f;

    /// <summary>消えるときに、透明になりきるまでの秒数</summary>
    private const float FadeOutSeconds = 0.4f;

    /// <summary>後ろに知らせが待っているときに出しておく秒数。待っていなければ waittime 秒</summary>
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
    public float waittime;

    [SerializeField] private TextMeshProUGUI txt;

    private readonly Queue<string> pending = new Queue<string>();
    private CanvasGroup group;
    private RectTransform band;
    private Vector2 origin;
    private Coroutine showing;
    private string current;

    /// <summary>いちばん新しく頼まれた知らせ。出ている最中でも、順番待ちでも</summary>
    public string Latest { get; private set; }

    void Awake()
    {
        band = (RectTransform)transform;
        origin = band.anchoredPosition;

        //帯と字をまとめて薄くする。出ていないあいだも出ているあいだも、帯の下のボタンを押せるようにする
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        txt.outlineWidth = OutlineWidth;
        txt.outlineColor = OutlineColor;
    }

    void Start()
    {
        Midtxt_Anim("管理地ーはじまりの地ー");
    }

    //止められたコルーチンは戻らない。次に頼まれたときに出し直す
    void OnDisable()
    {
        showing = null;
        current = null;
    }

    /// <summary>知らせを出す。出ている知らせがあれば、消えてから出す。出ている最中か順番待ちの同じ知らせは重ねない</summary>
    public void Midtxt_Anim(string s)
    {
        Latest = s;
        if (s == current || pending.Contains(s)) return;

        pending.Enqueue(s);
        if (showing == null && isActiveAndEnabled) showing = StartCoroutine(Show());
    }

    //経った時間で進める。時間止めのあいだも止めない
    private IEnumerator Show()
    {
        while (pending.Count > 0)
        {
            current = pending.Dequeue();
            txt.text = current;
            Fit();

            for (float t = 0f; t < FadeInSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / FadeInSeconds;
                band.anchoredPosition = origin + Vector2.left * (SlideWidth * (1f - k));
                group.alpha = k;
                yield return null;
            }

            band.anchoredPosition = origin;
            group.alpha = 1f;

            for (float t = 0f; t < waittime; t += Time.unscaledDeltaTime)
            {
                if (pending.Count > 0 && t >= HurriedSeconds) break;
                yield return null;
            }

            for (float t = 0f; t < FadeOutSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeOutSeconds;
                yield return null;
            }

            group.alpha = 0f;
        }

        current = null;
        showing = null;
    }

    /// <summary>帯の幅を文字の長さに合わせる。文字は帯いっぱいに広がっている</summary>
    private void Fit()
    {
        float width = Mathf.Max(MinWidth, txt.GetPreferredValues(current, 10000f, 1000f).x + Padding * 2f);
        band.sizeDelta = new Vector2(width, band.sizeDelta.y);
    }
}
