using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int playerhp;

    public string saveName = ""; //タイトルで付けた枠の名前。空ならタイトルは「データ」と枠の番号を出す

    public string entered_scene;
    public int daytime;
    public int coin;
    public Vector3 respawn;
    public List<ItemSlot> slots = new List<ItemSlot>();
    public string equipped = ""; //装備している武器の ID。素手と、これを足す前の保存は空
    public List<string> enemies = new List<string>(); //現シーンのみ
    public List<Vector3> respawns = new List<Vector3>(); //現シーンのみ
    public List<int> enemyHps = new List<int>(); //現シーンのみ。respawns と同じ添字の残りの体力。0 と、足りない保存の敵は新しく湧いた敵として出す

    //置いた物。同じ添字の7つで1つ。saveObjects はアイテム ID、saveObjectsYaw は y 軸まわりの向き（度）、
    //saveObjectsDurability は残りの耐久（耐久を持たない物は -1）、saveObjectsAge は控えてから保存までの秒数、
    //saveObjectsFallen は柵が倒れているか。足りない保存は、向きを 0、耐久を控えていない（満タンで立つ）、
    //控えたのは保存したとき、立っているとみなす。ゲームを閉じているあいだは数えないよう、控えた時刻ではなく経った秒数で持つ
    public List<string> saveObjects = new List<string>();
    public List<string> saveObjectsScene = new List<string>();
    public List<Vector3> saveObjectsPos = new List<Vector3>();
    public List<float> saveObjectsYaw = new List<float>();
    public List<int> saveObjectsDurability = new List<int>();
    public List<float> saveObjectsAge = new List<float>();
    public List<bool> saveObjectsFallen = new List<bool>();

    //根源は遊ぶうちに動く値だけを持つ。名前や危険度はコードから作り直す
    public List<RootRecord> roots = new List<RootRecord>();
    public List<QuestRecord> quests = new List<QuestRecord>();
    public string ordered_id = "";
    public int raiders; //前の形の保存の、拠点へ向かっていた敵の数。読むだけで、書くときは 0
    public List<RaiderRecord> baseRaiders = new List<RaiderRecord>(); //拠点に攻め込んだ敵
    public List<string> plannedRoots = new List<string>(); //保留中の調査

    public void SyncStatic(Whereabouts whereabouts, PlacedObjects placed) //SaveSystem.Save()の中で最初に実行
    {
        playerhp = Player2.Hp.Current;
        entered_scene = whereabouts.AreaId;
        daytime = Sun2.Cycle.ElapsedSeconds;
        SyncDynamic(placed);
        SyncWorld();
    }

    public void SyncDynamic(PlacedObjects placed)
    {
        PlayerInventory pi = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        slots = new List<ItemSlot>(pi.Slots);
        equipped = Weapon.Equipped.ItemId;

        coin = GameManager.Coins.Amount;
        respawn = GameObject.FindWithTag("Player").transform.position;

        enemies.Clear();
        respawns.Clear();
        enemyHps.Clear();
        var allenemy = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in allenemy)
        {
            //倒れて消える途中のものは書かない
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && damageable.CurrentHp <= 0) continue;

            enemies.Add(enemy.name);
            respawns.Add(enemy.transform.position);
            enemyHps.Add(damageable != null ? damageable.CurrentHp : 0);
        }

        saveObjects.Clear();
        saveObjectsPos.Clear();
        saveObjectsScene.Clear();
        saveObjectsYaw.Clear();
        saveObjectsDurability.Clear();
        saveObjectsAge.Clear();
        saveObjectsFallen.Clear();

        placed.Flush();
        foreach (string area in placed.Ledger.Areas)
        {
            foreach (Placement placement in placed.Ledger.In(area))
            {
                saveObjects.Add(placement.ItemId);
                saveObjectsPos.Add(new Vector3(placement.X, placement.Y, placement.Z));
                saveObjectsScene.Add(area);
                saveObjectsYaw.Add(placement.Yaw);
                saveObjectsDurability.Add(placement.Durability);
                saveObjectsAge.Add(Time.time - placement.RecordedAt);
                saveObjectsFallen.Add(placement.Fallen);
            }
        }
    }

    /// <summary>根源・クエスト・受注・拠点に攻め込んだ敵・保留中の調査を写す</summary>
    public void SyncWorld()
    {
        roots.Clear();
        foreach (Root root in RootsManager.Roots.All) roots.Add(RootRecord.Of(root));

        quests.Clear();
        foreach (Quest quest in QuestManager.Quests.All) quests.Add(QuestRecord.Of(quest));

        ordered_id = QuestManager.ordered_id;

        //拠点に居れば、出ている敵も写す。控えには拠点のシーンが無いあいだの敵しか無い
        raiders = 0;
        baseRaiders.Clear();
        foreach (Raider raider in RootsManager.Raid.Kept) baseRaiders.Add(RaiderRecord.Of(raider));
        MS_Spawner gate = UnityEngine.Object.FindAnyObjectByType<MS_Spawner>();
        if (gate != null)
        {
            foreach (Raider raider in gate.Survivors()) baseRaiders.Add(RaiderRecord.Of(raider));
        }

        plannedRoots = new List<string>(Sun2.Plan.Pending);
    }

    /// <summary>
    /// 根源・クエスト・受注・拠点に攻め込んだ敵・保留中の調査を戻す。保存のあとで足した根源には始めのクエストを出す。
    /// 最初の場所に着く前に呼ぶ。着いたときにクエストが始まり、核の位置が決まるため。保存に根源が無ければ何もせず false
    /// </summary>
    public bool LoadWorld()
    {
        if (roots.Count == 0) return false;

        //作る根源から外れた Id は捨てる
        foreach (RootRecord record in roots)
        {
            Root root = RootsManager.Roots.Find(record.id);
            if (root != null) record.Apply(root);
        }

        QuestManager.Quests.Clear();
        foreach (QuestRecord record in quests) QuestManager.Quests.TryAdd(record.ToQuest());

        //出さないと、足した根源には 2 回目の夜明けまで受けるものが無い
        foreach (Root root in RootsManager.Roots.All)
        {
            if (!roots.Exists(record => record.id == root.Id)) QuestManager.IssueFirst(root);
        }

        QuestManager.ordered_id = QuestManager.Quests.Find(ordered_id) != null ? ordered_id : "";

        RootsManager.Raid.Clear();
        foreach (RaiderRecord record in baseRaiders) RootsManager.Raid.Keep(record.ToRaider());
        RootsManager.Raid.Invade(raiders);
        Sun2.Plan.Restore(plannedRoots);

        return true;
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

        //戻した持ち物から持ち直す。空なら始めの鉄の剣のまま
        if (!string.IsNullOrEmpty(equipped)) pi.Wield(equipped);

        GameManager.Coins.SetAmount(coin);

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
            float yaw = i < saveObjectsYaw.Count ? saveObjectsYaw[i] : 0f;
            int durability = i < saveObjectsDurability.Count ? saveObjectsDurability[i] : Placement.NoDurability;
            float age = i < saveObjectsAge.Count ? saveObjectsAge[i] : 0f;
            bool fallen = i < saveObjectsFallen.Count && saveObjectsFallen[i];
            byArea[area].Add(new Placement(saveObjects[i], position.x, position.y, position.z, yaw, durability, Time.time - age, fallen));
        }

        foreach (KeyValuePair<string, List<Placement>> pair in byArea) ledger.Record(pair.Key, pair.Value);
    }
}

/// <summary>根源 1 つの、遊ぶうちに動く値</summary>
[Serializable]
public class RootRecord
{
    public string id;
    public int progress;
    public int accumulation;
    public bool hasSpawnPoint;
    public Vector3 spawnPoint;

    public static RootRecord Of(Root root)
    {
        return new RootRecord
        {
            id = root.Id,
            progress = root.Progress,
            accumulation = root.Accumulation,
            hasSpawnPoint = root.HasSpawnPoint,
            spawnPoint = new Vector3(root.SpawnX, root.SpawnY, root.SpawnZ),
        };
    }

    public void Apply(Root root)
    {
        root.Restore(progress, accumulation);
        if (hasSpawnPoint) root.PlaceSpawnPoint(spawnPoint.x, spawnPoint.y, spawnPoint.z);
    }
}

/// <summary>拠点に攻め込んだ敵 1 体。位置を持たないものは、拠点の湧き口のまわりに満タンで出る</summary>
[Serializable]
public class RaiderRecord
{
    public bool hasPosition;
    public Vector3 position;
    public int hp;

    public static RaiderRecord Of(Raider raider)
    {
        return new RaiderRecord
        {
            hasPosition = raider.HasPosition,
            position = new Vector3(raider.X, raider.Y, raider.Z),
            hp = raider.Hp,
        };
    }

    public Raider ToRaider()
    {
        return hasPosition ? new Raider(position.x, position.y, position.z, hp) : Raider.Fresh;
    }
}

/// <summary>クエスト 1 本。報酬は同じ添字の 2 つで 1 つ</summary>
[Serializable]
public class QuestRecord
{
    public string id;
    public string rootId;
    public string target;
    public int amount;
    public int progress;
    public List<string> rewardKinds = new List<string>();
    public List<int> rewardAmounts = new List<int>();

    public static QuestRecord Of(Quest quest)
    {
        QuestRecord record = new QuestRecord
        {
            id = quest.Id,
            rootId = quest.RootId,
            target = quest.Target,
            amount = quest.Amount,
            progress = quest.Progress,
        };

        foreach (Reward reward in quest.Rewards)
        {
            record.rewardKinds.Add(reward.Kind);
            record.rewardAmounts.Add(reward.Amount);
        }

        return record;
    }

    public Quest ToQuest()
    {
        List<Reward> rewards = new List<Reward>();
        for (int i = 0; i < rewardKinds.Count && i < rewardAmounts.Count; i++)
            rewards.Add(new Reward(rewardKinds[i], rewardAmounts[i]));

        Quest quest = new Quest(id, rootId, target, amount, rewards);
        quest.SetProgress(progress);

        return quest;
    }
}
