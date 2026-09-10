using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;
using MemorialFloor.Game;

public class PlayerInventory : MonoBehaviour, IItemReceiver
{
    private int CHILDPLUS = 5; //Inventory直下、Boxまでのobjの個数

    [Tooltip("スロット数。起動時にこの数まで空きスロットを用意する")]
    [SerializeField] private int slotCount = 5;

    [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();

    /// <summary>保存と復元のための口。並びは書き換えず、中身を入れ替える</summary>
    public IReadOnlyList<ItemSlot> Slots => slots;

    public GameObject wi_button; // 装備時に、ibから情報のみ代入
    [SerializeField] private Sprite buttonsprite;

    void Awake()
    {
        while (slots.Count < slotCount) slots.Add(new ItemSlot());
    }

    /// <summary>読み込んだ内容で埋め直す。参照は差し替えない</summary>
    public void ReplaceSlots(List<ItemSlot> loaded)
    {
        slots.Clear();
        if (loaded != null) slots.AddRange(loaded);

        while (slots.Count < slotCount) slots.Add(new ItemSlot());
    }

    /// <summary>格納規則。並びを直接書き換える</summary>
    public Inventory Inventory => new Inventory(slots);

    void Start()
    {
        for (int i = 0; i < 14; i++)
        {
            LoadInventory("Branch", 0);
        }
        LoadInventory("Ironsword",0);
    }

    // 格納規則は MemorialFloor.Domain.Inventory 側。ここは効果適用と表示のみ。

    public int LoadInventory(string s, int durability)
    {
        if (s == "Speedneckless") Player2.speed += 0.3f; //所持しているだけで加速する

        var result = Inventory.Add(s, () => GetMaxStock(s)); //新規配置時のみ評価される

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
        ItemDefinition item = GameManager.Items.Find(s);

        return item == null ? 0 : item.MaxStock;
    }

    public void UnloadInventory(string s)
    {
        if (s == "Speedneckless") Player2.speed -= 0.3f;

        var result = Inventory.Remove(s);
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