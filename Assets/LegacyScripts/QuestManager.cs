using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour // タスク風にクエ管理。成功判定も行う
{
    public static List<string[]> quests = new List<string[]>(); // ID、対象根源、対象、目標量、現在量
    public static List<string[]> rewards = new List<string[]>(); //報酬群。（クエ数）行（報酬の種類数 ×2）列

    // entered_sceneはGameManagerにあります
    public static string ordered_id = ""; // クエ中不変。QBから受注
   // private int pluschild_qm = 6;

    [SerializeField] private Text progress;
    [SerializeField] private GameObject questUI;

    void Start()
    {
        CreateQuest("X", "Root1", "MainSpawner", "1");
        rewards.Add(new string[] {"coin", "100", "progress", "15"});
        CreateQuest("X", "Root2", "MainSpawner", "1");
        rewards.Add(new string[] { "coin", "200", "progress", "15" });
    }

    public static void CreateQuest(string type, string root, string target, string amount)
    {
        // タイプ、クエ対象根源ID(又はCommon)、対象、目標量

        int count = quests.Count(q => q[0].Contains(type));
        /*
        for (int i = 0; i < quests.Count; i++) //唯一のIDを調べる
        {
            string j = i.ToString();
            if (quests.FindIndex(quest => quest[0] == (type + j) ) == -1)
            {
                type += j;
                break;
            }
        }*/
        quests.Add(new string[] { type + count.ToString(), root, target, amount, "0" });
    }

    public void ShowQuestUI(string root) //QuestUI(ボタン)の生成 + 情報入力
    {
        //----------------書き直し
        int n = 0;
        int[] result = new int[quests.Count];
        string type = "Y";
        Array.Fill(result, -1);
        /*
        for (int t = 0; t < 4; t++) //表示順ソート：result[0]にY、result[1]にX、その後にSが続く
        {
            for (int r = 0; r < quests.Count; r++)
            {
                if ((quests[r][1] == root) && (quests[r][0].IndexOf(type) != -1)) //対象根源確認＋type抽出
                {
                    result[n] = r;
                    n++;
                }
            }
            if (t == 0) type = "X"; //Xは２つ以上存在できない
            if (t == 1) type = "S";
        }
        n = 0;
        */
        Debug.Log("クエスト数:" + quests.Count);
        for(int t = 0; (n < 4 && t < 10); t++) //4個発見 or 表示Sがなくなる で終了
        {
            bool ToNext = true;
            for (int r = 0; r < quests.Count; r++)
            {
                Debug.Log("表示クエ：type " +type + " 場所 "+ quests[r][1] + " 正誤 " + quests[r][0].IndexOf(type) +" 非既出 "+ !Array.Exists(result, x => x == r));
                if ((quests[r][1] == root) && (quests[r][0].IndexOf(type) != -1) && !Array.Exists(result, x => x == r)) //対象根源、type、非既出
                {
                    result[n] = r;
                    Debug.Log("ShowQuestUI_result[" + n + "]　" + result[n]);
                    n++;
                    ToNext = false;
                }
            }
            if(ToNext)
            {
                if (type == "Y") type = "X";
                else if (type == "X") type = "S";
                else if(type == "S") break;
            }
        }

        for (int i = 0; i < n; i++) //resultの個数分、UI生成
        {
            GameObject obj = Instantiate(questUI, transform.Find("QuestTopPosition").GetChild(n-1).position, Quaternion.identity, transform);
            obj.name = quests[result[i]][0];
            // obj.transform.Find("HeadImage").GetComponent<Image>().sprite = Resources.Load<Sprite>("HeadImage_"+quests[result[i]][0].SubString(0,1));
            var slider = transform.Find(quests[result[i]][0]).Find("Slider_IconType_03_basic_WhiteFill").GetComponent<Slider>();
            slider.maxValue = float.Parse(quests[result[i]][3]);
            slider.value = float.Parse(quests[result[i]][4]);

            TextMeshProUGUI t = obj.transform.Find("TypeText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI b = obj.transform.Find("BodyText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI r = obj.transform.Find("RewardText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI s = obj.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

            if (quests[result[i]][0].IndexOf("X") != -1)
            {
                t.text = "【調査クエスト】";
                b.text += "根源を探し出し、破壊する"; //クエ目的記入
            }
            else if (quests[result[i]][0].IndexOf("Y") != -1) t.text = "【決壊クエスト】";
            else if (quests[result[i]][0].IndexOf("S") != -1)
            {
                t.text = "【サブクエスト】";
                b.text += quests[result[i]][2] + "を" + quests[result[i]][3] + "体討伐する"; //クエ目的記入
            }


            for (int j = 0; j < rewards[result[i]].Length; j++)
            {
                if (j % 2 == 0) r.text += rewards[result[i]][j] + " x ";
                else r.text += rewards[result[i]][j] + "\n";

                if (rewards[result[i]][j] == "progress") r.text.Replace("progress","攻略度");
            } //クエ報酬記入

            if (obj.name.IndexOfAny(new char[] { 'S', 'C' }) != -1)
            {
                if (obj.name.IndexOf("S") != -1) t.text = "【サブクエスト】";
                else t.text = "【共通クエスト】";

                obj.GetComponent<Button>().interactable = false;
                s.text = "";
            }
            else if (ordered_id == quests[result[i]][0])
            {
                if (quests[i][4] == quests[i][3]) s.text = "達成済み"; 
                else if (GameManager.entered_scene == quests[result[i]][1]) s.text = "クエスト中";
                else s.text = "受注済み";
            }
           
        }
    }

    public void StartQuest() //クエ受注後に侵入で開始。受注処理はQB。メインのみ
    {
        //SceneStarterより起動
        int index = -1;
        for (int i = 0; i < quests.Count; i++) { if (quests[i][0] == ordered_id) index = i; }
        if (index == -1) return;

       string bigstr = "「" + RootsManager.roots.Find(root => root[2] == quests[index][1])[0] + "」調査クエスト\n\n";
        bigstr += "根源を探し出し、破壊する";
        if (ordered_id.IndexOf("Y") == 0) bigstr.Replace("調査", "決壊");

        RewardUI.rewardUI_index = index;
        transform.parent.Find("BigText").gameObject.GetComponent<BigText>().Bigtxt_Anim(bigstr, "開始");

        //クエスト物生成
    }

    public void SyncQuest(string target) // クエ進捗追加、外部から呼ぶ
    {
        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i][2] != target) { }
            else if ((quests[i][0].IndexOf("S") != -1) && (quests[i][1] == GameManager.entered_scene)) //サブクエ
            {
                SyncQuestSub(i, "S");
            }
            else if (quests[i][0].IndexOf("C") != -1) //共通
            {
                SyncQuestSub(i, "C");
            }
            else if (quests[i][0] == ordered_id) //メインクエ
            {
                SyncQuestSub(i, ordered_id.Substring(0,1));
            }
        }
    }

    public void SyncQuestSub(int i, string type)
    {
        quests[i][4] = (int.Parse(quests[i][4]) + 1).ToString();

        if (int.Parse(quests[i][4]) >= int.Parse(quests[i][3])) //達成処理
        {
            quests[i][4] = quests[i][3];
            var t = GameObject.FindWithTag("QuestManager").transform.Find(quests[i][0]);
           // t.Find("ClearStamp").localScale = new Vector3(1, 1, 1);
           // t.Find("StatusText").GetComponent<TextMeshProUGUI>().text = "達成済み";

            if((type == "X") || (type == "Y"))
            {
                string txt = "「" + RootsManager.roots.Find(root => root[2] == quests[i][1])[0] + "」調査クエスト\n\n";
                txt += "根源を探し出し、破壊する";
                if (type == "Y") txt = txt.Replace("調査","決壊");
                transform.parent.Find("BigText").gameObject.GetComponent<BigText>().Bigtxt_Anim(txt,"達成");

                RewardUI.rewardUI_show = 2; //帰還後UI表示フラグ
            }
        }
    }

    //FinQuest()はRewardUIへ移行しました

    private void ClearUI(int index, string type)
    {
        Transform ui;
        if(type == "Common") { ui = transform.parent.Find("Task").Find(quests[index][0]); }
        else { ui = transform.Find(quests[index][0]); }
        Destroy(ui);
    }
}
