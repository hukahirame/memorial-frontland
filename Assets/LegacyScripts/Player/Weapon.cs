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

    [SerializeField] private GameObject damage_set;
    private BoxCollider box;
    private HitStop hitStop;
    private MainCamera mainCamera;

    void Start()
    {
        box = GetComponent<BoxCollider>();
        box.enabled = false;

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
    private void OnTriggerEnter(Collider other)
    {
        int power = Equipped.Attack;

        if (other.CompareTag("MainSpawner"))
        {
            other.GetComponent<OF_Spawner>().SpawnerBreak(power);
            box.enabled = false;
            TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact");
        }
        else if (other.CompareTag("SpawnerCandidate")) 
        {
            other.GetComponent<SpawnerCandidate>().Candidate(power);
            box.enabled = false;
            TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact"); 
        }
        else if (other.transform.parent == null) { }
        else if (other.transform.parent.tag == "Enemy")
        {
            GameObject target = other.transform.parent.gameObject;
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null || damageable.CurrentHp <= 0) return; //倒れて弾けている最中の相手には当たらない

            box.enabled = false;
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
        }
    }
}
