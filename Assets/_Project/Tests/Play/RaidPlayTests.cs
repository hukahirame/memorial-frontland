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

//根源の氾濫が拠点へ届くことと、根源で敵を倒して氾濫を防げること。攻め込んだ敵が拠点の中心へ寄り、
//見つけたプレイヤーへ向かうこと。倒れて中心の近くへ戻されても、しばらくは当たらないこと。
//零時を待たず RootsManager.Dayover を直に呼ぶ
public class RaidPlayTests
{
    private const int SettleFrames = 120;
    private const int TravelFrames = 600;

    /// <summary>湧いてから測るまでに、うろついて離れうる距離</summary>
    private const float Drift = 2.5f;

    /// <summary>倒れてから戻され終わるまでを待つ上限</summary>
    private const float ReviveGiveUpSeconds = 12f;

    /// <summary>秒数を待つときの余裕</summary>
    private const float Slack = 0.15f;

    [UnityTest]
    public IEnumerator 拠点に居るあいだは間を空けて1体ずつ湧く()
    {
        yield return Boot();
        int before = SlimesHere();

        BringToEdgeOfStampede("Root1");
        Dayover();

        Assert.AreEqual(0, Roots().Find("Root1").Accumulation, "氾濫で蓄積値が 0 に戻る");

        yield return WaitUntilSlimes(before + 1, BaseRaid.IntervalSeconds + 2f);
        Assert.AreEqual(before + 1, SlimesHere(), "1体目");

        yield return new WaitForSeconds(BaseRaid.IntervalSeconds * 0.5f);
        Assert.AreEqual(before + 1, SlimesHere(), "間を空けずに2体目が出た");

        yield return WaitUntilSlimes(before + 2, BaseRaid.IntervalSeconds + 2f);
        Assert.AreEqual(before + 2, SlimesHere(), "2体目");
        Assert.AreEqual(Root.StampedeRaiders - 2, Raid().Pending);
    }

    [UnityTest]
    public IEnumerator 留守のあいだの氾濫は戻ったときにまとめて湧く()
    {
        yield return Boot();
        int atBase = SlimesHere();

        yield return GoTo("Root1");

        BringToEdgeOfStampede("Root1");
        Dayover();
        for (int i = 0; i < SettleFrames; i++) yield return null;

        Assert.AreEqual(Root.StampedeRaiders, Raid().Pending, "根源の中では湧かず、拠点へ向かう");

        yield return GoHome();

        Assert.AreEqual(0, Raid().Pending);
        Assert.AreEqual(atBase + Root.StampedeRaiders, SlimesHere());
    }

    [UnityTest]
    public IEnumerator 拠点を離れるとき生き残っている敵は戻ったときにまた湧く()
    {
        yield return Boot();
        int before = SlimesHere();

        BringToEdgeOfStampede("Root1");
        Dayover();
        yield return WaitUntilSlimes(before + 1, BaseRaid.IntervalSeconds + 2f);
        Assert.AreEqual(before + 1, SlimesHere(), "出かける前に1体出ている");

        yield return GoTo("Root1");
        Assert.AreEqual(Root.StampedeRaiders, Raid().Pending, "出ていた1体が積み戻される");

        yield return GoHome();
        Assert.AreEqual(before + Root.StampedeRaiders, SlimesHere());
    }

    [UnityTest]
    public IEnumerator 倒した敵は積み戻されない()
    {
        yield return Boot();
        var existing = new HashSet<Component>(SlimeObjectsHere());

        BringToEdgeOfStampede("Root1");
        Dayover();
        yield return WaitUntilSlimes(existing.Count + 1, BaseRaid.IntervalSeconds + 2f);
        Assert.AreEqual(existing.Count + 1, SlimesHere(), "出かける前に1体出ている");

        IDamageable raider = SlimeObjectsHere().First(slime => !existing.Contains(slime)).GetComponent<IDamageable>();
        raider.TakeDamage(raider.CurrentHp);

        yield return GoTo("Root1");
        Assert.AreEqual(Root.StampedeRaiders - 1, Raid().Pending);
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

    [UnityTest]
    public IEnumerator 留守のあいだに攻め込んだ敵は拠点の中心のまわりに湧く()
    {
        yield return Boot();
        yield return GoTo("Root1");

        BringToEdgeOfStampede("Root1");
        Dayover();

        yield return GoHome();

        Vector3 center = CenterHere();
        List<Component> raiders = RaidersHere();
        Assert.AreEqual(Root.StampedeRaiders, raiders.Count, "攻め込んだ敵が行き先を持たない");

        foreach (Component raider in raiders)
        {
            Assert.Less(Apart(raider.transform.position, center), AimRule.GoalRadius + Drift, "中心から離れたところに湧いた");
        }
    }

    [UnityTest]
    public IEnumerator 拠点に攻め込んだ敵は中心へ寄っていく()
    {
        yield return Boot();
        Vector3 center = CenterHere();

        var gate = (Component)UnityEngine.Object.FindAnyObjectByType(GameType("MS_Spawner"));
        Assert.IsNotNull(gate, "拠点の湧き口が無い");
        MethodInfo setGoal = GameType("Slime").GetMethod("SetGoal");

        //1体ずつは乱数でぶれるので、何体かの平均で見る
        var raiders = new List<Component>();
        for (int i = 0; i < Root.StampedeRaiders; i++)
        {
            Component raider = PlaceSlime(gate.transform.position + Vector3.forward * (i - 2) * 0.6f);
            setGoal.Invoke(raider, new object[] { center });
            raiders.Add(raider);
        }

        float before = raiders.Average(raider => Apart(raider.transform.position, center));
        Assert.Greater(before, AimRule.GoalRadius, "湧き口が中心に近すぎて寄るかを見られない");

        yield return new WaitForSeconds(15f);

        float after = raiders.Average(raider => Apart(raider.transform.position, center));
        Assert.Less(after, before - 1f, "中心へ寄っていかない");
    }

    [UnityTest]
    public IEnumerator 見えるところのプレイヤーへ向きを合わせる()
    {
        yield return Boot();
        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        hp.SetCurrent(hp.Max);

        Transform player = GameObject.FindWithTag("Player").transform;
        Component slime = PlaceSlime(player.position + Vector3.right * (AimRule.SightRange - 1f));
        FieldInfo attitude = GameType("Slime").GetField("attitude", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(attitude, "Slime.attitude が見つからない");

        //最初の行動は Start で選ぶ。プレイヤーは -x の側に居る
        yield return null;

        Vector3 toward = (Vector3)attitude.GetValue(slime);
        Assert.Less(toward.x, -0.95f, "プレイヤーの方を向かない");
    }

    [UnityTest]
    public IEnumerator 倒れて戻ってからしばらくは当たらない()
    {
        yield return Boot();
        Component player = GameObject.FindWithTag("Player").GetComponent(GameType("Player2"));
        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);

        Hurt(player, hp.Current);
        Assert.IsTrue(hp.IsDead, "倒れない");
        yield return WaitForRevive();

        int revived = hp.Current;
        Hurt(player, 1);
        Assert.AreEqual(revived, hp.Current, "戻ってすぐに当たった");

        yield return new WaitForSeconds(Invulnerability.ReviveSeconds + Slack);

        int before = hp.Current;
        Hurt(player, 1);
        Assert.AreEqual(before - 1, hp.Current, "無敵が明けない");
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

    //あと一晩で氾濫する手前まで溜める
    private static void BringToEdgeOfStampede(string rootId)
    {
        Root root = Roots().Find(rootId);
        Assert.IsNotNull(root, rootId + " が無い");

        while (!root.WillOverflowNextDay) root.AccumulateDaily();
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

    private static Component PlaceSlime(Vector3 at)
    {
        GameObject prefab = Resources.Load<GameObject>("Slime");
        Assert.IsNotNull(prefab, "Slime の prefab が無い");

        return UnityEngine.Object.Instantiate(prefab, at, Quaternion.identity).GetComponent(GameType("Slime"));
    }

    private static void Hurt(Component player, int damage)
    {
        GameType("Player2").GetMethod("Player_Hurt").Invoke(player, new object[] { damage, Vector3.right });
    }

    //起き上がって、戻される移動が終わるまで待つ。着いてから間を置かない。戻ったあとの無敵の秒数を食うため
    private static IEnumerator WaitForRevive()
    {
        FieldInfo cool = StaticField("Player2", "cool");
        AreaTravel travel = Travel();
        float giveUp = Time.time + ReviveGiveUpSeconds;

        while ((bool)cool.GetValue(null) && Time.time < giveUp) yield return null;
        Assert.IsFalse((bool)cool.GetValue(null), "起き上がらない");

        while (travel.IsTraveling && Time.time < giveUp) yield return null;
        Assert.IsFalse(travel.IsTraveling, "戻されない");
    }

    private static IEnumerator WaitUntilSlimes(int count, float seconds)
    {
        float until = Time.time + seconds;
        while (SlimesHere() < count && Time.time < until) yield return null;
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
