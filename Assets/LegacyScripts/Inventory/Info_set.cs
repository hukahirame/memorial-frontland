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

    [SerializeField] private Sprite weapon1;
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

        if (current.Installable)
            button.gameObject.SetActive(true); // 設置ボタン
        else button.gameObject.SetActive(false);

        if (nametxt.text== "スライムゼリー") button.GetComponentInChildren<Text>().text = "食べる";
        if (nametxt.text.IndexOf("剣") != -1) button.GetComponentInChildren<Text>().text = "装備";
        else button.GetComponentInChildren<Text>().text = "設置";

        return 1;
    }

    public void Delete_Info()
    {
        mainImage.sprite = buttonsprite;
        nametxt.text = "";
        txt.text = "";
        button.gameObject.SetActive(false);
    }

    private const int SlimejellyHeal = 5;

    public void Install()
    {
        if (nametxt.text == "スライムゼリー")
        {
            Player2.Hp.Heal(SlimejellyHeal);
            Player2.RefreshHpView();
            GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory("Slimejelly");
        }
        else if (nametxt.text.IndexOf("鉄") != -1) 
        {
            GameObject.Find("WeaponChild").GetComponent<SpriteRenderer>().sprite = weapon1;
            Weapon.power = 40;
            TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
        }
        else if (nametxt.text.IndexOf("伝") != -1)
        {
            GameObject.Find("WeaponChild").GetComponent<SpriteRenderer>().sprite = weapon2;
            Weapon.power = 999;
            TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
        }
        else //設置
        {
            Vector3 installpos = GameObject.FindWithTag("Player").transform.position + Vector3.down * 0.4f;
            var o = Instantiate((GameObject)Resources.Load(current.ItemId + "_obj"), installpos, Quaternion.identity);
          //  SceneStarter.saveobjects.Add(new string[] { o.name.Substring(0,o.name.Length-7), GameManager.entered_scene, installpos.x.ToString(), installpos.y.ToString(), installpos.z.ToString() });
            GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(current.ItemId);
        }
    }
}
