using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RootsManager : MonoBehaviour
{
    //private int CHILDPLUS = 2;

    public static List<string[]> roots = new List<string[]>(); // 根源名、シード値、ID
    public static List<float[]> pos = new List<float[]>(); // スポナーX、スポナーY、スポナーZ、RM上のX、RM上のY
    public static List<int[]> parameta = new List<int[]>(); // 攻略度、危険度、蓄積値

    [SerializeField] private FieldCreator creator;
    [SerializeField] private GameObject rootset;
    [SerializeField] private Text power;
    [SerializeField] private Text capacity; //この辺はRMから変更
    [SerializeField] private GameObject slime;

    private int d;

    void Start()
    {
        RootCreate("はじまりの根源","01010101","Root1", new float[] { -100, -100, -100, 0, -100 }, new int[] { 0, 1000, 0 });
        RootCreate("水の根源", "02020202", "Root2", new float[] { -100, -100, -100, 200, -100 }, new int[] { 0, 2000, 0 });

    }

    public void RootCreate(string name, string seed, string id, float[] t, int[] p) //3日前後で1回実行
    {
        var fss = creator.SeedCreate(); // fss.seed, fss.spownerpos.y, fss.uipos.xなど
        roots.Add(new string[] { name, seed, id });
        //pos.Add(new float[] { fss.spownerpos.x, fss.spownerpos.y, fss.spownerpos.z, fss.uipos.x, fss.uipos.y });
        pos.Add(t);
        parameta.Add(p);
        //RootUIShow();
    }

    public void RootUIShow() //RootButton(UI)を全て生成
    {
        foreach(var ui in roots) //既存UIを全削除
        {
            if (transform.Find(ui[2]) != null) Destroy(transform.Find(ui[2]));
        }
        for (int i = 0; i < roots.Count; i++)
        {
            /*if (transform.Find(roots[i][2]) != null)
            {
                 return;
            }*/
            Vector3 rootUIpos = new Vector3(pos[i][3], pos[i][4], 0);
            GameObject rootUI = Instantiate(rootset, transform.position + rootUIpos, Quaternion.identity, transform);
            rootUI.name = roots[i][2];
            var str = rootUI.transform.Find("Text").GetComponent<Text>();
            str.text = "危険度 " + parameta[i][1] + "　攻略度 " + parameta[i][0] + "％";
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
        d++;

        for (int i = 0; i < roots.Count; i++)
        {
            if (Random.Range(1, 3) >= 2) RootTrick(roots[i][1]);
        }

        for (int i = 0; i < roots.Count; i++) // 蓄積値増加
        {
            parameta[i][2] += 10;
            StampedeJudge(i);
        }

        for(int i = 0;i < roots.Count; i++) //攻略度減少
        {
            parameta[i][0] -= 3;
            if (parameta[i][0] < 0) parameta[i][0] = 0;
        }
    }

    public void StampedeJudge(int i) // 蓄積値更新時
    {
        if (parameta[i][2] >= 100) //【氾濫】
        {
            Transform ms = GameObject.FindWithTag("MainSpawner").transform;

            for (int f = 0; f < 5; f++)
            {
                if (GameManager.entered_scene == "MainSite") ms.GetComponent<MS_Spawner>().Spawn();
                else                                         ms.GetComponent<OF_Spawner>().Spawn();
            }
            capacity.text = "-";
            capacity.color = Color.black;
        }
        else if (parameta[i][2] >= 75) { capacity.text = "高"; capacity.color = Color.red; }
        else if (parameta[i][2] >= 40) { capacity.text = "中"; capacity.color = Color.yellow; }
        else if (parameta[i][2] >= 15) { capacity.text = "小"; capacity.color = Color.green; }
        else if (parameta[i][2] >= 0) { capacity.text = "微"; capacity.color = Color.blue; }
    }
}
