using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 作業台の画面。品を選ぶと説明と材料（要る数と持っている数）を出し、作る・買うのボタンを並べる。
    /// 作る・買うそのものは Workshop が行う
    /// </summary>
    public sealed class CraftMenu : MonoBehaviour
    {
        /// <summary>材料の欄の数</summary>
        private const int MaterialSlots = 4;

        /// <summary>材料の欄 1 つあたりの、材料の並びの子の数</summary>
        private const int WidgetsPerSlot = 5;

        /// <summary>作ると買うが並ぶとき、真ん中からそれぞれをずらす幅</summary>
        private const float ButtonGap = 150f;

        [SerializeField, FormerlySerializedAs("pi")] private PlayerInventory _inventory;
        [SerializeField, FormerlySerializedAs("items")] private ItemBook _items;
        [SerializeField] private Workshop _workshop;

        [Tooltip("Assets/_Project/Data/Recipes/ の RecipeDefinition をすべて割り当てる")]
        [SerializeField, FormerlySerializedAs("recipeDefinitions")] private RecipeDefinition[] _recipeDefinitions;

        [Tooltip("選んだ品の説明の枠")]
        [SerializeField] private CraftItemCard _card;

        [SerializeField, FormerlySerializedAs("material1")] private Text _material1;
        [SerializeField, FormerlySerializedAs("demand1")] private Text _demand1;
        [SerializeField, FormerlySerializedAs("Image1")] private Image _icon1;
        [SerializeField, FormerlySerializedAs("supply1")] private Text _supply1;

        [SerializeField, FormerlySerializedAs("material2")] private Text _material2;
        [SerializeField, FormerlySerializedAs("demand2")] private Text _demand2;
        [SerializeField, FormerlySerializedAs("Image2")] private Image _icon2;
        [SerializeField, FormerlySerializedAs("supply2")] private Text _supply2;

        [SerializeField, FormerlySerializedAs("material3")] private Text _material3;
        [SerializeField, FormerlySerializedAs("demand3")] private Text _demand3;
        [SerializeField, FormerlySerializedAs("Image3")] private Image _icon3;
        [SerializeField, FormerlySerializedAs("supply3")] private Text _supply3;

        [SerializeField, FormerlySerializedAs("material4")] private Text _material4;
        [SerializeField, FormerlySerializedAs("demand4")] private Text _demand4;
        [SerializeField, FormerlySerializedAs("Image4")] private Image _icon4;
        [SerializeField, FormerlySerializedAs("supply4")] private Text _supply4;

        [Tooltip("材料の欄の部品を並べた親。欄ごとに WidgetsPerSlot 個ずつ並ぶ")]
        [SerializeField, FormerlySerializedAs("matset")] private Transform _materialWidgets;

        [Tooltip("作るボタン。レシピのある品を開いたときだけ出す")]
        [SerializeField, FormerlySerializedAs("makeButton")] private RectTransform _makeButton;

        [Tooltip("買うボタン。原簿に値段のある品を開いたときだけ出す")]
        [SerializeField, FormerlySerializedAs("buyButton")] private RectTransform _buyButton;

        [SerializeField, FormerlySerializedAs("buyLabel")] private Text _buyLabel;

        private readonly Dictionary<string, Recipe> _recipes = new Dictionary<string, Recipe>();

        /// <summary>開いている品。何も開いていなければ null</summary>
        private ItemDefinition _shown;

        /// <summary>開いている品のレシピ。作れない品なら null</summary>
        private Recipe _current;

        private Text[] _materialLabels;
        private Text[] _demandLabels;
        private Image[] _icons;
        private Text[] _supplyLabels;

        private void Awake()
        {
            _materialLabels = new[] { _material1, _material2, _material3, _material4 };
            _demandLabels = new[] { _demand1, _demand2, _demand3, _demand4 };
            _icons = new[] { _icon1, _icon2, _icon3, _icon4 };
            _supplyLabels = new[] { _supply1, _supply2, _supply3, _supply4 };

            if (_recipeDefinitions == null) return;
            foreach (RecipeDefinition definition in _recipeDefinitions)
            {
                if (definition == null) continue;

                Recipe recipe = definition.ToDomain();
                _recipes[recipe.ProductId] = recipe;
            }
        }

        private void OnEnable()
        {
            //持ち物が変わるたびに所持数を書き直す。開いたまま拾っても、閉じて開き直しても今の数が出る
            _inventory.Changed += Refresh;
        }

        private void OnDisable()
        {
            _inventory.Changed -= Refresh;
        }

        private void Start()
        {
            //品を選ぶまで、説明と材料の枠と作る・買うのボタンを出さない。選ぶ前は空の枠だけが並ぶ
            _card.gameObject.SetActive(false);
        }

        /// <summary>品を選んだ。品のボタン（CraftButton）から呼ぶ</summary>
        public void Show(string itemId)
        {
            _shown = _items.Items.Find(itemId);
            _recipes.TryGetValue(itemId, out _current);

            _card.gameObject.SetActive(true);
            _card.Show(_shown);
            ShowMaterials();
            ShowButtons();
        }

        /// <summary>作るボタンから呼ぶ</summary>
        public void Make()
        {
            if (_current != null) _workshop.Make(_current);
        }

        /// <summary>買うボタンから呼ぶ</summary>
        public void Buy()
        {
            if (_shown != null) _workshop.Buy(_shown);
        }

        private void Refresh()
        {
            if (_shown != null) ShowMaterials();
        }

        private void ShowMaterials()
        {
            int count = _current == null ? 0 : _current.Ingredients.Count;
            Inventory inventory = _inventory.Inventory;

            for (int slot = 0; slot < MaterialSlots; slot++)
            {
                bool used = slot < count;
                SetSlotActive(slot, used);
                if (!used) continue;

                Ingredient ingredient = _current.Ingredients[slot];
                _materialLabels[slot].text = ingredient.DisplayName;
                _demandLabels[slot].text = ingredient.Amount.ToString();
                _icons[slot].sprite = Resources.Load<Sprite>(ingredient.ItemId);
                _supplyLabels[slot].text = inventory.CountOf(ingredient.ItemId).ToString();
            }
        }

        /// <summary>作れる品には作るボタン、売っている品には買うボタンを出す。片方だけなら真ん中に寄せる</summary>
        private void ShowButtons()
        {
            bool make = _current != null;
            bool buy = _shown != null && _shown.Price > 0;

            _makeButton.gameObject.SetActive(make);
            _buyButton.gameObject.SetActive(buy);
            if (buy) _buyLabel.text = _shown.Price + " コインで買う";

            float apart = make && buy ? ButtonGap : 0f;
            _makeButton.anchoredPosition = new Vector2(-apart, _makeButton.anchoredPosition.y);
            _buyButton.anchoredPosition = new Vector2(apart, _buyButton.anchoredPosition.y);
        }

        private void SetSlotActive(int slot, bool active)
        {
            for (int i = slot * WidgetsPerSlot; i < (slot + 1) * WidgetsPerSlot; i++)
            {
                if (i >= _materialWidgets.childCount) return;
                _materialWidgets.GetChild(i).gameObject.SetActive(active);
            }
        }
    }
}
