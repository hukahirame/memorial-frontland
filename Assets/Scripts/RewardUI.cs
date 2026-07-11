using UnityEngine;
using TMPro;
using System;

public class RewardUI : MonoBehaviour
{
    public static int rewardUI_show = 0; //0：非表示　1：失敗　2：成功
    public static int rewardUI_index = -1; //roots[r_i][i]
    [SerializeField] TextMeshProUGUI toptxt;
    [SerializeField] TextMeshProUGUI sidetxt;
    [SerializeField] TextMeshProUGUI resulttxt;
    public void InputInfo()
    {
        toptxt.text = "「" + RootsManager.roots[rewardUI_index][0] + "」のクエスト結果";
        sidetxt.text = "◎危険度：" + RootsManager.parameta[rewardUI_index][1] + "\n\n";

        if (rewardUI_show == 1)
        {

        }
        else if (rewardUI_show == 2)
        {
            resulttxt.text = "成功";
            resulttxt.color = new Color(170, 0, 0, 255);

            int now_progress = RootsManager.parameta[rewardUI_index][0];
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
                    int index_rm = -1;
                    for (int j = 0; j < RootsManager.roots.Count; j++)
                    {
                        if (RootsManager.roots[j][2] == QuestManager.quests[index][1]) index_rm = j;
                        if (index_rm != -1) RootsManager.parameta[index_rm][0] += int.Parse(QuestManager.rewards[index][i + 1]);
                    }
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
