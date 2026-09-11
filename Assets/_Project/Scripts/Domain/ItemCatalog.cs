using System.Collections.Generic;
using System.Globalization;

namespace MemorialFloor.Domain
{
    /// <summary>アイテムの使い道。情報枠のボタンを押したときに何が起きるか</summary>
    public enum ItemUse
    {
        /// <summary>足元に置く</summary>
        Place = 0,
        /// <summary>食べて体力を回復する</summary>
        Eat = 1,
        /// <summary>武器として装備する</summary>
        Equip = 2
    }

    /// <summary>アイテム1種の素性。原簿の1行に対応する</summary>
    public sealed class ItemDefinition
    {
        /// <summary>システム内名称。インベントリとレシピが使う ID</summary>
        public string ItemId { get; }

        /// <summary>UI に出す表示名</summary>
        public string Name { get; }

        /// <summary>1スロットに積める上限</summary>
        public int MaxStock { get; }

        /// <summary>耐久値の上限。0 は耐久の概念を持たない</summary>
        public int MaxDurability { get; }

        /// <summary>設置できるか。情報枠にボタンを出すかを決める</summary>
        public bool Installable { get; }

        /// <summary>装備したときの攻撃力。武器でなければ 0</summary>
        public int Attack { get; }

        /// <summary>当たった相手を吹き飛ばす強さ。0 なら飛ばさない</summary>
        public float Knockback { get; }

        /// <summary>使い道。ボタンの文言と押したときの動きが決まる</summary>
        public ItemUse Use { get; }

        /// <summary>食べたときに回復する体力。食べ物でなければ 0</summary>
        public int Heal { get; }

        public string Description { get; }

        public ItemDefinition(string itemId, string name, int maxStock,
                              int maxDurability, bool installable, int attack,
                              float knockback, ItemUse use, int heal, string description)
        {
            ItemId = itemId;
            Name = name;
            MaxStock = maxStock;
            MaxDurability = maxDurability;
            Installable = installable;
            Attack = attack;
            Knockback = knockback;
            Use = use;
            Heal = heal;
            Description = description;
        }
    }

    /// <summary>
    /// アイテムの原簿。CSV 1行が1種で、先頭行は見出しとして読み飛ばす。
    /// 列は 0:ID 1:名称 2:最大ストック 3:最大耐久値 4:設置可
    /// 5:攻撃力 6:吹き飛ばし 7:使い道 8:回復量 10:紹介文。
    /// </summary>
    public sealed class ItemCatalog
    {
        private const int Columns = 11;
        private const int ItemIdColumn = 0;
        private const int NameColumn = 1;
        private const int MaxStockColumn = 2;
        private const int MaxDurabilityColumn = 3;
        private const int InstallableColumn = 4;
        private const int AttackColumn = 5;
        private const int KnockbackColumn = 6;
        private const int UseColumn = 7;
        private const int HealColumn = 8;
        private const int DescriptionColumn = 10;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> All
        {
            get { return _items; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// 原簿を読む。列が足りない行と空行は捨てる。
        /// 数として読めない値は 0 にする
        /// </summary>
        public static ItemCatalog Parse(string csv)
        {
            ItemCatalog catalog = new ItemCatalog();
            catalog.Load(csv);

            return catalog;
        }

        /// <summary>読み直す。以前の中身は捨てる</summary>
        public void Load(string csv)
        {
            _items.Clear();
            if (string.IsNullOrEmpty(csv)) return;

            string[] lines = csv.Replace("\r", "").Split('\n');
            bool heading = true;

            foreach (string raw in lines)
            {
                string line = raw.Trim('\ufeff').Trim();
                if (line.Length == 0) continue;

                if (heading)
                {
                    heading = false;
                    continue;
                }

                string[] cell = line.Split(',');
                if (cell.Length < Columns) continue;

                _items.Add(new ItemDefinition(
                    itemId: cell[ItemIdColumn].Trim(),
                    name: cell[NameColumn].Trim(),
                    maxStock: Number(cell[MaxStockColumn]),
                    maxDurability: Number(cell[MaxDurabilityColumn]),
                    installable: Number(cell[InstallableColumn]) == 1,
                    attack: Number(cell[AttackColumn]),
                    knockback: Real(cell[KnockbackColumn]),
                    use: Use(cell[UseColumn]),
                    heal: Number(cell[HealColumn]),
                    description: cell[DescriptionColumn].Trim()));
            }
        }

        /// <summary>見つからなければ null。呼ぶ側で探索を書かない</summary>
        public ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ItemId == itemId) return _items[i];
            }

            return null;
        }

        private static int Number(string cell)
        {
            int value;

            return int.TryParse(cell.Trim(), out value) ? value : 0;
        }

        private static float Real(string cell)
        {
            float value;

            return float.TryParse(cell.Trim(), NumberStyles.Float,
                                  CultureInfo.InvariantCulture, out value) ? value : 0f;
        }

        /// <summary>読めない値と空欄は設置にする。列を足す前の行がそう読まれる</summary>
        private static ItemUse Use(string cell)
        {
            switch (Number(cell))
            {
                case 1: return ItemUse.Eat;
                case 2: return ItemUse.Equip;
                default: return ItemUse.Place;
            }
        }
    }
}
