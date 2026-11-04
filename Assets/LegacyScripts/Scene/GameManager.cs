using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    /// <summary>アイテムの原簿。所有者はここ1つ</summary>
    public static readonly ItemCatalog Items = new ItemCatalog();

    public TextAsset itemdata;

    [SerializeField] Transform maincanvas;
    [SerializeField] private AreaTravel travel;
    [SerializeField] private SaveSystem saveSystem;

    [Tooltip("遊ぶ画面の設定。「タイトルに戻る」を受ける")]
    [SerializeField] private SettingsMenu settings;

    /// <summary>所持金。所有者はここ1つ（QuestManager.Quests と同じ形）</summary>
    public static readonly Wallet Coins = new Wallet();

    // シーンの CoinText が 1000 で始まっていたので、その値を引き継ぐ
    [SerializeField] private int startingCoins = 1000;

    private TextMeshProUGUI coinText;

    void Awake()
    {
        coinText = maincanvas.Find("Status_Gold").Find("CoinText").GetComponent<TextMeshProUGUI>();
        Coins.Changed += ShowCoins;
        Coins.SetAmount(startingCoins);
        ShowCoins();

        Items.Load(itemdata.text);

        settings.TitleRequested += ToTitle;
    }

    void Start()
    {
        //保存があれば根源とクエストを戻し、無ければ始めの調査とサブを出す。着いたときにクエストが始まるので、着く前に済ませる
        if (!saveSystem.RestoreWorld()) QuestManager.IssueFirst();

        travel.Begin(saveSystem.SavedAreaId);
    }

    void OnDestroy()
    {
        Coins.Changed -= ShowCoins;
        settings.TitleRequested -= ToTitle;
    }

    /// <summary>
    /// 設定の「タイトルに戻る」。書いてよいときなら保存してから、常駐シーンごとタイトルに入れ替える。
    /// 書けないとき（移っている途中、倒れているあいだ）は、10 秒おきの保存の最後のものが残る。遊びの状態はタイトルが戻す
    /// </summary>
    private void ToTitle()
    {
        saveSystem.TrySave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(AreaTravel.TitleScene);
    }

    private void ShowCoins()
    {
        coinText.text = Coins.Amount.ToString();
    }
}
