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

//木の柵を持ち物から立てることと、場所を移っても保存して読み直しても、同じ所に同じ向きと耐久で残ること。
//枠を押す代わりに Inventbutton.Show_Info を、使い道のボタンの代わりに Info_set.Install を直に呼ぶ
public class FencePlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>向きの比較の許容（度）</summary>
    private const float AngleTolerance = 0.01f;

    /// <summary>位置の比較の許容</summary>
    private const float Tolerance = 1e-3f;

    private const float Right = 1f;

    /// <summary>拠点の左手前の、何も置いていない平らな所。奥へ 1 マスより先（z が -2.8 より奥）は一段低い池</summary>
    private static readonly Vector3 OpenSpot = new Vector3(-6f, 0.65f, -4f);

    [UnityTest]
    public IEnumerator 持ち物から置くと向いている側の格子の線に柵が立ち1つ減る()
    {
        yield return Boot();
        Face(Right);
        Load("Fence");
        Load("Fence");
        Vector3 at = Player().transform.position;

        Press(Box("Fence"));
        Use();
        yield return null;

        Fence fence = Single();
        Vector3 stood = fence.transform.position;
        Assert.AreEqual(1, Count("Fence"), "置いても持ち物が減らない");
        Assert.Greater(stood.x, at.x, "右を向いているのに右に立たない");
        Assert.AreEqual(0f, Mathf.DeltaAngle(fence.transform.eulerAngles.y, 90f), AngleTolerance, "右を向いて立てた柵が奥行きの向きに沿わない");
        Assert.AreEqual(Mathf.Round(stood.x / FenceRule.Cell) * FenceRule.Cell, stood.x, Tolerance, "格子の線の上に立たない");
        Assert.AreEqual(Mathf.Round(stood.z / FenceRule.Cell) * FenceRule.Cell, stood.z, Tolerance, "真ん中が格子の点にそろわない");
        Assert.AreEqual(at.y - 0.65f, stood.y, 0.1f, "地面の高さに立たない");
    }

    [UnityTest]
    public IEnumerator 場所を移って戻っても柵は同じ所に同じ向きで立っている()
    {
        yield return Boot();
        Face(Right);
        Load("Fence");
        Press(Box("Fence"));
        Use();
        yield return null;
        Vector3 stood = Single().transform.position;

        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();
        Assert.AreEqual(0, Fences().Length, "拠点の柵が根源に立っている");

        Travel().Go(Route.To(TravelRule.BaseAreaId, Arrival.FromNext));
        yield return Arrive();

        Fence fence = Single();
        Assert.AreEqual(0f, Vector3.Distance(stood, fence.transform.position), Tolerance, "戻ると柵の位置が変わる");
        Assert.AreEqual(0f, Mathf.DeltaAngle(fence.transform.eulerAngles.y, 90f), AngleTolerance, "戻ると柵の向きが変わる");
    }

    [UnityTest]
    public IEnumerator 保存して読み直すと柵の向きと耐久も戻る()
    {
        yield return Boot();
        Face(Right);
        Load("Fence");
        Press(Box("Fence"));
        Use();
        yield return null;
        Wear(Single(), 123);

        Type type = GameType("SaveData");
        object saved = Activator.CreateInstance(type);
        type.GetMethod("SyncDynamic").Invoke(saved, new object[] { UnityEngine.Object.FindAnyObjectByType<PlacedObjects>() });
        object loaded = JsonUtility.FromJson(JsonUtility.ToJson(saved), type);

        PlacementLedger ledger = new PlacementLedger();
        type.GetMethod("LoadPlacements").Invoke(loaded, new object[] { ledger });

        Placement fence = ledger.In(TravelRule.BaseAreaId).Single(placement => placement.ItemId == "Fence");
        Assert.AreEqual(0f, Mathf.DeltaAngle(fence.Yaw, 90f), AngleTolerance, "保存から戻すと柵の向きが変わる");
        Assert.AreEqual(123, fence.Durability, "保存から戻すと柵の耐久が変わる");
    }

    [Test]
    public void 向きを持たない古い保存の柵は向き0で戻る()
    {
        Type type = GameType("SaveData");
        object old = JsonUtility.FromJson(
            "{\"saveObjects\":[\"Fence\"],\"saveObjectsScene\":[\"MainSite\"],\"saveObjectsPos\":[{\"x\":1.0,\"y\":0.0,\"z\":-2.0}]}", type);

        PlacementLedger ledger = new PlacementLedger();
        type.GetMethod("LoadPlacements").Invoke(old, new object[] { ledger });

        Assert.AreEqual(1, ledger.In("MainSite").Count);
        Assert.AreEqual(0f, ledger.In("MainSite")[0].Yaw);
        Assert.AreEqual(Placement.NoDurability, ledger.In("MainSite")[0].Durability, "耐久を持たない古い保存の柵が満タンで立たない");
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

        //倒れたまま終えると、起き上がる前にプレイヤーごと捨てられ、次のテストで動けない
        StaticField("Player2", "cool").SetValue(null, false);
        StaticField("Player2", "speed").SetValue(null, 1f);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    private static Fence[] Fences()
    {
        return UnityEngine.Object.FindObjectsByType<Fence>(FindObjectsSortMode.None);
    }

    private static Fence Single()
    {
        Fence[] fences = Fences();
        Assert.AreEqual(1, fences.Length, "柵が 1 つだけ立っていない");

        return fences[0];
    }

    /// <summary>柵の残りの耐久を left にする</summary>
    private static void Wear(Fence fence, int left)
    {
        FenceDurability state = (FenceDurability)typeof(Fence).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fence);
        state.Strike(state.Current - left, Time.time);
    }

    /// <summary>絵の向く横の側を決める。柵はこの側に立つ</summary>
    private static void Face(float side)
    {
        FaceBody(Player(), side);
    }

    /// <summary>攻撃の向きを決める。攻撃の選び分けが向きとして見る部品を裏返す</summary>
    private static void FaceBody(Component player, float facing)
    {
        Component body = player.GetComponentInChildren(GameType("SideJab"));
        Assert.IsNotNull(body, "SideJab が無い");

        body.transform.localScale = new Vector3(facing, 1f, 1f);
    }

    private static Component Player()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "プレイヤーが居ない");

        return player.GetComponent(GameType("Player2"));
    }

    private static Component Inventory()
    {
        GameObject owner = GameObject.FindWithTag("PlayerInventory");
        Assert.IsNotNull(owner, "持ち物が無い");

        return owner.GetComponent(GameType("PlayerInventory"));
    }

    private static IReadOnlyList<ItemSlot> Slots()
    {
        return (IReadOnlyList<ItemSlot>)GameType("PlayerInventory").GetProperty("Slots").GetValue(Inventory());
    }

    private static int Count(string itemId)
    {
        return Slots().Where(slot => slot.ItemId == itemId).Sum(slot => slot.Stock);
    }

    private static int Load(string itemId)
    {
        return (int)Call(Inventory(), "LoadInventory", itemId, 0);
    }

    private static Component Box(string itemId)
    {
        int index = Slots().ToList().FindIndex(slot => slot.ItemId == itemId);
        Assert.GreaterOrEqual(index, 0, itemId + " が持ち物に無い");

        //スロットの箱。持ち物の直下に、箱より前の物が CHILDPLUS 個並ぶ
        Component inventory = Inventory();
        int offset = (int)Field(inventory, "CHILDPLUS").GetValue(inventory);

        return inventory.transform.GetChild(offset + index).GetComponent(GameType("Inventbutton"));
    }

    private static void Press(Component box)
    {
        Call(box, "Show_Info");
    }

    private static void Use()
    {
        Component info = Inventory().transform.Find("Info_set").GetComponent(GameType("Info_set"));
        Call(info, "Install");
    }

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");

        return travel;
    }

    /// <summary>始めて、拠点の左の何も置いていない所に立つ。決まった地点のまわりにはテントや岩があり、柵や敵が引っかかる</summary>
    private static IEnumerator Boot()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();

        Component player = Player();
        player.GetComponent<Rigidbody>().position = OpenSpot;
        player.transform.position = OpenSpot;
        yield return new WaitForFixedUpdate();
        yield return null;
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
