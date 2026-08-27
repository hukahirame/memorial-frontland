using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class RootsManager : MonoBehaviour
{
    /// <summary>根源の唯一の置き場。追加・検索・日次更新はここを通す</summary>
    public static readonly RootRegistry Roots = new RootRegistry();

    [SerializeField] private GameObject rootset;
    [SerializeField] private Text capacity; //この辺はRMから変更

    void Start()
    {
        RootCreate("はじまりの根源", "01010101", "Root1", 0f, -100f, 1000);
        RootCreate("水の根源", "02020202", "Root2", 200f, -100f, 2000);
    }

    public void RootCreate(string name, string seed, string id, float uiX, float uiY, int danger) //3日前後で1回実行
    {
        //既に同じ ID があれば何もしない。MainSite に戻るたび Start が再実行されるため
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

        //氾濫判定は蓄積値の増加直後、攻略度の減少より前に行う（元の順序）
        foreach (var root in Roots.All)
        {
            root.AccumulateDaily();
            StampedeJudge(root);
        }

        foreach (var root in Roots.All) root.DecayProgressDaily();
    }

    public void StampedeJudge(Root root) // 蓄積値更新時
    {
        switch (root.Level)
        {
            case AccumulationLevel.Stampede: //【氾濫】
                Transform ms = GameObject.FindWithTag("MainSpawner").transform;

                for (int f = 0; f < 5; f++)
                {
                    if (GameManager.entered_scene == "MainSite") ms.GetComponent<MS_Spawner>().Spawn();
                    else                                         ms.GetComponent<OF_Spawner>().Spawn();
                }
                capacity.text = "-"; capacity.color = Color.black;
                break;

            case AccumulationLevel.High:    capacity.text = "高"; capacity.color = Color.red;    break;
            case AccumulationLevel.Medium:  capacity.text = "中"; capacity.color = Color.yellow; break;
            case AccumulationLevel.Small:   capacity.text = "小"; capacity.color = Color.green;  break;
            default:                        capacity.text = "微"; capacity.color = Color.blue;   break;
        }
    }
}
