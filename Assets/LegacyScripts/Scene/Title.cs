using System;
using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル画面。最初は題字と「PRESS ANY BUTTON」だけを出し、キーかボタンか画面のどこかを押すとデータの一覧を開く。
/// 一覧は右に暗い帯を敷き、保存の枠を名前・場所・書いた日時と上から並べる。保存のある枠は押すと続きから始まり、
/// 空いた枠は名前を決めてからはじめから始まる。保存のある枠は「編集」で名前を変えたり消したりできる。
/// キーでも選べ（上下と決定、Esc で一つ前へ）、一覧を開いたときはいちばん新しく書いた枠を選んでおく。
/// 開くたびに遊びの状態を起動した直後と同じに戻す（GameSession）。設定の「タイトルに戻る」で来たときのため
/// </summary>
public class Title : MonoBehaviour
{
    /// <summary>開いてから画面が浮かび上がりきるまでの秒数</summary>
    private const float AppearSeconds = 1.2f;

    /// <summary>題字が上下に揺れる幅。参照解像度の画素</summary>
    private const float FloatHeight = 8f;

    /// <summary>題字が 1 往復する秒数</summary>
    private const float FloatSeconds = 4f;

    /// <summary>最初の画面の誘いの字</summary>
    private const string Prompt = "PRESS ANY BUTTON";

    /// <summary>誘いの字の大きさ。参照解像度の画素</summary>
    private const float PromptFontSize = 46f;

    /// <summary>誘いの字の字のあいだ</summary>
    private const float PromptSpacing = 12f;

    /// <summary>誘いの字の、画面の下端からの高さ</summary>
    private const float PromptHeight = 150f;

    /// <summary>誘いの字が薄くなって戻る 1 往復の秒数</summary>
    private const float PromptPulseSeconds = 2.4f;

    /// <summary>誘いの字の、いちばん薄いときの濃さ</summary>
    private const float PromptMinAlpha = 0.3f;

    /// <summary>一覧と名前の画面で右に敷く帯の幅。左上の題字にかからない幅</summary>
    private const float BandWidth = 820f;

    /// <summary>帯の濃さ。明るい背景の上でも字が読める濃さ</summary>
    private static readonly Color BandColor = new Color(0f, 0f, 0f, 0.55f);

    /// <summary>帯の中の左右と下の余白</summary>
    private const float BandPadding = 64f;

    /// <summary>帯の上端から見出しまで</summary>
    private const float HeadingTop = 80f;

    /// <summary>見出しの字の大きさ</summary>
    private const float HeadingFontSize = 56f;

    /// <summary>見出しの段の高さ</summary>
    private const float HeadingHeight = 80f;

    /// <summary>見出しの下の金の線から、最初の段まで</summary>
    private const float HeadingGap = 36f;

    /// <summary>段の高さ</summary>
    private const float RowHeight = 118f;

    /// <summary>段のあいだ</summary>
    private const float RowGap = 12f;

    /// <summary>枠の名前と決めるボタンの字の大きさ。場所と日時はこれに対する割合で小さくする</summary>
    private const float SlotFontSize = 54f;

    /// <summary>編集・消す・もどるの字の大きさ</summary>
    private const float SubFontSize = 38f;

    /// <summary>編集のボタンの幅。枠の段はこの分だけ右を空ける</summary>
    private const float EditWidth = 150f;

    /// <summary>もどるのボタンの大きさ</summary>
    private static readonly Vector2 BackSize = new Vector2(300f, 80f);

    /// <summary>指が乗った段と選んでいる段の字の色、見出しの線の色。題字の「ほのぼの」と同じ金</summary>
    private static readonly Color Gold = new Color32(0xFF, 0xD2, 0x7A, 0xFF);

    /// <summary>消すボタンを 1 回押したときの文字。2 回目で消す</summary>
    private const string ConfirmText = "もう一度で消す";

    /// <summary>枠の名前の長さの上限（字数）。枠の段に収まる長さ</summary>
    private const int NameLimit = 12;

    /// <summary>名前の欄の下敷き。帯より暗く敷き、打つ所が分かるようにする</summary>
    private static readonly Color FieldBack = new Color(0f, 0f, 0f, 0.45f);

    [Tooltip("画面全体。開いたときに透明から浮かび上がらせる")]
    [SerializeField] private CanvasGroup screen;

    [Tooltip("題字。ゆっくり上下に揺らす")]
    [SerializeField] private RectTransform logo;

    [Tooltip("字のボタンの型の文字。一覧の枠の段と名前の画面の決めるボタンは、このボタンを写して作る。型そのものは隠す")]
    [SerializeField] private TextMeshProUGUI startLabel;

    [Tooltip("小さい字のボタンの型。編集・消す・もどるは、このボタンを写して作る。型そのものは隠す")]
    [SerializeField] private GameObject freshButton;

    /// <summary>一覧と名前の画面に並べた物。画面を替えるときに消す</summary>
    private readonly List<GameObject> made = new List<GameObject>();

    /// <summary>消すボタンを 1 回押して確かめを出したか。保存を消すと取り返せないので、2 回目で消す</summary>
    private bool confirming;

    private Vector2 logoHome;

    /// <summary>最初の画面の誘いの字</summary>
    private TextMeshProUGUI prompt;

    /// <summary>Esc で戻る先。最初の画面では無い</summary>
    private UnityAction back;

    /// <summary>画面を替えたフレーム。替えるきっかけになった押下で、同じフレームのうちに次の画面まで進ませない</summary>
    private int shownFrame;

    private GameObject StartButton
    {
        get { return startLabel.GetComponentInParent<Button>(true).gameObject; }
    }

    private bool OnFront
    {
        get { return prompt != null && prompt.gameObject.activeSelf; }
    }

    //前に遊んだ状態を捨て、起動した直後と同じにする。設定からタイトルに戻ったあとに始め直すため
    private void Awake()
    {
        GameSession.Reset();
    }

    private void Start()
    {
        StartButton.SetActive(false);
        freshButton.SetActive(false);
        prompt = PromptText();
        ShowFront();
        PressFeedback.AttachAll(screen.transform);

        logoHome = logo.anchoredPosition;
        StartCoroutine(Appear());
    }

    private void Update()
    {
        float phase = Time.unscaledTime / FloatSeconds * 2f * Mathf.PI;
        logo.anchoredPosition = logoHome + Vector2.up * (Mathf.Sin(phase) * FloatHeight);

        if (OnFront)
        {
            float pulse = (Mathf.Cos(Time.unscaledTime / PromptPulseSeconds * 2f * Mathf.PI) + 1f) / 2f;
            prompt.alpha = Mathf.Lerp(PromptMinAlpha, 1f, pulse);
        }

        if (Time.frameCount <= shownFrame) return;

        //浮かび上がりきるまでは受けない。開いた瞬間の押し間違いで進まないように
        if (OnFront)
        {
            if (screen.alpha >= 1f && Input.anyKeyDown) ShowPicker();
        }
        else if (back != null && Input.GetKeyDown(KeyCode.Escape))
        {
            back();
        }
    }

    private IEnumerator Appear()
    {
        for (float t = 0f; t < AppearSeconds; t += Time.unscaledDeltaTime)
        {
            screen.alpha = Mathf.SmoothStep(0f, 1f, t / AppearSeconds);
            yield return null;
        }

        screen.alpha = 1f;
    }

    /// <summary>name ははじめから始めるときに枠へ付ける名前。続きからなら保存の名前のままで、使われない</summary>
    private static void Play(int slot, string name)
    {
        SaveSystem.Slot = slot;
        SaveSystem.PendingName = name;
        SceneManager.LoadScene(AreaTravel.CoreScene);
    }

    /// <summary>最初の画面。題字と誘いの字だけを出す</summary>
    private void ShowFront()
    {
        Clear();
        prompt.gameObject.SetActive(true);
        back = null;
    }

    /// <summary>
    /// データの一覧。右の帯に見出しと保存の枠を上から並べる。保存のある枠は押すと続きから始まり、右に「編集」を付ける。
    /// 空いた枠は押すと名前の画面へ。いちばん新しく書いた枠を選んでおき、そのまま決めれば続きから始まる
    /// </summary>
    private void ShowPicker()
    {
        Clear();
        RectTransform band = Band("データをえらぶ");

        int latest = LatestSlot();
        GameObject focus = null;

        for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
        {
            int chosen = slot;
            float top = RowTop(slot - 1);

            Button row = Copy(StartButton, band, SlotText(slot), SlotFontSize, TextAlignmentOptions.MidlineLeft, () => PressSlot(chosen));
            Place(row, top, RowHeight, 0f, EditWidth);
            if (focus == null || slot == latest) focus = row.gameObject;

            if (!SaveSystem.HasSaveIn(slot)) continue;

            Button edit = Copy(freshButton, band, "編集", SubFontSize, TextAlignmentOptions.MidlineRight, () => ShowNamer(chosen));
            var rect = (RectTransform)edit.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(EditWidth, RowHeight);
            rect.anchoredPosition = new Vector2(-BandPadding, -top);
        }

        BackButton(band, ShowFront);
        back = ShowFront;
        StartCoroutine(SelectSoon(focus));
    }

    /// <summary>枠の段を押した。保存があれば続きから、無ければ名前を決めてからはじめる</summary>
    private void PressSlot(int slot)
    {
        if (SaveSystem.HasSaveIn(slot)) Play(slot, "");
        else ShowNamer(slot);
    }

    /// <summary>
    /// 名前の画面。空いた枠は名前を決めて「はじめる」で始め、保存のある枠は名前を書き換えて「決める」か、2 回押して「消す」。
    /// もどると一覧へ。名前の欄で決定のキーを押しても、はじめる・決めると同じ
    /// </summary>
    private void ShowNamer(int slot)
    {
        Clear();

        bool saved = SaveSystem.HasSaveIn(slot);
        SaveData data = SaveSystem.Peek(slot);
        RectTransform band = Band(saved ? "データを編集" : "名前をつける");

        TMP_InputField field = NameField(band, data != null ? data.saveName : "", DefaultName(slot));
        Place(field, RowTop(0), RowHeight);

        UnityAction confirm = () =>
        {
            string name = field.text.Trim();
            if (!saved)
            {
                Play(slot, name);
                return;
            }

            SaveSystem.Rename(slot, name);
            ShowPicker();
        };
        field.onSubmit.AddListener(_ => confirm());

        Button ok = Copy(StartButton, band, saved ? "決める" : "はじめる", SlotFontSize, TextAlignmentOptions.MidlineRight, confirm);
        Place(ok, RowTop(1), RowHeight);

        if (saved)
        {
            TextMeshProUGUI eraseLabel = null;
            Button erase = Copy(freshButton, band, "消す", SubFontSize, TextAlignmentOptions.MidlineRight, () => Erase(slot, eraseLabel));
            Place(erase, RowTop(2), RowHeight);
            eraseLabel = erase.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        BackButton(band, ShowPicker);
        back = ShowPicker;
        StartCoroutine(ActivateSoon(field));
    }

    /// <summary>1 回目は確かめを出し、2 回目でその枠の保存を消して一覧へ戻る</summary>
    private void Erase(int slot, TextMeshProUGUI label)
    {
        if (!confirming)
        {
            confirming = true;
            label.text = ConfirmText;
            return;
        }

        SaveSystem.Erase(slot);
        ShowPicker();
    }

    /// <summary>画面を替える前に、前の画面に並べた物を消す。替えたフレームの押下は次の画面で数えない</summary>
    private void Clear()
    {
        foreach (GameObject thing in made) Destroy(thing);
        made.Clear();
        confirming = false;

        prompt.gameObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        shownFrame = Time.frameCount;
    }

    //選ぶのは次のフレーム。選んだフレームのうちに、いま押した決定のキーで押されないように
    private static IEnumerator SelectSoon(GameObject target)
    {
        yield return null;
        if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }

    private static IEnumerator ActivateSoon(TMP_InputField field)
    {
        yield return null;
        if (field == null) yield break;

        field.Select();
        field.ActivateInputField();
    }

    /// <summary>最初の画面の誘いの字。画面の下の真ん中に置く</summary>
    private TextMeshProUGUI PromptText()
    {
        TextMeshProUGUI text = Text(StartButton.transform.parent, Prompt, PromptFontSize, TextAlignmentOptions.Center);
        text.characterSpacing = PromptSpacing;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(1200f, 80f);
        rect.anchoredPosition = new Vector2(0f, PromptHeight);
        return text;
    }

    /// <summary>右に画面の上から下まで敷く暗い帯と、その上の見出しと金の線</summary>
    private RectTransform Band(string heading)
    {
        var band = new GameObject("Band", typeof(RectTransform), typeof(Image));
        band.layer = StartButton.layer;
        made.Add(band);

        var rect = (RectTransform)band.transform;
        rect.SetParent(StartButton.transform.parent, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(BandWidth, 0f);
        rect.anchoredPosition = Vector2.zero;
        band.GetComponent<Image>().color = BandColor;

        TextMeshProUGUI title = Text(rect, heading, HeadingFontSize, TextAlignmentOptions.BottomLeft);
        Place(title, HeadingTop, HeadingHeight);

        var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        line.layer = band.layer;
        line.transform.SetParent(rect, false);
        Image image = line.GetComponent<Image>();
        image.color = Gold;
        image.raycastTarget = false;
        Place(image, HeadingTop + HeadingHeight + 8f, 3f);

        return rect;
    }

    /// <summary>帯の右下に「もどる」を置く</summary>
    private void BackButton(RectTransform band, UnityAction onClick)
    {
        Button button = Copy(freshButton, band, "もどる", SubFontSize, TextAlignmentOptions.MidlineRight, onClick);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = BackSize;
        rect.anchoredPosition = new Vector2(-BandPadding, BandPadding);
    }

    /// <summary>段の上端の、帯の上端からの深さ。0 が最初の段</summary>
    private static float RowTop(int index)
    {
        return HeadingTop + HeadingHeight + HeadingGap + index * (RowHeight + RowGap);
    }

    /// <summary>帯の上端から top の深さに、高さ height で置く。左右は帯の余白から left・right だけ内へ寄せる</summary>
    private static void Place(Component part, float top, float height, float left = 0f, float right = 0f)
    {
        var rect = (RectTransform)part.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(BandPadding + left, -top - height);
        rect.offsetMax = new Vector2(-(BandPadding + right), -top);
    }

    /// <summary>
    /// 型のボタンを parent に写し、文字と押したときの動きを替える。型がシーンでつないだ呼び先は外す。
    /// 板は透けていて色を変えても見えないので、指が乗ったときと選んでいるときは字を金にする
    /// </summary>
    private Button Copy(GameObject model, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, UnityAction onClick)
    {
        GameObject copy = Instantiate(model, parent);
        copy.name = model.name;
        copy.SetActive(true);
        made.Add(copy);

        //金の下線は題字の飾り。並べたボタンには付けない
        Transform underline = copy.transform.Find("Underline");
        if (underline != null) underline.gameObject.SetActive(false);

        //名前が長いと段に収まらない。決めた大きさを上限に、収まるまで縮める
        TextMeshProUGUI label = copy.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = text;
        label.alignment = alignment;
        label.fontSize = fontSize;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = fontSize * 0.5f;
        label.enableAutoSizing = true;

        Button button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = label;

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = Gold;
        colors.selectedColor = Gold;
        colors.pressedColor = new Color(Gold.r * 0.8f, Gold.g * 0.8f, Gold.b * 0.8f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return button;
    }

    /// <summary>型のボタンの字と同じ書体の字。指は取らない</summary>
    private TextMeshProUGUI Text(Transform parent, string words, float size, TextAlignmentOptions alignment)
    {
        var holder = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        holder.layer = StartButton.layer;
        holder.transform.SetParent(parent, false);

        TextMeshProUGUI text = holder.GetComponent<TextMeshProUGUI>();
        text.font = startLabel.font;
        text.fontSharedMaterial = startLabel.fontSharedMaterial;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.text = words;
        return text;
    }

    /// <summary>名前の欄。段と同じ書体と大きさで左に寄せる。空のときは枠の決まりの名前を薄く出す</summary>
    private TMP_InputField NameField(RectTransform band, string text, string placeholder)
    {
        GameObject root = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        root.name = "NameField";
        root.transform.SetParent(band, false);
        made.Add(root);

        //UI だけを写すカメラで描くこともあるので、子も帯と同じ層に置く
        foreach (Transform part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = band.gameObject.layer;

        root.GetComponent<Image>().color = FieldBack;

        TMP_InputField field = root.GetComponent<TMP_InputField>();
        field.characterLimit = NameLimit;
        field.lineType = TMP_InputField.LineType.SingleLine;

        foreach (TextMeshProUGUI shown in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            shown.font = startLabel.font;
            shown.fontSize = SlotFontSize;
            shown.alignment = TextAlignmentOptions.MidlineLeft;
            shown.color = Color.white;
        }

        var hint = (TextMeshProUGUI)field.placeholder;
        hint.text = placeholder;
        hint.color = new Color(1f, 1f, 1f, 0.45f);

        field.text = text;
        return field;
    }

    /// <summary>枠の段の字。1 行めに名前と場所、2 行めに書いた日時。空いた枠は名前と「空き」</summary>
    private static string SlotText(int slot)
    {
        SaveData data = SaveSystem.Peek(slot);
        DateTime? at = SaveSystem.SavedAt(slot);

        string name = data != null && !string.IsNullOrEmpty(data.saveName) ? data.saveName : DefaultName(slot);
        if (!at.HasValue) return name + "　<size=70%><alpha=#80>空き</size>";

        string place = AreaName(data != null ? data.entered_scene : null);
        return name + "　<size=70%><color=#FFD27A>" + place + "</color></size>\n" +
               "<size=60%><alpha=#B0>" + at.Value.ToString("M/d HH:mm") + "</size>";
    }

    /// <summary>名前を付けていない枠の名前</summary>
    private static string DefaultName(int slot)
    {
        return "データ" + slot;
    }

    /// <summary>枠に出す場所の名前。場所の Id の読み方は TravelRule だけが知るので、拠点か根源かだけを出す</summary>
    private static string AreaName(string areaId)
    {
        if (string.IsNullOrEmpty(areaId)) return "";

        return areaId == TravelRule.BaseAreaId ? "管理地" : "根源";
    }

    /// <summary>いちばん新しく書いた保存の枠。保存が無ければ 0</summary>
    private static int LatestSlot()
    {
        int latest = 0;
        DateTime newest = DateTime.MinValue;

        for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
        {
            DateTime? at = SaveSystem.SavedAt(slot);
            if (!at.HasValue || at.Value <= newest) continue;

            newest = at.Value;
            latest = slot;
        }

        return latest;
    }
}
