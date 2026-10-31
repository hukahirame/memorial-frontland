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

//掲示板で受注し、根源で拠点を壊して帰るまでの流れ。結果の画面と受注の状態を見る。
//掲示板は地図から開く代わりに QuestManager.ShowQuestUI を、枠を押す代わりに QuestButton.QuestOrder を直に呼ぶ
public class QuestPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>倒れてから拠点に戻されるまでの上限（秒）。起き上がるまでの 5 秒を含む</summary>
    private const float ReviveSeconds = 12f;

    /// <summary>壊れた拠点が見えなくなるのを待つ秒数。膨らんで縮む 0.25 秒に余裕を足す</summary>
    private const float BurstSeconds = 0.5f;

    [UnityTest]
    public IEnumerator 始めから根源ごとに調査とサブが1本ずつある()
    {
        yield return Boot();

        foreach (string root in new[] { "Root1", "Root2" })
        {
            MainQuestFor(root);
            Assert.AreEqual(1, Quests().All.Count(q => q.Kind == QuestKind.Sub && q.RootId == root), root + " のサブ");
        }
    }

    [UnityTest]
    public IEnumerator サブが残っている根源にはサブを足さない()
    {
        yield return Boot();
        Quest sub = SubQuestFor("Root1");

        IssueSub("Root1");
        Assert.AreEqual(1, Quests().All.Count(q => q.Kind == QuestKind.Sub && q.RootId == "Root1"), "1 体で両方が進み、2 本ぶん払う");

        Quests().Remove(sub.Id);
        IssueSub("Root1");
        Assert.IsNotNull(Quests().All.FirstOrDefault(q => q.Kind == QuestKind.Sub && q.RootId == "Root1"), "受け取ったあとに次が出ない");
    }

    [UnityTest]
    public IEnumerator サブは達成した1回だけ知らせ掲示板に達成済みと出る()
    {
        yield return Boot();
        Quest sub = SubQuestFor("Root1");
        string objective = "スライムを" + sub.Amount + "体討伐する";
        yield return OrderAndGo(MainQuestFor("Root1"), "Root1");

        for (int i = 0; i < sub.Amount; i++) Sync("Slime");
        Assert.AreEqual("サブクエスト達成：" + objective, Shown(Middle(), "txt"));

        Middle().Midtxt_Anim("別の知らせ");
        Sync("Slime");
        Assert.AreEqual("別の知らせ", Shown(Middle(), "txt"), "倒すたびに達成を知らせ直す");

        OpenBoard("Root1");
        yield return null;

        Component button = Button(sub.Id);
        Assert.AreEqual("達成済み", Status(button));
        StringAssert.Contains(objective, TextOf(button.transform.Find("BodyText").gameObject), "対象が内部名のまま出る");
        StringAssert.Contains("コイン x 100", TextOf(button.transform.Find("RewardText").gameObject));
    }

    [UnityTest]
    public IEnumerator 拠点を壊すと破壊と出て帰るよう促し見えなくなる()
    {
        yield return Boot();
        yield return OrderAndGo(MainQuestFor("Root1"), "Root1");

        Component spawner = Spawner();
        Vector3 size = spawner.transform.localScale;

        Assert.IsTrue(Break(spawner), "壊した一撃と扱わない");
        Assert.IsFalse(Break(spawner), "壊れたあとにもう一度壊れる");

        BigText big = UnityEngine.Object.FindAnyObjectByType<BigText>();
        Assert.AreEqual("破壊", Shown(big, "txt2"), "根源を出る前に倒れると失敗になるのに達成と出る");
        StringAssert.Contains("生きて根源を出る", Shown(big, "txt1"));

        yield return new WaitForSeconds(BurstSeconds);
        Assert.IsFalse(spawner.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "壊れても見えたまま");
        Assert.AreEqual(size, spawner.transform.localScale, "縮んだまま残る");
    }

    [UnityTest]
    public IEnumerator 調査とサブが並んでも枠は別の段に出て調査の枠を押せる()
    {
        yield return Boot();
        string main = MainQuestFor("Root1");
        SubQuestFor("Root1");

        OpenBoard("Root1");
        yield return null;

        Component[] buttons = Buttons();
        Assert.AreEqual(2, buttons.Length);

        //後に並ぶ枠ほど手前に描かれ、押した位置の入力を先に受ける
        Component mainButton = Button(main);
        Vector2 center = Area(mainButton).center;
        foreach (Component other in buttons.Where(b => b.transform.GetSiblingIndex() > mainButton.transform.GetSiblingIndex()))
        {
            Assert.IsFalse(Area(other).Contains(center), other.name + " の枠が調査の枠を覆う");
        }

        Order(mainButton);
        Assert.AreEqual(main, OrderedId());
    }

    [UnityTest]
    public IEnumerator 掲示板を開き直しても枠は積もらない()
    {
        yield return Boot();

        OpenBoard("Root1");
        yield return null;
        OpenBoard("Root1");
        yield return null;
        OpenBoard("Root2");
        yield return null;

        CollectionAssert.AreEquivalent(new[] { MainQuestFor("Root2"), SubQuestFor("Root2").Id },
                                       Buttons().Select(b => b.name).ToArray(), "前に開いた枠が残る");
    }

    [UnityTest]
    public IEnumerator 拠点を壊して帰ると達成済みの報酬をすべて受け取り受注が空く()
    {
        yield return Boot();
        string main = MainQuestFor("Root1");
        Quest sub = SubQuestFor("Root1");
        int coins = Coins().Amount;

        yield return OrderAndGo(main, "Root1");

        for (int i = 0; i < sub.Amount; i++) Sync("Slime");
        Assert.IsTrue(sub.IsComplete, "根源にいる間にサブが進まない");
        BreakSpawner();
        Assert.IsTrue(Quests().Find(main).IsComplete, "拠点を壊しても達成にならない");

        yield return WalkTo(TravelRule.BaseAreaId);

        Assert.AreEqual("成功", Text("resulttxt"));
        StringAssert.Contains("0 % → 15 %", Text("sidetxt"));
        StringAssert.Contains("コイン　x　200", Text("sidetxt"), "調査とサブの両方が出ない");
        Assert.AreEqual(coins + 200, Coins().Amount);
        Assert.AreEqual("", OrderedId());
        Assert.IsFalse(Quests().All.Any(quest => quest.IsComplete), "受け取ったクエストが残る");
    }

    [UnityTest]
    public IEnumerator 拠点を壊してから根源を出る前に倒れると失敗になりもう一度挑める()
    {
        yield return Boot();
        string main = MainQuestFor("Root1");

        yield return OrderAndGo(main, "Root1");
        BreakSpawner();
        Assert.IsTrue(Quests().Find(main).IsComplete);

        yield return DieAndRevive();

        Assert.AreEqual(TravelRule.BaseAreaId, Travel().Where.AreaId);
        Assert.AreEqual("失敗", Text("resulttxt"));
        Assert.IsFalse(Quests().Find(main).IsComplete, "達成のまま残り、受け取りも解除もできない");
        Assert.AreEqual(main, OrderedId(), "受注が外れた");

        OpenBoard("Root1");
        yield return null;
        Assert.AreEqual("受注済み", Status(Button(main)));

        yield return WalkTo("Root1");
        BreakSpawner();
        yield return WalkTo(TravelRule.BaseAreaId);

        Assert.AreEqual("成功", Text("resulttxt"));
        Assert.AreEqual("", OrderedId());
    }

    [UnityTest]
    public IEnumerator 成功のあとで失敗すると失敗と出る()
    {
        yield return Boot();

        yield return OrderAndGo(MainQuestFor("Root1"), "Root1");
        BreakSpawner();
        yield return WalkTo(TravelRule.BaseAreaId);
        Assert.AreEqual("成功", Text("resulttxt"));

        yield return OrderAndGo(MainQuestFor("Root2"), "Root2");
        yield return DieAndRevive();

        Assert.AreEqual("失敗", Text("resulttxt"));
    }

    [UnityTest]
    public IEnumerator 拠点で倒れても結果の画面は出ない()
    {
        yield return Boot();
        string main = MainQuestFor("Root1");
        OpenBoard("Root1");
        yield return null;
        Order(Button(main));

        yield return DieAndRevive();

        Assert.AreEqual(0, StaticField("RewardUI", "rewardUI_show").GetValue(null), "あとで帰ったときに失敗の結果が出る");
        Assert.AreEqual(main, OrderedId(), "拠点で倒れただけで受注が外れた");
    }

    [UnityTest]
    public IEnumerator 受注と解除は対象の根源の外でだけできる()
    {
        yield return Boot();
        string root1 = MainQuestFor("Root1");
        string root2 = MainQuestFor("Root2");
        yield return WalkTo("Root1");

        OpenBoard("Root1");
        yield return null;
        Order(Button(root1));
        Assert.AreEqual("", OrderedId(), "対象の根源の中で受注できる");

        OpenBoard("Root2");
        yield return null;
        Order(Button(root2));
        Assert.AreEqual(root2, OrderedId(), "ほかの根源から受注できない");

        yield return WalkTo("Root2");
        OpenBoard("Root2");
        yield return null;
        Order(Button(root2));
        Assert.AreEqual(root2, OrderedId(), "対象の根源の中で解除できる");

        yield return WalkTo("Root1");
        OpenBoard("Root2");
        yield return null;
        Order(Button(root2));
        Assert.AreEqual("", OrderedId(), "外に出ても解除できない");
    }

    [UnityTest]
    public IEnumerator 受けたクエストは対象の根源に入るまで始まらず手前で倒れても失敗にならない()
    {
        yield return Boot();
        string root2 = MainQuestFor("Root2");
        OpenBoard("Root2");
        yield return null;
        Order(Button(root2));

        yield return WalkTo("Root1");
        Assert.AreNotEqual(root2, StaticField("RewardUI", "rewardUI_id").GetValue(null), "通り道の根源で始まった");

        yield return DieAndRevive();

        Assert.AreEqual(0, StaticField("RewardUI", "rewardUI_show").GetValue(null), "対象の根源の外で倒れて失敗になった");
        Assert.AreEqual(root2, OrderedId(), "受注が外れた");
    }

    [UnityTest]
    public IEnumerator 拠点を壊して隣の根源へ出ると着いたところで成功と出て報酬を受け取る()
    {
        yield return Boot();
        string main = MainQuestFor("Root1");
        int coins = Coins().Amount;
        yield return OrderAndGo(main, "Root1");
        BreakSpawner();

        yield return WalkTo("Root2");

        BigText big = UnityEngine.Object.FindAnyObjectByType<BigText>();
        Assert.AreEqual("成功", Shown(big, "txt2"), "根源を出ても成功と知らせない");
        StringAssert.Contains("コイン x 100", Shown(big, "txt1"), "受け取った分を出さない");
        Assert.AreEqual(coins + 100, Coins().Amount, "成功と出たのに払わない");
        Assert.IsNull(Quests().Find(main), "受け取ったクエストが残る");
        Assert.AreEqual("", OrderedId(), "受注の枠が空かない");

        yield return DieAndRevive();
        Assert.AreEqual(0, StaticField("RewardUI", "rewardUI_show").GetValue(null), "受け取ったあとに倒れて結果が出る");
    }

    [UnityTest]
    public IEnumerator 成功を受け取ったあと別の根源で失敗すると失敗だけが出る()
    {
        yield return Boot();
        string root1 = MainQuestFor("Root1");
        string root2 = MainQuestFor("Root2");

        yield return SucceedThenOrder(root1, root2);
        yield return WalkTo("Root2");
        yield return DieAndRevive();

        Assert.AreEqual("「水の根源」のクエスト結果", Text("toptxt"));
        Assert.AreEqual("失敗", Text("resulttxt"));
        Assert.IsNull(Quests().Find(root1), "先に受け取った成功が残る");
        Assert.AreEqual(0, Quests().Find(root2).Progress);
        Assert.AreEqual(root2, OrderedId(), "失敗した受注が外れた");
    }

    [UnityTest]
    public IEnumerator 拠点へ戻らずに続けて成功させるとそれぞれ着いたところで受け取る()
    {
        yield return Boot();
        string root1 = MainQuestFor("Root1");
        string root2 = MainQuestFor("Root2");
        int coins = Coins().Amount;

        yield return SucceedThenOrder(root1, root2);
        Assert.AreEqual(coins + 100, Coins().Amount, "1 つ目を受け取らない");

        yield return WalkTo("Root2");
        BreakSpawner();
        yield return WalkTo("Root1");

        Assert.AreEqual(coins + 300, Coins().Amount, "2 つ目を受け取らない");
        Assert.IsNull(Quests().Find(root1));
        Assert.IsNull(Quests().Find(root2));
    }

    //Root1 の調査を成功させて Root2 へ出たあと、Root1 へ戻って Root2 の調査を受ける。拠点へは戻らない
    private static IEnumerator SucceedThenOrder(string root1, string root2)
    {
        yield return OrderAndGo(root1, "Root1");
        BreakSpawner();
        yield return WalkTo("Root2");
        yield return WalkTo("Root1");

        OpenBoard("Root2");
        yield return null;
        Order(Button(root2));
        Assert.AreEqual(root2, OrderedId(), "成功が決まったあとに次を受けられない");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;

        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        ((RootRegistry)StaticField("RootsManager", "Roots").GetValue(null)).Clear();
        ((BaseRaid)StaticField("RootsManager", "Raid").GetValue(null)).Clear();
        Quests().Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);
        StaticField("RewardUI", "rewardUI_id").SetValue(null, "");
        StaticField("Player2", "cool").SetValue(null, false);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    private static IEnumerator OrderAndGo(string questId, string rootId)
    {
        OpenBoard(rootId);
        yield return null;
        Order(Button(questId));
        Assert.AreEqual(questId, OrderedId(), "受注できない");

        yield return WalkTo(rootId);
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

    //場所は拠点のあとに、根源を作った順で一列に並ぶ
    private static string[] RootIds()
    {
        return ((RootRegistry)StaticField("RootsManager", "Roots").GetValue(null)).All.Select(root => root.Id).ToArray();
    }

    //体力を 0 にして、起き上がって拠点に戻されるまで待つ
    private static IEnumerator DieAndRevive()
    {
        Component player = GameObject.FindWithTag("Player").GetComponent(GameType("Player2"));
        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        GameType("Player2").GetMethod("Player_Hurt").Invoke(player, new object[] { hp.Current, Vector3.right });
        Assert.IsTrue(hp.IsDead, "倒れない");

        FieldInfo cool = StaticField("Player2", "cool");
        float giveUp = Time.time + ReviveSeconds;
        while ((bool)cool.GetValue(null) && Time.time < giveUp) yield return null;
        Assert.IsFalse((bool)cool.GetValue(null), "起き上がらない");

        yield return Arrive();
    }

    private static void BreakSpawner()
    {
        Assert.IsTrue(Break(Spawner()), "拠点が壊れない");
    }

    //いま居る根源の拠点
    private static Component Spawner()
    {
        Scene here = SceneManager.GetActiveScene();
        Component spawner = UnityEngine.Object.FindObjectsByType(GameType("OF_Spawner"), FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(found => found.gameObject.scene == here);
        Assert.IsNotNull(spawner, "根源に拠点が無い");

        return spawner;
    }

    //その一撃で壊れたら true
    private static bool Break(Component spawner)
    {
        return (bool)GameType("OF_Spawner").GetMethod("SpawnerBreak").Invoke(spawner, new object[] { 10000 });
    }

    private static void IssueSub(string rootId)
    {
        GameType("QuestManager").GetMethod("IssueSub").Invoke(null, new object[] { rootId });
    }

    private static void Sync(string target)
    {
        GameType("QuestManager").GetMethod("SyncQuest").Invoke(Board(), new object[] { target });
    }

    private static Component Board()
    {
        GameObject board = GameObject.FindWithTag("QuestManager");
        Assert.IsNotNull(board, "掲示板が無い");

        return board.GetComponent(GameType("QuestManager"));
    }

    private static void OpenBoard(string rootId)
    {
        GameType("QuestManager").GetMethod("ShowQuestUI").Invoke(Board(), new object[] { rootId });
    }

    private static Component[] Buttons()
    {
        return Board().GetComponentsInChildren(GameType("QuestButton")).ToArray();
    }

    private static Component Button(string questId)
    {
        Component button = Buttons().FirstOrDefault(found => found.name == questId);
        Assert.IsNotNull(button, questId + " の枠が無い");

        return button;
    }

    private static void Order(Component button)
    {
        GameType("QuestButton").GetMethod("QuestOrder").Invoke(button, null);
    }

    private static Rect Area(Component button)
    {
        Vector3[] corners = new Vector3[4];
        ((RectTransform)button.transform).GetWorldCorners(corners);

        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static string Status(Component button)
    {
        return TextOf(button.transform.Find("StatusText").gameObject);
    }

    //結果の画面の文字
    private static string Text(string fieldName)
    {
        UnityEngine.Object reward = UnityEngine.Object.FindAnyObjectByType(GameType("RewardUI"));
        Assert.IsNotNull(reward, "結果の画面が無い");

        return Shown(reward, fieldName);
    }

    private static MiddleText Middle()
    {
        MiddleText middle = UnityEngine.Object.FindAnyObjectByType<MiddleText>();
        Assert.IsNotNull(middle, "中くらいの文字が無い");

        return middle;
    }

    //文字を出す部品が持つ文字の欄の中身。TextMeshPro は asmdef から参照できないため名前で引く
    private static string Shown(UnityEngine.Object holder, string fieldName)
    {
        Component field = (Component)holder.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(holder);
        return TextOf(field.gameObject);
    }

    private static string TextOf(GameObject holder)
    {
        Component text = holder.GetComponents<Component>().First(c => c.GetType().Name == "TextMeshProUGUI");
        return (string)text.GetType().GetProperty("text").GetValue(text);
    }

    private static string OrderedId()
    {
        return (string)StaticField("QuestManager", "ordered_id").GetValue(null);
    }

    //QuestManager.Start が根源ごとに足す調査クエスト
    private static string MainQuestFor(string rootId)
    {
        Quest quest = Quests().All.FirstOrDefault(q => q.Kind == QuestKind.Main && q.RootId == rootId);
        Assert.IsNotNull(quest, rootId + " の調査クエストが無い");

        return quest.Id;
    }

    //QuestManager.Start が根源ごとに足すサブクエスト
    private static Quest SubQuestFor(string rootId)
    {
        Quest quest = Quests().All.FirstOrDefault(q => q.Kind == QuestKind.Sub && q.RootId == rootId);
        Assert.IsNotNull(quest, rootId + " のサブクエストが無い");

        return quest;
    }

    private static QuestRegistry Quests()
    {
        return (QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null);
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
