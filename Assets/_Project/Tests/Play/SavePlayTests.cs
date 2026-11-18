using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//保存を読み込んだあとに戻るもの。テストではファイルを読み書きしない（NoSaveDuringTests）ので、
//読み込んだ中身は SaveSystem.Data へ直に入れ、着いたときの戻しは SaveSystem.Restore を直に呼ぶ
public class SavePlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>保存した敵の体力。湧いたばかりの敵の体力と見分けられればよい</summary>
    private const int SavedHp = 7;

    /// <summary>攻め込んだ敵に負わせる傷。満タンと見分けられればよい</summary>
    private const int Wound = 100;

    [UnityTest]
    public IEnumerator 保存した立ち位置は保存した場所に着いたときだけ戻る()
    {
        yield return Boot();
        Transform player = GameObject.FindWithTag("Player").transform;
        Vector3 standing = player.position;
        Vector3 saved = standing + new Vector3(1f, 0f, 1f);
        SaveData data = Loaded("Root1", saved);

        //エディタで場所のシーンから始めると、保存した場所と違う場所に着く
        Restore(Travel().Current);
        Assert.AreEqual(standing, player.position, "保存した場所でないのに、保存した立ち位置に立つ");

        data.entered_scene = Travel().Current.Id;
        Restore(Travel().Current);
        Assert.AreEqual(saved, player.position);
    }

    //隣の場所も読み込んでおくので、保存した根源の敵は、その根源を読み込んだときに戻る
    [UnityTest]
    public IEnumerator 保存した敵は保存した根源を読み込んだときだけ戻る()
    {
        yield return Boot();
        SaveData data = Loaded("Root2", Vector3.zero);
        List<Vector3> respawns = data.respawns;
        data.enemies.Add("Slime");
        respawns.Add(new Vector3(0f, 1f, 0f));

        //拠点に居るあいだは、隣の根源1だけを読み込む
        yield return Loading("Root1");
        Assert.AreEqual(1, respawns.Count, "保存していない根源で、保存した敵が戻された");

        yield return GoToRoot("Root2");
        Assert.AreEqual(0, respawns.Count, "保存した根源を読み込んでも、保存した敵が戻らない");
    }

    [UnityTest]
    public IEnumerator 保存した根源の敵は控えた体力で戻る()
    {
        yield return Boot();

        //湧いた敵の立つところなら、戻した敵も床の上に出る。保存の位置は場所の原点からの位置
        yield return GoToRoot("Root2");
        Slime wild = SlimesHere().FirstOrDefault();
        Assert.IsNotNull(wild, "根源2に敵が湧かない");
        Vector3 ground = Travel().Current.ToLocal(wild.transform.position);

        //拠点へ戻ると、2 つ離れた根源2は手放される
        Travel().Go(Route.To(TravelRule.BaseAreaId, Arrival.Home));
        yield return Arrive();
        Assert.IsFalse(SceneManager.GetSceneByName("Root2").isLoaded, "拠点に居るのに根源2を手放さない");

        SaveData data = Loaded("Root2", Vector3.zero);
        data.enemies.Add("Slime");
        data.respawns.Add(ground);
        data.enemyHps.Add(SavedHp);

        yield return GoToRoot("Root2");

        Assert.IsTrue(SlimesHere().Any(slime => slime.GetComponent<IDamageable>().CurrentHp == SavedHp), "控えた体力で戻らない");
    }

    [UnityTest]
    public IEnumerator 拠点で保存すると攻め込んだ敵の位置と体力も書く()
    {
        yield return Boot();
        var existing = new HashSet<Slime>(SlimesHere());

        Raid().Invade(Root.StampedeRaiders);
        yield return null;
        yield return null;

        List<Slime> raiders = SlimesHere().Where(slime => !existing.Contains(slime)).ToList();
        Assert.AreEqual(Root.StampedeRaiders, raiders.Count, "攻め込んだ敵が出ない");
        IDamageable hurt = raiders[0].GetComponent<IDamageable>();
        hurt.TakeDamage(Wound);
        int left = hurt.CurrentHp;

        SaveData data = Written();
        Raid().Clear();
        Assert.IsTrue(data.HasWorld, "保存した根源を戻さない");
        SaveSystem().RestoreWorld(data);

        Assert.AreEqual(Root.StampedeRaiders, Raid().Kept.Count, "出ている敵が書かれない");
        Assert.IsTrue(Raid().Kept.All(raider => raider.HasPosition), "出ている敵の位置が書かれない");
        Assert.AreEqual(1, Raid().Kept.Count(raider => raider.Hp == left), "出ている敵の体力が書かれない");
    }

    [UnityTest]
    public IEnumerator 前の形の保存の拠点へ向かう敵は湧き口のまわりに出る敵として戻る()
    {
        yield return Boot();
        SaveData data = Written();
        data.raiders = Root.StampedeRaiders;

        Raid().Clear();
        Assert.IsTrue(data.HasWorld, "保存した根源を戻さない");
        SaveSystem().RestoreWorld(data);

        Assert.AreEqual(Root.StampedeRaiders, Raid().Kept.Count);
        Assert.IsTrue(Raid().Kept.All(raider => !raider.HasPosition), "位置を持たない敵として戻らない");
    }

    [UnityTest]
    public IEnumerator 根源とクエストと受注は書き出して読み直すと戻る()
    {
        yield return Boot();
        Root root = Roots().Find("Root1");
        root.Gain(40);
        root.AccumulateDaily(UnityRandom.Shared);
        root.PlaceSpawnPoint(1f, 2f, 3f);
        int progress = root.Progress;
        int accumulation = root.Accumulation;

        Quest sub = Quests().All.First(q => q.Kind == QuestKind.Sub && q.RootId == "Root2");
        sub.Advance();
        string ordered = Quests().All.First(q => q.Kind == QuestKind.Main && q.RootId == "Root2").Id;
        UnityEngine.Object.FindAnyObjectByType<QuestManager>().Order(ordered);
        Raid().Invade(2);
        Raid().Keep(new Raider(1f, 2f, 3f, 40));
        Plan().Clear();
        Plan().ShouldIssue("Root1");
        string[] ids = Quests().All.Select(q => q.Id).ToArray();

        SaveData data = Written();

        //起動し直したときと同じく、根源は作り直し、ほかは空にする
        Roots().Clear();
        RootsManager manager = UnityEngine.Object.FindAnyObjectByType<RootsManager>();
        typeof(RootsManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        Quests().Clear();
        UnityEngine.Object.FindAnyObjectByType<QuestManager>().CancelOrder();
        Raid().Clear();
        Plan().Clear();

        Assert.IsTrue(data.HasWorld, "保存した根源を戻さない");
        SaveSystem().RestoreWorld(data);

        Root restored = Roots().Find("Root1");
        Assert.AreEqual(progress, restored.Progress);
        Assert.AreEqual(accumulation, restored.Accumulation);
        Assert.IsTrue(restored.HasSpawnPoint, "核の位置が戻らない");
        Assert.AreEqual(new Vector3(1f, 2f, 3f), new Vector3(restored.SpawnX, restored.SpawnY, restored.SpawnZ));
        CollectionAssert.AreEqual(ids, Quests().All.Select(q => q.Id).ToArray(), "クエストが戻らない");
        Assert.AreEqual(1, Quests().Find(sub.Id).Progress, "進捗が戻らない");
        CollectionAssert.AreEqual(sub.Rewards.Select(r => r.Kind + r.Amount).ToArray(),
                                  Quests().Find(sub.Id).Rewards.Select(r => r.Kind + r.Amount).ToArray(), "報酬が戻らない");
        Assert.AreEqual(ordered, UnityEngine.Object.FindAnyObjectByType<QuestManager>().OrderedId, "受注が戻らない");
        Assert.AreEqual(3, Raid().Kept.Count, "拠点に攻め込んだ敵が戻らない");
        Raider kept = Raid().Kept.Single(raider => raider.HasPosition);
        Assert.AreEqual(new Vector3(1f, 2f, 3f), new Vector3(kept.X, kept.Y, kept.Z), "攻め込んだ敵の位置が戻らない");
        Assert.AreEqual(40, kept.Hp, "攻め込んだ敵の体力が戻らない");
        CollectionAssert.AreEqual(new[] { "Root1" }, Plan().Pending.ToArray(), "保留中の調査が戻らない");
    }

    [UnityTest]
    public IEnumerator 保存のあとで足した根源には始めの調査とサブが出る()
    {
        yield return Boot();
        SaveData data = Written();

        //根源3を足す前の保存にする
        data.roots.RemoveAll(record => record.id == "Root3");
        data.quests.RemoveAll(record => record.rootId == "Root3");
        string[] saved = Quests().All.Where(q => q.RootId != "Root3").Select(q => q.Id).ToArray();
        Quests().Clear();

        Assert.IsTrue(data.HasWorld, "保存した根源を戻さない");
        SaveSystem().RestoreWorld(data);

        Assert.IsTrue(Quests().HasMainFor("Root3"), "保存のあとで足した根源に調査が出ない");
        Assert.IsTrue(Quests().All.Any(q => q.Kind == QuestKind.Sub && q.RootId == "Root3"), "保存のあとで足した根源にサブが出ない");
        CollectionAssert.AreEqual(saved, Quests().All.Where(q => q.RootId != "Root3").Select(q => q.Id).ToArray(),
                                  "保存にある根源のクエストが変わる");
    }

    [UnityTest]
    public IEnumerator 根源の無い保存からは何も戻さず始めのクエストを残す()
    {
        yield return Boot();
        SaveData empty = new SaveData();
        string[] ids = Quests().All.Select(q => q.Id).ToArray();

        Assert.IsFalse(empty.HasWorld);
        SaveSystem().RestoreWorld(empty);
        CollectionAssert.AreEqual(ids, Quests().All.Select(q => q.Id).ToArray(), "空の保存でクエストを消す");
    }

    //遊びの状態は常駐シーンの部品が持つ。タイトルに戻ると常駐シーンごと捨てられ、始め直すと作り直される。
    //残ると、設定からタイトルに戻って別の枠や「はじめる」で始めたときに、前の遊びが混ざる
    [UnityTest]
    public IEnumerator 設定からタイトルに戻って始め直すと前の遊びが残らない()
    {
        yield return Boot();
        DayCycle cycle = UnityEngine.Object.FindAnyObjectByType<Sun>().Cycle;
        PlayerHealth hp = Health();
        int quests = Quests().All.Count;

        //前の遊び
        Roots().All[0].Restore(80, 0);
        UnityEngine.Object.FindAnyObjectByType<QuestManager>().Order(Quests().All[0].Id);
        Plan().Restore(new[] { "Root1" });
        cycle.SetElapsed(60);
        hp.SetCurrent(1);
        Movement().Speed = 3f;

        SettingsMenu settings = UnityEngine.Object.FindAnyObjectByType<SettingsMenu>();
        Assert.IsNotNull(settings, "常駐シーンに設定が無い");
        settings.Open();
        Assert.AreEqual(0f, Time.timeScale, "設定を開いても止まらない");

        settings.RequestTitle();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (SceneManager.GetActiveScene().name != AreaTravel.TitleScene && Time.realtimeSinceStartup < giveUp) yield return null;
        Assert.AreEqual(AreaTravel.TitleScene, SceneManager.GetActiveScene().name, "タイトルに戻らない");
        Assert.AreEqual(1f, Time.timeScale, "タイトルに戻っても止まったまま");

        yield return Boot();
        cycle = UnityEngine.Object.FindAnyObjectByType<Sun>().Cycle;
        Assert.AreEqual(0, Roots().All[0].Progress, "根源の攻略度が前の遊びのまま");
        Assert.AreEqual("", UnityEngine.Object.FindAnyObjectByType<QuestManager>().OrderedId, "受注が前の遊びのまま");
        Assert.AreEqual(quests, Quests().All.Count, "前の遊びのクエストが残る");
        Assert.AreEqual(0, Plan().Count, "保留中の調査が前の遊びのまま");
        Assert.Less(cycle.ElapsedSeconds, 60, "時刻が前の遊びのまま");
        hp = Health();
        Assert.AreEqual(hp.Max, hp.Current, "体力が前の遊びのまま");
        Assert.AreEqual(PlayerMovement.BaseSpeed, Movement().Speed, "速さが前の遊びのまま");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトを捨てる()
    {
        Time.timeScale = 1f;

        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        yield return null;
    }

    private static PlayerHealth Health()
    {
        return GameObject.FindWithTag("Player").GetComponent<PlayerHealth>();
    }

    private static PlayerMovement Movement()
    {
        return GameObject.FindWithTag("Player").GetComponent<PlayerMovement>();
    }

    //読み込んだ保存。体力・所持金・経過秒・持ち物はいまのまま入れ、場所と立ち位置だけ変える
    private static SaveData Loaded(string areaId, Vector3 respawn)
    {
        SaveSystem save = SaveSystem();
        SaveData data = save.Data;
        PlayerInventory inventory = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        IReadOnlyList<ItemSlot> slots = inventory.Slots;

        data.playerhp = Health().Current;
        data.coin = UnityEngine.Object.FindAnyObjectByType<Purse>().Coins.Amount;
        data.daytime = UnityEngine.Object.FindAnyObjectByType<Sun>().Cycle.ElapsedSeconds;
        data.slots = new List<ItemSlot>(slots);
        data.entered_scene = areaId;
        data.respawn = respawn;

        return data;
    }

    //いまの根源とクエストを写し、ファイルと同じく JSON にして読み直したもの。JsonUtility で落ちる欄があればここで分かる
    private static SaveData Written()
    {
        SaveSystem save = SaveSystem();
        SaveData data = save.Data;
        save.CaptureWorld(data);

        return JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
    }

    private static RootRegistry Roots()
    {
        return UnityEngine.Object.FindAnyObjectByType<RootsManager>().Roots;
    }

    //いま居る場所で動いている敵
    private static List<Slime> SlimesHere()
    {
        Scene here = SceneManager.GetActiveScene();

        return UnityEngine.Object.FindObjectsByType<Slime>(FindObjectsSortMode.None)
            .Where(slime => slime.gameObject.scene == here)
            .ToList();
    }

    private static QuestRegistry Quests()
    {
        return UnityEngine.Object.FindAnyObjectByType<QuestManager>().Quests;
    }

    private static BaseRaid Raid()
    {
        return UnityEngine.Object.FindAnyObjectByType<RootsManager>().Raid;
    }

    private static DayPlan Plan()
    {
        return UnityEngine.Object.FindAnyObjectByType<QuestManager>().Plan;
    }

    private static void Restore(Area arrived)
    {
        SaveSystem save = SaveSystem();
        MethodInfo restore = typeof(SaveSystem).GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(restore, "SaveSystem.Restore が見つからない");

        restore.Invoke(save, new object[] { arrived });
    }

    //隣の場所として読み込み終え、その場所の Start とコルーチンが一巡するまで待つ
    private static IEnumerator Loading(string areaId)
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (!travel.Present.Any(area => area.Id == areaId) && Time.realtimeSinceStartup < giveUp) yield return null;
        Assert.IsTrue(travel.Present.Any(area => area.Id == areaId), areaId + " が読み込まれない");

        for (int i = 0; i < SettleFrames; i++) yield return null;
    }

    private static IEnumerator GoToRoot(string rootId)
    {
        Travel().Go(Route.To(rootId, Arrival.FromPrevious));
        yield return Arrive();
        Assert.AreEqual(rootId, Travel().Current.Id);
    }

    private static SaveSystem SaveSystem()
    {
        GameObject manager = GameObject.Find("GameManager");
        Assert.IsNotNull(manager, "GameManager が無い");

        return manager.GetComponent<SaveSystem>();
    }

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");

        return travel;
    }

    private static IEnumerator Boot()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();
    }

    //着いて、着いた場所の Start とコルーチンが一巡するまで待つ
    private static IEnumerator Arrive()
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;

        while (Time.realtimeSinceStartup < giveUp)
        {
            if (travel.Current != null && !travel.IsTraveling) break;
            yield return null;
        }

        Assert.IsNotNull(travel.Current, "場所に着かない");
        Assert.IsFalse(travel.IsTraveling, "移動が終わらない");

        for (int i = 0; i < SettleFrames; i++) yield return null;
    }
}
