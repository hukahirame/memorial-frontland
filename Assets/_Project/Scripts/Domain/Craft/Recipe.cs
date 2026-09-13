using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>レシピが要求する材料1種。</summary>
    public readonly struct Ingredient
    {
        /// <summary>システム内名称。インベントリのアイテムIDと一致する</summary>
        public string ItemId { get; }
        /// <summary>UI に出す表示名</summary>
        public string DisplayName { get; }
        public int Amount { get; }

        public Ingredient(string itemId, string displayName, int amount)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Amount = amount;
        }
    }

    /// <summary>
    /// 完成品1種と、その材料。材料は0個以上（上限は設けない）。
    /// </summary>
    public sealed class Recipe
    {
        public string ProductId { get; }
        public IReadOnlyList<Ingredient> Ingredients { get; }

        public Recipe(string productId, IReadOnlyList<Ingredient> ingredients)
        {
            ProductId = productId;
            Ingredients = ingredients ?? new List<Ingredient>();
        }

        /// <summary>
        /// 手持ちで作れるか。材料が0個のレシピは常に作れる。
        /// </summary>
        public bool CanCraftWith(Inventory inventory)
        {
            for (int i = 0; i < Ingredients.Count; i++)
            {
                if (inventory.CountOf(Ingredients[i].ItemId) < Ingredients[i].Amount) return false;
            }
            return true;
        }
    }
}