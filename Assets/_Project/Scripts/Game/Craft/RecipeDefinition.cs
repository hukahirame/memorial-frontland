using System;
using System.Collections.Generic;
using UnityEngine;
using MemorialFloor.Domain;

namespace MemorialFloor.Game
{
    /// <summary>レシピが要求する材料1種。Inspector で編集する。</summary>
    [Serializable]
    public class IngredientEntry
    {
        [Tooltip("システム内名称。インベントリのアイテムIDと一致させる")]
        public string itemId;

        [Tooltip("UI に出す表示名")]
        public string displayName;

        [Min(1)]
        public int amount = 1;
    }

    /// <summary>
    /// クラフトのレシピ定義。Project ウィンドウの Create メニューから作る。
    /// ルールは持たない。Domain の Recipe に変換して判定させる。
    /// 実行中に書き換えないこと（ScriptableObject への変更はエディタ上で永続化される）。
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe", menuName = "MemorialFloor/Recipe")]
    public class RecipeDefinition : ScriptableObject
    {
        [Tooltip("完成品のシステム内名称")]
        public string productId;

        public List<IngredientEntry> ingredients = new List<IngredientEntry>();

        /// <summary>Domain 側の型へ変換する。空欄や個数0の行は無視する。</summary>
        public Recipe ToDomain()
        {
            var list = new List<Ingredient>();
            foreach (var e in ingredients)
            {
                if (e == null || string.IsNullOrEmpty(e.itemId) || e.amount <= 0) continue;
                list.Add(new Ingredient(e.itemId, e.displayName, e.amount));
            }
            return new Recipe(productId, list);
        }
    }
}