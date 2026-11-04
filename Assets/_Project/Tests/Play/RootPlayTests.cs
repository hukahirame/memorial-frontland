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

//Root3 の 2 階建ての作り。核のある階に敵が湧くことと、エスカレーターで上の階へ上がれること。組み直すと壊れうる。
//湧き口は、いつもの湧きの連鎖を止めてから湧かせる口を直に呼ぶ
public class RootPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>Root3 の上の階の床の高さ（FloodedMallBuilder.UpperFloor）</summary>
    private const float Root3UpperFloor = 4f;

    /// <summary>Root3 のエスカレーターが上の階まで横に進む長さ。30 度で高さ 4 を上る</summary>
    private const float Root3EscalatorRun = 6.93f;

    /// <summary>歩かせて着くのを待つ上限（ゲームの秒）。エスカレーターを上りきるのに 10 秒ほどかかる</summary>
    private const float WalkSeconds = 30f;

    [UnityTest]
    public IEnumerator Root3で核が下の階にあると敵は下の階の通路に湧く()
    {
        yield return SpawnsOnCoreFloor(-1000f, 0f);
    }

    [UnityTest]
    public IEnumerator Root3で核が上の階にあると敵は上の階に湧く()
    {
        yield return SpawnsOnCoreFloor(1000f, Root3UpperFloor);
    }

    [UnityTest]
    public IEnumerator Root3ではエスカレーターを歩いて上の階へ上がれる()
    {
        yield return GoToRoot("Root3");
        MonoBehaviour spawner = Spawner();
        spawner.StopAllCoroutines();
        yield return Defeat();

        //エスカレーターの原点は下端の縁の真ん中で、前（forward）へ上る。上の踊り場から右（right）の上の階へ降りる
        Transform escalator = InHere("MallEscalator");
        Component player = Player();
        Rigidbody body = player.GetComponent<Rigidbody>();
        Vector3 start = escalator.position - escalator.forward + Vector3.up * 0.65f;
        body.linearVelocity = Vector3.zero;
        body.position = start;
        player.transform.position = start;

        Time.timeScale = 4f;
        Vector3 up = escalator.forward;
        yield return WalkUntil(up, () => Vector3.Dot(player.transform.position - escalator.position, up) > Root3EscalatorRun + 0.5f);

        Vector3 off = escalator.right;
        yield return WalkUntil(off, () => Vector3.Dot(player.transform.position - escalator.position, off) > 2.5f);
        Time.timeScale = 1f;

        //降りたあとも上の階に居続ける。場の外とみなされると、下の階の手前へ連れ戻される
        for (int i = 0; i < SettleFrames; i++) yield return null;
        Assert.AreEqual(Root3UpperFloor + 0.6f, player.transform.position.y, 0.3f, "上の階に立っていない: " + player.transform.position);
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

    /// <summary>
    /// Root3 の核を、控えた位置にいちばん近い候補に置いて入り、湧いた敵が floor の階の床の 0.6 上に出るかを見る。
    /// 2 回目からは控えた位置にいちばん近い候補に核が置かれる。控える高さ toward を大きく離せば、選ばれる候補の階が決まる
    /// </summary>
    private static IEnumerator SpawnsOnCoreFloor(float toward, float floor)
    {
        yield return Boot();
        RootRegistry roots = (RootRegistry)StaticField("RootsManager", "Roots").GetValue(null);
        roots.Find("Root3").PlaceSpawnPoint(12f, toward, 6f);

        Travel().Go(Route.To("Root3", Arrival.FromPrevious));
        yield return Arrive();

        MonoBehaviour spawner = Spawner();
        spawner.StopAllCoroutines();
        yield return Defeat();

        //核の像は土台を床に載せてあり、像の真ん中は床より上にある
        Assert.That(spawner.transform.position.y, Is.InRange(floor, floor + 3f), "核が狙った階の候補に置かれない: " + spawner.transform.position);

        float range = (float)Field(spawner, "spawnrange").GetValue(spawner);
        int spawned = 0;

        for (int i = 0; i < 20; i++)
        {
            Call(spawner, "Spawn", range * 2f);

            //ほかの階（4 離れる）・吹き抜けの底・像や柱の上には湧かない。崩れた縁の欠けた所は床より 0.4 ほど低いので、1 まで許す
            foreach (Component slime in Slimes())
            {
                Assert.AreEqual(floor + 0.6f, slime.transform.position.y, 1f, "核と同じ階の通路でない所に湧く: " + slime.transform.position);
                spawned++;
            }

            yield return Defeat();
        }

        Assert.Greater(spawned, 0, "核のまわりに湧かない");
    }

    /// <summary>移動のスティックを toward へ倒したまま、done になるまで歩かせる。歩き終えたらスティックを戻す</summary>
    private static IEnumerator WalkUntil(Vector3 toward, Func<bool> done)
    {
        Component player = Player();
        Component stick = (Component)Field(player, "joystick").GetValue(player);
        FieldInfo input = GameType("Joystick").GetField("input", BindingFlags.Instance | BindingFlags.NonPublic);
        input.SetValue(stick, new Vector2(toward.x, toward.z));

        float giveUp = Time.time + WalkSeconds;
        while (!done() && Time.time < giveUp) yield return null;

        input.SetValue(stick, Vector2.zero);
        Assert.IsTrue(done(), "歩いても着かない: " + player.transform.position);
    }

    private static Component Player()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "プレイヤーが居ない");

        Component component = player.GetComponent(GameType("Player2"));
        Assert.IsNotNull(component, "プレイヤーに Player2 が無い");

        return component;
    }

    /// <summary>いま居る場所のシーンの、name という名前の物</summary>
    private static Transform InHere(string name)
    {
        Transform found = Travel().Current.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(part => part.name == name);
        Assert.IsNotNull(found, Travel().Current.Id + " に " + name + " が無い");

        return found;
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
