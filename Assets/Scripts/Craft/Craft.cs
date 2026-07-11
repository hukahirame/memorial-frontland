using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Craft : MonoBehaviour//Craftchildから実行。対応するsetを出す
{
    public PlayerInventory pi;
    public List<string[]> crafts = new List<string[]>(); //これと下2行は別ファイル化

    private int[] supply = new int[4]; //計算用

    public GameObject set;
    public TextAsset craftdata;

    public Text material1;
    public Text demand1;
    public Image Image1;
    public Text supply1;

    public Text material2;
    public Text demand2;
    public Image Image2;
    public Text supply2;

    public Text material3;
    public Text demand3;
    public Image Image3;
    public Text supply3;

    public Text material4;
    public Text demand4;
    public Image Image4;
    public Text supply4;

    public Transform matset;

    private string target1;
    private string target2;
    private string target3;
    private string target4;
    private string craftobject;
    [SerializeField] private GameObject dropcapsule;

    void Start()
    {
        StringReader reader = new StringReader(craftdata.text); // TextAssetをStringReaderに変換
        while (reader.Peek() != -1)
        {
            string line = reader.ReadLine(); // 1行ずつ読み込む
            crafts.Add(line.Split(',')); // itemsリストに追加する
        }
    }

    public void Craftprepare2(string showname)
    {
        set.GetComponent<Craft_set>().Put_Info(showname);
        Put_Materials(showname);
    }

    public void CraftStart()
    {
        if(CraftPermit() == 1)
        {
            for (int i = 0; i < int.Parse(demand1.text); i++) pi.UnloadInventory(target1);
            if (matset.GetChild(5).gameObject.activeSelf == true)
                for (int i = 0; i < int.Parse(demand2.text); i++) pi.UnloadInventory(target2);
            if (matset.GetChild(10).gameObject.activeSelf == true)
                for (int i = 0; i < int.Parse(demand3.text); i++) pi.UnloadInventory(target3);
            if (matset.GetChild(15).gameObject.activeSelf == true)
                for (int i = 0; i < int.Parse(demand4.text); i++) pi.UnloadInventory(target4);
            GameObject drop = Instantiate(dropcapsule,GameObject.FindWithTag("CraftBench").transform.position + new Vector3(0,0.5f,0), Quaternion.identity);
            drop.gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(craftobject);
            Put_Materials(craftobject);
        }
        else
        {
            Debug.Log("材料が足りません!!");
        }
    }

    private void Put_Materials(string s) // 完成品の材料に関する情報をアップロード
    {
        int index = -1;
        for (int i = 0; i < 4; i++) supply[i] = 0;

        for (int i = 0; i < crafts.Count; i++) { if (crafts[i][0] == s) { index = i; break; } }
        if (index == -1) return;
        /*
        for (int i = 0; index == -1; i++)
            if (crafts[i][0] == s) index = i;
        */
        craftobject = crafts[index][0];

        //craftdata.xlsxに、要求名称と、要求システム内名称の2つがいるので、material"n".text = crafts[index][3n-1]
        material1.text = crafts[index][2];
        demand1.text = crafts[index][3];
        Image1.sprite = Resources.Load<Sprite>(crafts[index][1]);
        var p1 = pi.items.FindAll(n => n == crafts[index][1]);
        foreach (var sp in p1) supply[0] += pi.stocks[pi.items.IndexOf(sp)];
        supply1.text = supply[0].ToString();
        target1 = crafts[index][1];

        //for (int d = 5; d < 20; d++) matset.transform.GetChild(d).gameObject.SetActive(false);
        if (craftobject == "Torch") for (int d = 5; d < 20; d++) matset.transform.GetChild(d).gameObject.SetActive(false);
        else for (int d = 5; d < 10; d++) matset.transform.GetChild(d).gameObject.SetActive(true);




        if (crafts[index][4] == null) for (int d = 5; d < 20; d++) matset.transform.GetChild(d).gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(crafts[index][4]))
        {
            material2.text = crafts[index][5];
            demand2.text = crafts[index][6];
            Image2.sprite = Resources.Load<Sprite>(crafts[index][4]);
            var p2 = pi.items.FindAll(n => n == crafts[index][4]);
            foreach (var sp in p2) supply[1] += pi.stocks[pi.items.IndexOf(sp)];
            supply2.text = supply[1].ToString();
            target2 = crafts[index][4];

        }
        if (crafts[index][7] == null) for (int d = 10; d < 20; d++) matset.transform.GetChild(d).gameObject.SetActive(false);
        material3.text = crafts[index][8];
        demand3.text = crafts[index][9];
        Image3.sprite = Resources.Load<Sprite>(crafts[index][7]);
        var p3 = pi.items.FindAll(n => n == crafts[index][7]);
        foreach (var sp in p3) supply[2] += pi.stocks[pi.items.IndexOf(sp)];
        supply3.text = supply[2].ToString();
        target3 = crafts[index][7];

        if (crafts[index][10] == null) for (int d = 15; d < 20; d++) matset.transform.GetChild(d).gameObject.SetActive(false);
        material4.text = crafts[index][11];
        demand4.text = crafts[index][12];
        Image4.sprite = Resources.Load<Sprite>(crafts[index][10]);
        var p4 = pi.items.FindAll(n => n == crafts[index][10]);
        foreach (var sp in p4) supply[3] += pi.stocks[pi.items.IndexOf(sp)];
        supply4.text = supply[3].ToString();
        target4 = crafts[index][10];

    }

    int CraftPermit()
    {
        if (craftobject == null) return 0;
        if (int.Parse(supply1.text) < int.Parse(demand1.text)) return 0;
        if (matset.GetChild(5).gameObject.activeSelf == false) return 1;
        if (int.Parse(supply2.text) < int.Parse(demand2.text)) return 0;
        if (matset.GetChild(10).gameObject.activeSelf == false) return 1;
        if (int.Parse(supply3.text) < int.Parse(demand3.text)) return 0;
        if (matset.GetChild(15).gameObject.activeSelf == false) return 1;
        if (int.Parse(supply4.text) < int.Parse(demand4.text)) return 0;
        return 1;
    }
}
