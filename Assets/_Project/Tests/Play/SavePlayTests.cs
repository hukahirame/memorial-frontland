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

//保存を読み込んだあと、場所に着いたときに戻るもの。保存はいま止まっていてファイルも無いので、
//読み込んだ中身は SaveSystem.savedata へ直に入れ、着いたときの戻しは SaveSystem.Restore を直に呼ぶ
public class SavePlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    [UnityTest]
    public IEnumerator 保存した立ち位置は保存した場所に着いたときだけ戻る()
    {
        yield return Boot();
        Transform player = GameObject.FindWithTag("Player").transform;
        Vector3 standing = player.position;
        Vector3 saved = standing + new Vector3(1f, 0f, 1f);
        object data = Loaded("Root1", saved);

        //エディタで場所のシーンから始めると、保存した場所と違う場所に着く
        Restore(Travel().Current);
        Assert.AreEqual(standing, player.position, "保存した場所でないのに、保存した立ち位置に立つ");

        Set(data, "entered_scene", Travel().Current.Id);
        Restore(Travel().Current);
        Assert.AreEqual(saved, player.position);
    }

    [UnityTest]
    public IEnumerator 保存した敵は保存した根源に入ったときだけ戻る()
    {
        yield return Boot();
        object data = Loaded("Root2", Vector3.zero);
        List<Vector3> respawns = (List<Vector3>)FieldOf(data, "respawns").GetValue(data);
        ((List<string>)FieldOf(data, "enemies").GetValue(data)).Add("Slime");
        respawns.Add(new Vector3(0f, 1f, 0f));

        yield return GoToRoot("Root1");
        Assert.AreEqual(1, respawns.Count, "保存していない根源で、保存した敵が戻された");

        yield return GoToRoot("Root2");
        Assert.AreEqual(0, respawns.Count, "保存した根源に入っても、保存した敵が戻らない");
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

    //読み込んだ保存。体力・所持金・経過秒・持ち物はいまのまま入れ、場所と立ち位置だけ変える
    private static object Loaded(string areaId, Vector3 respawn)
    {
        Component save = SaveSystem();
        object data = FieldOf(save, "savedata").GetValue(save);
        Component inventory = GameObject.FindWithTag("PlayerInventory").GetComponent(GameType("PlayerInventory"));
        IReadOnlyList<ItemSlot> slots = (IReadOnlyList<ItemSlot>)GameType("PlayerInventory").GetProperty("Slots").GetValue(inventory);

        Set(data, "playerhp", ((Health)StaticField("Player2", "Hp").GetValue(null)).Current);
        Set(data, "coin", ((Wallet)StaticField("GameManager", "Coins").GetValue(null)).Amount);
        Set(data, "daytime", ((DayCycle)StaticField("Sun2", "Cycle").GetValue(null)).ElapsedSeconds);
        Set(data, "slots", new List<ItemSlot>(slots));
        Set(data, "entered_scene", areaId);
        Set(data, "respawn", respawn);

        return data;
    }

    private static void Restore(Area arrived)
    {
        Component save = SaveSystem();
        MethodInfo restore = save.GetType().GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(restore, "SaveSystem.Restore が見つからない");

        restore.Invoke(save, new object[] { arrived });
    }

    private static IEnumerator GoToRoot(string rootId)
    {
        Travel().Go(Route.To(rootId, Arrival.FromPrevious));
        yield return Arrive();
        Assert.AreEqual(rootId, Travel().Current.Id);
    }

    private static Component SaveSystem()
    {
        GameObject manager = GameObject.Find("GameManager");
        Assert.IsNotNull(manager, "GameManager が無い");

        return manager.GetComponent(GameType("SaveSystem"));
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

    private static FieldInfo FieldOf(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " が見つからない");

        return field;
    }

    private static void Set(object target, string fieldName, object value)
    {
        FieldOf(target, fieldName).SetValue(target, value);
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
