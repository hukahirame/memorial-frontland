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

//曲は裏で読む。場所に着いたときは、覆いが開くまでに着いた場所の曲が鳴り始めている。
//曲は Git 管理外（Assets/Resources）なので、無い環境では確かめられないとして止める
public class BgmPlayTests
{
    private const float TravelSeconds = 10f;
    private const int SettleFrames = 60;

    /// <summary>核を壊してから曲が替わるまで待つ上限（実時間の秒）</summary>
    private const float SwitchSeconds = 5f;

    private const string RootBgm = "solarisnoame";
    private const string BreakBgm = "dark city";

    [UnityTest]
    public IEnumerator 根源に着いて覆いが開いたときには根源の曲が鳴っている()
    {
        yield return Boot();

        AreaTravel travel = Travel();
        travel.Go(Route.To("Root1", Arrival.FromPrevious));
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while ((travel.IsTraveling || travel.Current.Id != "Root1") && Time.realtimeSinceStartup < giveUp) yield return null;
        Assert.AreEqual("Root1", travel.Current.Id, "根源1 に着かない");

        AudioSource bgm = Bgm();
        Require(RootBgm);
        Assert.IsNotNull(bgm.clip, "曲が入っていない");
        Assert.AreEqual(RootBgm, bgm.clip.name);
        Assert.IsTrue(bgm.isPlaying, "覆いが開いたのに曲が鳴っていない");
    }

    [UnityTest]
    public IEnumerator 核を壊すと曲が替わる()
    {
        yield return Boot();
        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();

        //隣の根源も読み込んであるので、今いる根源の核を探す
        Component spawner = UnityEngine.Object.FindObjectsByType(GameType("OF_Spawner"), FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(found => found.gameObject.scene == SceneManager.GetActiveScene());
        Assert.IsNotNull(spawner, "根源に核が無い");
        Assert.IsTrue((bool)GameType("OF_Spawner").GetMethod("SpawnerBreak").Invoke(spawner, new object[] { 10000 }), "核が壊れない");

        AudioSource bgm = Bgm();
        float giveUp = Time.realtimeSinceStartup + SwitchSeconds;
        while ((bgm.clip == null || bgm.clip.name != BreakBgm) && Time.realtimeSinceStartup < giveUp) yield return null;

        Require(BreakBgm);
        Assert.IsNotNull(bgm.clip, "曲が入っていない");
        Assert.AreEqual(BreakBgm, bgm.clip.name);
        Assert.IsTrue(bgm.isPlaying, "替えた曲が鳴っていない");
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

    /// <summary>曲が無い環境では、鳴るかを確かめられない</summary>
    private static void Require(string clip)
    {
        if (Resources.Load<AudioClip>(clip) == null) Assert.Inconclusive(clip + " が Assets/Resources に無い");
    }

    private static AudioSource Bgm()
    {
        GameObject manager = GameObject.Find("GameManager");
        Assert.IsNotNull(manager, "GameManager が無い");

        return manager.GetComponent<AudioSource>();
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

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");

        return travel;
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
}
