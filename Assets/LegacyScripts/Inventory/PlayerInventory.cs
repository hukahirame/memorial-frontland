using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class PlayerInventory : MonoBehaviour
{
    private int CHILDPLUS = 5; //Inventory直下、Boxまでのobjの個数

    public List<string> items = new List<string>();
    public List<int> stocks = new List<int>();
    public List<int> maxstocks = new List<int>();

    public GameObject wi_button; // 装備時に、ibから情報のみ代入
    [SerializeField] private Sprite buttonsprite;

    void Start()
    {
        for (int i = 0; i < 14; i++)
        {
            LoadInventory("Branch", 0);
        }
        LoadInventory("Ironsword",0);
    }

    // 格納規則は MemorialFloor.Domain.Inventory 側。ここは効果適用と表示のみ。
    // SaveData.LoadDynamic() がリスト参照ごと差し替えるため、Inventory は保持せず都度生成する。

    public int LoadInventory(string s, int durability)
    {
        if (s == "Speedneckless") Player2.speed += 0.3f; //所持しているだけで加速する

        var result = new Inventory(items, stocks, maxstocks).Add(s, () => GetMaxStock(s)); //新規配置時のみ評価される

        switch (result.Outcome)
        {
            case AddOutcome.Stacked:
                transform.GetChild(CHILDPLUS + result.SlotIndex).Find("Text").GetComponent<Text>().text = result.Stock.ToString();
                return 1;

            case AddOutcome.Placed:
                // ボタンへの描写処理 GetSiblingIndex←いつか使う
                Inventbutton ib = transform.GetChild(result.SlotIndex + CHILDPLUS).GetComponent<Inventbutton>();
                ib.Ready(s, durability);
                return 1;

            default:
                return -1;
        }
    }

    private int GetMaxStock(string s) //最大ストック数の取得
    {
        int i;
        for (i = 0; i < GameManager.items.Count; i++)
            if (GameManager.items[i][0] == s) break;
        return int.Parse(GameManager.items[i][2]);
    }

    public void UnloadInventory(string s)
    {
        if (s == "Speedneckless") Player2.speed -= 0.3f;

        var result = new Inventory(items, stocks, maxstocks).Remove(s);
        if (result.Outcome == RemoveOutcome.NotFound) return;

        transform.GetChild(CHILDPLUS + result.SlotIndex).Find("Text").GetComponent<Text>().text = result.Stock.ToString();

        if (result.Outcome == RemoveOutcome.SlotCleared)
        {
            transform.GetChild(CHILDPLUS + result.SlotIndex).GetComponent<Image>().sprite = buttonsprite;
            transform.GetChild(CHILDPLUS + result.SlotIndex).Find("Text").GetComponent<Text>().text = "";

            transform.Find("Info_set").GetComponent<Info_set>().Delete_Info();
        }
    }
}