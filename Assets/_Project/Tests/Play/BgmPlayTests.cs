using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//曲は裏で読む。場所に着いたときは、覆いが開くまでに着いた場所の曲が鳴り始めている。
//境目を歩いて越えたときは、前の曲を鳴らしたまま次の曲へ重ねて移り、同じ曲なら流し続ける。
//曲は Git 管理外（Assets/Music）なので、無い環境では確かめられないとして止める
public class BgmPlayTests
{
    private const float TravelSeconds = 10f;
    private const int SettleFrames = 60;

    /// <summary>核を壊してから曲が替わるまで待つ上限（実時間の秒）</summary>
    private const float SwitchSeconds = 5f;

    /// <summary>出口から、歩いて越えたとみなすまで境目の向こうへ出る距離。SceneSmokeTests と同じ</summary>
    private const float BeyondExit = 1.5f;

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

        Music music = Bgm();
        Require(RootBgm);
        Assert.IsNotNull(music.Clip, "曲が入っていない");
        Assert.AreEqual(RootBgm, music.Clip.name);
        Assert.IsTrue(music.IsPlaying, "覆いが開いたのに曲が鳴っていない");
    }

    [UnityTest]
    public IEnumerator 境目を歩いて越えると前の曲を鳴らしたまま着いた場所の曲へ移る()
    {
        yield return Boot();
        Require(RootBgm);
        Assert.IsTrue(Bgm().IsPlaying, "拠点の曲が鳴っていない（拠点の曲が Assets/Music に無いかもしれない）");

        yield return WalkAcross(ExitKind.ToNext);
        Assert.AreEqual("Root1", Travel().Where.AreaId, "歩いて根源1 へ移らない");

        //替わりきるまでのどのコマでも、何かが鳴っている
        AudioSource[] sources = GameObject.Find("GameManager").GetComponents<AudioSource>();
        float giveUp = Time.realtimeSinceStartup + SwitchSeconds + Music.CrossSeconds;
        while ((Bgm().Clip == null || Bgm().Clip.name != RootBgm || sources.Count(source => source.isPlaying) > 1) && Time.realtimeSinceStartup < giveUp)
        {
            Assert.IsTrue(sources.Any(source => source.isPlaying && source.volume > 0f), "曲を替えるあいだに鳴らないコマがある");
            yield return null;
        }

        Assert.AreEqual(RootBgm, Bgm().Clip.name);
        Assert.IsTrue(Bgm().IsPlaying, "着いた場所の曲が鳴っていない");
        Assert.AreEqual(1, sources.Count(source => source.isPlaying), "前の曲が止まらない");
    }

    [UnityTest]
    public IEnumerator 同じ曲の場所へ歩いて越えても曲を始めからにしない()
    {
        yield return Boot();
        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();
        Require(RootBgm);

        AudioSource playing = GameObject.Find("GameManager").GetComponents<AudioSource>().First(source => source.isPlaying);
        Assert.AreEqual(RootBgm, playing.clip.name);
        float before = playing.time;

        yield return WalkAcross(ExitKind.ToNext);
        Assert.AreEqual("Root2", Travel().Where.AreaId, "歩いて根源2 へ移らない");
        for (int i = 0; i < SettleFrames; i++) yield return null;

        Assert.IsTrue(playing.isPlaying, "同じ曲なのに止めた");
        Assert.AreEqual(RootBgm, playing.clip.name);
        Assert.Greater(playing.time, before, "同じ曲なのに始めから鳴らし直した");
    }

    [UnityTest]
    public IEnumerator 核を壊すと曲が替わる()
    {
        yield return Boot();
        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();

        //隣の根源も読み込んであるので、今いる根源の核を探す
        RootCore spawner = UnityEngine.Object.FindObjectsByType<RootCore>(FindObjectsSortMode.None)
            .FirstOrDefault(found => found.gameObject.scene == SceneManager.GetActiveScene());
        Assert.IsNotNull(spawner, "根源に核が無い");
        spawner.Hit(10000);
        Assert.IsTrue(spawner.IsBroken, "核が壊れない");

        Music music = Bgm();
        float giveUp = Time.realtimeSinceStartup + SwitchSeconds;
        while ((music.Clip == null || music.Clip.name != BreakBgm) && Time.realtimeSinceStartup < giveUp) yield return null;

        Require(BreakBgm);
        Assert.IsNotNull(music.Clip, "曲が入っていない");
        Assert.AreEqual(BreakBgm, music.Clip.name);
        Assert.IsTrue(music.IsPlaying, "替えた曲が鳴っていない");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトを捨てる()
    {
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        yield return null;
    }

    /// <summary>曲が無い環境では、鳴るかを確かめられない</summary>
    private static void Require(string clip)
    {
        AsyncOperationHandle<IList<IResourceLocation>> found = Addressables.LoadResourceLocationsAsync(clip, typeof(AudioClip));
        int count = found.WaitForCompletion().Count;
        Addressables.Release(found);

        if (count == 0) Assert.Inconclusive(clip + " が Assets/Music に無い");
    }

    private static Music Bgm()
    {
        GameObject manager = GameObject.Find("GameManager");
        Assert.IsNotNull(manager, "GameManager が無い");

        Music music = manager.GetComponent<Music>();
        Assert.IsNotNull(music, "GameManager に Music が無い");

        return music;
    }

    //境目の向こうの場所が読み込まれるのを待ち、プレイヤーを出口から境目の向こうへ置く。SceneSmokeTests と同じ
    private static IEnumerator WalkAcross(ExitKind kind)
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (!travel.CanWalk(kind, RootIds()) && Time.realtimeSinceStartup < giveUp) yield return null;
        Assert.IsTrue(travel.CanWalk(kind, RootIds()), "境目の向こうの場所が読み込まれない");

        string from = travel.Where.AreaId;
        Transform player = GameObject.FindWithTag("Player").transform;
        Scene here = SceneManager.GetActiveScene();
        Exit exit = UnityEngine.Object.FindObjectsByType<Exit>(FindObjectsSortMode.None)
            .First(found => found.gameObject.scene == here && found.Kind == kind);
        float toward = kind == ExitKind.ToNext ? 1f : -1f;
        player.position = new Vector3(exit.transform.position.x + toward * BeyondExit, player.position.y, exit.transform.position.z);

        for (int i = 0; i < SettleFrames && travel.Where.AreaId == from; i++) yield return null;
    }

    //場所は拠点のあとに、根源を作った順で一列に並ぶ
    private static string[] RootIds()
    {
        return UnityEngine.Object.FindAnyObjectByType<RootsManager>().Roots.All.Select(root => root.Id).ToArray();
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
}
