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

//一日の巡りは常駐シーンにただ 1 つ。場所のシーンをいくつ読み込んでも時刻は 1 秒に 1 つ進み、どの場所の日の光も同じ強さになる。
//点いている日の光は常駐の 1 本だけ
public class DayPlayTests
{
    private const float TravelSeconds = 10f;
    private const int SettleFrames = 60;

    /// <summary>時刻の進みを数える長さ（実時間の秒）。1 秒の刻みの位置しだいで 3 か 4 進む</summary>
    private const float CountSeconds = 3.5f;

    [UnityTest]
    public IEnumerator 場所を2つ読み込んでも時刻は1秒に1つ進み日の光はどれも同じ強さ()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();

        //拠点に居ると、隣の根源1も読み込む
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (!travel.Present.Any(area => area.Id == "Root1") && Time.realtimeSinceStartup < giveUp) yield return null;
        Assert.IsTrue(travel.Present.Any(area => area.Id == "Root1"), "隣の根源1が読み込まれない");
        for (int i = 0; i < SettleFrames; i++) yield return null;

        //前のテストの時刻が残っていると、数えているあいだに零時を跨ぐ
        DayCycle cycle = (DayCycle)StaticField("Sun2", "Cycle").GetValue(null);
        cycle.SetElapsed(0);
        yield return new WaitForSecondsRealtime(CountSeconds);

        Assert.That(cycle.ElapsedSeconds, Is.InRange(3, 4), CountSeconds + " 秒で時刻が " + cycle.ElapsedSeconds + " 進んだ");

        //常駐の日の光と、拠点と根源1 の日の光
        Light[] suns = UnityEngine.Object.FindObjectsByType<Daylight>(FindObjectsSortMode.None)
            .Select(daylight => daylight.GetComponent<Light>())
            .ToArray();
        Assert.AreEqual(3, suns.Length, "常駐と拠点と根源1 の日の光が揃っていない");
        Assert.IsTrue(suns.All(sun => Mathf.Abs(sun.intensity - suns[0].intensity) < 1e-4f), "場所によって日の光の強さが違う");

        Light[] lit = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(light => light.type == LightType.Directional && light.isActiveAndEnabled)
            .ToArray();
        Assert.AreEqual(1, lit.Length, "点いている日の光が 1 本ではない");
        Assert.AreEqual(AreaTravel.CoreScene, lit[0].gameObject.scene.name, "点いている日の光が常駐シーンのものではない");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        //遊びの状態は static にあり、テストのあいだでドメインを読み直さないので残る。タイトルを開くときと同じ戻し方をする
        GameType("GameSession").GetMethod("Reset", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

        yield return null;
    }

    //着いて、着いた場所の Start とコルーチンが一巡するまで待つ
    private static IEnumerator Arrive()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");
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
