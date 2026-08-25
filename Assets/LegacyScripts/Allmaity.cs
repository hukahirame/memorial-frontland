using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Allmaity : MonoBehaviour // Allmaityボタン
{
    [SerializeField] private RectTransform rect; //自身

    public void Prepare_Button(string s) // 引数のAllmaityボタンを用意する
    {
        int index = -1;
        for (int i = 0; i < 3; i++) { if (transform.GetChild(i).name.IndexOf(s) != -1) return; }
        for (int i = 0; i < 3; i++) { if (transform.GetChild(i).name.IndexOf("AllmaityButton") != -1) index = i; i=10; }
        Transform button = transform.GetChild(index);
        button.localScale = Vector3.one;
        button.name = s;

        var txt = button.Find("Text").GetComponent<Text>();
        if ((s.IndexOf("ST_") != -1)&&(s.IndexOf("Root") != -1)) txt.text = "根源へ移動";
        else if ((s.IndexOf("ST_") != -1) && (s.IndexOf("Main") != -1)) txt.text = "管理地へ戻る";
        else if (s.IndexOf("Craft") != -1) txt.text = "作業台を使う";
    }

    public void Trash_Button(string s)
    {
        Transform button = transform.Find(s);
        if (button == null) return;
        button.localScale = Vector3.zero;
        button.name = "AllmaityButton_" + transform.Find(s).GetSiblingIndex();
    }

    public void Allmaity_Play(int i) // ボタンが押された時
    {
        string target = transform.GetChild(i).name;
        if (target == null) { Debug.Log(target + "が見つかりません!"); return; }
        // if (target == "CraftInventory") transform.parent.Find("CraftInventory").Find("CloseButton").GetComponent<CloseInventory>().CloseClick();

        if (target.IndexOf("Inventory") != -1) transform.parent.Find(target).transform.position = rect.position;
        // インベントリ系は戻す場所が決まっているなど、まとまった処理ごとに条件文を工夫してもいい

        if (target.IndexOf("ST_") != -1)
        {
            Debug.Log(QuestManager.ordered_id);
            if(QuestManager.ordered_id == "")
            {
                GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("受注されたクエストがありません");
                return;
            }

            for (int k = 0; i < 3; i++) { Trash_Button(transform.GetChild(k).name); }

            //本来は自動生成シーンなので target.Substring(3) == roots[index][2]
            if (target.IndexOf("Roots") != -1)
            {
                int index = QuestManager.quests.FindIndex(q => q[0] == QuestManager.ordered_id);
                target = target.Replace("Roots", QuestManager.quests[index][1]);

            }

            GameManager.SceneTrans(target.Substring(3));
        }
    }

}
