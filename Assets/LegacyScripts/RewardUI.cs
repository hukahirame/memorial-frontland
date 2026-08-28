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

            //旧コードは Length / 2 まで 2 ずつ回していたので、報酬が半分しか出ていなかった。
            //受け取り側は Length まで回っていたため、表示と実際が食い違っていた
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

        //旧コードは添字を集めてから添字で消していた。1つ消すと後ろがずれるので、
        //2つ以上クリアすると別のクエストを消していた
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
            //旧コードは一致後もループを回し続け、残り回数ぶん加算を繰り返していた
            Root rewarded = RootsManager.Roots.Find(quest.RootId);
            if (rewarded != null) rewarded.Gain(reward.Amount);
        }
        else if (reward.Kind == "coin")
        {
            TextMeshProUGUI coin = GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>();
            coin.text = (int.Parse(coin.text) + reward.Amount).ToString();
        }
        else
        {
            //旧コードは手前に IndexOf("") != -1 という常に真の分岐があり、
            //ここへは決して来なかった。アイテム報酬が配られていなかった
            transform.Find("Inventory").GetComponent<PlayerInventory>()
                     .LoadInventory(reward.Kind, reward.Amount);
        }
    }
}
