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

//持ち物を使う、拾う、作る。枠を押す代わりに Inventbutton.Show_Info を、使い道のボタンの代わりに Info_set.Install を直に呼ぶ
public class InventoryPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>浮動小数の比較の許容</summary>
    private const float Tolerance = 1e-4f;

    [UnityTest]
    public IEnumerator 最後の1個を食べたあと空いた枠を押しても食べられない()
    {
        yield return Boot();
        Health hp = Hp();
        hp.SetCurrent(hp.Max / 2);
        Load("Slimejelly");
        Component box = Box("Slimejelly");

        Press(box);
        Use();
        Assert.AreEqual(0, Count("Slimejelly"));
        int healed = hp.Current;
        Assert.Greater(healed, hp.Max / 2, "持っているゼリーを食べても体力が戻らない");

        Press(box);
        Use();
        Assert.AreEqual(healed, hp.Current, "持っていないゼリーを食べられる");
        Assert.IsFalse(InfoButton().activeSelf, "空いた枠を押すと、前に入っていたアイテムの使い道が出る");
    }

    [UnityTest]
    public IEnumerator 持ち物が埋まっていると首飾りは入らず速さも変わらない()
    {
        yield return Boot();
        //初めから枝と鉄の剣が入っている。残りの 3 枠を別々のアイテムで埋める
        Load("Slimejelly");
        Load("Slimecore");
        Load("Torch", 100);
        float speed = Speed();

        Assert.AreEqual(-1, Load("Speedneckless"));
        Assert.AreEqual(-1, Load("Speedneckless"));
        Assert.AreEqual(speed, Speed(), Tolerance, "入らなかった首飾りで速くなる");

        Unload("Slimecore");
        Assert.AreEqual(1, Load("Speedneckless"));
        Assert.AreEqual(speed + 0.3f, Speed(), Tolerance);

        Unload("Speedneckless");
        Unload("Speedneckless");
        Assert.AreEqual(speed, Speed(), Tolerance, "持っていない首飾りを外すと遅くなる");
    }

    [UnityTest]
    public IEnumerator 倒れている間は食べてもゼリーは減らない()
    {
        yield return Boot();
        Load("Slimejelly");
        Load("Slimejelly");
        Press(Box("Slimejelly"));

        Hurt(Hp().Current);
        Assert.IsTrue(Hp().IsDead, "倒れない");

        Use();
        Assert.AreEqual(2, Count("Slimejelly"), "倒れている間に食べると、体力は戻らずに 1 個減る");
    }

    [UnityTest]
    public IEnumerator 場所を移っている間は松明を置いても減らない()
    {
        yield return Boot();
        Load("Torch", 100);
        Press(Box("Torch"));

        Travel().Revive();
        Assert.IsTrue(Travel().IsTraveling);
        Use();
        Assert.AreEqual(1, Count("Torch"), "移る途中に置くと、置いた松明は前の場所と一緒に消えて持ち物だけ減る");

        yield return Arrive();
        Use();
        Assert.AreEqual(0, Count("Torch"), "着いたあとは置ける");
    }

    [UnityTest]
    public IEnumerator 作業台の無い場所で作っても材料は減らない()
    {
        yield return Boot();
        Component craft = Craft();
        Call(craft, "Craftprepare2", "Torch");
        int branches = Count("Branch");

        Call(craft, "CraftStart");
        Assert.Less(Count("Branch"), branches, "拠点の作業台で作れない");
        branches = Count("Branch");

        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();

        Call(craft, "CraftStart");
        Assert.AreEqual(branches, Count("Branch"), "作業台の無い根源で押すと、材料だけ消える");
    }

    [UnityTest]
    public IEnumerator スロット数を超える箱は見えない()
    {
        yield return Boot();
        Component inventory = Inventory();
        int slotCount = (int)Field(inventory, "slotCount").GetValue(inventory);

        Assert.AreEqual(slotCount, inventory.GetComponentsInChildren(GameType("Inventbutton")).Length,
            "使わない箱が見えて、空きがあるのに拾えないように見える");
    }

    [UnityTest]
    public IEnumerator 始めは持ち物の鉄の剣を装備していて手の絵もそのまま()
    {
        yield return Boot();
        Equipment equipped = (Equipment)StaticField("Weapon", "Equipped").GetValue(null);

        Assert.AreEqual("Ironsword", equipped.ItemId, "手には鉄の剣が見えるのに、中身は素手");
        Assert.AreEqual(1, Count("Ironsword"));

        Sprite inHand = GameObject.Find("WeaponChild").GetComponent<SpriteRenderer>().sprite;
        Assert.AreEqual(Call(Info(), "InHand", "Ironsword"), inHand, "持ち物から装備し直すと手の絵が変わる");
    }

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

    [UnityTest]
    public IEnumerator 持ち物が空くと触れたままの落とし物を拾う()
    {
        yield return Boot();
        //初めから枝と鉄の剣が入っている。残りの 3 枠を別々のアイテムで埋める
        Load("Slimejelly");
        Load("Slimecore");
        Load("Torch", 100);

        GameObject drop = Drop("Legendsword", GameObject.FindWithTag("Player").transform.position);
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.IsTrue(drop != null, "埋まっているのに拾った");

        Unload("Slimecore");
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.AreEqual(1, Count("Legendsword"), "空いても、触れたままの落とし物を拾わない。離れて入り直すまで拾えない");
    }

    [UnityTest]
    public IEnumerator 交換画面の所持数は持ち物が変わるたびに書き直す()
    {
        yield return Boot();
        Component craft = Craft();
        Call(craft, "Craftprepare2", "Torch");
        Assert.AreEqual(Count("Branch").ToString(), Supply(craft));

        Load("Branch");
        Assert.AreEqual(Count("Branch").ToString(), Supply(craft), "拾った枝が所持数に出ない。品目を押し直すまで古いまま");

        Unload("Branch");
        Assert.AreEqual(Count("Branch").ToString(), Supply(craft), "使った枝が所持数に出ない");
    }

    [UnityTest]
    public IEnumerator 場所を移っている間は作っても材料は減らない()
    {
        yield return Boot();
        Component craft = Craft();
        Call(craft, "Craftprepare2", "Torch");
        int branches = Count("Branch");

        Travel().Revive();
        Assert.IsTrue(Travel().IsTraveling);
        Call(craft, "CraftStart");
        Assert.AreEqual(branches, Count("Branch"), "移る途中に作ると、品は前の場所と一緒に消えて材料だけ減る");

        yield return Arrive();
        Call(craft, "CraftStart");
        Assert.Less(Count("Branch"), branches, "着いたあとは作れる");
    }

    [UnityTest]
    public IEnumerator 入れ替えの途中にもう一方の交換を押しても画面は消えない()
    {
        yield return Boot();
        Component toMap = Exchange(true);
        Component toInventory = Exchange(false);
        Transform map = (Transform)Field(toMap, "rm").GetValue(toMap);
        Transform center = (Transform)Field(toMap, "center").GetValue(toMap);

        Call(toMap, "Exchange_UI");
        yield return null;
        Call(toInventory, "Exchange_UI");
        for (int i = 0; i < 120; i++) yield return null;

        Assert.AreEqual(center.position.x, map.position.x, 1f, "入れ替えが重なり、持ち物も地図も画面の外へ送られる");
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

    private static IReadOnlyList<ItemSlot> Slots()
    {
        return (IReadOnlyList<ItemSlot>)GameType("PlayerInventory").GetProperty("Slots").GetValue(Inventory());
    }

    private static int Count(string itemId)
    {
        return Slots().Where(slot => slot.ItemId == itemId).Sum(slot => slot.Stock);
    }

    private static int Load(string itemId, int durability = 0)
    {
        return (int)Call(Inventory(), "LoadInventory", itemId, durability);
    }

    private static void Unload(string itemId)
    {
        Call(Inventory(), "UnloadInventory", itemId);
    }

    private static Component Box(string itemId)
    {
        int index = Slots().ToList().FindIndex(slot => slot.ItemId == itemId);
        Assert.GreaterOrEqual(index, 0, itemId + " が持ち物に無い");

        return BoxAt(index);
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

    private static void Use()
    {
        Call(Info(), "Install");
    }

    private static GameObject InfoButton()
    {
        Component info = Info();
        return ((Component)Field(info, "button").GetValue(info)).gameObject;
    }

    private static Component Craft()
    {
        UnityEngine.Object craft = UnityEngine.Object.FindAnyObjectByType(GameType("Craft"));
        Assert.IsNotNull(craft, "交換画面が無い");

        return (Component)craft;
    }

    //先頭の材料の所持数。松明の材料は枝だけ
    private static string Supply(Component craft)
    {
        Component text = (Component)Field(craft, "supply1").GetValue(craft);
        return (string)text.GetType().GetProperty("text").GetValue(text);
    }

    //拾う品はスプライトの名前で決まる
    private static GameObject Drop(string itemId, Vector3 at)
    {
        GameObject prefab = Resources.Load<GameObject>("Dropitem");
        Assert.IsNotNull(prefab, "落とし物の prefab が無い");

        GameObject drop = UnityEngine.Object.Instantiate(prefab, at, Quaternion.identity);
        drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(itemId);
        return drop;
    }

    //持ち物の側の「交換」は地図へ、地図の側の「交換」は持ち物へ入れ替える
    private static Component Exchange(bool fromInventory)
    {
        Component found = UnityEngine.Object.FindObjectsByType(GameType("ExchangeButton"), FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(button => (bool)Field(button, "is_pi").GetValue(button) == fromInventory);
        Assert.IsNotNull(found, "交換ボタンが無い");

        return found;
    }

    private static void Hurt(int damage)
    {
        Component player = GameObject.FindWithTag("Player").GetComponent(GameType("Player2"));
        Call(player, "Player_Hurt", damage, Vector3.right);
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
