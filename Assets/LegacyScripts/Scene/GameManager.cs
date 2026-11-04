using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    /// <summary>アイテムの原簿。所有者はここ1つ</summary>
    public static readonly ItemCatalog Items = new ItemCatalog();

    public TextAsset itemdata;

    [SerializeField] Transform maincanvas;
    [SerializeField] private AreaTravel travel;
    [SerializeField] private SaveSystem saveSystem;

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
    }

    private void ShowCoins()
    {
        coinText.text = Coins.Amount.ToString();
    }
}
