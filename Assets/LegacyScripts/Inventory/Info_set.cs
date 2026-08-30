using System.Collections;
using System.Collections.Generic;
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
    private int index = -1; 

    void Start()
    {
        button.gameObject.SetActive(false);
    }

    public int Show_Info2(string s)　// アイテム情報フレームへの代入
    {
        index = -1;
        for (int i = 0; index == -1; i++)
            if (GameManager.items[i][0] == s) index = i;
        nametxt.text = GameManager.items[index][1];
        txt.text = GameManager.items[index][10];
        mainImage.sprite = Resources.Load<Sprite>(s);

        if (int.Parse(GameManager.items[index][4]) == 1)
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
            var o = Instantiate((GameObject)Resources.Load(GameManager.items[index][0] + "_obj"), installpos, Quaternion.identity);
          //  SceneStarter.saveobjects.Add(new string[] { o.name.Substring(0,o.name.Length-7), GameManager.entered_scene, installpos.x.ToString(), installpos.y.ToString(), installpos.z.ToString() });
            GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(GameManager.items[index][0]);
        }
    }
}
