using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class Slime : MonoBehaviour
{
    public float speed;  //種類により変わるが、基本初期値はここで決める
    public float power;
    public int Level;
    public List<string> drops = new List<string>(); // ドロップアイテム群
    public GameObject dropcapsule;
    public Slider hp;

    private Animator anim;
    private Rigidbody rb;
    private Vector3 attitude;
    private bool isDead = false;

    private bool move = false;
    private Player2 p;
    void Start()
    {
        if (gameObject.name.IndexOf("(Clone)") != -1) gameObject.name = gameObject.name.Replace("(Clone)", "");
        
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        p = GameObject.FindWithTag("Player").GetComponent<Player2>();
        RandomAction();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (move == true) rb.linearVelocity = attitude * speed;
        if (transform.position.y < -10) gameObject.SetActive(false);
        if (hp.value <= 0)
        {
            //anim
            transform.Find("BodyCollider").gameObject.SetActive(false);
            isDead = true;
            Invoke("Death", 1f);
        }
    }

    private void RandomAction()
    {
        if (isDead) return;
        int act = Random.Range(0, 101);

        move = false;
        attitude = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        if (act <= 60) //待機 60%
        {
            Invoke("RandomAction", Random.Range(1.5f, 2f));
        }
        else if (act <= 80) //移動 20%
        {
            move = true;
            Invoke("RandomAction", Random.Range(0.5f, 2.5f));
        }
        else  //ジャンプ 20%
        {
            rb.AddForce((attitude + Vector3.up) * rb.mass * 2, ForceMode.Impulse);
            Invoke("RandomAction", 1.2f);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if(isDead) return;
        if(other.gameObject.tag == "Player")
        {
            p.Player_Hurt((int)power,other.gameObject.transform.position - transform.position);
        }
    }

    private void Death() //死亡モーション終了時
    {
        if (Random.Range(0, 100) < 10) drops.Add("Slimecore");
        foreach (string s in drops)
        {
            GameObject d = Instantiate(dropcapsule,transform.position,Quaternion.identity);
            d.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(s);
            Vector3 v = new Vector3(Random.Range(0, 1f), Random.Range(0.5f, 1), Random.Range(0, 1f)) * 2;
            d.GetComponent<Rigidbody>().AddForce(v, ForceMode.Impulse);
        }
        GameObject.FindWithTag("QuestManager").GetComponent<QuestManager>().SyncQuest(gameObject.name);
        Root root = RootsManager.Roots.Find(GameManager.entered_scene);
        if (root != null) root.Calm(3);
        Destroy(gameObject);
    }
}

