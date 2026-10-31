using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;

//攻撃・撃破・被弾の仕掛けが、実際の場で例外なく働くことを見る。手触りの良し悪しは見ない。
//当たったあとの処理の多くは武器の OnTriggerEnter を直に呼んで確かめる。振りが届くかだけは物理で当てる
public class CombatPlayTests
{
    private const int SettleFrames = 120;

    /// <summary>移動が終わるのを待つ上限（実時間の秒）</summary>
    private const float TravelSeconds = 10f;

    /// <summary>フレームの刻みで遅れてよい幅（秒）</summary>
    private const float Slack = 0.15f;

    /// <summary>敵を置く距離。触れて被弾しない程度に離す。落とし物は +x へ飛ぶので、拾わないよう +x 側に置く</summary>
    private const float EnemyDistance = 1.5f;

    /// <summary>突きが届く距離。触れずに済む近さ（体どうしが触れるのは約 0.5）で、振りの先が確実に重なるところ</summary>
    private const float ReachDistance = 0.7f;

    /// <summary>敵が寄ってこない距離。プレイヤーを見つけるのは 4 以内</summary>
    private const float FarDistance = 6f;

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
        yield return new WaitForSeconds(AttackTiming.SwingSeconds + Slack);

        Assert.Less(enemy.CurrentHp, before, "突きが届かない");
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
    public IEnumerator 倒れている間は攻撃スティックを離しても振らない()
    {
        yield return Boot();
        Component player = Player();
        BoxCollider box = WeaponBox(player);

        Hurt(player, PlayerHp().Current);
        Assert.IsTrue(PlayerHp().IsDead, "倒れない");

        ReleaseAttackStick(0f, 0f);

        bool swung = false;
        float started = Time.time;
        while (Time.time - started < AttackTiming.SwingSeconds + Slack)
        {
            if (box.enabled) swung = true;
            yield return null;
        }

        Assert.IsFalse(swung, "倒れたまま振った");
    }

    [UnityTest]
    public IEnumerator 振りかぶっているあいだに倒れると当たり判定は出ない()
    {
        yield return Boot();
        Component player = Player();
        BoxCollider box = WeaponBox(player);

        StartAttack(player);
        yield return null;
        Hurt(player, PlayerHp().Current);
        Assert.IsTrue(PlayerHp().IsDead, "倒れない");

        bool swung = false;
        float started = Time.time;
        while (Time.time - started < AttackTiming.SwingSeconds + Slack)
        {
            if (box.enabled) swung = true;
            yield return null;
        }

        Assert.IsFalse(swung, "倒れた体から当たり判定が出る");
    }

    [UnityTest]
    public IEnumerator 倒れたあとは出ている当たり判定に触れても当たらない()
    {
        yield return Boot();
        Component player = Player();
        GameObject slime = SpawnSlime(player, Vector3.right * FarDistance, "Slime");
        yield return Hold(slime);
        IDamageable enemy = slime.GetComponent<IDamageable>();
        int before = enemy.CurrentHp;

        Hurt(player, PlayerHp().Current);
        Assert.IsTrue(PlayerHp().IsDead, "倒れない");
        Hit(player, slime);

        Assert.AreEqual(before, enemy.CurrentHp, "倒れた体の振りが当たる");
    }

    [UnityTest]
    public IEnumerator 当てた敵は離れる向きに飛び白く光り時間は少し止まって戻る()
    {
        yield return Boot();
        Component player = Player();
        GameObject slime = SpawnSlime(player, Vector3.right * EnemyDistance, "Slime");
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

    //当たり判定の子の名前と並びは prefab ごとに違う。同じ名前の無効な子が先に並ぶものもある
    [UnityTest]
    public IEnumerator 倒した敵はすぐ通り抜けられその場で弾けて消え落とし物が残る(
        [Values("Slime", "SodaSlime")] string kind)
    {
        yield return Boot();
        Component player = Player();
        GameObject slime = SpawnSlime(player, Vector3.right * EnemyDistance, kind);
        yield return null;

        Collider[] bodies = slime.GetComponentsInChildren<Collider>();
        Assert.IsNotEmpty(bodies, "当たり判定が無い");

        IDamageable enemy = slime.GetComponent<IDamageable>();
        enemy.TakeDamage(enemy.CurrentHp - 1);
        int drops = UnityEngine.Object.FindObjectsByType<Dropitem>(FindObjectsSortMode.None).Length;

        //跳ぶ力は次の物理の段まで溜まる。跳んだ段に倒されると、倒れたあとの体に掛かる。乱数で跳ぶのを待たずにここで溜める
        Rigidbody slimeBody = slime.GetComponent<Rigidbody>();
        slimeBody.AddForce(Vector3.up * slimeBody.mass * 2f, ForceMode.Impulse);

        Hit(player, slime);
        Assert.AreEqual(0, enemy.CurrentHp);
        Vector3 at = slime.transform.position;

        yield return new WaitForSecondsRealtime(HitStop.KillSeconds + Slack);
        Assert.IsTrue(slime != null, "弾ける前に消えた");
        Assert.IsTrue(bodies.All(body => !body.enabled), "倒れた敵が道をふさいだまま");
        Assert.AreEqual(at.y, slime.transform.position.y, 0.02f, "弾けるあいだに落ちたか浮いた");

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
    public IEnumerator 被弾の点滅が終わると髪や目の色は元に戻る()
    {
        yield return Boot();
        Component player = Player();
        //カメラの子の演出は自分で色を動かすことがあるので、体の部品だけを見る
        SpriteRenderer[] parts = player.GetComponentsInChildren<SpriteRenderer>()
            .Where(part => part.GetComponentInParent<MainCamera>() == null)
            .ToArray();
        Color[] before = parts.Select(part => part.color).ToArray();

        Hurt(player, 1);
        yield return new WaitForSeconds(Invulnerability.Seconds + Slack);

        for (int i = 0; i < parts.Length; i++)
        {
            Assert.AreEqual(before[i], parts[i].color, parts[i].name + " の色が戻らない");
        }
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

        //移されたとみなさない大きさ
        player.transform.position += Vector3.right * MainCamera.SnapDistance * 0.5f;
        yield return null;

        Assert.Greater(player.transform.position.x + offset.x - view.position.x, 0.05f, "一度に追い付いた");

        yield return new WaitForSeconds(1f);
        Assert.AreEqual(player.transform.position.x + offset.x, view.position.x, 0.05f, "追い付かない");
    }

    [UnityTest]
    public IEnumerator 近くへ移されたときカメラは追わずに飛ぶ()
    {
        yield return Boot();
        Component player = Player();
        Transform view = player.GetComponentInChildren<MainCamera>().transform;
        Vector3 offset = view.position - player.transform.position;

        //復活の地点へ戻されるときのように、場所は変わらず少しだけ離れたところへ移す
        player.transform.position += new Vector3(1.5f, 0f, 1.5f);
        yield return null;

        Vector3 lag = player.transform.position + offset - view.position;
        Assert.Less(new Vector2(lag.x, lag.z).magnitude, 0.01f, "移されたあと画面が流れる");
    }

    [UnityTest]
    public IEnumerator 時間止めが明けてすぐ動いてもカメラは崩れない()
    {
        yield return Boot();
        Component player = Player();
        Transform view = player.GetComponentInChildren<MainCamera>().transform;
        HitStop hitStop = player.transform.root.GetComponent<HitStop>();
        Assert.IsNotNull(hitStop, "時間止めが無い");

        //移されたとみなす大きさで動かし、カメラを追い付き切らせる。止まって当てたときと同じ
        player.transform.position += Vector3.left * MainCamera.SnapDistance * 2f;
        yield return null;
        yield return null;

        hitStop.Stop(HitStop.HitSeconds);
        yield return null;
        float giveUp = Time.realtimeSinceStartup + 1f;
        while (Time.timeScale == 0f && Time.realtimeSinceStartup < giveUp) yield return null;

        player.transform.position += Vector3.right * 0.1f;
        yield return null;
        yield return null;

        Assert.IsFalse(float.IsNaN(view.position.x), "時間止めのあと、カメラの位置が壊れる");
    }

    [UnityTest]
    public IEnumerator 場所を移っているあいだはジャンプボタンを押しても跳ばない()
    {
        yield return Boot();
        Component player = Player();
        Rigidbody body = player.GetComponent<Rigidbody>();
        UnityEvent jump = JumpButton(player);

        //拠点で倒れたときと同じく、読み直さずに立ち位置だけ移す。覆い終わると物理は戻るが、明けるまでは移っている
        Travel().Revive();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (body.isKinematic && Time.realtimeSinceStartup < giveUp) yield return null;
        yield return null;
        Assert.IsTrue(Travel().IsTraveling, "明ける前に移り終わった");

        jump.Invoke();
        yield return new WaitForFixedUpdate();
        Assert.Less(body.linearVelocity.y, 1f, "移っているあいだに跳ぶ");

        while (Travel().IsTraveling && Time.realtimeSinceStartup < giveUp) yield return null;
        for (int i = 0; i < 10; i++) yield return null;

        jump.Invoke();
        yield return new WaitForFixedUpdate();
        Assert.Greater(body.linearVelocity.y, 1f, "着いたあとも跳ばない");
    }

    [UnityTest]
    public IEnumerator 場所を移っているあいだと倒れているあいだは足音が止まる()
    {
        yield return Boot();
        Component player = Player();
        AudioSource footstep = (AudioSource)PrivateField(player, "footstep").GetValue(player);

        Walk(footstep);
        Travel().Revive();
        yield return null;
        Assert.IsFalse(footstep.isPlaying, "移っているあいだも足音が鳴り続ける");

        float giveUp = Time.realtimeSinceStartup + TravelSeconds;
        while (Travel().IsTraveling && Time.realtimeSinceStartup < giveUp) yield return null;
        yield return null;

        Walk(footstep);
        Hurt(player, PlayerHp().Current);
        Assert.IsTrue(PlayerHp().IsDead, "倒れない");
        yield return null;
        Assert.IsFalse(footstep.isPlaying, "倒れているあいだも足音が鳴り続ける");
    }

    [UnityTest]
    public IEnumerator 場の縁を越えて押し戻されても跳んだ勢いは残る()
    {
        yield return Boot();
        Component player = Player();
        Rigidbody body = player.GetComponent<Rigidbody>();
        FieldBounds bounds = Exposition.ToBounds((Vector4)GameType("Player2").GetField("expos").GetValue(player));

        //手前の縁を少し越えたところ。壁際の帯で、連れ戻されるほどは出ていない
        Vector3 at = new Vector3(body.position.x, body.position.y, bounds.MinZ - FieldBounds.EscapeMargin * 0.25f);
        body.position = at;
        player.transform.position = at;
        body.linearVelocity = Vector3.up * 4f;
        yield return null;

        Assert.Greater(body.linearVelocity.y, 1f, "押し戻すときに縦の勢いまで消え、宙で止まる");
    }

    [UnityTest]
    public IEnumerator 大きい文字を続けて出すと後の文字は最後まで出る()
    {
        yield return Boot();
        Component big = GameObject.Find("BigText").GetComponent(GameType("BigText"));
        float wait = (float)GameType("BigText").GetField("waittime").GetValue(big);

        //入りと出に 1 秒ずつ。達成のすぐあとに倒れたときの順
        float shown = wait + 2f;
        Call(big, "Bigtxt_Anim", "達成", "達成");
        yield return new WaitForSeconds(shown / 2f);
        Call(big, "Bigtxt_Anim", "体力が0になりました", "失敗");

        //前の文字が終わるはずの時刻を過ぎても、後の文字は出ている
        yield return new WaitForSeconds(shown / 2f + 0.5f);
        Assert.AreEqual(1f, big.transform.localScale.x, "前の文字の終わりで、後の文字まで消える");
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

    private static BoxCollider WeaponBox(Component player)
    {
        Component weapon = player.GetComponentInChildren(GameType("Weapon"), true);
        Assert.IsNotNull(weapon, "武器が無い");

        return weapon.GetComponent<BoxCollider>();
    }

    /// <summary>向きを決める。攻撃の選び分けが向きとして見る部品を裏返す</summary>
    private static void Face(Component player, float facing)
    {
        Component body = player.GetComponentInChildren(GameType("SideJab"));
        Assert.IsNotNull(body, "SideJab が無い");

        body.transform.localScale = new Vector3(facing, 1f, 1f);
    }

    //攻撃スティックを離したときと同じ入口。スティックは倒れていないのでケサになる
    private static void StartAttack(Component player)
    {
        IEnumerator attack = (IEnumerator)GameType("Player2").GetMethod("Attack").Invoke(player, null);
        ((MonoBehaviour)player).StartCoroutine(attack);
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

    //ジャンプボタンを押したときに呼ばれるもの。UnityEngine.UI は asmdef から参照していないので名前で引く
    private static UnityEvent JumpButton(Component player)
    {
        Component button = (Component)PrivateField(player, "jumpbutton").GetValue(player);
        Assert.IsNotNull(button, "ジャンプボタンが無い");

        return (UnityEvent)button.GetType().GetProperty("onClick").GetValue(button);
    }

    //歩いているときと同じく鳴らす
    private static void Walk(AudioSource footstep)
    {
        footstep.loop = true;
        footstep.Play();
        Assert.IsTrue(footstep.isPlaying, "足音を鳴らせない");
    }

    private static object Call(Component target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, target.GetType().Name + "." + methodName + " が見つからない");

        return method.Invoke(target, args);
    }

    private static FieldInfo PrivateField(Component target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " が見つからない");

        return field;
    }

    private static void Hurt(Component player, int damage)
    {
        GameType("Player2").GetMethod("Player_Hurt").Invoke(player, new object[] { damage, Vector3.right });
    }

    private static void Hit(Component player, GameObject enemy)
    {
        Component weapon = player.GetComponentInChildren(GameType("Weapon"), true);
        Collider body = enemy.GetComponentsInChildren<Collider>().FirstOrDefault(found => found.enabled);
        Assert.IsNotNull(body, "敵に有効な当たり判定が無い");

        MethodInfo onTrigger = GameType("Weapon").GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(onTrigger, "Weapon.OnTriggerEnter が見つからない");

        onTrigger.Invoke(weapon, new object[] { body });
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
