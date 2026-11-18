using System.Collections;
using System.Linq;
using System.Reflection;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

//タイトルから遊び始める流れ。タイトルは常駐シーンを読み込んだ直後に、選んだ枠と名前を保存の部品へ渡す
public class TitlePlayTests
{
    /// <summary>タイトルが浮かび上がりきるまでの待ち（実時間の秒）。浮かび上がりは 1.2 秒</summary>
    private const float AppearSeconds = 1.5f;

    /// <summary>拠点に着いて動けるようになるのを待つ上限（実時間の秒）</summary>
    private const float ArriveSeconds = 20f;

    private const string NewName = "テスト";

    [UnityTest]
    public IEnumerator 空いた枠に名前を付けて始めると拠点に着いて動ける()
    {
        int slot = Enumerable.Range(1, SaveSlots.SlotCount).FirstOrDefault(s => !SaveSlots.HasSave(s));
        if (slot == 0) Assert.Ignore("空いた保存の枠が無い");

        yield return SceneManager.LoadSceneAsync(AreaTravel.TitleScene, LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(AppearSeconds);

        //最初の画面はキーの押下で一覧を開く。キーは打てないので、一覧を開いて枠を押すところから呼ぶ
        Title title = Object.FindAnyObjectByType<Title>();
        Call(title, "ShowPicker");
        yield return null;
        Call(title, "PressSlot", slot);
        yield return null;
        yield return null;

        TMP_InputField field = Object.FindAnyObjectByType<TMP_InputField>();
        Assert.IsNotNull(field, "名前の画面にならない");
        field.text = NewName;

        Button start = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "はじめる");
        Assert.IsNotNull(start, "はじめるのボタンが無い");
        ExecuteEvents.Execute(start.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        AreaTravel travel = null;
        float giveUp = Time.realtimeSinceStartup + ArriveSeconds;
        while (Time.realtimeSinceStartup < giveUp)
        {
            if (travel == null) travel = Object.FindAnyObjectByType<AreaTravel>();
            if (travel != null && travel.Current != null && !travel.IsTraveling) break;
            yield return null;
        }

        Scene core = SceneManager.GetSceneByName(AreaTravel.CoreScene);
        Assert.IsTrue(core.isLoaded, "常駐シーンを読み込まない");
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");
        Assert.IsNotNull(travel.Current, "場所に着かない");
        Assert.IsFalse(travel.IsTraveling, "移動が終わらない");
        Assert.AreEqual(TravelRule.BaseAreaId, travel.Current.Id, "はじめからは拠点から始まる");

        SaveSystem save = SaveSystem.In(core);
        Assert.IsNotNull(save, "常駐シーンに保存の部品が無い");
        Assert.AreEqual(NewName, save.Data.saveName, "タイトルで決めた名前が保存へ渡らない");

        Assert.Greater(Object.FindAnyObjectByType<QuestManager>().Quests.Count, 0, "始めのクエストが出ない");
        Assert.IsTrue(GameObject.FindWithTag("Player").GetComponent<PlayerMovement>().CanAct, "着いても動けない");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトを捨てる()
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") Object.Destroy(go);
        }

        yield return null;
    }

    private static void Call(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(info, method + " が無い");
        info.Invoke(target, args);
    }
}
