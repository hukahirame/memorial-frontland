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

//根源の氾濫で拠点に敵が攻め込むことと、拠点を離れても攻め込んだ敵が位置と体力ごと残ること。
//根源へ入り直しても、湧き口が同じ位置に置かれること。零時を待たず RootsManager.Dayover を直に呼ぶ
public class RaidPlayTests
{
    private const int SettleFrames = 120;
    private const int TravelFrames = 600;

    /// <summary>湧いてから測るまでに、うろついて離れうる距離</summary>
    private const float Drift = 2.5f;

    /// <summary>生き残った敵に負わせる傷。満タンと見分けられればよい</summary>
    private const int Wound = 100;

    [UnityTest]
    public IEnumerator 拠点に居るあいだの氾濫はすぐ湧き口のまわりにまとめて湧く()
    {
        yield return Boot();
        int before = SlimesHere();

        BringToEdgeOfStampede("Root1");
        Dayover();

        Assert.AreEqual(0, Roots().Find("Root1").Accumulation, "氾濫で蓄積値が 0 に戻る");

        yield return null;
        yield return null;

        Assert.AreEqual(before + Root.StampedeRaiders, SlimesHere(), "まとめて湧かない");
        Assert.AreEqual(0, Raid().Kept.Count, "出したのに控えに残る");
        AssertNearGate(RaidersHere());
    }

    [UnityTest]
    public IEnumerator 留守のあいだの氾濫は戻ったときに湧き口のまわりに湧く()
    {
        yield return Boot();
        int atBase = SlimesHere();

        yield return GoTo("Root1");

        BringToEdgeOfStampede("Root1");
        Dayover();
        for (int i = 0; i < SettleFrames; i++) yield return null;

        Assert.AreEqual(Root.StampedeRaiders, Raid().Kept.Count, "根源の中では湧かず、拠点の控えに入る");

        yield return GoHome();

        Assert.AreEqual(0, Raid().Kept.Count);
        Assert.AreEqual(atBase + Root.StampedeRaiders, SlimesHere());
        AssertNearGate(RaidersHere());
    }

    [UnityTest]
    public IEnumerator 拠点を離れるとき生き残っている敵は戻ったときに同じところへ同じ体力で湧く()
    {
        yield return Boot();

        BringToEdgeOfStampede("Root1");
        Dayover();
        yield return null;
        yield return null;

        List<Component> raiders = RaidersHere();
        Assert.AreEqual(Root.StampedeRaiders, raiders.Count, "出かける前に出ていない");

        //湧き口から離れたところへ移しておく。湧き口のまわりに湧き直しても、同じところに見えないように
        Component moved = raiders[0];
        Vector3 center = CenterHere();
        Vector3 spot = new Vector3(center.x, moved.transform.position.y, center.z);
        moved.transform.position = spot;
        IDamageable hurt = moved.GetComponent<IDamageable>();
        hurt.TakeDamage(Wound);
        int left = hurt.CurrentHp;

        yield return GoTo("Root1");

        Assert.AreEqual(Root.StampedeRaiders, Raid().Kept.Count, "生き残っている敵が控えられない");
        Assert.IsTrue(Raid().Kept.All(raider => raider.HasPosition), "位置が控えられない");
        Assert.AreEqual(1, Raid().Kept.Count(raider => raider.Hp == left), "減った体力が控えられない");

        //出た直後に見る。着いてからはうろついて離れる
        Travel().Go(Route.To(TravelRule.BaseAreaId, Arrival.FromNext));
        for (int i = 0; i < TravelFrames && RaidersHere().Count == 0; i++) yield return null;

        raiders = RaidersHere();
        Assert.AreEqual(Root.StampedeRaiders, raiders.Count, "控えた数だけ湧かない");
        Component back = raiders.OrderBy(raider => Apart(raider.transform.position, spot)).First();
        Assert.Less(Apart(back.transform.position, spot), Drift, "控えた位置に戻らない");
        Assert.AreEqual(left, back.GetComponent<IDamageable>().CurrentHp, "減った体力で戻らない");

        yield return Arrive();
    }

    [UnityTest]
    public IEnumerator 倒した敵は控えに残らない()
    {
        yield return Boot();

        BringToEdgeOfStampede("Root1");
        Dayover();
        yield return null;
        yield return null;

        IDamageable raider = RaidersHere()[0].GetComponent<IDamageable>();
        raider.TakeDamage(raider.CurrentHp);

        yield return GoTo("Root1");
        Assert.AreEqual(Root.StampedeRaiders - 1, Raid().Kept.Count);
    }

    [UnityTest]
    public IEnumerator 根源へ2回目に入っても同じ位置に湧き口が置かれ敵が湧く()
    {
        yield return Boot();
        yield return GoTo("Root1");
        Vector3 first = SpawnerHere().transform.position;

        yield return GoHome();
        yield return GoTo("Root1");

        Assert.AreEqual(first, SpawnerHere().transform.position);
        Assert.Greater(SlimesHere(), 0, "2回目でも敵が湧く");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        Roots().Clear();
        Raid().Clear();
        ((QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null)).Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);

        //倒れる試験が減らした体力を、次のテストへ持ち越さない
        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    //いちばん少なく増えても、あと一晩で氾濫する手前まで溜める
    private static void BringToEdgeOfStampede(string rootId)
    {
        Root root = Roots().Find(rootId);
        Assert.IsNotNull(root, rootId + " が無い");

        root.Restore(root.Progress, Root.StampedeAccumulation - 1);
    }

    private static Component SpawnerHere()
    {
        Scene here = SceneManager.GetActiveScene();
        Component spawner = UnityEngine.Object.FindObjectsByType(GameType("OF_Spawner"), FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(found => found.gameObject.scene == here);
        Assert.IsNotNull(spawner, "湧き口が無い");

        return spawner;
    }

    private static void Dayover()
    {
        Type type = GameType("RootsManager");
        UnityEngine.Object manager = UnityEngine.Object.FindAnyObjectByType(type);
        Assert.IsNotNull(manager, "RootsManager が無い");

        type.GetMethod("Dayover").Invoke(manager, null);
    }

    //いま居る場所で動いているスライム。場外へ落ちたものは非アクティブになり数えない
    private static List<Component> SlimeObjectsHere()
    {
        Scene here = SceneManager.GetActiveScene();

        return UnityEngine.Object.FindObjectsByType(GameType("Slime"), FindObjectsSortMode.None)
            .Cast<Component>()
            .Where(slime => slime.gameObject.scene == here)
            .ToList();
    }

    private static int SlimesHere()
    {
        return SlimeObjectsHere().Count;
    }

    //攻め込んだ敵。湧き口から行き先を渡されたスライム
    private static List<Component> RaidersHere()
    {
        FieldInfo hasGoal = GameType("Slime").GetField("hasGoal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(hasGoal, "Slime.hasGoal が見つからない");

        return SlimeObjectsHere().Where(slime => (bool)hasGoal.GetValue(slime)).ToList();
    }

    private static Component GateHere()
    {
        var gate = (Component)UnityEngine.Object.FindAnyObjectByType(GameType("MS_Spawner"));
        Assert.IsNotNull(gate, "拠点の湧き口が無い");

        return gate;
    }

    //湧き口の側に居る。中心のまわりに湧いたものは、湧き口より中心の方が近い
    private static void AssertNearGate(List<Component> raiders)
    {
        Vector3 gate = GateHere().transform.position;
        Vector3 center = CenterHere();

        foreach (Component raider in raiders)
        {
            Assert.Less(Apart(raider.transform.position, gate), Apart(raider.transform.position, center), "湧き口より中心の近くに湧いた");
        }
    }

    //拠点の中心。場の範囲の真ん中
    private static Vector3 CenterHere()
    {
        Area area = Area.In(SceneManager.GetActiveScene());
        Assert.IsNotNull(area, "いまの場所が無い");

        FieldBounds bounds = Exposition.ToBounds(area.Exposition);
        return new Vector3(bounds.CenterX, 0f, bounds.CenterZ);
    }

    //高さを見ない距離
    private static float Apart(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    private static RootRegistry Roots()
    {
        return (RootRegistry)StaticField("RootsManager", "Roots").GetValue(null);
    }

    private static BaseRaid Raid()
    {
        return (BaseRaid)StaticField("RootsManager", "Raid").GetValue(null);
    }

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");

        return travel;
    }

    //並びを通らず、根源の手前の出口の前へ直に移る。侵攻を見るのに、間の根源は要らない
    private static IEnumerator GoTo(string rootId)
    {
        Travel().Go(Route.To(rootId, Arrival.FromPrevious));
        yield return Arrive();
    }

    private static IEnumerator GoHome()
    {
        Travel().Go(Route.To(TravelRule.BaseAreaId, Arrival.FromNext));
        yield return Arrive();
    }

    private static IEnumerator Boot()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();
    }

    private static IEnumerator Arrive()
    {
        AreaTravel travel = Travel();

        for (int i = 0; i < TravelFrames; i++)
        {
            if (travel.Current != null && !travel.IsTraveling) break;
            yield return null;
        }

        Assert.IsNotNull(travel.Current, "場所に着かない");
        Assert.IsFalse(travel.IsTraveling, "移動が終わらない");

        for (int i = 0; i < SettleFrames; i++) yield return null;
    }

    //Assembly-CSharp は asmdef から参照できないため名前で引く
    private static Type GameType(string typeName)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName))
            .FirstOrDefault(t => t != null);
        Assert.IsNotNull(type, typeName + " 型が見つからない");

        return type;
    }

    private static FieldInfo StaticField(string typeName, string fieldName)
    {
        var field = GameType(typeName).GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, typeName + "." + fieldName + " が見つからない");

        return field;
    }
}
