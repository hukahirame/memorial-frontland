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

    public void InputInfo()
    {
        Quest quest = QuestManager.Quests.Find(rewardUI_id);
        if (quest == null) return;

        Root root = RootsManager.Roots.Find(quest.RootId);
        if (root == null) return;

        toptxt.text = "「" + root.Name + "」のクエスト結果";
        sidetxt.text = "◎危険度：" + root.Danger + "\n\n";

        if (rewardUI_show == 1)
        {

        }
        else if (rewardUI_show == 2)
        {
            resulttxt.text = "成功";
            resulttxt.color = new Color(170, 0, 0, 255);

            int now_progress = root.Progress;
            sidetxt.text += string.Format("◎攻略度　{0} % → {1} %\n\n", now_progress, now_progress + 15);
            sidetxt.text += "【報酬】\n";

            foreach (Reward reward in quest.Rewards)
                sidetxt.text += "・" + QuestManager.RewardName(reward.Kind) + "　x　" + reward.Amount + "\n";

            FinQuest();
        }

        rewardUI_show = 0;
    }

    public void FinQuest() // 帰還後の処理
    {
        List<Quest> cleared = new List<Quest>();
        foreach (Quest quest in QuestManager.Quests.All)
        {
            if (!quest.IsComplete) continue;

            cleared.Add(quest);
            Debug.Log("クエストクリア：" + quest.Id);
        }

        if (cleared.Count == 0)
        {
            Debug.Log("クリアされたクエストなし");
            return;
        }

        //消すのは添字ではなく Id で行う。添字は1つ消すたびに後ろがずれる
        foreach (Quest quest in cleared)
        {
            foreach (Reward reward in quest.Rewards) Grant(quest, reward);

            QuestManager.ordered_id = "";

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
