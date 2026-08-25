using System.Collections.Generic;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class RecipeTests
    {
        private Inventory _inventory;
        private List<string> _items;
        private List<int> _stocks;
        private List<int> _maxStocks;

        private Inventory CreateInventory(params (string id, int stock)[] slots)
        {
            _items = new List<string>();
            _stocks = new List<int>();
            _maxStocks = new List<int>();
            foreach (var s in slots)
            {
                _items.Add(s.id);
                _stocks.Add(s.stock);
                _maxStocks.Add(999);
            }
            _inventory = new Inventory(_items, _stocks, _maxStocks);
            return _inventory;
        }

        private static Recipe TorchRecipe()
            => new Recipe("Torch", new List<Ingredient> { new Ingredient("Branch", "木の枝", 4) });

        // --- CountOf ---

        [Test]
        public void 所持数は複数スロットにまたがって合計される()
        {
            var inv = CreateInventory(("Branch", 14), ("Ironsword", 1), ("Branch", 6));

            Assert.AreEqual(20, inv.CountOf("Branch"));
        }

        [Test]
        public void 所持していないアイテムの所持数はゼロ()
        {
            var inv = CreateInventory(("Branch", 14));

            Assert.AreEqual(0, inv.CountOf("Slimecore"));
        }

        // --- CanCraftWith ---

        [Test]
        public void 材料が足りていれば作れる()
        {
            var inv = CreateInventory(("Branch", 4));

            Assert.IsTrue(TorchRecipe().CanCraftWith(inv));
        }

        [Test]
        public void 材料が一つでも足りなければ作れない()
        {
            var inv = CreateInventory(("Branch", 3));

            Assert.IsFalse(TorchRecipe().CanCraftWith(inv));
        }

        [Test]
        public void 複数材料はすべて満たす必要がある()
        {
            var recipe = new Recipe("Legendsword", new List<Ingredient>
            {
                new Ingredient("Branch", "木の枝", 20),
                new Ingredient("Slimecore", "スライムコア", 2),
            });
            var inv = CreateInventory(("Branch", 20), ("Slimecore", 1));

            Assert.IsFalse(recipe.CanCraftWith(inv), "2つ目が足りない");
        }

        [Test]
        public void 分割された所持数の合計で判定される()
        {
            var recipe = TorchRecipe();
            var inv = CreateInventory(("Branch", 2), ("Slimecore", 5), ("Branch", 2));

            Assert.IsTrue(recipe.CanCraftWith(inv), "2+2 で 4 に届く");
        }

        [Test]
        public void 材料が無いレシピは常に作れる()
        {
            var recipe = new Recipe("Free", new List<Ingredient>());
            var inv = CreateInventory();

            Assert.IsTrue(recipe.CanCraftWith(inv));
        }
    }
}