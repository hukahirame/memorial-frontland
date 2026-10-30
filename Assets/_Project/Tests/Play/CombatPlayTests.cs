using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//攻撃・撃破・被弾の仕掛けが、実際の場で例外なく働くことを見る。手触りの良し悪しは見ない。
//当たり判定の重なりは物理と振りの姿勢に左右されるため、当たったあとの処理は武器の OnTriggerEnter を直に呼んで確かめる
public class CombatPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）</summary>
    private const float TravelSeconds = 10f;

    /// <summary>フレームの刻みで遅れてよい幅（秒）</summary>
    private const float Slack = 0.15f;

    /// <summary>敵を置く距離。触れて被弾しない程度に離す。落とし物は +x へ飛ぶので、拾わないよう +x 側に置く</summary>
    private const float EnemyDistance = 1.5f;

    [UnityTest]
    public IEnumerator 攻撃の当たり判定は振りの打点のあたりでだけ有効になる()
    {
        yield return Boot();
        Component player = Player();
        BoxCollider box = WeaponBox(player);

        float started = Time.time;
        StartAttack(player);

        float firstOn = -1f;
        float lastOn = -1f;
        while (Time.time - started < AttackTiming.SwingSeconds + Slack)
        {
            if (box.enabled)
            {
                float t = Time.time - started;
                if (firstOn < 0f) firstOn = t;
                lastOn = t;
            }
            yield return null;
        }

        Assert.GreaterOrEqual(firstOn, 0f, "一度も当たり判定が出ない");
        Assert.GreaterOrEqual(firstOn, AttackTiming.WindupSeconds, "振りかぶりの間に当たり判定が出た");
        Assert.LessOrEqual(lastOn, AttackTiming.WindupSeconds + AttackTiming.ActiveSeconds + Slack, "打点を過ぎても当たり判定が残る");
    }

    [UnityTest]
    public IEnumerator 終わり際に入れた攻撃は終わってすぐ次が出る()
    {
        yield return Boot();
        Component player = Player();
        BoxCollider box = WeaponBox(player);

        float started = Time.time;
        StartAttack(player);
        yield return new WaitForSeconds(AttackTiming.SwingSeconds - AttackTiming.BufferSeconds * 0.5f);
        StartAttack(player);

        float secondOn = -1f;
        while (Time.time - started < AttackTiming.SwingSeconds * 2f + Slack)
        {
            float t = Time.time - started;
            if (box.enabled && t > AttackTiming.SwingSeconds)
            {
                secondOn = t;
                break;
            }
            yield return null;
        }

        Assert.GreaterOrEqual(secondOn, 0f, "預けた攻撃が出ない");
        Assert.LessOrEqual(secondOn, AttackTiming.SwingSeconds + AttackTiming.WindupSeconds + Slack, "終わってすぐには出ていない");
    }

    [UnityTest]
    public IEnumerator 振りの序盤に入れた攻撃は捨てられる()
    {
        yield return Boot();
        Component player = Player();
        BoxCollider box = WeaponBox(player);

        float started = Time.time;
        StartAttack(player);
        yield return null;
        StartAttack(player);

        bool again = false;
        while (Time.time - started < AttackTiming.SwingSeconds * 2f + Slack)
        {
            if (box.enabled && Time.time - started > AttackTiming.SwingSeconds) again = true;
            yield return null;
        }

        Assert.IsFalse(again, "序盤の入力で次の攻撃が出た");
    }

    [UnityTest]
    public IEnumerator 当てた敵は離れる向きに飛び白く光り時間は少し止まって戻る()
    {
        yield return Boot();
        Component player = Player();
        GameObject slime = SpawnSlimeBeside(player);
        yield return null;

        IDamageable enemy = slime.GetComponent<IDamageable>();
        int before = enemy.CurrentHp;
        SpriteRenderer sprite = slime.GetComponent<SpriteRenderer>();
        Material normal = sprite.sharedMaterial;

        Hit(player, slime);

        Assert.Less(enemy.CurrentHp, before, "体力が減らない");
        Assert.AreEqual(0f, Time.timeScale, "当てた瞬間に時間が止まらない");
        Assert.Greater(slime.GetComponent<Rigidbody>().linearVelocity.x, 0f, "プレイヤーから離れる向きに飛ばない");
        Assert.AreEqual("MemorialFloor/SpriteFlash", sprite.sharedMaterial.shader.name, "白く光らない");

        yield return new WaitForSecondsRealtime(0.3f);

        Assert.AreEqual(1f, Time.timeScale, "時間が戻らない");
        Assert.AreSame(normal, sprite.sharedMaterial, "光ったまま戻らない");
    }

    [UnityTest]
    public IEnumerator 倒した敵は弾けて消え落とし物が残る()
    {
        yield return Boot();
        Component player = Player();
        GameObject slime = SpawnSlimeBeside(player);
        yield return null;

        IDamageable enemy = slime.GetComponent<IDamageable>();
        enemy.TakeDamage(enemy.CurrentHp - 1);
        int drops = UnityEngine.Object.FindObjectsByType<Dropitem>(FindObjectsSortMode.None).Length;

        Hit(player, slime);
        Assert.AreEqual(0, enemy.CurrentHp);

        yield return new WaitForSecondsRealtime(HitStop.KillSeconds + Slack);
        yield return new WaitForSeconds(1f);

        Assert.IsTrue(slime == null, "倒した敵が消えない");
        Assert.Greater(UnityEngine.Object.FindObjectsByType<Dropitem>(FindObjectsSortMode.None).Length, drops, "落とし物が出ない");
        Assert.AreEqual(1f, Time.timeScale, "時間が戻らない");
    }

    [UnityTest]
    public IEnumerator 被弾したあと無敵のあいだは続けて当たっても体力が減らない()
    {
        yield return Boot();
        Component player = Player();
        Health hp = PlayerHp();
        hp.SetCurrent(hp.Max);
        int full = hp.Current;

        Hurt(player, 10);
        Hurt(player, 10);
        Assert.AreEqual(full - 10, hp.Current, "無敵のあいだに 2 回目が通った");

        yield return new WaitForSeconds(Invulnerability.Seconds + Slack);

        //待つ間に自然回復が入ることがあるので、直前の値から見る
        int before = hp.Current;
        Hurt(player, 10);
        Assert.AreEqual(before - 10, hp.Current, "無敵が明けても当たらない");
    }

    [UnityTest]
    public IEnumerator カメラはプレイヤーへ一度に飛ばず滑らかに追い付く()
    {
        yield return Boot();
        Component player = Player();
        MainCamera follower = player.GetComponentInChildren<MainCamera>();
        Assert.IsNotNull(follower, "カメラが無い");
        Assert.IsTrue(follower.enabled, "カメラの追従が無効のまま");

        Transform view = follower.transform;
        Vector3 offset = view.position - player.transform.position;

        player.transform.position += Vector3.right;
        yield return null;

        Assert.Greater(player.transform.position.x + offset.x - view.position.x, 0.05f, "一度に追い付いた");

        yield return new WaitForSeconds(1f);
        Assert.AreEqual(player.transform.position.x + offset.x, view.position.x, 0.05f, "追い付かない");
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

    private static BoxCollider WeaponBox(Component player)
    {
        Component weapon = player.GetComponentInChildren(GameType("Weapon"), true);
        Assert.IsNotNull(weapon, "武器が無い");

        return weapon.GetComponent<BoxCollider>();
    }

    //攻撃スティックを離したときと同じ入口。スティックは倒れていないのでケサになる
    private static void StartAttack(Component player)
    {
        IEnumerator attack = (IEnumerator)GameType("Player2").GetMethod("Attack").Invoke(player, null);
        ((MonoBehaviour)player).StartCoroutine(attack);
    }

    private static void Hurt(Component player, int damage)
    {
        GameType("Player2").GetMethod("Player_Hurt").Invoke(player, new object[] { damage, Vector3.right });
    }

    private static void Hit(Component player, GameObject enemy)
    {
        Component weapon = player.GetComponentInChildren(GameType("Weapon"), true);
        Collider body = enemy.transform.Find("BodyCollider").GetComponent<Collider>();

        MethodInfo onTrigger = GameType("Weapon").GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(onTrigger, "Weapon.OnTriggerEnter が見つからない");

        onTrigger.Invoke(weapon, new object[] { body });
    }

    private static GameObject SpawnSlimeBeside(Component player)
    {
        GameObject prefab = Resources.Load<GameObject>("Slime");
        Assert.IsNotNull(prefab, "Slime の prefab が無い");

        Vector3 at = player.transform.position + Vector3.right * EnemyDistance;
        return UnityEngine.Object.Instantiate(prefab, at, Quaternion.identity);
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
