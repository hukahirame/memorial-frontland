using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class RootUI : MonoBehaviour
{
    private Transform qm;
    private Transform center;

    private void Start()
    {
        qm = GameObject.FindWithTag("QuestManager").transform;
        center = transform.parent.parent.Find("CenterPoint");
    }

    public void Show_QM()
    {
        var root = RootsManager.Roots.Find(gameObject.name); // UI の名前が根源 ID
        if (root == null) return;

        qm.position = center.position;

        qm.Find("RootName").GetComponent<TextMeshProUGUI>().text = "「" + root.Name + "」";
        qm.Find("RootPower").GetChild(0).GetComponent<Text>().text = "危険度 " + root.Danger;
        qm.Find("RootProgress").Find("TotalProgress").GetComponent<Text>().text = "　攻略度 " + root.Progress + "％";
        Text dp = qm.Find("RootProgress").Find("DetailProgress").GetComponent<Text>();
        if (root.Progress >= 50) dp.transform.Find("DP2").GetComponent<Text>().color = new Color(0, 50, 0, 255);
        if (root.Progress >= 30) dp.color = new Color(50, 0, 50, 255);

        Text capa = qm.Find("RootCapacity").GetChild(0).GetComponent<Text>(); // 容量更新（値変化時はまた別で行う）
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
