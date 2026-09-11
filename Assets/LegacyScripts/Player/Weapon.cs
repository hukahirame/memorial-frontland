using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;

public class Weapon : MonoBehaviour // 武器オブジェクトに直接
{
    /// <summary>装備している武器。所有者はここ1つ（Player2.Hp と同じ形）</summary>
    public static readonly Equipment Equipped = new Equipment();

    [SerializeField] private GameObject damage_set;
    private BoxCollider box;
    void Start()
    {
        box = GetComponent<BoxCollider>();
        box.enabled = false;
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
            box.enabled = false;
            TempAudio.TempAudioPlay("Fantasy_Game_Attack_Weapon_Impact");
            GameObject target = other.transform.parent.gameObject;
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null) damageable.TakeDamage(power);

            var o = Instantiate(damage_set, other.transform.position, Quaternion.identity, other.transform.parent.Find("Canvas"));
            o.GetComponent<DamageSet>().Text(power);

            //TempAudio.TempAudioPlay("EnemyHit");

            Rigidbody enemyrb = target.GetComponent<Rigidbody>();
            float knockback = Equipped.Knockback;
            enemyrb.AddForce(new Vector3(knockback * transform.up.x, knockback / 3, 0) * enemyrb.mass, ForceMode.Impulse);

            if (Equipped.Wear(1)) //武器耐久値0
            {
                Equipped.Unequip();
                GameObject.FindWithTag("PlayerInventory/WeaponBox").GetComponent<WeaponBox>().Weapon_Des();
            }
        }
    }
}
