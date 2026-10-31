using MemorialFloor.Domain;
using MemorialFloor.Game;
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
    public List<ItemSlot> slots = new List<ItemSlot>();
    public List<string> enemies = new List<string>(); //現シーンのみ
    public List<Vector3> respawns = new List<Vector3>(); //現シーンのみ

    //置いた物。同じ添字の3つで1つ。saveObjects はアイテム ID
    public List<string> saveObjects = new List<string>();
    public List<string> saveObjectsScene = new List<string>();
    public List<Vector3> saveObjectsPos = new List<Vector3>();

    public void SyncStatic(Whereabouts whereabouts, PlacedObjects placed) //SaveSystem.Save()の中で最初に実行
    {
        playerhp = Player2.Hp.Current;
        entered_scene = whereabouts.AreaId;
        daytime = Sun2.Cycle.ElapsedSeconds;
        SyncDynamic(placed);
    }

    public void SyncDynamic(PlacedObjects placed)
    {
        PlayerInventory pi = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        slots = new List<ItemSlot>(pi.Slots);

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

        placed.Flush();
        foreach (string area in placed.Ledger.Areas)
        {
            foreach (Placement placement in placed.Ledger.In(area))
            {
                saveObjects.Add(placement.ItemId);
                saveObjectsPos.Add(new Vector3(placement.X, placement.Y, placement.Z));
                saveObjectsScene.Add(area);
            }
        }
    }

    /// <summary>arrivedAreaId は読み込んでから最初に着いた場所</summary>
    public void LoadStatic(string arrivedAreaId)
    {
        Player2.Hp.SetCurrent(playerhp);
        Player2.RefreshHpView();
        Sun2.Cycle.SetElapsed(daytime);
        LoadDynamic(arrivedAreaId);
    }

    public void LoadDynamic(string arrivedAreaId)
    {
        PlayerInventory pi = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        pi.ReplaceSlots(slots);

        GameManager.Coins.SetAmount(coin);
        GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>().text =
            GameManager.Coins.Amount.ToString();

        //立ち位置は保存した場所のもの。エディタで場所のシーンから始めると、保存した場所とは違う場所に着く
        if (arrivedAreaId == entered_scene) GameObject.FindWithTag("Player").transform.position = respawn;

        //enemies関連は、MainSpawner生成が遅れるため、そっちで処理

        //シーンの静的状態は、いずれシード値から復元できるように
    }

    /// <summary>置いた物を控えへ戻す。場所に入ったときに置き直される</summary>
    public void LoadPlacements(PlacementLedger ledger)
    {
        Dictionary<string, List<Placement>> byArea = new Dictionary<string, List<Placement>>();

        for (int i = 0; i < saveObjects.Count; i++)
        {
            string area = saveObjectsScene[i];
            if (!byArea.ContainsKey(area)) byArea[area] = new List<Placement>();

            Vector3 position = saveObjectsPos[i];
            byArea[area].Add(new Placement(saveObjects[i], position.x, position.y, position.z));
        }

        foreach (KeyValuePair<string, List<Placement>> pair in byArea) ledger.Record(pair.Key, pair.Value);
    }
}
