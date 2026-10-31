using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;

/// <summary>
/// 拠点の湧き口。根源の氾濫で拠点へ向かってきた敵を出す。拠点のシーンにだけ置く。
/// 出した敵は拠点の中心を目指す。拠点を離れるときに生き残っている敵は積み戻し、戻ったときにまた出す
/// </summary>
public class MS_Spawner : MonoBehaviour
{
    /// <summary>円が場からはみ出すとき、場に入る点を引き直す回数。尽きたら円の中心を場へ丸める</summary>
    private const int PickTries = 16;

    [Tooltip("湧き口からのばらつき。この半径の中に出す")]
    [SerializeField] private float scatter = 1.5f;

    private GameObject enemy;
    private AreaTravel travel;

    /// <summary>場の範囲。拠点の場所が見つからなければ null</summary>
    private FieldBounds bounds;

    /// <summary>攻め込んだ敵が目指す、拠点の中心。場の範囲の真ん中</summary>
    private Vector3 center;

    /// <summary>ここから出した敵。倒されて消えたものは null になる</summary>
    private readonly List<GameObject> raiders = new List<GameObject>();

    void Start()
    {
        enemy = (GameObject)Resources.Load("Slime");

        Area area = Area.In(gameObject.scene);
        if (area != null) bounds = Exposition.ToBounds(area.Exposition);
        center = bounds != null ? new Vector3(bounds.CenterX, transform.position.y, bounds.CenterZ) : transform.position;

        //移動の入口は常駐シーンにあり、シーンを跨ぐ参照は持てないので引く
        travel = FindAnyObjectByType<AreaTravel>();
        if (travel != null) travel.Leaving += Retreat;

        //留守のあいだに来たものは、もう中心のあたりまで攻め込んでいる
        int arrived = RootsManager.Raid.Arrive();
        for (int left = arrived; left > 0; left--) Spawn(center, AimRule.GoalRadius);

        if (arrived > 0) StartCoroutine(Announce(arrived));
    }

    /// <summary>暗転が明けてから知らせる。覆われているあいだに出すと見えない</summary>
    private IEnumerator Announce(int count)
    {
        while (travel != null && travel.IsTraveling) yield return null;

        GameObject middle = GameObject.Find("MiddleText");
        if (middle != null) middle.GetComponent<MiddleText>().Midtxt_Anim("拠点に敵が " + count + " 体入り込んでいます");
    }

    void OnDestroy()
    {
        if (travel != null) travel.Leaving -= Retreat;
    }

    void Update()
    {
        //移動のあいだは出さない。積み戻したあとに出すと、拠点のシーンと一緒に消える
        if (travel != null && travel.IsTraveling) return;

        for (int left = RootsManager.Raid.Tick(Time.deltaTime); left > 0; left--) Spawn(transform.position, scatter);
    }

    /// <summary>拠点を離れる。生き残っている敵は拠点のシーンと一緒に消えるので、数えて積み戻す</summary>
    private void Retreat(Area area)
    {
        if (area.Scene != gameObject.scene) return;

        int alive = 0;
        foreach (GameObject raider in raiders)
        {
            if (IsAlive(raider)) alive++;
        }

        raiders.Clear();
        RootsManager.Raid.Add(alive);
    }

    /// <summary>倒されて消えたもの、倒れて消える途中のもの、場外へ落ちたものは数えない</summary>
    private static bool IsAlive(GameObject raider)
    {
        if (raider == null || !raider.activeSelf) return false;

        IDamageable damageable = raider.GetComponent<IDamageable>();
        return damageable == null || damageable.CurrentHp > 0;
    }

    /// <summary>around から radius の中に出す。場の外へははみ出さない</summary>
    private void Spawn(Vector3 around, float radius)
    {
        Vector3 pos = PickInField(around, radius);

        //地面が見つからなければ高さを変えずに出す。拠点へ向かってきた敵を取りこぼさない
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f)) pos = hit.point + Vector3.up * 0.6f;

        //MainSite に対応する根源は無いため、攻略度による弱体化は行わない
        GameObject raider = Instantiate(enemy, pos, Quaternion.identity);
        raider.GetComponent<Slime>().SetGoal(center);
        raiders.Add(raider);
    }

    /// <summary>around から radius の円の中で、場に入る点。はみ出した点を丸めると場の縁に一列に並ぶので、引き直す</summary>
    private Vector3 PickInField(Vector3 around, float radius)
    {
        for (int tries = 0; tries < PickTries; tries++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 pos = around + new Vector3(offset.x, 0, offset.y);
            if (bounds == null || bounds.Contains(pos.x, pos.z)) return pos;
        }

        return new Vector3(bounds.ClampX(around.x), around.y, bounds.ClampZ(around.z));
    }
}
