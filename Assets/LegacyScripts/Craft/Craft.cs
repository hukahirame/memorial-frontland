using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;
using MemorialFloor.Game;

public class Craft : MonoBehaviour//Craftchildから実行。対応するsetを出す
{
    private const int SlotCount = 4;        // UI が持つ材料スロット数
    private const int WidgetsPerSlot = 5;   // 材料1スロットあたりの子オブジェクト数
    private const float ButtonGap = 150f;   // 作ると買うが並ぶとき、真ん中からそれぞれをずらす幅

    public PlayerInventory pi;

    [Tooltip("Assets/_Project/Data/Recipes/ の RecipeDefinition をすべて割り当てる")]
    [SerializeField] private RecipeDefinition[] recipeDefinitions;

    public GameObject set;

    public Text material1;
    public Text demand1;
    public Image Image1;
    public Text supply1;

    public Text material2;
    public Text demand2;
    public Image Image2;
    public Text supply2;

    public Text material3;
    public Text demand3;
    public Image Image3;
    public Text supply3;

    public Text material4;
    public Text demand4;
    public Image Image4;
    public Text supply4;

    public Transform matset;

    [Tooltip("作るボタン。レシピのある品を開いたときだけ出す")]
    [SerializeField] private RectTransform makeButton;

    [Tooltip("買うボタン。原簿に値段のある品を開いたときだけ出す")]
    [SerializeField] private RectTransform buyButton;

    [SerializeField] private Text buyLabel;

    [SerializeField] private GameObject dropcapsule;

    private readonly Dictionary<string, Recipe> recipes = new Dictionary<string, Recipe>();

    /// <summary>開いている品。何も開いていなければ null</summary>
    private ItemDefinition shown;

    /// <summary>開いている品のレシピ。作れない品なら null</summary>
    private Recipe current;

    private Text[] materialTexts;
    private Text[] demandTexts;
    private Image[] icons;
    private Text[] supplyTexts;

    void Awake()
    {
        materialTexts = new[] { material1, material2, material3, material4 };
        demandTexts   = new[] { demand1,   demand2,   demand3,   demand4 };
        icons         = new[] { Image1,    Image2,    Image3,    Image4 };
        supplyTexts   = new[] { supply1,   supply2,   supply3,   supply4 };
    }

    void Start()
    {
        //品を選ぶまで、説明と材料の枠と作る・買うのボタンを出さない。選ぶ前は空の枠だけが並ぶ
        set.SetActive(false);

        //持ち物が変わるたびに所持数を書き直す。開いたまま拾っても、閉じて開き直しても今の数が出る
        pi.Changed += Refresh;

        if (recipeDefinitions == null) return;
        foreach (var def in recipeDefinitions)
        {
            if (def == null) continue;
            var recipe = def.ToDomain();
            recipes[recipe.ProductId] = recipe;
        }
    }

    void OnDestroy()
    {
        if (pi != null) pi.Changed -= Refresh;
    }

    public void Craftprepare2(string showname)
    {
        set.SetActive(true);
        set.GetComponent<Craft_set>().Put_Info(showname);
        shown = GameManager.Items.Find(showname);
        recipes.TryGetValue(showname, out current);

        Put_Materials();
        Put_Buttons();
    }

    private void Refresh()
    {
        if (shown != null) Put_Materials();
    }

    public void CraftStart()
    {
        if (current == null) return;

        //倒れている間と場所を移っている間は作らない。移る途中に作ると、品は前の場所と一緒に消えて材料だけ減る
        if (!GameObject.FindWithTag("Player").GetComponent<Player2>().CanAct) return;

        if (!current.CanCraftWith(CurrentInventory()))
        {
            Debug.Log("材料が足りません!!");
            return;
        }

        //完成品は作業台に落とす。画面は離れても閉じないので、作業台の無い場所では作らない。材料を減らしてから探すと材料だけ消える
        GameObject bench = GameObject.FindWithTag("CraftBench");
        if (bench == null) return;

        foreach (var ingredient in current.Ingredients)
            for (int i = 0; i < ingredient.Amount; i++) pi.UnloadInventory(ingredient.ItemId);

        Drop(current.ProductId, bench);
        Put_Materials();
    }

    /// <summary>開いている品を所持金で買う。品は作ったときと同じく作業台に落ちる</summary>
    public void BuyStart()
    {
        if (shown == null || shown.Price <= 0) return;

        //作るときと同じく、倒れている間と移っている間、作業台の無い場所では買わない。払ってから探すと所持金だけ減る
        if (!GameObject.FindWithTag("Player").GetComponent<Player2>().CanAct) return;

        GameObject bench = GameObject.FindWithTag("CraftBench");
        if (bench == null) return;

        if (!GameManager.Coins.Spend(shown.Price))
        {
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("所持金が足りません");
            return;
        }

        Drop(shown.ItemId, bench);
    }

    private void Drop(string itemId, GameObject bench)
    {
        GameObject drop = Instantiate(dropcapsule,
            bench.transform.position + new Vector3(0, 0.5f, 0),
            Quaternion.identity);
        drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(itemId);
    }

    private void Put_Materials() // 完成品の材料に関する情報をアップロード
    {
        int count = current == null ? 0 : current.Ingredients.Count;
        var inventory = CurrentInventory();

        for (int slot = 0; slot < SlotCount; slot++)
        {
            bool used = slot < count;
            SetSlotActive(slot, used);
            if (!used) continue;

            var ingredient = current.Ingredients[slot];
            materialTexts[slot].text = ingredient.DisplayName;
            demandTexts[slot].text = ingredient.Amount.ToString();
            icons[slot].sprite = Resources.Load<Sprite>(ingredient.ItemId);
            supplyTexts[slot].text = inventory.CountOf(ingredient.ItemId).ToString();
        }
    }

    /// <summary>作れる品には作るボタン、売っている品には買うボタンを出す。片方だけなら真ん中に寄せる</summary>
    private void Put_Buttons()
    {
        bool make = current != null;
        bool buy = shown != null && shown.Price > 0;

        makeButton.gameObject.SetActive(make);
        buyButton.gameObject.SetActive(buy);
        if (buy) buyLabel.text = shown.Price + " コインで買う";

        float apart = make && buy ? ButtonGap : 0f;
        makeButton.anchoredPosition = new Vector2(-apart, makeButton.anchoredPosition.y);
        buyButton.anchoredPosition = new Vector2(apart, buyButton.anchoredPosition.y);
    }

    private void SetSlotActive(int slot, bool active)
    {
        for (int d = slot * WidgetsPerSlot; d < (slot + 1) * WidgetsPerSlot; d++)
        {
            if (d >= matset.childCount) return;
            matset.GetChild(d).gameObject.SetActive(active);
        }
    }

    private Inventory CurrentInventory() => pi.Inventory;
}