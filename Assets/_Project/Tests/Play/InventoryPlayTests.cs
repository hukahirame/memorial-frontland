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

//読み込んだ持ち物が、枠の絵と首飾りの効き目にも出ること。枠を押す代わりに Inventbutton.Show_Info を直に呼ぶ
public class InventoryPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>浮動小数の比較の許容</summary>
    private const float Tolerance = 1e-4f;

    [UnityTest]
    public IEnumerator 読み込んだ持ち物は枠の絵と首飾りの速さにも出る()
    {
        yield return Boot();
        float speed = Speed();
        List<ItemSlot> saved = new List<ItemSlot>
        {
            new ItemSlot { ItemId = "Slimejelly", Stock = 3, MaxStock = 100 },
            new ItemSlot { ItemId = "Speedneckless", Stock = 1, MaxStock = 1 },
        };

        Call(Inventory(), "ReplaceSlots", saved);

        Assert.AreEqual(speed + 0.3f, Speed(), Tolerance, "読み込んだ首飾りで速くならない");
        Assert.AreEqual("Slimejelly", BoxSprite(0));
        Assert.AreEqual("3", BoxText(0));
        Assert.AreEqual("Speedneckless", BoxSprite(1));
        Assert.AreEqual("", BoxText(2));

        //起動時の鉄の剣が入っていた枠。描き直さないと、押したときに持っていない剣が出る
        Press(BoxAt(1));
        Component info = Info();
        Assert.AreEqual("Speedneckless", ((ItemDefinition)Field(info, "current").GetValue(info)).ItemId);
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

        Health hp = Hp();
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    private static Component Inventory()
    {
        GameObject owner = GameObject.FindWithTag("PlayerInventory");
        Assert.IsNotNull(owner, "持ち物が無い");

        return owner.GetComponent(GameType("PlayerInventory"));
    }

    //スロットの箱。持ち物の直下に、箱より前の物が CHILDPLUS 個並ぶ
    private static Component BoxAt(int index)
    {
        Component inventory = Inventory();
        int offset = (int)Field(inventory, "CHILDPLUS").GetValue(inventory);

        return inventory.transform.GetChild(offset + index).GetComponent(GameType("Inventbutton"));
    }

    //UnityEngine.UI は asmdef から参照していないので、Image と Text は名前で引く
    private static string BoxSprite(int index)
    {
        Component image = BoxAt(index).GetComponent("Image");
        return ((Sprite)image.GetType().GetProperty("sprite").GetValue(image)).name;
    }

    private static string BoxText(int index)
    {
        Component text = BoxAt(index).transform.Find("Text").GetComponent("Text");
        return (string)text.GetType().GetProperty("text").GetValue(text);
    }

    private static void Press(Component box)
    {
        Call(box, "Show_Info");
    }

    private static Component Info()
    {
        return Inventory().transform.Find("Info_set").GetComponent(GameType("Info_set"));
    }

    private static Health Hp()
    {
        return (Health)StaticField("Player2", "Hp").GetValue(null);
    }

    private static float Speed()
    {
        return (float)StaticField("Player2", "speed").GetValue(null);
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
