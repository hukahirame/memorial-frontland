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

//根源の氾濫が拠点へ届くことと、根源で敵を倒して氾濫を防げること。零時を待たず RootsManager.Dayover を直に呼ぶ
public class RaidPlayTests
{
    private const int SettleFrames = 120;
    private const int TravelFrames = 600;

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

    private static IEnumerator GoTo(string rootId)
    {
        Travel().Through(ExitKind.ToOrderedRoot, new Quest("X0", rootId, "MainSpawner", 1, null));
        yield return Arrive();
    }

    private static IEnumerator GoHome()
    {
        Travel().Through(ExitKind.ToBase, null);
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
