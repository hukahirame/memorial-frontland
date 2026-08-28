using System;
using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using TMPro;

public class QuestButton : MonoBehaviour
{
    void Start()
    {
        if (gameObject.name.IndexOf("(Clone)") != -1) gameObject.name = gameObject.name.Substring(0, gameObject.name.Length - 7); //(Clone)除去
    }

    public void QuestOrder() //メインクエのみ
    {
        var s = transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

        if (s.text == "受注済み")
        {
            s.text = "非受注";
            QuestManager.ordered_id = "";
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("クエストを解除しました");
            return;
        }

        if (QuestId.Is(QuestManager.ordered_id, QuestKind.Main)) //メイン受注条件
        {
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("これ以上受注できません");
            return;
        }


        if (GameManager.entered_scene.IndexOf("Root") != -1) //根源内
        {
            if (s.text == "非受注")
            {
                s.text = "受注不可";
                Invoke("Alert", 1.5f);
            }
            else { }
        }
        else if(s.text == "達成済み") //消す
        {
        
        }
        else //管理地通常受注
        {
            s.text = "受注済み";
            QuestManager.ordered_id = gameObject.name;
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("クエストを受注しました");
        }

       // if (GameManager.entered_scene.IndexOf("Root") == -1) return;
       // transform.parent.GetComponent<QuestManager>().StartQuest(); //根源内受注用
    

    }
    private void Alert()
    {
        transform.Find("StatusText").GetComponent<TextMeshProUGUI>().text = "非受注";
    }

}
