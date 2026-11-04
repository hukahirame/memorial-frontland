using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;

/// <summary>
/// 拠点の湧き口。根源の氾濫で拠点に攻め込んだ敵を出す。拠点のシーンにだけ置く。
/// 出した敵は拠点の中心を目指す。拠点を離れるときに生き残っている敵は位置と体力を控え、戻ったときに同じところへ出す
/// </summary>
public class MS_Spawner : MonoBehaviour
{
    /// <summary>円が場からはみ出すとき、場に入る点を引き直す回数。尽きたら円の中心を場へ丸める</summary>
    private const int PickTries = 16;

    [Tooltip("湧き口からのばらつき。氾濫で来た敵は、この半径の中に出す")]
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

        //留守のあいだに攻め込んだ敵と、離れる前に生き残っていた敵
        int arrived = Release();
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
        //移動のあいだは出さない。離れる途中に出すと、拠点のシーンと一緒に消える
        if (travel != null && travel.IsTraveling) return;

        //居るあいだに氾濫で攻め込んだ敵は、すぐ出す
        if (RootsManager.Raid.Kept.Count > 0) Release();
    }

    /// <summary>控えている敵をすべて出す。出した数を返す</summary>
    private int Release()
    {
        List<Raider> kept = RootsManager.Raid.TakeAll();
        foreach (Raider raider in kept) Spawn(raider);

        return kept.Count;
    }

    /// <summary>拠点を離れる。生き残っている敵は拠点のシーンと一緒に消えるので、位置と体力を控える</summary>
    private void Retreat(Area area)
    {
        if (area.Scene != gameObject.scene) return;

        foreach (Raider raider in Survivors()) RootsManager.Raid.Keep(raider);
        raiders.Clear();
    }

    /// <summary>生き残っている敵の位置と体力。拠点に居るあいだに保存するときも写す</summary>
    public List<Raider> Survivors()
    {
        List<Raider> alive = new List<Raider>();
        foreach (GameObject raider in raiders)
        {
            if (!IsAlive(raider)) continue;

            Vector3 at = raider.transform.position;
            IDamageable damageable = raider.GetComponent<IDamageable>();
            alive.Add(new Raider(at.x, at.y, at.z, damageable != null ? damageable.CurrentHp : Raider.FullHp));
        }

        return alive;
    }

    /// <summary>倒されて消えたもの、倒れて消える途中のもの、場外へ落ちたものは数えない</summary>
    private static bool IsAlive(GameObject raider)
    {
        if (raider == null || !raider.activeSelf) return false;

        IDamageable damageable = raider.GetComponent<IDamageable>();
        return damageable == null || damageable.CurrentHp > 0;
    }

    /// <summary>控えていた 1 体を出す。位置を持つものはその位置に、持たないものは湧き口のまわりに出す。体力は控えた値まで減らす</summary>
    private void Spawn(Raider kept)
    {
        Vector3 pos;
        if (kept.HasPosition)
        {
            pos = new Vector3(kept.X, kept.Y, kept.Z);
        }
        else
        {
            pos = PickInField(transform.position, scatter);

            //地面が見つからなければ高さを変えずに出す。攻め込んだ敵を取りこぼさない
            if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f)) pos = hit.point + Vector3.up * 0.6f;
        }

        //MainSite に対応する根源は無いため、攻略度による弱体化は行わない
        GameObject raider = Instantiate(enemy, pos, Quaternion.identity);
        raider.GetComponent<Slime>().SetGoal(center);

        IDamageable damageable = raider.GetComponent<IDamageable>();
        if (damageable != null && kept.Hp > Raider.FullHp && kept.Hp < damageable.CurrentHp)
        {
            damageable.TakeDamage(damageable.CurrentHp - kept.Hp);
        }

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
