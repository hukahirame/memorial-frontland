using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//攻撃が実際の場で目の前の敵に届くことだけを見る。届かなくなると遊べないので残す。手触りの良し悪しは見ない。
//振りは物理で当てる
public class CombatPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）</summary>
    private const float TravelSeconds = 10f;

    /// <summary>フレームの刻みで遅れてよい幅（秒）</summary>
    private const float Slack = 0.15f;

    /// <summary>突きが届く距離。触れずに済む近さ（体どうしが触れるのは約 0.5）で、振りの先が確実に重なるところ</summary>
    private const float ReachDistance = 0.7f;

    /// <summary>敵が寄ってこない距離。プレイヤーを見つけるのは 4 以内</summary>
    private const float FarDistance = 6f;

    [UnityTest]
    public IEnumerator 攻撃スティックを倒して離すと目の前の敵に突きが届いて当たる()
    {
        yield return Boot();
        Component player = Player();
        Face(player, 1f);

        GameObject slime = SpawnSlime(player, Vector3.right * FarDistance, "Slime");
        yield return Hold(slime);
        Place(slime, player.transform.position + Vector3.right * ReachDistance);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

        IDamageable enemy = slime.GetComponent<IDamageable>();
        int before = enemy.CurrentHp;

        ReleaseAttackStick(0.9f, 0f);
        yield return new WaitForSeconds(AttackTiming.ThrustSeconds + Slack);

        Assert.Less(enemy.CurrentHp, before, "突きが届かない");
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;

        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
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

        Health hp = PlayerHp();
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        yield return null;
    }

    private static Component Player()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "プレイヤーが居ない");

        Component component = player.GetComponent(GameType("Player2"));
        Assert.IsNotNull(component, "プレイヤーに Player2 が無い");

        return component;
    }

    private static Health PlayerHp()
    {
        return (Health)StaticField("Player2", "Hp").GetValue(null);
    }

    /// <summary>向きを決める。攻撃の選び分けが向きとして見る部品を裏返す</summary>
    private static void Face(Component player, float facing)
    {
        Component body = player.GetComponentInChildren(GameType("SideJab"));
        Assert.IsNotNull(body, "SideJab が無い");

        body.transform.localScale = new Vector3(facing, 1f, 1f);
    }

    //攻撃スティックを倒して離す。指を離したときの入口をそのまま通す
    private static void ReleaseAttackStick(float horizontal, float vertical)
    {
        Type joystick = GameType("Joystick");
        Component stick = UnityEngine.Object.FindObjectsByType(joystick, FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(found => found.name.IndexOf("Attack") != -1);
        Assert.IsNotNull(stick, "攻撃スティックが無い");

        joystick.GetField("input", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(stick, new Vector2(horizontal, vertical));
        joystick.GetMethod("OnPointerUp").Invoke(stick, new object[] { null });
    }

    private static GameObject SpawnSlime(Component player, Vector3 offset, string kind)
    {
        GameObject prefab = Resources.Load<GameObject>(kind);
        Assert.IsNotNull(prefab, kind + " の prefab が無い");

        return UnityEngine.Object.Instantiate(prefab, player.transform.position + offset, Quaternion.identity);
    }

    //自分では動かないようにして、着地するまで待つ。最初の行動は Start で決まるので、その後で止める
    private static IEnumerator Hold(GameObject slime)
    {
        yield return null;

        MonoBehaviour self = (MonoBehaviour)slime.GetComponent(GameType("Slime"));
        self.CancelInvoke();
        GameType("Slime").GetField("speed").SetValue(self, 0f);

        yield return new WaitForSeconds(1f);
    }

    private static void Place(GameObject slime, Vector3 at)
    {
        Rigidbody body = slime.GetComponent<Rigidbody>();
        Vector3 to = new Vector3(at.x, body.position.y, at.z);

        body.linearVelocity = Vector3.zero;
        body.position = to;
        slime.transform.position = to;
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

    //Assembly-CSharp は asmdef から参照できないため名前で引く
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
}
