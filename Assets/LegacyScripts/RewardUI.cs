using UnityEngine;
using TMPro;
using System;

public class RewardUI : MonoBehaviour
{
    public static int rewardUI_show = 0; //0：非表示　1：失敗　2：成功
    public static int rewardUI_index = -1; //quests の添字。QuestManager が入れる
    [SerializeField] TextMeshProUGUI toptxt;
    [SerializeField] TextMeshProUGUI sidetxt;
    [SerializeField] TextMeshProUGUI resulttxt;
    public void InputInfo()
    {
        //rewardUI_index は quests の添字。旧コードは同じ値で roots も引いており、
        //クエスト数が根源数を超えると別の根源を指すか例外になっていた
        if (rewardUI_index < 0 || rewardUI_index >= QuestManager.quests.Count) return;

        var root = RootsManager.Roots.Find(QuestManager.quests[rewardUI_index][1]);
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
            for (int i = 0; i < QuestManager.rewards[rewardUI_index].Length / 2; i += 2)
            {
                sidetxt.text += "・" + QuestManager.rewards[rewardUI_index][i];
                sidetxt.text += "　x　" + QuestManager.rewards[rewardUI_index][i + 1] + "\n";
            }
            FinQuest();
        }

        rewardUI_show = 0;
    }

    public void FinQuest() // 帰還後の処理
    {
        int[] index_array = new int[16]; //クリア済みクエスト群
        Array.Fill(index_array, -1);

        int n = 0;
        for (int i = 0; i < QuestManager.quests.Count; i++)
        {
            if (int.Parse(QuestManager.quests[i][4]) >= int.Parse(QuestManager.quests[i][3]))
            {
                index_array[n] = i;
                Debug.Log("クエストクリア：" + QuestManager.quests[i][0]);
                n++;
            }
        }

        if (n == 0)
        {
            Debug.Log("クリアされたクエストなし");
            return;
        }

        for (int amount = 0; amount < n; amount++) // 複数クリアも含む
        {
            int index = index_array[amount];

            for (int i = 0; i < QuestManager.rewards[index].Length; i += 2) // 報酬受け取り
            {
                if (QuestManager.rewards[index][i].IndexOf("progress") != -1)
                {
                    //旧コードは一致後もループを回し続け、残り回数ぶん加算を繰り返していた
                    var rewarded = RootsManager.Roots.Find(QuestManager.quests[index][1]);
                    if (rewarded != null) rewarded.Gain(int.Parse(QuestManager.rewards[index][i + 1]));
                }
                else if (QuestManager.rewards[index][i].IndexOf("coin") != -1)
                {
                    //GameManager.coin.text = (int.Parse(QuestManager.rewards[index][i + 1]) + int.Parse(GameManager.coin.text)).ToString();
                    var coin = GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>();
                    coin.text = (int.Parse(coin.text) + int.Parse(QuestManager.rewards[index][i + 1])).ToString(); 
                }
                else if (QuestManager.rewards[index][i].IndexOf("") != -1)
                {
                    // 空の条件処理
                }
                else
                {
                    transform.Find("Inventory").GetComponent<PlayerInventory>().LoadInventory(QuestManager.rewards[index][i], int.Parse(QuestManager.rewards[index][i + 1]));
                }
            }

            QuestManager.ordered_id = "";

            Debug.Log("クリア＆リスト破壊対象：" + QuestManager.quests[index][0] + "　　" + QuestManager.rewards[index][0]);
            GameObject clearedUI = GameObject.Find(QuestManager.quests[index][0]).gameObject;
            if (clearedUI != null) Destroy(clearedUI);
            
            QuestManager.quests.RemoveAt(index);
            QuestManager.rewards.RemoveAt(index);
        }
    }

}
