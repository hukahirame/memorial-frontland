using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Allmaity : MonoBehaviour // Allmaityボタン
{
    [SerializeField] private RectTransform rect; //自身

    /// <summary>押したときの動き。ボタンの名前で引く。持たないボタン（作業台）は、名前の画面を開く</summary>
    private readonly Dictionary<string, Action> plays = new Dictionary<string, Action>();

    public void Prepare_Button(string s) // 引数のAllmaityボタンを用意する
    {
        Prepare(s, s.IndexOf("Craft") != -1 ? "作業台を使う" : null, null);
    }

    /// <summary>s の名前でボタンを出す。label はボタンの字、play は押したときの動き。出ていれば何もしない。空きが無ければ出さない</summary>
    public void Prepare(string s, string label, Action play)
    {
        Transform button = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == s) return;
            if (button == null && child.name.StartsWith("AllmaityButton")) button = child;
        }
        if (button == null) return;

        button.localScale = Vector3.one;
        button.name = s;
        if (label != null) button.Find("Text").GetComponent<Text>().text = label;
        if (play != null) plays[s] = play;
    }

    public void Trash_Button(string s)
    {
        Transform button = transform.Find(s);
        if (button == null) return;
        button.localScale = Vector3.zero;
        button.name = "AllmaityButton_" + transform.Find(s).GetSiblingIndex();
        plays.Remove(s);
    }

    public void Allmaity_Play(int i) // ボタンが押された時
    {
        string target = transform.GetChild(i).name;
        if (target == null) { Debug.Log(target + "が見つかりません!"); return; }

        if (plays.TryGetValue(target, out Action play))
        {
            play();
            return;
        }
        // if (target == "CraftInventory") transform.parent.Find("CraftInventory").Find("CloseButton").GetComponent<CloseInventory>().CloseClick();

        if (target.IndexOf("Inventory") != -1) transform.parent.Find(target).transform.position = rect.position;
        // インベントリ系は戻す場所が決まっているなど、まとまった処理ごとに条件文を工夫してもいい
    }

}
