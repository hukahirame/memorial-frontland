using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    /// <summary>アイテムの原簿。所有者はここ1つ</summary>
    public static readonly ItemCatalog Items = new ItemCatalog();

    public TextAsset itemdata;

    [SerializeField] Transform maincanvas;
    [SerializeField] GameObject origin_player;
    private static bool GM_singleton = false;
    private static bool P_singleton = false;
    public static string entered_scene = "MainSite";

    /// <summary>所持金。所有者はここ1つ（QuestManager.Quests と同じ形）</summary>
    public static readonly Wallet Coins = new Wallet();

    // シーンの CoinText が 1000 で始まっていたので、その値を引き継ぐ
    [SerializeField] private int startingCoins = 1000;

    void Awake()
    {
        if (GM_singleton == false)
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(maincanvas);
            GM_singleton = true;

            Coins.SetAmount(startingCoins);
            maincanvas.Find("Status_Gold").Find("CoinText")
                      .GetComponent<TextMeshProUGUI>().text = Coins.Amount.ToString();
        }
        else
        {
            Destroy(maincanvas.gameObject);
            Destroy(gameObject);
        }

        if(P_singleton == false) { origin_player.SetActive(true); P_singleton = true; }

        Items.Load(itemdata.text);
    }

    public static void SceneTrans(string target)
    {
        GameObject.FindWithTag("SceneFinisher").GetComponent<SceneFinisher>().SceneFinish();
        entered_scene = target;
        SceneManager.LoadScene(target);
    }
}
