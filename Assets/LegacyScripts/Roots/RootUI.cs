using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class RootUI : MonoBehaviour
{
    /// <summary>達していない段の色。シーンに置いたときの色</summary>
    private static readonly Color Unreached = new Color(0.47058824f, 0.47058824f, 0.47058824f, 1f);

    private Transform qm;
    private Transform center;

    private void Start()
    {
        qm = GameObject.FindWithTag("QuestManager").transform;
        center = transform.parent.parent.Find("CenterPoint");
    }

    public void Show_QM()
    {
        Root root = RootsManager.Roots.Find(gameObject.name); // UI の名前が根源 ID
        if (root == null) return;

        qm.position = center.position;

        qm.Find("RootName").GetComponent<TextMeshProUGUI>().text = "「" + root.Name + "」";
        qm.Find("RootPower").GetChild(0).GetComponent<Text>().text = "危険度 " + root.Danger;
        qm.Find("RootProgress").Find("TotalProgress").GetComponent<Text>().text = "　攻略度 " + root.Progress + "％";
        //詳細の枠は根源どうしで使い回す。達していない段も塗り直さないと、前に開いた根源の色が残る
        Text dp = qm.Find("RootProgress").Find("DetailProgress").GetComponent<Text>();
        dp.transform.Find("DP2").GetComponent<Text>().color =
            root.Progress >= SpawnRule.SuppressProgress ? new Color(0, 50, 0, 255) : Unreached;
        dp.color = root.Progress >= SpawnRule.WeakenProgress ? new Color(50, 0, 50, 255) : Unreached;

        Text capa = qm.Find("RootCapacity").GetChild(0).GetComponent<Text>(); // 開いたときの値。開いているあいだは更新しない
        capa.text = root.Accumulation.ToString();
        switch (root.Level) //閾値は Domain の AccumulationLevel が持つ
        {
            case AccumulationLevel.Stampede: capa.text = "-"; capa.color = Color.black;  break;
            case AccumulationLevel.High:     capa.text = "高"; capa.color = Color.red;    break;
            case AccumulationLevel.Medium:   capa.text = "中"; capa.color = Color.yellow; break;
            case AccumulationLevel.Small:    capa.text = "小"; capa.color = Color.green;  break;
            default:                         capa.text = "微"; capa.color = Color.blue;   break;
        }

        // QMのUI生成プログラムを呼んで、クエスト欄（左側）の情報を入れてもらう
        qm.GetComponent<QuestManager>().ShowQuestUI(root.Id);

    }

}
