using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static List<string[]> items = new List<string[]>();

    public TextAsset itemdata;

    [SerializeField] Transform maincanvas;
    [SerializeField] GameObject origin_player;
    private static bool GM_singleton = false;
    private static bool P_singleton = false;
    public static string entered_scene = "MainSite";

    public static TextMeshProUGUI coin;

    void Awake()
    {
        if (GM_singleton == false)
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(maincanvas);
            GM_singleton = true;
        }
        else
        {
            Destroy(maincanvas.gameObject);
            Destroy(gameObject);
        }

        if(P_singleton == false) { origin_player.SetActive(true); P_singleton = true; }

        coin = maincanvas.Find("Status_Gold").Find("CoinText").GetComponent<TextMeshProUGUI>();

        StringReader reader = new StringReader(itemdata.text); // TextAssetをStringReaderに変換
        while (reader.Peek() != -1)
        {
            string line = reader.ReadLine(); // 1行ずつ読み込む
            items.Add(line.Split(',')); // itemsリストに追加する
        }
        for (int i = 0; i < items.Count; i++) // itemsリストの条件を満たす値の数（全て）
        {
        /*    Debug.Log("システム内名称：" + items[i][0] +
                "　名称：" + items[i][1] +
                    "　最大ストック数：" + items[i][2] +
                      "　最大耐久値：" + items[i][3] +
                      "　分類：" + items[i][4] // 素材、装備品、設備など ※設置可などの判断基準にするべき(bool型要素)
                      );*/
        }
    }

    public static void SceneTrans(string target)
    {
        GameObject.FindWithTag("SceneFinisher").GetComponent<SceneFinisher>().SceneFinish();
        entered_scene = target;
        SceneManager.LoadScene(target);
    }
}
