using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;
using MemorialFloor.Game;

public class RootsManager : MonoBehaviour
{
    /// <summary>根源の唯一の置き場。追加・検索・日次更新はここを通す</summary>
    public static readonly RootRegistry Roots = new RootRegistry();

    /// <summary>拠点に攻め込んだ敵の控え。氾濫で足し、拠点の MS_Spawner が出す</summary>
    public static readonly BaseRaid Raid = new BaseRaid();

    [SerializeField] private GameObject rootset;

    /// <summary>地図の札の 2 行目（危険度と攻略度）の字の大きさ。1 行目の名前は prefab の大きさで出す</summary>
    private const int StatsFontSize = 22;

    [Tooltip("地図に出す、拠点へ戻る近道のボタン")]
    [SerializeField] private HomeShortcut home;

    //始めのクエストと保存の戻しが根源を引くので、どの Start よりも先に作る
    void Awake()
    {
        RootCreate("はじまりの根源", "01010101", "Root1", -80f, -300f, 1000);
        RootCreate("水の根源", "02020202", "Root2", 420f, -300f, 2000);
        RootCreate("沈んだ街の根源", "03030303", "Root3", -420f, 60f, 3000, 2, 5);
    }

    /// <summary>根源の Id を作った順に。場所は拠点のあとにこの順で一列に並ぶ</summary>
    public static List<string> RootIds()
    {
        List<string> ids = new List<string>();
        foreach (Root root in Roots.All) ids.Add(root.Id);

        return ids;
    }

    /// <summary>gainLow から gainHigh は、攻略度 0 のときに日ごとに増える蓄積値の幅</summary>
    public void RootCreate(string name, string seed, string id, float uiX, float uiY, int danger,
        int gainLow = Root.DailyGainLow, int gainHigh = Root.DailyGainHigh) //3日前後で1回実行
    {
        //既に同じ ID があれば何もしない
        Roots.TryAdd(new Root(id, name, seed, danger, uiX, uiY, gainLow, gainHigh));
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
            //札は地図の中の座標で置く。画面の画素で置くと、画面が大きいほど札どうしが近づいて重なる
            GameObject rootUI = Instantiate(rootset, transform);
            ((RectTransform)rootUI.transform).anchoredPosition = new Vector2(root.UiX, root.UiY);
            rootUI.name = root.Id;
            var str = rootUI.transform.Find("Text").GetComponent<Text>();
            str.text = root.Name + "\n<size=" + StatsFontSize + "><color=#F2E27A>危険度 " + root.Danger + "　攻略度 " + root.Progress + "％</color></size>";
        }

        if (home != null) home.Show();
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
            root.AccumulateDaily(UnityRandom.Shared);

            //どこに居ても拠点に攻め込ませる。拠点に居れば MS_Spawner がすぐ、留守なら戻ったときに出す
            int raiders = root.Overflow();
            if (raiders == 0) continue;

            Raid.Invade(raiders);
            news.Add(root.Name + "が氾濫し、拠点に敵が攻め込みました");
            if (QuestManager.IssueBreach(root)) news.Add(root.Name + "に決壊クエストが出ました");
        }

        foreach (var root in Roots.All) root.DecayProgressDaily();

        //予兆は攻略度を減らしたあとで見る。次の零時に増える量は、そのときの攻略度で決まる
        foreach (var root in Roots.All)
        {
            if (root.MayOverflowNextDay) news.Add(root.Name + "が次の零時に氾濫しそうです");
        }

        if (news.Count > 0) Announce(news);
    }

    /// <summary>知らせを並べて出す。中くらいの文字が 1 件ずつ順に出す</summary>
    private void Announce(List<string> news)
    {
        GameObject found = GameObject.Find("MiddleText");
        if (found == null) return;

        MiddleText middle = found.GetComponent<MiddleText>();
        foreach (string item in news) middle.Midtxt_Anim(item);
    }
}
