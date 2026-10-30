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

    void Awake()
    {
        Coins.SetAmount(startingCoins);
        maincanvas.Find("Status_Gold").Find("CoinText")
                  .GetComponent<TextMeshProUGUI>().text = Coins.Amount.ToString();

        Items.Load(itemdata.text);
    }

    void Start()
    {
        travel.Begin(saveSystem.SavedAreaId);
    }
}
