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

//読み込んだ持ち物が、枠の絵と首飾りの効き目にも出ること。枠を押す代わりに InventoryBox.Select を直に呼ぶ
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

        Inventory().ReplaceSlots(saved);

        Assert.AreEqual(speed + 0.3f, Speed(), Tolerance, "読み込んだ首飾りで速くならない");
        Assert.AreEqual("Slimejelly", BoxSprite(0));
        Assert.AreEqual("3", BoxText(0));
        Assert.AreEqual("Speedneckless", BoxSprite(1));
        Assert.AreEqual("", BoxText(2));

        //起動時の鉄の剣が入っていた枠。描き直さないと、押したときに持っていない剣が出る
        Press(BoxAt(1));
        ItemInfo info = Info();
        Assert.AreEqual("Speedneckless", ((ItemDefinition)Field(info, "_current").GetValue(info)).ItemId);
    }

    [UnityTest]
    public IEnumerator 捨てた品は向いている側へ落ち一度離れて戻るまで拾わない()
    {
        yield return Boot();
        for (int i = 0; i < 3; i++) Assert.AreEqual(1, Inventory().LoadInventory("Slimejelly", 0));

        Transform player = GameObject.FindWithTag("Player").transform;
        float side = player.GetComponent<PlayerMovement>().Side;
        Vector3 from = player.position;

        //放ったまま待つ。体に押し出されず、向いている側へ落ちる
        Press(BoxOf("Slimejelly"));
        Info().Throw();
        Assert.AreEqual(2, Count("Slimejelly"), "捨てても減らない");
        Dropitem tossed = Drops("Slimejelly").SingleOrDefault();
        Assert.IsNotNull(tossed, "捨てた品が落ちていない");

        yield return new WaitForSeconds(LandSeconds);
        Assert.AreEqual(side, Mathf.Sign(tossed.transform.position.x - from.x), "向いている側へ落ちない");
        Assert.Less(Mathf.Abs(tossed.transform.position.z - from.z), DepthTolerance, "奥行きへ押し出された");
        UnityEngine.Object.Destroy(tossed.gameObject);
        yield return null;

        //拾わない時間が過ぎても、重なったまま離れないうちは拾わない
        Info().Throw();
        Dropitem drop = Drops("Slimejelly").SingleOrDefault();
        Assert.IsNotNull(drop, "2 つ目の捨てた品が落ちていない");
        yield return Hold(drop, player, DiscardHoldSeconds);
        Assert.AreEqual(1, Count("Slimejelly"), "離れないうちに拾い直した");

        //一度離れて戻れば拾う
        yield return Away(drop, player);
        yield return Hold(drop, player, ReturnSeconds);
        Assert.AreEqual(2, Count("Slimejelly"), "離れて戻っても拾わない");
    }

    [UnityTest]
    public IEnumerator 装備している武器は最後の1本だけ捨てない()
    {
        yield return Boot();
        Assert.AreEqual(1, Count("Ironsword"), "始まりの鉄の剣が 1 本でない");

        Press(BoxOf("Ironsword"));
        Info().Throw();
        yield return null;

        Assert.AreEqual(1, Count("Ironsword"), "装備している最後の剣を捨てた");
        Assert.IsEmpty(Drops("Ironsword"), "捨てていない剣が落ちている");

        //2 本あれば捨てられ、装備は残る
        Assert.AreEqual(1, Inventory().LoadInventory("Ironsword", 0));
        Info().Throw();
        yield return null;

        Assert.AreEqual(1, Count("Ironsword"), "2 本あるのに捨てられない");
        Assert.AreEqual(1, Drops("Ironsword").Count, "捨てた剣が落ちていない");
        Assert.AreEqual("Ironsword", GameObject.FindWithTag("Player").GetComponent<PlayerEquipment>().Equipped.ItemId, "捨てたら装備が外れた");
    }

    /// <summary>捨てた品を拾わない秒（ItemActions）。過ぎたかを見るので、少し足して待つ</summary>
    private const float DiscardHoldSeconds = 3.5f;

    /// <summary>放った品が落ちきるまでの待ち</summary>
    private const float LandSeconds = 1f;

    /// <summary>奥行きへのずれの許容。押し出されると拾う当たりの半径ほど飛ぶ</summary>
    private const float DepthTolerance = 0.3f;

    /// <summary>離れたあと、重ねて拾うのを待つ秒</summary>
    private const float ReturnSeconds = 0.5f;

    /// <summary>離すときの高さ。拾う当たりの外になり、壁にも当たらない</summary>
    private const float AwayHeight = 5f;

    /// <summary>離しておく物理の段の数。落とし物が離れたことに気づくまで</summary>
    private const int AwaySteps = 3;

    //落とし物をプレイヤーに重ねたまま待つ。放られて離れるので、毎段置き直す
    private static IEnumerator Hold(Dropitem drop, Transform player, float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end && drop != null)
        {
            drop.transform.position = player.position;
            yield return new WaitForFixedUpdate();
        }

        yield return null;
    }

    //落とし物をプレイヤーの真上の高い所へ離す。プレイヤーが離れたのと同じに見える
    private static IEnumerator Away(Dropitem drop, Transform player)
    {
        for (int i = 0; i < AwaySteps && drop != null; i++)
        {
            drop.transform.position = player.position + Vector3.up * AwayHeight;
            yield return new WaitForFixedUpdate();
        }
    }

    private static int Count(string itemId)
    {
        PlayerInventory inventory = Inventory();
        MemorialFloor.Domain.Inventory rule = inventory.Inventory;

        return rule.CountOf(itemId);
    }

    private static List<Dropitem> Drops(string itemId)
    {
        return UnityEngine.Object.FindObjectsByType<Dropitem>(FindObjectsSortMode.None)
            .Where(drop => drop.GetComponent<SpriteRenderer>().sprite.name == itemId)
            .ToList();
    }

    private static InventoryBox BoxOf(string itemId)
    {
        PlayerInventory inventory = Inventory();
        IReadOnlyList<ItemSlot> slots = inventory.Slots;
        int index = slots.ToList().FindIndex(slot => slot.ItemId == itemId);
        Assert.GreaterOrEqual(index, 0, itemId + " が持ち物に無い");

        return BoxAt(index);
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトを捨てる()
    {
        Time.timeScale = 1f;

        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        yield return null;
    }

    private static PlayerInventory Inventory()
    {
        GameObject owner = GameObject.FindWithTag("PlayerInventory");
        Assert.IsNotNull(owner, "持ち物が無い");

        return owner.GetComponent<PlayerInventory>();
    }

    //スロットの箱。InventoryView が並べた順
    private static InventoryBox BoxAt(int index)
    {
        InventoryView view = Inventory().GetComponent<InventoryView>();
        return ((System.Collections.Generic.List<InventoryBox>)Field(view, "_boxes").GetValue(view))[index];
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

    private static void Press(InventoryBox box)
    {
        box.Select();
    }

    private static ItemInfo Info()
    {
        return Inventory().transform.Find("Info_set").GetComponent<ItemInfo>();
    }

    private static float Speed()
    {
        return GameObject.FindWithTag("Player").GetComponent<PlayerMovement>().Speed;
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

    private static FieldInfo Field(Component target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " が見つからない");

        return field;
    }
}
