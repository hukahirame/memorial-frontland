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
//読み込んだ中身は SaveSystem.savedata へ直に入れ、着いたときの戻しは SaveSystem.Restore を直に呼ぶ
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
        object data = Loaded("Root1", saved);

        //エディタで場所のシーンから始めると、保存した場所と違う場所に着く
        Restore(Travel().Current);
        Assert.AreEqual(standing, player.position, "保存した場所でないのに、保存した立ち位置に立つ");

        Set(data, "entered_scene", Travel().Current.Id);
        Restore(Travel().Current);
        Assert.AreEqual(saved, player.position);
    }

    [UnityTest]
    public IEnumerator 保存した敵は保存した根源に入ったときだけ戻る()
    {
        yield return Boot();
        object data = Loaded("Root2", Vector3.zero);
        List<Vector3> respawns = (List<Vector3>)FieldOf(data, "respawns").GetValue(data);
        ((List<string>)FieldOf(data, "enemies").GetValue(data)).Add("Slime");
        respawns.Add(new Vector3(0f, 1f, 0f));

        yield return GoToRoot("Root1");
        Assert.AreEqual(1, respawns.Count, "保存していない根源で、保存した敵が戻された");

        yield return GoToRoot("Root2");
        Assert.AreEqual(0, respawns.Count, "保存した根源に入っても、保存した敵が戻らない");
    }

    [UnityTest]
    public IEnumerator 保存した根源の敵は控えた体力で戻る()
    {
        yield return Boot();

        //湧いた敵の立つところなら、戻した敵も床の上に出る
        yield return GoToRoot("Root2");
        Component wild = SlimesHere().FirstOrDefault();
        Assert.IsNotNull(wild, "根源2に敵が湧かない");
        Vector3 ground = wild.transform.position;
        yield return GoToRoot("Root1");

        object data = Loaded("Root2", Vector3.zero);
        ((List<string>)FieldOf(data, "enemies").GetValue(data)).Add("Slime");
        ((List<Vector3>)FieldOf(data, "respawns").GetValue(data)).Add(ground);
        ((List<int>)FieldOf(data, "enemyHps").GetValue(data)).Add(SavedHp);

        yield return GoToRoot("Root2");

        Assert.IsTrue(SlimesHere().Any(slime => slime.GetComponent<IDamageable>().CurrentHp == SavedHp), "控えた体力で戻らない");
    }

    [UnityTest]
    public IEnumerator 拠点で保存すると攻め込んだ敵の位置と体力も書く()
    {
        yield return Boot();
        var existing = new HashSet<Component>(SlimesHere());

        Raid().Invade(Root.StampedeRaiders);
        yield return null;
        yield return null;

        List<Component> raiders = SlimesHere().Where(slime => !existing.Contains(slime)).ToList();
        Assert.AreEqual(Root.StampedeRaiders, raiders.Count, "攻め込んだ敵が出ない");
        IDamageable hurt = raiders[0].GetComponent<IDamageable>();
        hurt.TakeDamage(Wound);
        int left = hurt.CurrentHp;

        object data = Written();
        Raid().Clear();
        Assert.IsTrue((bool)data.GetType().GetMethod("LoadWorld").Invoke(data, null), "保存した根源を戻さない");

        Assert.AreEqual(Root.StampedeRaiders, Raid().Kept.Count, "出ている敵が書かれない");
        Assert.IsTrue(Raid().Kept.All(raider => raider.HasPosition), "出ている敵の位置が書かれない");
        Assert.AreEqual(1, Raid().Kept.Count(raider => raider.Hp == left), "出ている敵の体力が書かれない");
    }

    [UnityTest]
    public IEnumerator 前の形の保存の拠点へ向かう敵は湧き口のまわりに出る敵として戻る()
    {
        yield return Boot();
        object data = Written();
        Set(data, "raiders", Root.StampedeRaiders);

        Raid().Clear();
        Assert.IsTrue((bool)data.GetType().GetMethod("LoadWorld").Invoke(data, null), "保存した根源を戻さない");

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
        StaticField("QuestManager", "ordered_id").SetValue(null, ordered);
        Raid().Invade(2);
        Raid().Keep(new Raider(1f, 2f, 3f, 40));
        Plan().Clear();
        Plan().ShouldIssue("Root1");
        string[] ids = Quests().All.Select(q => q.Id).ToArray();

        object data = Written();

        //起動し直したときと同じく、根源は作り直し、ほかは空にする
        Roots().Clear();
        Component manager = GameObject.FindWithTag("RootsManager").GetComponent(GameType("RootsManager"));
        GameType("RootsManager").GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        Quests().Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        Raid().Clear();
        Plan().Clear();

        Assert.IsTrue((bool)data.GetType().GetMethod("LoadWorld").Invoke(data, null), "保存した根源を戻さない");

        Root restored = Roots().Find("Root1");
        Assert.AreEqual(progress, restored.Progress);
        Assert.AreEqual(accumulation, restored.Accumulation);
        Assert.IsTrue(restored.HasSpawnPoint, "核の位置が戻らない");
        Assert.AreEqual(new Vector3(1f, 2f, 3f), new Vector3(restored.SpawnX, restored.SpawnY, restored.SpawnZ));
        CollectionAssert.AreEqual(ids, Quests().All.Select(q => q.Id).ToArray(), "クエストが戻らない");
        Assert.AreEqual(1, Quests().Find(sub.Id).Progress, "進捗が戻らない");
        CollectionAssert.AreEqual(sub.Rewards.Select(r => r.Kind + r.Amount).ToArray(),
                                  Quests().Find(sub.Id).Rewards.Select(r => r.Kind + r.Amount).ToArray(), "報酬が戻らない");
        Assert.AreEqual(ordered, StaticField("QuestManager", "ordered_id").GetValue(null), "受注が戻らない");
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
        object data = Written();

        //根源3を足す前の保存にする
        Strip(data, "roots", "id", "Root3");
        Strip(data, "quests", "rootId", "Root3");
        string[] saved = Quests().All.Where(q => q.RootId != "Root3").Select(q => q.Id).ToArray();
        Quests().Clear();

        Assert.IsTrue((bool)data.GetType().GetMethod("LoadWorld").Invoke(data, null), "保存した根源を戻さない");

        Assert.IsTrue(Quests().HasMainFor("Root3"), "保存のあとで足した根源に調査が出ない");
        Assert.IsTrue(Quests().All.Any(q => q.Kind == QuestKind.Sub && q.RootId == "Root3"), "保存のあとで足した根源にサブが出ない");
        CollectionAssert.AreEqual(saved, Quests().All.Where(q => q.RootId != "Root3").Select(q => q.Id).ToArray(),
                                  "保存にある根源のクエストが変わる");
    }

    [UnityTest]
    public IEnumerator 根源の無い保存からは何も戻さず始めのクエストを残す()
    {
        yield return Boot();
        object empty = Activator.CreateInstance(GameType("SaveData"));
        string[] ids = Quests().All.Select(q => q.Id).ToArray();

        Assert.IsFalse((bool)empty.GetType().GetMethod("LoadWorld").Invoke(empty, null));
        CollectionAssert.AreEqual(ids, Quests().All.Select(q => q.Id).ToArray(), "空の保存でクエストを消す");
    }

    //遊びの状態は static にあり、常駐シーンを読み直しても残る。タイトルが開くたびに起動した直後へ戻さないと、
    //設定からタイトルに戻って別の枠や「はじめる」で始めたときに、前の遊びが混ざる
    [UnityTest]
    public IEnumerator 設定からタイトルに戻って始め直すと前の遊びが残らない()
    {
        yield return Boot();
        DayCycle cycle = (DayCycle)StaticField("Sun2", "Cycle").GetValue(null);
        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        int quests = Quests().All.Count;

        //前の遊び
        Roots().All[0].Restore(80, 0);
        StaticField("QuestManager", "ordered_id").SetValue(null, Quests().All[0].Id);
        Plan().Restore(new[] { "Root1" });
        cycle.SetElapsed(60);
        hp.SetCurrent(1);
        StaticField("Player2", "speed").SetValue(null, 3f);

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
        Assert.AreEqual(0, Roots().All[0].Progress, "根源の攻略度が前の遊びのまま");
        Assert.AreEqual("", StaticField("QuestManager", "ordered_id").GetValue(null), "受注が前の遊びのまま");
        Assert.AreEqual(quests, Quests().All.Count, "前の遊びのクエストが残る");
        Assert.AreEqual(0, Plan().Count, "保留中の調査が前の遊びのまま");
        Assert.Less(cycle.ElapsedSeconds, 60, "時刻が前の遊びのまま");
        Assert.AreEqual(hp.Max, hp.Current, "体力が前の遊びのまま");
        Assert.AreEqual(1f, (float)StaticField("Player2", "speed").GetValue(null), "速さが前の遊びのまま");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;

        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        Roots().Clear();
        Raid().Clear();
        Plan().Clear();
        Quests().Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);
        StaticField("Player2", "cool").SetValue(null, false);
        StaticField("Player2", "speed").SetValue(null, 1f);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    //読み込んだ保存。体力・所持金・経過秒・持ち物はいまのまま入れ、場所と立ち位置だけ変える
    private static object Loaded(string areaId, Vector3 respawn)
    {
        Component save = SaveSystem();
        object data = FieldOf(save, "savedata").GetValue(save);
        Component inventory = GameObject.FindWithTag("PlayerInventory").GetComponent(GameType("PlayerInventory"));
        IReadOnlyList<ItemSlot> slots = (IReadOnlyList<ItemSlot>)GameType("PlayerInventory").GetProperty("Slots").GetValue(inventory);

        Set(data, "playerhp", ((Health)StaticField("Player2", "Hp").GetValue(null)).Current);
        Set(data, "coin", ((Wallet)StaticField("GameManager", "Coins").GetValue(null)).Amount);
        Set(data, "daytime", ((DayCycle)StaticField("Sun2", "Cycle").GetValue(null)).ElapsedSeconds);
        Set(data, "slots", new List<ItemSlot>(slots));
        Set(data, "entered_scene", areaId);
        Set(data, "respawn", respawn);

        return data;
    }

    //いまの根源とクエストを写し、ファイルと同じく JSON にして読み直したもの。JsonUtility で落ちる欄があればここで分かる
    private static object Written()
    {
        Component save = SaveSystem();
        object data = FieldOf(save, "savedata").GetValue(save);
        data.GetType().GetMethod("SyncWorld").Invoke(data, null);

        return JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
    }

    private static RootRegistry Roots()
    {
        return (RootRegistry)StaticField("RootsManager", "Roots").GetValue(null);
    }

    //いま居る場所で動いている敵
    private static List<Component> SlimesHere()
    {
        Scene here = SceneManager.GetActiveScene();

        return UnityEngine.Object.FindObjectsByType(GameType("Slime"), FindObjectsSortMode.None)
            .Cast<Component>()
            .Where(slime => slime.gameObject.scene == here)
            .ToList();
    }

    private static QuestRegistry Quests()
    {
        return (QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null);
    }

    private static BaseRaid Raid()
    {
        return (BaseRaid)StaticField("RootsManager", "Raid").GetValue(null);
    }

    private static DayPlan Plan()
    {
        return (DayPlan)StaticField("Sun2", "Plan").GetValue(null);
    }

    private static void Restore(Area arrived)
    {
        Component save = SaveSystem();
        MethodInfo restore = save.GetType().GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(restore, "SaveSystem.Restore が見つからない");

        restore.Invoke(save, new object[] { arrived });
    }

    private static IEnumerator GoToRoot(string rootId)
    {
        Travel().Go(Route.To(rootId, Arrival.FromPrevious));
        yield return Arrive();
        Assert.AreEqual(rootId, Travel().Current.Id);
    }

    private static Component SaveSystem()
    {
        GameObject manager = GameObject.Find("GameManager");
        Assert.IsNotNull(manager, "GameManager が無い");

        return manager.GetComponent(GameType("SaveSystem"));
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

    private static FieldInfo FieldOf(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " が見つからない");

        return field;
    }

    private static void Set(object target, string fieldName, object value)
    {
        FieldOf(target, fieldName).SetValue(target, value);
    }

    //保存の一覧から、欄 key が value の項目を抜く。項目の型は Assembly-CSharp にあるので名前で引く
    private static void Strip(object data, string listName, string key, string value)
    {
        IList list = (IList)FieldOf(data, listName).GetValue(data);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if ((string)FieldOf(list[i], key).GetValue(list[i]) == value) list.RemoveAt(i);
        }
    }

    //Assembly-CSharp は asmdef から参照できないため名前で引く
    private static Type GameType(string typeName)
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName))
            .FirstOrDefault(t => t != null);
        Assert.IsNotNull(type, typeName + " 型が見つからない");

        return type;
    }

    private static FieldInfo StaticField(string typeName, string fieldName)
    {
        FieldInfo field = GameType(typeName).GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, typeName + "." + fieldName + " が見つからない");

        return field;
    }
}
