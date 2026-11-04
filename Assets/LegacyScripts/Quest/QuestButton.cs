using System;
using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using TMPro;

public class QuestButton : MonoBehaviour
{
    /// <summary>今いる場所。生成した QuestManager が渡す</summary>
    private Whereabouts whereabouts;

    public void Bind(Whereabouts whereabouts)
    {
        this.whereabouts = whereabouts;
    }

    void Start()
    {
        if (gameObject.name.IndexOf("(Clone)") != -1) gameObject.name = gameObject.name.Substring(0, gameObject.name.Length - 7); //(Clone)除去
    }

    /// <summary>
    /// 受注と解除。どちらも枠の表示ではなく受注の状態で決める。表示は掲示板を開いたまま移ると古いまま残る
    /// </summary>
    public void QuestOrder() //メインクエのみ
    {
        Quest quest = QuestManager.Quests.Find(gameObject.name);
        if (quest == null) return;

        //成功が決まったものは、拠点で受け取るまで受け直しも解除もしない
        if (quest.IsComplete) return;

        bool ordered = QuestManager.ordered_id == quest.Id;

        //受けるのも解除するのも、対象の根源の外でだけ。中ではもう始まっている
        if (whereabouts != null && whereabouts.RootId == quest.RootId)
        {
            Notice(ordered ? "この根源の中では解除できません" : "この根源の中では受注できません");
            return;
        }

        if (ordered)
        {
            QuestManager.ordered_id = "";
            Redraw(quest.RootId);
            Notice("クエストを解除しました");
            return;
        }

        if (QuestManager.Quests.Find(QuestManager.ordered_id) != null)
        {
            Notice("これ以上受注できません");
            return;
        }

        QuestManager.ordered_id = quest.Id;
        Redraw(quest.RootId);
        Notice("クエストを受注しました");
    }

    /// <summary>掲示板の枠を描き直す。受注中の枠の強調と、ほかの枠の「ほかを受注中」を付け直す</summary>
    private void Redraw(string rootId)
    {
        QuestManager board = GetComponentInParent<QuestManager>();
        if (board != null) board.ShowQuestUI(rootId);
    }

    private static void Notice(string text)
    {
        GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim(text);
    }

}
