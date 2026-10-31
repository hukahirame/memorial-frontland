using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MemorialFloor.Domain;

public class RewardUI : MonoBehaviour
{
    public static int rewardUI_show = 0; //0：非表示　1：失敗　2：成功

    //添字ではなく Id で指す。添字は削除で他のクエストにずれる
    public static string rewardUI_id = "";

    [SerializeField] TextMeshProUGUI toptxt;
    [SerializeField] TextMeshProUGUI sidetxt;
    [SerializeField] TextMeshProUGUI resulttxt;

    /// <summary>置いたときの文字色が失敗の色。成功で塗り替えたあと、失敗で戻す</summary>
    private Color failColor;

    private void Awake()
    {
        failColor = resulttxt.color;
    }

    /// <summary>
    /// 結果を書き込む。出すものが無ければ false。印は書き込めなくても持ち越さない。
    /// 持ち越すと、あとで関係の無い帰還のときに古い結果が出る
    /// </summary>
    public bool InputInfo()
    {
        int show = rewardUI_show;
        rewardUI_show = 0;

        Quest quest = QuestManager.Quests.Find(rewardUI_id);
        if (quest == null) return false;

        Root root = RootsManager.Roots.Find(quest.RootId);
        if (root == null) return false;

        toptxt.text = "「" + root.Name + "」のクエスト結果";
        sidetxt.text = "◎危険度：" + root.Danger + "\n\n";

        if (show == 1)
        {
            resulttxt.text = "失敗";
            resulttxt.color = failColor;

            //達成していても、根源を出る前に倒れたらやり直し。受注は残すので、もう一度挑める
            quest.SetProgress(0);
        }
        else if (show == 2)
        {
            resulttxt.text = "成功";
            resulttxt.color = new Color(170, 0, 0, 255);

            //受け取るのは達成済みのすべて（他の根源のサブも）。表示もそれに合わせる
            List<Quest> cleared = Cleared();
            int now_progress = root.Progress;
            sidetxt.text += string.Format("◎攻略度　{0} % → {1} %\n\n", now_progress, now_progress + ProgressGain(cleared, root.Id));
            sidetxt.text += "【報酬】\n";

            foreach (Reward reward in Total(cleared))
                sidetxt.text += "・" + QuestManager.RewardName(reward.Kind) + "　x　" + reward.Amount + "\n";

            FinQuest();
        }

        return true;
    }

    /// <summary>
    /// 根源に着いたときに成功を受け取る。画面は出さず、払ったものを 1 行で返す。
    /// 結果の画面は時間を止めず閉じるまで残るので、根源で開くと敵の前で手が止まる
    /// </summary>
    public string Receive()
    {
        rewardUI_show = 0;

        List<string> paid = new List<string>();
        foreach (Reward reward in Total(Cleared()))
            paid.Add(QuestManager.RewardName(reward.Kind) + " x " + reward.Amount);

        FinQuest();
        return string.Join("、", paid);
    }

    private static List<Quest> Cleared()
    {
        List<Quest> cleared = new List<Quest>();
        foreach (Quest quest in QuestManager.Quests.All)
        {
            if (quest.IsComplete) cleared.Add(quest);
        }

        return cleared;
    }

    /// <summary>その根源の攻略度が報酬で伸びる量</summary>
    private static int ProgressGain(List<Quest> quests, string rootId)
    {
        int gain = 0;
        foreach (Quest quest in quests)
        {
            if (quest.RootId != rootId) continue;

            foreach (Reward reward in quest.Rewards)
            {
                if (reward.Kind == "progress") gain += reward.Amount;
            }
        }

        return gain;
    }

    /// <summary>種類ごとに足し合わせる。並びは最初に出た順</summary>
    private static List<Reward> Total(List<Quest> quests)
    {
        List<Reward> total = new List<Reward>();
        foreach (Quest quest in quests)
        {
            foreach (Reward reward in quest.Rewards)
            {
                int at = total.FindIndex(r => r.Kind == reward.Kind);
                if (at < 0) total.Add(reward);
                else total[at] = new Reward(reward.Kind, total[at].Amount + reward.Amount);
            }
        }

        return total;
    }

    public void FinQuest() // 帰還後の処理
    {
        List<Quest> cleared = Cleared();
        foreach (Quest quest in cleared) Debug.Log("クエストクリア：" + quest.Id);

        if (cleared.Count == 0)
        {
            Debug.Log("クリアされたクエストなし");
            return;
        }

        //消すのは添字ではなく Id で行う。添字は1つ消すたびに後ろがずれる
        foreach (Quest quest in cleared)
        {
            foreach (Reward reward in quest.Rewards) Grant(quest, reward);

            //空けるのは受け取ったものの受注だけ。一緒に失敗したクエストは受けたまま残す
            if (QuestManager.ordered_id == quest.Id) QuestManager.ordered_id = "";

            Debug.Log("クリア＆リスト破壊対象：" + quest.Id);
            GameObject clearedUI = GameObject.Find(quest.Id);
            if (clearedUI != null) Destroy(clearedUI);

            QuestManager.Quests.Remove(quest.Id);
        }
    }

    private void Grant(Quest quest, Reward reward)
    {
        if (reward.Kind == "progress")
        {
            Root rewarded = RootsManager.Roots.Find(quest.RootId);
            if (rewarded != null) rewarded.Gain(reward.Amount);
        }
        else if (reward.Kind == "coin")
        {
            GameManager.Coins.Add(reward.Amount);
            GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>().text =
                GameManager.Coins.Amount.ToString();
        }
        else
        {
            //coin でも progress でもないものはアイテム報酬
            transform.Find("Inventory").GetComponent<PlayerInventory>()
                     .LoadInventory(reward.Kind, reward.Amount);
        }
    }
}
