using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class RootsManager : MonoBehaviour
{
    /// <summary>根源の唯一の置き場。追加・検索・日次更新はここを通す</summary>
    public static readonly RootRegistry Roots = new RootRegistry();

    /// <summary>拠点へ向かっている敵。氾濫で積み、拠点の MS_Spawner が出す</summary>
    public static readonly BaseRaid Raid = new BaseRaid();

    [SerializeField] private GameObject rootset;

    void Start()
    {
        RootCreate("はじまりの根源", "01010101", "Root1", 0f, -100f, 1000);
        RootCreate("水の根源", "02020202", "Root2", 200f, -100f, 2000);
    }

    public void RootCreate(string name, string seed, string id, float uiX, float uiY, int danger) //3日前後で1回実行
    {
        //既に同じ ID があれば何もしない
        Roots.TryAdd(new Root(id, name, seed, danger, uiX, uiY));
    }

    public void RootUIShow() //RootButton(UI)を全て生成
    {
        foreach (var root in Roots.All) //既存UIを全削除
        {
            var existing = transform.Find(root.Id);
            if (existing != null) Destroy(existing.gameObject);
        }

        foreach (var root in Roots.All)
        {
            Vector3 rootUIpos = new Vector3(root.UiX, root.UiY, 0);
            GameObject rootUI = Instantiate(rootset, transform.position + rootUIpos, Quaternion.identity, transform);
            rootUI.name = root.Id;
            var str = rootUI.transform.Find("Text").GetComponent<Text>();
            str.text = "危険度 " + root.Danger + "　攻略度 " + root.Progress + "％";
        }
    }

    public void RootDecode(string seed) //ロード時、seedから地形を復元
    {
        // ST → Allmaity → これ
    }

    void RootTrick(string seed) //根源変化
    {

    }

    public void Dayover() //夜12時
    {
        foreach (var root in Roots.All)
        {
            if (Random.Range(1, 3) >= 2) RootTrick(root.Seed);
        }

        List<string> news = new List<string>();

        //氾濫判定は蓄積値の増加直後、攻略度の減少より前に行う（元の順序）
        foreach (var root in Roots.All)
        {
            root.AccumulateDaily();

            //どこに居ても拠点へ向かわせる。拠点に居れば MS_Spawner が少しずつ、留守なら戻ったときにまとめて出す
            int raiders = root.Overflow();
            if (raiders == 0) continue;

            Raid.Add(raiders);
            news.Add(root.Name + "が氾濫し、拠点へ敵が向かっています");
        }

        foreach (var root in Roots.All) root.DecayProgressDaily();

        //予兆は攻略度を減らしたあとで見る。次の零時に増える量は、そのときの攻略度で決まる
        foreach (var root in Roots.All)
        {
            if (root.WillOverflowNextDay) news.Add(root.Name + "が次の零時に氾濫しそうです");
        }

        if (news.Count > 0) StartCoroutine(Announce(news));
    }

    /// <summary>MiddleText は後の知らせで前の知らせを消すので、1件ずつ出し終えてから次を出す</summary>
    private IEnumerator Announce(List<string> news)
    {
        GameObject found = GameObject.Find("MiddleText");
        if (found == null) yield break;

        MiddleText middle = found.GetComponent<MiddleText>();
        foreach (string item in news)
        {
            middle.Midtxt_Anim(item);
            yield return new WaitForSeconds(middle.waittime + 2f); //入りに 1 秒、出に約 1 秒
        }
    }
}
