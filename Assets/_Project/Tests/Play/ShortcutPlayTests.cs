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

//地図から近道で根源へ移り、根源から拠点へ戻る。地図の根源のボタンを押す代わりに RootUI.Show_QM を、
//近道のボタンを押す代わりに RootShortcut.Go と HomeShortcut.Go を直に呼ぶ
public class ShortcutPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    [UnityTest]
    public IEnumerator 近道を押すと払ってその根源の手前の入口に着き地図が閉じる()
    {
        yield return Boot();
        int coins = Coins().Amount;
        OpenMap();
        yield return ShowDetail("Root2");

        Go();
        yield return Arrive();

        Assert.AreEqual("Root2", Travel().Where.AreaId, "近道で根源へ移らない");
        Assert.AreEqual(coins - TravelRule.ShortcutFee, Coins().Amount);
        Assert.AreEqual(Coins().Amount.ToString(), TextOf(GameObject.Find("CoinText")), "払った額が表示に出ない");
        Assert.Less(FlatDistance(GameObject.FindWithTag("Player").transform.position, Travel().Current.PointFor(Arrival.FromPrevious)), 0.5f,
                    "手前の出口の前に立たない");
        //閉じると画面の外（y 5000）へ送られる。開いた位置は画面の真ん中
        Assert.AreEqual(5000f, GameObject.FindWithTag("QuestManager").transform.position.y, 1f, "根源の詳細が開いたまま");
        Assert.AreEqual(5000f, GameObject.FindWithTag("RootsManager").transform.position.y, 1f, "地図が開いたまま");
    }

    [UnityTest]
    public IEnumerator 根源からは地図で無料で拠点の決まった地点へ戻れて拠点では出ない()
    {
        yield return Boot();
        OpenMap();
        Assert.IsFalse(Home().gameObject.activeSelf, "拠点で拠点への近道が出る");

        yield return WalkTo("Root2");
        int coins = Coins().Amount;
        OpenMap();
        Assert.IsTrue(Home().gameObject.activeSelf, "根源で拠点への近道が出ない");
        StringAssert.Contains("無料", HomeLabel());

        GoHome();
        yield return Arrive();

        Assert.AreEqual(TravelRule.BaseAreaId, Travel().Where.AreaId, "拠点へ戻らない");
        Assert.AreEqual(coins, Coins().Amount, "無料のはずが払っている");
        Assert.Less(FlatDistance(GameObject.FindWithTag("Player").transform.position, Travel().Current.PointFor(Arrival.Home)), 0.5f,
                    "拠点の決まった地点に立たない");
        Assert.AreEqual(5000f, GameObject.FindWithTag("RootsManager").transform.position.y, 1f, "地図が開いたまま");
    }

    [UnityTest]
    public IEnumerator 受けたクエストの対象の根源からは拠点へ戻れず通り道の根源からは戻れる()
    {
        yield return Boot();
        Quest main = Quests().All.First(q => q.Kind == QuestKind.Main && q.RootId == "Root1");
        StaticField("QuestManager", "ordered_id").SetValue(null, main.Id);

        yield return WalkTo("Root1");
        OpenMap();
        Assert.IsTrue(Home().gameObject.activeSelf);
        Assert.IsFalse(HomeInteractable(), "クエスト中の根源から戻れる。拠点を壊したあとの脱出が要らなくなる");
        StringAssert.Contains("クエスト中", HomeLabel());
        GoHome();
        yield return Arrive();
        Assert.AreEqual("Root1", Travel().Where.AreaId, "押せないはずのボタンで戻る");

        yield return WalkTo("Root2");
        OpenMap();
        Assert.IsTrue(HomeInteractable(), "通り道の根源から戻れない");
        GoHome();
        yield return Arrive();
        Assert.AreEqual(TravelRule.BaseAreaId, Travel().Where.AreaId);
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;

        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        Roots().Clear();
        ((BaseRaid)StaticField("RootsManager", "Raid").GetValue(null)).Clear();
        ((QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null)).Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);
        StaticField("RewardUI", "rewardUI_id").SetValue(null, "");
        StaticField("Player2", "cool").SetValue(null, false);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    //地図に根源のボタンを並べ直し、その根源の詳細を開く。並べ直したボタンは Start を待ってから押せる
    private static IEnumerator ShowDetail(string rootId)
    {
        Component map = GameObject.FindWithTag("RootsManager").GetComponent(GameType("RootsManager"));
        GameType("RootsManager").GetMethod("RootUIShow").Invoke(map, null);
        yield return null;

        Transform button = map.transform.Find(rootId);
        Assert.IsNotNull(button, rootId + " のボタンが地図に無い");
        GameType("RootUI").GetMethod("Show_QM").Invoke(button.GetComponent(GameType("RootUI")), null);
    }

    //地図のボタンと同じく、地図を画面へ出してから根源のボタンを並べ直す
    private static void OpenMap()
    {
        Transform map = GameObject.FindWithTag("RootsManager").transform;
        map.Find("CloseButton").GetComponent<CloseManager>().OpenClick();
        GameType("RootsManager").GetMethod("RootUIShow").Invoke(map.GetComponent(GameType("RootsManager")), null);
    }

    private static Component Home()
    {
        Component home = GameObject.FindWithTag("RootsManager").GetComponentInChildren(GameType("HomeShortcut"), true);
        Assert.IsNotNull(home, "地図に拠点へ戻るボタンが無い");

        return home;
    }

    private static void GoHome()
    {
        GameType("HomeShortcut").GetMethod("Go").Invoke(Home(), null);
    }

    private static string HomeLabel()
    {
        Component label = (Component)GameType("HomeShortcut").GetField("label", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Home());
        return (string)label.GetType().GetProperty("text").GetValue(label);
    }

    private static bool HomeInteractable()
    {
        Component button = (Component)GameType("HomeShortcut").GetField("button", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Home());
        return (bool)button.GetType().GetProperty("interactable").GetValue(button);
    }

    //出口を通って、並びに沿って隣へ 1 つずつ移り、その場所に着くまで進む
    private static IEnumerator WalkTo(string areaId)
    {
        string[] line = new[] { TravelRule.BaseAreaId }.Concat(RootIds()).ToArray();
        int goal = Array.IndexOf(line, areaId);
        Assert.GreaterOrEqual(goal, 0, areaId + " が並びに無い");

        while (Travel().Where.AreaId != areaId)
        {
            int here = Array.IndexOf(line, Travel().Where.AreaId);
            ExitKind way = goal > here ? ExitKind.ToNext : ExitKind.ToPrevious;
            Assert.IsTrue(Travel().Through(way, RootIds()), Travel().Where.AreaId + " から先へ行けない");
            yield return Arrive();
        }
    }

    private static QuestRegistry Quests()
    {
        return (QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null);
    }

    private static Component Shortcut()
    {
        Component shortcut = GameObject.FindWithTag("QuestManager").GetComponentInChildren(GameType("RootShortcut"), true);
        Assert.IsNotNull(shortcut, "根源の詳細に近道のボタンが無い");

        return shortcut;
    }

    private static void Go()
    {
        GameType("RootShortcut").GetMethod("Go").Invoke(Shortcut(), null);
    }

    private static string TextOf(GameObject holder)
    {
        Component text = holder.GetComponents<Component>().First(c => c.GetType().Name == "TextMeshProUGUI");
        return (string)text.GetType().GetProperty("text").GetValue(text);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    private static RootRegistry Roots()
    {
        return (RootRegistry)StaticField("RootsManager", "Roots").GetValue(null);
    }

    private static string[] RootIds()
    {
        return Roots().All.Select(root => root.Id).ToArray();
    }

    private static Wallet Coins()
    {
        return (Wallet)StaticField("GameManager", "Coins").GetValue(null);
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
