using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;

public class Weapon : MonoBehaviour // 武器オブジェクトに直接
{
    /// <summary>装備している武器。所有者はここ1つ（Player2.Hp と同じ形）</summary>
    public static readonly Equipment Equipped = new Equipment();

    /// <summary>倒したときの音の高さ。当てたときと同じ音を低くして重さを出す</summary>
    private const float KillPitch = 0.7f;

    /// <summary>回避攻撃の当たりの下端と上端。体の中心からの高さで、足もとの敵から頭の高さまで</summary>
    private const float SpinBottom = -0.5f;
    private const float SpinTop = 0.2f;

    [SerializeField] private GameObject damage_set;
    private BoxCollider box;
    private HitStop hitStop;
    private MainCamera mainCamera;
    private Player2 player;
    private PlayerInventory inventory;

    void Start()
    {
        box = GetComponent<BoxCollider>();
        box.enabled = false;
        player = GetComponentInParent<Player2>();

        //持っているだけで攻撃力が上がる品がある。持ち物の無い場面では足さない
        GameObject held = GameObject.FindWithTag("PlayerInventory");
        if (held != null) inventory = held.GetComponent<PlayerInventory>();

        //時間止めはプレイヤーの一番上に1つだけ置く。武器を持ち替えても同じものを使う
        hitStop = transform.root.GetComponent<HitStop>();
        if (hitStop == null) hitStop = transform.root.gameObject.AddComponent<HitStop>();
        mainCamera = transform.root.GetComponentInChildren<MainCamera>();
    }


    public IEnumerator AttackWeaponProcess(float time) // 攻撃時処理
    {
        box.enabled = true;

        yield return new WaitForSeconds(time);

        box.enabled = false;
    }

    /// <summary>
    /// 回避攻撃の当たり。seconds のあいだ、体のまわりの半径 radius の縦長の範囲にいる相手に当てる。
    /// 剣の当たりと違って 1 体では止めず、範囲の中の相手それぞれに 1 回ずつ当てる
    /// </summary>
    public IEnumerator SpinStrikeProcess(float seconds, float radius)
    {
        var struck = new HashSet<GameObject>();

        for (float until = Time.time + seconds; Time.time < until;)
        {
            Vector3 center = transform.root.position;
            Collider[] found = Physics.OverlapCapsule(center + Vector3.up * SpinBottom, center + Vector3.up * SpinTop,
                                                      radius, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider other in found) Strike(other, struck);

            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //剣の当たりは、1 体に当たったらその振りではもう当たらない
        if (Strike(other, null)) box.enabled = false;
    }

    /// <summary>other に当てる。当てたら true。struck を渡すと、そこにある相手には当てず、当てた相手を足す</summary>
    private bool Strike(Collider other, HashSet<GameObject> struck)
    {
        //出ている当たりは振りの終わりまで残る。そのあいだに倒れたり移り始めたりしたら当てない
        if (player != null && !player.CanAct) return false;

        int power = Equipped.Attack + (inventory != null ? inventory.HeldAttack : 0);

        if (other.CompareTag("MainSpawner"))
        {
            if (!Mark(struck, other.gameObject)) return false;

            bool broke = other.GetComponent<OF_Spawner>().SpawnerBreak(power);
            TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact");

            //壊した一撃は、敵を倒したときと同じだけ止めて揺らす
            if (broke)
            {
                hitStop.Stop(HitStop.KillSeconds);
                if (mainCamera != null) mainCamera.Shake(MainCamera.KillShake);
                TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact", KillPitch);
            }
            return true;
        }

        if (other.CompareTag("SpawnerCandidate"))
        {
            if (!Mark(struck, other.gameObject)) return false;

            other.GetComponent<SpawnerCandidate>().Candidate(power);
            TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact");
            return true;
        }

        //柵は剣では削れない。壊すときは、柵のそばで出る Allmaity ボタンで壊す
        if (other.GetComponentInParent<Fence>() != null) return false;

        if (other.transform.parent == null || other.transform.parent.tag != "Enemy") return false;

        GameObject target = other.transform.parent.gameObject;
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return false; //倒れて弾けている最中の相手には当たらない
        if (!Mark(struck, target)) return false;

        TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact");

        Vector3 from = transform.root.position;
        Vector3 to = target.transform.position;
        Push push = KnockbackRule.Velocity(from.x, from.z, to.x, to.z, Equipped.Knockback, transform.lossyScale.x);
        damageable.TakeHit(power, new Vector3(push.X, push.Y, push.Z));
        bool killed = damageable.CurrentHp <= 0;

        var o = Instantiate(damage_set, other.transform.position, Quaternion.identity, other.transform.parent.Find("Canvas"));
        o.GetComponent<DamageSet>().Text(power);

        hitStop.Stop(killed ? HitStop.KillSeconds : HitStop.HitSeconds);
        if (mainCamera != null) mainCamera.Shake(killed ? MainCamera.KillShake : MainCamera.HitShake);
        if (killed) TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact", KillPitch);

        if (Equipped.Wear(1)) //武器耐久値0
        {
            Equipped.Unequip();
            GameObject.FindWithTag("PlayerInventory/WeaponBox").GetComponent<WeaponBox>().Weapon_Des();
        }
        return true;
    }

    //struck が無ければ、いつでも当てる
    private static bool Mark(HashSet<GameObject> struck, GameObject target)
    {
        return struck == null || struck.Add(target);
    }
}
