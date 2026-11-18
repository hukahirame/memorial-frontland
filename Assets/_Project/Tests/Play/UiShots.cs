using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        PlayerAttack player = GameObject.FindWithTag("Player").GetComponent<PlayerAttack>();
        player.Press();
        yield return Frames(2);
        yield return Shot("01a_attack_aim");

        player.Release(Vector2.zero);
        yield return Frames(2);
        yield return Shot("01b_attack_swing");
        yield return new WaitForSeconds(AttackTiming.SwingSeconds + 0.1f);

        //重なる品と耐久のある品を混ぜて入れる。枠は 15 で、初めから鉄の剣が 1 つ使っている
        Supply("Slimejelly", 12);
        Supply("Branch", 5);
        Supply("Slimecore", 2);
        Supply("Torch", 1, 100);
        Opener(Inventory().transform).Open();
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("02_inventory");

        BoxOf("Slimejelly").Select();
        yield return Frames(2);
        yield return Shot("03_item_info");

        Exchange(true).Exchange();
        yield return new WaitForSecondsRealtime(SlideSeconds);
        yield return Shot("04_exchange");
        yield return new WaitForSecondsRealtime(SlideEndSeconds);
        yield return Shot("05_map");

        Transform map = GameObject.FindWithTag("RootsManager").transform;
        map.Find("Root1").GetComponent<RootCard>().Open();
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("06_root_detail");

        GameObject.FindWithTag("QuestManager").transform.Find("CloseButton").GetComponent<CloseManager>().Close();
        map.Find("CloseButton").GetComponent<CloseManager>().Close();

        //作業台に触れると出るボタンと、それで開く交換画面
        Allmaity allmaity = UnityEngine.Object.FindAnyObjectByType<Allmaity>();
        allmaity.ShowOpener("CraftInventory", "作業台を使う");
        yield return Frames(2);
        yield return Shot("07_bench_button");

        allmaity.Press(allmaity.transform.Find("CraftInventory").GetSiblingIndex());
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("08_craft");

        CraftMenu menu = UnityEngine.Object.FindAnyObjectByType<CraftMenu>();
        menu.transform.Find("Torch").GetComponent<CraftButton>().Select();
        yield return Frames(2);
        yield return Shot("09_craft_selected");

        Opener(menu.transform).Close();
        allmaity.Hide("CraftInventory");

        UnityEngine.Object.FindAnyObjectByType<MiddleText>().Show("新クエストが発生しました");
        yield return new WaitForSecondsRealtime(TextSeconds);
        yield return Shot("10_middle_text");

        UnityEngine.Object.FindAnyObjectByType<BigText>().Show("体力が0になりました", "失敗");
        yield return new WaitForSecondsRealtime(TextSeconds);
        yield return Shot("11_big_text");

        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return new WaitForSecondsRealtime(VeilSeconds);
        yield return Shot("12_veil");
        yield return Arrive();
        yield return Shot("13_root");
    }

    //テストでは保存を読まないので、枠はどれも空き。空いた枠を押すと名前の画面になる
    [UnityTest]
    public IEnumerator タイトルの枠と名前の画面を撮る()
    {
        Directory.CreateDirectory(Folder);

        yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(TitleSeconds);
        Title title = UnityEngine.Object.FindAnyObjectByType<Title>();

        Call(title, "ShowPicker");
        yield return Frames(2);
        yield return Shot("00a_title_slots");

        //名前の欄の字は、Editor が描き方を組み立て終えるまで水色の四角で出ることがある。少し待ってから撮る
        Call(title, "PressSlot", 1);
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
        UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("Slime").WaitForCompletion(), player.position + Vector3.right * 1.5f, Quaternion.identity);

        yield return new WaitForSecondsRealtime(AlertMark.PopSeconds + 0.1f);
        yield return Shot("14_alert");
    }

    //タイトルの背景の動画が流れているか。時間をずらした 2 枚で、滝や水面が動いていることを見る
    [UnityTest]
    public IEnumerator タイトルの背景の動画を撮る()
    {
        Directory.CreateDirectory(Folder);

        yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(2.5f);
        yield return Shot("00c_title_movie_a");

        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("00d_title_movie_b");
    }

    //回避攻撃の途中。残像は回っているあいだに等しい間隔で 3 枚、正面を向いた姿で残る。2 枚めと 3 枚めが重なる頃と、回り終わる頃を撮る
    [UnityTest]
    public IEnumerator 回避攻撃の残像を撮る()
    {
        Directory.CreateDirectory(Folder);
        yield return Boot();

        PlayerAttack player = GameObject.FindWithTag("Player").GetComponent<PlayerAttack>();
        player.StartCoroutine((IEnumerator)Call(player, "Roll", 1f, 1f));

        yield return new WaitForSeconds(0.45f);
        yield return Shot("17_roll_middle");

        yield return new WaitForSeconds(0.15f);
        yield return Shot("18_roll_end");
    }

    //木の柵の耐久のゲージ。満タン、削られた直後、減りきったあと、倒れて戻りかけのとき。
    //拠点の左手前の、何も置いていない平らな所に立ち、右を向いて立てたときの所（体から 1 マス先の線）に柵を置く
    [UnityTest]
    public IEnumerator 柵のゲージを撮る()
    {
        Directory.CreateDirectory(Folder);
        yield return Boot();

        Vector3 open = new Vector3(-6f, 0.65f, -4f);
        GameObject player = GameObject.FindWithTag("Player");
        player.GetComponent<Rigidbody>().position = open;
        player.transform.position = open;

        GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Fence_obj").WaitForCompletion();
        Fence fence = UnityEngine.Object.Instantiate(prefab, new Vector3(-5.25f, 0f, -3.75f), Quaternion.Euler(0f, 90f, 0f)).GetComponent<Fence>();
        yield return new WaitForSeconds(1f);
        yield return Shot("19_fence_full");

        fence.Ram(80, open);
        yield return Frames(2);
        yield return Shot("20_fence_hit");

        yield return new WaitForSeconds(1f);
        yield return Shot("21_fence_worn");

        //倒れたあと戻るのは 3 秒後から 1 秒に 1 ずつなので、戻りかけた耐久を直に入れる
        fence.Ram(fence.Durability, open);
        fence.Restore(40, true, 0f);
        yield return new WaitForSeconds(1f);
        yield return Shot("22_fence_fallen");
    }

    //設定の歯車（持ち物のボタンの真下）と、開いた窓
    [UnityTest]
    public IEnumerator 設定の窓を撮る()
    {
        Directory.CreateDirectory(Folder);
        yield return Boot();
        yield return Shot("15_settings_button");

        UnityEngine.Object.FindAnyObjectByType<SettingsMenu>().Open();
        yield return new WaitForSecondsRealtime(OpenSeconds);
        yield return Shot("16_settings_open");
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

    private static PlayerInventory Inventory()
    {
        GameObject owner = GameObject.FindWithTag("PlayerInventory");
        Assert.IsNotNull(owner, "持ち物が無い");

        return owner.GetComponent<PlayerInventory>();
    }

    private static void Supply(string itemId, int count, int durability = 0)
    {
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(1, Inventory().LoadInventory(itemId, durability), itemId + " が持ち物に入らない");
        }
    }

    //スロットの箱。InventoryView が並べた順
    private static InventoryBox BoxOf(string itemId)
    {
        PlayerInventory inventory = Inventory();
        IReadOnlyList<ItemSlot> slots = inventory.Slots;
        int index = slots.ToList().FindIndex(slot => slot.ItemId == itemId);
        Assert.GreaterOrEqual(index, 0, itemId + " が持ち物に無い");

        InventoryView view = inventory.GetComponent<InventoryView>();
        return ((System.Collections.Generic.List<InventoryBox>)Field(view, "_boxes").GetValue(view))[index];
    }

    //画面の閉じるボタン。開く呼び先も持っている
    private static PanelOpener Opener(Transform panel)
    {
        Transform close = panel.Find("CloseButton");
        Assert.IsNotNull(close, panel.name + " に閉じるボタンが無い");

        return close.GetComponent<PanelOpener>();
    }

    //持ち物の側の「交換」は地図へ、地図の側の「交換」は持ち物へ入れ替える
    private static ExchangeButton Exchange(bool fromInventory)
    {
        ExchangeButton found = UnityEngine.Object.FindObjectsByType<ExchangeButton>(FindObjectsSortMode.None)
            .FirstOrDefault(button => (bool)Field(button, "_fromInventory").GetValue(button) == fromInventory);
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
}
