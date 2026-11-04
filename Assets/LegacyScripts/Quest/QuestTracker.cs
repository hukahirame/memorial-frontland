using MemorialFloor.Domain;
using MemorialFloor.Game;
using TMPro;
using UnityEngine;

/// <summary>
/// 受注中の調査・決壊クエストを、画面の左上に小さく出す。受注していなければ出さない。
/// 受注・解除・達成・受け取り・読み込みのどれで変わっても拾えるよう、毎コマ受注の様子を見て、変わったときだけ書き直す
/// </summary>
public class QuestTracker : MonoBehaviour
{
    /// <summary>出し入れにかける秒数</summary>
    private const float FadeSeconds = 0.25f;

    /// <summary>札の左右の、文字の端から札の端までの幅。札の両端はとがっている</summary>
    private const float Padding = 44f;

    /// <summary>札のいちばん短い幅</summary>
    private const float MinWidth = 360f;

    private const string StateColor = "#F2C46D";
    private const string ObjectiveColor = "#D9D2C3";

    [SerializeField] private AreaTravel travel;
    [SerializeField] private TextMeshProUGUI text;

    private CanvasGroup group;
    private RectTransform plate;
    private string shownKey;
    private float target;

    void Awake()
    {
        plate = (RectTransform)transform;

        //出ていないあいだも出ているあいだも、札の下のボタンを押せるようにする
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void Update()
    {
        Quest quest = QuestManager.Quests.Find(QuestManager.ordered_id);
        string key = Key(quest);
        if (key != shownKey)
        {
            shownKey = key;
            if (quest != null) Write(quest);
            target = quest != null ? 1f : 0f;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / FadeSeconds);
    }

    /// <summary>出す中身が変わったかを見るための鍵。受注・進み・居る根源のどれかが変われば変わる</summary>
    private string Key(Quest quest)
    {
        if (quest == null) return "";

        return quest.Id + "|" + quest.Progress + "|" + IsInside(quest);
    }

    private bool IsInside(Quest quest)
    {
        return travel != null && travel.Where.RootId == quest.RootId;
    }

    private void Write(Quest quest)
    {
        Root root = RootsManager.Roots.Find(quest.RootId);
        string place = root != null ? root.Name : quest.RootId;
        //決壊は核を壊したあとも、敵を倒しきるまで続く。倒した数を目的に添える
        string state = !quest.CoreBroken ? (IsInside(quest) ? "クエスト中" : "受注中")
                     : quest.IsComplete && quest.DefeatedAfterCore > 0 ? "討伐済み" : "破壊済み";
        string objective = QuestManager.CurrentObjective(quest);
        if (quest.CoreBroken && !quest.IsComplete) objective += "（" + quest.DefeatedAfterCore + "/" + Quest.BreachDefeats + "）";

        text.text = "<color=" + StateColor + "><b>" + state + "</b></color>　「" + place + "」" + QuestManager.KindName(quest) + "クエスト\n"
                  + "<size=80%><color=" + ObjectiveColor + ">" + objective + "</color></size>";

        float width = Mathf.Max(MinWidth, text.GetPreferredValues(text.text, 10000f, 1000f).x + Padding * 2f);
        plate.sizeDelta = new Vector2(width, plate.sizeDelta.y);
    }
}
