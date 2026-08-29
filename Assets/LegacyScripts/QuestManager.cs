using System.Collections.Generic;
using MemorialFloor.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour // タスク風にクエ管理。成功判定も行う
{
    //追加・検索・削除の唯一の窓口。参照を持ち回らず、使うときにここから引く
    public static readonly QuestRegistry Quests = new QuestRegistry();

    /// <summary>一度に表示するクエストの数</summary>
    private const int ShownAtOnce = 4;

    /// <summary>表示順。決壊、調査、サブの順に並べる</summary>
    private static readonly QuestKind[] ShowOrder =
        { QuestKind.Breach, QuestKind.Main, QuestKind.Sub };

    // entered_sceneはGameManagerにあります
    public static string ordered_id = ""; // クエ中不変。QBから受注
   // private int pluschild_qm = 6;

    [SerializeField] private Text progress;
    [SerializeField] private GameObject questUI;

    void Start()
    {
        Quests.Create(QuestKind.Main, "Root1", "MainSpawner", 1,
                      new[] { new Reward("coin", 100), new Reward("progress", 15) });
        Quests.Create(QuestKind.Main, "Root2", "MainSpawner", 1,
                      new[] { new Reward("coin", 200), new Reward("progress", 15) });
    }

    public void ShowQuestUI(string root) //QuestUI(ボタン)の生成 + 情報入力
    {
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

        foreach (Quest quest in shown)
        {
            GameObject obj = Instantiate(questUI,
                transform.Find("QuestTopPosition").GetChild(shown.Count - 1).position,
                Quaternion.identity, transform);
            obj.name = quest.Id;

            Slider slider = transform.Find(quest.Id).Find("Slider_IconType_03_basic_WhiteFill").GetComponent<Slider>();
            slider.maxValue = quest.Amount;
            slider.value = quest.Progress;

            TextMeshProUGUI t = obj.transform.Find("TypeText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI b = obj.transform.Find("BodyText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI r = obj.transform.Find("RewardText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI s = obj.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

            if (quest.Kind == QuestKind.Main)
            {
                t.text = "【調査クエスト】";
                b.text += "根源を探し出し、破壊する"; //クエ目的記入
            }
            else if (quest.Kind == QuestKind.Breach) t.text = "【決壊クエスト】";
            else if (quest.Kind == QuestKind.Sub)
            {
                t.text = "【サブクエスト】";
                b.text += quest.Target + "を" + quest.Amount + "体討伐する"; //クエ目的記入
            }

            foreach (Reward reward in quest.Rewards)
                r.text += RewardName(reward.Kind) + " x " + reward.Amount + "\n";

            if (quest.Kind == QuestKind.Sub || quest.Kind == QuestKind.Common)
            {
                t.text = quest.Kind == QuestKind.Sub ? "【サブクエスト】" : "【共通クエスト】";

                obj.GetComponent<Button>().interactable = false;
                s.text = "";
            }
            else if (ordered_id == quest.Id)
            {
                if (quest.IsComplete) s.text = "達成済み";
                else if (GameManager.entered_scene == quest.RootId) s.text = "クエスト中";
                else s.text = "受注済み";
            }
        }
    }

    public static string RewardName(string kind)
    {
        return kind == "progress" ? "攻略度" : kind;
    }

    public void StartQuest() //クエ受注後に侵入で開始。受注処理はQB。メインのみ
    {
        //SceneStarterより起動
        Quest quest = Quests.Find(ordered_id);
        if (quest == null) return;

        RewardUI.rewardUI_id = quest.Id;
        transform.parent.Find("BigText").gameObject.GetComponent<BigText>()
                 .Bigtxt_Anim(Headline(quest), "開始");

        //クエスト物生成
    }

    /// <summary>達成時と開始時に出す見出し。決壊クエストは見出しを差し替える</summary>
    private static string Headline(Quest quest)
    {
        string text = "「" + RootsManager.Roots.Find(quest.RootId).Name + "」調査クエスト\n\n";
        text += "根源を探し出し、破壊する";

        return quest.Kind == QuestKind.Breach ? text.Replace("調査", "決壊") : text;
    }

    public void SyncQuest(string target) // クエ進捗追加、外部から呼ぶ
    {
        foreach (Quest quest in Quests.All)
        {
            if (quest.Target != target) continue;

            if (quest.Kind == QuestKind.Sub && quest.RootId == GameManager.entered_scene) SyncQuestSub(quest);
            else if (quest.Kind == QuestKind.Common) SyncQuestSub(quest);
            else if (quest.Id == ordered_id) SyncQuestSub(quest);
        }
    }

    public void SyncQuestSub(Quest quest)
    {
        quest.Advance();
        if (!quest.IsComplete) return;

        if (quest.Kind == QuestKind.Main || quest.Kind == QuestKind.Breach)
        {
            transform.parent.Find("BigText").gameObject.GetComponent<BigText>()
                     .Bigtxt_Anim(Headline(quest), "達成");

            RewardUI.rewardUI_show = 2; //帰還後UI表示フラグ
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
