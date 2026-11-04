using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour // タスク風にクエ管理。成功判定も行う
{
    //追加・検索・削除の唯一の窓口。参照を持ち回らず、使うときにここから引く
    public static readonly QuestRegistry Quests = new QuestRegistry();

    /// <summary>一度に表示するクエストの数</summary>
    private const int ShownAtOnce = 4;

    /// <summary>調査の目的。始めたときの見出しと、調査の枠と、画面の左上の受注中の札に出す</summary>
    public const string MainObjective = "スポナーを探し出し、破壊する";

    /// <summary>決壊で核を壊したあとの目的</summary>
    public static readonly string DefeatObjective = "敵を" + Quest.BreachDefeats + "体討伐する";

    /// <summary>決壊の目的。核を壊しても終わらず、続けて敵を倒す。出す所は調査の目的と同じ</summary>
    public static readonly string BreachObjective = "スポナーを破壊し、" + DefeatObjective;

    /// <summary>達成したあとの目的。根源を出る前にその中で倒れると失敗になるので、達成した時点では達成と出さない</summary>
    public const string ReturnObjective = "生きて根源を出る";

    /// <summary>表示順。決壊、調査、サブの順に並べる</summary>
    private static readonly QuestKind[] ShowOrder =
        { QuestKind.Breach, QuestKind.Main, QuestKind.Sub };

    /// <summary>受注中の枠の地と縁の色。ほかの枠と見分けがつくように</summary>
    private static readonly Color OrderedPaper = new Color(1f, 0.93f, 0.74f);
    private static readonly Color OrderedFrame = new Color(1f, 0.8f, 0.36f);

    /// <summary>ほかの根源のクエストを受注しているときの、状態の字の色</summary>
    private static readonly Color OtherOrderedColor = new Color(0.45f, 0.42f, 0.4f);

    public static string ordered_id = ""; // クエ中不変。QBから受注
   // private int pluschild_qm = 6;

    [SerializeField] private Text progress;
    [SerializeField] private GameObject questUI;
    [SerializeField] private AreaTravel travel;

    /// <summary>成功が決まり、まだ受け取っていないクエスト。着いた先ですぐ受け取る</summary>
    private string unannounced = "";

    /// <summary>
    /// 保存が無いまま始めたとき、根源ごとに調査とサブを 1 本ずつ出す。
    /// 夜明けの発行は保留を挟むので、ここで出さないと 2 回目の夜明けまで受けるものが無い
    /// </summary>
    public static void IssueFirst()
    {
        foreach (Root root in RootsManager.Roots.All) IssueFirst(root);
    }

    /// <summary>根源 1 つに、始めの調査とサブを出す。保存のあとで足した根源にも使う</summary>
    public static void IssueFirst(Root root)
    {
        if (!Quests.HasMainFor(root.Id)) IssueMain(root, QuestKind.Main);
        IssueSub(root.Id);
    }

    /// <summary>調査か決壊を 1 本出す。報酬は根源の危険度で決まる</summary>
    public static Quest IssueMain(Root root, QuestKind kind)
    {
        return Quests.Create(kind, root.Id, "MainSpawner", Quest.CoreAmount(kind), QuestRewards.For(kind, root.Danger));
    }

    /// <summary>氾濫した根源に決壊を 1 本出す。既にあれば足さずに false</summary>
    public static bool IssueBreach(Root root)
    {
        if (Quests.HasBreachFor(root.Id)) return false;

        IssueMain(root, QuestKind.Breach);
        return true;
    }

    /// <summary>
    /// 根源にサブを 1 本足す。夜明けの発行と始めたときとで同じものを出す。足したら true。
    /// 残っていれば足さない。足すと 1 体倒すたびに両方が進み、2 本ぶん払う
    /// </summary>
    public static bool IssueSub(string rootId)
    {
        if (Quests.HasSubFor(rootId)) return false;

        Quests.Create(QuestKind.Sub, rootId, "Slime", 3, new[] { new Reward("coin", 100) });
        return true;
    }

    public void ShowQuestUI(string root) //QuestUI(ボタン)の生成 + 情報入力
    {
        //開くたびに作り直す。閉じるボタンは枠を消さないので、残すと開くたびに積もる
        foreach (QuestButton old in GetComponentsInChildren<QuestButton>(true)) Destroy(old.gameObject);

        List<Quest> shown = new List<Quest>();

        foreach (QuestKind kind in ShowOrder)
        {
            foreach (Quest quest in Quests.All)
            {
                if (shown.Count >= ShownAtOnce) break;
                if (quest.RootId != root || quest.Kind != kind) continue;

                shown.Add(quest);
            }
        }

        Debug.Log("クエスト数:" + Quests.Count + " 表示:" + shown.Count);

        Transform slots = transform.Find("QuestTopPosition");
        for (int i = 0; i < shown.Count; i++)
        {
            Quest quest = shown[i];

            //1 本ずつ別の段に置く。重ねると、後に置いた押せないサブの枠が調査の枠への入力を奪う
            GameObject obj = Instantiate(questUI, slots.GetChild(i).position, Quaternion.identity, transform);
            obj.name = quest.Id;
            obj.GetComponent<QuestButton>().Bind(travel.Where);

            //消した古い枠はこのフレームの終わりまで残り、同じ名前で先に見つかるので、名前で引かない
            Slider slider = obj.transform.Find("Slider_IconType_03_basic_WhiteFill").GetComponent<Slider>();
            slider.maxValue = quest.Amount;
            slider.value = quest.Progress;

            TextMeshProUGUI t = obj.transform.Find("TypeText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI b = obj.transform.Find("BodyText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI r = obj.transform.Find("RewardText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI s = obj.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

            if (quest.Kind == QuestKind.Main || quest.Kind == QuestKind.Breach)
            {
                t.text = quest.Kind == QuestKind.Main ? "【調査クエスト】" : "【決壊クエスト】";
                b.text += quest.Kind == QuestKind.Main ? MainObjective : BreachObjective; //クエ目的記入
            }
            else if (quest.Kind == QuestKind.Sub)
            {
                t.text = "【サブクエスト】";
                b.text += Objective(quest); //クエ目的記入
            }

            foreach (Reward reward in quest.Rewards)
                r.text += RewardName(reward.Kind) + " x " + reward.Amount + "\n";

            if (quest.Kind == QuestKind.Sub || quest.Kind == QuestKind.Common)
            {
                t.text = quest.Kind == QuestKind.Sub ? "【サブクエスト】" : "【共通クエスト】";

                obj.GetComponent<Button>().interactable = false;
                s.text = quest.IsComplete ? "達成済み" : "";
            }
            else if (ordered_id == quest.Id)
            {
                s.text = quest.IsComplete ? "達成済み" : travel.Where.RootId == quest.RootId ? "クエスト中" : "受注中";
                Emphasize(obj, s);
            }
            else if (quest.IsComplete) s.text = "達成済み";
            else if (Quests.Find(ordered_id) != null)
            {
                //受注できるのは全体で 1 本だけ。ほかの根源のものを受けているあいだは受けられない
                s.text = "ほかを受注中";
                s.color = OtherOrderedColor;
            }
        }
    }

    /// <summary>受注中の枠を目立たせる。縁を金に、地を暖かい色にし、状態を赤いリボンに白い字で載せる</summary>
    private static void Emphasize(GameObject card, TextMeshProUGUI status)
    {
        card.GetComponent<Image>().color = OrderedPaper;
        card.transform.Find("Frame").GetComponent<Image>().color = OrderedFrame;

        Transform badge = card.transform.Find("OrderedBadge");
        if (badge != null) badge.gameObject.SetActive(true);

        status.color = Color.white;
        status.fontStyle = FontStyles.Bold;
    }

    public static string RewardName(string kind)
    {
        if (kind == "progress") return "攻略度";
        if (kind == "coin") return "コイン";

        //品物の報酬は原簿の表示名で出す
        ItemDefinition item = GameManager.Items.Find(kind);
        return item != null ? item.Name : kind;
    }

    /// <summary>サブと共通の目的</summary>
    private static string Objective(Quest quest)
    {
        return TargetName(quest.Target) + "を" + quest.Amount + "体討伐する";
    }

    /// <summary>討伐の対象の表示名。対象は倒した敵の GameObject の名前と突き合わせるので、名前のまま持つ</summary>
    private static string TargetName(string target)
    {
        return target == "Slime" ? "スライム" : target;
    }

    public void StartQuest() //クエ受注後に侵入で開始。受注処理はQB。メインのみ
    {
        //SceneStarterより起動。始まるのは受けたクエストの根源に入ったときだけで、ほかの根源は通り道
        Quest quest = Quests.Find(ordered_id);
        if (quest == null || quest.RootId != travel.Where.RootId) return;

        RewardUI.rewardUI_id = quest.Id;
        transform.parent.Find("BigText").gameObject.GetComponent<BigText>()
                 .Bigtxt_Anim(Headline(quest, CurrentObjective(quest)), "開始");

        //クエスト物生成
    }

    /// <summary>受けたクエストの根源に居るか。ここで倒れると、そのクエストは失敗になる</summary>
    public static bool IsUnderwayIn(string rootId)
    {
        Quest quest = Quests.Find(ordered_id);
        return quest != null && rootId != null && quest.RootId == rootId;
    }

    /// <summary>
    /// 場所を離れる。受けたクエストを達成したまま、その根源を生きて出たら成功が決まる。
    /// 受注の枠はここで空き、報酬は着いた先ですぐ受け取る
    /// </summary>
    public void Leave(string rootId)
    {
        Quest quest = Quests.Find(ordered_id);
        if (quest == null || rootId == null || quest.RootId != rootId || !quest.IsComplete) return;

        //倒れて拠点へ戻される途中。失敗のまま
        if (RewardUI.rewardUI_show == 1) return;

        RewardUI.rewardUI_show = 2;
        ordered_id = "";
        unannounced = quest.Id;
    }

    /// <summary>
    /// 場所に着いた。成功が決まっていれば、ここで受け取る。拠点では報酬 UI で受け取るので何もしない。
    /// 根源では画面を出さず、払った分を大きい文字に添える
    /// </summary>
    public void Arrived(AreaKind kind, RewardUI rewardUI)
    {
        Quest quest = Quests.Find(unannounced);
        unannounced = "";
        if (quest == null || kind == AreaKind.Base) return;

        //受け取るとクエストは一覧から消える。見出しに使う quest は、受け取る前に引いてある
        string paid = rewardUI.Receive();
        transform.parent.Find("BigText").gameObject.GetComponent<BigText>()
                 .Bigtxt_Anim(Headline(quest, paid), "成功");
    }

    /// <summary>開始時と、根源の核を壊したとき、成功が決まったときに出す見出し。決壊クエストは種別の名前を差し替える</summary>
    private static string Headline(Quest quest, string objective)
    {
        return "「" + RootsManager.Roots.Find(quest.RootId).Name + "」" + KindName(quest) + "クエスト\n\n" + objective;
    }

    /// <summary>調査か決壊か。見出しと受注中の札に出す</summary>
    public static string KindName(Quest quest)
    {
        return quest.Kind == QuestKind.Breach ? "決壊" : "調査";
    }

    /// <summary>調査か決壊の、いまの目的。見出しと受注中の札に出す</summary>
    public static string CurrentObjective(Quest quest)
    {
        if (quest.IsComplete) return ReturnObjective;
        if (quest.Kind != QuestKind.Breach) return MainObjective;

        return quest.CoreBroken ? DefeatObjective : BreachObjective;
    }

    public void SyncQuest(string target) // クエ進捗追加、外部から呼ぶ
    {
        //サブは達成したその場で一覧から消えるので、写しを回す
        foreach (Quest quest in new List<Quest>(Quests.All))
        {
            if (!quest.AdvancesOn(target)) continue;

            if (quest.Kind == QuestKind.Sub && quest.RootId == travel.Where.RootId) SyncQuestSub(quest);
            else if (quest.Kind == QuestKind.Common) SyncQuestSub(quest);
            //受けた調査と決壊は、対象の根源の中でだけ進む。通り道の根源で核を壊しても進めない
            else if (quest.Id == ordered_id && quest.RootId == travel.Where.RootId) SyncQuestSub(quest);
        }
    }

    public void SyncQuestSub(Quest quest)
    {
        //達成したあとも、対象を倒すたびに呼ばれる。知らせるのは達成した 1 回だけ
        if (quest.IsComplete) return;

        quest.Advance();

        if (quest.Kind == QuestKind.Main || quest.Kind == QuestKind.Breach)
        {
            //大きい文字は、核を壊したときと、決壊で敵を倒しきったときだけ。倒した数は受注中の札に出る。
            //成功が決まるのは、生きてこの根源を出たとき（Leave）
            string word = quest.DefeatedAfterCore == 0 ? "破壊" : quest.IsComplete ? "討伐" : null;
            if (word != null)
                transform.parent.Find("BigText").gameObject.GetComponent<BigText>()
                         .Bigtxt_Anim(Headline(quest, CurrentObjective(quest)), word);
        }
        else if (!quest.IsComplete) return;
        else if (quest.Kind == QuestKind.Sub)
        {
            //サブは達成したその場で払って一覧から消す。次の夜明けにまた出る
            string paid = FindAnyObjectByType<RewardUI>(FindObjectsInactive.Include).Pay(quest);
            Notice("サブクエスト達成：" + Objective(quest) + "（" + paid + "）");
        }
        else Notice("共通クエスト達成：" + Objective(quest));
    }

    private static void Notice(string text)
    {
        GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim(text);
    }

    //FinQuest()はRewardUIへ移行しました

    private void ClearUI(string id, string type)
    {
        Transform ui;
        if (type == "Common") { ui = transform.parent.Find("Task").Find(id); }
        else { ui = transform.Find(id); }
        Destroy(ui);
    }
}
