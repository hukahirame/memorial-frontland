using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OF_Spawner : MonoBehaviour
{
    public float spawnrange; // 最大範囲
    public GameObject seeker;
    public GameObject enemy;

    private int n;

    private Seeker seeker_cs;

    public static bool spawnable = false;
    public static int spawnerhp = 100;

    public Vector4 expos; //XL,XS,ZL,ZS
    private int index = -1;

 //   private GameObject tower1;
 //   private GameObject tower1_down;

    public List<string> loadEnemiesName = new List<string>();
    public List<Vector3> loadEnemiesPos = new List<Vector3>();

    void Start()
    {
        //大部分はSceneStarterから実行

        gameObject.tag = "MainSpawner";
        spawnrange = Random.Range(6f, 12f);
        expos = FindObjectOfType<SceneStarter>().exposition;
        index = RootsManager.roots.FindIndex(s => s[2] == GameManager.entered_scene);

        seeker = GameObject.Find("Seeker");
        seeker.transform.parent = transform;
        seeker_cs = seeker.GetComponent<Seeker>();
        //この後seekerが自分で適正位置まで下がる
        enemy = (GameObject)Resources.Load("Slime");

        SaveData sd = GameObject.Find("GameManager").GetComponent<SaveSystem>().savedata;
        loadEnemiesName = sd.enemies;
        loadEnemiesPos = sd.respawns;
        if (loadEnemiesName.Count > 0)
        {
            for (int i = 0; i < loadEnemiesName.Count; i++)
            {
                Spawn2(loadEnemiesPos[i]);
            }
            loadEnemiesName.Clear();
            loadEnemiesPos.Clear();
        }
        else
        {
            spawnrange = spawnrange * 2;
            Spawn();
            Spawn();
            spawnrange = spawnrange / 2;
        }

    //    tower1 = GameObject.Find("LOD_Wall_Stone_x1");
    //    tower1_down = GameObject.Find("LOD_Wall_Stone_x1(down)");
    //    tower1_down.SetActive(false);
    }

    public void Spawn()
    {
        seeker.SetActive(true);
        seeker_cs.SeekPosition(spawnrange,expos);
    }

    public void Spawn2(Vector3 pos)
    {
        Debug.Log("Spawn");
        Invoke("Spawn", Random.Range(15, 30));
        if ((RootsManager.parameta[index][0] >= 50) && (Random.Range(0, 100) > 50)) return;
        if (Random.Range(0, 100) < 3 * n) return;

        var obj = Instantiate(enemy, pos, Quaternion.identity);
        seeker.gameObject.SetActive(false);
        if (RootsManager.parameta[index][0] >= 30)
        {
            var s = obj.transform.Find("Canvas").Find("Slider").GetComponent<Slider>();
            s.value -= s.value * 0.2f;
        }
        n++;
    }

    public void Stampede(int amount)
    {
        for (int i = 0; i < amount; i++) 
        {
            spawnrange = spawnrange * 2;
            Spawn();
            spawnrange = spawnrange / 2;
        }
        Vector3 stoppos = GameObject.Find("ST_MainSite").transform.position;
        Spawn2(stoppos + new Vector3(Random.Range(1f,6f),0,Random.Range(-5f,1f)));
        Spawn2(stoppos + new Vector3(Random.Range(1f, 6f), 0, Random.Range(-5f, 1f)));
     //   tower1.SetActive(false);
     //   tower1_down.SetActive(true);
    }


    public void SpawnerBreak(int damage)
    {
        spawnerhp -= damage;
        if(spawnerhp <= 0)
        {
            GameObject.Find("GameManager").GetComponent<AudioSource>().clip =(AudioClip) Resources.Load("dark city");
            GameObject.Find("GameManager").GetComponent<AudioSource>().Play();
            Stampede(Random.Range(4,7));
            GameObject.FindWithTag("QuestManager").GetComponent<QuestManager>().SyncQuest("MainSpawner");
            var colliders = GetComponents<Collider>();
            foreach (Collider collider in colliders) { collider.enabled = false; }
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("敵が大量発生しています");
        }
    }
}
