using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//UI の見た目を確かめるために、画面を順に開いて撮る。確かめる値は無いので、名指しで回したときだけ走る。
//撮った絵はプロジェクト直下の Temp/UiShots に置く。ボタンを押す代わりに、押したときの呼び先を直に呼ぶ
[Explicit]
public class UiShots
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）。暗転と明転を含む</summary>
    private const float TravelSeconds = 10f;

    /// <summary>文字が濃くなりきるまでの待ち（実時間の秒）。大きい文字も中くらいの文字も、出始めから約 1 秒かかる</summary>
    private const float TextSeconds = 1.2f;

    /// <summary>持ち物と地図の入れ替えの途中を撮るまでの待ち（実時間の秒）。入れ替えは 0.3 秒で終わる</summary>
    private const float SlideSeconds = 0.1f;

    /// <summary>入れ替えが終わるまでの待ち（実時間の秒）</summary>
    private const float SlideEndSeconds = 0.5f;

    /// <summary>暗転の途中を撮るまでの待ち（実時間の秒）</summary>
    private const float VeilSeconds = 0.3f;

    /// <summary>開いた画面が浮かび上がりきるまでの待ち（実時間の秒）。浮かび上がりは 0.12 秒</summary>
    private const float OpenSeconds = 0.3f;

    /// <summary>タイトルが浮かび上がりきるまでの待ち（実時間の秒）。浮かび上がりは 1.2 秒</summary>
    private const float TitleSeconds = 1.5f;

    private static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "UiShots"));

    [UnityTest]
    public IEnumerator 画面を順に開いて撮る()
    {
        Directory.CreateDirectory(Folder);

        yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(TitleSeconds);
        yield return Shot("00_title");

        yield return Boot();
        yield return Shot("01_base");

        //攻撃の案内。構えているあいだは構え歩きが赤く、離すと出る攻撃が金色に、振っているあいだはその攻撃が赤くなる
        Component player = GameObject.FindWithTag("Player").GetComponent(GameType("Player2"));
        Call(player, "AttackDown");
        yield return Frames(2);
        yield return Shot("01a_attack_aim");

        ((MonoBehaviour)player).StartCoroutine((IEnumerator)Call(player, "Attack"));
        yield return Frames(2);
        yield return Shot("01b_attack_swing");
        yield return new WaitForSeconds(AttackTiming.SwingSeconds + 0.1f);

        //重なる品と耐久のある品を混ぜて入れる。枠は 15 で、初めから鉄の剣が 1 つ使っている
        Supply("Slimejelly", 12);
        Supply("Branch", 5);
        Supply("Slimecore", 2);
        Supply("Torch", 1, 100);
        Call(Opener(Inventory().transform), "OpenClick");
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("02_inventory");

        Call(BoxOf("Slimejelly"), "Show_Info");
        yield return Frames(2);
        yield return Shot("03_item_info");

        Call(Exchange(true), "Exchange_UI");
        yield return new WaitForSecondsRealtime(SlideSeconds);
        yield return Shot("04_exchange");
        yield return new WaitForSecondsRealtime(SlideEndSeconds);
        yield return Shot("05_map");

        Transform map = GameObject.FindWithTag("RootsManager").transform;
        Call(map.Find("Root1").GetComponent(GameType("RootUI")), "Show_QM");
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("06_root_detail");

        GameObject.FindWithTag("QuestManager").transform.Find("CloseButton").GetComponent<CloseManager>().CloseClick();
        map.Find("CloseButton").GetComponent<CloseManager>().CloseClick();

        //作業台に触れると出るボタンと、それで開く交換画面
        Component allmaity = (Component)UnityEngine.Object.FindAnyObjectByType(GameType("Allmaity"));
        Call(allmaity, "Prepare_Button", "CraftInventory");
        yield return Frames(2);
        yield return Shot("07_bench_button");

        Call(allmaity, "Allmaity_Play", allmaity.transform.Find("CraftInventory").GetSiblingIndex());
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("08_craft");

        Component craft = (Component)UnityEngine.Object.FindAnyObjectByType(GameType("Craft"));
        Call(craft.transform.Find("Torch").GetComponent(GameType("CraftButton")), "CraftPrepare1");
        yield return Frames(2);
        yield return Shot("09_craft_selected");

        Call(Opener(craft.transform), "CloseClick");
        Call(allmaity, "Trash_Button", "CraftInventory");

        UnityEngine.Object.FindAnyObjectByType<MiddleText>().Midtxt_Anim("新クエストが発生しました");
        yield return new WaitForSecondsRealtime(TextSeconds);
        yield return Shot("10_middle_text");

        UnityEngine.Object.FindAnyObjectByType<BigText>().Bigtxt_Anim("体力が0になりました", "失敗");
        yield return new WaitForSecondsRealtime(TextSeconds);
        yield return Shot("11_big_text");

        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return new WaitForSecondsRealtime(VeilSeconds);
        yield return Shot("12_veil");
        yield return Arrive();
        yield return Shot("13_root");
    }

    //テストでは保存を読まないので、枠はどれも空きで、始めるボタンは保存の無いときの「はじめる」になる
    [UnityTest]
    public IEnumerator タイトルの枠と名前の画面を撮る()
    {
        Directory.CreateDirectory(Folder);

        yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(TitleSeconds);
        Component title = (Component)UnityEngine.Object.FindAnyObjectByType(GameType("Title"));

        Call(title, "LoadFresh");
        yield return Frames(2);
        yield return Shot("00a_title_slots");

        //名前の欄の字は、Editor が描き方を組み立て終えるまで水色の四角で出ることがある。少し待ってから撮る
        Call(title, "Load");
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("00b_title_name");
    }

    //敵がプレイヤーに気づいたときの「！」。プレイヤーの横に敵を湧かせると、湧いてすぐ見つけるので、弾み終えたところを撮る
    [UnityTest]
    public IEnumerator 敵が気づいたときの印を撮る()
    {
        Directory.CreateDirectory(Folder);

        yield return Boot();
        Transform player = GameObject.FindWithTag("Player").transform;
        UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Slime"), player.position + Vector3.right * 1.5f, Quaternion.identity);

        yield return new WaitForSecondsRealtime(AlertMark.PopSeconds + 0.1f);
        yield return Shot("14_alert");
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
        StaticField("RewardUI", "rewardUI_id").SetValue(null, "");
        StaticField("Player2", "cool").SetValue(null, false);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    //描き終えたフレームの画面を撮る。上に重ねる UI も写る
    private static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(Folder, name + ".png"), shot.EncodeToPNG());
        UnityEngine.Object.Destroy(shot);
    }

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    private static Component Inventory()
    {
        GameObject owner = GameObject.FindWithTag("PlayerInventory");
        Assert.IsNotNull(owner, "持ち物が無い");

        return owner.GetComponent(GameType("PlayerInventory"));
    }

    private static void Supply(string itemId, int count, int durability = 0)
    {
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(1, (int)Call(Inventory(), "LoadInventory", itemId, durability), itemId + " が持ち物に入らない");
        }
    }

    //スロットの箱。持ち物の直下に、箱より前の物が CHILDPLUS 個並ぶ
    private static Component BoxOf(string itemId)
    {
        Component inventory = Inventory();
        IReadOnlyList<ItemSlot> slots = (IReadOnlyList<ItemSlot>)GameType("PlayerInventory").GetProperty("Slots").GetValue(inventory);
        int index = slots.ToList().FindIndex(slot => slot.ItemId == itemId);
        Assert.GreaterOrEqual(index, 0, itemId + " が持ち物に無い");

        int offset = (int)Field(inventory, "CHILDPLUS").GetValue(inventory);
        return inventory.transform.GetChild(offset + index).GetComponent(GameType("Inventbutton"));
    }

    //画面の閉じるボタン。開く呼び先も持っている
    private static Component Opener(Transform panel)
    {
        Transform close = panel.Find("CloseButton");
        Assert.IsNotNull(close, panel.name + " に閉じるボタンが無い");

        return close.GetComponent(GameType("CloseInventory"));
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

        yield return Frames(SettleFrames);
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
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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
