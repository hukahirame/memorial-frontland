using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;

public class Info_set : MonoBehaviour
{
    [SerializeField] private Image mainImage;
    [SerializeField] private Text nametxt;
    [SerializeField] private Text txt;
    [SerializeField] private Button button;
    [SerializeField] private Sprite buttonsprite;
    [SerializeField] private PlacedObjects placed;

    [Header("手に持つ絵。原簿の ID と対で持つ")]
    [SerializeField] private string weapon1Id = "Ironsword";
    [SerializeField] private Sprite weapon1;
    [SerializeField] private string weapon2Id = "Legendsword";
    [SerializeField] private Sprite weapon2;

    private ItemDefinition current;

    void Start()
    {
        button.gameObject.SetActive(false);
    }

    public int Show_Info2(string s)　// アイテム情報フレームへの代入
    {
        current = GameManager.Items.Find(s);
        if (current == null) return 0;

        nametxt.text = current.Name;
        txt.text = current.Description;
        mainImage.sprite = Resources.Load<Sprite>(s);

        // 非表示のボタンからも文字を取る。既定の GetComponentInChildren は
        // 活性なものしか返さず、設置できないアイテムを選ぶと null になっていた
        button.GetComponentInChildren<Text>(true).text = Label(current.Use);
        button.gameObject.SetActive(current.Installable);

        return 1;
    }

    private static string Label(ItemUse use)
    {
        switch (use)
        {
            case ItemUse.Eat: return "食べる";
            case ItemUse.Equip: return "装備";
            default: return "設置";
        }
    }

    public void Delete_Info()
    {
        current = null;
        mainImage.sprite = buttonsprite;
        nametxt.text = "";
        txt.text = "";
        button.gameObject.SetActive(false);
    }

    public void Install()
    {
        if (current == null) return;

        //倒れている間と場所を移っている間は使えない。倒れていると食べても体力は戻らず、移る途中に置いた物は前の場所と一緒に消える
        if (!GameObject.FindWithTag("Player").GetComponent<Player2>().CanAct) return;

        switch (current.Use)
        {
            case ItemUse.Eat: Eat(); break;
            case ItemUse.Equip: Equip(); break;
            default: Place(); break;
        }
    }

    private void Eat()
    {
        Player2.Hp.Heal(current.Heal);
        Player2.RefreshHpView();
        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(current.ItemId);
    }

    private void Equip()
    {
        if (!Wield(current.ItemId)) return;

        TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
    }

    /// <summary>武器を持つ。手の絵を替え、持ち物の「装備中」の印も付け直す。保存から戻すときも使う。装備できない品なら何もせず false</summary>
    public bool Wield(string itemId)
    {
        if (!Weapon.Equipped.Equip(GameManager.Items.Find(itemId))) return false;

        Sprite inhand = InHand(itemId);
        if (inhand != null) GameObject.Find("WeaponChild").GetComponent<SpriteRenderer>().sprite = inhand;

        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().MarkEquipped();
        return true;
    }

    /// <summary>手に持つ絵。原簿には置けないため Inspector で ID と対にしてある</summary>
    private Sprite InHand(string itemId)
    {
        if (itemId == weapon1Id) return weapon1;
        if (itemId == weapon2Id) return weapon2;

        return null;
    }

    private void Place()
    {
        Player2 player = GameObject.FindWithTag("Player").GetComponent<Player2>();
        Vector3 installpos = player.transform.position + Vector3.down * 0.4f;
        float yaw = 0f;

        //柵は足元ではなく、向いている側の格子の線に立てる。地面が無い所や、動かない物に重なる所には立てない
        GameObject prefab = PlacedObjects.Prefab(current.ItemId);
        if (prefab != null && prefab.TryGetComponent(out Fence fence)
            && !FenceSite.TryFind(fence, player.Body, player.Side, out installpos, out yaw))
        {
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("ここには置けません");
            return;
        }

        if (!placed.Place(current.ItemId, installpos, yaw)) return;
        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(current.ItemId);
    }
}
