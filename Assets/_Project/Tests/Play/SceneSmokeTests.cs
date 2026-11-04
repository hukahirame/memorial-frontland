using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//シーンが起動し、場所を移っても例外を出さないことだけを見る。
//ゲームの正しさは見ない。参照割り当ての抜けを捕まえるための網。
public class SceneSmokeTests
{
    private const int SettleFrames = 120; //Start とコルーチンが一巡するまで

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    [UnityTest]
    public IEnumerator 常駐シーンから拠点に着く()
    {
        yield return Boot();
        Assert.AreEqual(TravelRule.BaseAreaId, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 拠点からRoot1へ移れる()
    {
        yield return Boot();

        Assert.IsTrue(Travel().Through(ExitKind.ToNext, RootIds()));
        yield return Arrive();

        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Root1を通ってRoot2へ移れる()
    {
        yield return Boot();
        Travel().Through(ExitKind.ToNext, RootIds());
        yield return Arrive();

        Assert.IsTrue(Travel().Through(ExitKind.ToNext, RootIds()));
        yield return Arrive();

        Assert.AreEqual("Root2", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 根源から1つずつ戻って拠点に着く()
    {
        yield return Boot();
        Travel().Go(Route.To("Root2", Arrival.FromPrevious));
        yield return Arrive();

        Assert.IsTrue(Travel().Through(ExitKind.ToPrevious, RootIds()));
        yield return Arrive();
        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);

        Assert.IsTrue(Travel().Through(ExitKind.ToPrevious, RootIds()));
        yield return Arrive();
        Assert.AreEqual(TravelRule.BaseAreaId, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 受注していなくても拠点の出口に入るだけで根源へ移る()
    {
        yield return Boot();

        StepIntoExit(ExitKind.ToNext);
        yield return ArriveAt("Root1");

        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Root1の奥の出口からRoot2へ行き戻ると奥の出口の前に立つ()
    {
        yield return Boot();
        StepIntoExit(ExitKind.ToNext);
        yield return ArriveAt("Root1");

        StepIntoExit(ExitKind.ToNext);
        yield return ArriveAt("Root2");

        StepIntoExit(ExitKind.ToPrevious);
        yield return ArriveAt("Root1");

        Vector3 at = GameObject.FindWithTag("Player").transform.position;
        Assert.Less(FlatDistance(at, ExitHere(ExitKind.ToNext).transform.position),
                    FlatDistance(at, ExitHere(ExitKind.ToPrevious).transform.position),
                    "奥から戻ったのに手前の入口に立つ");
    }

    [UnityTest]
    public IEnumerator Root2を通ってRoot3へ移れる()
    {
        yield return Boot();
        Travel().Go(Route.To("Root2", Arrival.FromPrevious));
        yield return Arrive();

        Assert.IsTrue(Travel().Through(ExitKind.ToNext, RootIds()));
        yield return Arrive();

        Assert.AreEqual("Root3", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Root2の奥の出口からRoot3へ行き戻ると奥の出口の前に立つ()
    {
        yield return Boot();
        Travel().Go(Route.To("Root2", Arrival.FromPrevious));
        yield return Arrive();

        StepIntoExit(ExitKind.ToNext);
        yield return ArriveAt("Root3");

        StepIntoExit(ExitKind.ToPrevious);
        yield return ArriveAt("Root2");

        Vector3 at = GameObject.FindWithTag("Player").transform.position;
        Assert.Less(FlatDistance(at, ExitHere(ExitKind.ToNext).transform.position),
                    FlatDistance(at, ExitHere(ExitKind.ToPrevious).transform.position),
                    "奥から戻ったのに手前の入口に立つ");
    }

    [UnityTest]
    public IEnumerator 根源で倒れると拠点へ戻る()
    {
        yield return Boot();
        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();

        Travel().Revive();
        yield return Arrive();

        Assert.AreEqual(TravelRule.BaseAreaId, SceneManager.GetActiveScene().name);
    }

    //Play を押し直した直後の状態に戻す。
    //PlayMode テストはテスト間でドメインを再読み込みしないため、
    //オブジェクトを消すだけでは static が前のテストの結果を持ち越す
    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        //LoadScene(Single) は DontDestroyOnLoad を消さない。ゲームは使わないが、外部の部品が作ることがある
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        //根源は Domain 側。RootsManager だけ Assembly-CSharp なので名前で引く
        var roots = StaticField("RootsManager", "Roots").GetValue(null) as RootRegistry;
        Assert.IsNotNull(roots, "RootsManager.Roots が RootRegistry ではない");
        roots.Clear();

        //クエストも同じ。QuestManager.Start が毎回足すので、消さないと溜まる
        var quests = StaticField("QuestManager", "Quests").GetValue(null) as QuestRegistry;
        Assert.IsNotNull(quests, "QuestManager.Quests が QuestRegistry ではない");
        quests.Clear();
        SetStatic("QuestManager", "ordered_id", "");

        //倒れると立つ。次のテストで拠点に着いたとき報酬 UI が出てしまう
        SetStatic("RewardUI", "rewardUI_show", 0);

        yield return null;
    }

    //場所は拠点のあとに、根源を作った順で一列に並ぶ
    private static string[] RootIds()
    {
        var roots = StaticField("RootsManager", "Roots").GetValue(null) as RootRegistry;
        Assert.IsNotNull(roots, "RootsManager.Roots が RootRegistry ではない");

        return roots.All.Select(root => root.Id).ToArray();
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

    //出口に触れてから移り始めるまで間があるので、着いた場所で見分ける
    private static IEnumerator ArriveAt(string areaId)
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;

        while (Time.realtimeSinceStartup < giveUp)
        {
            if (travel.Where.AreaId == areaId && !travel.IsTraveling) break;
            yield return null;
        }

        Assert.AreEqual(areaId, travel.Where.AreaId, "出口から移らない");
        yield return Arrive();
    }

    //プレイヤーを出口の上へ置く。高さはそのままにして、歩いて入ったときと同じ重なり方にする
    private static void StepIntoExit(ExitKind kind)
    {
        Transform player = GameObject.FindWithTag("Player").transform;
        Vector3 at = ExitHere(kind).transform.position;
        player.position = new Vector3(at.x, player.position.y, at.z);
    }

    //いま居る場所の、その種類の出口
    private static Exit ExitHere(ExitKind kind)
    {
        Scene here = SceneManager.GetActiveScene();
        Exit exit = UnityEngine.Object.FindObjectsByType<Exit>(FindObjectsSortMode.None)
            .FirstOrDefault(found => found.gameObject.scene == here && found.Kind == kind);
        Assert.IsNotNull(exit, here.name + " に " + kind + " の出口が無い");

        return exit;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    //Assembly-CSharp は asmdef から参照できない（参照は一方向）ため名前で引く。
    //改名するとコンパイルではなくここで落ちる
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

    private static void SetStatic(string typeName, string fieldName, object value)
    {
        StaticField(typeName, fieldName).SetValue(null, value);
    }

}
