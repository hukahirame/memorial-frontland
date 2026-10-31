using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MemorialFloor.Domain;
using MemorialFloor.Game;

public class OF_Spawner : MonoBehaviour
{
    public float spawnrange; // 最大範囲
    public GameObject seeker;
    public GameObject enemy;

    /// <summary>ここから出した敵。湧きを見送るかは、このうちいま居る数で決める</summary>
    private readonly List<GameObject> spawned = new List<GameObject>();

    private AreaTravel travel;

    private Seeker seeker_cs;

    public static bool spawnable = false;

    [Tooltip("拠点の耐久。スポナーごとに個別に持つ")]
    [SerializeField] private int maxHp = 100;

    private readonly Health health = new Health();

    public Vector4 expos; //XL,XS,ZL,ZS

    /// <summary>置かれた根源の Id</summary>
    private string rootId;

 //   private GameObject tower1;
 //   private GameObject tower1_down;

    /// <summary>いつも走らせておく湧きの連鎖の本数。壊したときの大量発生でも増やさない</summary>
    private const int SpawnChains = 2;

    /// <summary>壊れてから見えなくなるまでの秒数。倒した敵と同じ見せ方にする</summary>
    private const float BurstSeconds = 0.25f;

    /// <summary>壊れる前に膨らむ大きさの倍率</summary>
    private const float BurstSwell = 1.3f;

    void Awake()
    {
        health.SetMax(maxHp);
        health.SetCurrent(maxHp);
    }

    /// <summary>根源に置かれたとき、場の範囲と根源を受け取る。Start より前に呼ぶ</summary>
    public void Bind(Vector4 exposition, string rootId)
    {
        expos = exposition;
        this.rootId = rootId;
    }

    void Start()
    {
        //大部分はSceneStarterから実行

        gameObject.tag = "MainSpawner";
        spawnrange = Random.Range(6f, 12f);

        seeker = GameObject.Find("Seeker");
        seeker.transform.parent = transform;
        seeker_cs = seeker.GetComponent<Seeker>();
        enemy = (GameObject)Resources.Load("Slime");

        //移動の入口は常駐シーンにあり、シーンを跨ぐ参照は持てないので引く
        travel = FindAnyObjectByType<AreaTravel>();

        //保存した敵は、保存した根源に入ったときにだけ戻す。拠点で保存した敵は戻さない（攻め込んだ敵と同じく保存しない）
        SaveData sd = GameObject.Find("GameManager").GetComponent<SaveSystem>().savedata;
        Area here = Area.In(gameObject.scene);
        if (here != null && here.Id == sd.entered_scene && sd.respawns.Count > 0)
        {
            foreach (Vector3 pos in sd.respawns) Put(pos);
            sd.enemies.Clear();
            sd.respawns.Clear();
        }
        else
        {
            for (int i = 0; i < SpawnChains; i++) Spawn(spawnrange * 2);
        }

        for (int i = 0; i < SpawnChains; i++) StartCoroutine(Chain());

    //    tower1 = GameObject.Find("LOD_Wall_Stone_x1");
    //    tower1_down = GameObject.Find("LOD_Wall_Stone_x1(down)");
    //    tower1_down.SetActive(false);
    }

    /// <summary>湧きの連鎖の 1 本。待っては 1 体湧かせる。根源を離れて物が無効になると止まる</summary>
    private IEnumerator Chain()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(15, 30));

            //移動のあいだは出さない。根源を離れる途中に出すと、着いた先のシーンに入る
            if (travel != null && travel.IsTraveling) continue;

            Spawn(spawnrange);
        }
    }

    /// <summary>湧き口のまわり range の中に 1 体湧かせる。Seeker が地面を見つけて Spawn2 を呼び返す</summary>
    private void Spawn(float range)
    {
        seeker.SetActive(true);
        seeker_cs.SeekPosition(range, expos);
    }

    /// <summary>Seeker が見つけた地面に 1 体出す</summary>
    public void Spawn2(Vector3 pos)
    {
        Debug.Log("Spawn");
        Put(pos);
    }

    /// <summary>pos に 1 体出す。次の湧きは仕掛けない</summary>
    private void Put(Vector3 pos)
    {
        //根源は保持せず使うときに引く。参照を握るとレジストリが唯一の窓口でなくなり、
        //中身を入れ替えたときに古い Root を指したまま生き残る
        Root root = RootsManager.Roots.Find(rootId);
        if (SpawnRule.ShouldSuppress(root, Alive(), UnityRandom.Shared)) return;

        var obj = Instantiate(enemy, pos, Quaternion.identity);
        Slime slime = obj.GetComponent<Slime>();
        if (slime != null) slime.SetHome(rootId);
        seeker.gameObject.SetActive(false);
        var damageable = obj.GetComponent<IDamageable>();
        if (SpawnRule.ShouldWeaken(root) && damageable != null)
        {
            damageable.TakeDamage(Mathf.RoundToInt(damageable.CurrentHp * SpawnRule.WeakenRatio));
        }
        spawned.Add(obj);
    }

    /// <summary>ここから出した敵のうち、いま居る数。倒されて消えたものと場外へ落ちたものは数えない</summary>
    private int Alive()
    {
        spawned.RemoveAll(slime => slime == null || !slime.activeSelf);
        return spawned.Count;
    }

    /// <summary>壊したときの大量発生。一度きりで、湧きの連鎖は足さない</summary>
    public void Stampede(int amount)
    {
        for (int i = 0; i < amount; i++) Spawn(spawnrange * 2);
        Vector3 stoppos = GameObject.Find("ST_MainSite").transform.position;
        Put(stoppos + new Vector3(Random.Range(1f,6f),0,Random.Range(-5f,1f)));
        Put(stoppos + new Vector3(Random.Range(1f, 6f), 0, Random.Range(-5f, 1f)));
     //   tower1.SetActive(false);
     //   tower1_down.SetActive(true);
    }


    /// <summary>この一撃で壊れたら true。壊れたあとに呼ばれても、湧きや達成を重ねて起こさない</summary>
    public bool SpawnerBreak(int damage)
    {
        if (health.IsDead) return false;

        health.Take(damage);
        if (health.IsDead)
        {
            GameObject.Find("GameManager").GetComponent<AudioSource>().clip =(AudioClip) Resources.Load("dark city");
            GameObject.Find("GameManager").GetComponent<AudioSource>().Play();
            Stampede(Random.Range(4,7));
            GameObject.FindWithTag("QuestManager").GetComponent<QuestManager>().SyncQuest("MainSpawner");
            var colliders = GetComponents<Collider>();
            foreach (Collider collider in colliders) { collider.enabled = false; }
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("敵が大量発生しています");
            StartCoroutine(Burst());
            return true;
        }

        return false;
    }

    /// <summary>
    /// 膨らんでから縮み、見えなくなる。湧き口としては残すので物は消さず、描画だけ切る。
    /// 子の Seeker を縮めたままにしないよう、大きさは最後に戻す
    /// </summary>
    private IEnumerator Burst()
    {
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

        foreach (Renderer shown in GetComponentsInChildren<Renderer>()) shown.enabled = false;
        transform.localScale = size;
    }
}
