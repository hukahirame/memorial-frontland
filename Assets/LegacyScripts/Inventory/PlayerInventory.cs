using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public int LoadInventory(string s, int durability)
    {
        int index = -1;

        if (s == "Speedneckless")
        {
            if (Player2.speed < 1.2f) Player2.speed = 1.3f;
            else Player2.speed *= 1.3f;
        }

        for (int i = 0; i <= items.Count-1; i++) //同名探し
        {
            if ((items[i] == s) && (stocks[i] < maxstocks[i]))
            {
                index = i;
                break;
            }

        }
        if(index != -1)
        {
            stocks[index]++;
            transform.GetChild(CHILDPLUS + index).Find("Text").GetComponent<Text>().text = stocks[index].ToString();
            return 1;
        }

        index = items.FindIndex(n => n == ""); //空いてる所探し
        if (index != -1)
        {
            items[index] = s;
            stocks[index]++;

            int i;
            for (i = 0; i < GameManager.items.Count; i++) //最大ストック数の取得
                if (GameManager.items[i][0] == s) break;
            maxstocks[index] = int.Parse(GameManager.items[i][2]);

            // ボタンへの描写処理 GetSiblingIndex←いつか使う
            Inventbutton ib = transform.GetChild(index + CHILDPLUS).GetComponent<Inventbutton>();
            ib.Ready(s, durability);

            return 1;
        }
        return -1;
    }

    public void UnloadInventory(string s) // 所持数以上消す指示出すとエラー
    {
        int index = items.FindLastIndex(n => n == s);
        stocks[index]--;
        transform.GetChild(CHILDPLUS + index).Find("Text").GetComponent<Text>().text = stocks[index].ToString();

        if (stocks[index] <= 0)
        {
            items[index] = "";
            maxstocks[index] = 0;
            transform.GetChild(CHILDPLUS + index).GetComponent<Image>().sprite = buttonsprite;
            transform.GetChild(CHILDPLUS + index).Find("Text").GetComponent<Text>().text = "";

            transform.Find("Info_set").GetComponent<Info_set>().Delete_Info();
        }


    }
}
