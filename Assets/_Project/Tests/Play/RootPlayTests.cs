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

//根源の湧き口から湧く数と場所、根源の詳細の表示。湧き口は、いつもの湧きの連鎖を止めてから湧かせる口を直に呼んで数える
public class RootPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>湧きの連鎖の 1 回を待つ最長（秒）。15〜29 秒で次が湧く</summary>
    private const float ChainSeconds = 30f;

    /// <summary>浮動小数の比較の許容</summary>
    private const float Tolerance = 1e-3f;

    [UnityTest]
    public IEnumerator 倒した敵は湧きの抑えに数えない()
    {
        yield return GoToRoot("Root1");
        MonoBehaviour spawner = Spawner();
        spawner.StopAllCoroutines();
        yield return Defeat();

        //攻略度 0 の根源では、居る敵が 0 なら見送らない。湧いた累計で数えると 34 体で湧かなくなる
        const int times = 40;
        int spawned = 0;
        for (int i = 0; i < times; i++)
        {
            Call(spawner, "Spawn2", spawner.transform.position + Vector3.up);
            spawned += Slimes().Count;
            yield return Defeat();
        }

        Assert.AreEqual(times, spawned, "倒した敵まで数えて、長く居るほど湧かなくなる");
    }

    [UnityTest]
    public IEnumerator 湧き口を壊したときの大量発生は一度きりで湧きの連鎖は増えない()
    {
        yield return GoToRoot("Root1");
        MonoBehaviour spawner = Spawner();
        spawner.StopAllCoroutines();

        Call(spawner, "SpawnerBreak", 10000);
        Assert.Greater(Slimes().Count, 0, "壊しても湧かない");
        yield return Defeat();

        //いつもの連鎖は止めてあるので、ここで湧くのは壊したときに足された連鎖だけ
        Time.timeScale = 10f;
        int later = 0;
        float until = Time.time + ChainSeconds + 1f;
        while (Time.time < until)
        {
            later += Slimes().Count;
            yield return Defeat();
        }

        Assert.AreEqual(0, later, "壊したときの大量発生のぶん、湧きの連鎖が増えて続く");
    }

    [UnityTest]
    public IEnumerator 根源の敵は湧き口のまわりに湧く()
    {
        yield return GoToRoot("Root1");
        MonoBehaviour spawner = Spawner();
        spawner.StopAllCoroutines();
        yield return Defeat();

        float range = (float)Field(spawner, "spawnrange").GetValue(spawner);
        GameObject seeker = (GameObject)Field(spawner, "seeker").GetValue(spawner);
        Vector3 origin = spawner.transform.position;

        for (int i = 0; i < 10; i++)
        {
            //前に湧かせた所に残っているのと同じく、湧き口から離しておく
            seeker.transform.position = origin + new Vector3(range * 3f, 0f, range * 3f);
            Call(spawner, "Spawn", range);

            foreach (Component slime in Slimes())
            {
                Vector3 offset = slime.transform.position - origin;
                Assert.LessOrEqual(Mathf.Abs(offset.x), range + Tolerance, "湧き口から離れた所に湧く");
                Assert.LessOrEqual(Mathf.Abs(offset.z), range + Tolerance, "湧き口から離れた所に湧く");
            }

            yield return Defeat();
        }
    }

    [UnityTest]
    public IEnumerator 根源の詳細の解明と抑制は達していない根源を開くと元の色に戻る()
    {
        yield return Boot();
        RootRegistry roots = (RootRegistry)StaticField("RootsManager", "Roots").GetValue(null);
        roots.Find("Root1").Gain(SpawnRule.SuppressProgress);

        Component manager = GameObject.FindWithTag("RootsManager").GetComponent(GameType("RootsManager"));
        Call(manager, "RootUIShow");
        yield return null;

        //詳細の枠は掲示板 1 枚を根源どうしで使い回す
        Transform weaken = GameObject.FindWithTag("QuestManager").transform.Find("RootProgress/DetailProgress");
        Transform suppress = weaken.Find("DP2");
        Color weakenAtFirst = ColorOf(weaken);
        Color suppressAtFirst = ColorOf(suppress);

        Call(RootButton(manager, "Root1"), "Show_QM");
        Assert.AreNotEqual(weakenAtFirst, ColorOf(weaken), "達した【解明】に色が付かない");
        Assert.AreNotEqual(suppressAtFirst, ColorOf(suppress), "達した【抑制】に色が付かない");

        Call(RootButton(manager, "Root2"), "Show_QM");
        AssertColor(weakenAtFirst, ColorOf(weaken), "攻略度 0 の根源でも【解明】が達した色のまま");
        AssertColor(suppressAtFirst, ColorOf(suppress), "攻略度 0 の根源でも【抑制】が達した色のまま");
    }

    [UnityTest]
    public IEnumerator 掲示板のタグは掲示板にだけ付いている()
    {
        yield return Boot();
        GameObject[] tagged = GameObject.FindGameObjectsWithTag("QuestManager");

        //敵を倒したときと湧き口を壊したときは、このタグで掲示板を探して進める。どれが返るかは決まっていない
        Assert.AreEqual(1, tagged.Length, "掲示板を探すと、掲示板でない物が返ることがある");
        Assert.IsNotNull(tagged[0].GetComponent(GameType("QuestManager")), "タグの付いた物が掲示板でない");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;

        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        ((RootRegistry)StaticField("RootsManager", "Roots").GetValue(null)).Clear();
        ((BaseRaid)StaticField("RootsManager", "Raid").GetValue(null)).Clear();
        ((QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null)).Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);
        StaticField("Player2", "cool").SetValue(null, false);
        StaticField("Player2", "speed").SetValue(null, 1f);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    private static MonoBehaviour Spawner()
    {
        UnityEngine.Object spawner = UnityEngine.Object.FindAnyObjectByType(GameType("OF_Spawner"));
        Assert.IsNotNull(spawner, "根源に湧き口が無い");

        return (MonoBehaviour)spawner;
    }

    /// <summary>いま居る根源の敵。場外へ落ちて消えたものは数えない</summary>
    private static List<Component> Slimes()
    {
        Scene here = Travel().Current.Scene;

        return UnityEngine.Object.FindObjectsByType(GameType("Slime"), FindObjectsSortMode.None)
            .Cast<Component>()
            .Where(slime => slime.gameObject.scene == here)
            .ToList();
    }

    /// <summary>居る敵をすべて消し、消えるまで待つ</summary>
    private static IEnumerator Defeat()
    {
        foreach (Component slime in Slimes()) UnityEngine.Object.Destroy(slime.gameObject);
        yield return null;
    }

    private static Component RootButton(Component manager, string rootId)
    {
        Transform button = manager.transform.Find(rootId);
        Assert.IsNotNull(button, rootId + " の地図のボタンが無い");

        return button.GetComponent(GameType("RootUI"));
    }

    //UnityEngine.UI は asmdef から参照していないので、Text は名前で引く
    private static Color ColorOf(Transform shown)
    {
        Component text = shown.GetComponent("Text");
        Assert.IsNotNull(text, shown.name + " に Text が無い");

        return (Color)text.GetType().GetProperty("color").GetValue(text);
    }

    private static void AssertColor(Color expected, Color actual, string message)
    {
        Assert.AreEqual(expected.r, actual.r, Tolerance, message);
        Assert.AreEqual(expected.g, actual.g, Tolerance, message);
        Assert.AreEqual(expected.b, actual.b, Tolerance, message);
        Assert.AreEqual(expected.a, actual.a, Tolerance, message);
    }

    private static IEnumerator GoToRoot(string rootId)
    {
        yield return Boot();

        Travel().Go(Route.To(rootId, Arrival.FromPrevious));
        yield return Arrive();
        Assert.AreEqual(rootId, Travel().Current.Id);
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

    private static object Call(Component target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, target.GetType().Name + "." + methodName + " が見つからない");

        return method.Invoke(target, args);
    }

    private static FieldInfo Field(Component target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " が見つからない");

        return field;
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
