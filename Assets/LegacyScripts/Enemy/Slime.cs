using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class Slime : MonoBehaviour, IDamageable
{
    /// <summary>当たって白く光る長さ。止めた時間の中でも見えるよう実時間で数える</summary>
    private const float FlashSeconds = 0.1f;

    /// <summary>倒れてから弾けて消えるまでの秒数</summary>
    private const float BurstSeconds = 0.25f;

    /// <summary>弾ける前に膨らむ大きさの倍率</summary>
    private const float BurstSwell = 1.3f;

    /// <summary>倒した一撃のダメージ表示を、本体が消えたあとも残す秒数</summary>
    private const float NumberLingerSeconds = 1f;

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

    /// <summary>この時刻までは吹き飛ばしに任せ、自分では動かない</summary>
    private float staggerUntil;

    private SpriteRenderer sprite;
    private Material baseMaterial;
    private Material flashMaterial;

    /// <summary>湧いた根源の Id。根源の外で湧いたものは null</summary>
    private string homeRootId;

    /// <summary>湧かせた側が、どの根源の敵かを渡す</summary>
    public void SetHome(string rootId)
    {
        homeRootId = rootId;
    }

    /// <summary>攻め込んだ先で目指す地点。持たないものはうろつくだけ</summary>
    private Vector3 goal;
    private bool hasGoal;

    /// <summary>湧かせた側が、攻め込む先の地点を渡す</summary>
    public void SetGoal(Vector3 point)
    {
        goal = point;
        hasGoal = true;
    }

    /// <summary>上限と初期値は prefab の Slider が持つ。以後 Slider は表示</summary>
    void Awake()
    {
        health.SetMax(Mathf.RoundToInt(hp.maxValue));
        health.SetCurrent(Mathf.RoundToInt(hp.value));

        rb = GetComponent<Rigidbody>();
        sprite = GetComponent<SpriteRenderer>();
        baseMaterial = sprite.sharedMaterial;

        Shader flash = Resources.Load<Shader>("SpriteFlash");
        if (flash != null) flashMaterial = new Material(flash);
    }

    private void OnDestroy()
    {
        if (flashMaterial != null) Destroy(flashMaterial);
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

    public void TakeHit(int amount, Vector3 push)
    {
        TakeDamage(amount);

        rb.linearVelocity = push;
        staggerUntil = Time.time + ActionRule.StaggerSeconds;

        if (flashMaterial != null) StartCoroutine(Flash());
    }

    void Start()
    {
        if (gameObject.name.IndexOf("(Clone)") != -1) gameObject.name = gameObject.name.Replace("(Clone)", "");

        anim = GetComponent<Animator>();
        p = GameObject.FindWithTag("Player").GetComponent<Player2>();
        RandomAction();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (move == true && Time.time >= staggerUntil)
        {
            //見つけているあいだは、動いている最中もプレイヤーへ向け直す
            if (Aim() == EnemyAim.Player) attitude = Direction(EnemyAim.Player);
            rb.linearVelocity = attitude * speed;
        }
        if (FallRule.IsOutOfField(transform.position.y)) gameObject.SetActive(false);
        if (health.IsDead)
        {
            isDead = true;
            StartCoroutine(Burst());
        }
    }

    private IEnumerator Flash()
    {
        sprite.sharedMaterial = flashMaterial;
        yield return new WaitForSecondsRealtime(FlashSeconds);
        sprite.sharedMaterial = baseMaterial;
    }

    /// <summary>
    /// 膨らんでから縮んで消える。表示の Canvas は本体と一緒に縮むため切り離し、
    /// 倒した一撃のダメージ表示だけを残す
    /// </summary>
    private IEnumerator Burst()
    {
        //倒れたらすぐ通り抜けられるようにし、その場で弾ける。床も抜けるので重力も切る。
        //当たり判定の子は prefab ごとに名前が違い、同じ名前の無効な子が先に並ぶものもあるので、名前で探さない
        foreach (Collider body in GetComponentsInChildren<Collider>()) body.enabled = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        //倒れた体はもう動かさない。跳んだ段に倒されると、溜まった跳ぶ力があとから掛かって浮いていく。
        //物理から外した体は連続の衝突検出を持てないので、先に戻す。当たり判定はもう切ってある
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;

        hp.gameObject.SetActive(false);

        Transform canvas = transform.Find("Canvas");
        if (canvas != null)
        {
            canvas.SetParent(null, true);
            Destroy(canvas.gameObject, NumberLingerSeconds);
        }

        Vector3 size = transform.localScale;
        for (float t = 0f; t < BurstSeconds; t += Time.deltaTime)
        {
            float k = t / BurstSeconds;
            float scale = k < 0.3f
                ? Mathf.Lerp(1f, BurstSwell, k / 0.3f)
                : Mathf.Lerp(BurstSwell, 0f, (k - 0.3f) / 0.7f);

            transform.localScale = size * scale;
            yield return null;
        }

        Death();
    }

    private void RandomAction()
    {
        if (isDead) return;
        EnemyAim aim = Aim();
        EnemyAction act = ActionRule.Choose(UnityRandom.Shared, aim == EnemyAim.Player);

        move = act == EnemyAction.Move;
        attitude = Direction(aim);
        if (act == EnemyAction.Jump && Time.time >= staggerUntil)
        {
            rb.AddForce((attitude + Vector3.up) * rb.mass * 2, ForceMode.Impulse);
        }

        Invoke("RandomAction", ActionRule.Duration(act, UnityRandom.Shared));
    }

    /// <summary>いま何を目指すか。倒れているプレイヤーは見ない</summary>
    private EnemyAim Aim()
    {
        float toPlayer = p != null && !Player2.Hp.IsDead ? Flat(p.transform.position).magnitude : float.MaxValue;
        float toGoal = hasGoal ? Flat(goal).magnitude : 0f;

        return AimRule.Choose(toPlayer, hasGoal, toGoal);
    }

    /// <summary>目指すものへの向き。うろつく向きは乱数で決める</summary>
    private Vector3 Direction(EnemyAim aim)
    {
        if (aim == EnemyAim.Player)
        {
            Vector3 toPlayer = Flat(p.transform.position);
            return Along(AimRule.Toward(toPlayer.x, toPlayer.z));
        }

        Vector3 wander = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        if (aim == EnemyAim.Wander) return wander;

        Vector3 toGoal = Flat(goal);
        return Along(AimRule.Lean(new Heading(wander.x, wander.z), toGoal.x, toGoal.z));
    }

    /// <summary>ここから point への水平な差</summary>
    private Vector3 Flat(Vector3 point)
    {
        Vector3 diff = point - transform.position;
        return new Vector3(diff.x, 0f, diff.z);
    }

    private static Vector3 Along(Heading heading)
    {
        return new Vector3(heading.X, 0f, heading.Z);
    }

    private void OnCollisionEnter(Collision other)
    {
        HurtPlayer(other);
    }

    //無敵の間は吹き飛ばさないので、離れずに触れ続けることがある。無敵が切れたらまた当てる
    private void OnCollisionStay(Collision other)
    {
        HurtPlayer(other);
    }

    private void HurtPlayer(Collision other)
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
        Root root = RootsManager.Roots.Find(homeRootId);
        if (root != null) root.Calm(SpawnRule.CalmPerDefeat);
        Destroy(gameObject);
    }
}

