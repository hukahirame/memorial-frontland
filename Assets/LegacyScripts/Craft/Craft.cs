using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;
using MemorialFloor.Game;

public class Craft : MonoBehaviour//Craftchildから実行。対応するsetを出す
{
    private const int SlotCount = 4;        // UI が持つ材料スロット数
    private const int WidgetsPerSlot = 5;   // 材料1スロットあたりの子オブジェクト数

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

    [SerializeField] private GameObject dropcapsule;

    private readonly Dictionary<string, Recipe> recipes = new Dictionary<string, Recipe>();
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
        if (recipeDefinitions == null) return;
        foreach (var def in recipeDefinitions)
        {
            if (def == null) continue;
            var recipe = def.ToDomain();
            recipes[recipe.ProductId] = recipe;
        }
    }

    public void Craftprepare2(string showname)
    {
        set.GetComponent<Craft_set>().Put_Info(showname);
        Put_Materials(showname);
    }

    public void CraftStart()
    {
        if (current == null) return;

        if (!current.CanCraftWith(CurrentInventory()))
        {
            Debug.Log("材料が足りません!!");
            return;
        }

        foreach (var ingredient in current.Ingredients)
            for (int i = 0; i < ingredient.Amount; i++) pi.UnloadInventory(ingredient.ItemId);

        GameObject drop = Instantiate(dropcapsule,
            GameObject.FindWithTag("CraftBench").transform.position + new Vector3(0, 0.5f, 0),
            Quaternion.identity);
        drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(current.ProductId);

        Put_Materials(current.ProductId);
    }

    private void Put_Materials(string productId) // 完成品の材料に関する情報をアップロード
    {
        if (!recipes.TryGetValue(productId, out var recipe)) return;
        current = recipe;

        var inventory = CurrentInventory();

        for (int slot = 0; slot < SlotCount; slot++)
        {
            bool used = slot < recipe.Ingredients.Count;
            SetSlotActive(slot, used);
            if (!used) continue;

            var ingredient = recipe.Ingredients[slot];
            materialTexts[slot].text = ingredient.DisplayName;
            demandTexts[slot].text = ingredient.Amount.ToString();
            icons[slot].sprite = Resources.Load<Sprite>(ingredient.ItemId);
            supplyTexts[slot].text = inventory.CountOf(ingredient.ItemId).ToString();
        }
    }

    private void SetSlotActive(int slot, bool active)
    {
        if (slot == 0) return; // 先頭スロットは常時表示

        for (int d = slot * WidgetsPerSlot; d < (slot + 1) * WidgetsPerSlot; d++)
        {
            if (d >= matset.childCount) return;
            matset.GetChild(d).gameObject.SetActive(active);
        }
    }

    // PlayerInventory はロード時にリスト参照ごと差し替わるため、都度生成する
    private Inventory CurrentInventory() => new Inventory(pi.items, pi.stocks, pi.maxstocks);
}