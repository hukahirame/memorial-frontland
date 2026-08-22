using UnityEngine.UI;
using UnityEngine;

public class Inventbutton : MonoBehaviour //インベントリのボタン本体に付ける
{
    private Text txt;
    private Image image;
    private string target;
    public void Ready(string s, int durability) //PlayerInventoryから呼ばれる。アイテムが入ってくる時
    {
        txt = transform.Find("Text").GetComponent<Text>();
        txt.text = "1";
        // Resources.Loadはいつか改良する
        image = GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>(s); // 原画のまま載せるので、縮尺合わない可能性あり

        if (durability != 0)
        {
            txt.text = "";
            transform.Find("Slider").localScale = Vector3.one;
            Slider ds = transform.Find("Slider").GetComponent<Slider>();
            for (int i = 0; i < GameManager.items.Count; i++) //最大耐久値の取得
                if (GameManager.items[i][0] == s) { ds.maxValue = float.Parse(GameManager.items[i][3]); break; }
            ds.value = durability;
        }
        target = s;
    }

    public void Show_Info()
    {
        if(image != null) //アイテム有りBoxの時
        {
            transform.parent.Find("Info_set").GetComponent<Info_set>().Show_Info2(target); //Show_Info2の引数はImageにしたほうがいい
        }
    }

  /*  private Observer observer;
    public string hogehoge;
    void Start()
    {
        observer = GameObject.FindWithTag("Observer").GetComponent<Observer>();
    }
    public void InventButtonClick()
    {
        if (gameObject.name.Length >= 6)//非重要
        {
            int hoge = gameObject.name.Length;
            hogehoge = gameObject.name.Substring(0, hoge - 6);//Buttonを除く
        }
        if (observer.Changefrom != null)//交換時(fromが入っていることを確認してobserverに処理を引き渡す
        {
            observer.Changeto = gameObject;
            Transform parent = observer.Changeto.transform.parent;//Changetoが何番地か疑似的に求める
            Destroy(observer.InventChangeSign2);
            if ((observer.Changefrom.tag == "WeaponButton") || (observer.Changeto.tag == "WeaponButton"))//武器を外した時
            {
                Destroy(observer.instant);
            }
            if ((observer.Changeto.tag=="WeaponButton")&&(observer.Changefrom.name.IndexOf("w",8)!=-1))//武器用スロットと交換 & 交換元が武器
            {
                var player = GameObject.FindWithTag("Player");
                var change = (GameObject)Resources.Load(observer.Changefrom.name.Substring(0, observer.Changefrom.name.Length - 6) + "set");
                var change2= int.Parse(change.transform.Find("PowerText").GetComponent<Text>().text);
                var s = (GameObject)Resources.Load(observer.Changefrom.name.Substring(0, observer.Changefrom.name.Length - 8)+"R");
                var pos = new Vector2(player.transform.position.x+0.2f, player.transform.position.y-0.88f);
                observer.instant = Instantiate(s,pos,Quaternion.Euler(0,0,190),player.transform);
                var w1 = observer.instant;
                player.GetComponent<Player>().w = w1;
                var wacs = observer.instant.GetComponent<Wacs>();
                wacs.power = change2;
                wacs.weight=int.Parse(change.transform.Find("WeightText").GetComponent<Text>().text);
                var ef = change.transform.Find("Effectvalue").GetComponent<Text>();
                observer.instant.GetComponent<Wacs>().effecttext1 = ef;

                var ef2 = change.transform.Find("Effectvalue2").GetComponent<Text>();
                observer.instant.GetComponent<Wacs>().effecttext2 = ef2;
                if (ef != null)
                {
                    if (ef.text == "吹き飛ばし")
                    {
                        var n = change.transform.Find("Effectvalue").GetChild(0).GetComponent<Text>().text;
                        wacs.nockbackpower = 0.07f * int.Parse(n.Substring(0, n.Length - 1)) + 7;
                    }
                }
                if (ef2 != null)
                {
                    if (ef2.text == "吹き飛ばし")
                    {
                        var n = change.transform.Find("Effectvalue2").GetChild(0).GetComponent<Text>().text;
                        wacs.nockbackpower = 0.07f * int.Parse(n.Substring(0, n.Length - 1)) + 7;
                    }
                }
            }
            else if(parent.name=="インベントリ")
            {
                observer.Changetoindex = 0;
                for (int i = 0; parent.GetChild(i + 4).gameObject != observer.Changeto; i++)
                {
                    observer.Changetoindex += 1;
                }
            }
            else if (parent.name == "ChestInventory")
            {
                observer.Changetoindex = 0;
                for (int i = 0; parent.GetChild(i).gameObject != observer.Changeto; i++)
                {
                    observer.Changetoindex += 1;
                }
            }
            observer.InventChangeSign2 = null;
            observer.InventButtonNext();
            observer.Changeto = null;
        }
        else if((observer.obj!=null)&&(observer.obj.name==hogehoge+"set(Clone)"))//既に出ていた場合,連続押しされた場合
        {
            observer.Changefrom = gameObject;
            Transform parent = observer.Changefrom.transform.parent;//Changefromが何番地か疑似的に求める
            if (parent.name == "インベントリ")
            {
                observer.Changefromindex = 0;
                for (int i = 0; parent.GetChild(i + 4).gameObject != observer.Changefrom; i++)
                {
                    observer.Changefromindex += 1;
                }
            }
            else if (parent.name == "ChestInventory")
            {
                observer.Changefromindex = 0;
                for (int i = 0; parent.GetChild(i).gameObject != observer.Changefrom; i++)
                {
                    observer.Changefromindex += 1;
                }
            }
            observer.InventChangeSign2=Instantiate(observer.InventChangeSign, transform.position, Quaternion.identity, transform.parent);
        }
        else if(gameObject.name.Length>=6)//setを出す命令を出す（１回目）
        {
            if (hogehoge.IndexOf("max", 3) != -1)
            {
                var L = hogehoge.Length;
                var L2 = hogehoge.Substring(0, L - 3);//後ろ１文字以外
                observer.showingset = (GameObject)Resources.Load(L2 + "set");
            }
            else if ((hogehoge.Length<=7)||(hogehoge.IndexOf("w", 8)==-1))
            {
                observer.showingset = (GameObject)Resources.Load(hogehoge + "set");
            }
            else if(hogehoge.IndexOf("w", 8) != -1)
            {
                var L = hogehoge.Length;
                var L2 = hogehoge.Substring(0, L - 1);//後ろ１文字以外
                observer.showingset = (GameObject)Resources.Load(hogehoge + "set");
            }
            observer.InventButtonNext();
        }
        else
        {

        }
    }*/
}
