using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        int index = -1;
        for(int i = 0; i < RootsManager.roots.Count; i++) if (RootsManager.roots[i][2] == gameObject.name) index = i; // ID

        qm.position = center.position;

        qm.Find("RootName").GetComponent<TextMeshProUGUI>().text = "「" + RootsManager.roots[index][0].ToString() + "」";
        qm.Find("RootPower").GetChild(0).GetComponent<Text>().text = "危険度 " + RootsManager.parameta[index][1].ToString();
        qm.Find("RootProgress").Find("TotalProgress").GetComponent<Text>().text = "　攻略度 " + RootsManager.parameta[index][0].ToString() + "％";
        Text dp = qm.Find("RootProgress").Find("DetailProgress").GetComponent<Text>();
        if (RootsManager.parameta[index][0] >= 50) dp.transform.Find("DP2").GetComponent<Text>().color = new Color(0, 50, 0, 255);
        if (RootsManager.parameta[index][0] >= 30) dp.color = new Color(50, 0, 50, 255);

        Text capa = qm.Find("RootCapacity").GetChild(0).GetComponent<Text>(); // 容量更新（値変化時はまた別で行う）
        capa.text = RootsManager.parameta[index][2].ToString();
        if (RootsManager.parameta[index][2] >= 100) { capa.text = "-"; capa.color = Color.black; }
        else if (RootsManager.parameta[index][2] >= 75) { capa.text = "高"; capa.color = Color.red; }
        else if (RootsManager.parameta[index][2] >= 40) { capa.text = "中"; capa.color = Color.yellow; }
        else if (RootsManager.parameta[index][2] >= 15) { capa.text = "小"; capa.color = Color.green; }
        else if (RootsManager.parameta[index][2] >= 0) { capa.text = "微"; capa.color = Color.blue; }

        // QMのUI生成プログラムを呼んで、クエスト欄（左側）の情報を入れてもらう
        qm.GetComponent<QuestManager>().ShowQuestUI(RootsManager.roots[index][2]);

    }

}
