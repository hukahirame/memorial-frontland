using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

[Serializable]
public class SaveData
{
    public int playerhp;

    public string entered_scene;
    public int daytime;
    //roots と quests は保存していない。JsonUtility が List<string[]> を保存できないため。
    //保存を再開するなら、先に入れ子を持たない形へ直すこと
    public int coin;
    public Vector3 respawn;
    public List<string> items = new List<string>();
    public List<int> stocks = new List<int>();
    public List<int> maxstocks = new List<int>();
    public List<string> enemies = new List<string>(); //現シーンのみ
    public List<Vector3> respawns = new List<Vector3>(); //現シーンのみ
    public List<string> saveObjects = new List<string>();
    public List<string> saveObjectsScene = new List<string>();
    public List<Vector3> saveObjectsPos = new List<Vector3>();

    public void SyncStatic() //SaveSystem.Save()の中で最初に実行
    {
        playerhp = Player2.Hp.Current;
        entered_scene = GameManager.entered_scene;
        daytime = Sun2.Cycle.ElapsedSeconds;
        SyncDynamic();
    }

    public void SyncDynamic()
    {
        PlayerInventory pi = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        items = pi.items;
        stocks = pi.stocks;
        maxstocks = pi.maxstocks;

        coin = GameManager.Coins.Amount;
        respawn = GameObject.FindWithTag("Player").transform.position;

        enemies.Clear();
        respawns.Clear();
        var allenemy = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in allenemy)
        {
            enemies.Add(enemy.name);
            respawns.Add(enemy.transform.position);
        }

        saveObjects.Clear();
        saveObjectsPos.Clear();
        saveObjectsScene.Clear();
        var saveobj = GameObject.FindGameObjectsWithTag("SaveObject");
        foreach(var obj in saveobj)
        {
            saveObjects.Add(obj.name);
            saveObjectsPos.Add(obj.transform.position);
            saveObjectsScene.Add(entered_scene);
        }
    }

    public void LoadStatic()
    {
        Player2.Hp.SetCurrent(playerhp);
        Player2.RefreshHpView();
        GameManager.entered_scene = entered_scene;
        Sun2.Cycle.SetElapsed(daytime);
        LoadDynamic();
    }

    public void LoadDynamic()
    {
        PlayerInventory pi = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        pi.items = items;
        pi.stocks = stocks;
        pi.maxstocks = maxstocks;

        GameManager.Coins.SetAmount(coin);
        GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>().text =
            GameManager.Coins.Amount.ToString();
        GameObject.FindWithTag("Player").transform.position = respawn;
        
        //enemies関連は、MainSpawner生成が遅れるため、そっちで処理

        //saveObject関連は、MonoBehaviourのみで生成可なので、SaveSystem.Loadで処理  

        //シーンの静的状態は、いずれシード値から復元できるように
    }
}
