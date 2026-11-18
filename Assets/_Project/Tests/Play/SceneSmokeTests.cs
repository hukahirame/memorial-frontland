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

    /// <summary>出口から、歩いて越えたとみなすまで境目の向こうへ出る距離。境目を越えたとみなす幅（AreaTravel.CrossMargin）より広く、出口の当たりより外</summary>
    private const float BeyondExit = 1.5f;

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
    public IEnumerator 受注していなくても拠点の境目を歩いて越えるだけで根源へ移る()
    {
        yield return Boot();

        yield return WalkAcross(ExitKind.ToNext);
        yield return ArriveAt("Root1");

        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 今いる場所とその隣だけを読み込んでおく()
    {
        yield return Boot();
        Travel().Go(Route.To("Root2", Arrival.FromPrevious));
        yield return Arrive();

        AreaTravel travel = Travel();
        string[] expected = { "Root1", "Root2", "Root3" };
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (!expected.All(id => travel.Present.Any(area => area.Id == id)) && Time.realtimeSinceStartup < giveUp) yield return null;

        CollectionAssert.AreEquivalent(expected, travel.Present.Select(area => area.Id).ToArray(), "根源2の隣が読み込まれない");
        Assert.IsFalse(SceneManager.GetSceneByName(TravelRule.BaseAreaId).isLoaded, "2 つ離れた拠点を手放さない");
    }

    [UnityTest]
    public IEnumerator Root1の奥の境目からRoot2へ行き戻ると奥の出口の前に立つ()
    {
        yield return Boot();
        yield return WalkAcross(ExitKind.ToNext);
        yield return ArriveAt("Root1");

        yield return WalkAcross(ExitKind.ToNext);
        yield return ArriveAt("Root2");

        yield return WalkAcross(ExitKind.ToPrevious);
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
    public IEnumerator Root2の奥の扉口からRoot3へ行き戻ると奥の出口の前に立つ()
    {
        yield return Boot();
        Travel().Go(Route.To("Root2", Arrival.FromPrevious));
        yield return Arrive();

        yield return WalkAcross(ExitKind.ToNext);
        yield return ArriveAt("Root3");

        yield return WalkAcross(ExitKind.ToPrevious);
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

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトを捨てる()
    {
        //LoadScene(Single) は DontDestroyOnLoad を消さない。ゲームは使わないが、外部の部品が作ることがある
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        yield return null;
    }

    //場所は拠点のあとに、根源を作った順で一列に並ぶ
    private static string[] RootIds()
    {
        return UnityEngine.Object.FindAnyObjectByType<RootsManager>().Roots.All.Select(root => root.Id).ToArray();
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

    //境目の向こうの場所が読み込まれるのを待ち、プレイヤーを出口から境目の向こうへ置く。高さはそのまま。
    //歩いて越えたときと同じく、画面を覆わずに移ることも見る
    private static IEnumerator WalkAcross(ExitKind kind)
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (!travel.CanWalk(kind, RootIds()) && Time.realtimeSinceStartup < giveUp) yield return null;
        Assert.IsTrue(travel.CanWalk(kind, RootIds()), "境目の向こうの場所が読み込まれない");

        string from = travel.Where.AreaId;
        Transform player = GameObject.FindWithTag("Player").transform;
        Vector3 at = ExitHere(kind).transform.position;
        float toward = kind == ExitKind.ToNext ? 1f : -1f;
        player.position = new Vector3(at.x + toward * BeyondExit, player.position.y, at.z);

        for (int i = 0; i < SettleFrames && travel.Where.AreaId == from; i++)
        {
            Assert.IsFalse(travel.IsTraveling, "歩いて境目を越えたのに画面を覆った");
            yield return null;
        }
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

}
