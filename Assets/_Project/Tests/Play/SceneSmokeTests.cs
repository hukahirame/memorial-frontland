using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;

//シーンが起動し、遷移しても例外を出さないことだけを見る。
//ゲームの正しさは見ない。参照割り当ての抜けを捕まえるための網。
public class SceneSmokeTests
{
    private const int SettleFrames = 120; //Start とコルーチンが一巡するまで

    [UnityTest]
    public IEnumerator MainSiteは例外を出さずに起動する()
    {
        yield return LoadAndSettle("MainSite");
        Assert.AreEqual("MainSite", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator MainSiteからRoot1へ遷移できる()
    {
        yield return LoadAndSettle("MainSite");

        SceneTrans("Root1");
        yield return Settle();

        Assert.AreEqual("Root1", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator MainSiteからRoot2へ遷移できる()
    {
        yield return LoadAndSettle("MainSite");

        SceneTrans("Root2");
        yield return Settle();

        Assert.AreEqual("Root2", SceneManager.GetActiveScene().name);
    }

    //Play を押し直した直後の状態に戻す。
    //PlayMode テストはテスト間でドメインを再読み込みしないため、
    //オブジェクトを消すだけでは static が前のテストの結果を持ち越す
    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        //LoadScene(Single) は DontDestroyOnLoad を消さない
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        //MainSite の Player は非アクティブで保存されており、GameManager.Awake が
        //P_singleton == false のときだけ起こす。オブジェクトを消しても
        //フラグが true のままだと、次のテストで Player が存在しない（D-006 の実例）
        SetStatic("GameManager", "GM_singleton", false);
        SetStatic("GameManager", "P_singleton", false);
        SetStatic("GameManager", "entered_scene", "MainSite");

        //Start のたびに Add され、Clear されない静的リスト
        ClearStaticList("GameManager", "items");

        //根源は Domain 側。RootsManager だけ Assembly-CSharp なので名前で引く
        var roots = StaticField("RootsManager", "Roots").GetValue(null) as RootRegistry;
        Assert.IsNotNull(roots, "RootsManager.Roots が RootRegistry ではない");
        roots.Clear();

        //クエストも同じ。QuestManager.Start が毎回2本足すので、消さないと溜まる
        var quests = StaticField("QuestManager", "Quests").GetValue(null) as QuestRegistry;
        Assert.IsNotNull(quests, "QuestManager.Quests が QuestRegistry ではない");
        quests.Clear();
        SetStatic("QuestManager", "ordered_id", "");

        yield return null;
    }

    private static IEnumerator LoadAndSettle(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return Settle();
    }

    private static IEnumerator Settle()
    {
        for (int i = 0; i < SettleFrames; i++) yield return null;
    }

    //GameManager.SceneTrans を通す。生の LoadScene では entered_scene が更新されず、
    //SceneStarter が実際のゲームでは起きない壊れ方をする
    private static void SceneTrans(string target)
    {
        var method = GameType("GameManager")
            .GetMethod("SceneTrans", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(method, "GameManager.SceneTrans が見つからない");

        method.Invoke(null, new object[] { target });
    }

    //Assembly-CSharp は asmdef から参照できない（参照は一方向）ため名前で引く。
    //改名するとコンパイルではなくここで落ちる
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

    private static void SetStatic(string typeName, string fieldName, object value)
    {
        StaticField(typeName, fieldName).SetValue(null, value);
    }

    private static void ClearStaticList(string typeName, string fieldName)
    {
        var list = StaticField(typeName, fieldName).GetValue(null) as IList;
        Assert.IsNotNull(list, typeName + "." + fieldName + " がリストではない");

        list.Clear();
    }
}
