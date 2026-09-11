using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class Slime : MonoBehaviour, IDamageable
{
    public float speed;  //種類により変わるが、基本初期値はここで決める
    public float power;
    public int Level;
    public List<string> drops = new List<string>(); // 必ず落ちる取得物

    [Tooltip("割合で落ちる取得物")]
    [SerializeField]
    private List<DropChance> chanceDrops = new List<DropChance>
    {
        new DropChance { ItemId = "Slimecore", Percent = 10 }
    };

    public GameObject dropcapsule;
    public Slider hp;

    private readonly Health health = new Health();

    private Animator anim;
    private Rigidbody rb;
    private Vector3 attitude;
    private bool isDead = false;

    private bool move = false;
    private Player2 p;

    /// <summary>上限と初期値は prefab の Slider が持つ。以後 Slider は表示</summary>
    void Awake()
    {
        health.SetMax(Mathf.RoundToInt(hp.maxValue));
        health.SetCurrent(Mathf.RoundToInt(hp.value));
    }

    public int CurrentHp
    {
        get { return health.Current; }
    }

    public void TakeDamage(int amount)
    {
        health.Take(amount);
        hp.value = health.Current;
    }

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
        if (FallRule.IsOutOfField(transform.position.y)) gameObject.SetActive(false);
        if (health.IsDead)
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
        EnemyAction act = ActionRule.Choose(UnityRandom.Shared);

        move = act == EnemyAction.Move;
        attitude = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        if (act == EnemyAction.Jump)
        {
            rb.AddForce((attitude + Vector3.up) * rb.mass * 2, ForceMode.Impulse);
        }

        Invoke("RandomAction", ActionRule.Duration(act, UnityRandom.Shared));
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
        foreach (string s in DropRule.Roll(drops, chanceDrops, UnityRandom.Shared))
        {
            GameObject d = Instantiate(dropcapsule,transform.position,Quaternion.identity);
            d.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(s);
            Vector3 v = new Vector3(Random.Range(0, 1f), Random.Range(0.5f, 1), Random.Range(0, 1f)) * 2;
            d.GetComponent<Rigidbody>().AddForce(v, ForceMode.Impulse);
        }
        GameObject.FindWithTag("QuestManager").GetComponent<QuestManager>().SyncQuest(gameObject.name);
        Root root = RootsManager.Roots.Find(GameManager.entered_scene);
        if (root != null) root.Calm(SpawnRule.CalmPerDefeat);
        Destroy(gameObject);
    }
}

