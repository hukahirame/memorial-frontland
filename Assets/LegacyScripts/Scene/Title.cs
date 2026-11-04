using System;
using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル画面。保存があれば「つづきから」（いちばん新しく書いた枠）と「データをえらぶ」を、無ければ「はじめる」だけを出す。
/// 「データをえらぶ」では保存の枠を名前と並べ、押した枠の続きから始める。空いた枠は名前を決めてからはじめから始める。
/// 「はじめる」も 1 つめの枠の名前を決めてから始める。保存のある枠は「編集」で名前を変えたり消したりできる
/// </summary>
public class Title : MonoBehaviour
{
    /// <summary>開いてから画面が浮かび上がりきるまでの秒数</summary>
    private const float AppearSeconds = 1.2f;

    /// <summary>題字が上下に揺れる幅。参照解像度の画素</summary>
    private const float FloatHeight = 8f;

    /// <summary>題字が 1 往復する秒数</summary>
    private const float FloatSeconds = 4f;

    /// <summary>データをえらぶ画面で、並べたボタンのあいだ。参照解像度の画素</summary>
    private const float Gap = 16f;

    /// <summary>消すボタンを 1 回押したときの文字。2 回目で消す</summary>
    private const string ConfirmText = "もう一度で消す";

    /// <summary>枠の名前の長さの上限（字数）。枠のボタンに収まる長さ</summary>
    private const int NameLimit = 12;

    /// <summary>名前の欄の下敷き。ボタンの板は透けているが、欄は打つ所が分かるよう暗く敷く</summary>
    private static readonly Color FieldBack = new Color(0f, 0f, 0f, 0.45f);

    [Tooltip("画面全体。開いたときに透明から浮かび上がらせる")]
    [SerializeField] private CanvasGroup screen;

    [Tooltip("題字。ゆっくり上下に揺らす")]
    [SerializeField] private RectTransform logo;

    [Tooltip("始めるボタンの文字。保存があれば「つづきから」、無ければ「はじめる」。データをえらぶ画面の枠のボタンは、このボタンを写して作る")]
    [SerializeField] private TextMeshProUGUI startLabel;

    [Tooltip("2 つめのボタン。保存があるときだけ見せ、データをえらぶ画面を開く。編集・消す・もどるのボタンは、このボタンを写して作る")]
    [SerializeField] private GameObject freshButton;

    [Tooltip("2 つめのボタンの文字")]
    [SerializeField] private TextMeshProUGUI freshLabel;

    /// <summary>データをえらぶ画面と名前の画面に並べた物。画面を替えるときに消す</summary>
    private readonly List<GameObject> made = new List<GameObject>();

    /// <summary>消すボタンを 1 回押して確かめを出したか。保存を消すと取り返せないので、2 回目で消す</summary>
    private bool confirming;

    private Vector2 logoHome;

    private GameObject StartButton
    {
        get { return startLabel.GetComponentInParent<Button>(true).gameObject; }
    }

    private void Start()
    {
        ShowFront();
        PressFeedback.AttachAll(screen.transform);

        logoHome = logo.anchoredPosition;
        StartCoroutine(Appear());
    }

    private void Update()
    {
        float phase = Time.unscaledTime / FloatSeconds * 2f * Mathf.PI;
        logo.anchoredPosition = logoHome + Vector2.up * (Mathf.Sin(phase) * FloatHeight);
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

    /// <summary>始めるボタンから呼ぶ。保存があればいちばん新しく書いた枠の続きから、無ければ 1 つめの枠の名前を決めてからはじめる</summary>
    public void Load()
    {
        int latest = LatestSlot();
        if (latest > 0) Play(latest, "");
        else ShowNamer(1, ShowFront);
    }

    /// <summary>2 つめのボタンから呼ぶ。データをえらぶ画面を開く。名前はシーンのボタンが呼ぶので変えない</summary>
    public void LoadFresh()
    {
        ShowPicker();
    }

    /// <summary>name ははじめから始めるときに枠へ付ける名前。続きからなら保存の名前のままで、使われない</summary>
    private static void Play(int slot, string name)
    {
        SaveSystem.Slot = slot;
        SaveSystem.PendingName = name;
        SceneManager.LoadScene(AreaTravel.CoreScene);
    }

    /// <summary>最初の画面。保存が無ければ、始めるボタンで 1 つめの枠の名前を決めてからはじめる</summary>
    private void ShowFront()
    {
        Clear();

        bool saved = LatestSlot() > 0;
        startLabel.text = saved ? "つづきから" : "はじめる";
        freshLabel.text = "データをえらぶ";
        StartButton.SetActive(true);
        freshButton.SetActive(saved);
    }

    /// <summary>
    /// データをえらぶ画面。枠のボタンを始めるボタンの位置から上へ積み（1 つめが上）、保存のある枠には字の左に編集のボタンを付ける。
    /// もどるボタンは 2 つめのボタンの位置に置く。位置は型のボタンから測るので、タイトルの並べ方が変わってもついていく
    /// </summary>
    private void ShowPicker()
    {
        Clear();

        for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
        {
            int chosen = slot;
            bool saved = SaveSystem.HasSaveIn(slot);

            //保存のある枠は押すと続きから、空いた枠は名前を決めてから始める
            GameObject row = Copy(StartButton, Row(SaveSystem.SlotCount - slot), SlotText(slot),
                                  () => { if (saved) Play(chosen, ""); else ShowNamer(chosen, ShowPicker); });

            if (!saved) continue;

            GameObject edit = Copy(freshButton, Row(SaveSystem.SlotCount - slot), "編集", () => ShowNamer(chosen, ShowPicker));
            BesideText(edit, row, "編集");
        }

        Copy(freshButton, ((RectTransform)freshButton.transform).anchoredPosition, "もどる", ShowFront);
        HideFront();
    }

    /// <summary>
    /// 名前の画面。いちばん上に名前の欄、その下に決めるボタン、保存のある枠なら消すボタンを並べ、もどるは 2 つめのボタンの位置で back へ戻る。
    /// 空いた枠は「はじめる」で名前を付けて始め、保存のある枠は「決める」で名前を書き換えてデータをえらぶ画面へ戻る
    /// </summary>
    private void ShowNamer(int slot, UnityAction back)
    {
        Clear();

        bool saved = SaveSystem.HasSaveIn(slot);
        SaveData data = SaveSystem.Peek(slot);
        TMP_InputField field = NameField(Row(2), data != null ? data.saveName : "", DefaultName(slot));

        Copy(StartButton, Row(1), saved ? "決める" : "はじめる", () =>
        {
            string name = field.text.Trim();
            if (!saved)
            {
                Play(slot, name);
                return;
            }

            SaveSystem.Rename(slot, name);
            ShowPicker();
        });

        if (saved)
        {
            TextMeshProUGUI eraseLabel = null;
            GameObject erase = Copy(freshButton, Row(0), "消す", () => Erase(slot, eraseLabel));
            eraseLabel = erase.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Copy(freshButton, ((RectTransform)freshButton.transform).anchoredPosition, "もどる", back);
        HideFront();

        field.Select();
        field.ActivateInputField();
    }

    /// <summary>1 回目は確かめを出し、2 回目でその枠の保存を消してデータをえらぶ画面へ戻る</summary>
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

    /// <summary>並べる段の位置。0 が始めるボタンの位置で、上へ 1 段ずつ積む</summary>
    private Vector2 Row(int up)
    {
        var start = (RectTransform)StartButton.transform;
        return start.anchoredPosition + Vector2.up * (start.rect.height + Gap) * up;
    }

    private void HideFront()
    {
        StartButton.SetActive(false);
        freshButton.SetActive(false);
    }

    /// <summary>画面を替える前に、前の画面に並べた物を消す</summary>
    private void Clear()
    {
        foreach (GameObject thing in made) Destroy(thing);
        made.Clear();
        confirming = false;
    }

    /// <summary>
    /// ボタンを、枠のボタンの字の左に並べる。ボタンの板は透けていて字は右に寄るので、板の左端ではなく字の左端に合わせる。
    /// 幅は widest が入る分にする。字は右に寄るので、短い字でも枠の字のすぐ左に出る
    /// </summary>
    private static void BesideText(GameObject button, GameObject row, string widest)
    {
        TextMeshProUGUI rowLabel = row.GetComponentInChildren<TextMeshProUGUI>(true);
        rowLabel.ForceMeshUpdate();
        Bounds text = rowLabel.textBounds;
        Vector3 textLeft = rowLabel.rectTransform.TransformPoint(new Vector3(text.min.x, text.center.y, 0f));

        var rect = (RectTransform)button.transform;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(label.GetPreferredValues(widest).x + Gap * 2f, rect.sizeDelta.y);
        rect.position = textLeft;
        rect.anchoredPosition += Vector2.left * Gap;
    }

    /// <summary>型のボタンを同じ親に写し、文字と押したときの動きを替える。型がシーンでつないだ呼び先は外す</summary>
    private GameObject Copy(GameObject model, Vector2 at, string text, UnityAction onClick)
    {
        GameObject copy = Instantiate(model, model.transform.parent);
        copy.SetActive(true);
        ((RectTransform)copy.transform).anchoredPosition = at;

        //金の下線は、最初の画面でいちばん先に押すボタンの印。並べたボタンには付けない
        Transform underline = copy.transform.Find("Underline");
        if (underline != null) underline.gameObject.SetActive(false);

        //枠の文字は 2 行になる。型の大きさを上限に、収まるまで縮める
        TextMeshProUGUI label = copy.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = text;
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = label.fontSize * 0.5f;
        label.enableAutoSizing = true;

        Button button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);

        made.Add(copy);
        return copy;
    }

    /// <summary>名前の欄。始めるボタンと同じ大きさと並びにし、字も同じ書体で右に寄せる。空のときは枠の決まりの名前を薄く出す</summary>
    private TMP_InputField NameField(Vector2 at, string text, string placeholder)
    {
        var model = (RectTransform)StartButton.transform;
        GameObject root = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        root.name = "NameField";

        var rect = (RectTransform)root.transform;
        rect.SetParent(model.parent, false);
        rect.anchorMin = model.anchorMin;
        rect.anchorMax = model.anchorMax;
        rect.pivot = model.pivot;
        rect.sizeDelta = model.sizeDelta;
        rect.anchoredPosition = at;

        //UI だけを写すカメラで描くので、子もボタンと同じ層に置く
        foreach (Transform part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = model.gameObject.layer;

        root.GetComponent<Image>().color = FieldBack;

        TMP_InputField field = root.GetComponent<TMP_InputField>();
        field.characterLimit = NameLimit;
        field.lineType = TMP_InputField.LineType.SingleLine;

        foreach (TextMeshProUGUI shown in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            shown.font = startLabel.font;
            shown.fontSize = startLabel.fontSize;
            shown.alignment = TextAlignmentOptions.Right;
            shown.color = Color.white;
        }

        var hint = (TextMeshProUGUI)field.placeholder;
        hint.text = placeholder;
        hint.color = new Color(1f, 1f, 1f, 0.45f);

        field.text = text;
        made.Add(root);
        return field;
    }

    /// <summary>枠のボタンの文字。名前（無ければ決まりの名前）と、保存があれば書いた日時と場所、無ければ空き</summary>
    private static string SlotText(int slot)
    {
        SaveData data = SaveSystem.Peek(slot);
        DateTime? at = SaveSystem.SavedAt(slot);

        string name = data != null && !string.IsNullOrEmpty(data.saveName) ? data.saveName : DefaultName(slot);
        string detail = at.HasValue ? at.Value.ToString("M/d HH:mm") + "　" + AreaName(data != null ? data.entered_scene : null) : "空き";
        return name + "\n<size=60%>" + detail + "</size>";
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
