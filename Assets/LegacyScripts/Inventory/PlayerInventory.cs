using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;
using MemorialFloor.Game;

public class PlayerInventory : MonoBehaviour, IItemReceiver
{
    private int CHILDPLUS = 5; //Inventory直下、Boxまでのobjの個数

    [Tooltip("スロット数。起動時にこの数まで空きスロットを用意する")]
    [SerializeField] private int slotCount = 15;

    /// <summary>1 段に並ぶ箱の数</summary>
    private const int BoxesPerRow = 5;

    [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();

    /// <summary>保存と復元のための口。並びは書き換えず、中身を入れ替える</summary>
    public IReadOnlyList<ItemSlot> Slots => slots;

    public GameObject wi_button; // 装備時に、ibから情報のみ代入
    [SerializeField] private Sprite buttonsprite;

    [Tooltip("枠の箱の後ろに敷く受け皿の絵。空いた箱は中身の絵が無く、敷かないとそこに箱があることが見えない")]
    [SerializeField] private Sprite slotBack;

    /// <summary>受け皿の大きさ。箱に対する比。品の絵が受け皿の縁に掛からないよう、ひと回り大きくする</summary>
    private const float SlotBackScale = 1.1f;

    /// <summary>中身が変わった。交換画面の所持数を書き直すのに使う</summary>
    public event System.Action Changed;

    /// <summary>装備している武器の入った枠に付ける印。1 つだけ作り、その枠へ付け替える</summary>
    private RectTransform equippedMark;

    /// <summary>印の字の大きさ。枠の中の大きさで、画面では枠と一緒に縮む</summary>
    private const float EquippedMarkSize = 36f;

    void Awake()
    {
        while (slots.Count < slotCount) slots.Add(new ItemSlot());

        AddBoxes();

        //枠の箱は場に多めに並べてある。使わない箱が見えると、空きがあるのに拾えないように見える
        for (int i = CHILDPLUS + slotCount; i < transform.childCount; i++)
        {
            Transform box = transform.GetChild(i);
            if (box.GetComponent<Inventbutton>() != null) box.gameObject.SetActive(false);
        }

        LayBacks();
        Changed += MarkEquipped;
    }

    //場の箱がスロット数より少なければ、1 段上の箱を写して下の段に並べる。段の間は、場の 1 段目と 2 段目の間と同じ。
    //箱は並びの番号で引くので、写した箱は今ある箱の続きの番号に差し込む
    private void AddBoxes()
    {
        int boxes = 0;
        while (CHILDPLUS + boxes < transform.childCount && transform.GetChild(CHILDPLUS + boxes).GetComponent<Inventbutton>() != null) boxes++;
        if (boxes >= slotCount || boxes < BoxesPerRow * 2) return;

        Vector2 rowStep = Box(BoxesPerRow).anchoredPosition - Box(0).anchoredPosition;
        for (int i = boxes; i < slotCount; i++)
        {
            RectTransform above = Box(i - BoxesPerRow);
            var box = (RectTransform)Instantiate(above.gameObject, transform).transform;
            box.name = above.name;
            box.SetSiblingIndex(CHILDPLUS + i);
            box.anchoredPosition = above.anchoredPosition + rowStep;
        }
    }

    private RectTransform Box(int index)
    {
        return (RectTransform)transform.GetChild(CHILDPLUS + index);
    }

    //受け皿は箱より前の並びに置き、箱の後ろに描く。箱は並びの番号で引くので、置いたぶん番号をずらす
    private void LayBacks()
    {
        if (slotBack == null) return;

        var backs = new GameObject("SlotBacks", typeof(RectTransform)).GetComponent<RectTransform>();
        backs.gameObject.layer = gameObject.layer;
        backs.SetParent(transform, false);
        backs.anchorMin = Vector2.zero;
        backs.anchorMax = Vector2.one;
        backs.offsetMin = Vector2.zero;
        backs.offsetMax = Vector2.zero;
        backs.SetSiblingIndex(CHILDPLUS);
        CHILDPLUS++;

        for (int i = 0; i < slotCount; i++)
        {
            var box = (RectTransform)transform.GetChild(CHILDPLUS + i);
            var back = new GameObject("Back", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            back.gameObject.layer = gameObject.layer;
            back.SetParent(backs, false);
            back.anchorMin = box.anchorMin;
            back.anchorMax = box.anchorMax;
            back.pivot = box.pivot;
            back.anchoredPosition = box.anchoredPosition;
            back.sizeDelta = box.sizeDelta;
            back.localScale = new Vector3(box.localScale.x * SlotBackScale, box.localScale.y * SlotBackScale, 1f);

            Image image = back.GetComponent<Image>();
            image.sprite = slotBack;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
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

    /// <summary>持っているだけで足される攻撃力。装備しない品の原簿の攻撃力を、持っている数だけ足す</summary>
    public int HeldAttack
    {
        get
        {
            int total = 0;
            foreach (ItemSlot slot in slots)
            {
                ItemDefinition item = GameManager.Items.Find(slot.ItemId);
                if (item != null && item.Use != ItemUse.Equip) total += item.Attack * slot.Stock;
            }

            return total;
        }
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
        MarkEquipped();
    }

    /// <summary>
    /// 装備している武器の入った枠に「装備中」の印を付ける。同じ武器が 2 つあれば前の枠に付ける。
    /// 持ち物が変わったときと、装備を替えたとき（Info_set）に付け直す
    /// </summary>
    public void MarkEquipped()
    {
        string weapon = Weapon.Equipped.ItemId;
        int index = string.IsNullOrEmpty(weapon) ? -1 : slots.FindIndex(slot => slot.ItemId == weapon);

        if (index < 0 || index >= slotCount)
        {
            if (equippedMark != null) equippedMark.gameObject.SetActive(false);
            return;
        }

        var box = (RectTransform)transform.GetChild(CHILDPLUS + index);
        if (equippedMark == null) equippedMark = KeyHint.Badge("EquippedMark", box, "装備中", Vector2.up, EquippedMarkSize);

        equippedMark.SetParent(box, false);
        equippedMark.gameObject.SetActive(true);
    }

    /// <summary>持ち物にある武器を持つ。手の絵も替える（Info_set）。持っていない品と装備できない品は持たない</summary>
    public bool Wield(string itemId)
    {
        if (Stock(itemId) <= 0) return false;

        return transform.Find("Info_set").GetComponent<Info_set>().Wield(itemId);
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