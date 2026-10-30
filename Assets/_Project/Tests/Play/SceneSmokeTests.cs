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

        Assert.IsTrue(Travel().Through(ExitKind.ToOrderedRoot, Ordered("Root1")));
        yield return Arrive();

        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 拠点からRoot2へ移れる()
    {
        yield return Boot();

        Assert.IsTrue(Travel().Through(ExitKind.ToOrderedRoot, Ordered("Root2")));
        yield return Arrive();

        Assert.AreEqual("Root2", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 根源から拠点へ戻れる()
    {
        yield return Boot();
        Travel().Through(ExitKind.ToOrderedRoot, Ordered("Root1"));
        yield return Arrive();

        Assert.IsTrue(Travel().Through(ExitKind.ToBase, null));
        yield return Arrive();

        Assert.AreEqual(TravelRule.BaseAreaId, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 受注していれば拠点の出口に入るだけで根源へ移る()
    {
        yield return Boot();
        SetStatic("QuestManager", "ordered_id", MainQuestFor("Root1"));

        StepIntoExit();
        yield return ArriveAt("Root1");

        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 受注していなければ出口に入っても移らない()
    {
        yield return Boot();

        StepIntoExit();
        for (int i = 0; i < SettleFrames; i++) yield return null;

        Assert.IsFalse(Travel().IsTraveling);
        Assert.AreEqual(TravelRule.BaseAreaId, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator 根源で倒れると拠点へ戻る()
    {
        yield return Boot();
        Travel().Through(ExitKind.ToOrderedRoot, Ordered("Root1"));
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

        //クエストも同じ。QuestManager.Start が毎回2本足すので、消さないと溜まる
        var quests = StaticField("QuestManager", "Quests").GetValue(null) as QuestRegistry;
        Assert.IsNotNull(quests, "QuestManager.Quests が QuestRegistry ではない");
        quests.Clear();
        SetStatic("QuestManager", "ordered_id", "");

        //倒れると立つ。次のテストで拠点に着いたとき報酬 UI が出てしまう
        SetStatic("RewardUI", "rewardUI_show", 0);

        yield return null;
    }

    private static Quest Ordered(string rootId)
    {
        return new Quest("X0", rootId, "MainSpawner", 1, null);
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
    private static void StepIntoExit()
    {
        Exit exit = UnityEngine.Object.FindAnyObjectByType<Exit>();
        Assert.IsNotNull(exit, "拠点に出口が無い");

        Transform player = GameObject.FindWithTag("Player").transform;
        Vector3 at = exit.transform.position;
        player.position = new Vector3(at.x, player.position.y, at.z);
    }

    //QuestManager.Start が根源ごとに足す調査クエスト
    private static string MainQuestFor(string rootId)
    {
        var quests = StaticField("QuestManager", "Quests").GetValue(null) as QuestRegistry;
        Assert.IsNotNull(quests, "QuestManager.Quests が QuestRegistry ではない");

        Quest quest = quests.All.FirstOrDefault(q => q.Kind == QuestKind.Main && q.RootId == rootId);
        Assert.IsNotNull(quest, rootId + " の調査クエストが無い");

        return quest.Id;
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
