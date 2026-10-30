using UnityEngine;
using UnityEngine.UI;

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

        if (s.IndexOf("Craft") != -1) button.Find("Text").GetComponent<Text>().text = "作業台を使う";
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
    }

}
