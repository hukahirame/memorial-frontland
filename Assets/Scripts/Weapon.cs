using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon : MonoBehaviour // 武器オブジェクトに直接
{
    public static int power = 40;
    public static float knockback;

    [SerializeField] private GameObject damage_set;
    public static Slider pw_durability;
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
            target.transform.Find("Canvas/Slider").GetComponent<Slider>().value -= power;

            var o = Instantiate(damage_set, other.transform.position, Quaternion.identity, other.transform.parent.Find("Canvas"));
            o.GetComponent<DamageSet>().Text(power);

            //TempAudio.TempAudioPlay("EnemyHit");

            Rigidbody enemyrb = target.GetComponent<Rigidbody>();
            enemyrb.AddForce(new Vector3(knockback * transform.up.x, knockback / 3, 0) * enemyrb.mass, ForceMode.Impulse);

            // pw_durability.value -= 1;
            if ((pw_durability != null) && (pw_durability.value <= 0))  //武器耐久値0
            {
                GameObject.FindWithTag("PlayerInventory/WeaponBox").GetComponent<WeaponBox>().Weapon_Des();

                /*player.weaponbutton.GetComponent<Image>().sprite = Resources.Load<Sprite>("ButtonSprite");
                player.weaponbutton.name = "Box";
                player.weaponbutton.transform.GetChild(0).GetComponent<Text>().text = null;
                player.weaponparameta.transform.localScale = new Vector3(0, 1, 1);
                Destroy(player.w);*/
            }


        }
    }
}
