using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.UI;

public class Info_set : MonoBehaviour
{
    [SerializeField] private Image mainImage;
    [SerializeField] private Text nametxt;
    [SerializeField] private Text txt;
    [SerializeField] private Button button;
    [SerializeField] private Sprite buttonsprite;

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
        mainImage.sprite = buttonsprite;
        nametxt.text = "";
        txt.text = "";
        button.gameObject.SetActive(false);
    }

    public void Install()
    {
        if (current == null) return;

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
        if (!Weapon.Equipped.Equip(current)) return;

        Sprite inhand = InHand(current.ItemId);
        if (inhand != null) GameObject.Find("WeaponChild").GetComponent<SpriteRenderer>().sprite = inhand;

        TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
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
        Vector3 installpos = GameObject.FindWithTag("Player").transform.position + Vector3.down * 0.4f;
        Instantiate((GameObject)Resources.Load(current.ItemId + "_obj"), installpos, Quaternion.identity);
        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(current.ItemId);
    }
}
