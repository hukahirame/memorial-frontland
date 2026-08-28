using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class SceneStarter : MonoBehaviour //シーン起動時の処理全般。ODLに含まず、〇〇STとして設置
{
    [SerializeField] private string type; //役割
    [SerializeField] private Transform playerspowner;
    [SerializeField] public Vector4 exposition;

   // public static List<string[]> saveobjects = new List<string[]>(); //名称、対応シーン、x,y,z
    private void Start()
    {
        Transform pt = GameObject.FindWithTag("Player").transform;
        Player2 p = pt.GetComponent<Player2>();
        p.expos = exposition;

        Player2.playerrb = pt.GetComponent<Rigidbody>();
        Player2.playerhp = GameObject.FindWithTag("HpSlider").GetComponent<Slider>();

        SaveData sd = GameObject.Find("GameManager").GetComponent<SaveSystem>().savedata;
        for (int i = 0; i < sd.saveObjects.Count; i++)
        {
            if (sd.saveObjectsScene[i] == GameManager.entered_scene)
            {
                var obj = Instantiate((GameObject)Resources.Load(sd.saveObjects[i]), sd.saveObjectsPos[i], Quaternion.identity);
                obj.name = obj.name.Replace("(Clone)", "");
            }
        }

        if (type == "Root")
        {
            SpawnerDecide();
            JumpReset(p);
            GameObject.Find("GameManager").GetComponent<AudioSource>().clip = (AudioClip)Resources.Load("solarisnoame");
            GameObject.Find("GameManager").GetComponent<AudioSource>().Play();
            GameObject.FindWithTag("QuestManager").GetComponent<QuestManager>().StartQuest();
            pt.transform.position = playerspowner.position;
        }
        else if (type == "MainSite")
        {
            if(Random.Range(0,100) < 50) GameObject.Find("GameManager").GetComponent<AudioSource>().clip = (AudioClip)Resources.Load("music.dream");
            else GameObject.Find("GameManager").GetComponent<AudioSource>().clip = (AudioClip)Resources.Load("雨雫 @ フリーBGM DOVA-SYNDROME OFFICIAL YouTube CHANNEL");
            GameObject.Find("GameManager").GetComponent<AudioSource>().Play();

            if (RewardUI.rewardUI_show != 0) //RewardUIの唯一のスタートポイント
            {
                Transform mc = GameObject.FindWithTag("MainCanvas").transform;
                mc.Find("RewardUI").transform.position = mc.Find("CenterPoint").position;
                mc.Find("RewardUI").GetComponent<RewardUI>().InputInfo();
            }

            if (pt.position != new Vector3(-2, 0.65f, -2))
            {
                pt.position = playerspowner.position;
            }
            else
            {
                pt.position = new Vector3(-2, 0.65f, -2);
            }
        }
    }

    private void SpawnerDecide()
    {
        Root root = RootsManager.Roots.Find(GameManager.entered_scene);
        if (root == null) return;

        if (!root.HasSpawnPoint)
        {
            var candidates = GameObject.FindGameObjectsWithTag("SpawnerCandidate");
            var candidate = candidates[Random.Range(0, candidates.Length)];
            candidate.AddComponent<OF_Spawner>();
            Vector3 targetpos = candidate.transform.position;
            root.PlaceSpawnPoint(targetpos.x, targetpos.y, targetpos.z);
        }
    }

    private void JumpReset(Player2 p)
    {
        Button jumpbutton = GameObject.Find("Jumpbutton").GetComponent<Button>();
        jumpbutton.onClick.RemoveAllListeners();
        jumpbutton.onClick.AddListener(p.Jump);
    }
}
