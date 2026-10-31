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

    /// <summary>調査と決壊の目的。始めたときの見出しと、調査の枠に出す</summary>
    private const string MainObjective = "根源を探し出し、破壊する";

    /// <summary>根源の拠点を壊したあとの目的。根源を出る前にその中で倒れると失敗になるので、壊した時点では達成と出さない</summary>
    private const string ReturnObjective = "生きて根源を出る";

    /// <summary>表示順。決壊、調査、サブの順に並べる</summary>
    private static readonly QuestKind[] ShowOrder =
        { QuestKind.Breach, QuestKind.Main, QuestKind.Sub };

    public static string ordered_id = ""; // クエ中不変。QBから受注
   // private int pluschild_qm = 6;

    [SerializeField] private Text progress;
    [SerializeField] private GameObject questUI;
    [SerializeField] private AreaTravel travel;

    /// <summary>成功が決まり、まだ受け取っていないクエスト。着いた先ですぐ受け取る</summary>
    private string unannounced = "";

    void Start()
    {
        //夜明けの発行は保留を挟む。ここで出さないと、2 回目の夜明けまで受けるものが無い
        Quests.Create(QuestKind.Main, "Root1", "MainSpawner", 1,
                      new[] { new Reward("coin", 100), new Reward("progress", 15) });
        IssueSub("Root1");
        Quests.Create(QuestKind.Main, "Root2", "MainSpawner", 1,
                      new[] { new Reward("coin", 200), new Reward("progress", 15) });
        IssueSub("Root2");
    }

    /// <summary>
    /// 根源にサブを 1 本足す。夜明けの発行と始めたときとで同じものを出す。
    /// 残っていれば足さない。足すと 1 体倒すたびに両方が進み、2 本ぶん払う
    /// </summary>
    public static void IssueSub(string rootId)
    {
        if (Quests.HasSubFor(rootId)) return;

        Quests.Create(QuestKind.Sub, rootId, "Slime", 3, new[] { new Reward("coin", 100) });
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

            if (quest.Kind == QuestKind.Main)
            {
                t.text = "【調査クエスト】";
                b.text += MainObjective; //クエ目的記入
            }
            else if (quest.Kind == QuestKind.Breach) t.text = "【決壊クエスト】";
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
            else if (quest.IsComplete) s.text = "達成済み";
            else if (ordered_id == quest.Id) s.text = travel.Where.RootId == quest.RootId ? "クエスト中" : "受注済み";
        }
    }

    public static string RewardName(string kind)
    {
        if (kind == "progress") return "攻略度";
        if (kind == "coin") return "コイン";

        return kind;
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
                 .Bigtxt_Anim(Headline(quest, MainObjective), "開始");

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

    /// <summary>開始時と、根源の拠点を壊したとき、成功が決まったときに出す見出し。決壊クエストは種別の名前を差し替える</summary>
    private static string Headline(Quest quest, string objective)
    {
        string kind = quest.Kind == QuestKind.Breach ? "決壊" : "調査";
        return "「" + RootsManager.Roots.Find(quest.RootId).Name + "」" + kind + "クエスト\n\n" + objective;
    }

    public void SyncQuest(string target) // クエ進捗追加、外部から呼ぶ
    {
        foreach (Quest quest in Quests.All)
        {
            if (quest.Target != target) continue;

            if (quest.Kind == QuestKind.Sub && quest.RootId == travel.Where.RootId) SyncQuestSub(quest);
            else if (quest.Kind == QuestKind.Common) SyncQuestSub(quest);
            else if (quest.Id == ordered_id) SyncQuestSub(quest);
        }
    }

    public void SyncQuestSub(Quest quest)
    {
        //達成したあとも、対象を倒すたびに呼ばれる。知らせるのは達成した 1 回だけ
        if (quest.IsComplete) return;

        quest.Advance();
        if (!quest.IsComplete) return;

        if (quest.Kind == QuestKind.Main || quest.Kind == QuestKind.Breach)
        {
            //成功が決まるのは、生きてこの根源を出たとき（Leave）
            transform.parent.Find("BigText").gameObject.GetComponent<BigText>()
                     .Bigtxt_Anim(Headline(quest, ReturnObjective), "破壊");
        }
        else
        {
            string kind = quest.Kind == QuestKind.Sub ? "サブクエスト" : "共通クエスト";
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim(kind + "達成：" + Objective(quest));
        }
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
