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

    /// <summary>中身が変わった。交換画面の所持数を書き直すのに使う</summary>
    public event System.Action Changed;

    void Awake()
    {
        while (slots.Count < slotCount) slots.Add(new ItemSlot());

        //枠の箱は場に多めに並べてある。使わない箱が見えると、空きがあるのに拾えないように見える
        for (int i = CHILDPLUS + slotCount; i < transform.childCount; i++)
        {
            Transform box = transform.GetChild(i);
            if (box.GetComponent<Inventbutton>() != null) box.gameObject.SetActive(false);
        }
    }

    /// <summary>読み込んだ内容で埋め直す。参照は差し替えない。枠の絵と首飾りの加速も中身に合わせ直す</summary>
    public void ReplaceSlots(List<ItemSlot> loaded)
    {
        int necklaces = Stock("Speedneckless");

        slots.Clear();
        if (loaded != null) slots.AddRange(loaded);

        while (slots.Count < slotCount) slots.Add(new ItemSlot());

        Player2.speed += (Stock("Speedneckless") - necklaces) * 0.3f;
        for (int i = 0; i < slotCount; i++) Redraw(i);
        transform.Find("Info_set").GetComponent<Info_set>().Delete_Info();
        Changed?.Invoke();
    }

    private int Stock(string itemId)
    {
        int total = 0;
        foreach (ItemSlot slot in slots)
        {
            if (slot.ItemId == itemId) total += slot.Stock;
        }

        return total;
    }

    /// <summary>枠の箱を中身どおりに描き直す。耐久は保存しないので出さない</summary>
    private void Redraw(int index)
    {
        Transform box = transform.GetChild(CHILDPLUS + index);
        Inventbutton ib = box.GetComponent<Inventbutton>();
        ItemSlot slot = slots[index];

        ib.Clear();
        if (string.IsNullOrEmpty(slot.ItemId))
        {
            box.GetComponent<Image>().sprite = buttonsprite;
            box.Find("Text").GetComponent<Text>().text = "";
            return;
        }

        ib.Ready(slot.ItemId, 0);
        box.Find("Text").GetComponent<Text>().text = slot.Stock.ToString();
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

        //鉄の剣は手に持って始まる。手の絵はシーンで鉄の剣にしてあり、素手のままだと絵と強さが食い違う
        Weapon.Equipped.Equip(GameManager.Items.Find("Ironsword"));
    }

    // 格納規則は MemorialFloor.Domain.Inventory 側。ここは効果適用と表示のみ。

    public int LoadInventory(string s, int durability)
    {
        var result = Inventory.Add(s, () => GetMaxStock(s)); //新規配置時のみ評価される

        switch (result.Outcome)
        {
            case AddOutcome.Stacked:
                transform.GetChild(CHILDPLUS + result.SlotIndex).Find("Text").GetComponent<Text>().text = result.Stock.ToString();
                break;

            case AddOutcome.Placed:
                // ボタンへの描写処理 GetSiblingIndex←いつか使う
                Inventbutton ib = transform.GetChild(result.SlotIndex + CHILDPLUS).GetComponent<Inventbutton>();
                ib.Ready(s, durability);
                break;

            default:
                return -1;
        }

        //所持しているだけで加速する。入ったときだけ上げる。入らなかったものは地面に残るので、先に上げると触れ直すたびに積み上がる
        if (s == "Speedneckless") Player2.speed += 0.3f;
        Changed?.Invoke();
        return 1;
    }

    private int GetMaxStock(string s) //最大ストック数の取得
    {
        ItemDefinition item = GameManager.Items.Find(s);

        return item == null ? 0 : item.MaxStock;
    }

    public void UnloadInventory(string s)
    {
        var result = Inventory.Remove(s);
        if (result.Outcome == RemoveOutcome.NotFound) return;

        if (s == "Speedneckless") Player2.speed -= 0.3f;

        transform.GetChild(CHILDPLUS + result.SlotIndex).Find("Text").GetComponent<Text>().text = result.Stock.ToString();

        if (result.Outcome == RemoveOutcome.SlotCleared)
        {
            transform.GetChild(CHILDPLUS + result.SlotIndex).GetComponent<Image>().sprite = buttonsprite;
            transform.GetChild(CHILDPLUS + result.SlotIndex).Find("Text").GetComponent<Text>().text = "";
            transform.GetChild(CHILDPLUS + result.SlotIndex).GetComponent<Inventbutton>().Clear();

            transform.Find("Info_set").GetComponent<Info_set>().Delete_Info();
        }

        Changed?.Invoke();
    }
}